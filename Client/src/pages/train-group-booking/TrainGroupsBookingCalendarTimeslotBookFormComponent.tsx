import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { DateService } from "../../services/DateService";
import { Checkbox } from "primereact/checkbox";
import { useState } from "react";
import { TokenService } from "../../services/TokenService";

interface IField {}

export default function TrainGroupsBookingCalendarTimeslotBookFormComponent({}: IField) {
  const {
    timeSlotRequestDto,
    selectedTimeSlot,
    trainGroupDateParticipantUpdateDto,
    updateTrainGroupDateParticipantUpdateDto,
  } = useTrainGroupBookingStore();
  const [bookCurrentDate, setBookCurrentDate] = useState<boolean>(false);
  const [selectedRecurrenceDates, setSelectedRecurrenceDates] = useState<
    number[]
  >([]);
  return (
    <>
      {selectedTimeSlot && (
        <div className="p-fluid">
          <h3>{"datatable.select_booking_options"}</h3>
          <div className="field">
            <Checkbox
              inputId="bookCurrentDate"
              checked={
                trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                  (y) => y.selectedDate == timeSlotRequestDto.selectedDate
                ).length === 1
              }
              onChange={(e) => {
                updateTrainGroupDateParticipantUpdateDto({
                  trainGroupParticipantDtos: e.checked
                    ? [
                        ...trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos,
                        {
                          selectedDate: timeSlotRequestDto.selectedDate,
                          trainGroupId: selectedTimeSlot.trainGroupId,
                          trainGroupDateId: undefined,
                          userId: TokenService.getUserId(),
                        },
                      ]
                    : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                        (x) => x.trainGroupId !== selectedTimeSlot.trainGroupId
                      ),
                });
              }}
              disabled={selectedTimeSlot.spotsLeft <= 0}
            />
            <label
              htmlFor="bookCurrentDate"
              className="ml-2"
            >
              {timeSlotRequestDto.selectedDate
                ? new Date(timeSlotRequestDto.selectedDate).toLocaleDateString()
                : ""}
            </label>
          </div>

          {selectedTimeSlot.recurrenceDates.length > 0 && (
            <>
              <h4>{"datatable.recurring_dates"}</h4>
              {selectedTimeSlot.recurrenceDates.filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
              ).length > 0 && (
                <div className="field">
                  <p>
                    <strong>{"datatable.days_of_week"}:</strong>
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
                                      selectedDate: undefined,
                                      trainGroupId:
                                        selectedTimeSlot.trainGroupId,
                                      trainGroupDateId: x.trainGroupDateId,
                                      userId: TokenService.getUserId(),
                                    },
                                  ]
                                : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                                    (x) =>
                                      x.trainGroupDateId !== x.trainGroupDateId
                                  ),
                            });
                          }}
                          disabled={selectedTimeSlot.spotsLeft <= 0}
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
                    <strong>{"datatable.days_of_month"}:</strong>
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
                                      selectedDate: undefined,
                                      trainGroupId:
                                        selectedTimeSlot.trainGroupId,
                                      trainGroupDateId: x.trainGroupDateId,
                                      userId: TokenService.getUserId(),
                                    },
                                  ]
                                : trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.filter(
                                    (x) =>
                                      x.trainGroupDateId !== x.trainGroupDateId
                                  ),
                            });
                          }}
                          disabled={selectedTimeSlot.spotsLeft <= 0}
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
