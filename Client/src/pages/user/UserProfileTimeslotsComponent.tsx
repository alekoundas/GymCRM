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
import { useTranslator } from "../../services/TranslatorService";
import { LocalStorageService } from "../../services/LocalStorageService";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";
import { useDateService } from "../../services/DateService";
import { DayOfWeekEnum } from "../../enum/DayOfWeekEnum";
import { useParams, useSearchParams } from "react-router-dom";

export default function UserProfileTimeslotsComponent() {
  const { t } = useTranslator();
  const {
    getDayOfWeekFromDate,
    getNextDayOfWeekDateUTC,
    getNextDayOfMonthDateUTC,
    getUTCTime,
  } = useDateService();
  const apiService = useApiService();
  const params = useParams();

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
    const id = params["id"];
    if (id !== undefined) {
      timeSlotDto.userId = id;
    }
    
    const response = await apiService.timeslots("Users/TimeSlots", timeSlotDto); // Replace with your API endpoint

    // Generate 7 days starting from sunday of current week
    const calendarDates = Array.from({ length: 7 }, (_, i) => {
      const date = new Date(currentDate);
      date.setDate(currentDate.getDate() + i);
      return date;
    });

    if (response) {
      const mappedEvents = response
        // .filter((x) => !x.isUnavailableTrainGroup)
        .flatMap((slot) =>
          slot.recurrenceDates
            .filter((x) => !x.isUnavailableTrainGroup)
            .map((x) => {
              let startDate: Date | undefined;

              // Fixed Date
              if (x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY)
                startDate = new Date(x.date);

              // Date Of Month
              if (
                x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
              ) {
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
                const startTime = getUTCTime(slot.startOn);
                startDate = new Date(
                  startDate.getFullYear(),
                  startDate.getMonth(),
                  startDate.getDate(),
                  startTime.getHours(),
                  startTime.getMinutes(),
                  0
                );

                // Calculate endDate
                const durationTime = getUTCTime(slot.duration);
                const durationMinutes =
                  durationTime.getHours() * 60 + durationTime.getMinutes();
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
      apiService
        .delete(
          "TrainGroupParticipants",
          selectedTimeSlotRecurrenceDate.trainGroupParticipantId
        )
        .then(async () => {
          optOutTimeSlotDialogControl.hideDialog();
          timeSlotDialogControl.hideDialog();
          const calendarApi = calendarRef.current?.getApi();
          const currentStart = calendarApi?.view.currentStart;
          if (currentStart) {
            await fetchTimeSlots(currentStart);
          }
        });
  };

  const onOptOutDate = async () => {
    apiService
      .create("TrainGroupParticipantUnavailableDates", {
        trainGroupParticipantId:
          selectedTimeSlotRecurrenceDate.trainGroupParticipantId,
        unavailableDate: selectedDate.toISOString(),
      } as TrainGroupParticipantUnavailableDateDto)
      .then(async () => {
        optOutDateTimeSlotDialogControl.hideDialog();
        timeSlotDialogControl.hideDialog();
        const calendarApi = calendarRef.current?.getApi();
        const currentStart = calendarApi?.view.currentStart;
        if (currentStart) {
          await fetchTimeSlots(currentStart);
        }
      });
  };

  const onOptInDate = async () => {
    if (selectedTimeSlotRecurrenceDate.trainGroupParticipantUnavailableDateId)
      apiService
        .delete(
          "TrainGroupParticipantUnavailableDates",
          selectedTimeSlotRecurrenceDate.trainGroupParticipantUnavailableDateId
        )
        .then(async () => {
          optInDateTimeSlotDialogControl.hideDialog();
          timeSlotDialogControl.hideDialog();
          const calendarApi = calendarRef.current?.getApi();
          const currentStart = calendarApi?.view.currentStart;
          if (currentStart) {
            await fetchTimeSlots(currentStart);
          }
        });
  };

  // Custom chip template for selected users
  const chipTemplate = (user: UserDto | undefined) => {
    if (user) {
      const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(
        0
      )}`.toUpperCase();
      const imageSrc = "data:image/png;base64," + user.profileImage;
      return (
        <div className="flex m-0 p-0 pl-3 align-items-center ">
          <Avatar
            image={user.profileImage ? imageSrc : ""}
            label={user.profileImage ? undefined : initials}
            shape="circle"
            size="normal"
            className=" mr-2 "
          />
          {" " +
            user.firstName[0].toUpperCase() +
            user.firstName.slice(1, user.firstName.length) +
            " " +
            user.lastName[0].toUpperCase() +
            user.lastName.slice(1, user.lastName.length)}
        </div>
      );
    }
  };

  const isDateTwelveHoursFromNow = (
    apiDate: string,
    apiStartOnDate: string,
    type: TrainGroupDateTypeEnum | undefined
  ): boolean => {
    let startOnDate = new Date(apiDate);
    if (type === TrainGroupDateTypeEnum.DAY_OF_WEEK) {
      const dayOfWeek: DayOfWeekEnum = getDayOfWeekFromDate(
        new Date(apiDate)
      )?.toUpperCase() as DayOfWeekEnum;

      startOnDate = getNextDayOfWeekDateUTC(dayOfWeek, new Date());
    }
    if (type === TrainGroupDateTypeEnum.DAY_OF_MONTH) {
      startOnDate = getNextDayOfMonthDateUTC(
        new Date(apiDate).getUTCDate(),
        new Date()
      );
    }

    // Parse the time string as local (remove 'Z' if present to treat as local ISO)
    const timeStr = apiStartOnDate;
    const localTimeStr = timeStr.endsWith("Z") ? timeStr.slice(0, -1) : timeStr;
    const timeDate = new Date(localTimeStr);

    // Use local setHours and getHours to overlay local time
    startOnDate.setHours(
      timeDate.getHours(),
      timeDate.getMinutes(),
      timeDate.getSeconds(),
      timeDate.getMilliseconds()
    );

    const isTwelveHoursFromNow =
      startOnDate > new Date() &&
      startOnDate <= new Date(new Date().getTime() + 12 * 60 * 60 * 1000);

    return isTwelveHoursFromNow;
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
            const utcDate = new Date(
              Date.UTC(x.date.year, x.date.month, x.date.day, 0, 0, 0, 0)
            );
            const day = utcDate.getUTCDate();
            const month = utcDate.getUTCMonth() + 1;
            const weekday = utcDate.toLocaleDateString(
              LocalStorageService.getLanguage() ?? "el",
              {
                weekday: "short",
                timeZone: "UTC",
              }
            );
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
                    <strong> {t("Start On")}:</strong>{" "}
                    {new Date(selectedTrainGroup.startOn)
                      .getUTCHours()
                      .toString()
                      .padStart(2, "0") +
                      ":" +
                      new Date(selectedTrainGroup.startOn)
                        .getUTCMinutes()
                        .toString()
                        .padStart(2, "0")}
                  </p>
                  <p>
                    <strong>{t("Duration")}:</strong>{" "}
                    {new Date(selectedTrainGroup.duration)
                      .getUTCHours()
                      .toString()
                      .padStart(2, "0") +
                      ":" +
                      new Date(selectedTrainGroup.duration)
                        .getUTCMinutes()
                        .toString()
                        .padStart(2, "0")}
                  </p>
                  <p className="mb-0">
                    <strong>{t("Group Name")}:</strong>{" "}
                    {selectedTrainGroup.title || "N/A"}
                  </p>
                  <div className="flex p-0 m-0">
                    <p>
                      <strong>{t("Trainer")}:</strong>
                    </p>
                    {chipTemplate(selectedTrainGroup.trainer)}
                  </div>

                  <p className="flex align-items-center mt-0">
                    <strong>{t("Book Type")}:</strong>
                    {selectedTimeSlotRecurrenceDate.isOneOff ? (
                      <Tag>{t("One-off")}</Tag>
                    ) : (
                      <Tag>{t("Recurring")}</Tag>
                    )}
                  </p>

                  <p className="flex align-items-center ">
                    <strong>{t("Joined")}:</strong>{" "}
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
                      t("No description available.")}
                  </p>
                </div>

                {isDateTwelveHoursFromNow(
                  selectedTimeSlotRecurrenceDate.date,
                  selectedTrainGroup.startOn,
                  selectedTimeSlotRecurrenceDate.trainGroupDateType
                ) && (
                  <div className="flex justify-content-center pt-5">
                    <p className="text-xl text-primary m-0 pt-4">
                      {t("Already 12h away! You cant opt out.")}
                    </p>
                  </div>
                )}

                {/* Opt Out */}
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
                        label={t("Opt out")}
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
                            ) ||
                          isDateTwelveHoursFromNow(
                            selectedTimeSlotRecurrenceDate.date,
                            selectedTrainGroup.startOn,
                            selectedTimeSlotRecurrenceDate.trainGroupDateType
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
                          label={t("Opt out for this date")}
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
                              ) ||
                            isDateTwelveHoursFromNow(
                              selectedTimeSlotRecurrenceDate.date,
                              selectedTrainGroup.startOn,
                              selectedTimeSlotRecurrenceDate.trainGroupDateType
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
                        label={t("Opt out")}
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
                            ) ||
                          isDateTwelveHoursFromNow(
                            selectedTimeSlotRecurrenceDate.date,
                            selectedTrainGroup.startOn,
                            selectedTimeSlotRecurrenceDate.trainGroupDateType
                          )
                        }
                      ></Button>
                      <Button
                        label={t("Opt in again for this date")}
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

                  {timeSlots.some((x) =>
                    x.recurrenceDates.some(
                      (y) =>
                        y.trainGroupDateId ===
                          selectedTimeSlotRecurrenceDate.trainGroupDateId &&
                        !y.isUserJoined &&
                        !y.trainGroupParticipantId &&
                        !y.trainGroupParticipantUnavailableDateId
                    )
                  ) && (
                    <div className="flex justify-content-between pt-5">
                      <div></div>
                      <div>
                        <p className="text-xl text-primary m-0 pt-4">
                          {t(
                            "You havent joined this date. Please join via apointment tab."
                          )}
                        </p>
                      </div>
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
          <p>{t("This action will cancel your booking for this date.")}</p>
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
          <p>
            {t("This action will cancel your booking ONLY for this date.")}{" "}
          </p>
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
          <p>
            {t(
              "You will join this date, only if there are any spots available."
            )}
          </p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
