using System.Globalization;
using System.Linq.Expressions;
using Business.Services;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

namespace Business.Tests.Services
{
    /// <summary>
    /// Whether a booking holds a place on a day. The rule is written once and used two ways, inside
    /// queries and on rows already loaded, so both are checked over the same bookings: every
    /// combination of every date a booking can carry, against the rule written out again in plain
    /// steps below.
    /// </summary>
    public sealed class BookingRulesTests : IDisposable
    {
        // A Wednesday, the 5th.
        private static readonly DateTime Day = new DateTime(2031, 3, 5);

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }


        // ================================================================
        // Every booking there could be
        // ================================================================

        private sealed record Booking(
            DateTime? SelectedDate,
            TrainGroupDateTypeEnum Type,
            DateTime? FixedDay,
            DayOfWeek? DayOfWeek,
            int? DayOfMonth,
            DateTime? Start,
            DateTime CreatedOn,
            DateTime? End,
            DateTime? RemovedOn,
            DateTime? Skipped)
        {
            public TrainGroupParticipant ToParticipant()
            {
                TrainGroupParticipant participant = new TrainGroupParticipant
                {
                    SelectedDate = SelectedDate,
                    RecurringStartOnDate = Start,
                    RecurringEndOnDate = End,
                    RemovedOn = RemovedOn,
                    CreatedOn = CreatedOn,
                    TrainGroupDate = new TrainGroupDate { TrainGroupDateType = Type, FixedDay = FixedDay, RecurrenceDayOfWeek = DayOfWeek, RecurrenceDayOfMonth = DayOfMonth }
                };

                if (Skipped != null)
                    participant.TrainGroupParticipantUnavailableDates.Add(new TrainGroupParticipantUnavailableDate { UnavailableDate = Skipped.Value });

                return participant;
            }
        }

        // Around the day by the smallest step there is, so each edge has something on both sides.
        private static readonly DateTime?[] AroundTheDay = [null, Day.AddTicks(-1), Day, Day.AddHours(10), Day.AddDays(1).AddTicks(-1), Day.AddDays(1)];

        private static readonly DateTime?[] SelectedDates = AroundTheDay;
        private static readonly TrainGroupDateTypeEnum[] Types = [TrainGroupDateTypeEnum.DAY_OF_WEEK, TrainGroupDateTypeEnum.DAY_OF_MONTH, TrainGroupDateTypeEnum.FIXED_DAY];
        private static readonly DateTime?[] FixedDays = AroundTheDay;
        private static readonly DayOfWeek?[] DaysOfWeek = [null, DayOfWeek.Wednesday, DayOfWeek.Thursday];
        private static readonly int?[] DaysOfMonth = [null, 5, 6];
        private static readonly DateTime?[] Starts = [null, Day.AddDays(-7), Day, Day.AddDays(1).AddTicks(-1), Day.AddDays(1)];
        private static readonly DateTime[] Creations = [Day.AddDays(-30), Day.AddDays(1)];
        private static readonly DateTime?[] Ends = [null, Day.AddDays(-1), Day, Day.AddTicks(1), Day.AddDays(1)];
        private static readonly DateTime?[] Removals = [null, Day.AddDays(-2)];
        private static readonly DateTime?[] Skips = AroundTheDay;

        private static IEnumerable<Booking> EveryBooking()
        {
            return
                from selectedDate in SelectedDates
                from type in Types
                from fixedDay in FixedDays
                from dayOfWeek in DaysOfWeek
                from dayOfMonth in DaysOfMonth
                from start in Starts
                from createdOn in Creations
                from end in Ends
                from removedOn in Removals
                from skipped in Skips
                select new Booking(selectedDate, type, fixedDay, dayOfWeek, dayOfMonth, start, createdOn, end, removedOn, skipped);
        }


        // ================================================================
        // The rule again, in plain steps
        // ================================================================

        private static bool IsOnTheDay(DateTime? date)
        {
            return date != null && date.Value.Date == Day;
        }

