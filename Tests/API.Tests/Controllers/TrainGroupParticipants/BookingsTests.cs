using API.Tests.Host;
using Core.Enums;
using Core.Models;

namespace API.Tests.Controllers.TrainGroupParticipants
{
    /// <summary>
    /// Everything a member has booked, as the booking page lists it.
    /// </summary>
    public sealed class BookingsTests : ApiTest
    {
        // A Wednesday, the 5th. The session is at six in the evening.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _member;
        private readonly User _trainer;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;

        public BookingsTests(ApiFixture api) : base(api)
        {
            _member = Seed.User();
            _trainer = Seed.User(x => { x.FirstName = "Νίκος"; x.LastName = "Παπάς"; });
            _group = Seed.Group(_trainer, x => x.Title = "Pilates");
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
        }

        private Task<ApiResult<List<TrainGroupParticipantBookingDto>>> BookingsAsync(Caller caller, DateTime now, string? userId = null)
        {
            return PostAsync<List<TrainGroupParticipantBookingDto>>(caller, "api/TrainGroupParticipants/Bookings", new { userId, clientTimezoneOffsetMinutes = OffsetFor(now) });
        }

        private async Task<TrainGroupParticipantBookingDto> BookingAsync(DateTime now)
        {
            return (await BookingsAsync(Caller.Of(_member), now)).Value.ShouldHaveSingleItem();
        }


        // ================================================================
        // Whose
        // ================================================================

        [Fact]
        public async Task A_member_lists_their_own_and_staff_anybodys()
        {
            User other = Seed.User();
            TrainGroupParticipant mine = Seed.OneOff(_wednesday, _member, Day);
            TrainGroupParticipant theirs = Seed.OneOff(_wednesday, other, Day);

            async Task<int> OnlyAsync(Caller caller, string? userId)
            {
                return (await BookingsAsync(caller, Day, userId)).Value.Single().Id;
            }

            (await OnlyAsync(Caller.Of(_member), null)).ShouldBe(mine.Id);
            (await OnlyAsync(Caller.Of(_member), "")).ShouldBe(mine.Id);
            (await OnlyAsync(Caller.Of(_member), "  ")).ShouldBe(mine.Id);
            (await OnlyAsync(Caller.Of(_member), _member.Id.ToString())).ShouldBe(mine.Id);
            (await OnlyAsync(Staff(_member), other.Id.ToString())).ShouldBe(theirs.Id);
            (await OnlyAsync(Staff(_member), null)).ShouldBe(mine.Id);
        }

        [Fact]
        public async Task Somebody_elses_cannot_be_listed()
        {
            (await BookingsAsync(Caller.Of(_member), Day, Seed.User().Id.ToString())).ShouldBeBad(English.NotAuthorized);
        }

        [Fact]
        public async Task Somebody_who_is_no_id_is_refused_even_to_staff()
        {
            (await BookingsAsync(Staff(_member), Day, "x")).ShouldBeBad(English.NotAuthorized);
        }

        [Fact]
        public async Task A_token_that_does_not_say_who_is_refused()
        {
            (await BookingsAsync(Caller.WithNoId("TrainGroupParticipants_View"), Day, _member.Id.ToString())).ShouldBeBad(English.NotAuthorized);
        }

        [Fact]
        public async Task A_member_with_none_gets_an_empty_list()
        {
            (await BookingsAsync(Caller.Of(_member), Day)).Value.ShouldBeEmpty();
        }


        // ================================================================
        // A single session
        // ================================================================

        [Fact]
        public async Task A_one_off_is_its_day()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day.AddHours(5));

            TrainGroupParticipantBookingDto dto = await BookingAsync(Day.AddDays(-1));

