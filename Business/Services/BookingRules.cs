using Core.Enums;
using Core.Models;
using System.Linq.Expressions;

namespace Business.Services
{
    // Whether a booking holds a place on a given day, written once. Spots left, the
    // capacity check on booking and rejoining, the attendance grid and the profile
    // calendar all used to carry a copy of this, and the copies had drifted apart.
    //
    //   one-off     - it is that day, and it has not been cancelled
    //   fixed day   - it is the group's fixed day, and it has not been cancelled
    //   recurring   - it is the booking's weekday (or day of the month), the day is
    //                 on or after its start and before its end, and it is not skipped
    //
    // Recurring bookings made before RecurringStartOnDate existed, and every one added
    // from the admin screens, have no start date. They count from the day the row was
    // created rather than from forever, or the calendar would fill with missed sessions
    // from before the member ever joined.
    public static class BookingRules
    {
        // Staff act for members: they book, change and end anybody's sessions, and
        // an admin screen may skip the 12-hour rule. Members hold none of the
        // participant claims but Delete, so View is what tells the two apart.
        public const string StaffPermission = "TrainGroupParticipants_View";

        public const int MemberChangeWindowHours = 12;

        // countSkipped: true asks "is this one of the booking's days at all", which the
        // calendar needs to show a skipped date; false asks whether it takes a place.
        private static readonly Expression<Func<TrainGroupParticipant, DateTime, bool, bool>> Rule =
            (x, date, countSkipped) =>
                x.SelectedDate != null
                    ? x.SelectedDate >= date && x.SelectedDate < date.AddDays(1) && x.RemovedOn == null
                : x.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY
                    ? x.TrainGroupDate.FixedDay >= date && x.TrainGroupDate.FixedDay < date.AddDays(1) && x.RemovedOn == null
                : (x.TrainGroupDate.RecurrenceDayOfWeek == date.DayOfWeek || x.TrainGroupDate.RecurrenceDayOfMonth == date.Day)
                    && (x.RecurringStartOnDate ?? x.CreatedOn) < date.AddDays(1)
                    && (x.RecurringEndOnDate == null || x.RecurringEndOnDate > date)
                    && (countSkipped || !x.TrainGroupParticipantUnavailableDates.Any(u => u.UnavailableDate >= date && u.UnavailableDate < date.AddDays(1)));

        private static readonly Func<TrainGroupParticipant, DateTime, bool, bool> CompiledRule = Rule.Compile();

        // For queries. The day goes in as a captured value so EF sends it as a
        // parameter and reuses one query plan for every date.
        public static Expression<Func<TrainGroupParticipant, bool>> HoldsPlaceOn(DateTime day) => Bind(day.Date, false);
        public static Expression<Func<TrainGroupParticipant, bool>> IsBookedOn(DateTime day) => Bind(day.Date, true);

        // For rows already loaded, which must carry TrainGroupDate and the unavailable
        // dates. Same rule, compiled once.
        public static bool HoldsPlace(TrainGroupParticipant participant, DateTime day) => CompiledRule(participant, day.Date, false);
        public static bool IsBooked(TrainGroupParticipant participant, DateTime day) => CompiledRule(participant, day.Date, true);

        // Still booked on this day or on some day after it: what the admin grids list
        // as a group's current participants.
        public static Expression<Func<TrainGroupParticipant, bool>> IsOpenOn(DateTime day)
        {
            Holder<DateTime> holder = new Holder<DateTime> { Value = day.Date };
            return x => (x.SelectedDate != null || x.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                ? x.RemovedOn == null
                : x.RecurringEndOnDate == null || x.RecurringEndOnDate > holder.Value;
        }

        public static bool IsRecurring(TrainGroupParticipant participant) =>
            participant.SelectedDate == null && participant.TrainGroupDate.TrainGroupDateType != TrainGroupDateTypeEnum.FIXED_DAY;

        public static bool OccursOn(TrainGroupDate trainGroupDate, DateTime day) => trainGroupDate.TrainGroupDateType switch
        {
            TrainGroupDateTypeEnum.DAY_OF_WEEK => trainGroupDate.RecurrenceDayOfWeek == day.DayOfWeek,
            TrainGroupDateTypeEnum.DAY_OF_MONTH => trainGroupDate.RecurrenceDayOfMonth == day.Day,
            _ => trainGroupDate.FixedDay?.Date == day.Date
        };

        // The first day on or after `from` that the group date runs. A day of the
        // month that some months lack (the 31st) simply skips those months.
        public static DateTime? NextOccurrence(TrainGroupDate trainGroupDate, DateTime from)
        {
            if (trainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                return trainGroupDate.FixedDay?.Date >= from.Date ? trainGroupDate.FixedDay.Value.Date : null;

            DateTime day = from.Date;
            for (int i = 0; i < 400; i++, day = day.AddDays(1))
                if (OccursOn(trainGroupDate, day))
                    return day;

            return null;
        }

        // The wall clock a member reads for this session. StartOn comes back from
        // sqlite with no kind, and the api's json converter runs ToUniversalTime over
        // it before any page sees it - so the hour on screen is that one, not the
        // column as stored. The calendar mail does the same for the same reason.
        public static DateTime SessionStart(TrainGroup trainGroup, DateTime day) =>
            day.Date + trainGroup.StartOn.ToUniversalTime().TimeOfDay;

        // The member's own wall clock, from the offset their browser reports.
        public static DateTime ClientNow(int clientTimezoneOffsetMinutes) =>
            DateTime.UtcNow.AddMinutes(-clientTimezoneOffsetMinutes);

        private static Expression<Func<TrainGroupParticipant, bool>> Bind(DateTime date, bool countSkipped)
        {
            Expression dateValue = Expression.Field(Expression.Constant(new Holder<DateTime> { Value = date }), nameof(Holder<DateTime>.Value));
            Expression skippedValue = Expression.Field(Expression.Constant(new Holder<bool> { Value = countSkipped }), nameof(Holder<bool>.Value));

            Expression body = new ParameterReplacer(new Dictionary<ParameterExpression, Expression>
            {
                [Rule.Parameters[1]] = dateValue,
                [Rule.Parameters[2]] = skippedValue
            }).Visit(Rule.Body);

            return Expression.Lambda<Func<TrainGroupParticipant, bool>>(body, Rule.Parameters[0]);
        }

        private sealed class Holder<T>
        {
            public T Value = default!;
        }

        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, Expression> _replacements;

            public ParameterReplacer(Dictionary<ParameterExpression, Expression> replacements)
            {
                _replacements = replacements;
            }

            protected override Expression VisitParameter(ParameterExpression node) =>
                _replacements.TryGetValue(node, out Expression? replacement) ? replacement : base.VisitParameter(node);
        }
    }
}
