import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { DateService } from "../../services/DateService";
import { Checkbox } from "primereact/checkbox";
import { useState } from "react";

interface IField {}

export default function TrainGroupsBookingCalendarTimeslotBookFormComponent({}: IField) {
  const { timeSlotRequestDto, selectedTimeSlotResponseDto } =
    useTrainGroupBookingStore();
  const [bookCurrentDate, setBookCurrentDate] = useState<boolean>(false);
  const [selectedRecurrenceDates, setSelectedRecurrenceDates] = useState<
    number[]
  >([]);
  return (
    <>
      {selectedTimeSlotResponseDto && (
        <div className="p-fluid">
          <h3>{"datatable.select_booking_options"}</h3>
          <div className="field">
            <Checkbox
              inputId="bookCurrentDate"
              checked={bookCurrentDate}
              onChange={(e) => setBookCurrentDate(e.checked ?? false)}
              disabled={selectedTimeSlotResponseDto.spotsLeft <= 0}
            />
            <label
              htmlFor="bookCurrentDate"
              className="ml-2"
            >
              {timeSlotRequestDto.selectedDate?.toLocaleDateString()}
              {/* {t("datatable.book_current_date", {
                       date: selectedDate?.toLocaleDateString(),
                       time: new Date(selectedSlot.startOn).toLocaleTimeString(),
                     })} */}
            </label>
          </div>

          {selectedTimeSlotResponseDto.recurrenceDates.length > 0 && (
            <>
              <h4>{"datatable.recurring_dates"}</h4>
              {selectedTimeSlotResponseDto.recurrenceDates.filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
              ).length > 0 && (
                <div className="field">
                  <p>
                    <strong>{"datatable.days_of_week"}:</strong>
                  </p>
                  {selectedTimeSlotResponseDto.recurrenceDates
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
                          checked={selectedRecurrenceDates.includes(
                            x.trainGroupDateId
                          )}
                          onChange={(e) => {
                            setSelectedRecurrenceDates(
                              e.checked
                                ? [
                                    ...selectedRecurrenceDates,
                                    x.trainGroupDateId,
                                  ]
                                : selectedRecurrenceDates.filter(
                                    (id) => id !== x.trainGroupDateId
                                  )
                            );
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

              {selectedTimeSlotResponseDto.recurrenceDates.filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
              ).length > 0 && (
                <div className="field">
                  <p>
                    <strong>{"datatable.days_of_month"}:</strong>
                  </p>
                  {selectedTimeSlotResponseDto.recurrenceDates
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
                          checked={selectedRecurrenceDates.includes(
                            x.trainGroupDateId
                          )}
                          onChange={(e) => {
                            setSelectedRecurrenceDates(
                              e.checked
                                ? [
                                    ...selectedRecurrenceDates,
                                    x.trainGroupDateId,
                                  ]
                                : selectedRecurrenceDates.filter(
                                    (id) => id !== x.trainGroupDateId
                                  )
                            );
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