        private static bool Expected(Booking booking, bool countSkipped)
        {
            // A one-off is that day, unless it was cancelled.
            if (booking.SelectedDate != null)
                return IsOnTheDay(booking.SelectedDate) && booking.RemovedOn == null;

            // A fixed day is the group's one day, unless it was cancelled.
            if (booking.Type == TrainGroupDateTypeEnum.FIXED_DAY)
                return IsOnTheDay(booking.FixedDay) && booking.RemovedOn == null;

            // A recurring one needs the weekday or the day of the month. Being removed says nothing:
            // ending it is what its end date is for.
            bool isItsDay = booking.DayOfWeek == Day.DayOfWeek || booking.DayOfMonth == Day.Day;
            if (!isItsDay)
                return false;

            // It counts from its start, or from the day it was made.
            DateTime start = booking.Start ?? booking.CreatedOn;
            if (start.Date > Day)
                return false;

            // The end is the first moment it no longer covers.
            if (booking.End != null && booking.End.Value <= Day)
                return false;

            return countSkipped || !IsOnTheDay(booking.Skipped);
        }

        private static bool ExpectedOpen(Booking booking)
        {
            if (booking.SelectedDate != null || booking.Type == TrainGroupDateTypeEnum.FIXED_DAY)
                return booking.RemovedOn == null;

            return booking.End == null || booking.End.Value > Day;
        }


        // ================================================================
        // On rows already loaded
        // ================================================================

        // 583,200 bookings, each asked four ways.
        [Fact]
        public void Every_booking_that_could_be_holds_its_place_by_the_rule()
        {
            Func<TrainGroupParticipant, bool> isOpen = BookingRules.IsOpenOn(Day).Compile();

            List<string> wrong = new List<string>();
            int count = 0;

            foreach (Booking booking in EveryBooking())
            {
                TrainGroupParticipant participant = booking.ToParticipant();
                count++;

                if (BookingRules.HoldsPlace(participant, Day) != Expected(booking, false))
                    wrong.Add($"HoldsPlace: {booking}");

                if (BookingRules.IsBooked(participant, Day) != Expected(booking, true))
                    wrong.Add($"IsBooked: {booking}");

                if (isOpen(participant) != ExpectedOpen(booking))
                    wrong.Add($"IsOpenOn: {booking}");

                if (BookingRules.IsRecurring(participant) != (booking.SelectedDate == null && booking.Type != TrainGroupDateTypeEnum.FIXED_DAY))
                    wrong.Add($"IsRecurring: {booking}");

                if (wrong.Count > 10)
                    break;
            }

            wrong.ShouldBeEmpty();
            count.ShouldBe(583_200);
        }

        // The time of the day asked about makes no difference.
        [Theory]
        [InlineData(0, 0)]
        [InlineData(10, 30)]
        [InlineData(23, 59)]
        public void The_time_of_day_asked_about_does_not_matter(int hour, int minute)
        {
            DateTime time = Day.AddHours(hour).AddMinutes(minute);

            foreach (Booking booking in EveryBooking().Where((x, i) => i % 97 == 0))
            {
                TrainGroupParticipant participant = booking.ToParticipant();

                BookingRules.HoldsPlace(participant, time).ShouldBe(BookingRules.HoldsPlace(participant, Day), booking.ToString());
                BookingRules.IsBooked(participant, time).ShouldBe(BookingRules.IsBooked(participant, Day), booking.ToString());
            }
        }


        // ================================================================
        // Inside a query
        // ================================================================

        private static IEnumerable<Booking> AFairShare()
        {
            // A step that shares no factor with any of the lists, so every value of each meets every
            // value of every other.
            return EveryBooking().Where((x, i) => i % 91 == 0);
        }

        // The same bookings as rows, each asked by sqlite itself.
        [Fact]
        public void The_same_rule_holds_inside_a_query()
        {
            Seed seed = new Seed(_database);
            User user = seed.User();
            TrainGroup group = seed.Group(user);

            List<Booking> bookings = AFairShare().ToList();
            List<TrainGroupParticipant> participants = bookings.Select(x => x.ToParticipant()).ToList();

            foreach (TrainGroupParticipant participant in participants)
            {
                participant.UserId = user.Id;
                participant.TrainGroupId = group.Id;
                participant.TrainGroupDate.TrainGroupId = group.Id;
            }

            _database.Change(db => db.AddRange(participants));

            ShouldMatch(BookingRules.HoldsPlaceOn(Day), x => Expected(x, false));
            ShouldMatch(BookingRules.IsBookedOn(Day), x => Expected(x, true));
            ShouldMatch(BookingRules.IsOpenOn(Day), ExpectedOpen);

            // And the time of the day makes no difference there either.
            ShouldMatch(BookingRules.HoldsPlaceOn(Day.AddHours(15)), x => Expected(x, false));
            ShouldMatch(BookingRules.IsBookedOn(Day.AddHours(15)), x => Expected(x, true));
            ShouldMatch(BookingRules.IsOpenOn(Day.AddHours(15)), ExpectedOpen);

            bookings.Count.ShouldBe(6_409);

            void ShouldMatch(Expression<Func<TrainGroupParticipant, bool>> rule, Func<Booking, bool> expected)
            {
                using ApiDbContext db = _database.Open();
                HashSet<int> found = db.TrainGroupParticipants.Where(rule).Select(x => x.Id).ToHashSet();

                List<string> wrong = new List<string>();
                for (int i = 0; i < bookings.Count; i++)
                    if (found.Contains(participants[i].Id) != expected(bookings[i]))
                        wrong.Add(bookings[i].ToString());

                wrong.Take(10).ShouldBeEmpty();
            }
        }

