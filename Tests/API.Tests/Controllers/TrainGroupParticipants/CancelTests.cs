using API.Tests.Host;
using Core.Models;
using Core.Translations;

namespace API.Tests.Controllers.TrainGroupParticipants
{
    /// <summary>
    /// Giving up a single session: a one-off, or the group's fixed day.
    /// </summary>
    public sealed class CancelTests : ApiTest
    {
        // A Wednesday. The session is at six in the evening.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Now = Day.AddDays(-2).AddHours(10);

        private readonly User _member;
        private readonly User _staff;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;
        private readonly TrainGroupParticipant _booking;

        public CancelTests(ApiFixture api) : base(api)
        {
            _member = Seed.User(x => { x.Email = "maria@gym.test"; x.FirstName = "Maria"; x.LastName = "Papa"; });
            _staff = Seed.User(x => { x.FirstName = "Desk"; x.LastName = "Person"; });
            _group = Seed.Group(Seed.User());
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            _booking = Seed.OneOff(_wednesday, _member, Day);
        }

        private Task<ApiResult<bool>> CancelAsync(Caller caller, DateTime? now = null, bool isAdminPage = false, int? id = null)
        {
            return PostAsync<bool>(caller, $"api/TrainGroupParticipants/{id ?? _booking.Id}/Cancel", new { clientTimezoneOffsetMinutes = OffsetFor(now ?? Now), isAdminPage });
        }

        private TrainGroupParticipant Read(int? id = null)
        {
            return Database.Read(db => db.TrainGroupParticipants.Single(x => x.Id == (id ?? _booking.Id)));
        }

        private void ShouldBeCancelled(ApiResult<bool> result, int? id = null)
        {
            result.ShouldSucceed(English.Of(TranslationKeys.Booking_removed));
            result.Data.ShouldBeTrue();
            Read(id).RemovedOn.ShouldNotBeNull();
        }


        // ================================================================
        // Who may
        // ================================================================

        [Fact]
        public async Task A_member_gives_their_session_up()
        {
            DateTime before = DateTime.UtcNow;

            ShouldBeCancelled(await CancelAsync(Caller.Of(_member)));

            TrainGroupParticipant booking = Read();
            booking.RemovedOn.ShouldNotBeNull().ShouldBeInRange(before, DateTime.UtcNow);
            booking.RemovedBy_Id.ShouldBe(_member.Id.ToString());
            booking.RemovedBy_FullName.ShouldBe("Maria Papa");
            booking.SelectedDate.ShouldBe(Day);
            booking.RecurringEndOnDate.ShouldBeNull();
        }

        [Fact]
        public async Task Staff_give_anybodys_up()
        {
            ShouldBeCancelled(await CancelAsync(Staff(_staff)));

            Read().RemovedBy_FullName.ShouldBe("Desk Person");
        }

        [Fact]
        public async Task Somebody_elses_cannot_be_given_up()
        {
            (await CancelAsync(Caller.Of(_staff, "TrainGroupParticipants_Delete"))).ShouldBeBad(English.NotAuthorized);
            (await CancelAsync(Caller.Of(_staff), isAdminPage: true)).ShouldBeBad(English.NotAuthorized);
            (await CancelAsync(Caller.WithNoId())).ShouldBeBad(English.NotAuthorized);

            Read().RemovedOn.ShouldBeNull();
        }

        [Fact]
        public async Task A_booking_that_is_not_there_is_not_found()
        {
            (await CancelAsync(Caller.Of(_member), id: 99)).ShouldBeBad(English.NotFound("TrainGroupParticipant"));
        }

        [Fact]
        public async Task A_recurring_booking_is_not_cancelled_but_ended()
        {
            (await CancelAsync(Caller.Of(_member), id: Seed.Recurring(_wednesday, _member, Day).Id)).ShouldBeBad(English.Invalid);
        }

        [Fact]
        public async Task A_session_given_up_already_is_refused()
        {
            Database.Change(db => { TrainGroupParticipant booking = db.TrainGroupParticipants.Single(); booking.RemovedOn = Now; booking.RemovedBy_FullName = "before"; });

            (await CancelAsync(Caller.Of(_member))).ShouldBeBad(English.Of(TranslationKeys.This_booking_is_already_cancelled));
            (await CancelAsync(Staff(_staff), isAdminPage: true)).ShouldBeBad(English.Of(TranslationKeys.This_booking_is_already_cancelled));

            Read().RemovedBy_FullName.ShouldBe("before");
        }


        // ================================================================
        // How late
        // ================================================================

        [Theory]
        [InlineData(-24 * 60, null)]
        [InlineData(-12 * 60 - 2, null)]
        [InlineData(-12 * 60, TranslationKeys.Cannot_remove_a_session_starting_within_12_hours)]
        [InlineData(-2, TranslationKeys.Cannot_remove_a_session_starting_within_12_hours)]
        [InlineData(0, TranslationKeys.This_session_has_already_started)]
        [InlineData(60 * 24 * 30, TranslationKeys.This_session_has_already_started)]
        public async Task A_member_has_until_twelve_hours_before(int minutesFromStart, string? error)
        {
            DateTime now = Day.AddHours(18).AddMinutes(minutesFromStart);
            TrainGroupParticipant fixedDay = Seed.Participant(Seed.Fixed(_group, Day.AddHours(7)), _member);

            foreach (int id in new[] { _booking.Id, fixedDay.Id })
                foreach ((Caller caller, bool isAdminPage, bool isBoundByTheWindow) in new[]
                {
                    (Caller.Of(_member), false, true),
                    (Caller.Of(_member), true, true),
                    (Staff(_staff), false, true),
                    (Staff(_staff), true, false)
                })
                {
                    Database.Execute("UPDATE TrainGroupParticipants SET RemovedOn = NULL");

                    ApiResult<bool> result = await CancelAsync(caller, now, isAdminPage, id);

                    if (error == null || !isBoundByTheWindow)
                        ShouldBeCancelled(result, id);
                    else
                        result.ShouldBeBad(English.Of(error));
                }
        }

        // A fixed day that names no date was a very long time ago.
        [Fact]
        public async Task A_session_on_no_day_is_over()
        {
            TrainGroupParticipant never = Seed.Participant(Seed.Fixed(_group, null), _member);

            (await CancelAsync(Caller.Of(_member), id: never.Id)).ShouldBeBad(English.Of(TranslationKeys.This_session_has_already_started));
            ShouldBeCancelled(await CancelAsync(Staff(_staff), isAdminPage: true, id: never.Id), never.Id);
        }


        // ================================================================
        // The mail
        // ================================================================

        [Fact]
        public async Task The_member_is_sent_what_was_given_up()
        {
            await CancelAsync(Staff(_staff));

            Emails.Lines.ShouldBe([$"booking maria@gym.test: + -{_booking.Id}"]);
            Emails.Removed.Single().TrainGroup.Title.ShouldBe(_group.Title);
        }

        [Fact]
        public async Task A_mail_that_fails_does_not_undo_it()
        {
            Emails.Failure = new InvalidOperationException("No.");

            ShouldBeCancelled(await CancelAsync(Caller.Of(_member)));
        }

        // A member who goes takes their bookings along. Were one left, there would be nobody to write to.
        [Fact]
        public async Task Somebody_who_is_gone_is_not_written_to()
        {
            Database.Execute("PRAGMA foreign_keys = OFF; DELETE FROM AspNetUsers WHERE Email = 'maria@gym.test'; PRAGMA foreign_keys = ON;");

            ShouldBeCancelled(await CancelAsync(Staff(_staff)));

            Emails.Lines.ShouldBeEmpty();
        }
    }
}
