using API.Tests.Host;
using Core.Enums;
using Core.Models;
using Core.Translations;
using Microsoft.EntityFrameworkCore;

namespace API.Tests.Controllers.TrainGroupParticipants
{
    /// <summary>
    /// One press of Book: a single session, the group's weekdays from a date on, or both at once.
    /// </summary>
    public sealed class BookTests : ApiTest
    {
        // A Wednesday, the 5th. The session is at six in the evening.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Now = Day.AddDays(-2).AddHours(10);

        private readonly User _member;
        private readonly User _staff;
        private readonly User _trainer;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;
        private readonly TrainGroupDate _friday;

        public BookTests(ApiFixture api) : base(api)
        {
            _member = Seed.User(x => { x.Email = "maria@gym.test"; x.FirstName = "Maria"; x.LastName = "Papa"; });
            _staff = Seed.User(x => { x.Email = "staff@gym.test"; x.FirstName = "Desk"; x.LastName = ""; });
            _trainer = Seed.User();
            _group = Seed.Group(_trainer, x => x.MaxParticipants = 2);
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            _friday = Seed.Weekly(_group, DayOfWeek.Friday);
        }

        private Task<ApiResult<List<DateTime>>> BookAsync(
            Caller caller,
            bool isOneOff = false,
            TrainGroupDate[]? recurring = null,
            DateTime? day = null,
            string? userId = null,
            DateTime? now = null,
            int? groupId = null)
        {
            return PostAsync<List<DateTime>>(caller, "api/TrainGroupParticipants/Book", new
            {
                trainGroupId = groupId ?? _group.Id,
                selectedDate = day ?? Day,
                isOneOff,
                recurringTrainGroupDateIds = (recurring ?? []).Select(x => x.Id),
                userId,
                clientTimezoneOffsetMinutes = OffsetFor(now ?? Now)
            });
        }

        private Task<ApiResult<List<DateTime>>> OnceAsync(DateTime? day = null, DateTime? now = null, int? groupId = null)
        {
            return BookAsync(Caller.Of(_member), isOneOff: true, day: day, now: now, groupId: groupId);
        }

        private Task<ApiResult<List<DateTime>>> EveryAsync(TrainGroupDate date, DateTime? day = null)
        {
            return BookAsync(Caller.Of(_member), recurring: [date], day: day);
        }

        private List<TrainGroupParticipant> Bookings(User? user = null)
        {
            Guid userId = (user ?? _member).Id;

            return Database.Read(db => db.TrainGroupParticipants.Include(x => x.TrainGroupParticipantUnavailableDates).Where(x => x.UserId == userId).OrderBy(x => x.Id).ToList());
        }

        private static List<string> SkipsOf(TrainGroupParticipant booking)
        {
            return booking.TrainGroupParticipantUnavailableDates.Select(x => x.UnavailableDate.ToString("yyyy-MM-dd")).Order().ToList();
        }

        private static void ShouldBeSaved(ApiResult<List<DateTime>> result, params string[] fullDates)
        {
            result.ShouldSucceed(English.Of(TranslationKeys.Booking_saved));
            result.Value.Select(x => x.ToString("yyyy-MM-dd")).ShouldBe(fullDates);
        }

        // Somebody else, in the way.
        private TrainGroupParticipant Taken(DateTime day)
        {
            return Seed.OneOff(day.DayOfWeek == DayOfWeek.Friday ? _friday : _wednesday, Seed.User(), day);
        }


        // ================================================================
        // Who books, and for whom
        // ================================================================

