import { ReactNode } from "react";
import { Button } from "primereact/button";
import { Tag } from "primereact/tag";
import {
  TrainGroupParticipantBookingDto,
  TrainGroupParticipantSkipDto,
} from "../../model/entities/train-group-participant/TrainGroupParticipantBookingDto";
import { useTranslator } from "../../services/TranslatorService";
import {
  endClock,
  formatClock,
  formatDay,
  formatDayWithYear,
  formatDuration,
  fromUtcDay,
  hoursUntil,
  weekdayName,
} from "./BookingDates";
import { isLocked } from "./BookingChangeFormComponent";

interface IField {
  bookings: TrainGroupParticipantBookingDto[];
  isActingForOther: boolean;
  onSkip: (booking: TrainGroupParticipantBookingDto) => void;
  onStop: (booking: TrainGroupParticipantBookingDto) => void;
  onCancel: (booking: TrainGroupParticipantBookingDto) => void;
  onRejoin: (skip: TrainGroupParticipantSkipDto) => void;
}

// Running, coming up, and ended - one list per kind. Nothing ever leaves the last one:
// it is what the profile calendar draws its history from.
export const isRunning = (x: TrainGroupParticipantBookingDto): boolean =>
  !x.isOneOff && (!x.endOnDate || x.nextSessionDate !== undefined);

export const isUpcoming = (x: TrainGroupParticipantBookingDto): boolean =>
  x.isOneOff && !x.removedOn && x.nextSessionDate !== undefined;

const isHistory = (x: TrainGroupParticipantBookingDto): boolean =>
  x.isOneOff ? !!x.removedOn : !!x.endOnDate && x.nextSessionDate === undefined;

