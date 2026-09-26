import { Dropdown } from "primereact/dropdown";
import { Message } from "primereact/message";
import { TrainGroupParticipantBookingDto } from "../../model/entities/train-group-participant/TrainGroupParticipantBookingDto";
import { useTranslator } from "../../services/TranslatorService";
import {
  CHANGE_WINDOW_HOURS,
  addDays,
  formatClock,
  formatDay,
  fromUtcDay,
  hoursUntil,
  occurrences,
  sessionStart,
  startOfDay,
  toUtcDay,
} from "./BookingDates";

export type ChangeMode = "SKIP" | "STOP";

interface IField {
  booking: TrainGroupParticipantBookingDto;
  mode: ChangeMode;
  // Skip: the date picked to skip. Stop: the first date the member gives up,
  // worked out by the page - there is nothing to pick.
  date: string | undefined; // toUtcDay
  onDateChange: (date: string | undefined) => void;
  isActingForOther: boolean;
}

// The upcoming dates of a recurring booking that are still to happen.
export const upcomingDates = (
  booking: TrainGroupParticipantBookingDto,
  mode: ChangeMode
): Date[] => {
  const today = startOfDay(new Date());
  const start = booking.startOnDate ? fromUtcDay(booking.startOnDate) : today;
  const until = booking.endOnDate ? fromUtcDay(booking.endOnDate) : undefined;
  const skipped = booking.upcomingSkips.map((x) => toUtcDay(fromUtcDay(x.date)));

  return occurrences(
    start > today ? start : today,
    14,
    booking.recurrenceDayOfWeek,
    booking.recurrenceDayOfMonth,
    until
  )
    .filter((x) => sessionStart(x, booking.startOn) > new Date())
    .filter((x) => mode === "STOP" || !skipped.includes(toUtcDay(x)))
    .slice(0, 10);
};

export const isLocked = (
  day: Date,
  startOn: string,
  isActingForOther: boolean
): boolean => !isActingForOther && hoursUntil(day, startOn) < CHANGE_WINDOW_HOURS;

// Skip is one date, picked from the booking's upcoming ones. Stop is permanent and
// has nothing to pick: it ends the booking from the first session that can still be
// given up, and the member can book the group again whenever they like.
export default function BookingChangeFormComponent({
  booking,
  mode,
  date,
  onDateChange,
  isActingForOther,
}: IField) {
  const { t } = useTranslator();

  const dates = upcomingDates(booking, mode);
  const picked = date ? fromUtcDay(date) : undefined;
  const hasLockedDate = dates.some((x) => isLocked(x, booking.startOn, isActingForOther));

  // The last date before the given one that the booking still covers.
  const lastSessionBefore = (day: Date): Date | undefined => {
    const start = booking.startOnDate ? fromUtcDay(booking.startOnDate) : day;
    const skipped = booking.upcomingSkips.map((x) => toUtcDay(fromUtcDay(x.date)));

    for (let previous = addDays(day, -1); previous >= start; previous = addDays(previous, -1)) {
      const isDay =
        booking.recurrenceDayOfWeek !== undefined
          ? previous.getDay() === booking.recurrenceDayOfWeek
          : previous.getDate() === booking.recurrenceDayOfMonth;
      if (isDay && !skipped.includes(toUtcDay(previous))) return previous;
    }

    return undefined;
  };

  const windowMessage = isActingForOther ? (
    <Message
      severity="info"
      className="w-full justify-content-start"
      text={`${t("Staff can change sessions inside the 12-hour window")}.`}
    />
  ) : (
    hasLockedDate && (
      <Message
        severity="info"
        className="w-full justify-content-start"
        text={`${t("Sessions starting within 12 hours cannot be changed")}.`}
      />
    )
  );

  if (mode === "STOP") {
    const last = picked ? lastSessionBefore(picked) : undefined;

    return (
      <div className="flex flex-column gap-4">
        <div className="surface-ground border-1 surface-border border-round p-3 flex flex-column gap-2">
          <span>
            {last
              ? `${t("Last session")}: ${formatDay(last)}, ${formatClock(booking.startOn)}.`
              : `${t("No session of this booking will have taken place")}.`}
          </span>
          <span className="text-color-secondary">
            {t("You can book it again any time from the Book tab")}.
          </span>
        </div>

        {windowMessage}
      </div>
    );
  }

  return (
    <div className="flex flex-column gap-4">
      <div className="flex flex-column gap-2">
        <label
          htmlFor="change-date"
          className="font-semibold"
        >
          {t("Date to skip")}
        </label>
        <Dropdown
          inputId="change-date"
          value={date}
          options={dates.map((x) => {
            const locked = isLocked(x, booking.startOn, isActingForOther);
            return {
              label:
                `${formatDay(x)}, ${formatClock(booking.startOn)}` +
                (locked ? ` · ${t("under 12 hours")}` : ""),
              value: toUtcDay(x),
              disabled: locked,
            };
          })}
          optionDisabled="disabled"
          onChange={(e) => onDateChange(e.value)}
          placeholder={t("Select a date")}
          emptyMessage={t("No upcoming dates")}
          className="w-full"
        />
      </div>

      {picked && (
        <div className="surface-ground border-1 surface-border border-round p-3">
          {`${t("You will miss only")} ${formatDay(picked)}. ${t(
            "The booking carries on after it"
          )}.`}
        </div>
      )}

      {windowMessage}
    </div>
  );
}
