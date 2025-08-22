import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { classNames } from "primereact/utils";
import { useEffect, useState } from "react";

interface TimeSlot {
  id: number;
  time: string;
  available: boolean;
}

function TrainGroups() {
  const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  const [timeSlots, setTimeSlots] = useState<TimeSlot[]>([]);
  const [selectedTimeSlot, setSelectedTimeSlot] = useState<number | null>(null);

  // Mock function to generate time slots based on selected date
  useEffect(() => {
    if (selectedDate) {
      // Simulate fetching time slots for the selected date
      const mockTimeSlots: TimeSlot[] = [
        { id: 1, time: "09:00 AM", available: true },
        { id: 2, time: "10:00 AM", available: true },
        { id: 3, time: "11:00 AM", available: false },
        { id: 4, time: "01:00 PM", available: true },
        { id: 5, time: "02:00 PM", available: true },
        { id: 6, time: "03:00 PM", available: false },
      ];
      setTimeSlots(mockTimeSlots);
      setSelectedTimeSlot(null); // Reset selected time slot when date changes
    }
  }, [selectedDate]);

  const handleTimeSlotClick = (slotId: number) => {
    setSelectedTimeSlot(slotId);
  };

  const handleBooking = () => {
    if (selectedDate && selectedTimeSlot) {
      const selectedSlot = timeSlots.find(
        (slot) => slot.id === selectedTimeSlot
      );
      alert(
        `Booking confirmed for ${selectedDate.toDateString()} at ${
          selectedSlot?.time
        }`
      );
      // Call your API here to book the slot (e.g., POST to api/TrainGroupParticipant)
    }
  };

  return (
    <>
      <div className="grid w-full ">
        <div className="col-12 lg:col-6 xl:col-6">
          <Card title="Train Groups">
            <Calendar
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.value as Date)}
              inline
              showIcon={false}
              minDate={new Date()} // Prevent selecting past dates
              className="w-full"
            />
          </Card>
        </div>

        {/* Time Slots Section */}
        <div className=" col-12  lg:col-6 xl:col-6 ">
          <Card title="Available Time Slots">
            {timeSlots.length === 0 ? (
              <p className="text-gray-500">
                No time slots available for this date.
              </p>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {timeSlots.map((slot) => (
                  <Button
                    key={slot.id}
                    label={slot.time}
                    // className={classNames("p-button-text", {
                    //   "bg-blue-500 text-white":
                    //     selectedTimeSlot === slot.id && slot.available,
                    //   "bg-gray-200 text-gray-500 cursor-not-allowed":
                    //     !slot.available,
                    //   "bg-white border border-gray-300 hover:bg-blue-100":
                    //     slot.available && selectedTimeSlot !== slot.id,
                    // })}

                    disabled={!slot.available}
                    onClick={() =>
                      slot.available && handleTimeSlotClick(slot.id)
                    }
                  />
                ))}
              </div>
            )}
            <div className="mt-6">
              <Button
                label="Book Now"
                icon="pi pi-check"
                className="p-button-raised p-button-success w-full"
                disabled={!selectedTimeSlot}
                onClick={handleBooking}
              />
            </div>
          </Card>
        </div>
      </div>
    </>
  );
}

export default TrainGroups;