        // The day goes in as a parameter. Written into the text it would be a new query plan for
        // every date anybody looks at.
        [Fact]
        public void The_day_is_a_parameter_of_the_query()
        {
            using ApiDbContext db = _database.Open();

            string first = db.TrainGroupParticipants.Where(BookingRules.HoldsPlaceOn(Day)).ToQueryString();
            string second = db.TrainGroupParticipants.Where(BookingRules.HoldsPlaceOn(Day.AddDays(1))).ToQueryString();

            // The parameters are listed above the query, which is the same for both.
            first[first.IndexOf("SELECT", StringComparison.Ordinal)..].ShouldBe(second[second.IndexOf("SELECT", StringComparison.Ordinal)..]);
            first.ShouldNotBe(second);
        }


        // ================================================================
        // The days a group date runs on
        // ================================================================

        [Theory]
        [InlineData(DayOfWeek.Wednesday, true)]
        [InlineData(DayOfWeek.Tuesday, false)]
        [InlineData(DayOfWeek.Thursday, false)]
        [InlineData(null, false)]
        public void A_weekday_occurs_on_its_day_of_the_week(DayOfWeek? day, bool expected)
        {
            TrainGroupDate date = new TrainGroupDate { TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK, RecurrenceDayOfWeek = day, RecurrenceDayOfMonth = 5, FixedDay = Day };

            BookingRules.OccursOn(date, Day).ShouldBe(expected);
            BookingRules.OccursOn(date, Day.AddHours(23)).ShouldBe(expected);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(4, false)]
        [InlineData(6, false)]
        [InlineData(null, false)]
        public void A_day_of_the_month_occurs_on_its_number(int? day, bool expected)
        {
            TrainGroupDate date = new TrainGroupDate { TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_MONTH, RecurrenceDayOfMonth = day, RecurrenceDayOfWeek = DayOfWeek.Wednesday, FixedDay = Day };

            BookingRules.OccursOn(date, Day).ShouldBe(expected);
            BookingRules.OccursOn(date, Day.AddHours(23)).ShouldBe(expected);
        }

        public static readonly TheoryData<DateTime?, bool> FixedDayCases = new TheoryData<DateTime?, bool>
        {
            { Day, true },
            { Day.AddHours(10), true },
            { Day.AddDays(1).AddTicks(-1), true },
            { Day.AddTicks(-1), false },
            { Day.AddDays(1), false },
            { null, false },
        };

        [Theory]
        [MemberData(nameof(FixedDayCases))]
        public void A_fixed_day_occurs_on_its_date(DateTime? day, bool expected)
        {
            TrainGroupDate date = new TrainGroupDate { TrainGroupDateType = TrainGroupDateTypeEnum.FIXED_DAY, FixedDay = day, RecurrenceDayOfMonth = 5, RecurrenceDayOfWeek = DayOfWeek.Wednesday };

            BookingRules.OccursOn(date, Day).ShouldBe(expected);
            BookingRules.OccursOn(date, Day.AddHours(23)).ShouldBe(expected);
        }

        // A type that is none of the three is read as a fixed day.
        [Fact]
        public void A_date_of_no_type_at_all_occurs_like_a_fixed_day()
        {
            TrainGroupDate date = new TrainGroupDate { TrainGroupDateType = (TrainGroupDateTypeEnum)9, FixedDay = Day };

            BookingRules.OccursOn(date, Day).ShouldBeTrue();
            BookingRules.OccursOn(date, Day.AddDays(1)).ShouldBeFalse();
        }


