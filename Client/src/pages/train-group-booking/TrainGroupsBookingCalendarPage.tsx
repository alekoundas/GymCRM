import { useEffect, useState } from "react";
import { Badge } from "primereact/badge";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { Message } from "primereact/message";
import { TabPanel, TabView } from "primereact/tabview";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { FormMode } from "../../enum/FormMode";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import {
  TrainGroupParticipantBookingDto,
  TrainGroupParticipantSkipDto,
} from "../../model/entities/train-group-participant/TrainGroupParticipantBookingDto";
import { TrainGroupParticipantUnavailableDateDto } from "../../model/entities/train-group-participant-unavailable-date/TrainGroupParticipantUnavailableDateDto";
import { useApiService } from "../../services/ApiService";
import { TokenService } from "../../services/TokenService";
import { useTranslator } from "../../services/TranslatorService";
import SubscriptionBalanceTag from "../subscription/SubscriptionBalanceTag";
import BookingBookFormComponent, { BookSelection } from "./BookingBookFormComponent";
import BookingChangeFormComponent, {
  ChangeMode,
  isLocked,
  upcomingDates,
} from "./BookingChangeFormComponent";
import BookingMyBookingsComponent, {
  isRunning,
  isUpcoming,
} from "./BookingMyBookingsComponent";
import BookingSessionListComponent from "./BookingSessionListComponent";
import BookingWeekStripComponent from "./BookingWeekStripComponent";
import {
  endClock,
  formatClock,
  formatDay,
  formatDuration,
  fromUtcDay,
  hoursUntil,
  startOfDay,
  toUtcDay,
} from "./BookingDates";

// The dialogs below hand their child component DialogChildProps, and unwrap a plain
// div to do it - so every body is a component of its own.
function CancelContent({
  booking,
  day,
  isLocked,
}: {
  booking: TrainGroupParticipantBookingDto;
  day: Date;
  isLocked: boolean;
}) {
  const { t } = useTranslator();

  return (
    <div className="flex flex-column gap-4">
      <div className="flex flex-wrap gap-3 text-color-secondary">
        <span className="flex align-items-center gap-2">
          <i className="pi pi-calendar" />
          {formatDay(day)}
        </span>
        <span className="flex align-items-center gap-2">
          <i className="pi pi-clock" />
          {formatClock(booking.startOn)}–{endClock(booking.startOn, booking.duration)}
        </span>
      </div>

      {isLocked ? (
        <Message
          severity="warn"
          className="w-full justify-content-start"
          text={`${t("Starts in")} ${formatDuration(hoursUntil(day, booking.startOn))}. ${t(
            "Already 12h away! You cant opt out."
          )}`}
        />
      ) : (
        <div className="surface-ground border-1 surface-border border-round p-3">
          {t(
            "The place goes back to the group straight away. The booking stays in the history as cancelled"
          )}
          .
        </div>
      )}
    </div>
  );
}

function FullDatesContent({ dates }: { dates: string[] }) {
  const { t } = useTranslator();

  return (
    <div className="flex flex-column gap-3">
      <ul className="list-none p-0 m-0 flex flex-column gap-2">
        {dates.map((x) => (
          <li
            key={x}
            className="flex align-items-center gap-2"
          >
            <i className="pi pi-calendar-times text-red-500" />
            <span>{formatDay(fromUtcDay(x))}</span>
          </li>
        ))}
      </ul>

      <div className="text-sm text-color-secondary">
        {t("If any of those dates become available, you can join from Profile page.")}
      </div>
      <div className="text-sm text-color-secondary">
        {t("The email invitation wont take any of the unavailable dates into consideration.")}
      </div>
    </div>
  );
}

