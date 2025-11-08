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
                TimeSpan startTimeOfDayUtc = participant.TrainGroup.StartOn.TimeOfDay;
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
                TimeSpan startTimeOfDayUtc = participant.TrainGroup.StartOn.TimeOfDay;
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

        private string GenerateUid(int trainGroupParticipantId, int trainGroupDateId, Guid userId)
        {
            return $"tg-{trainGroupParticipantId}--{trainGroupDateId:N}-{userId:N}";
        }

        // Computes next occurrence datetime in UTC
        private DateTime CalculateNextOccurrenceDateTime(TrainGroupDate trainGroupDate, DateTime nowUtc)
        {
            DateTime nextLocalDate;
            switch (trainGroupDate.TrainGroupDateType)
            {
                case TrainGroupDateTypeEnum.DAY_OF_WEEK:
                    // Assume RecurrenceDayOfWeek is DayOfWeek enum or convertible
                    DayOfWeek targetDow = Enum.Parse<DayOfWeek>(trainGroupDate.RecurrenceDayOfWeek.ToString());
                    int inputDowNum = (int)nowUtc.DayOfWeek;
                    int targetDowNum = (int)targetDow;
                    int daysToAdd = (targetDowNum - inputDowNum + 7) % 7;
                    if (daysToAdd == 0) daysToAdd = 7; // Next occurrence if today (but adjust if same-day allowed; here next for safety)
                    nextLocalDate = nowUtc.Date.AddDays(daysToAdd);
                    break;
                case TrainGroupDateTypeEnum.DAY_OF_MONTH:
                    int targetDay = trainGroupDate.RecurrenceDayOfMonth ?? throw new ArgumentException("Missing RecurrenceDayOfMonth");
                    int year = nowUtc.Year;
                    int month = nowUtc.Month;
                    if (nowUtc.Day > targetDay)
                    {
                        month++;
                        if (month > 12) { month = 1; year++; }
                    }
                    nextLocalDate = new DateTime(year, month, targetDay, 0, 0, 0);
                    // JS/.NET auto-rollover for invalid (e.g., Jan 31 -> Feb 3? Wait, new Date rolls to next month)
                    break;
                case TrainGroupDateTypeEnum.FIXED_DAY:
                    nextLocalDate = trainGroupDate.FixedDay!.Value;
                    break;
                default:
                    throw new ArgumentException($"Unsupported TrainGroupDateType: {trainGroupDate.TrainGroupDateType}");
            }

            DateTime localStart = nextLocalDate.AddHours(trainGroupDate.TrainGroup.StartOn.Hour);
            localStart = localStart.AddMinutes(trainGroupDate.TrainGroup.StartOn.Minute);

            return localStart;
        }

        private (DateTime? Dtstart, RecurrencePattern? Rrule) ParseDateDescriptionToDtstartAndRrule(TrainGroupParticipant participant, TimeSpan startTimeOfDayUtc)
        {
            DateTime dtstart;
            if (participant.SelectedDate.HasValue)
            {
                // One-off: Use stored full UTC datetime (assumes time is set as in frontend)
                dtstart = participant.SelectedDate.Value;
                dtstart = dtstart.Date.Add(startTimeOfDayUtc).ToUniversalTime();
                return (dtstart, null);
            }
            else
            {
                // Recurring: Compute next upcoming occurrence with local time overlaid
                dtstart = CalculateNextOccurrenceDateTime(participant.TrainGroupDate, DateTime.UtcNow);
                dtstart = dtstart.Date.Add(startTimeOfDayUtc).ToUniversalTime();

                if (participant.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                {
                    var rrule = new RecurrencePattern
                    {
                        Frequency = FrequencyType.Monthly,
                        Interval = 1,
                        ByMonthDay = new List<int> { dtstart.Month }
                    };
                    // Optional: Set Until to e.g. 1 year from now (avoids infinite series)
                    rrule.Until = new CalDateTime(dtstart.AddYears(1));

                    return (dtstart, rrule);
                }

                if (participant.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                {
                    var rrule = new RecurrencePattern
                    {
                        Frequency = FrequencyType.Weekly,
                        Interval = 1,
                        ByDay = new List<WeekDay> { new WeekDay(dtstart.DayOfWeek) }
                    };
                    // Optional: Set Until to e.g. 1 year from now (avoids infinite series)
                    rrule.Until = new CalDateTime(dtstart.AddYears(1));

                    return (dtstart, rrule);
                }

                if (participant.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                {
                    if (participant.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                    {
                        // FIXED_DAY is a single one-off entry, no recurrence
                        return (dtstart, null);
                    }
                }
            }

            return (null, null);
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
                Start = new CalDateTime(dtstart),
                End = dtend.HasValue ? new CalDateTime(dtend.Value) : null, // Handle nullable dtend
                Summary = summary,
                Uid = uid,
                Organizer = new Organizer($"MAILTO:{organizerEmail}"),
                Created = new CalDateTime(DateTime.UtcNow), // Fixed: CalDateTime type
                LastModified = new CalDateTime(DateTime.UtcNow) // Fixed: CalDateTime type
            };

            // Fixed: Initialize and add attendee via Attendees collection
            evt.Attendees = new List<Attendee>(); // Initialize the list
            var attendee = new Attendee
            {
                Value = new Uri($"mailto:{attendeeEmail}"), // Fixed: Uri for Value
                Rsvp = true
            };
            evt.Attendees.Add(attendee);

            if (rrule != null)
            {
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