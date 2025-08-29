import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { useState } from "react";
import ApiService from "../../services/ApiService";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { Tag } from "primereact/tag";
import { DateService } from "../../services/DateService";

export default function TrainGroupsBookingCalendarPage() {
  const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<TimeSlotResponseDto | null>(
    null
  );

  const handleChangeDate = (value: Date) => {
    const dateCleaned = new Date(
      value.getFullYear(),
      value.getMonth(),
      value.getDate(),
      0,
      0,
      0,
      0
    );
    setSelectedDate(dateCleaned);
    setSelectedSlot(null); // Reset selected slot when date changes

    const timeSlotDto = new TimeSlotRequestDto();
    timeSlotDto.selectedDate = dateCleaned;
    ApiService.timeslots("TrainGroupDates/TimeSlots", timeSlotDto).then(
      (response) => {
        if (response) {
          setTimeSlots(response);
        }
      }
    );
  };

  const handleBooking = () => {
    if (selectedDate && selectedSlot) {
      // Assuming you have a way to get the current user ID, e.g., from TokenService
      // const userId = TokenService.getUserId();
      const bookingDto = {
        trainGroupDateId: selectedSlot.trainGroupDateId,
        // userId: userId, // Uncomment and adjust as needed
      };

      ApiService.create("TrainGroups", bookingDto)
        .then((response) => {
          if (response) {
            alert(
              `Booking confirmed for ${selectedDate.toDateString()} at ${new Date(
                selectedSlot.startOn
              ).toLocaleTimeString()}`
            );
            // Refresh time slots after booking
            handleChangeDate(selectedDate);
          }
        })
        .catch((error) => {
          alert("Booking failed. Please try again.");
          console.error(error);
        });
    }
  };

  return (
    <>
      {/*                  */}
      {/*     Calendar     */}
      {/*                  */}
      <div className="grid w-full">
        <div className="col-12 lg:col-6 xl:col-6">
          <Card
            title="Booking Calendar"
            subTitle="Select a date to view available time slots"
          >
            <Calendar
              value={selectedDate}
              onChange={(e) => handleChangeDate(e.value as Date)}
              inline
              showIcon={false}
              minDate={new Date()} // Prevent selecting past dates
              className="w-full"
            />
          </Card>
        </div>

        {/*                  */}
        {/*     Timeslots    */}
        {/*                  */}
        <div className="col-12 lg:col-6 xl:col-6">
          <Card
            header={
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  padding: "1rem",
                }}
              >
                <h2 style={{ margin: 0 }}>Available Time Slots</h2>
              </div>
            }
          >
            {timeSlots.length === 0 ? (
              <p className="text-gray-500">
                No time slots available for this date.
              </p>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {timeSlots.map((slot) => (
                  <Button
                    key={slot.trainGroupDateId}
                    label={`${new Date(slot.startOn).toLocaleTimeString()} - ${
                      slot.title || "Train Group"
                    }`}
                    className={
                      selectedSlot?.trainGroupDateId === slot.trainGroupDateId
                        ? "p-button-raised p-button-primary"
                        : "p-button-outlined"
                    }
                    // disabled={!slot.isAvailable}
                    onClick={() => setSelectedSlot(slot)}
                  />
                ))}
              </div>
            )}
          </Card>

          {/*                       */}
          {/*     Timeslot Info     */}
          {/*                       */}
          {selectedSlot && (
            <Card
              title="Selected Time Slot Details"
              className="mt-4"
            >
              <p>
                <strong>Time:</strong>{" "}
                {new Date(selectedSlot.startOn).toLocaleTimeString()}
              </p>
              <p>
                <strong>Group Name:</strong> {selectedSlot.title || "N/A"}
              </p>
              <p>
                <strong>Trainer:</strong> {selectedSlot.trainerId || "N/A"}
              </p>
              <p>
                <strong>Description:</strong>{" "}
                {selectedSlot.description || "No description available."}
              </p>
              <p>
                <strong>Spots Left:</strong>{" "}
                {selectedSlot.spotsLeft !== undefined
                  ? selectedSlot.spotsLeft
                  : "N/A"}
              </p>
              <p>
                <strong>Available:</strong>{" "}
                {selectedSlot.spotsLeft > 0 ? "Yes" : "No"}
              </p>

              <h3 className="mt-6">Group is also recurring on </h3>

              <p></p>

              {selectedSlot.recurrenceDates.filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
              ).length > 0 ? (
                <p>
                  <strong>Days of the week:</strong>
                </p>
              ) : null}

              <div>
                {selectedSlot.recurrenceDates
                  .filter(
                    (x) =>
                      x.trainGroupDateType ===
                      TrainGroupDateTypeEnum.DAY_OF_WEEK
                  )
                  .map((x) => (
                    <Tag
                      className="p-2 m-1"
                      key={x.trainGroupDateId}
                      value={DateService.getDayOfWeekFromDate(new Date(x.date))}
                    ></Tag>
                  ))}
              </div>

              {selectedSlot.recurrenceDates.filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
              ).length > 0 ? (
                <p>
                  <strong>Days of the month:</strong>
                </p>
              ) : null}

              <div>
                {selectedSlot.recurrenceDates
                  .filter(
                    (x) =>
                      x.trainGroupDateType ===
                      TrainGroupDateTypeEnum.DAY_OF_MONTH
                  )
                  .map((x) => (
                    <Tag
                      className="p-2 m-1"
                      key={x.trainGroupDateId}
                      value={new Date(x.date).getDate()}
                    ></Tag>
                  ))}
              </div>

              <Button
                label="Book Now"
                icon="pi pi-check"
                className="mt-4"
                onClick={handleBooking}
                disabled={!(selectedSlot.spotsLeft > 0)}
              />
            </Card>
          )}
        </div>
      </div>
    </>
  );
}
