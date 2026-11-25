using API.Tests.Host;
using Core.Models;
using Core.Translations;

namespace API.Tests.Controllers.TrainGroupParticipants
{
    /// <summary>
    /// What time it is. A session is at an hour of the gym's, in Athens, so the clock there says whether
    /// it has started and whether it is less than twelve hours off - the server's is two or three hours
    /// behind it, and whatever the browser says is passed over.
    /// </summary>
    public sealed class GymClockTests : ApiTest
    {
        // Two Wednesdays. The session is at six in the evening: four in the afternoon in UTC in winter,
        // three in summer.
        private static readonly DateTime Winter = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Summer = new DateTime(2031, 7, 2, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _member;
        private readonly User _staff;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;

        public GymClockTests(ApiFixture api) : base(api)
        {
            _member = Seed.User();
            _staff = Seed.User();
            _group = Seed.Group(Seed.User());
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
        }

        private static DateTime DayOf(bool isSummer)
        {
            return isSummer ? Summer : Winter;
        }

        // What each of them sends, and whatever else it says.
        private Task<ApiResult<List<DateTime>>> BookAsync(DateTime day, object? clientTimezoneOffsetMinutes = null)
        {
            return PostAsync<List<DateTime>>(Caller.Of(_member), "api/TrainGroupParticipants/Book", new
            {
                trainGroupId = _group.Id,
                selectedDate = day,
                isOneOff = true,
                recurringTrainGroupDateIds = Array.Empty<int>(),
                clientTimezoneOffsetMinutes
            });
        }

        private Task<ApiResult<bool>> CancelAsync(DateTime day, object? clientTimezoneOffsetMinutes = null)
        {
            return PostAsync<bool>(Caller.Of(_member), $"api/TrainGroupParticipants/{Seed.OneOff(_wednesday, _member, day).Id}/Cancel", new { clientTimezoneOffsetMinutes });
        }

        private Task<ApiResult<bool>> EndAsync(DateTime day, object? clientTimezoneOffsetMinutes = null)
        {
            return PostAsync<bool>(Caller.Of(_member), $"api/TrainGroupParticipants/{Seed.Recurring(_wednesday, _member, day).Id}/End", new { fromDate = day, clientTimezoneOffsetMinutes });
        }

        private Task<ApiResult<List<TrainGroupParticipantUnavailableDate>>> SkipAsync(DateTime day, object? clientTimezoneOffsetMinutes = null)
        {
            return PostAsync<List<TrainGroupParticipantUnavailableDate>>(Caller.Of(_member), "api/TrainGroupParticipantUnavailableDates", new[]
            {
                new { trainGroupParticipantId = Seed.Recurring(_wednesday, _member, day).Id, unavailableDate = day, clientTimezoneOffsetMinutes }
            });
        }

        private Task<ApiResult<TrainGroupParticipantUnavailableDate>> RejoinAsync(DateTime day)
        {
            return DeleteAsync<TrainGroupParticipantUnavailableDate>(Caller.Of(_member), $"api/TrainGroupParticipantUnavailableDates/{Seed.Skip(Seed.Recurring(_wednesday, _member, day), day).Id}");
        }

        private async Task<DateTime?> NextSessionAsync(DateTime day)
        {
            Seed.OneOff(_wednesday, _member, day);

            return (await PostAsync<List<TrainGroupParticipantBookingDto>>(Caller.Of(_member), "api/TrainGroupParticipants/Bookings", new { })).Value.Single().NextSessionDate;
        }


        // ================================================================
        // Twelve hours before
        // ================================================================

        // Hours and minutes in UTC, on the day of the session.
        [Theory]
        [InlineData(false, 3, 59, true)]
        [InlineData(false, 4, 0, false)]
        [InlineData(true, 2, 59, true)]
        [InlineData(true, 3, 0, false)]
        public async Task A_session_is_given_up_by_six_in_the_morning_at_the_gym(bool isSummer, int hour, int minute, bool isInTime)
        {
            DateTime day = DayOf(isSummer);
            Clock.Set(day.AddHours(hour).AddMinutes(minute));

            (await CancelAsync(day)).IsSucceed.ShouldBe(isInTime);
            (await EndAsync(day)).IsSucceed.ShouldBe(isInTime);
            (await SkipAsync(day)).IsSucceed.ShouldBe(isInTime);
        }


        // ================================================================
        // The start
        // ================================================================

        [Theory]
        [InlineData(false, 15, 59, true)]
        [InlineData(false, 16, 0, false)]
        [InlineData(true, 14, 59, true)]
        [InlineData(true, 15, 0, false)]
        public async Task A_session_starts_at_six_in_the_evening_at_the_gym(bool isSummer, int hour, int minute, bool isInTime)
        {
            DateTime day = DayOf(isSummer);
            Clock.Set(day.AddHours(hour).AddMinutes(minute));

            (await BookAsync(day)).IsSucceed.ShouldBe(isInTime);
            (await RejoinAsync(day)).IsSucceed.ShouldBe(isInTime);

            Database.Execute("DELETE FROM TrainGroupParticipants");
            (await NextSessionAsync(day)).ShouldBe(isInTime ? day : null);
        }


        // ================================================================
        // Today
        // ================================================================

        // After midnight in Athens it is still the day before in UTC.
        [Theory]
        [InlineData(21, 59, 5)]
        [InlineData(22, 0, 6)]
        [InlineData(23, 59, 6)]
        public async Task The_day_begins_at_midnight_at_the_gym(int hour, int minute, int dayOfMarch)
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Winter.AddDays(-70));
            Clock.Set(Winter.AddHours(hour).AddMinutes(minute));

