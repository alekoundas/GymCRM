import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { DateService } from "../../services/DateService";
import { Checkbox } from "primereact/checkbox";
import { useState } from "react";
import { TokenService } from "../../services/TokenService";
import { TrainGroupParticipantDto } from "../../model/TrainGroupParticipantDto";

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
              {/* <h4>{"datatable.select_booking_options"}</h4> */}
              {selectedTimeSlot.recurrenceDates.some(
                (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
              ) && (
                <div className="field">
                  <p>
                    <strong>{"Current Date"}:</strong>
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
                              (y) => y.trainGroupDateId == x.trainGroupDateId
                            ).length === 1
                          }
                          onChange={(e) => {
                            updateTrainGroupDateParticipantUpdateDto({
                              trainGroupParticipantDtos: e.checked
                                ? [
                                    ...trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos,
                                    {
                                      id: 0,
                                      trainGroupId:
                                        selectedTimeSlot.trainGroupId,
                                      trainGroupDateId:
                                        selectedTimeSlot.trainGroupDateId,
                                      userId: TokenService.getUserId(),
                                    } as TrainGroupParticipantDto,
                                  ]
                                : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                                    (y) => y.selectedDate === null
                                  ),
                            });
                          }}
                          // disabled={selectedTimeSlot.spotsLeft <= 0}
                        />
                        <label
                          htmlFor={`recurrence-${x.trainGroupDateId}`}
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

              {!selectedTimeSlot.recurrenceDates.some(
                (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
              ) && (
                <div className="field">
                  <p>
                    <strong>{"Current Date"}:</strong>
                  </p>
                  {selectedTimeSlot.recurrenceDates
                    .filter((x) => x.trainGroupDateType === undefined)
                    .map((x) => (
                      <div
                        key={x.trainGroupDateId}
                        className="field-checkbox"
                      >
                        <Checkbox
                          inputId={`recurrence-${x.trainGroupDateId}`}
                          checked={
                            trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                              (y) => y.trainGroupDateId == x.trainGroupDateId
                            ).length === 1
                          }
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
                                      trainGroupDateId: undefined,
                                      userId: TokenService.getUserId(),
                                    } as TrainGroupParticipantDto,
                                  ]
                                : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                                    (y) => y.selectedDate === null
                                  ),
                            });
                          }}
                          // disabled={selectedTimeSlot.spotsLeft <= 0}
                        />
                        <label
                          htmlFor={`recurrence-${x.trainGroupDateId}`}
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

              <h4>{"Reccuring Dates:"}</h4>
              {selectedTimeSlot.recurrenceDates.filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
              ).length > 0 && (
                <div className="field">
                  <p>
                    <strong>{"Days of Week"}:</strong>
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
                              (y) => y.trainGroupDateId == x.trainGroupDateId
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
                                      y.trainGroupDateId !== x.trainGroupDateId
                                  ),
                            });
                          }}
                          // disabled={selectedTimeSlot.spotsLeft <= 0}
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

              {selectedTimeSlot.recurrenceDates.filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
              ).length > 0 && (
                <div className="field">
                  <p>
                    <strong>{"Days of Month"}:</strong>
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
                              (y) => y.trainGroupDateId == x.trainGroupDateId
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
                                      y.trainGroupDateId !== x.trainGroupDateId
                                  ),
                            });
                          }}
                          // disabled={selectedTimeSlot.spotsLeft <= 0}
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
