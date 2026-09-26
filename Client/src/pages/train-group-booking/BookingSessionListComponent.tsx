import { Avatar } from "primereact/avatar";
import { Button } from "primereact/button";
import { ProgressBar } from "primereact/progressbar";
import { Tag } from "primereact/tag";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import { useTranslator } from "../../services/TranslatorService";
import {
  durationMinutes,
  endClock,
  formatClock,
  formatLongDay,
  sessionStart,
} from "./BookingDates";
import { isLocked } from "./BookingChangeFormComponent";

interface IField {
  day: Date;
  slots: TimeSlotResponseDto[];
  isActingForOther: boolean;
  onBook: (slot: TimeSlotResponseDto) => void;
  onChange: (slot: TimeSlotResponseDto) => void;
  onRejoin: (slot: TimeSlotResponseDto) => void;
}

// Whether the group repeats at all - a fixed-day event can only be booked once.
export const hasRecurringOption = (slot: TimeSlotResponseDto): boolean =>
  slot.recurrenceDates.some(
    (x) =>
      x.recurrenceDayOfWeek !== undefined || x.recurrenceDayOfMonth !== undefined
  );

export default function BookingSessionListComponent({
  day,
  slots,
  isActingForOther,
  onBook,
  onChange,
  onRejoin,
}: IField) {
  const { t } = useTranslator();

  // A session the gym called off for the day is not bookable and not shown.
  const sessions = slots
    .filter((x) => !x.isUnavailableTrainGroup)
    .sort((a, b) => formatClock(a.startOn).localeCompare(formatClock(b.startOn)));

  const trainerAvatar = (slot: TimeSlotResponseDto) => {
    const name = slot.trainerFullName ?? "";
    const initials = name
      .split(" ")
      .map((x) => x.charAt(0))
      .join("")
      .slice(0, 2)
      .toUpperCase();

    return (
      <Avatar
        image={
          slot.trainer?.profileImage
            ? "data:image/png;base64," + slot.trainer.profileImage
            : undefined
        }
        label={slot.trainer?.profileImage ? undefined : initials}
        shape="circle"
        style={{ width: "1.5rem", height: "1.5rem", fontSize: "0.65rem" }}
      />
    );
  };

  const renderStatus = (slot: TimeSlotResponseDto, isFull: boolean) => {
    if (slot.skippedUnavailableDateId)
      return (
        <Tag
          severity="secondary"
          icon="pi pi-minus-circle"
          value={t("Skipped")}
        />
      );

    if (slot.bookedParticipantId)
      return (
        <Tag
          severity="info"
          icon={slot.isBookedRecurring ? "pi pi-replay" : "pi pi-calendar"}
          value={slot.isBookedRecurring ? t("Recurring") : t("One-off")}
        />
      );

    if (isFull)
      return (
        <Tag
          severity="danger"
          value={t("Full")}
        />
      );

    if (!hasRecurringOption(slot))
      return (
        <Tag
          severity="secondary"
          value={t("One-off event")}
        />
      );

    return null;
  };

  const renderAction = (slot: TimeSlotResponseDto, isFull: boolean) => {
    const start = sessionStart(day, slot.startOn);
    const end = new Date(start.getTime() + durationMinutes(slot.duration) * 60000);
    const now = new Date();

    if (start <= now)
      return (
        <Button
          className="w-full"
          size="small"
          outlined
          disabled
          label={end > now ? t("In progress") : t("Finished")}
        />
      );

    if (slot.skippedUnavailableDateId)
      return (
        <Button
          className="w-full"
          size="small"
          outlined
          label={t("Rejoin")}
          disabled={isFull}
          onClick={() => onRejoin(slot)}
        />
      );

    // Skip this one date of a recurring booking, or cancel a one-off. Inside 12
    // hours a member can do neither.
    if (slot.bookedParticipantId)
      return (
        <Button
          className="w-full"
          size="small"
          outlined
          severity={slot.isBookedRecurring ? undefined : "danger"}
          label={slot.isBookedRecurring ? t("Skip") : t("Cancel")}
          disabled={isLocked(day, slot.startOn, isActingForOther)}
          onClick={() => onChange(slot)}
        />
      );

    // Full for the day, but a recurring booking can still go ahead around it.
    if (isFull && !hasRecurringOption(slot))
      return (
        <Button
          className="w-full"
          size="small"
          disabled
          label={t("Full")}
        />
      );

    return (
      <Button
        className="w-full"
        size="small"
        label={t("Book")}
        onClick={() => onBook(slot)}
      />
    );
  };

  return (
    <div className="flex flex-column">
      <div className="flex flex-wrap align-items-baseline justify-content-between gap-2 mt-4 mb-2">
        <h3 className="m-0 text-lg">{formatLongDay(day)}</h3>
        <span className="text-sm text-color-secondary">
          {sessions.length} {sessions.length === 1 ? t("session") : t("sessions")}
        </span>
      </div>

      {sessions.length === 0 ? (
        <div className="border-1 border-dashed surface-border border-round p-5 text-center text-color-secondary">
          {t("No time slots available for this date.")}
        </div>
      ) : (
        <div className="border-1 surface-border border-round">
          {sessions.map((slot, index) => {
            const max = slot.maxParticipants ?? 0;
            const left = Math.max(0, slot.spotsLeft);
            const isFull = left <= 0;
            const taken = max > 0 ? Math.round(((max - left) / max) * 100) : 0;

            return (
              <div
                key={slot.trainGroupId}
                className={`flex flex-wrap align-items-center gap-3 p-3 ${
                  index > 0 ? "border-top-1 surface-border" : ""
                }`}
              >
                <div style={{ width: "3.5rem" }}>
                  <div className="text-xl font-bold">{formatClock(slot.startOn)}</div>
                  <div className="text-sm text-color-secondary">
                    {durationMinutes(slot.duration)}'
                  </div>
                </div>

                <div
                  className="flex-1"
                  style={{ minWidth: "12rem" }}
                >
                  <div className="flex flex-wrap align-items-center gap-2 font-semibold">
                    <span>{slot.title}</span>
                    {renderStatus(slot, isFull)}
                  </div>
                  <div className="flex align-items-center gap-2 mt-1 text-sm text-color-secondary">
                    {trainerAvatar(slot)}
                    <span>{slot.trainerFullName}</span>
                    <span>·</span>
                    <span>
                      {formatClock(slot.startOn)}–{endClock(slot.startOn, slot.duration)}
                    </span>
                  </div>
                </div>

                <div style={{ width: "9rem" }}>
                  <ProgressBar
                    value={taken}
                    showValue={false}
                    style={{ height: "6px" }}
                    color={
                      isFull
                        ? "var(--red-500)"
                        : left <= 2
                          ? "var(--orange-500)"
                          : undefined
                    }
                  />
                  <div className="text-xs text-color-secondary mt-1">
                    {isFull
                      ? `${t("Full")} · ${max}/${max}`
                      : `${left}/${max} ${t("spots left")}`}
                  </div>
                </div>

                <div style={{ width: "7.5rem" }}>{renderAction(slot, isFull)}</div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
