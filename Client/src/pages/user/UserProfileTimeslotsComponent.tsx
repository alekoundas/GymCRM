import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import { useEffect, useRef, useState } from "react";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import listPlugin from "@fullcalendar/list";
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
import { useParams } from "react-router-dom";
import {
  CHANGE_WINDOW_HOURS,
  durationMinutes,
  fromUtcDay,
  hoursUntil,
  sessionStart,
  startOfDay,
} from "../train-group-booking/BookingDates";

// What a date on the calendar is. Past and future are decided here, against the
// member's own today - the server only says booked, skipped or attended.
type EntryStatus = "ATTENDED" | "MISSED" | "UPCOMING" | "SKIPPED";

const entryStatus = (entry: TimeSlotRecurrenceDateDto): EntryStatus => {
  if (entry.isAttendance) return "ATTENDED";
  if (entry.trainGroupParticipantUnavailableDateId) return "SKIPPED";
  return fromUtcDay(entry.date) < startOfDay(new Date()) ? "MISSED" : "UPCOMING";
};

const statusSeverity = (
  status: EntryStatus
): "success" | "danger" | "info" | "secondary" =>
  status === "ATTENDED"
    ? "success"
    : status === "MISSED"
      ? "danger"
      : status === "UPCOMING"
        ? "info"
        : "secondary";

// Every entry is one session on one real date, so it carries its own id.
const entryId = (entry: TimeSlotRecurrenceDateDto): string =>
  entry.isAttendance
    ? `att-${entry.attendanceId}`
    : `p-${entry.trainGroupParticipantId}-${entry.date}`;