        // ================================================================
        // The next one
        // ================================================================

        // Every weekday from every weekday: today counts, and never more than six days off.
        [Fact]
        public void The_next_weekday_is_today_or_within_the_six_days_after()
        {
            for (int from = 0; from < 7; from++)
                foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
                {
                    TrainGroupDate date = new TrainGroupDate { TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK, RecurrenceDayOfWeek = day };
                    DateTime start = Day.AddDays(from);

                    DateTime next = BookingRules.NextOccurrence(date, start.AddHours(22)).ShouldNotBeNull();

                    next.DayOfWeek.ShouldBe(day);
                    next.TimeOfDay.ShouldBe(TimeSpan.Zero);
                    (next - start).Days.ShouldBeInRange(0, 6);
                }
        }

        [Theory]
        // The same day, later the same month, and the month after.
        [InlineData(5, "2031-03-05", "2031-03-05")]
        [InlineData(9, "2031-03-05", "2031-03-09")]
        [InlineData(4, "2031-03-05", "2031-04-04")]
        // A month that has no such day is passed over.
        [InlineData(31, "2031-03-31", "2031-03-31")]
        [InlineData(31, "2031-04-01", "2031-05-31")]
        [InlineData(31, "2031-06-01", "2031-07-31")]
        [InlineData(30, "2031-01-31", "2031-03-30")]
        [InlineData(29, "2031-02-01", "2031-03-29")]
        [InlineData(29, "2032-02-01", "2032-02-29")]
        // Into the next year.
        [InlineData(1, "2031-12-02", "2032-01-01")]
        public void The_next_day_of_the_month_passes_over_the_months_without_it(int day, string from, string expected)
        {
            TrainGroupDate date = new TrainGroupDate { TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_MONTH, RecurrenceDayOfMonth = day };

            BookingRules.NextOccurrence(date, DateTime.Parse(from).AddHours(13)).ShouldBe(DateTime.Parse(expected));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(32)]
        public void A_day_no_month_has_never_comes(int? day)
        {
            TrainGroupDate date = new TrainGroupDate { TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_MONTH, RecurrenceDayOfMonth = day };

            BookingRules.NextOccurrence(date, Day).ShouldBeNull();
        }

        [Fact]
        public void A_weekday_that_names_no_day_never_comes()
        {
            BookingRules.NextOccurrence(new TrainGroupDate { TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK }, Day).ShouldBeNull();
        }

        public static readonly TheoryData<DateTime?, DateTime?> NextFixedDay = new TheoryData<DateTime?, DateTime?>
        {
            { Day, Day },
            { Day.AddHours(10), Day },
            { Day.AddDays(1), Day.AddDays(1) },
            { Day.AddYears(5), Day.AddYears(5) },
            { Day.AddTicks(-1), null },
            { Day.AddDays(-1), null },
            { null, null },
        };

        [Theory]
        [MemberData(nameof(NextFixedDay))]
        public void The_next_fixed_day_is_its_date_until_it_has_passed(DateTime? day, DateTime? expected)
        {
            TrainGroupDate date = new TrainGroupDate { TrainGroupDateType = TrainGroupDateTypeEnum.FIXED_DAY, FixedDay = day };

            BookingRules.NextOccurrence(date, Day).ShouldBe(expected);
            BookingRules.NextOccurrence(date, Day.AddHours(22)).ShouldBe(expected);
        }


        // ================================================================
        // The clock
        // ================================================================

        [Theory]
        [InlineData(0, 0)]
        [InlineData(18, 30)]
        [InlineData(23, 59)]
        public void A_session_starts_at_the_groups_time_on_the_day(int hour, int minute)
        {
            TrainGroup group = new TrainGroup { StartOn = new DateTime(2020, 6, 1, hour, minute, 0, DateTimeKind.Utc) };

            BookingRules.SessionStart(group, Day).ShouldBe(new DateTime(2031, 3, 5, hour, minute, 0));
            BookingRules.SessionStart(group, Day.AddHours(21)).ShouldBe(new DateTime(2031, 3, 5, hour, minute, 0));
        }

