using Business.Services;
using Core.Enums;
using Core.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Business.Services.CalendarService
{
    public class CalendarService : ICalendarService
    {
        // The language the numbers in an entry's name are written in, whoever asks.
        private static readonly CultureInfo UidLanguage = CultureInfo.GetCultureInfo("el");

        private readonly DateTime _referenceDate;

        public CalendarService()
        {
            _referenceDate = DateTime.UtcNow;
        }

        // For testing: Allow injecting a fixed date
        public CalendarService(DateTime referenceDate)
        {
            _referenceDate = referenceDate;
        }

        public List<(string IcsContent, string FileName)> GenerateAddIcsContents(
            List<TrainGroupParticipant> trainGroupParticipants,
            string organizerEmail,
            string attendeeEmail,
            Guid userId)
        {
            var results = new List<(string, string)>();
            foreach (var participant in trainGroupParticipants)
            {

                string uid = GenerateUid(participant.Id, participant.TrainGroupDateId, userId);
                TimeSpan startTimeOfDayUtc = GetDisplayedStartTimeOfDay(participant.TrainGroup);
                int durationMinutes = (int)participant.TrainGroup.Duration.TimeOfDay.TotalMinutes;

                (DateTime? dtstart, RecurrencePattern? rrule) = ParseDateDescriptionToDtstartAndRrule(participant, startTimeOfDayUtc);

                if (dtstart != null)
                {
                    var dtend = dtstart.Value.AddMinutes(durationMinutes);

                    var icsContent = GenerateIcs(
                        method: "PUBLISH",
                        uid: uid,
                        summary: participant.TrainGroup.Title,
                        dtstart: dtstart.Value,
                        dtend: dtend, // Always set for adds
                        rrule: rrule,
                        organizerEmail: organizerEmail,
                        attendeeEmail: attendeeEmail
                    );

                    var fileName = $"add-{SanitizeForFilename(uid)}.ics";
                    results.Add((icsContent, fileName));
                }
            }

            return results;
        }

        // A booking stopped from a later date keeps its sessions up to then. It used to be
        // cancelled like any other, and a cancelled series goes from the member's calendar
        // altogether, the sessions they still have included. So the entry is written again
        // under the same name, up to the last session that is theirs.
        public List<(string IcsContent, string FileName)> GenerateEndIcsContents(
            List<TrainGroupParticipant> trainGroupParticipants,
            string organizerEmail,
            string attendeeEmail,
            Guid userId)
        {
            var results = new List<(string, string)>();

            foreach (var participant in trainGroupParticipants)
            {
                DateTime? lastDay = GetLastKeptDay(participant);
                if (lastDay == null)
                    continue;

                string uid = GenerateUid(participant.Id, participant.TrainGroupDateId, userId);
                TimeSpan startTimeOfDayUtc = GetDisplayedStartTimeOfDay(participant.TrainGroup);
                int durationMinutes = (int)participant.TrainGroup.Duration.TimeOfDay.TotalMinutes;

                (DateTime? dtstart, RecurrencePattern? rrule) = GetSeries(participant.TrainGroupDate, GetFirstDay(participant), startTimeOfDayUtc);
                if (dtstart == null || rrule == null)
                    continue;

                rrule.Until = new CalDateTime(lastDay.Value.Add(startTimeOfDayUtc));

                // Written again, the series would bring back the dates the member has skipped.
                List<DateTime> skippedStarts = participant.TrainGroupParticipantUnavailableDates
                    .Select(x => x.UnavailableDate.Date)
                    .Where(x => x >= dtstart.Value.Date && x < lastDay)
                    .Order()
                    .Select(x => x.Add(startTimeOfDayUtc))
                    .ToList();

                var icsContent = GenerateIcs(
                    method: "PUBLISH",
                    uid: uid,
                    summary: participant.TrainGroup.Title,
                    dtstart: dtstart.Value,
                    dtend: dtstart.Value.AddMinutes(durationMinutes),
                    rrule: rrule,
                    organizerEmail: organizerEmail,
                    attendeeEmail: attendeeEmail,
                    sequence: GetChangeSequence(),
                    skippedStarts: skippedStarts
                );

                // Named as the mail tells the member to look for it.
                var fileName = $"add-{SanitizeForFilename(uid)}.ics";
                results.Add((icsContent, fileName));
            }

            return results;
        }

        public List<(string IcsContent, string FileName)> GenerateCancelIcsContents(
            List<TrainGroupParticipant> trainGroupParticipants,
            string organizerEmail,
            string attendeeEmail,
            Guid userId)
        {
            var results = new List<(string, string)>();

            foreach (var participant in trainGroupParticipants)
            {
                // It ends and is not cancelled. See above.
                if (GetLastKeptDay(participant) != null)
                    continue;

                string uid = GenerateUid(participant.Id, participant.TrainGroupDateId, userId);
                TimeSpan startTimeOfDayUtc = GetDisplayedStartTimeOfDay(participant.TrainGroup);
                int durationMinutes = (int)participant.TrainGroup.Duration.TimeOfDay.TotalMinutes;

                (DateTime? dtstart, RecurrencePattern? rrule) = ParseDateDescriptionToDtstartAndRrule(participant, startTimeOfDayUtc);
                // For cancels, rrule not needed in ICS (UID matches series)

                if (dtstart != null)
                {
                    var icsContent = GenerateIcs(
                        method: "CANCEL",
                        uid: uid,
                        summary: $"Cancelled: {participant.TrainGroup.Title}",
                        dtstart: dtstart.Value,
                        dtend: default, // Optional for cancels
                        rrule: null,
                        organizerEmail: organizerEmail,
                        attendeeEmail: attendeeEmail,
                        sequence: GetChangeSequence()
                    );

                    var fileName = $"cancel-{SanitizeForFilename(uid)}.ics";
                    results.Add((icsContent, fileName));
                }
            }

            return results;
        }

        // The hour a member actually reads on screen. Sqlite returns StartOn with no
        // kind, and the api's json converter runs ToUniversalTime over it before the
        // client ever sees it, which moves it by the host's offset on the stored date.
        // The calendar file used to take the column as it stood and so disagreed with
        // the screen by exactly that much. Calling the same conversion here keeps the
        // two in step whatever the offset and whatever the daylight saving rules say.
        //
        // Only the start is treated this way. Duration is a length, not a moment, and
        // putting it through a timezone would turn an hour into something else.
        private TimeSpan GetDisplayedStartTimeOfDay(TrainGroup trainGroup)
        {
            return trainGroup.StartOn.ToUniversalTime().TimeOfDay;
        }

        // What a calendar knows an entry by: a file that changes it or takes it out has to
        // carry the very same name. The number of the group's day reads 57,000 in Greek and
        // 57.000 in English, so what was booked in one language was not found in another. It
        // is always the Greek way now, which is how nearly every entry out there was made.
        private string GenerateUid(int trainGroupParticipantId, int trainGroupDateId, Guid userId)
        {
            return string.Create(UidLanguage, $"tg-{trainGroupParticipantId}--{trainGroupDateId:N}-{userId:N}");
        }

        // A calendar takes a change to an entry it has when the number of the change is higher
        // than the one it has. An entry starts with nought, so any clock will do from then on.
        private int GetChangeSequence()
        {
            return (int)(DateTime.UtcNow - new DateTime(2020, 1, 1)).TotalMinutes;
        }

        // The last session a stopped booking still covers. None for one that goes on, for one
        // that is over before its first session, and for anything but a recurring one.
        private DateTime? GetLastKeptDay(TrainGroupParticipant participant)
        {
            if (participant.RecurringEndOnDate == null || !BookingRules.IsRecurring(participant))
                return null;

            DateTime firstDay = GetFirstDay(participant);
            for (DateTime day = participant.RecurringEndOnDate.Value.Date.AddDays(-1); day >= firstDay; day = day.AddDays(-1))
                if (BookingRules.HoldsPlace(participant, day))
                    return day;

            return null;
        }

        // Where a recurring booking begins. One from before the start was kept began when it
        // was made, as in BookingRules.
        private DateTime GetFirstDay(TrainGroupParticipant participant)
        {
            return (participant.RecurringStartOnDate ?? participant.CreatedOn).Date;
        }

        private (DateTime? Dtstart, RecurrencePattern? Rrule) ParseDateDescriptionToDtstartAndRrule(TrainGroupParticipant participant, TimeSpan startTimeOfDayUtc)
        {
            TrainGroupDate trainGroupDate = participant.TrainGroupDate;

            // One-off: the day that was booked.
            if (participant.SelectedDate.HasValue)
                return (participant.SelectedDate.Value.Date.Add(startTimeOfDayUtc), null);

            // A fixed-day event is a single entry on its date.
            if (trainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                return trainGroupDate.FixedDay.HasValue
                    ? (trainGroupDate.FixedDay.Value.Date.Add(startTimeOfDayUtc), null)
                    : (null, null);

            // Recurring: the series starts on the booking's first session. It used to start
            // on the next occurrence after today, which skipped today - book "every Monday"
            // on a Monday and that evening's session never reached the calendar - and
            // ignored a booking that starts weeks later. Bookings from before the start date
            // was kept, and the ones the admin screens add, start from their next session.
            DateTime from = participant.RecurringStartOnDate?.Date ?? DateTime.UtcNow.Date;
            return GetSeries(trainGroupDate, from, startTimeOfDayUtc);
        }

        private (DateTime? Dtstart, RecurrencePattern? Rrule) GetSeries(TrainGroupDate trainGroupDate, DateTime from, TimeSpan startTimeOfDayUtc)
        {
            DateTime? firstDay = BookingRules.NextOccurrence(trainGroupDate, from);
            if (firstDay == null)
                return (null, null);

            DateTime dtstart = firstDay.Value.Add(startTimeOfDayUtc);

            RecurrencePattern rrule = trainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
                ? new RecurrencePattern
                {
                    Frequency = FrequencyType.Monthly,
                    Interval = 1,
                    // The day of the month. It used to be the month number, so a booking
                    // on the 5th made in October repeated on the 10th of every month.
                    ByMonthDay = new List<int> { trainGroupDate.RecurrenceDayOfMonth ?? dtstart.Day }
                }
                : new RecurrencePattern
                {
                    Frequency = FrequencyType.Weekly,
                    Interval = 1,
                    ByDay = new List<WeekDay> { new WeekDay(dtstart.DayOfWeek) }
                };

            // A year ahead rather than an endless series.
            rrule.Until = new CalDateTime(dtstart.AddYears(1));

            return (dtstart, rrule);
        }

        //private DateTime GetNextWeeklyOccurrence(DateTime now, DayOfWeek targetDow)
        //{
        //    var daysAhead = ((int)targetDow - (int)now.DayOfWeek + 7) % 7;
        //    if (daysAhead == 0) daysAhead = 7; // Next week if today
        //    return now.AddDays(daysAhead);
        //}

        //private DateTime GetNextMonthlyOccurrence(DateTime now, int targetDay)
        //{
        //    var year = now.Year;
        //    var month = now.Month + 1;
        //    if (month > 12) { month = 1; year++; }
        //    var candidate = new DateTime(year, month, targetDay);
        //    if (candidate <= now)
        //    {
        //        candidate = candidate.AddMonths(1);
        //    }
        //    // Note: If targetDay > days in month, this throws; assume valid per business logic
        //    return candidate;
        //}

        private string GenerateIcs(string method, string uid, string summary, DateTime dtstart, DateTime? dtend, RecurrencePattern? rrule, string organizerEmail, string attendeeEmail, int sequence = 0, List<DateTime>? skippedStarts = null)
        {
            var calendar = new Ical.Net.Calendar();
            calendar.Version = "2.0";
            calendar.Method = method;

            var evt = new CalendarEvent
            {
                Start = new CalDateTime(DateTime.SpecifyKind(dtstart, DateTimeKind.Unspecified)),  // Floating time
                End = dtend.HasValue ? new CalDateTime(DateTime.SpecifyKind(dtend.Value, DateTimeKind.Unspecified)) : null,  // Floating time
                Summary = summary,
                Uid = uid,
                Sequence = sequence,
                Organizer = new Organizer($"MAILTO:{organizerEmail}"),
                Created = new CalDateTime(DateTime.UtcNow),
                LastModified = new CalDateTime(DateTime.UtcNow)
            };

            // Initialize and add attendee via Attendees collection
            evt.Attendees = new List<Attendee>(); // Initialize the list
            var attendee = new Attendee
            {
                Value = new Uri($"mailto:{attendeeEmail}"), // Uri for Value
                Rsvp = true
            };
            evt.Attendees.Add(attendee);

            if (rrule != null)
            {
                // Also make rrule.Until floating if set
                if (rrule.Until != null)
                {
                    rrule.Until = new CalDateTime(DateTime.SpecifyKind(rrule.Until.Value, DateTimeKind.Unspecified));
                }
                evt.RecurrenceRules.Add(rrule);
            }

            foreach (DateTime skippedStart in skippedStarts ?? new List<DateTime>())
                evt.ExceptionDates.Add(new CalDateTime(DateTime.SpecifyKind(skippedStart, DateTimeKind.Unspecified)));

            calendar.Events.Add(evt);

            var serializer = new CalendarSerializer();
            return serializer.SerializeToString(calendar);
        }

        private string SanitizeForFilename(string input)
        {
            return Regex.Replace(input, "[^a-zA-Z0-9\\-]", "_");
        }
    }
}