        [Fact]
        public async Task A_member_books_for_themselves()
        {
            ShouldBeSaved(await OnceAsync());

            TrainGroupParticipant booking = Bookings().Single();
            booking.SelectedDate.ShouldBe(Day);
            booking.RecurringStartOnDate.ShouldBeNull();
            booking.RecurringEndOnDate.ShouldBeNull();
            booking.TrainGroupId.ShouldBe(_group.Id);
            booking.TrainGroupDateId.ShouldBe(_wednesday.Id);
            booking.RemovedOn.ShouldBeNull();
            booking.CreatedBy_Id.ShouldBe(_member.Id.ToString());
            booking.CreatedBy_FullName.ShouldBe("Maria Papa");
            SkipsOf(booking).ShouldBeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task Naming_nobody_is_naming_oneself(string? userId)
        {
            ShouldBeSaved(await BookAsync(Caller.Of(_member), isOneOff: true, userId: userId));

            Bookings().Count.ShouldBe(1);
        }

        [Fact]
        public async Task A_member_may_name_themselves()
        {
            ShouldBeSaved(await BookAsync(Caller.Of(_member), isOneOff: true, userId: _member.Id.ToString().ToUpperInvariant()));
        }

        [Fact]
        public async Task Staff_book_for_a_member_and_the_booking_says_so()
        {
            ShouldBeSaved(await BookAsync(Staff(_staff), isOneOff: true, recurring: [_friday], userId: _member.Id.ToString()));

            Bookings().ShouldAllBe(x => x.CreatedBy_Id == _staff.Id.ToString() && x.CreatedBy_FullName == "Desk");
            Bookings(_staff).ShouldBeEmpty();
            Emails.Lines.Single().ShouldStartWith("booking maria@gym.test:");
        }

        [Fact]
        public async Task A_member_cannot_book_for_another()
        {
            (await BookAsync(Caller.Of(_staff, "TrainGroupParticipants_Add", "TrainGroupParticipants_Delete"), isOneOff: true, userId: _member.Id.ToString())).ShouldBeBad(English.NotAuthorized);

            Bookings().ShouldBeEmpty();
        }

        [Fact]
        public async Task Somebody_who_is_no_id_is_refused()
        {
            (await BookAsync(Staff(_staff), isOneOff: true, userId: "x")).ShouldBeBad(English.Invalid);
        }

        [Fact]
        public async Task A_token_that_does_not_say_who_is_refused()
        {
            (await BookAsync(Caller.WithNoId("TrainGroupParticipants_View"), isOneOff: true, userId: _member.Id.ToString())).ShouldBeBad(English.NotAuthorized);
        }

        [Fact]
        public async Task A_member_who_is_not_there_breaks()
        {
            (await BookAsync(Staff(_staff), isOneOff: true, userId: Guid.NewGuid().ToString())).ShouldBreak();
        }

        // Somebody whose account is gone and whose token is not yet.
        [Fact]
        public async Task Staff_who_are_gone_are_nameless()
        {
            User gone = new User { Id = Guid.NewGuid(), UserName = "gone" };

            ShouldBeSaved(await BookAsync(Staff(gone), isOneOff: true, userId: _member.Id.ToString()));

            Bookings().Single().CreatedBy_FullName.ShouldBe("");
            Bookings().Single().CreatedBy_Id.ShouldBe(gone.Id.ToString());
        }


        // ================================================================
        // What was picked
        // ================================================================

        [Fact]
        public async Task Nothing_ticked_is_refused()
        {
            (await BookAsync(Caller.Of(_member))).ShouldBeBad(English.Of(TranslationKeys.Select_at_least_one_date_to_book));
        }

        [Fact]
        public async Task A_group_that_is_not_there_is_not_found()
        {
            (await OnceAsync(groupId: 99)).ShouldBeBad(English.Of(TranslationKeys._0_not_found, "TrainGroup"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(4)]
        public async Task A_day_the_group_does_not_run_on_is_refused(int days)
        {
            (await OnceAsync(Day.AddDays(days))).ShouldBeBad(English.Of(TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates));
            (await EveryAsync(_wednesday, Day.AddDays(days))).ShouldBeBad(English.Of(TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates));
        }

        // The clock is the member's, to the minute: two before, and the moment itself.
        [Theory]
        [InlineData(-2, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(60 * 24, false)]
        public async Task A_session_can_be_booked_until_it_starts(int minutesFromStart, bool isBooked)
        {
            DateTime now = Day.AddHours(18).AddMinutes(minutesFromStart);

            ApiResult<List<DateTime>> once = await OnceAsync(now: now);
            ApiResult<List<DateTime>> every = await BookAsync(Staff(_staff), recurring: [_friday], now: now);

            foreach (ApiResult<List<DateTime>> result in new[] { once, every })
                if (isBooked)
                    ShouldBeSaved(result);
                else
                    result.ShouldBeBad(English.Of(TranslationKeys.This_session_has_already_started));
        }

        [Fact]
        public async Task The_time_of_the_day_picked_does_not_matter()
        {
            ShouldBeSaved(await OnceAsync(Day.AddHours(22)));

            Bookings().Single().SelectedDate.ShouldBe(Day);
        }

        [Fact]
        public async Task A_weekday_that_is_not_the_groups_is_refused()
        {
            TrainGroupDate elsewhere = Seed.Weekly(Seed.Group(_trainer), DayOfWeek.Wednesday);

            (await EveryAsync(elsewhere)).ShouldBeBad(English.Invalid);
            (await BookAsync(Caller.Of(_member), recurring: [_friday, elsewhere])).ShouldBeBad(English.Invalid);
            (await BookAsync(Caller.Of(_member), recurring: [new TrainGroupDate { Id = 99 }])).ShouldBeBad(English.Invalid);

            Bookings().ShouldBeEmpty();
        }

        [Fact]
        public async Task A_fixed_day_cannot_be_booked_every_time()
        {
            (await EveryAsync(Seed.Fixed(_group, Day.AddDays(1)))).ShouldBeBad(English.Invalid);
        }

        [Fact]
        public async Task The_session_and_its_own_weekday_together_are_one_too_many()
        {
            (await BookAsync(Caller.Of(_member), isOneOff: true, recurring: [_wednesday])).ShouldBeBad(English.Of(TranslationKeys.Current_date_is_already_selected_in_a_Recurrence_date));
            (await BookAsync(Caller.Of(_member), isOneOff: true, recurring: [_friday, _wednesday])).ShouldBeBad(English.Of(TranslationKeys.Current_date_is_already_selected_in_a_Recurrence_date));
        }

        // A day that no month has is in the form all the same, and never comes.
        [Fact]
        public async Task A_day_that_never_comes_is_refused()
        {
            TrainGroup group = Seed.Group(_trainer);
            Seed.Monthly(group, 5);
            TrainGroupDate never = Seed.Monthly(group, 40);

            (await BookAsync(Caller.Of(_member), recurring: [never], groupId: group.Id)).ShouldBeBad(English.Invalid);
        }


        // ================================================================
        // One session
        // ================================================================

        [Fact]
        public async Task A_fixed_day_is_booked_with_no_date_of_its_own()
        {
            TrainGroup group = Seed.Group(_trainer);
            TrainGroupDate fixedDay = Seed.Fixed(group, Day.AddHours(9));

            ShouldBeSaved(await OnceAsync(groupId: group.Id));

            Bookings().Single().SelectedDate.ShouldBeNull();
            Bookings().Single().RecurringStartOnDate.ShouldBeNull();
            Bookings().Single().TrainGroupDateId.ShouldBe(fixedDay.Id);
        }

        [Fact]
        public async Task A_fixed_day_comes_before_a_weekday()
        {
            TrainGroupDate fixedDay = Seed.Fixed(_group, Day);

            ShouldBeSaved(await OnceAsync());

            Bookings().Single().TrainGroupDateId.ShouldBe(fixedDay.Id);
        }

        [Fact]
        public async Task A_day_of_the_month_is_booked_once()
        {
            TrainGroup group = Seed.Group(_trainer);
            TrainGroupDate fifth = Seed.Monthly(group, 5);

            ShouldBeSaved(await OnceAsync(groupId: group.Id));

            Bookings().Single().SelectedDate.ShouldBe(Day);
            Bookings().Single().TrainGroupDateId.ShouldBe(fifth.Id);
        }

        [Fact]
        public async Task A_session_the_gym_called_off_is_refused()
        {
            Seed.Cancelled(_group, Day.AddHours(5));

            (await OnceAsync()).ShouldBeBad(English.Of(TranslationKeys.This_session_has_been_cancelled_by_the_gym));
        }

        [Fact]
        public async Task A_session_called_off_on_another_day_is_not()
        {
            Seed.Cancelled(_group, Day.AddDays(7));
            Seed.Cancelled(Seed.Group(_trainer), Day);

            ShouldBeSaved(await OnceAsync());
        }

        [Fact]
        public async Task A_session_booked_already_is_refused()
        {
            Seed.OneOff(_wednesday, _member, Day);

            (await OnceAsync()).ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
        }

        [Fact]
        public async Task A_session_a_recurring_booking_covers_is_refused_even_skipped()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-7));
            (await OnceAsync()).ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));

