import { Checkbox } from "primereact/checkbox";
import { Message } from "primereact/message";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import { TimeSlotRecurrenceDateDto } from "../../model/TimeSlotRecurrenceDateDto";
import { useTranslator } from "../../services/TranslatorService";
import {
  CHANGE_WINDOW_HOURS,
  endClock,
  formatClock,
  formatDay,
  formatDuration,
  hoursUntil,
  occurrences,
  weekdayName,
} from "./BookingDates";

export interface BookSelection {
  isOneOff: boolean;
  recurringTrainGroupDateIds: number[];
}

interface IField {
  slot: TimeSlotResponseDto;
  day: Date;
  selection: BookSelection;
  onSelectionChange: (selection: BookSelection) => void;
  // Staff booking for somebody else: no 12-hour warning, and the balance matters.
  isActingForOther: boolean;
  balance: number | undefined;
}

const isRecurring = (x: TimeSlotRecurrenceDateDto): boolean =>
  x.recurrenceDayOfWeek !== undefined || x.recurrenceDayOfMonth !== undefined;

// Monday first, days of the month after the weekdays.
const sortKey = (x: TimeSlotRecurrenceDateDto): number =>
  x.recurrenceDayOfWeek !== undefined
    ? (x.recurrenceDayOfWeek + 6) % 7
    : 7 + (x.recurrenceDayOfMonth ?? 0);

// "How often": just the picked session, and/or every one of the group's days from the
// picked day on. Any mix can be ticked; each becomes its own booking.
export default function BookingBookFormComponent({
  slot,
  day,
  selection,
  onSelectionChange,
  isActingForOther,
  balance,
}: IField) {
  const { t } = useTranslator();

  const isFull = slot.spotsLeft <= 0;
  const recurringOptions = slot.recurrenceDates
    .filter(isRecurring)
    .sort((a, b) => sortKey(a) - sortKey(b));

  // Ticking the picked day's own weekday covers the single session already.
  const coversPickedDay = selection.recurringTrainGroupDateIds.includes(
    slot.trainGroupDateId
  );

  const toggleOneOff = () =>
    onSelectionChange({ ...selection, isOneOff: !selection.isOneOff });

  const toggleRecurring = (trainGroupDateId: number) => {
    const isTicked = selection.recurringTrainGroupDateIds.includes(trainGroupDateId);
    const recurringTrainGroupDateIds = isTicked
      ? selection.recurringTrainGroupDateIds.filter((x) => x !== trainGroupDateId)
      : [...selection.recurringTrainGroupDateIds, trainGroupDateId];

    onSelectionChange({
      recurringTrainGroupDateIds,
      isOneOff:
        !isTicked && trainGroupDateId === slot.trainGroupDateId
          ? false
          : selection.isOneOff,
    });
  };

  const recurringLabel = (x: TimeSlotRecurrenceDateDto): string =>
    x.recurrenceDayOfWeek !== undefined
      ? `${t("Every")} ${weekdayName(x.recurrenceDayOfWeek)}`
      : `${t("Every month on day")} ${x.recurrenceDayOfMonth}`;

  const option = (
    id: string,
    checked: boolean,
    disabled: boolean,
    title: string,
    subtitle: string,
    onToggle: () => void
  ) => (
    <div
      key={id}
      className={`flex align-items-start gap-3 p-3 border-2 border-round ${
        checked ? "border-primary" : "surface-border"
      } ${disabled ? "opacity-50" : "cursor-pointer"}`}
      onClick={() => !disabled && onToggle()}
    >
      {/* The whole card toggles, so the box's own click must not bubble up and
          toggle it a second time. */}
      <span onClick={(e) => e.stopPropagation()}>
        <Checkbox
          inputId={id}
          checked={checked}
          disabled={disabled}
          onChange={() => !disabled && onToggle()}
        />
      </span>
      <label
        htmlFor={id}
        className={`flex flex-column gap-1 ${disabled ? "" : "cursor-pointer"}`}
        onClick={(e) => e.stopPropagation()}
      >
        <span className="font-semibold">{title}</span>
        <span className="text-sm text-color-secondary">{subtitle}</span>
      </label>
    </div>
  );

  const hours = hoursUntil(day, slot.startOn);
  const isInsideWindow = hours > 0 && hours < CHANGE_WINDOW_HOURS;

  return (
    <div className="flex flex-column gap-4">
      <div className="flex flex-wrap gap-3 text-color-secondary">
        <span className="flex align-items-center gap-2">
          <i className="pi pi-calendar" />
          {formatDay(day)}
        </span>
        <span className="flex align-items-center gap-2">
          <i className="pi pi-clock" />
          {formatClock(slot.startOn)}–{endClock(slot.startOn, slot.duration)}
        </span>
        <span className="flex align-items-center gap-2">
          <i className="pi pi-user" />
          {slot.trainerFullName}
        </span>
      </div>

      <div className="flex flex-column gap-2">
        <span className="font-semibold">{t("How often")}</span>

        {option(
          "book-one-off",
          selection.isOneOff,
          isFull || coversPickedDay,
          t("Just this session"),
          isFull
            ? t("Full on this date")
            : coversPickedDay
              ? t("Included in the recurring booking")
              : formatDay(day),
          toggleOneOff
        )}

        {recurringOptions.map((x) => {
          const first = occurrences(
            day,
            1,
            x.recurrenceDayOfWeek,
            x.recurrenceDayOfMonth
          )[0];

          return option(
            `book-recurring-${x.trainGroupDateId}`,
            selection.recurringTrainGroupDateIds.includes(x.trainGroupDateId),
            x.isUserJoined,
            recurringLabel(x),
            x.isUserJoined
              ? t("Already booked")
              : `${t("From")} ${first ? formatDay(first) : ""}, ${t("until you stop it")}`,
            () => toggleRecurring(x.trainGroupDateId)
          );
        })}
      </div>

      {selection.isOneOff && isInsideWindow && !isActingForOther && (
        <Message
          severity="info"
          className="w-full justify-content-start"
          text={`${t("Starts in")} ${formatDuration(hours)}. ${t(
            "Once booked, you cannot cancel it yourself"
          )}.`}
        />
      )}

      {selection.recurringTrainGroupDateIds.length > 0 && (
        <Message
          severity="info"
          className="w-full justify-content-start"
          text={`${t("Dates that are already full are skipped and listed after booking")}.`}
        />
      )}

      {isActingForOther && balance !== undefined && balance <= 0 && (
        <Message
          severity="warn"
          className="w-full justify-content-start"
          text={`${t("No lessons left")}. ${t(
            "The booking still goes through, and the next attendance takes the balance below zero"
          )}.`}
        />
      )}
    </div>
  );
}