// Booking and unbooking are separate calls now: Book adds, End and Cancel take away,
// and skip / rejoin keep their own endpoints. Nothing here deletes a booking - an
// ended one stays so the profile calendar can show every session it covered.
export default function TrainGroupsBookingCalendarPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const ownId = TokenService.getUserId() ?? "";
  const isStaff = TokenService.isUserAllowed("TrainGroupParticipants_View");
  const canSeeOthersBalance = TokenService.isUserAllowed("SubscriptionsAdmin_View");

  // Staff can book on a member's behalf; everybody else books for themselves.
  const [userId, setUserId] = useState<string>(ownId);
  const isActingForOther = isStaff && userId !== ownId;

  const today = startOfDay(new Date());
  const [weekStart, setWeekStart] = useState<Date>(today);
  const [selectedDay, setSelectedDay] = useState<Date>(today);
  const [activeIndex, setActiveIndex] = useState(0);

  const [slots, setSlots] = useState<TimeSlotResponseDto[]>([]);
  const [bookedDays, setBookedDays] = useState<Record<string, number>>({});
  const [bookings, setBookings] = useState<TrainGroupParticipantBookingDto[]>([]);
  const [balance, setBalance] = useState<number | undefined>(undefined);

  // Bumped after anything changes, so every list reloads from the server.
  const [refreshKey, setRefreshKey] = useState(0);
  const refresh = () => setRefreshKey((x) => x + 1);

  const [isSaving, setIsSaving] = useState(false);

  const [bookSlot, setBookSlot] = useState<TimeSlotResponseDto | undefined>(undefined);
  const [selection, setSelection] = useState<BookSelection>({
    isOneOff: true,
    recurringTrainGroupDateIds: [],
  });
  const [fullDates, setFullDates] = useState<string[]>([]);

  const [changeBooking, setChangeBooking] = useState<
    TrainGroupParticipantBookingDto | undefined
  >(undefined);
  const [changeMode, setChangeMode] = useState<ChangeMode>("SKIP");
  const [changeDate, setChangeDate] = useState<string | undefined>(undefined);

  const [cancelBooking, setCancelBooking] = useState<
    TrainGroupParticipantBookingDto | undefined
  >(undefined);

  const bookDialogControl: DialogControl = {
    showDialog: () => {},
    hideDialog: () => setBookSlot(undefined),
  };
  const changeDialogControl: DialogControl = {
    showDialog: () => {},
    hideDialog: () => setChangeBooking(undefined),
  };
  const cancelDialogControl: DialogControl = {
    showDialog: () => {},
    hideDialog: () => setCancelBooking(undefined),
  };
  const fullDatesDialogControl: DialogControl = {
    showDialog: () => {},
    hideDialog: () => setFullDates([]),
  };

  // The sessions of the picked day.
  useEffect(() => {
    apiService
      .timeslots("TrainGroupDates/TimeSlots", {
        userId,
        selectedDate: toUtcDay(selectedDay),
      })
      .then((response) => {
        if (response) setSlots(response);
      });
  }, [selectedDay, userId, refreshKey]);

  // The dots on the week strip: whatever the member has on each day of it.
  useEffect(() => {
    apiService
      .timeslots("Users/TimeSlots", { userId, selectedDate: toUtcDay(weekStart) })
      .then((response) => {
        if (!response) return;

        const counts: Record<string, number> = {};
        response.forEach((slot) =>
          slot.recurrenceDates
            .filter((x) => x.isAttendance || x.isUserJoined)
            .forEach((x) => {
              const key = toUtcDay(fromUtcDay(x.date));
              counts[key] = (counts[key] ?? 0) + 1;
            })
        );
        setBookedDays(counts);
      });
  }, [weekStart, userId, refreshKey]);

  useEffect(() => {
    apiService
      .getBookings(isActingForOther ? userId : undefined)
      .then((response) => {
        if (response) setBookings(response);
      });
  }, [userId, refreshKey]);

  useEffect(() => {
    // Another member's balance is for whoever looks after subscriptions.
    if (isActingForOther && !canSeeOthersBalance) {
      setBalance(undefined);
      return;
    }

    apiService
      .getSubscriptionBalance(isActingForOther ? userId : undefined)
      .then((response) => {
        if (response !== null) setBalance(response);
      });
  }, [userId, refreshKey]);

  const onUserChange = (id: string | undefined) => {
    setBookSlot(undefined);
    setChangeBooking(undefined);
    setCancelBooking(undefined);
    setUserId(id || ownId);
  };

  const onWeekChange = (value: Date) => {
    setWeekStart(value);
    setSelectedDay(value);
  };

  const openBook = (slot: TimeSlotResponseDto) => {
    // A full day can still be booked as a recurring one, never as a single session.
    setSelection({ isOneOff: slot.spotsLeft > 0, recurringTrainGroupDateIds: [] });
    setBookSlot(slot);
  };

  const openChange = (
    booking: TrainGroupParticipantBookingDto,
    mode: ChangeMode,
    preferredDate?: string
  ) => {
    const allowed = upcomingDates(booking, mode).filter(
      (x) => !isLocked(x, booking.startOn, isActingForOther)
    );
    const preferred = allowed.find((x) => toUtcDay(x) === preferredDate);
    const date = preferred ?? allowed[0];

    setChangeMode(mode);
    setChangeDate(date ? toUtcDay(date) : undefined);
    setChangeBooking(booking);
  };

  // From a booked session in the day's list: skip that date of a recurring booking,
  // or cancel a one-off. Stopping for good is done from My bookings.
  const onSlotChange = (slot: TimeSlotResponseDto) => {
    const booking = bookings.find((x) => x.id === slot.bookedParticipantId);
    if (!booking) return;

    if (booking.isOneOff) setCancelBooking(booking);
    else openChange(booking, "SKIP", toUtcDay(selectedDay));
  };

  const rejoin = async (unavailableDateId: number | undefined) => {
    if (!unavailableDateId) return;
    await apiService.delete("TrainGroupParticipantUnavailableDates", unavailableDateId);
    refresh();
  };

  const onBook = async (): Promise<void> => {
    if (!bookSlot) return;

    setIsSaving(true);
    const response = await apiService.bookTrainGroup({
      trainGroupId: bookSlot.trainGroupId,
      selectedDate: toUtcDay(selectedDay),
      isOneOff: selection.isOneOff,
      recurringTrainGroupDateIds: selection.recurringTrainGroupDateIds,
      userId: isActingForOther ? userId : undefined,
      clientTimezoneOffsetMinutes: new Date().getTimezoneOffset(),
    });
    setIsSaving(false);

    if (response === null) return;

    setBookSlot(undefined);
    refresh();

    // Saved as skipped dates, so the member can rejoin any that open up.
    if (response.length > 0) setFullDates(response);
  };

  const onChangeSave = async (): Promise<void> => {
    if (!changeBooking || !changeDate) return;

    setIsSaving(true);
    const response =
      changeMode === "SKIP"
        ? await apiService.create<TrainGroupParticipantUnavailableDateDto>(
            "TrainGroupParticipantUnavailableDates",
            {
              id: 0,
              trainGroupParticipantId: changeBooking.id,
              unavailableDate: changeDate,
              isAdminPage: isActingForOther,
              clientTimezoneOffsetMinutes: new Date().getTimezoneOffset(),
            }
          )
        : await apiService.endBooking(changeBooking.id, {
            fromDate: changeDate,
            clientTimezoneOffsetMinutes: new Date().getTimezoneOffset(),
            isAdminPage: isActingForOther,
          });
    setIsSaving(false);

    if (response) {
      setChangeBooking(undefined);
      refresh();
    }
  };

  const onCancelSave = async (): Promise<void> => {
    if (!cancelBooking) return;

    setIsSaving(true);
    const response = await apiService.cancelBooking(cancelBooking.id, {
      clientTimezoneOffsetMinutes: new Date().getTimezoneOffset(),
      isAdminPage: isActingForOther,
    });
    setIsSaving(false);

    if (response) {
      setCancelBooking(undefined);
      refresh();
    }
  };

  const activeCount = bookings.filter((x) => isRunning(x) || isUpcoming(x)).length;
  const isNothingTicked =
    !selection.isOneOff && selection.recurringTrainGroupDateIds.length === 0;

  const cancelDay = cancelBooking?.date ? fromUtcDay(cancelBooking.date) : undefined;
  const isCancelLocked =
    !!cancelBooking &&
    !!cancelDay &&
    isLocked(cancelDay, cancelBooking.startOn, isActingForOther);

  return (
    <>
      <Card>
        <div className="flex flex-wrap align-items-start justify-content-between gap-3 mb-3">
          <div>
            <h2 className="m-0">{t("Book a session")}</h2>
            <p className="m-0 mt-1 text-color-secondary">
              {isActingForOther
                ? t("Book or change sessions on behalf of a member")
                : t("Pick a day, then book one session or make it recurring")}
            </p>
          </div>

          <div className="flex flex-wrap align-items-center gap-3">
            {isStaff && (
              <div className="flex align-items-center gap-2">
                <span className="text-sm text-color-secondary">{t("Booking for")}</span>
                <div style={{ width: "16rem", maxWidth: "70vw" }}>
                  <LookupComponent
                    controller="users"
                    selectedEntityId={userId}
                    isEnabled={true}
                    onChange={(x) => onUserChange(x?.id)}
                  />
                </div>
              </div>
            )}
            {balance !== undefined && (
              <div className="flex align-items-center gap-2">
                <span className="text-sm text-color-secondary">{t("Lessons left")}</span>
                <SubscriptionBalanceTag
                  balance={balance}
                  isAdminView={isActingForOther}
                />
              </div>
            )}
          </div>
        </div>

        <TabView
          activeIndex={activeIndex}
          onTabChange={(e) => setActiveIndex(e.index)}
        >
          <TabPanel
            header={t("Book")}
            leftIcon="pi pi-calendar-plus mr-2"
          >
            <BookingWeekStripComponent
              weekStart={weekStart}
              selectedDay={selectedDay}
              bookedDays={bookedDays}
              onWeekChange={onWeekChange}
              onSelect={setSelectedDay}
            />
            <BookingSessionListComponent
              day={selectedDay}
              slots={slots}
              isActingForOther={isActingForOther}
              onBook={openBook}
              onChange={onSlotChange}
              onRejoin={(slot) => rejoin(slot.skippedUnavailableDateId)}
            />
          </TabPanel>

          <TabPanel
            header={
              <span className="flex align-items-center gap-2">
                {isActingForOther ? t("Bookings") : t("My bookings")}
                <Badge value={activeCount} />
              </span>
            }
            leftIcon="pi pi-list mr-2"
          >
            <BookingMyBookingsComponent
              bookings={bookings}
              isActingForOther={isActingForOther}
              onSkip={(x) => openChange(x, "SKIP")}
              onStop={(x) => openChange(x, "STOP")}
              onCancel={setCancelBooking}
              onRejoin={(skip: TrainGroupParticipantSkipDto) => rejoin(skip.id)}
            />
          </TabPanel>
        </TabView>
      </Card>

      {/*                    */}
      {/*        Book        */}
      {/*                    */}
      <GenericDialogComponent
        header={bookSlot?.title}
        visible={bookSlot !== undefined}
        control={bookDialogControl}
        formMode={FormMode.ADD}
        footer={
          <Button
            label={t("Book")}
            icon="pi pi-check"
            loading={isSaving}
            disabled={isNothingTicked}
            onClick={onBook}
          />
        }
      >
        {bookSlot ? (
          <BookingBookFormComponent
            slot={bookSlot}
            day={selectedDay}
            selection={selection}
            onSelectionChange={setSelection}
            isActingForOther={isActingForOther}
            balance={balance}
          />
        ) : (
          <></>
        )}
      </GenericDialogComponent>

      {/*                            */}
      {/*     Skip or stop a date    */}
      {/*                            */}
      <GenericDialogComponent
        header={changeBooking?.title}
        visible={changeBooking !== undefined}
        control={changeDialogControl}
        formMode={FormMode.EDIT}
        footer={
          <Button
            label={
              changeMode === "STOP"
                ? t("Stop permanently")
                : changeDate
                  ? `${t("Skip")} ${formatDay(fromUtcDay(changeDate))}`
                  : t("Skip")
            }
            severity="danger"
            loading={isSaving}
            disabled={!changeDate}
            onClick={onChangeSave}
          />
        }
      >
        {changeBooking ? (
          <BookingChangeFormComponent
            booking={changeBooking}
            mode={changeMode}
            date={changeDate}
            onDateChange={setChangeDate}
            isActingForOther={isActingForOther}
          />
        ) : (
          <></>
        )}
      </GenericDialogComponent>

      {/*                        */}
      {/*     Cancel a one-off   */}
      {/*                        */}
      <GenericDialogComponent
        header={cancelBooking?.title}
        visible={cancelBooking !== undefined}
        control={cancelDialogControl}
        formMode={FormMode.EDIT}
        footer={
          <Button
            label={t("Cancel session")}
            severity="danger"
            loading={isSaving}
            disabled={isCancelLocked}
            onClick={onCancelSave}
          />
        }
      >
        {cancelBooking && cancelDay ? (
          <CancelContent
            booking={cancelBooking}
            day={cancelDay}
            isLocked={isCancelLocked}
          />
        ) : (
          <></>
        )}
      </GenericDialogComponent>

      {/*                                          */}
      {/*     Dates a recurring booking skipped    */}
      {/*                                          */}
      <GenericDialogComponent
        header={t(
          "The following dates have reached the maximum participants and could not be booked"
        )}
        visible={fullDates.length > 0}
        control={fullDatesDialogControl}
        formMode={FormMode.VIEW}
        footer={<></>}
      >
        <FullDatesContent dates={fullDates} />
      </GenericDialogComponent>
    </>
  );
}