            (await PostAsync<bool>(Caller.Of(_member), $"api/TrainGroupParticipants/{booking.Id}/End", new { })).IsSucceed.ShouldBe(dayOfMarch == 6);
            (await PostAsync<bool>(Staff(_staff), $"api/TrainGroupParticipants/{booking.Id}/End", new { isAdminPage = true })).IsSucceed.ShouldBe(dayOfMarch == 5);

            Database.Read(db => db.TrainGroupParticipants.Single()).RecurringEndOnDate.ShouldBe(new DateTime(2031, 3, dayOfMarch));
        }


        // ================================================================
        // What a request says
        // ================================================================

        // How far the browser is off UTC is what pages used to send, and the time was worked out from it:
        // any number would do to change a session at the last minute, or one that was over.
        [Theory]
        [InlineData(-120)]
        [InlineData(0)]
        [InlineData(840)]
        [InlineData(100_000)]
        [InlineData(-100_000)]
        [InlineData(int.MinValue)]
        [InlineData("840")]
        public async Task What_a_request_says_of_the_time_does_not_put_it_back(object offset)
        {
            AtTheGym(Winter.AddHours(17));

            (await CancelAsync(Winter, offset)).ShouldBeBad(English.Of(TranslationKeys.Cannot_remove_a_session_starting_within_12_hours));
            (await EndAsync(Winter, offset)).ShouldBeBad(English.Of(TranslationKeys.Cannot_remove_a_session_starting_within_12_hours));
            (await SkipAsync(Winter, offset)).ShouldBeBad(English.Of(TranslationKeys.Cannot_remove_a_session_starting_within_12_hours));

            AtTheGym(Winter.AddHours(19));

            (await CancelAsync(Winter, offset)).ShouldBeBad(English.Of(TranslationKeys.This_session_has_already_started));
            (await EndAsync(Winter, offset)).ShouldBeBad(English.Of(TranslationKeys.This_session_has_already_started));
            (await SkipAsync(Winter, offset)).ShouldBeBad(English.Of(TranslationKeys.This_session_has_already_started));
            (await BookAsync(Winter, offset)).ShouldBeBad(English.Of(TranslationKeys.This_session_has_already_started));
        }

        // A page that was open before the release goes on sending it, and goes on working.
        [Theory]
        [InlineData(-120)]
        [InlineData(0)]
        [InlineData(840)]
        [InlineData(100_000)]
        [InlineData(-100_000)]
        [InlineData(int.MinValue)]
        [InlineData("840")]
        public async Task Nor_does_it_put_it_on(object offset)
        {
            AtTheGym(Winter.AddDays(-1));

            (await BookAsync(Winter, offset)).ShouldSucceed(English.Of(TranslationKeys.Booking_saved));
            (await CancelAsync(Winter, offset)).ShouldSucceed(English.Of(TranslationKeys.Booking_removed));
            (await EndAsync(Winter, offset)).ShouldSucceed(English.Of(TranslationKeys.Booking_removed));
            (await SkipAsync(Winter, offset)).ShouldSucceed(English.Updated("TrainGroupParticipantUnavailableDate"));
        }


        // ================================================================
        // The real clock
        // ================================================================

        // With no test to tell it, the app asks the machine.
        [Fact]
        public async Task It_is_the_real_time()
        {
            (await NextSessionAsync(DateTime.UtcNow.Date.AddDays(2))).ShouldNotBeNull();

            Database.Execute("DELETE FROM TrainGroupParticipants");
            (await NextSessionAsync(DateTime.UtcNow.Date.AddDays(-2))).ShouldBeNull();
        }
    }
}
