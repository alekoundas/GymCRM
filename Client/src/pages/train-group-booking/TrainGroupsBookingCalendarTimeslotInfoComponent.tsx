import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { useDateService } from "../../services/DateService";
import { Tag } from "primereact/tag";
import { DividerComponent } from "../../components/core/divider/DividerComponent";
import { JSX } from "react";
import { TokenService } from "../../services/TokenService";
import { useTranslator } from "../../services/TranslatorService";

interface IField {
  onBook: () => void;
}

export default function TrainGroupsBookingCalendarTimeslotInfoComponent({
  onBook,
}: IField) {
  const { t } = useTranslator();
  const { getDayOfWeekFromDate } = useDateService();

  const { timeSlotRequestDto, selectedTimeSlot, timeSlotResponseDto } =
    useTrainGroupBookingStore();

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

  // Helper to render a Tag for a date
  const renderDateTag = (
    type: TrainGroupDateTypeEnum | undefined
  ): JSX.Element[] => {
    const htmlElements: JSX.Element[] = [];

    selectedTimeSlot!.recurrenceDates
      .filter((x) => x.trainGroupDateType === type)
      .forEach((date) => {
        const label = getDateLabel(date.date, type);
        let isJoined = date.isUserJoined;

        if (type === undefined && !isJoined)
          isJoined = selectedTimeSlot!.recurrenceDates.some(
            (x) =>
              x.trainGroupDateId === date.trainGroupDateId &&
              x.trainGroupDateType !== undefined &&
              x.isUserJoined
          );
        htmlElements.push(
          <Tag
            key={date.trainGroupDateId}
            className="p-2 m-1"
            severity={isJoined ? "success" : undefined}
          >
            {label}
            {isJoined && <i className="pi pi-check ml-1" />}
          </Tag>
        );
      });

    return htmlElements;
  };

  return (
    <>
      {timeSlotRequestDto.selectedDate && selectedTimeSlot && (
        <Card
          title={t("Selected Time Slot Details")}
          className="mt-4"
        >
          <p>
            <strong>{t("Start On")}:</strong>{" "}
            {new Date(selectedTimeSlot.startOn).toLocaleTimeString()}
          </p>
          <p>
            <strong>{t("Group Name")}:</strong>{" "}
            {selectedTimeSlot.title || "N/A"}
          </p>
          <p>
            <strong>{t("Trainer")}:</strong>{" "}
            {selectedTimeSlot.trainerId || "N/A"}
          </p>
          <p>
            <strong>{t("Spots Left")}:</strong>{" "}
            {selectedTimeSlot.spotsLeft !== undefined
              ? selectedTimeSlot.spotsLeft
              : "N/A"}
          </p>
          <p>
            <strong>{t("Available")}:</strong>{" "}
            {selectedTimeSlot.spotsLeft > 0 ? t("Yes") : t("No")}
          </p>
          <p>
            <strong>{t("Description")}:</strong>{" "}
            {selectedTimeSlot.description || t("No description available.")}
          </p>

          <h3 className="mt-6">{t("Group is occurring on")}:</h3>

          {/*                      */}
          {/*     Current Date     */}
          {/*                      */}
          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) => x.trainGroupDateType === undefined
            ) && (
              <>
                <DividerComponent>
                  <p>
                    <strong className="text-base">{t("One-off")}</strong>
                  </p>
                </DividerComponent>
                {renderDateTag(undefined)}
              </>
            )}
          </div>

          {/*                      */}
          {/*       FIXED_DAY      */}
          {/*                      */}
          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
            ) && (
              <>
                {/* <p>
                  <strong>One-off:</strong>
                </p> */}
                <DividerComponent>
                  <p>
                    <strong className="text-base">{t("One-off")}</strong>
                  </p>
                </DividerComponent>
                {renderDateTag(TrainGroupDateTypeEnum.FIXED_DAY)}
              </>
            )}
          </div>

          {/*                        */}
          {/*       DAY_OF_WEEK      */}
          {/*                        */}
          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
            ) && (
              <>
                <DividerComponent>
                  <p>
                    <strong className="text-base">
                      {t("Recurring Dates (Day of Week)")}
                    </strong>
                  </p>
                </DividerComponent>
                {renderDateTag(TrainGroupDateTypeEnum.DAY_OF_WEEK)}
              </>
            )}
          </div>

          {/*                        */}
          {/*       DAY_OF_MONTH     */}
          {/*                        */}
          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) =>
                x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
            ) && (
              <>
                <DividerComponent>
                  <p>
                    <strong className="text-base">
                      {t("Recurring Dates (Day of month)")}
                    </strong>
                  </p>
                </DividerComponent>
                {renderDateTag(TrainGroupDateTypeEnum.DAY_OF_MONTH)}
              </>
            )}
          </div>
          {TokenService.getUserId() && (
            <div className="flex justify-content-center">
              <Button
                label={t("Book Now")}
                icon="pi pi-check"
                className="mt-4 pr-5 pl-5 pt-3 pb-3"
                onClick={onBook}
              />
            </div>
          )}
        </Card>
      )}
    </>
  );
}