export default function BookingMyBookingsComponent({
  bookings,
  isActingForOther,
  onSkip,
  onStop,
  onCancel,
  onRejoin,
}: IField) {
  const { t } = useTranslator();

  const repeatLabel = (x: TrainGroupParticipantBookingDto): string =>
    x.recurrenceDayOfWeek !== undefined
      ? `${t("Every")} ${weekdayName(x.recurrenceDayOfWeek)}`
      : `${t("Every month on day")} ${x.recurrenceDayOfMonth}`;

  const time = (x: TrainGroupParticipantBookingDto): string =>
    `${formatClock(x.startOn)}–${endClock(x.startOn, x.duration)}`;

  const running = bookings.filter(isRunning);
  const upcoming = bookings
    .filter(isUpcoming)
    .sort((a, b) => (a.date ?? "").localeCompare(b.date ?? ""));
  const history = bookings
    .filter(isHistory)
    .sort((a, b) => (b.removedOn ?? "").localeCompare(a.removedOn ?? ""))
    .slice(0, 10);

  const section = (title: string, empty: string, rows: ReactNode[]) => (
    <div className="flex flex-column gap-2 mb-5">
      <span className="text-sm font-semibold uppercase text-color-secondary">
        {title}
      </span>
      {rows.length === 0 ? (
        <div className="border-1 border-dashed surface-border border-round p-4 text-center text-color-secondary">
          {empty}
        </div>
      ) : (
        <div className="border-1 surface-border border-round">{rows}</div>
      )}
    </div>
  );

  const row = (
    key: number,
    index: number,
    body: ReactNode,
    actions?: ReactNode,
    note?: string
  ) => (
    <div
      key={key}
      className={`flex flex-wrap align-items-center justify-content-between gap-3 p-3 ${
        index > 0 ? "border-top-1 surface-border" : ""
      }`}
    >
      <div
        className="flex-1"
        style={{ minWidth: "14rem" }}
      >
        {body}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
      {note && (
        <div className="w-full text-right text-sm text-color-secondary">{note}</div>
      )}
    </div>
  );

  return (
    <div>
      {section(
        t("Recurring bookings"),
        t("No recurring bookings"),
        running.map((x, index) =>
          row(
            x.id,
            index,
            <>
              <div className="flex flex-wrap align-items-center gap-2 font-semibold">
                <span>{x.title}</span>
                <Tag
                  severity="info"
                  icon="pi pi-replay"
                  value={repeatLabel(x)}
                />
              </div>
              <div className="text-sm text-color-secondary mt-1">
                {time(x)}
                {x.startOnDate &&
                  ` · ${t("Since")} ${formatDayWithYear(fromUtcDay(x.startOnDate))}`}
                {x.lastSessionDate &&
                  ` · ${t("Last session")} ${formatDay(fromUtcDay(x.lastSessionDate))}`}
              </div>
              <div className="text-sm text-color-secondary mt-1">
                {t("Next")}:{" "}
                <span className="font-semibold text-color">
                  {x.nextSessionDate ? formatDay(fromUtcDay(x.nextSessionDate)) : "–"}
                </span>
              </div>
              {x.upcomingSkips.length > 0 && (
                <div className="flex flex-wrap gap-2 mt-2">
                  {x.upcomingSkips.map((skip) => (
                    <span
                      key={skip.id}
                      className="flex align-items-center gap-1 surface-100 border-round px-2 py-1 text-sm"
                    >
                      {t("Skipping")} {formatDay(fromUtcDay(skip.date))}
                      <Button
                        link
                        size="small"
                        className="p-0 ml-1"
                        label={t("Rejoin")}
                        onClick={() => onRejoin(skip)}
                      />
                    </span>
                  ))}
                </div>
              )}
            </>,
            <>
              <Button
                text
                size="small"
                label={t("Skip a date")}
                onClick={() => onSkip(x)}
              />
              <Button
                outlined
                severity="danger"
                size="small"
                label={t("Stop")}
                onClick={() => onStop(x)}
              />
            </>
          )
        )
      )}

      {section(
        t("Single sessions"),
        t("No single sessions coming up"),
        upcoming.map((x, index) => {
          const day = fromUtcDay(x.date!);
          const hours = hoursUntil(day, x.startOn);
          const locked = isLocked(day, x.startOn, isActingForOther);

          return row(
            x.id,
            index,
            <>
              <div className="flex flex-wrap align-items-center gap-2 font-semibold">
                <span>{x.title}</span>
                <Tag
                  severity="info"
                  icon="pi pi-calendar"
                  value={t("One-off")}
                />
              </div>
              <div className="text-sm text-color-secondary mt-1">
                {formatDay(day)} · {time(x)}
                {hours > 0 && hours < 24 && ` · ${t("Starts in")} ${formatDuration(hours)}`}
              </div>
            </>,
            <Button
              outlined
              severity="danger"
              size="small"
              label={t("Cancel")}
              disabled={locked}
              onClick={() => onCancel(x)}
            />,
            locked ? `${t("Under 12 hours to go, so only the gym can remove it")}.` : undefined
          );
        })
      )}

      {section(
        t("Ended and cancelled"),
        t("Nothing here yet"),
        history.map((x, index) =>
          row(
            x.id,
            index,
            <>
              <div className="flex flex-wrap align-items-center gap-2 font-semibold">
                <span>{x.title}</span>
                <Tag
                  severity="secondary"
                  value={x.isOneOff ? t("Cancelled") : t("Ended")}
                />
              </div>
              <div className="text-sm text-color-secondary mt-1">
                {x.isOneOff
                  ? `${x.date ? formatDay(fromUtcDay(x.date)) : ""}, ${formatClock(x.startOn)}`
                  : `${repeatLabel(x)}, ${formatClock(x.startOn)}` +
                    (x.startOnDate
                      ? ` · ${formatDayWithYear(fromUtcDay(x.startOnDate))} – ${
                          x.lastSessionDate
                            ? formatDayWithYear(fromUtcDay(x.lastSessionDate))
                            : "–"
                        }`
                      : "")}
              </div>
              {x.removedOn && (
                <div className="text-sm text-color-secondary mt-1">
                  {x.isOneOff ? t("Cancelled by") : t("Stopped by")}{" "}
                  {x.removedBy_FullName || "–"} ·{" "}
                  {formatDayWithYear(new Date(x.removedOn))}
                </div>
              )}
            </>
          )
        )
      )}

      <p className="m-0 text-sm text-color-secondary">
        {t(
          "Ended and cancelled bookings are kept. The profile calendar still shows every session they covered"
        )}
        .
      </p>
    </div>
  );
}