            dto.Id.ShouldBe(booking.Id);
            dto.TrainGroupId.ShouldBe(_group.Id);
            dto.TrainGroupDateId.ShouldBe(_wednesday.Id);
            dto.TrainGroupDateType.ShouldBe(TrainGroupDateTypeEnum.DAY_OF_WEEK);
            dto.Title.ShouldBe("Pilates");
            dto.TrainerFullName.ShouldBe("Νίκος Παπάς");
            dto.StartOn.ShouldBe(_group.StartOn);
            dto.Duration.ShouldBe(_group.Duration);
            dto.IsOneOff.ShouldBeTrue();
            dto.Date.ShouldBe(Day);
            dto.Date!.Value.Kind.ShouldBe(DateTimeKind.Utc);
            dto.NextSessionDate.ShouldBe(Day);
            dto.RemovedOn.ShouldBeNull();
            dto.RemovedBy_FullName.ShouldBe("");
            dto.RecurrenceDayOfWeek.ShouldBeNull();
            dto.RecurrenceDayOfMonth.ShouldBeNull();
            dto.StartOnDate.ShouldBeNull();
            dto.EndOnDate.ShouldBeNull();
            dto.LastSessionDate.ShouldBeNull();
            dto.UpcomingSkips.ShouldBeEmpty();
        }

        // Upcoming until it starts.
        [Theory]
        [InlineData(-2, true)]
        [InlineData(0, false)]
        [InlineData(60 * 24, false)]
        public async Task A_single_session_is_next_until_it_starts(int minutesFromStart, bool isNext)
        {
            Seed.OneOff(_wednesday, _member, Day);

            (await BookingAsync(Day.AddHours(18).AddMinutes(minutesFromStart))).NextSessionDate.ShouldBe(isNext ? Day : null);
        }

        [Fact]
        public async Task A_session_given_up_is_not_next_and_says_who_by()
        {
            DateTime removedOn = new DateTime(2031, 3, 1, 10, 20, 30, DateTimeKind.Utc);
            Seed.OneOff(_wednesday, _member, Day, x => { x.RemovedOn = removedOn; x.RemovedBy_FullName = "Desk"; });

            TrainGroupParticipantBookingDto dto = await BookingAsync(Day.AddDays(-1));

            dto.NextSessionDate.ShouldBeNull();
            dto.RemovedOn.ShouldBe(removedOn);
            dto.RemovedBy_FullName.ShouldBe("Desk");
        }

        [Fact]
        public async Task A_fixed_day_is_its_date()
        {
            Seed.Participant(Seed.Fixed(_group, Day.AddHours(9)), _member);

            TrainGroupParticipantBookingDto dto = await BookingAsync(Day.AddDays(-1));

            dto.IsOneOff.ShouldBeTrue();
            dto.TrainGroupDateType.ShouldBe(TrainGroupDateTypeEnum.FIXED_DAY);
            dto.Date.ShouldBe(Day);
            dto.NextSessionDate.ShouldBe(Day);
        }

        [Fact]
        public async Task A_fixed_day_that_names_no_date_has_none()
        {
            Seed.Participant(Seed.Fixed(_group, null), _member);

            TrainGroupParticipantBookingDto dto = await BookingAsync(Day);

            dto.Date.ShouldBeNull();
            dto.NextSessionDate.ShouldBeNull();
        }


        // ================================================================
        // Every week
        // ================================================================

        [Fact]
        public async Task A_recurring_booking_is_its_weekday_and_the_time_it_covers()
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70).AddHours(5), Day.AddDays(70).AddHours(5));

            TrainGroupParticipantBookingDto dto = await BookingAsync(Day.AddDays(-1));

            dto.IsOneOff.ShouldBeFalse();
            dto.Date.ShouldBeNull();
            dto.RecurrenceDayOfWeek.ShouldBe(3);
            dto.RecurrenceDayOfMonth.ShouldBeNull();
            dto.StartOnDate.ShouldBe(Day.AddDays(-70));
            dto.EndOnDate.ShouldBe(Day.AddDays(70));
            dto.NextSessionDate.ShouldBe(Day);
            // The end is the first day it no longer covers, though by the hour it still holds a place then.
            dto.LastSessionDate.ShouldBe(Day.AddDays(63));
        }

        [Fact]
        public async Task A_monthly_booking_is_its_number()
        {
            Seed.Recurring(Seed.Monthly(_group, 5), _member, Day.AddDays(-70));

            TrainGroupParticipantBookingDto dto = await BookingAsync(Day.AddDays(1));

            dto.RecurrenceDayOfMonth.ShouldBe(5);
            dto.RecurrenceDayOfWeek.ShouldBeNull();
            dto.NextSessionDate.ShouldBe(new DateTime(2031, 4, 5));
            dto.EndOnDate.ShouldBeNull();
            dto.LastSessionDate.ShouldBeNull();
        }

        // Added from the admin screens, or before the start was kept.
        [Fact]
        public async Task A_booking_with_no_start_counts_from_the_day_it_was_made()
        {
            Seed.Recurring(_wednesday, _member, null, change: x => x.CreatedOn = new DateTime(2031, 2, 1, 15, 0, 0));

            (await BookingAsync(Day)).StartOnDate.ShouldBe(new DateTime(2031, 2, 1));
        }

        [Theory]
        // Today's until it starts, then next week's.
        [InlineData(-2, 0)]
        [InlineData(0, 7)]
        [InlineData(60 * 24 * 6, 7)]
        [InlineData(60 * 24 * 7 + 1, 14)]
        public async Task The_next_session_is_the_first_that_has_not_started(int minutesFromStart, int days)
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70));

            (await BookingAsync(Day.AddHours(18).AddMinutes(minutesFromStart))).NextSessionDate.ShouldBe(Day.AddDays(days));
        }

        [Fact]
        public async Task A_skipped_session_is_not_the_next()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-70));
            Seed.Skip(booking, Day);
            Seed.Skip(booking, Day.AddDays(7));

            (await BookingAsync(Day.AddDays(-1))).NextSessionDate.ShouldBe(Day.AddDays(14));
        }

        [Fact]
        public async Task A_booking_that_has_not_begun_begins_with_its_first_session()
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(70));

            (await BookingAsync(Day)).NextSessionDate.ShouldBe(Day.AddDays(70));
        }

        // Its end is the first date it no longer covers.
        [Theory]
        [InlineData(0, null)]
        [InlineData(1, 0)]
        [InlineData(7, 0)]
        [InlineData(8, 0)]
        public async Task There_is_no_next_session_from_the_end_on(int end, int? next)
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70), Day.AddDays(end));

            (await BookingAsync(Day.AddDays(-1))).NextSessionDate.ShouldBe(next == null ? null : Day.AddDays(next.Value));
        }

        // Nor on the day of it, when the end has an hour - though by the hour a place is still held.
        [Fact]
        public async Task The_day_of_the_end_is_past_the_end()
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70), Day.AddHours(5));

            TrainGroupParticipantBookingDto dto = await BookingAsync(Day.AddDays(-1));

            dto.NextSessionDate.ShouldBeNull();
            dto.LastSessionDate.ShouldBe(Day.AddDays(-7));
        }

        // Every session up to the end is skipped.
        [Fact]
        public async Task With_every_session_left_skipped_there_is_no_next()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-70), Day.AddDays(10));
            Seed.Skip(booking, Day);
            Seed.Skip(booking, Day.AddDays(7));

            (await BookingAsync(Day.AddDays(-1))).NextSessionDate.ShouldBeNull();
        }

        // Four hundred sessions are looked at and no more.
        [Fact]
        public async Task A_session_after_four_hundred_is_not_looked_at()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Day);
            Database.Change(db => db.TrainGroupParticipantUnavailableDates.AddRange(Enumerable.Range(0, 400)
                .Select(x => new TrainGroupParticipantUnavailableDate { TrainGroupParticipantId = booking.Id, UnavailableDate = Day.AddDays(7 * x) })));
            (await BookingAsync(Day)).NextSessionDate.ShouldBeNull();

            // One fewer, and the last of them is.
            Database.Change(db => db.TrainGroupParticipantUnavailableDates.Remove(db.TrainGroupParticipantUnavailableDates.OrderBy(x => x.UnavailableDate).Last()));
            (await BookingAsync(Day)).NextSessionDate.ShouldBe(Day.AddDays(7 * 399));
        }

        [Fact]
        public async Task A_booking_on_a_day_that_never_comes_has_no_next()
        {
            Seed.Recurring(Seed.Monthly(_group, 40), _member, Day);

            (await BookingAsync(Day)).NextSessionDate.ShouldBeNull();
        }

        // The last session before the end, skipped ones passed over; none when there was none.
        [Fact]
        public async Task An_ended_booking_says_which_session_was_the_last()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-21), Day.AddDays(3));
            (await BookingAsync(Day.AddDays(30))).LastSessionDate.ShouldBe(Day);

            Seed.Skip(booking, Day);
            (await BookingAsync(Day.AddDays(30))).LastSessionDate.ShouldBe(Day.AddDays(-7));
        }

        [Fact]
        public async Task A_booking_that_ended_before_its_first_session_has_no_last()
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(1), Day.AddDays(3));

            (await BookingAsync(Day.AddDays(30))).LastSessionDate.ShouldBeNull();
        }

        // Those from today on and before the end, in order.
        [Fact]
        public async Task The_skips_still_to_come_are_listed()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-70), Day.AddDays(21));
            Seed.Skip(booking, Day.AddDays(21));
            Seed.Skip(booking, Day.AddDays(-7));
            TrainGroupParticipantUnavailableDate later = Seed.Skip(booking, Day.AddDays(14).AddHours(3));
            TrainGroupParticipantUnavailableDate today = Seed.Skip(booking, Day);

            List<TrainGroupParticipantSkipDto> skips = (await BookingAsync(Day.AddHours(20))).UpcomingSkips;

            skips.Select(x => x.Id).ShouldBe([today.Id, later.Id]);
            skips.Select(x => x.Date).ShouldBe([Day, Day.AddDays(14)]);
        }

        [Fact]
        public async Task The_skips_of_a_booking_with_no_end_are_all_listed()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-70));
            Seed.Skip(booking, Day.AddDays(700));

            (await BookingAsync(Day)).UpcomingSkips.Count.ShouldBe(1);
        }


        // ================================================================
        // The trainer
        // ================================================================

        [Theory]
        [InlineData("Νίκος", "", "Νίκος")]
        [InlineData("", "Παπάς", "Παπάς")]
        [InlineData("", "", "")]
        public async Task A_trainers_name_has_no_spare_space(string firstName, string lastName, string expected)
        {
            Seed.OneOff(_wednesday, _member, Day);
            Database.Change(db => { User trainer = db.Users.Single(x => x.Id == _trainer.Id); trainer.FirstName = firstName; trainer.LastName = lastName; });

            (await BookingAsync(Day)).TrainerFullName.ShouldBe(expected);
        }
    }
}
