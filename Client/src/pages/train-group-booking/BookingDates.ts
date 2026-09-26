import { LocalStorageService } from "../../services/LocalStorageService";

// Days on the booking page are local midnights. The server takes and returns a day as
// midnight UTC, so the two conversions below are the only place that crosses over -
// read with the UTC getters, a day stays the same day wherever the member is.

export const startOfDay = (value: Date): Date =>
  new Date(value.getFullYear(), value.getMonth(), value.getDate());

export const addDays = (value: Date, days: number): Date =>
  new Date(value.getFullYear(), value.getMonth(), value.getDate() + days);

export const isSameDay = (a: Date, b: Date): boolean =>
  a.getFullYear() === b.getFullYear() &&
  a.getMonth() === b.getMonth() &&
  a.getDate() === b.getDate();

export const toUtcDay = (value: Date): string =>
  new Date(
    Date.UTC(value.getFullYear(), value.getMonth(), value.getDate())
  ).toISOString();

export const fromUtcDay = (value: string): Date => {
  const date = new Date(value);
  return new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
};

// A stored wall clock arrives stamped as UTC and is shown as the clock it is.
export const formatClock = (value: string | undefined): string => {
  if (!value) return "";
  const date = new Date(value);
  return (
    date.getUTCHours().toString().padStart(2, "0") +
    ":" +
    date.getUTCMinutes().toString().padStart(2, "0")
  );
};

export const durationMinutes = (duration: string | undefined): number => {
  if (!duration) return 0;
  const date = new Date(duration);
  return date.getUTCHours() * 60 + date.getUTCMinutes();
};

export const endClock = (startOn: string, duration: string): string => {
  const start = new Date(startOn);
  const total =
    start.getUTCHours() * 60 + start.getUTCMinutes() + durationMinutes(duration);
  return (
    (Math.floor(total / 60) % 24).toString().padStart(2, "0") +
    ":" +
    (total % 60).toString().padStart(2, "0")
  );
};

// When the session on this day starts, on the member's own clock.
export const sessionStart = (day: Date, startOn: string): Date => {
  const time = new Date(startOn);
  return new Date(
    day.getFullYear(),
    day.getMonth(),
    day.getDate(),
    time.getUTCHours(),
    time.getUTCMinutes()
  );
};

export const hoursUntil = (day: Date, startOn: string): number =>
  (sessionStart(day, startOn).getTime() - Date.now()) / 36e5;

// Members cannot change a session inside this window; staff can.
export const CHANGE_WINDOW_HOURS = 12;

const language = (): string => LocalStorageService.getLanguage() ?? "el";

export const formatDay = (value: Date): string =>
  value.toLocaleDateString(language(), {
    weekday: "short",
    day: "numeric",
    month: "short",
  });

export const formatLongDay = (value: Date): string =>
  value.toLocaleDateString(language(), {
    weekday: "long",
    day: "numeric",
    month: "long",
  });

export const formatDayWithYear = (value: Date): string =>
  value.toLocaleDateString(language(), {
    day: "numeric",
    month: "short",
    year: "numeric",
  });

export const formatMonth = (value: Date): string =>
  value.toLocaleDateString(language(), { month: "long", year: "numeric" });

// 2 Jan 2000 was a Sunday, so day 0 lands on Sunday like DayOfWeek does.
export const weekdayName = (dayOfWeek: number): string =>
  new Date(2000, 0, 2 + dayOfWeek).toLocaleDateString(language(), {
    weekday: "long",
  });

export const weekdayShort = (value: Date): string =>
  value.toLocaleDateString(language(), { weekday: "short" });

export const formatDuration = (hours: number): string => {
  const minutes = Math.max(0, Math.round(hours * 60));
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m}'`;
  return m === 0 ? `${h}h` : `${h}h ${m}'`;
};

// The dates a recurring day of a group falls on, from a day onwards.
export const occurrences = (
  from: Date,
  count: number,
  dayOfWeek?: number,
  dayOfMonth?: number,
  until?: Date
): Date[] => {
  const result: Date[] = [];
  let day = startOfDay(from);

  for (let i = 0; i < 400 && result.length < count; i++, day = addDays(day, 1)) {
    if (until && day >= until) break;
    if (dayOfWeek !== undefined && dayOfWeek !== null && day.getDay() === dayOfWeek)
      result.push(day);
    else if (dayOfMonth !== undefined && dayOfMonth !== null && day.getDate() === dayOfMonth)
      result.push(day);
  }

  return result;
};
