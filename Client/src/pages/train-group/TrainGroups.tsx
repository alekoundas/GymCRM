import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { Dialog } from "primereact/dialog";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { TokenService } from "../../services/TokenService";
import TrainGroupForm from "./TrainGroupForm";
import { FormMode } from "../../enum/FormMode";
import TrainGroupDateGrid from "../train-group-date/TrainGroupDateGrid";
import { useTrainGroupStore } from "./TrainGroupStore";
import ApiService from "../../services/ApiService";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";

function TrainGroups() {
  const navigate = useNavigate();

  const [isModalVisible, setModalVisibility] = useState(false); // Dialog visibility
  const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);
  const [selectedTimeSlot, setSelectedTimeSlot] = useState<number | null>(null);
  const { resetTrainGroupDto, trainGroupDto } = useTrainGroupStore();

  // const createNewData = (): TrainGroupDto => ({
  //   id: -1,
  //   name: "",
  //   description: "",
  //   isRepeating: false,
  //   duration: new Date(),
  //   startOn: new Date(),
  //   maxParticipants: 1,
  //   trainerId: "",
  //   repeatingParticipants: [],
  //   trainGroupDates: [],
  // });
  // const [trainGroupDto, setTrainGroupDto] = useState<TrainGroupDto>(
  //   createNewData()
  // );

  // Mock function to generate time slots based on selected date
  // useEffect(() => {
  //   if (selectedDate) {
  //     // Simulate fetching time slots for the selected date
  //     const mockTimeSlots: TimeSlot[] = [
  //       { id: 1, time: "09:00 AM", available: true },
  //       { id: 2, time: "10:00 AM", available: true },
  //       { id: 3, time: "11:00 AM", available: false },
  //       { id: 4, time: "01:00 PM", available: true },
  //       { id: 5, time: "02:00 PM", available: true },
  //       { id: 6, time: "03:00 PM", available: false },
  //     ];
  //     setTimeSlots(mockTimeSlots);
  //     setSelectedTimeSlot(null); // Reset selected time slot when date changes
  //   }
  // }, [selectedDate]);

  const handleChangeDate = (value: Date) => {
    setSelectedDate(value);

    const timeSlotDto = new TimeSlotRequestDto();
    timeSlotDto.selectedDate = value;
    ApiService.timeslots("TrainGroupDate/TimeSlots", timeSlotDto).then(
      (response) => {
        if (response) {
          setTimeSlots(response);
        }
      }
    );
  };

  const handleTimeSlotClick = (slotId: number) => {
    setSelectedTimeSlot(slotId);
  };

  const handleBooking = () => {
    if (selectedDate && selectedTimeSlot) {
      const selectedSlot = timeSlots.find(
        (slot) => slot.trainGroupDateId === selectedTimeSlot
      );
      alert(
        `Booking confirmed for ${selectedDate.toDateString()} at ${
          selectedSlot?.startOn
        }`
      );
      // Call your API here to book the slot (e.g., POST to api/TrainGroupParticipant)
    }
  };

  const handleSaveTrainGroup = async () => {
    const response = await ApiService.create("trainGroup", trainGroupDto);

    if (response) {
      resetTrainGroupDto();
      setModalVisibility(false);
    }
  };

  return (
    <>
      <div className="grid w-full ">
        <div className="col-12 lg:col-6 xl:col-6">
          <Card
            title="Train Groups"
            subTitle="Handle your train groups"
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

        {/*                     */}
        {/* Time Slots Section  */}
        {/*                     */}
        <div className=" col-12  lg:col-6 xl:col-6 ">
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
                <h2 style={{ margin: 0 }}>Available Timeslots</h2>
                {TokenService.isUserAllowed("TrainGroup_View") && (
                  <Button
                    className="m-2"
                    type="button"
                    icon="pi pi-plus"
                    label="Add"
                    outlined
                    onClick={() => {
                      setModalVisibility(true);
                      resetTrainGroupDto();
                    }}
                  />
                )}
              </div>
            }
          >
            {}
            {timeSlots.length === 0 ? (
              <p className="text-gray-500">
                No time slots available for this date.
              </p>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {timeSlots.map((slot) => (
                  <Button
                    key={slot.trainGroupDateId}
                    label={slot.displayDate}
                    disabled={!slot.available}
                    onClick={() =>
                      slot.available &&
                      handleTimeSlotClick(slot.trainGroupDateId)
                    }
                  />
                ))}
              </div>
            )}

            {/* {TokenService.isUserAllowed("Customers_View") && (
              <div className="mt-6">
                <Button
                  label="Book Now"
                  icon="pi pi-check"
                  className="p-button-raised p-button-success w-full"
                  disabled={!selectedTimeSlot}
                  //   onClick={handleBooking}
                  onClick={() => navigate("/administrator/customers/add")}
                />
              </div>
            )} */}
          </Card>
        </div>
      </div>

      {/*                                   */}
      {/* Modal of Contact Information Form */}
      {/*                                   */}
      <Dialog
        visible={isModalVisible}
        style={{ width: "35%" }}
        header="Add New Timeslot"
        modal
        className="p-fluid"
        footer={
          <div>
            <Button
              label="Cancel"
              icon="pi pi-times"
              className="p-button-text"
              onClick={() => setModalVisibility(false)}
            />
            <Button
              label="Save"
              icon="pi pi-check"
              onClick={handleSaveTrainGroup}
            />
          </div>
        }
        onHide={() => setModalVisibility(false)}
      >
        <TrainGroupForm formMode={FormMode.ADD} />
        <TrainGroupDateGrid formMode={FormMode.ADD} />
      </Dialog>
    </>
  );
}

export default TrainGroups;
