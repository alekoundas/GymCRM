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

export default function TrainGroupsBookingCalendarPage() {
  //  const { t } = useTranslation();
  const [isDialogVisible, setDialogVisibility] = useState(false);
  const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<TimeSlotResponseDto | null>(
    null
  );
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
    setSelectedDate(dateCleaned);
    setSelectedSlot(null); // Reset selected slot when date changes

    const timeSlotDto = new TimeSlotRequestDto();
    timeSlotDto.selectedDate = dateCleaned;
    ApiService.timeslots("TrainGroupDates/TimeSlots", timeSlotDto)
      .then((response) => {
        if (response) {
          setTimeSlots(response);
        }
      })
      .finally(() => setLoading(false));
  };

  const handleBooking = () => {
    if (selectedDate && selectedSlot) {
      setBookCurrentDate(true);
      setDialogVisibility(true);
    }
  };

  const handleConfirmBooking = () => {
    if (!selectedSlot || !selectedDate) return;

    const userId = TokenService.getUserId();
    const participants: TrainGroupDateParticipantDto[] = [];

    // Add participant for current selected date if checked
    if (bookCurrentDate) {
      participants.push({
        selectedDate: selectedDate.toISOString(),
        trainGroupDateId: selectedSlot.trainGroupDateId,
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
        handleChangeDate(selectedDate); // Refresh time slots
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
              <div className="flex justify-content-center">
                <Button
                  label="Book Now"
                  icon="pi pi-check"
                  className="mt-4 pr-5 pl-5 pt-3 pb-3"
                  onClick={handleBooking}
                  disabled={!(selectedSlot.spotsLeft > 0)}
                />
              </div>
            </Card>
          )}
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
        <>
          {selectedSlot && (
            <div className="p-fluid">
              <h3>{"datatable.select_booking_options"}</h3>
              <div className="field">
                <Checkbox
                  inputId="bookCurrentDate"
                  checked={bookCurrentDate}
                  onChange={(e) => setBookCurrentDate(e.checked ?? false)}
                  disabled={selectedSlot.spotsLeft <= 0}
                />
                <label
                  htmlFor="bookCurrentDate"
                  className="ml-2"
                >
                  {selectedDate?.toLocaleDateString()}
                  {/* {t("datatable.book_current_date", {
                  date: selectedDate?.toLocaleDateString(),
                  time: new Date(selectedSlot.startOn).toLocaleTimeString(),
                })} */}
                </label>
              </div>

              {selectedSlot.recurrenceDates.length > 0 && (
                <>
                  <h4>{"datatable.recurring_dates"}</h4>
                  {selectedSlot.recurrenceDates.filter(
                    (x) =>
                      x.trainGroupDateType ===
                      TrainGroupDateTypeEnum.DAY_OF_WEEK
                  ).length > 0 && (
                    <div className="field">
                      <p>
                        <strong>{"datatable.days_of_week"}:</strong>
                      </p>
                      {selectedSlot.recurrenceDates
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
                              {DateService.getDayOfWeekFromDate(
                                new Date(x.date)
                              )}
                            </label>
                          </div>
                        ))}
                    </div>
                  )}

                  {selectedSlot.recurrenceDates.filter(
                    (x) =>
                      x.trainGroupDateType ===
                      TrainGroupDateTypeEnum.DAY_OF_MONTH
                  ).length > 0 && (
                    <div className="field">
                      <p>
                        <strong>{"datatable.days_of_month"}:</strong>
                      </p>
                      {selectedSlot.recurrenceDates
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
      </GenericDialogComponent>
      {/* <GenericDialogComponent
        header=""
        visible={isDialogVisible}
        control={dialogControl}
      >
        <div className="w-full"></div>
      </GenericDialogComponent> */}
    </>
  );
}