            Seed.Skip(booking, Day);
            (await OnceAsync()).ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
        }

        [Fact]
        public async Task A_session_given_up_can_be_booked_again()
        {
            Seed.OneOff(_wednesday, _member, Day, x => x.RemovedOn = Now);
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70), Day);

            ShouldBeSaved(await OnceAsync());
        }

        [Fact]
        public async Task A_full_session_is_refused()
        {
            Taken(Day);
            ShouldBeSaved(await BookAsync(Caller.Of(_staff), isOneOff: true));

            (await OnceAsync()).ShouldBeBad(English.Of(TranslationKeys.Maximum_amount_of_participants_has_been_reached));
        }

        // Only who holds a place that day counts.
        [Fact]
        public async Task Places_that_are_free_again_are_free()
        {
            Taken(Day);
            Taken(Day.AddDays(7));
            Seed.OneOff(_wednesday, Seed.User(), Day, x => x.RemovedOn = Now);
            Seed.Skip(Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(-7)), Day);
            Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(7));
            Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(-70), Day);

            ShouldBeSaved(await OnceAsync());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task A_group_with_no_places_is_always_full(int places)
        {
            Database.Change(db => db.TrainGroups.Single().MaxParticipants = places);

            (await OnceAsync()).ShouldBeBad(English.Of(TranslationKeys.Maximum_amount_of_participants_has_been_reached));
        }

        [Fact]
        public async Task A_session_at_the_hour_of_another_groups_is_refused()
        {
            Seed.OneOff(Seed.Weekly(Seed.Group(_trainer), DayOfWeek.Wednesday), _member, Day);

            (await OnceAsync()).ShouldBeBad(English.Of(TranslationKeys.User_already_has_a_booking_for_the_same_time_and_date_on_a_different_train_group));
        }

        // Another hour, another day, another member, or given up.
        [Fact]
        public async Task A_session_that_meets_no_other_is_not()
        {
            Seed.OneOff(Seed.Weekly(Seed.Group(_trainer, x => x.StartOn = x.StartOn.AddMinutes(1)), DayOfWeek.Wednesday), _member, Day);
            TrainGroupDate sameHour = Seed.Weekly(Seed.Group(_trainer), DayOfWeek.Wednesday);
            Seed.OneOff(sameHour, _member, Day.AddDays(7));
            Seed.OneOff(sameHour, _staff, Day);
            Seed.OneOff(sameHour, _member, Day, x => x.RemovedOn = Now);
            Seed.Skip(Seed.Recurring(sameHour, _member, Day.AddDays(-7)), Day);

            ShouldBeSaved(await OnceAsync());
        }

        // The hour alone is compared, whatever date it was saved with.
        [Fact]
        public async Task The_hour_is_the_same_whatever_day_it_was_saved_on()
        {
            TrainGroup group = Seed.Group(_trainer, x => x.StartOn = new DateTime(1999, 1, 1, 18, 0, 0));
            Seed.Recurring(Seed.Weekly(group, DayOfWeek.Wednesday), _member, Day.AddDays(-7));

            (await OnceAsync()).ShouldBeBad(English.Of(TranslationKeys.User_already_has_a_booking_for_the_same_time_and_date_on_a_different_train_group));
        }


        // ================================================================
        // Every week
        // ================================================================

        [Fact]
        public async Task A_weekday_is_booked_from_its_next_day_on()
        {
            ShouldBeSaved(await BookAsync(Caller.Of(_member), recurring: [_wednesday, _friday]));

            List<TrainGroupParticipant> bookings = Bookings();
            bookings.Select(x => x.TrainGroupDateId).ShouldBe([_wednesday.Id, _friday.Id]);
            // The day that was picked, and the first Friday after it.
            bookings.Select(x => x.RecurringStartOnDate).ShouldBe([Day, Day.AddDays(2)]);
            bookings.ShouldAllBe(x => x.SelectedDate == null && x.RecurringEndOnDate == null && x.TrainGroupId == _group.Id);
            bookings.ShouldAllBe(x => x.CreatedBy_Id == _member.Id.ToString() && x.CreatedBy_FullName == "Maria Papa");
        }

        [Fact]
        public async Task A_day_of_the_month_is_booked_from_its_next_date_on()
        {
            TrainGroup group = Seed.Group(_trainer);
            TrainGroupDate fifth = Seed.Monthly(group, 5);
            TrainGroupDate last = Seed.Monthly(group, 31);

            ShouldBeSaved(await BookAsync(Caller.Of(_member), recurring: [last, fifth], groupId: group.Id));

            Bookings().Select(x => x.RecurringStartOnDate).ShouldBe([new DateTime(2031, 3, 31), Day]);
        }

        [Fact]
        public async Task A_session_and_another_weekday_are_booked_together()
        {
            ShouldBeSaved(await BookAsync(Caller.Of(_member), isOneOff: true, recurring: [_friday]));

            Bookings().Select(x => x.SelectedDate).ShouldBe([Day, null]);
        }

        [Fact]
        public async Task The_same_weekday_ticked_twice_is_booked_once()
        {
            ShouldBeSaved(await BookAsync(Caller.Of(_member), recurring: [_friday, _friday]));

            Bookings().Count.ShouldBe(1);
        }

        // Not even when that one has not begun.
        [Theory]
        [InlineData(-70)]
        [InlineData(0)]
        [InlineData(70)]
        public async Task A_weekday_booked_already_is_refused(int days)
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(days));

            (await EveryAsync(_wednesday)).ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
        }

        [Theory]
        // It ends on the day the new one starts, or before; or a moment after.
        [InlineData(0, true)]
        [InlineData(-7, true)]
        [InlineData(1, false)]
        [InlineData(7, false)]
        public async Task A_weekday_is_free_again_once_the_booking_has_ended(int days, bool isFree)
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70), Day.AddDays(days));

            ApiResult<List<DateTime>> result = await EveryAsync(_wednesday);

            if (isFree)
                ShouldBeSaved(result);
            else
                result.ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
        }

        [Fact]
        public async Task The_time_of_day_a_booking_ends_at_does_not_matter()
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70), Day.AddHours(20));

            ShouldBeSaved(await EveryAsync(_wednesday));
        }

        [Fact]
        public async Task Another_weekday_of_the_group_is_free()
        {
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70));

            ShouldBeSaved(await EveryAsync(_friday));
        }

        // Sessions of the weekday booked one by one from there on are now covered, and given up for
        // it; the ones before, of another day, or given up already stay.
        [Fact]
        public async Task The_members_own_single_sessions_are_taken_in()
        {
            TrainGroupParticipant covered = Seed.OneOff(_wednesday, _member, Day);
            TrainGroupParticipant coveredLater = Seed.OneOff(_wednesday, _member, Day.AddDays(21).AddHours(5));
            TrainGroupParticipant before = Seed.OneOff(_wednesday, _member, Day.AddDays(-7));
            TrainGroupParticipant friday = Seed.OneOff(_friday, _member, Day.AddDays(2));
            TrainGroupParticipant givenUp = Seed.OneOff(_wednesday, _member, Day.AddDays(7), x => { x.RemovedOn = Now; x.RemovedBy_FullName = "before"; });
            TrainGroupParticipant somebodyElses = Seed.OneOff(_wednesday, _staff, Day);
            DateTime start = DateTime.UtcNow;

            ShouldBeSaved(await EveryAsync(_wednesday));

            List<TrainGroupParticipant> after = Database.Read(db => db.TrainGroupParticipants.ToList());
            foreach (TrainGroupParticipant booking in new[] { covered, coveredLater })
            {
                TrainGroupParticipant now = after.Single(x => x.Id == booking.Id);
                now.RemovedOn.ShouldNotBeNull().ShouldBeInRange(start, DateTime.UtcNow);
                now.RemovedBy_Id.ShouldBe(_member.Id.ToString());
                now.RemovedBy_FullName.ShouldBe("Maria Papa");
            }

            foreach (TrainGroupParticipant booking in new[] { before, friday, somebodyElses })
                after.Single(x => x.Id == booking.Id).RemovedOn.ShouldBeNull();

            after.Single(x => x.Id == givenUp.Id).RemovedBy_FullName.ShouldBe("before");

            TrainGroupParticipant added = after.OrderBy(x => x.Id).Last();
            Emails.Lines.ShouldBe([$"booking maria@gym.test: +{added.Id} -{covered.Id},{coveredLater.Id}"]);
        }


        // ================================================================
        // Around the full dates
        // ================================================================

        // Each full date becomes a skipped one the member can take back later, and is named in the answer.
        [Fact]
        public async Task A_full_date_is_skipped_and_named()
        {
            Taken(Day.AddDays(7));
            Taken(Day.AddDays(7));
            Taken(Day.AddDays(21));
            Taken(Day.AddDays(21));
            Taken(Day.AddDays(28));

            ShouldBeSaved(await EveryAsync(_wednesday), "2031-03-12", "2031-03-26");

            TrainGroupParticipant booking = Bookings().Single();
            SkipsOf(booking).ShouldBe(["2031-03-12", "2031-03-26"]);
            booking.TrainGroupParticipantUnavailableDates.ShouldAllBe(x => x.CreatedBy_Id == _member.Id.ToString() && x.CreatedBy_FullName == "Maria Papa");
        }

        [Fact]
        public async Task The_first_date_may_be_the_full_one()
        {
            Taken(Day);
            Taken(Day);

            ShouldBeSaved(await EveryAsync(_wednesday), "2031-03-05");

            Bookings().Single().RecurringStartOnDate.ShouldBe(Day);
        }

        [Fact]
        public async Task The_full_dates_of_both_weekdays_are_named_in_order()
        {
            Taken(Day.AddDays(7));
            Taken(Day.AddDays(7));
            Taken(Day.AddDays(2));
            Taken(Day.AddDays(2));

            ShouldBeSaved(await BookAsync(Caller.Of(_member), recurring: [_wednesday, _friday]), "2031-03-07", "2031-03-12");

            SkipsOf(Bookings()[0]).ShouldBe(["2031-03-12"]);
            SkipsOf(Bookings()[1]).ShouldBe(["2031-03-07"]);
        }

        // Two of every week, with no end: there is no date left to come to.
        [Fact]
        public async Task A_weekday_full_for_good_is_refused()
        {
            Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(-70));
            Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(14));

            (await EveryAsync(_wednesday)).ShouldBeBad(English.Of(TranslationKeys.This_session_is_full_on_every_upcoming_date));

            Bookings().ShouldBeEmpty();
        }

        // Full until one of them ends, and free after it.
        [Fact]
        public async Task A_weekday_full_until_somebody_leaves_is_booked_around_it()
        {
            Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(-70));
            Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(-70), Day.AddDays(15));

            ShouldBeSaved(await EveryAsync(_wednesday), "2031-03-05", "2031-03-12", "2031-03-19");

            SkipsOf(Bookings().Single()).ShouldBe(["2031-03-05", "2031-03-12", "2031-03-19"]);
        }

        // A date somebody else skips is free for the member.
        [Fact]
        public async Task A_date_somebody_skips_is_a_place()
        {
            Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(-70));
            Seed.Skip(Seed.Recurring(_wednesday, Seed.User(), Day.AddDays(-70), Day.AddDays(15)), Day.AddDays(7));

            ShouldBeSaved(await EveryAsync(_wednesday), "2031-03-05", "2031-03-19");
        }

        // The gym is closed that day, so there is nothing to have missed.
        [Fact]
        public async Task A_full_date_the_gym_called_off_is_not_skipped()
        {
            Taken(Day.AddDays(7));
            Taken(Day.AddDays(7));
            Seed.Cancelled(_group, Day.AddDays(7));

            ShouldBeSaved(await EveryAsync(_wednesday));

            SkipsOf(Bookings().Single()).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_date_the_gym_called_off_does_not_stop_a_recurring_booking()
        {
            Seed.Cancelled(_group, Day);

            ShouldBeSaved(await EveryAsync(_wednesday));
        }

        // A single session already counted among the places makes room for the booking it becomes.
        [Fact]
        public async Task The_members_own_session_does_not_make_its_date_full()
        {
            Taken(Day);
            Seed.OneOff(_wednesday, _member, Day);

            ShouldBeSaved(await EveryAsync(_wednesday));

            SkipsOf(Bookings().Last()).ShouldBeEmpty();
        }

        // What the loop has to look past: two hundred dates apart.
        [Fact]
        public async Task A_full_date_years_ahead_is_found()
        {
            DateTime far = Day.AddDays(7 * 200);
            Taken(far);
            Taken(far);

            ShouldBeSaved(await EveryAsync(_wednesday), far.ToString("yyyy-MM-dd"));
        }

        // A thousand dates are looked at and no more, so one after them is not found.
        [Fact]
        public async Task A_full_date_after_a_thousand_is_not_looked_at()
        {
            DateTime last = Day.AddDays(7 * 999);
            DateTime beyond = Day.AddDays(7 * 1000);
            foreach (DateTime day in new[] { last, last, beyond, beyond })
                Taken(day);

            ShouldBeSaved(await EveryAsync(_wednesday), last.ToString("yyyy-MM-dd"));
        }


        // A weekday and a day of the month of one group that fall on one date: what is booked first takes
        // the place, and the other one goes around it.
        [Fact]
        public async Task Two_days_on_one_date_take_a_place_each()
        {
            Database.Change(db => db.TrainGroups.Single(x => x.Id == _group.Id).MaxParticipants = 1);
            TrainGroupDate fifth = Seed.Monthly(_group, 5);

            ShouldBeSaved(await BookAsync(Caller.Of(_member), recurring: [_wednesday, fifth]), "2031-03-05");

            Bookings().Select(x => $"{x.TrainGroupDateId == fifth.Id} {string.Join(" ", SkipsOf(x))}").ShouldBe(["False ", "True 2031-03-05"]);
        }

        // A single session does not take one from what is booked along with it: the member is in the
        // session twice, and the group is over its size.
        [Fact]
        public async Task A_single_session_takes_no_place_from_a_day_on_the_same_date()
        {
            Database.Change(db => db.TrainGroups.Single(x => x.Id == _group.Id).MaxParticipants = 1);
            TrainGroupDate fifth = Seed.Monthly(_group, 5);

            ShouldBeSaved(await BookAsync(Caller.Of(_member), isOneOff: true, recurring: [fifth]));

            Bookings().Count.ShouldBe(2);
            Bookings().ShouldAllBe(x => x.TrainGroupParticipantUnavailableDates.Count == 0);
        }


        // ================================================================
        // At the hour of another group
        // ================================================================

        // On any date there is to come, the first or a later one.
        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        [InlineData(70)]
        public async Task A_weekday_that_meets_another_booking_is_refused(int days)
        {
            Seed.OneOff(Seed.Weekly(Seed.Group(_trainer), DayOfWeek.Wednesday), _member, Day.AddDays(days));

            (await EveryAsync(_wednesday)).ShouldBeBad(English.Of(TranslationKeys.User_already_has_a_booking_for_the_same_time_and_date_on_a_different_train_group));

            Bookings().Count.ShouldBe(1);
        }

        [Fact]
        public async Task A_weekday_that_meets_another_recurring_booking_is_refused()
        {
            Seed.Recurring(Seed.Weekly(Seed.Group(_trainer), DayOfWeek.Wednesday), _member, Day.AddDays(49));

            (await EveryAsync(_wednesday)).ShouldBeBad(English.Of(TranslationKeys.User_already_has_a_booking_for_the_same_time_and_date_on_a_different_train_group));
        }

        [Fact]
        public async Task A_weekday_meets_no_booking_that_is_over()
        {
            TrainGroupDate elsewhere = Seed.Weekly(Seed.Group(_trainer), DayOfWeek.Wednesday);
            Seed.Recurring(elsewhere, _member, Day.AddDays(-70), Day);
            Seed.OneOff(elsewhere, _member, Day.AddDays(-7));
            Seed.OneOff(elsewhere, _member, Day.AddDays(7), x => x.RemovedOn = Now);

            ShouldBeSaved(await EveryAsync(_wednesday));
        }

        [Fact]
        public async Task A_weekday_of_the_month_meets_a_weekday_of_the_week()
        {
            TrainGroup monthly = Seed.Group(_trainer);
            TrainGroupDate fifth = Seed.Monthly(monthly, 5);
            Seed.Recurring(_wednesday, _member, Day.AddDays(-70));

            (await BookAsync(Caller.Of(_member), recurring: [fifth], groupId: monthly.Id)).ShouldBeBad(English.Of(TranslationKeys.User_already_has_a_booking_for_the_same_time_and_date_on_a_different_train_group));
        }


        // ================================================================
        // All or nothing
        // ================================================================

        [Fact]
        public async Task When_one_will_not_do_none_is_booked_and_nothing_is_given_up()
        {
            TrainGroupParticipant kept = Seed.OneOff(_wednesday, _member, Day.AddDays(7));
            Seed.Recurring(_friday, _member, Day.AddDays(-70));

            (await BookAsync(Caller.Of(_member), recurring: [_wednesday, _friday])).ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));

            Bookings().Count.ShouldBe(2);
            Bookings().Single(x => x.Id == kept.Id).RemovedOn.ShouldBeNull();
            Emails.Lines.ShouldBeEmpty();
        }


        // ================================================================
        // The mail
        // ================================================================

        [Fact]
        public async Task The_member_is_sent_what_was_booked_with_everything_the_mail_shows()
        {
            ShouldBeSaved(await BookAsync(Caller.Of(_member), isOneOff: true, recurring: [_friday]));

            List<TrainGroupParticipant> bookings = Bookings();
            Emails.Lines.ShouldBe([$"booking maria@gym.test: +{bookings[0].Id},{bookings[1].Id} -"]);
            Emails.Added.ShouldAllBe(x => x.TrainGroup.Title == _group.Title && x.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK);
        }

        [Fact]
        public async Task A_mail_that_fails_does_not_undo_the_booking()
        {
            Emails.Failure = new InvalidOperationException("No.");

            ShouldBeSaved(await OnceAsync());

            Bookings().Count.ShouldBe(1);
        }
    }
}
