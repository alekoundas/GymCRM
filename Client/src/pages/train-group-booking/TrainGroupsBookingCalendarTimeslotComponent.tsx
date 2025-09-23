import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";

export default function TrainGroupsBookingCalendarTimeslotComponent() {
  const { timeSlotResponseDto, selectedTimeSlot, setSelectedTimeSlot } =
    useTrainGroupBookingStore();

  return (
    <>
      {/*                  */}
      {/*     Timeslots    */}
      {/*                  */}
      <Card
        title="Available Time Slots"
        // header={
        //   <div
        //     style={{
        //       display: "flex",
        //       justifyContent: "space-between",
        //       alignItems: "center",
        //       padding: "1rem",
        //     }}
        //   >
        //     <h2 style={{ margin: 0 }}>Available Time Slots</h2>
        //   </div>
        // }
      >
        {timeSlotResponseDto?.length === 0 ? (
          <p className="text-gray-500">
            No time slots available for this date.
          </p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {timeSlotResponseDto?.map((slot) => (
              <Button
                key={slot.trainGroupDateId}
                label={`${new Date(slot.startOn).toLocaleTimeString()} - ${
                  slot.title || "Train Group"
                }`}
                className={
                  selectedTimeSlot?.trainGroupDateId === slot.trainGroupDateId
                    ? "p-button-raised p-button-primary"
                    : "p-button-outlined"
                }
                // disabled={!slot.isAvailable}
                onClick={() => setSelectedTimeSlot(slot)}
              />
            ))}
          </div>
        )}
      </Card>
    </>
  );
}
