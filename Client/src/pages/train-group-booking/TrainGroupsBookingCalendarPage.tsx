import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { useRef, useState } from "react";
import ApiService from "../../services/ApiService";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { Tag } from "primereact/tag";
import { DateService } from "../../services/DateService";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { TokenService } from "../../services/TokenService";
import { TrainGroupDateParticipantDto } from "../../model/TrainGroupDateParticipantDto";
import { Checkbox } from "primereact/checkbox";
import { FormMode } from "../../enum/FormMode";
import TrainGroupsBookingCalendarTimeslotComponent from "./TrainGroupsBookingCalendarTimeslotComponent";
import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import TrainGroupsBookingCalendarTimeslotInfoComponent from "./TrainGroupsBookingCalendarTimeslotInfoComponent";
import TrainGroupsBookingCalendarTimeslotBookFormComponent from "./TrainGroupsBookingCalendarTimeslotBookFormComponent";

export default function TrainGroupsBookingCalendarPage() {
  const {
    timeSlotRequestDto,
    selectedTimeSlotResponseDto,
    setTimeSlotRequestDto,
    setTimeSlotResponseDto,
    setSelectedTimeSlotResponseDto,
    resetTimeSlotResponseDto,
  } = useTrainGroupBookingStore();
  //  const { t } = useTranslation();
  const [isDialogVisible, setDialogVisibility] = useState(false);
  // const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  // const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);
  // const [selectedSlot, setSelectedSlot] = useState<TimeSlotResponseDto | null>(
  //   null
  // );
  const [bookCurrentDate, setBookCurrentDate] = useState<boolean>(false);
  const [selectedRecurrenceDates, setSelectedRecurrenceDates] = useState<
    number[]
  >([]);
  const [loading, setLoading] = useState<boolean>(false);

  const dialogControl: DialogControl = {
    showDialog: () => setDialogVisibility(true),
    hideDialog: () => {
      setDialogVisibility(false);
      setBookCurrentDate(false);
      setSelectedRecurrenceDates([]);
    },
  };

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
    // setSelectedDate(dateCleaned);
    setTimeSlotRequestDto({ selectedDate: dateCleaned });
    // setSelectedSlot(null); // Reset selected slot when date changes
    resetTimeSlotResponseDto(); // Reset selected slot when date changes

    const timeSlotDto = new TimeSlotRequestDto();
    timeSlotDto.selectedDate = dateCleaned;
    ApiService.timeslots("TrainGroupDates/TimeSlots", timeSlotDto)
      .then((response) => {
        if (response) {
          setTimeSlotResponseDto(response);
        }
      })
      .finally(() => setLoading(false));
  };

  const handleBooking = () => {
    if (timeSlotRequestDto.selectedDate && selectedTimeSlotResponseDto) {
      setBookCurrentDate(true);
      setDialogVisibility(true);
    }
  };

  const handleConfirmBooking = () => {
    if (!timeSlotRequestDto.selectedDate || !selectedTimeSlotResponseDto)
      return;

    const userId = TokenService.getUserId();
    const participants: TrainGroupDateParticipantDto[] = [];

    // Add participant for current selected date if checked
    if (bookCurrentDate) {
      participants.push({
        selectedDate: timeSlotRequestDto.selectedDate.toISOString(),
        trainGroupDateId: selectedTimeSlotResponseDto.trainGroupDateId,
        userId,
      });
    }

    // Add participants for selected recurring dates
    selectedRecurrenceDates.forEach((trainGroupDateId) => {
      participants.push({
        selectedDate: null,
        trainGroupDateId,
        userId,
      });
    });

    if (participants.length === 0) {
      // toast.current?.show({
      //   severity: "warn",
      //   summary: t("datatable.warning"),
      //   detail: t("datatable.no_dates_selected"),
      // });
      return;
    }

    setLoading(true);
    ApiService.create("TrainGroups/CreateParticipants", participants)
      .then((response) => {
        dialogControl.hideDialog();
        handleChangeDate(timeSlotRequestDto.selectedDate); // Refresh time slots
        // if (response.isSucceed) {
        //   toast.current?.show({
        //     severity: "success",
        //     summary: t("datatable.success"),
        //     detail: t("datatable.booking_confirmed"),
        //   });
        // }
      })
      .catch((error) => {
        // toast.current?.show({
        //   severity: "error",
        //   summary: t("datatable.error"),
        //   detail: t("datatable.booking_failed"),
        // });
      })
      .finally(() => setLoading(false));
  };

  // const onDialogDelete = ()=>{
  //   handleConfirmBooking
  // }

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
              value={timeSlotRequestDto.selectedDate}
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
            onBook={handleBooking}
          />
        </div>
      </div>

      {/*                                      */}
      {/*           Book a timeslot            */}
      {/*                                      */}

      <GenericDialogComponent
        header={"datatable.book_training_session"}
        visible={isDialogVisible}
        control={dialogControl}
        formMode={FormMode.ADD}
        // onDelete={() => handleConfirmBooking()}
        onSave={() => handleConfirmBooking()}
      >
        <TrainGroupsBookingCalendarTimeslotBookFormComponent />
      </GenericDialogComponent>
    </>
  );
}
