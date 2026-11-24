using API.Tests.Host;
using Core.Models;
using Core.Translations;

namespace API.Tests.Controllers.TrainGroupParticipants
{
    /// <summary>
    /// Ending a recurring booking from a date on. Nothing is deleted: the row stays with its end.
    /// </summary>
    public sealed class EndTests : ApiTest
    {
        // A Wednesday. The session is at six in the evening.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Now = Day.AddDays(-2).AddHours(10);

        private readonly User _member;
        private readonly User _staff;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;
        private readonly TrainGroupParticipant _booking;

        public EndTests(ApiFixture api) : base(api)
        {
            _member = Seed.User(x => { x.Email = "maria@gym.test"; x.FirstName = "Maria"; x.LastName = "Papa"; });
            _staff = Seed.User(x => { x.FirstName = "Desk"; x.LastName = "Person"; });
            _group = Seed.Group(Seed.User());
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            _booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-70));
        }

        private Task<ApiResult<bool>> EndAsync(Caller caller, DateTime? from, DateTime? now = null, bool isAdminPage = false, int? id = null)
        {
            return PostAsync<bool>(caller, $"api/TrainGroupParticipants/{id ?? _booking.Id}/End", new { fromDate = from, clientTimezoneOffsetMinutes = OffsetFor(now ?? Now), isAdminPage });
        }

        private TrainGroupParticipant Read()
        {
            return Database.Read(db => db.TrainGroupParticipants.Single(x => x.Id == _booking.Id));
        }

        private void ShouldBeEnded(ApiResult<bool> result, DateTime end)
        {
            result.ShouldSucceed(English.Of(TranslationKeys.Booking_removed));
            result.Data.ShouldBeTrue();
            Read().RecurringEndOnDate.ShouldBe(end);
            Read().RemovedOn.ShouldNotBeNull();
        }

        private void ShouldBeRefused(ApiResult<bool> result, string error)
        {
            result.ShouldBeBad(English.Of(error));
            Read().RemovedOn.ShouldBeNull();
        }


        // ================================================================
        // Who may
        // ================================================================

        [Fact]
        public async Task A_member_ends_their_booking_from_a_date_on()
        {
            DateTime before = DateTime.UtcNow;

            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day), Day);

            TrainGroupParticipant booking = Read();
            booking.RemovedOn.ShouldNotBeNull().ShouldBeInRange(before, DateTime.UtcNow);
            booking.RemovedBy_Id.ShouldBe(_member.Id.ToString());
            booking.RemovedBy_FullName.ShouldBe("Maria Papa");
            booking.RecurringStartOnDate.ShouldBe(Day.AddDays(-70));
        }

        [Fact]
        public async Task Staff_end_anybodys()
        {
            ShouldBeEnded(await EndAsync(Staff(_staff), Day), Day);

            Read().RemovedBy_FullName.ShouldBe("Desk Person");
        }

        [Fact]
        public async Task Somebody_elses_cannot_be_ended()
        {
            (await EndAsync(Caller.Of(_staff, "TrainGroupParticipants_Delete"), Day)).ShouldBeBad(English.NotAuthorized);
            (await EndAsync(Caller.Of(_staff, "TrainGroupParticipants_Delete"), Day, isAdminPage: true)).ShouldBeBad(English.NotAuthorized);
            (await EndAsync(Caller.WithNoId(), Day)).ShouldBeBad(English.NotAuthorized);

            Read().RemovedOn.ShouldBeNull();
        }

        [Fact]
        public async Task Staff_with_no_id_end_it_namelessly()
        {
            ShouldBeEnded(await EndAsync(Caller.WithNoId("TrainGroupParticipants_View"), Day), Day);

            Read().RemovedBy_Id.ShouldBe("");
            Read().RemovedBy_FullName.ShouldBe("");
        }

        [Fact]
        public async Task A_booking_that_is_not_there_is_not_found()
        {
            (await EndAsync(Caller.Of(_member), Day, id: 99)).ShouldBeBad(English.NotFound("TrainGroupParticipant"));
        }

        [Fact]
        public async Task A_single_session_is_not_ended_but_cancelled()
        {
            (await EndAsync(Caller.Of(_member), Day, id: Seed.OneOff(_wednesday, _member, Day).Id)).ShouldBeBad(English.Invalid);
            (await EndAsync(Caller.Of(_member), Day, id: Seed.Participant(Seed.Fixed(_group, Day), _member).Id)).ShouldBeBad(English.Invalid);
        }


        // ================================================================
        // From when
        // ================================================================

        [Fact]
        public async Task With_no_date_it_ends_today_where_the_member_is()
        {
            ShouldBeEnded(await EndAsync(Caller.Of(_member), null), Now.Date);
        }

        [Fact]
        public async Task The_time_of_the_day_does_not_matter()
        {
            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day.AddHours(23)), Day);
        }

        [Fact]
        public async Task It_may_end_in_the_future()
        {
            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day.AddDays(70)), Day.AddDays(70));
        }

        // Ended already by then; or brought forward.
        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(1, true)]
        [InlineData(700, true)]
        public async Task A_booking_that_has_ended_by_then_is_refused(int days, bool isEnded)
        {
            Database.Change(db => db.TrainGroupParticipants.Single().RecurringEndOnDate = Day.AddDays(days).AddHours(5));

            foreach (bool isAdminPage in new[] { false, true })
            {
                ApiResult<bool> result = await EndAsync(Staff(_staff), Day, isAdminPage: isAdminPage);

                if (!isEnded)
                {
                    ShouldBeRefused(result, TranslationKeys.This_booking_has_already_ended);
                    continue;
                }

                ShouldBeEnded(result, Day);
                break;
            }
        }


        // ================================================================
        // How late
        // ================================================================

        // Minutes from the first session that would be lost, for a member, for staff on the members'
        // page, and on the admin's.
        [Theory]
        [InlineData(-24 * 60, null)]
        [InlineData(-12 * 60 - 2, null)]
        [InlineData(-12 * 60, TranslationKeys.Cannot_remove_a_session_starting_within_12_hours)]
        [InlineData(-2, TranslationKeys.Cannot_remove_a_session_starting_within_12_hours)]
        [InlineData(0, TranslationKeys.This_session_has_already_started)]
        [InlineData(5 * 60, TranslationKeys.This_session_has_already_started)]
        public async Task The_first_session_given_up_has_to_be_more_than_twelve_hours_off(int minutesFromStart, string? error)
        {
            DateTime now = Day.AddHours(18).AddMinutes(minutesFromStart);

            foreach ((Caller caller, bool isAdminPage, bool isBoundByTheWindow) in new[]
            {
                (Caller.Of(_member), false, true),
                (Caller.Of(_member), true, true),
                (Staff(_staff), false, true),
                (Staff(_staff), true, false)
            })
            {
                Database.Execute("UPDATE TrainGroupParticipants SET RecurringEndOnDate = NULL, RemovedOn = NULL");

                ApiResult<bool> result = await EndAsync(caller, Day, now, isAdminPage);

                if (error == null || !isBoundByTheWindow)
                    ShouldBeEnded(result, Day);
                else
                    ShouldBeRefused(result, error);
            }
        }

        // From the day after a session, the next one is a week off.
        [Fact]
        public async Task The_session_of_today_may_be_kept()
        {
            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day.AddDays(1), Day.AddHours(17)), Day.AddDays(1));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(-1, false)]
        [InlineData(-70, false)]
        public async Task A_member_cannot_end_it_in_the_past(int days, bool isEnded)
        {
            DateTime monday = Day.AddDays(-2);

            ApiResult<bool> result = await EndAsync(Caller.Of(_member), monday.AddDays(days), monday.AddHours(10));

            if (isEnded)
                ShouldBeEnded(result, monday);
            else
                ShouldBeRefused(result, TranslationKeys.This_session_has_already_started);
        }

        [Fact]
        public async Task Staff_can_end_it_in_the_past()
        {
            ShouldBeEnded(await EndAsync(Staff(_staff), Day.AddDays(-21), isAdminPage: true), Day.AddDays(-21));
        }


        // ================================================================
        // The first session that is lost
        // ================================================================

        // One already skipped is no loss, so the one after it is what counts.
        [Fact]
        public async Task A_skipped_session_is_not_lost()
        {
            Seed.Skip(_booking, Day);

            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day, Day.AddHours(17)), Day);
        }

        // Nothing is left of it but skipped dates.
        [Fact]
        public async Task With_no_session_left_there_is_nothing_to_be_late_for()
        {
            Database.Change(db => db.TrainGroupParticipants.Single().RecurringEndOnDate = Day.AddDays(1));
            Seed.Skip(_booking, Day);

            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day, Day.AddHours(17)), Day);
        }

        // The end it has already comes before the next session.
        [Fact]
        public async Task A_session_after_its_end_is_not_lost()
        {
            Database.Change(db => db.TrainGroupParticipants.Single().RecurringEndOnDate = Day);

            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day.AddDays(-1), Day.AddDays(-1).AddHours(1)), Day.AddDays(-1));
        }

        [Fact]
        public async Task A_booking_that_has_not_begun_loses_its_first_session()
        {
            Database.Change(db => db.TrainGroupParticipants.Single().RecurringStartOnDate = Day.AddDays(7));

            // Within hours of a session that is not theirs yet.
            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day, Day.AddHours(17)), Day);
        }

        // Four hundred sessions are looked at. One that begins after them is as if there were none.
        [Fact]
        public async Task A_booking_that_begins_years_ahead_loses_nothing()
        {
            Database.Change(db => db.TrainGroupParticipants.Single().RecurringStartOnDate = Day.AddDays(7 * 500));

            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day, Day.AddHours(17)), Day);
        }

        [Fact]
        public async Task A_booking_on_a_day_that_never_comes_loses_nothing()
        {
            Database.Change(db => { TrainGroupDate date = db.TrainGroupDates.Single(); date.TrainGroupDateType = Core.Enums.TrainGroupDateTypeEnum.DAY_OF_MONTH; date.RecurrenceDayOfWeek = null; date.RecurrenceDayOfMonth = 40; });

            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day, Day.AddHours(17)), Day);
        }

        // The last of the four hundred still counts.
        [Fact]
        public async Task The_four_hundredth_session_is_looked_at()
        {
            DateTime last = Day.AddDays(7 * 399);
            Database.Change(db => db.TrainGroupParticipants.Single().RecurringStartOnDate = last);

            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day), Day);
        }


        // ================================================================
        // The mail
        // ================================================================

        [Fact]
        public async Task The_member_is_sent_what_was_given_up()
        {
            await EndAsync(Staff(_staff), Day);

            Emails.Lines.ShouldBe([$"booking maria@gym.test: + -{_booking.Id}"]);
            Emails.Removed.Single().TrainGroup.Title.ShouldBe(_group.Title);
            Emails.Removed.Single().TrainGroupDate.RecurrenceDayOfWeek.ShouldBe(DayOfWeek.Wednesday);
        }

        [Fact]
        public async Task A_mail_that_fails_does_not_undo_it()
        {
            Emails.Failure = new InvalidOperationException("No.");

            ShouldBeEnded(await EndAsync(Caller.Of(_member), Day), Day);
        }

        [Fact]
        public async Task What_is_refused_sends_no_mail()
        {
            await EndAsync(Caller.Of(_member), Day, Day.AddHours(17));

            Emails.Lines.ShouldBeEmpty();
        }
    }
}
