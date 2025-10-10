import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { useDateService } from "../../services/DateService";
import { Checkbox } from "primereact/checkbox";
import { TokenService } from "../../services/TokenService";
import { TrainGroupParticipantDto } from "../../model/entities/train-group-participant/TrainGroupParticipantDto";
import { DividerComponent } from "../../components/core/divider/DividerComponent";
import { JSX } from "react";
import { useTranslator } from "../../services/TranslatorService";

interface IField {}

export default function TrainGroupsBookingCalendarTimeslotBookFormComponent({}: IField) {
  const { t } = useTranslator();
  const { getDayOfWeekFromDate } = useDateService();
  const {
    timeSlotRequestDto,
    selectedTimeSlot,
    trainGroupDateParticipantUpdateDto,
    updateTrainGroupDateParticipantUpdateDto,
  } = useTrainGroupBookingStore();

  // Helper to check if a participant exists for a given trainGroupDateId.
  // If Recurring date is checked, current date will also be checked.
  const hasParticipantForDateId = (
    trainGroupDateId: number,
    type: TrainGroupDateTypeEnum | undefined
  ): boolean => {
    if (type === undefined) {
      return trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.some(
        (x) => x.trainGroupDateId === trainGroupDateId
      );
    }

    return trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.some(
      (x) =>
        x.trainGroupDateId === trainGroupDateId &&
        (type === undefined
          ? x.selectedDate !== undefined
          : x.selectedDate === undefined)
    );
  };

  // Helper to update the store by toggling a participant for a dateId
  const toggleParticipant = (
    trainGroupDateId: number,
    type: TrainGroupDateTypeEnum | undefined
  ) => {
    let newParticipants: TrainGroupParticipantDto[] = [];
    const isChecked = hasParticipantForDateId(trainGroupDateId, type);

    if (isChecked)
      newParticipants =
        trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
          (x) =>
            x.trainGroupDateId !== trainGroupDateId ||
            (x.trainGroupDateId === trainGroupDateId &&
              (type === undefined
                ? x.selectedDate === undefined
                : x.selectedDate !== undefined))
        );
    else {
      // If current date is selected and a recurring date is also just selected,
      // remove current participant and add only for recurring date
      const currentParticipantExists =
        trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.some(
          (x) =>
            x.trainGroupDateId === trainGroupDateId &&
            x.selectedDate !== undefined
        );

      let filteredParticipants: TrainGroupParticipantDto[] =
        trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos;

      if (type !== undefined && currentParticipantExists)
        filteredParticipants =
          trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
            (x) => x.selectedDate === undefined
          );

      newParticipants = [
        ...filteredParticipants,
        {
          id: 0,
          selectedDate:
            type === undefined ? timeSlotRequestDto.selectedDate : undefined,
          trainGroupId: selectedTimeSlot!.trainGroupId,
          trainGroupDateId: trainGroupDateId,
          userId: TokenService.getUserId() ?? "",
        },
      ];
    }

    updateTrainGroupDateParticipantUpdateDto({
      trainGroupParticipantDtos: newParticipants,
    });

    console.log(newParticipants);
  };

  // Helper to generate label for a date based on type
  const getDateLabel = (
    date: string,
    type: TrainGroupDateTypeEnum | undefined
  ): string => {
    switch (type) {
      case TrainGroupDateTypeEnum.DAY_OF_WEEK:
        return getDayOfWeekFromDate(new Date(date)) ?? "";
      case TrainGroupDateTypeEnum.DAY_OF_MONTH:
        return new Date(date).getDate().toString();
      case TrainGroupDateTypeEnum.FIXED_DAY:
      case undefined: // One-off
        return new Date(date).toLocaleDateString("en-GB");
      default:
        return "";
    }
  };

  const renderDateCheckbox = (type: TrainGroupDateTypeEnum | undefined) => {
    if (!selectedTimeSlot) return;
    const htmlElements: JSX.Element[] = [];

    selectedTimeSlot!.recurrenceDates
      .filter((x) => x.trainGroupDateType === type)
      .forEach((date) => {
        const label = getDateLabel(date.date, type);
        const isDisabled: boolean =
          type === undefined &&
          trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.some(
            (x) =>
              x.selectedDate === undefined &&
              x.trainGroupDateId === date.trainGroupDateId
          );

        htmlElements.push(
          <div
            key={type?.toString() + date.trainGroupDateId.toString()}
            className="field-checkbox"
          >
            <Checkbox
              inputId={date.trainGroupDateId.toString()}
              checked={hasParticipantForDateId(date.trainGroupDateId, type)}
              onChange={(e) => toggleParticipant(date.trainGroupDateId, type)}
              disabled={isDisabled}
            />
            <label
              htmlFor={type?.toString() + date.trainGroupDateId.toString()}
              className="ml-2"
            >
              {label}
            </label>
          </div>
        );
      });

    return htmlElements;
  };

  return (
    <>
      <div className="p-fluid">
        <div className="text-center mb-4">
          <p className="text-lg font-semibold mb-0">
            {t("Select the dates you want to book for this training session.")}
          </p>
          <p className="text-sm text-gray-600 mt-0">
            {t(
              "One-off dates apply only to the selected date, while recurring dates apply to all future sessions on that day of the week or month."
            )}
          </p>
        </div>

        {selectedTimeSlot && (
          <div className="p-fluid">
            {selectedTimeSlot.recurrenceDates.length > 0 && (
              <>
                {/*                      */}
                {/*     Current Date     */}
                {/*                      */}
                {selectedTimeSlot.recurrenceDates.some(
                  (x) => x.trainGroupDateType === undefined
                ) && (
                  <div className="field">
                    <DividerComponent>
                      <p>
                        <strong className="text-base">{t("One-off")}</strong>
                      </p>
                    </DividerComponent>
                    {renderDateCheckbox(undefined)}
                  </div>
                )}

                {/*                      */}
                {/*       FIXED_DAY      */}
                {/*                      */}
                {selectedTimeSlot.recurrenceDates.some(
                  (x) =>
                    x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
                ) && (
                  <div className="field">
                    <DividerComponent>
                      <p>
                        <strong className="text-base">{t("One-off")}</strong>
                      </p>
                    </DividerComponent>

                    {renderDateCheckbox(TrainGroupDateTypeEnum.FIXED_DAY)}
                  </div>
                )}

                {/*                        */}
                {/*       DAY_OF_WEEK      */}
                {/*                        */}
                {selectedTimeSlot.recurrenceDates.some(
                  (x) =>
                    x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
                ) && (
                  <div className="field">
                    <DividerComponent>
                      <p>
                        <strong className="text-base">
                          {t("Recurring Dates (Day of Week)")}
                        </strong>
                      </p>
                    </DividerComponent>

                    {renderDateCheckbox(TrainGroupDateTypeEnum.DAY_OF_WEEK)}
                  </div>
                )}

                {/*                        */}
                {/*       DAY_OF_MONTH     */}
                {/*                        */}
                {selectedTimeSlot.recurrenceDates.some(
                  (x) =>
                    x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
                ) && (
                  <div className="field">
                    <DividerComponent>
                      <p>
                        <strong className="text-base">
                          {t("Recurring Dates (Day of month)")}
                        </strong>
                      </p>
                    </DividerComponent>

                    {renderDateCheckbox(TrainGroupDateTypeEnum.DAY_OF_MONTH)}
                  </div>
                )}
              </>
            )}
          </div>
        )}
      </div>
    </>
  );
}
