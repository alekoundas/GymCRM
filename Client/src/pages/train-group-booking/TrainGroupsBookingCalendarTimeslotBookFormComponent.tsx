import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { DateService } from "../../services/DateService";
import { Checkbox } from "primereact/checkbox";
import { TokenService } from "../../services/TokenService";
import { TrainGroupParticipantDto } from "../../model/entities/train-group-participant/TrainGroupParticipantDto";
import { Divider } from "primereact/divider";

interface IField {}

export default function TrainGroupsBookingCalendarTimeslotBookFormComponent({}: IField) {
  const {
    timeSlotRequestDto,
    selectedTimeSlot,
    trainGroupDateParticipantUpdateDto,
    updateTrainGroupDateParticipantUpdateDto,
  } = useTrainGroupBookingStore();
  return (
    <>
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
                  <p className="mb-0">
                    <strong>One-off</strong>
                  </p>
                  <p className="mt-0 text-gray-600">
                    Join the train group once.
                  </p>

                  {selectedTimeSlot.recurrenceDates
                    .filter((x) => x.trainGroupDateType === undefined)
                    .map((x) => (
                      <div className="field-checkbox">
                        <Checkbox
                          inputId={`recurrence-${x.trainGroupDateId}-current`}
                          checked={trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.some(
                            (y) =>
                              y.selectedDate !== undefined &&
                              y.trainGroupDateId === x.trainGroupDateId
                          )}
                          onChange={(e) => {
                            updateTrainGroupDateParticipantUpdateDto({
                              trainGroupParticipantDtos: e.checked
                                ? [
                                    ...trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos,
                                    {
                                      id: 0,
                                      selectedDate:
                                        timeSlotRequestDto.selectedDate,
                                      trainGroupId:
                                        selectedTimeSlot.trainGroupId,
                                      trainGroupDateId: x.trainGroupDateId,
                                      userId: TokenService.getUserId(),
                                    } as TrainGroupParticipantDto,
                                  ]
                                : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                                    (y) => y.selectedDate === undefined
                                  ),
                            });
                          }}
                        />
                        <label
                          htmlFor={`recurrence-${x.trainGroupDateId}-current`}
                          className="ml-2"
                        >
                          {timeSlotRequestDto.selectedDate
                            ? new Date(
                                timeSlotRequestDto.selectedDate
                              ).getDate() +
                              "/" +
                              (new Date(
                                timeSlotRequestDto.selectedDate
                              ).getMonth() +
                                1) +
                              "/" +
                              new Date(
                                timeSlotRequestDto.selectedDate
                              ).getFullYear()
                            : ""}
                        </label>
                      </div>
                    ))}
                </div>
              )}

              {/*                      */}
              {/*       FIXED_DAY      */}
              {/*                      */}
              {selectedTimeSlot.recurrenceDates.some(
                (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
              ) && (
                <div className="field">
                  <p className="mb-0">
                    <strong>One-off:</strong>
                  </p>
                  <p className="mt-0 text-gray-600">
                    Join the train group once.
                  </p>

                  {selectedTimeSlot.recurrenceDates
                    .filter(
                      (x) =>
                        x.trainGroupDateType ===
                        TrainGroupDateTypeEnum.FIXED_DAY
                    )
                    .map((x) => (
                      <div
                        key={x.trainGroupDateId}
                        className="field-checkbox"
                      >
                        <Checkbox
                          inputId={`recurrence-${x.trainGroupDateId}`}
                          checked={
                            trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                              (y) =>
                                y.selectedDate === undefined &&
                                y.trainGroupDateId === x.trainGroupDateId
                            ).length === 1
                          }
                          onChange={(e) => {
                            updateTrainGroupDateParticipantUpdateDto({
                              trainGroupParticipantDtos: e.checked
                                ? [
                                    ...trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos,
                                    {
                                      id: 0,
                                      selectedDate: undefined,
                                      trainGroupId:
                                        selectedTimeSlot.trainGroupId,
                                      trainGroupDateId: x.trainGroupDateId,
                                      userId: TokenService.getUserId(),
                                    } as TrainGroupParticipantDto,
                                  ]
                                : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                                    (y) =>
                                      y.selectedDate !== undefined ||
                                      (y.selectedDate === undefined &&
                                        y.trainGroupDateId !==
                                          x.trainGroupDateId)
                                  ),
                            });
                          }}
                        />
                        <label
                          htmlFor={`recurrence-${x.trainGroupDateId}`}
                          className="ml-2"
                        >
                          {timeSlotRequestDto.selectedDate
                            ? new Date(x.date).getDate() +
                              "/" +
                              (new Date(x.date).getMonth() + 1) +
                              "/" +
                              new Date(x.date).getFullYear()
                            : ""}
                        </label>
                      </div>
                    ))}
                </div>
              )}

              <Divider align="center">
                <span className="p-tag">Badge</span>
              </Divider>
              {/*                        */}
              {/*       DAY_OF_WEEK      */}
              {/*                        */}
              {selectedTimeSlot.recurrenceDates.some(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
              ) && (
                <div className="field">
                  <p className="mb-0">
                    <strong>Recurring Dates(Day of Week):</strong>
                  </p>
                  <p className="mt-0 text-gray-600">
                    Join the train group for the bellow recurring dates.
                  </p>

                  {selectedTimeSlot.recurrenceDates
                    .filter(
                      (x) =>
                        x.trainGroupDateType ===
                        TrainGroupDateTypeEnum.DAY_OF_WEEK
                    )
                    .map((x) => (
                      <div
                        key={x.trainGroupDateId}
                        className="field-checkbox"
                      >
                        <Checkbox
                          inputId={`recurrence-${x.trainGroupDateId}`}
                          checked={
                            trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                              (y) =>
                                y.selectedDate === undefined &&
                                y.trainGroupDateId == x.trainGroupDateId
                            ).length === 1
                          }
                          onChange={(e) => {
                            updateTrainGroupDateParticipantUpdateDto({
                              trainGroupParticipantDtos: e.checked
                                ? [
                                    ...trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos,
                                    {
                                      id: 0,
                                      selectedDate: undefined,
                                      trainGroupId:
                                        selectedTimeSlot.trainGroupId,
                                      trainGroupDateId: x.trainGroupDateId,
                                      userId: TokenService.getUserId(),
                                    } as TrainGroupParticipantDto,
                                  ]
                                : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                                    (y) =>
                                      y.selectedDate !== undefined ||
                                      (y.selectedDate === undefined &&
                                        y.trainGroupDateId !==
                                          x.trainGroupDateId)
                                  ),
                            });
                          }}
                        />
                        <label
                          htmlFor={`recurrence-${x.trainGroupDateId}`}
                          className="ml-2"
                        >
                          {DateService.getDayOfWeekFromDate(new Date(x.date))}
                        </label>
                      </div>
                    ))}
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
                  <p className="mb-0">
                    <strong>Recurring Dates(Day of the month):</strong>
                  </p>
                  <p className="mt-0 text-gray-600">
                    Join the train group for the bellow recurring dates.
                  </p>

                  {selectedTimeSlot.recurrenceDates
                    .filter(
                      (x) =>
                        x.trainGroupDateType ===
                        TrainGroupDateTypeEnum.DAY_OF_MONTH
                    )
                    .map((x) => (
                      <div
                        key={x.trainGroupDateId}
                        className="field-checkbox"
                      >
                        <Checkbox
                          inputId={`recurrence-${x.trainGroupDateId}`}
                          checked={
                            trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                              (y) =>
                                y.selectedDate === undefined &&
                                y.trainGroupDateId == x.trainGroupDateId
                            ).length === 1
                          }
                          onChange={(e) => {
                            updateTrainGroupDateParticipantUpdateDto({
                              trainGroupParticipantDtos: e.checked
                                ? [
                                    ...trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos,
                                    {
                                      id: 0,
                                      selectedDate: undefined,
                                      trainGroupId:
                                        selectedTimeSlot.trainGroupId,
                                      trainGroupDateId: x.trainGroupDateId,
                                      userId: TokenService.getUserId(),
                                    } as TrainGroupParticipantDto,
                                  ]
                                : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                                    (y) =>
                                      y.selectedDate !== undefined ||
                                      (y.selectedDate === undefined &&
                                        y.trainGroupDateId !==
                                          x.trainGroupDateId)
                                  ),
                            });
                          }}
                        />
                        <label
                          htmlFor={`recurrence-${x.trainGroupDateId}`}
                          className="ml-2"
                        >
                          {new Date(x.date).getDate()}
                        </label>
                      </div>
                    ))}
                </div>
              )}
            </>
          )}
        </div>
      )}
    </>
  );
}