export default function UserProfileTimeslotsComponent() {
  const { t } = useTranslator();
  const apiService = useApiService();
  const params = useParams();
  const isAdminPage = location.pathname.includes("/administrator");

  const calendarRef = useRef<FullCalendar>(null);

  const [events, setEvents] = useState<any[]>([]); // Data
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);
  const [selectedTrainGroup, setSelectedTrainGroup] =
    useState<TimeSlotResponseDto>(new TimeSlotResponseDto());
  const [selectedEntry, setSelectedEntry] = useState<TimeSlotRecurrenceDateDto>(
    new TimeSlotRecurrenceDateDto()
  );

  const [loading, setLoading] = useState(false);
  const [isTimeSlotDialogVisible, setTimeSlotDialogVisible] = useState(false);
  const [isStopDialogVisible, setStopDialogVisible] = useState(false);
  const [isSkipDialogVisible, setSkipDialogVisible] = useState(false);
  const [isRejoinDialogVisible, setRejoinDialogVisible] = useState(false);

  const timeSlotDialogControl: DialogControl = {
    showDialog: () => setTimeSlotDialogVisible(true),
    hideDialog: () => setTimeSlotDialogVisible(false),
  };
  const stopDialogControl: DialogControl = {
    showDialog: () => setStopDialogVisible(true),
    hideDialog: () => setStopDialogVisible(false),
  };
  const skipDialogControl: DialogControl = {
    showDialog: () => setSkipDialogVisible(true),
    hideDialog: () => setSkipDialogVisible(false),
  };
  const rejoinDialogControl: DialogControl = {
    showDialog: () => setRejoinDialogVisible(true),
    hideDialog: () => setRejoinDialogVisible(false),
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

    const response = await apiService.timeslots("Users/TimeSlots", timeSlotDto);

    if (response) {
      // Every entry is a real date now - from booking to leaving, attended, missed,
      // skipped or still to come - so each is placed on its own day.
      const mappedEvents = response.flatMap((slot) =>
        slot.recurrenceDates.map((x) => {
          const day = fromUtcDay(x.date);
          const start = sessionStart(day, slot.startOn);
          const end = new Date(
            start.getTime() + durationMinutes(slot.duration) * 60 * 1000
          );
          const status = entryStatus(x);

          return {
            id: entryId(x),
            title: slot.title,
            start: start,
            end: end,
            extendedProps: { status: status },
          };
        })
      );

      setEvents(mappedEvents);
      setTimeSlots(response);
    }

    setLoading(false);
  };

  const refetch = async () => {
    const currentStart = calendarRef.current?.getApi().view.currentStart;
    if (currentStart) await fetchTimeSlots(currentStart);
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

  const onTimeSlotClick = (arg: EventContentArg) => {
    for (const slot of timeSlots) {
      const entry = slot.recurrenceDates.find((x) => entryId(x) === arg.event.id);
      if (entry) {
        // The slot itself, not another lookup by id: a deleted group has no id left to
        // look up, and the attendance carries everything the dialog needs anyway.
        setSelectedTrainGroup(slot);
        setSelectedEntry(entry);
        timeSlotDialogControl.showDialog();
        return;
      }
    }
  };

  const offset = () => new Date().getTimezoneOffset();

  // Stop a recurring booking from this date on, or cancel a one-off. Neither deletes
  // anything: the dates before stay on the calendar.
  const onStop = async () => {
    const id = selectedEntry.trainGroupParticipantId;
    if (!id) return;

    const dto = {
      fromDate: selectedEntry.date,
      clientTimezoneOffsetMinutes: offset(),
      isAdminPage: isAdminPage,
    };
    const response = selectedEntry.isOneOff
      ? await apiService.cancelBooking(id, dto)
      : await apiService.endBooking(id, dto);

    if (response) {
      stopDialogControl.hideDialog();
      timeSlotDialogControl.hideDialog();
      await refetch();
    }
  };

  const onSkip = async () => {
    const response = await apiService.create("TrainGroupParticipantUnavailableDates", {
      id: 0,
      trainGroupParticipantId: selectedEntry.trainGroupParticipantId ?? 0,
      unavailableDate: selectedEntry.date,
      isAdminPage: isAdminPage,
      clientTimezoneOffsetMinutes: offset(),
    } as TrainGroupParticipantUnavailableDateDto);

    if (response) {
      skipDialogControl.hideDialog();
      timeSlotDialogControl.hideDialog();
      await refetch();
    }
  };

  const onRejoin = async () => {
    const id = selectedEntry.trainGroupParticipantUnavailableDateId;
    if (!id) return;

    const response = await apiService.delete("TrainGroupParticipantUnavailableDates", id);
    if (response) {
      rejoinDialogControl.hideDialog();
      timeSlotDialogControl.hideDialog();
      await refetch();
    }
  };

  // "18:30" out of a stored wall clock that arrives stamped as utc.
  const formatClock = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return (
      date.getUTCHours().toString().padStart(2, "0") +
      ":" +
      date.getUTCMinutes().toString().padStart(2, "0")
    );
  };

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

  const status = entryStatus(selectedEntry);
  const statusLabel =
    status === "ATTENDED"
      ? t("Attended")
      : status === "MISSED"
        ? t("Missed")
        : status === "UPCOMING"
          ? t("Upcoming")
          : t("Skipped");
  const statusIcon =
    status === "ATTENDED"
      ? "pi pi-check"
      : status === "MISSED"
        ? "pi pi-times"
        : status === "UPCOMING"
          ? "pi pi-clock"
          : "pi pi-minus-circle";

  // Only a session still to come can change, and a member not inside 12 hours of it.
  // On the admin pages staff can, and the server checks they really are staff.
  const selectedDay = selectedEntry.date ? fromUtcDay(selectedEntry.date) : new Date();
  const hoursToGo = selectedTrainGroup.startOn
    ? hoursUntil(selectedDay, selectedTrainGroup.startOn)
    : 0;
  const isStillToCome = !selectedEntry.isAttendance && hoursToGo > 0;
  const isInsideWindow = hoursToGo > 0 && hoursToGo < CHANGE_WINDOW_HOURS;
  const canChange = isStillToCome && (isAdminPage || !isInsideWindow);

  return (
    <>
      {/* Seven columns need room to stay readable, so the calendar keeps a width of
          its own and the tab it sits in scrolls sideways when the screen is narrower
          than that - rather than the calendar spilling out over the card. */}
      <div
        className="p-0 md:p-4"
        style={{ minWidth: "700px" }}
      >
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
          eventContent={(arg) => (
            // Green attended, red missed, blue still to come, grey skipped.
            <Button
              severity={statusSeverity(arg.event.extendedProps.status as EntryStatus)}
              className="flex w-full h-full justify-content-center align-items-center"
              onClick={() => onTimeSlotClick(arg)}
            >
              <p>{arg.timeText}</p>
            </Button>
          )}
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
      </div>

      {/*                                      */}
      {/*           View TrainGroup            */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isTimeSlotDialogVisible}
        control={timeSlotDialogControl}
        // The group names the dialog, so it belongs in the title bar next to the close
        // button rather than repeated as the first line of the body.
        header={selectedTrainGroup?.title}
        // Nothing in the footer: closing is what the x in the corner is for.
        footer={<></>}
      >
        <div>
          {selectedTrainGroup?.startOn && (
            <>
              {/* The session at a glance: how it stands across the top, the few facts
                  underneath, and anything the trainer wrote last. */}
              <div className="flex flex-column gap-4">
                <div className="flex flex-wrap gap-2">
                  <Tag
                    severity={statusSeverity(status)}
                    icon={statusIcon}
                    value={statusLabel}
                  />

                  {/* How it was booked matters for a booking, not for the record of
                      a session that took place. */}
                  {!selectedEntry.isAttendance && (
                    <Tag
                      severity="info"
                      icon={selectedEntry.isOneOff ? "pi pi-calendar" : "pi pi-replay"}
                      value={selectedEntry.isOneOff ? t("One-off") : t("Recurring")}
                    />
                  )}
                </div>

                <div className="grid">
                  <div className="col-6 sm:col-3">
                    <div className="text-color-secondary text-sm mb-1">
                      {t("Start On")}
                    </div>
                    <div className="text-lg font-medium">
                      {formatClock(selectedTrainGroup.startOn)}
                    </div>
                  </div>

                  <div className="col-6 sm:col-3">
                    <div className="text-color-secondary text-sm mb-1">
                      {t("Duration")}
                    </div>
                    <div className="text-lg font-medium">
                      {formatClock(selectedTrainGroup.duration)}
                    </div>
                  </div>

                  <div className="col-12 sm:col-6">
                    <div className="text-color-secondary text-sm mb-1">
                      {t("Trainer")}
                    </div>
                    {/* The account may be gone; the attendance still carries the name. */}
                    {selectedTrainGroup.trainer?.firstName ? (
                      chipTemplate(selectedTrainGroup.trainer)
                    ) : (
                      <div className="text-lg font-medium">
                        {selectedTrainGroup.trainerFullName}
                      </div>
                    )}
                  </div>
                </div>

                {selectedTrainGroup.description && (
                  <div className="surface-100 border-round p-3">
                    <div className="text-color-secondary text-sm mb-1">
                      {t("Description")}
                    </div>
                    <p className="m-0">{selectedTrainGroup.description}</p>
                  </div>
                )}
              </div>

              {/* A cut-off only means something for a session still to come. */}
              {isStillToCome && isInsideWindow && !isAdminPage && (
                <div className="flex justify-content-center pt-5">
                  <p className="text-xl text-primary m-0 pt-4">
                    {t("Already 12h away! You cant opt out.")}
                  </p>
                </div>
              )}

              {/* A session that has been and gone is a record, not a booking - there
                  is nothing here to change. */}
              {isStillToCome && (
                <div className="flex flex-wrap justify-content-end gap-2 pt-4 mt-4 border-top-1 surface-border">
                  {status === "SKIPPED" ? (
                    <Button
                      label={t("Rejoin this date")}
                      severity="info"
                      onClick={rejoinDialogControl.showDialog}
                    />
                  ) : selectedEntry.isOneOff ? (
                    <Button
                      label={t("Cancel session")}
                      severity="danger"
                      disabled={!canChange}
                      onClick={stopDialogControl.showDialog}
                    />
                  ) : (
                    <>
                      <Button
                        label={t("Skip this date")}
                        severity="info"
                        disabled={!canChange}
                        onClick={skipDialogControl.showDialog}
                      />
                      <Button
                        label={t("Stop from this date")}
                        severity="danger"
                        disabled={!canChange}
                        onClick={stopDialogControl.showDialog}
                      />
                    </>
                  )}
                </div>
              )}
            </>
          )}
        </div>
      </GenericDialogComponent>

      {/*                                                */}
      {/*         Stop or cancel the booking             */}
      {/*                                                */}
      <GenericDialogComponent
        visible={isStopDialogVisible}
        control={stopDialogControl}
        onSave={onStop}
        formMode={FormMode.ADD}
        header={`${t("Are you sure")}?`}
        saveLabel={t("Yes")}
      >
        <div className="flex justify-content-center">
          <p>
            {selectedEntry.isOneOff
              ? t("This action will cancel your booking for this date.")
              : t(
                  "Your booking stops from this date on. The sessions before it stay on your calendar."
                )}
          </p>
        </div>
      </GenericDialogComponent>

      {/*                                               */}
      {/*               Skip this date                  */}
      {/*                                               */}
      <GenericDialogComponent
        visible={isSkipDialogVisible}
        control={skipDialogControl}
        onSave={onSkip}
        formMode={FormMode.ADD}
        header={`${t("Are you sure")}?`}
        saveLabel={t("Yes")}
      >
        <div className="flex justify-content-center">
          <p>{t("This action will cancel your booking ONLY for this date.")} </p>
        </div>
      </GenericDialogComponent>

      {/*                                               */}
      {/*              Rejoin this date                 */}
      {/*                                               */}
      <GenericDialogComponent
        visible={isRejoinDialogVisible}
        control={rejoinDialogControl}
        onSave={onRejoin}
        formMode={FormMode.ADD}
        header={`${t("Are you sure")}?`}
        saveLabel={t("Yes")}
      >
        <div className="flex justify-content-center">
          <p>
            {t("You will join this date, only if there are any spots available.")}
          </p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
