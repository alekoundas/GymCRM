import { Button } from "primereact/button";
import { useTranslator } from "../../services/TranslatorService";
import {
  addDays,
  formatMonth,
  isSameDay,
  startOfDay,
  toUtcDay,
  weekdayShort,
} from "./BookingDates";

interface IField {
  weekStart: Date;
  selectedDay: Date;
  // How many of the member's sessions fall on each day, keyed by toUtcDay.
  bookedDays: Record<string, number>;
  onWeekChange: (weekStart: Date) => void;
  onSelect: (day: Date) => void;
}

// Seven days from the one on the left. The week never starts before today - there is
// nothing to book in the past.
export default function BookingWeekStripComponent({
  weekStart,
  selectedDay,
  bookedDays,
  onWeekChange,
  onSelect,
}: IField) {
  const { t } = useTranslator();
  const today = startOfDay(new Date());
  const days = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));
  const lastDay = days[6];

  const monthLabel =
    weekStart.getMonth() === lastDay.getMonth()
      ? formatMonth(weekStart)
      : `${formatMonth(weekStart)} – ${formatMonth(lastDay)}`;

  const previousWeek = () => {
    const previous = addDays(weekStart, -7);
    onWeekChange(previous < today ? today : previous);
  };

  return (
    <div className="flex flex-column gap-2">
      <div className="flex align-items-center justify-content-between gap-2">
        <Button
          icon="pi pi-chevron-left"
          rounded
          text
          aria-label={t("Previous week")}
          disabled={weekStart <= today}
          onClick={previousWeek}
        />
        <span className="font-semibold text-center">{monthLabel}</span>
        <Button
          icon="pi pi-chevron-right"
          rounded
          text
          aria-label={t("Next week")}
          onClick={() => onWeekChange(addDays(weekStart, 7))}
        />
      </div>

      <div className="flex gap-1 md:gap-2">
        {days.map((day) => {
          const isSelected = isSameDay(day, selectedDay);
          const count = Math.min(bookedDays[toUtcDay(day)] ?? 0, 3);

          return (
            <Button
              key={day.toISOString()}
              outlined={!isSelected}
              className="flex-1 flex flex-column align-items-center gap-1 px-0 py-2"
              style={{ minWidth: 0 }}
              onClick={() => onSelect(day)}
            >
              <span className="text-xs uppercase white-space-nowrap">
                {isSameDay(day, today) ? t("Today") : weekdayShort(day)}
              </span>
              <span className="text-lg font-bold">{day.getDate()}</span>
              {/* A dot per session the member already has that day. */}
              <span
                className="flex gap-1"
                style={{ height: "6px" }}
              >
                {Array.from({ length: count }, (_, i) => (
                  <span
                    key={i}
                    className="border-circle"
                    style={{
                      width: "6px",
                      height: "6px",
                      background: "currentColor",
                    }}
                  />
                ))}
              </span>
            </Button>
          );
        })}
      </div>
    </div>
  );
}
