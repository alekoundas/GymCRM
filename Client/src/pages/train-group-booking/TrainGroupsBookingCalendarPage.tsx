import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { useState } from "react";
import ApiService from "../../services/ApiService";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { TokenService } from "../../services/TokenService";
import { FormMode } from "../../enum/FormMode";
import TrainGroupsBookingCalendarTimeslotComponent from "./TrainGroupsBookingCalendarTimeslotComponent";
import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import TrainGroupsBookingCalendarTimeslotInfoComponent from "./TrainGroupsBookingCalendarTimeslotInfoComponent";
import TrainGroupsBookingCalendarTimeslotBookFormComponent from "./TrainGroupsBookingCalendarTimeslotBookFormComponent";
import { ToastService } from "../../services/ToastService";
import { TrainGroupParticipantDto } from "../../model/entities/train-group-participant/TrainGroupParticipantDto";

export default function TrainGroupsBookingCalendarPage() {
  const {
    timeSlotRequestDto,
    selectedTimeSlot,
    trainGroupDateParticipantUpdateDto,
    resetTimeSlotRequestDto,
    updateTrainGroupDateParticipantUpdateDto,
    resetTimeSlotResponseDto,
    resetSelectedTimeSlotResponseDto,
  } = useTrainGroupBookingStore();
  const [isDialogVisible, setDialogVisibility] = useState(false);
  const [loading, setLoading] = useState<boolean>(false);

  const dialogControl: DialogControl = {
    showDialog: () => setDialogVisibility(true),
    hideDialog: () => setDialogVisibility(false),
  };

  const handleChangeDate = (value: Date) => {
    const dateCleaned = new Date(
      Date.UTC(value.getFullYear(), value.getMonth(), value.getDate(), 0, 0, 0)
    );

    // Reset selected slot when date changes
    resetTimeSlotResponseDto();
    resetSelectedTimeSlotResponseDto();

    resetTimeSlotRequestDto(dateCleaned.toISOString());
    updateTrainGroupDateParticipantUpdateDto({
      selectedDate: dateCleaned.toISOString(),
    });
  };

  const onBookNowClick = () => {
    const trainGroupParticipants = selectedTimeSlot?.recurrenceDates
      .filter((x) => x.isUserJoined)
      .map(
        (x) =>
          ({
            id: x.trainGroupParticipantId,
            selectedDate: x.trainGroupDateType ? undefined : x.date,
            trainGroupDateId: x.trainGroupDateId,
            userId: TokenService.getUserId(),
            trainGroupId: selectedTimeSlot.trainGroupId,
          } as TrainGroupParticipantDto)
      );

    updateTrainGroupDateParticipantUpdateDto({
      userId: TokenService.getUserId(),
      trainGroupParticipantDtos: trainGroupParticipants,
    });
    setDialogVisibility(true);
  };

  const onSave = async (): Promise<void> => {
    if (!timeSlotRequestDto.selectedDate || !selectedTimeSlot)
      return Promise.resolve();

    trainGroupDateParticipantUpdateDto.trainGroupId =
      selectedTimeSlot.trainGroupId;

    if (
      trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.length === 0
    ) {
      ToastService.showWarn("No dates selected!");
    }

    setLoading(true);
    return await ApiService.updateParticipants(
      trainGroupDateParticipantUpdateDto
    )
      .then((response) => {
        if (response) {
          dialogControl.hideDialog();
          handleChangeDate(new Date(timeSlotRequestDto.selectedDate)); // Refresh time slots
        }
      })
      .then(() => setLoading(false));
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
              value={new Date(timeSlotRequestDto.selectedDate)}
              onChange={(e) => handleChangeDate(e.value as Date)}
              inline
              showIcon={false}
              minDate={new Date()} // Prevent selecting past dates
              className="w-full"
            />
          </Card>
        </div>

        <div className="col-12 lg:col-6 xl:col-6">
          <TrainGroupsBookingCalendarTimeslotComponent />
          <TrainGroupsBookingCalendarTimeslotInfoComponent
            onBook={onBookNowClick}
          />
        </div>
      </div>

      {/*                                      */}
      {/*           Book a timeslot            */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.ADD}
        header={""}
        visible={isDialogVisible}
        control={dialogControl}
        onSave={onSave}
      >
        <TrainGroupsBookingCalendarTimeslotBookFormComponent />
      </GenericDialogComponent>
    </>
  );
}
