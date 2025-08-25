import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { Dialog } from "primereact/dialog";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { TokenService } from "../../services/TokenService";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupForm from "./TrainGroupForm";
import TrainGroupDateGrid from "../train-group-date/TrainGroupDateGrid";

export default function TrainGroupAdminPage() {
  const navigate = useNavigate();

  const [isEditModalVisible, setEditModalVisibility] = useState(false); // Dialog visibility
  const [isAddModalVisible, setAddModalVisibility] = useState(false); // Dialog visibility
  const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);
  const [selectedTimeSlot, setSelectedTimeSlot] = useState<number | null>(null);
  const { resetTrainGroupDto, trainGroupDto } = useTrainGroupStore();

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

  const dialogControlAdd: DialogControl = {
    showDialog: () => setAddModalVisibility(true),
    hideDialog: () => setAddModalVisibility(false),
  };

  const dialogControlEdit: DialogControl = {
    showDialog: () => setAddModalVisibility(true),
    hideDialog: () => setAddModalVisibility(false),
  };

  const OnSaveAdd = async () => {
    const response = await ApiService.create("trainGroup", trainGroupDto);

    if (response) {
      resetTrainGroupDto();
    }
  };

  const OnSaveEdit = async () => {
    const response = await ApiService.update(
      "trainGroup",
      trainGroupDto,
      trainGroupDto.id
    );

    if (response) {
      resetTrainGroupDto();
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
                {TokenService.isUserAllowed("TrainGroup_Add") && (
                  <Button
                    className="m-2"
                    type="button"
                    icon="pi pi-plus"
                    label="Add"
                    outlined
                    onClick={() => {
                      setAddModalVisibility(true);
                      resetTrainGroupDto();
                    }}
                  />
                )}
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
                    label={slot.displayDate}
                    // disabled={!slot.available}
                    onClick={() => handleTimeSlotClick(slot.trainGroupDateId)}
                  />
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>

      {/*                                     */}
      {/*           Add Train Group           */}
      {/*                                     */}

      <GenericDialogComponent
        visible={isAddModalVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <TrainGroupForm formMode={FormMode.ADD} />
          <TrainGroupDateGrid formMode={FormMode.ADD} />
        </div>
      </GenericDialogComponent>

      {/* 
      <Dialog
        visible={isAddModalVisible}
        style={{ width: "35%" }}
        header="Add New Timeslot"
        modal
        className="p-fluid"
        onHide={() => setAddModalVisibility(false)}
      >
        <TrainGroupContainer formMode={FormMode.ADD} />
      </Dialog> */}

      {/*                                     */}
      {/*          Edit Train Group           */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isEditModalVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <TrainGroupForm formMode={FormMode.EDIT} />
          <TrainGroupDateGrid formMode={FormMode.EDIT} />
        </div>
      </GenericDialogComponent>
    </>
  );
}
