import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { useEffect, useState } from "react";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { TokenService } from "../../services/TokenService";
import { FormMode } from "../../enum/FormMode";
import TrainGroupsBookingCalendarTimeslotComponent from "./TrainGroupsBookingCalendarTimeslotComponent";
import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import TrainGroupsBookingCalendarTimeslotInfoComponent from "./TrainGroupsBookingCalendarTimeslotInfoComponent";
import TrainGroupsBookingCalendarTimeslotBookFormComponent from "./TrainGroupsBookingCalendarTimeslotBookFormComponent";
import { TrainGroupParticipantDto } from "../../model/entities/train-group-participant/TrainGroupParticipantDto";
import { useToast } from "../../contexts/ToastContext";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

export default function TrainGroupsBookingCalendarPage() {
  const { t } = useTranslator();
  const {
    timeSlotRequestDto,
    selectedTimeSlot,
    trainGroupDateParticipantUpdateDto,
    resetTimeSlotRequestDto,
    updateTrainGroupDateParticipantUpdateDto,
    setTimeSlotResponseDto,
    resetTimeSlotResponseDto,
    resetSelectedTimeSlotResponseDto,
  } = useTrainGroupBookingStore();
  const apiService = useApiService();

  const [isDialogVisible, setDialogVisibility] = useState(false);
  const [loading, setLoading] = useState<boolean>(false);
  const { showSuccess, showInfo, showWarn, showError } = useToast();

  const dialogControl: DialogControl = {
    showDialog: () => setDialogVisibility(true),
    hideDialog: () => setDialogVisibility(false),
  };

  useEffect(() => {
    resetTimeSlotRequestDto();
    resetTimeSlotResponseDto();
    resetSelectedTimeSlotResponseDto();
    handleChangeDate(new Date());
  }, []);

  const handleChangeDate = (value: Date) => {
    const dateCleaned = new Date(
      Date.UTC(value.getFullYear(), value.getMonth(), value.getDate(), 0, 0, 0)
    );

    // Reset selected slot when date changes
    resetTimeSlotResponseDto();
    resetSelectedTimeSlotResponseDto();

    apiService
      .timeslots("TrainGroupDates/TimeSlots", {
        userId: TokenService.getUserId() ?? "",
        selectedDate: dateCleaned.toISOString(),
      })
      .then((response) => {
        if (response) {
          setTimeSlotResponseDto(
            response.filter((x) => !x.isUnavailableTrainGroup)
          );
        }
      });

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

    trainGroupDateParticipantUpdateDto.clientTimezoneOffsetMinutes =
      new Date().getTimezoneOffset();
    trainGroupDateParticipantUpdateDto.trainGroupId =
      selectedTimeSlot.trainGroupId;

    if (
      trainGroupDateParticipantUpdateDto.trainGroupParticipantDtos.length === 0
    ) {
      showWarn(t("No dates selected!"));
    }

    setLoading(true);
    return await apiService
      .updateParticipants(trainGroupDateParticipantUpdateDto)
      .then((response) => {
        if (response) {
          dialogControl.hideDialog();
          handleChangeDate(new Date(timeSlotRequestDto.selectedDate)); // Refresh time slots
          resetSelectedTimeSlotResponseDto();
          resetTimeSlotRequestDto();
          resetTimeSlotResponseDto();
          setLoading(false);
        }
      })
      .then(() => {
        // resetSelectedTimeSlotResponseDto();
        // resetTimeSlotRequestDto();
        // resetTimeSlotResponseDto();
        // setLoading(false);
      });
  };

  return (
    <>
      {/*                  */}
      {/*     Calendar     */}
      {/*                  */}
      <div className="grid w-full">
        <div className="col-12 lg:col-6 xl:col-6">
          <Card
            title={t("Booking Calendar")}
            subTitle={t("Select a date to view available time slots")}
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