        // Two hours ahead of UTC in winter and three in summer, changing on the last Sundays of March
        // and October at one in the night in UTC.
        [Theory]
        [InlineData("2026-11-24T10:00:00Z", "2026-11-24 12:00:00")]
        [InlineData("2026-11-24T21:59:59Z", "2026-11-24 23:59:59")]
        [InlineData("2026-11-24T22:00:00Z", "2026-11-25 00:00:00")]
        [InlineData("2026-12-31T23:00:00Z", "2027-01-01 01:00:00")]
        [InlineData("2026-07-01T10:00:00Z", "2026-07-01 13:00:00")]
        [InlineData("2026-07-01T21:00:00Z", "2026-07-02 00:00:00")]
        [InlineData("2026-03-29T00:59:59Z", "2026-03-29 02:59:59")]
        [InlineData("2026-03-29T01:00:00Z", "2026-03-29 04:00:00")]
        [InlineData("2026-10-25T00:59:59Z", "2026-10-25 03:59:59")]
        [InlineData("2026-10-25T01:00:00Z", "2026-10-25 03:00:00")]
        [InlineData("2031-03-05T04:00:00Z", "2031-03-05 06:00:00")]
        public void The_gyms_clock_is_the_one_in_Athens(string utc, string atTheGym)
        {
            Clock clock = new Clock { Now = DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture) };

            BookingRules.GymNow(clock).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ShouldBe(atTheGym);
        }

        [Fact]
        public void With_the_machines_own_clock_it_is_the_real_time()
        {
            TimeSpan ahead = BookingRules.GymNow(TimeProvider.System) - DateTime.UtcNow;

            ahead.ShouldBeInRange(TimeSpan.FromHours(2) - TimeSpan.FromMinutes(1), TimeSpan.FromHours(3) + TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void What_staff_hold_and_how_long_before_a_member_may_change_their_mind()
        {
            BookingRules.StaffPermission.ShouldBe("TrainGroupParticipants_View");
            BookingRules.MemberChangeWindowHours.ShouldBe(12);
        }
    }

    /// <summary>
    /// The hour of a session is read through the machine's own zone, as the page reads it. On the
    /// server, which is in UTC, that changes nothing; on a laptop in Athens it does.
    /// </summary>
    [Collection(ProcessState.Name)]
    public sealed class BookingRulesZoneTests
    {
        private static readonly DateTime Day = new DateTime(2031, 3, 5);

        // Wherever the server is.
        [Fact]
        public void The_gyms_clock_is_the_same_on_every_machine()
        {
            Clock clock = new Clock { Now = new DateTimeOffset(2026, 11, 24, 22, 30, 0, TimeSpan.Zero) };

            foreach (TimeZoneInfo zone in new[] { LocalTimeZone.Utc, LocalTimeZone.Athens, LocalTimeZone.NewYork })
                using (LocalTimeZone.Use(zone))
                    BookingRules.GymNow(clock).ShouldBe(new DateTime(2026, 11, 25, 0, 30, 0));
        }

        // The offset is the one on the date the hour was saved with, winter or summer.
        [Theory]
        [InlineData(1, 16, 30)]
        [InlineData(7, 15, 30)]
        public void A_time_with_no_kind_is_read_as_the_machines_own(int month, int hour, int minute)
        {
            TrainGroup group = new TrainGroup { StartOn = new DateTime(2026, month, 1, 18, 30, 0) };

            BookingRules.SessionStart(group, Day).ShouldBe(new DateTime(2031, 3, 5, 18, 30, 0));

            using (LocalTimeZone.Use(LocalTimeZone.Athens))
                BookingRules.SessionStart(group, Day).ShouldBe(new DateTime(2031, 3, 5, hour, minute, 0));
        }

        // West of Greenwich an evening session starts the day after it was saved for, and the day
        // asked about stays as it is: only the hour is taken.
        [Fact]
        public void Only_the_hour_is_taken_even_when_the_date_moves()
        {
            TrainGroup group = new TrainGroup { StartOn = new DateTime(2026, 1, 1, 22, 0, 0) };

            using (LocalTimeZone.Use(LocalTimeZone.NewYork))
                BookingRules.SessionStart(group, Day).ShouldBe(new DateTime(2031, 3, 5, 3, 0, 0));
        }

        [Fact]
        public void A_time_known_to_be_utc_is_not_moved()
        {
            TrainGroup group = new TrainGroup { StartOn = new DateTime(2026, 1, 1, 18, 30, 0, DateTimeKind.Utc) };

            using (LocalTimeZone.Use(LocalTimeZone.Athens))
                BookingRules.SessionStart(group, Day).ShouldBe(new DateTime(2031, 3, 5, 18, 30, 0));
        }
    }
}
