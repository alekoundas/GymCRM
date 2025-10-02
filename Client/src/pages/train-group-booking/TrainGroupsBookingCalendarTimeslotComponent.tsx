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
      <Card title="Available Time Slots">
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
                onClick={() => setSelectedTimeSlot(slot)}
              />
            ))}
          </div>
        )}
      </Card>
    </>
  );
}
