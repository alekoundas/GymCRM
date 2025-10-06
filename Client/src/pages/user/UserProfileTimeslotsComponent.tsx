import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import { useEffect, useRef, useState } from "react";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import listPlugin from "@fullcalendar/list";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { EventContentArg } from "@fullcalendar/core/index.js";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { FormMode } from "../../enum/FormMode";
import { Button } from "primereact/button";
import { Tag } from "primereact/tag";
import { TimeSlotRecurrenceDateDto } from "../../model/TimeSlotRecurrenceDateDto";
import { TrainGroupParticipantUnavailableDateDto } from "../../model/entities/train-group-participant-unavailable-date/TrainGroupParticipantUnavailableDateDto";
import ThemeService from "../../services/ThemeService";
import { useApiService } from "../../services/ApiService";

export default function UserProfileTimeslotsComponent() {
                    const apiService = useApiService();
  // const { userDto, updateUserDto } = useUserStore();
  const calendarRef = useRef<FullCalendar>(null);
  const [events, setEvents] = useState<any[]>([]); // Data
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);
  const [selectedDate, setSelectedDate] = useState<Date>(new Date());
  const [selectedTrainGroup, setSelectedTrainGroup] =
    useState<TimeSlotResponseDto>(new TimeSlotResponseDto());
  const [selectedTimeSlotRecurrenceDate, setSelectedTimeSlotRecurrenceDate] =
    useState<TimeSlotRecurrenceDateDto>(new TimeSlotRecurrenceDateDto());

  const [loading, setLoading] = useState(false);
  const [isTimeSlotDialogVisible, setTimeSlotDialogVisible] = useState(false); // Dialog visibility
  const [isOptOutTimeSlotDialogVisible, setOptOutTimeSlotDialogVisible] =
    useState(false); // Dialog visibility
  const [
    isOptOutDateTimeSlotDialogVisible,
    setOptOutDateTimeSlotDialogVisible,
  ] = useState(false); // Dialog visibility
  const [isOptInDateTimeSlotDialogVisible, setOptInDateTimeSlotDialogVisible] =
    useState(false); // Dialog visibility

  const timeSlotDialogControl: DialogControl = {
    showDialog: () => setTimeSlotDialogVisible(true),
    hideDialog: () => setTimeSlotDialogVisible(false),
  };
  const optOutTimeSlotDialogControl: DialogControl = {
    showDialog: () => setOptOutTimeSlotDialogVisible(true),
    hideDialog: () => setOptOutTimeSlotDialogVisible(false),
  };
  const optOutDateTimeSlotDialogControl: DialogControl = {
    showDialog: () => setOptOutDateTimeSlotDialogVisible(true),
    hideDialog: () => setOptOutDateTimeSlotDialogVisible(false),
  };
  const optInDateTimeSlotDialogControl: DialogControl = {
    showDialog: () => setOptInDateTimeSlotDialogVisible(true),
    hideDialog: () => setOptInDateTimeSlotDialogVisible(false),
  };

  const fetchTimeSlots = async (currentDate: Date) => {
    const timeSlotDto = new TimeSlotRequestDto();

    const dateCleaned = new Date(
      Date.UTC(
        currentDate.getFullYear(),
        currentDate.getMonth(),
        currentDate.getDate(),
        0,
        0,
        0,
        0
      )
    );

    timeSlotDto.selectedDate = dateCleaned.toISOString();
    const response = await apiService.timeslots("Users/TimeSlots", timeSlotDto); // Replace with your API endpoint

    // Generate 7 days starting from sunday of current week
    const calendarDates = Array.from({ length: 7 }, (_, i) => {
      const date = new Date(currentDate);
      date.setDate(currentDate.getDate() + i);
      return date;
    });

    if (response) {
      const mappedEvents = response.flatMap((slot) =>
        slot.recurrenceDates.map((x) => {
          let startDate: Date | undefined;

          // Fixed Date
          if (x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY)
            startDate = new Date(x.date);

          // Date Of Month
          if (x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH) {
            const dateOfWeek = calendarDates.find(
              (y) => y.getDate() === new Date(x.date).getDate()
            );
            if (dateOfWeek) {
              startDate = dateOfWeek;
            }
          }

          // Date Of Week
          if (x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK) {
            const dateOfWeek = calendarDates.find(
              (y) => y.getDay() === new Date(x.date).getDay()
            );
            if (dateOfWeek) {
              startDate = dateOfWeek;
            }
          }

          // One off participant
          if (!x.trainGroupDateId) {
            startDate = new Date(x.date);
          }

          if (startDate) {
            startDate = new Date(
              startDate!.getFullYear(),
              startDate!.getMonth(),
              startDate!.getDate(),
              new Date(slot.startOn).getHours(),
              new Date(slot.startOn).getMinutes(),
              0
            );

            // Calculate endDate
            const durationMinutes =
              new Date(slot.duration).getHours() * 60 +
              new Date(slot.duration).getMinutes();
            const endDate = new Date(
              startDate.getTime() + durationMinutes * 60 * 1000
            );

            return {
              id: x.trainGroupDateId,
              title: slot.title,
              start: startDate,

              end: endDate,
              backgroundColor: x.isUserJoined ? "#007ad9" : "#ced4da", // Joined: blue, not: gray
              isUserJoined: x.isUserJoined,
              extendedProps: { isUserJoined: x.isUserJoined },
            };
          }
        })
      );

      const mappedEventsCleaned = mappedEvents.filter((x) => x !== undefined);
      setEvents(mappedEventsCleaned);
      setTimeSlots(response);
    }

    setLoading(false);
  };

  useEffect(() => {
    // Initial call on mount to set starting theme
    handleThemeChange();

    // Listen to custom event (dispatched from switcher)
    document.addEventListener("primeThemeChange", handleThemeChange);
    return () =>
      document.removeEventListener("primeThemeChange", handleThemeChange);
  }, []);

  const handleThemeChange = () => {
    const palette = ThemeService.getCurrentThemeColors();
    const calendarApi = calendarRef.current?.getApi();

    // DOM manipulation for header styling
    if (calendarApi && palette.primaryColor) {
      // Day headers (e.g., "Fri 1/1") - .fc-col-header-cell
      const dayHeaders = document.querySelectorAll(
        ".fc-col-header-cell"
      ) as NodeListOf<HTMLElement>;
      dayHeaders.forEach((header) => {
        header.style.backgroundColor = palette.surfaceCard;
        header.style.color = palette.textColor;
      });
      // Day headers (e.g., "Fri 1/1") - .fc-col-header-cell
      const dayHeader = document.querySelectorAll(
        ".fc-timegrid-axis"
      ) as NodeListOf<HTMLElement>;
      dayHeader.forEach((header) => {
        header.style.backgroundColor = palette.surfaceCard;
        header.style.color = palette.textColor;
      });
    }
  };

  const onTimeSlotClick = async (arg: EventContentArg) => {
    const timeSlot: TimeSlotResponseDto | undefined = timeSlots.find((x) =>
      x.recurrenceDates.some((y) => y.trainGroupDateId === +arg.event.id)
    );

    if (arg.event.start) {
      const dateCleaned = new Date(
        Date.UTC(
          arg.event.start.getFullYear(),
          arg.event.start.getMonth(),
          arg.event.start.getDate(),
          0,
          0,
          0,
          0
        )
      );
      setSelectedDate(dateCleaned);

      if (timeSlot) {
        const trainGroup = timeSlots.find((x) => x.id === timeSlot.id);

        if (trainGroup) {
          const timeslotDate = timeSlot.recurrenceDates.find(
            (x) => x.trainGroupDateId === +arg.event.id
          );

          if (timeslotDate) {
            setSelectedTimeSlotRecurrenceDate(timeslotDate);
            setSelectedTrainGroup(trainGroup);
            timeSlotDialogControl.showDialog();
          }
        }
      }
    }
  };

  const onOptOut = async () => {
    if (selectedTimeSlotRecurrenceDate.trainGroupParticipantId)
      apiService.delete(
        "TrainGroupParticipants",
        selectedTimeSlotRecurrenceDate.trainGroupParticipantId
      );

    optOutTimeSlotDialogControl.hideDialog();
    timeSlotDialogControl.hideDialog();
    const calendarApi = calendarRef.current?.getApi();
    const currentStart = calendarApi?.view.currentStart;
    if (currentStart) {
      await fetchTimeSlots(currentStart);
    }
  };

  const onOptOutDate = async () => {
    apiService.create("TrainGroupParticipantUnavailableDates", {
      trainGroupParticipantId:
        selectedTimeSlotRecurrenceDate.trainGroupParticipantId,
      unavailableDate: selectedDate,
    } as TrainGroupParticipantUnavailableDateDto);

    optOutDateTimeSlotDialogControl.hideDialog();
    timeSlotDialogControl.hideDialog();
    const calendarApi = calendarRef.current?.getApi();
    const currentStart = calendarApi?.view.currentStart;
    if (currentStart) {
      await fetchTimeSlots(currentStart);
    }
  };

  const onOptInDate = async () => {
    if (selectedTimeSlotRecurrenceDate.trainGroupParticipantUnavailableDateId)
      apiService.delete(
        "TrainGroupParticipantUnavailableDates",
        selectedTimeSlotRecurrenceDate.trainGroupParticipantUnavailableDateId
      );

    optInDateTimeSlotDialogControl.hideDialog();
    timeSlotDialogControl.hideDialog();
    const calendarApi = calendarRef.current?.getApi();
    const currentStart = calendarApi?.view.currentStart;
    if (currentStart) {
      await fetchTimeSlots(currentStart);
    }
  };

  return (
    <>
      <div className="p-4">
        <FullCalendar
          ref={calendarRef}
          events={events} // Data
          plugins={[dayGridPlugin, timeGridPlugin, listPlugin]}
          initialView="timeGridWeek"
          initialDate={new Date()}
          allDaySlot={false} // Hide the All Day row
          dayHeaderFormat={(x) => {
            const day = x.date.day;
            const month = x.date.month + 1;
            const weekday = new Date(
              x.date.year,
              month,
              day
            ).toLocaleDateString("en-US", { weekday: "short" });
            return `${weekday} ${day}/${month}`;
          }}
          slotLabelFormat={{
            hour: "2-digit",
            minute: "2-digit",
            hour12: false,
            omitZeroMinute: false,
          }} // 24-hour format for time axis: "14:00"
          dayMaxEvents={false}
          headerToolbar={{
            left: "prev,next today",
            center: "title",
            right: "timeGridWeek,timeGridDay",
          }}
          eventContent={async (arg) => {
            const isUserJoined = timeSlots.some((x) =>
              x.recurrenceDates.some(
                (y) => y.trainGroupDateId === +arg.event.id && y.isUserJoined
              )
            );

            if (isUserJoined)
              return (
                <Button
                  className="flex w-full h-full justify-content-center align-items-center"
                  onClick={async () => await onTimeSlotClick(arg)}
                >
                  {/* <b>{arg.event.title}</b> */}
                  <p>{arg.timeText}</p>
                </Button>
              );
            else
              return (
                <Button
                  severity="secondary"
                  className="flex w-full h-full justify-content-center align-items-center"
                  onClick={() => onTimeSlotClick(arg)}
                >
                  {/* <b>{arg.event.title}</b> */}
                  <p>{arg.timeText}</p>
                </Button>
              );
          }}
          height="auto"
          editable={false} // Allow drag-and-drop
          themeSystem="standard" // Enables CSS vars theming (default, but explicit)
          datesSet={(x) => {
            setLoading(true);
            fetchTimeSlots(x.start);
            setLoading(false);

            // Apply theme after dates are rendered (includes headers, day cells, time grid)
            const timer = setTimeout(handleThemeChange, 0);
            return () => clearTimeout(timer);
          }} // Callback after dates render (modern equivalent for post-render DOM manipulation)
        />

        {/*                                      */}
        {/*           View TrainGroup            */}
        {/*                                      */}

        <GenericDialogComponent
          formMode={FormMode.VIEW}
          visible={isTimeSlotDialogVisible}
          control={timeSlotDialogControl}
        >
          <div>
            {selectedTrainGroup?.startOn && (
              <>
                <div>
                  <p>
                    <strong>Time:</strong>{" "}
                    {new Date(selectedTrainGroup.startOn).toLocaleTimeString()}
                  </p>
                  <p>
                    <strong>Group Name:</strong>{" "}
                    {selectedTrainGroup.title || "N/A"}
                  </p>
                  <p>
                    <strong>Trainer:</strong>{" "}
                    {selectedTrainGroup.trainerId || "N/A"}
                  </p>

                  <p className="flex align-items-center ">
                    <strong>Book Type:</strong>
                    {selectedTimeSlotRecurrenceDate.isOneOff ? (
                      <Tag>One-off</Tag>
                    ) : (
                      <Tag>Recurring</Tag>
                    )}
                  </p>

                  <p className="flex align-items-center ">
                    <strong>Joined:</strong>{" "}
                    {timeSlots.some((x) =>
                      x.recurrenceDates.some(
                        (y) =>
                          y.trainGroupDateId ===
                            selectedTimeSlotRecurrenceDate.trainGroupDateId &&
                          y.isUserJoined
                      )
                    ) ? (
                      <Tag severity={"success"}>Yes</Tag>
                    ) : (
                      <Tag severity={"secondary"}>No</Tag>
                    )}
                  </p>
                  <p>
                    <strong>Description:</strong>{" "}
                    {selectedTrainGroup.description ||
                      "No description available."}
                  </p>
                </div>

                <div>
                  {timeSlots.some((x) =>
                    x.recurrenceDates.some(
                      (y) =>
                        y.trainGroupDateId ===
                          selectedTimeSlotRecurrenceDate.trainGroupDateId &&
                        y.isUserJoined
                    )
                  ) && (
                    <div className="flex justify-content-between pt-5">
                      <div></div>
                      <Button
                        label="Opt out"
                        severity="danger"
                        onClick={optOutTimeSlotDialogControl.showDialog}
                        disabled={
                          new Date(
                            Date.UTC(
                              selectedDate.getFullYear(),
                              selectedDate.getMonth(),
                              selectedDate.getDate(),
                              0,
                              0,
                              0,
                              0
                            )
                          ) <
                          new Date(
                            Date.UTC(
                              new Date().getFullYear(),
                              new Date().getMonth(),
                              new Date().getDate(),
                              0,
                              0,
                              0,
                              0
                            )
                          )
                        }
                      ></Button>
                      {timeSlots.some((x) =>
                        x.recurrenceDates.some(
                          (y) =>
                            y.trainGroupDateId ===
                              selectedTimeSlotRecurrenceDate.trainGroupDateId &&
                            !y.isOneOff
                        )
                      ) && (
                        <Button
                          label="Opt out for this date"
                          severity="info"
                          onClick={optOutDateTimeSlotDialogControl.showDialog}
                          disabled={
                            new Date(
                              Date.UTC(
                                selectedDate.getFullYear(),
                                selectedDate.getMonth(),
                                selectedDate.getDate(),
                                0,
                                0,
                                0,
                                0
                              )
                            ) <
                            new Date(
                              Date.UTC(
                                new Date().getFullYear(),
                                new Date().getMonth(),
                                new Date().getDate(),
                                0,
                                0,
                                0,
                                0
                              )
                            )
                          }
                        ></Button>
                      )}
                      <div></div>
                    </div>
                  )}

                  {timeSlots.some((x) =>
                    x.recurrenceDates.some(
                      (y) =>
                        y.trainGroupDateId ===
                          selectedTimeSlotRecurrenceDate.trainGroupDateId &&
                        !y.isUserJoined &&
                        y.trainGroupParticipantUnavailableDateId
                    )
                  ) && (
                    <div className="flex justify-content-between pt-5">
                      <div></div>
                      <Button
                        label="Opt out"
                        severity="danger"
                        onClick={optOutTimeSlotDialogControl.showDialog}
                        disabled={
                          new Date(
                            Date.UTC(
                              selectedDate.getFullYear(),
                              selectedDate.getMonth(),
                              selectedDate.getDate(),
                              0,
                              0,
                              0,
                              0
                            )
                          ) <
                          new Date(
                            Date.UTC(
                              new Date().getFullYear(),
                              new Date().getMonth(),
                              new Date().getDate(),
                              0,
                              0,
                              0,
                              0
                            )
                          )
                        }
                      ></Button>
                      <Button
                        label="Opt in again for this date"
                        severity="info"
                        onClick={optInDateTimeSlotDialogControl.showDialog}
                        disabled={
                          new Date(
                            Date.UTC(
                              selectedDate.getFullYear(),
                              selectedDate.getMonth(),
                              selectedDate.getDate(),
                              0,
                              0,
                              0,
                              0
                            )
                          ) <
                          new Date(
                            Date.UTC(
                              new Date().getFullYear(),
                              new Date().getMonth(),
                              new Date().getDate(),
                              0,
                              0,
                              0,
                              0
                            )
                          )
                        }
                      ></Button>
                      <div></div>
                    </div>
                  )}
                </div>
              </>
            )}
          </div>
        </GenericDialogComponent>
      </div>

      {/*                                                */}
      {/*         Delete Train Group Participant         */}
      {/*                                                */}
      <GenericDialogComponent
        visible={isOptOutTimeSlotDialogVisible}
        control={optOutTimeSlotDialogControl}
        onSave={onOptOut}
        formMode={FormMode.ADD}
        header="Are you sure?"
      >
        <div className="flex justify-content-center">
          <p>This action will cancel your booking for this date.</p>
        </div>
      </GenericDialogComponent>

      {/*                                               */}
      {/*          Opt Out Train Group Participant      */}
      {/*                                               */}
      <GenericDialogComponent
        visible={isOptOutDateTimeSlotDialogVisible}
        control={optOutDateTimeSlotDialogControl}
        onSave={onOptOutDate}
        formMode={FormMode.ADD}
        header="Are you sure?"
      >
        <div className="flex justify-content-center">
          <p>This action will cancel your booking ONLY for this date. </p>
        </div>
      </GenericDialogComponent>

      {/*                                               */}
      {/*           Opt In Train Group Participant      */}
      {/*                                               */}
      <GenericDialogComponent
        visible={isOptInDateTimeSlotDialogVisible}
        control={optInDateTimeSlotDialogControl}
        onSave={onOptInDate}
        formMode={FormMode.ADD}
        header="Are you sure?"
      >
        <div className="flex justify-content-center">
          <p>You will join this date, only if there are any spots available.</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
