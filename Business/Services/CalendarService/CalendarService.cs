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

        public List<(string IcsContent, string FileName)> GenerateCancelIcsContents(
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
                        attendeeEmail: attendeeEmail
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

        private string GenerateUid(int trainGroupParticipantId, int trainGroupDateId, Guid userId)
        {
            return $"tg-{trainGroupParticipantId}--{trainGroupDateId:N}-{userId:N}";
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

        private string GenerateIcs(string method, string uid, string summary, DateTime dtstart, DateTime? dtend, RecurrencePattern? rrule, string organizerEmail, string attendeeEmail)
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