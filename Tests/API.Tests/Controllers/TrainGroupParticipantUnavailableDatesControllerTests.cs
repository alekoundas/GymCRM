using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
using Core.Enums;
using Core.Models;
using Core.Translations;
using Tests.Shared;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Skipping one date of a recurring booking, and taking it back.
    /// </summary>
    public sealed class TrainGroupParticipantUnavailableDatesControllerTests : ApiTest
    {
        private const string Url = "api/TrainGroupParticipantUnavailableDates";

        // A Wednesday. The session is at six in the evening.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _member;
        private readonly User _other;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;
        private readonly TrainGroupParticipant _booking;

        public TrainGroupParticipantUnavailableDatesControllerTests(ApiFixture api) : base(api)
        {
            _member = Seed.User();
            _other = Seed.User();
            _group = Seed.Group(Seed.User(), x => x.MaxParticipants = 2);
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            _booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-28));
        }

        private Task<ApiResult<List<TrainGroupParticipantUnavailableDate>>> SkipAsync(Caller caller, int participantId, DateTime day, DateTime now, bool isAdminPage = false)
        {
            AtTheGym(now);

            return PostAsync<List<TrainGroupParticipantUnavailableDate>>(caller, Url, new[]
            {
                new { trainGroupParticipantId = participantId, unavailableDate = day, isAdminPage }
            });
        }

        private List<string> Skips()
        {
            return Database.Read(db => db.TrainGroupParticipantUnavailableDates.OrderBy(x => x.UnavailableDate).Select(x => x.UnavailableDate.ToString("yyyy-MM-dd")).ToList());
        }


        // ================================================================
        // Skipping
        // ================================================================

        [Fact]
        public async Task A_member_skips_a_date_of_their_booking()
        {
            ApiResult<List<TrainGroupParticipantUnavailableDate>> result = await SkipAsync(Caller.Of(_member), _booking.Id, Day, Day.AddDays(-1));

            result.ShouldSucceed(English.Updated("TrainGroupParticipantUnavailableDate"));
            result.Value.Single().TrainGroupParticipantId.ShouldBe(_booking.Id);
            Skips().ShouldBe(["2031-03-05"]);
        }

        // Twelve hours before and a minute more, up to the start itself; for the member, for staff on
        // the members' page, and for staff on the admin's. Whose clock it is, is in GymClockTests.
        [Theory]
        [InlineData(-24 * 60, null)]
        [InlineData(-12 * 60 - 1, null)]
        [InlineData(-12 * 60, TranslationKeys.Cannot_remove_a_session_starting_within_12_hours)]
        [InlineData(-11 * 60, TranslationKeys.Cannot_remove_a_session_starting_within_12_hours)]
        [InlineData(-1, TranslationKeys.Cannot_remove_a_session_starting_within_12_hours)]
        [InlineData(0, TranslationKeys.This_session_has_already_started)]
        [InlineData(60, TranslationKeys.This_session_has_already_started)]
        [InlineData(60 * 24 * 30, TranslationKeys.This_session_has_already_started)]
        public async Task A_member_has_until_twelve_hours_before(int minutesFromStart, string? error)
        {
            DateTime now = Day.AddHours(18).AddMinutes(minutesFromStart);

            foreach ((Caller caller, bool isAdminPage, bool isBoundByTheWindow) in new[]
            {
                (Caller.Of(_member), false, true),
                // Saying it is the admin's page does not make it theirs.
                (Caller.Of(_member), true, true),
                (Staff(_other), false, true),
                (Staff(_other), true, false)
            })
            {
                Database.Execute("DELETE FROM TrainGroupParticipantUnavailableDates");

                ApiResult<List<TrainGroupParticipantUnavailableDate>> result = await SkipAsync(caller, _booking.Id, Day, now, isAdminPage);

                if (error == null || !isBoundByTheWindow)
                    result.ShouldSucceed(English.Updated("TrainGroupParticipantUnavailableDate"));
                else
                    result.ShouldBeBad(English.Of(error));

                Skips().Count.ShouldBe(result.IsSucceed ? 1 : 0);
            }
        }

        [Fact]
        public async Task Somebody_elses_date_cannot_be_skipped()
        {
            (await SkipAsync(Caller.Of(_other), _booking.Id, Day, Day.AddDays(-1))).ShouldBeBad(English.NotAuthorized);
            (await SkipAsync(Caller.Of(_other), _booking.Id, Day, Day.AddDays(-1), isAdminPage: true)).ShouldBeBad(English.NotAuthorized);
            (await SkipAsync(Caller.WithNoId(), _booking.Id, Day, Day.AddDays(-1))).ShouldBeBad(English.NotAuthorized);

            Skips().ShouldBeEmpty();
        }

        [Fact]
        public async Task A_booking_that_is_not_there_is_not_found()
        {
            (await SkipAsync(Caller.Of(_member), 99, Day, Day.AddDays(-1))).ShouldBeBad(English.NotFound("TrainGroupParticipant"));
            (await SkipAsync(Staff(_other), 99, Day, Day.AddDays(-1), isAdminPage: true)).ShouldBeBad(English.NotFound("TrainGroupParticipant"));
        }

        // Whoever asks, only a date the booking holds a place on can be given up.
        [Theory]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(-35)]
        [InlineData(28)]
        public async Task A_date_that_is_not_the_bookings_cannot_be_skipped(int days)
        {
            Database.Change(db => db.TrainGroupParticipants.Single().RecurringEndOnDate = Day.AddDays(28));

            (await SkipAsync(Caller.Of(_member), _booking.Id, Day.AddDays(days), Day.AddDays(-100))).ShouldBeBad(English.Of(TranslationKeys.This_date_is_not_part_of_the_booking));
            (await SkipAsync(Staff(_other), _booking.Id, Day.AddDays(days), Day.AddDays(-100), isAdminPage: true)).ShouldBeBad(English.Of(TranslationKeys.This_date_is_not_part_of_the_booking));
        }

        [Fact]
        public async Task A_date_skipped_already_cannot_be_skipped_again()
        {
            Seed.Skip(_booking, Day);

            (await SkipAsync(Caller.Of(_member), _booking.Id, Day, Day.AddDays(-1))).ShouldBeBad(English.Of(TranslationKeys.This_date_is_not_part_of_the_booking));
        }

        [Fact]
        public async Task A_single_session_is_not_skipped_but_cancelled()
        {
            TrainGroupParticipant oneOff = Seed.OneOff(_wednesday, _other, Day);
            TrainGroupParticipant fixedDay = Seed.Participant(Seed.Fixed(_group, Day), _other);

            (await SkipAsync(Caller.Of(_other), oneOff.Id, Day, Day.AddDays(-1))).ShouldBeBad(English.Of(TranslationKeys.This_date_is_not_part_of_the_booking));
            (await SkipAsync(Caller.Of(_other), fixedDay.Id, Day, Day.AddDays(-1))).ShouldBeBad(English.Of(TranslationKeys.This_date_is_not_part_of_the_booking));
        }

        [Fact]
        public async Task The_time_of_the_day_sent_does_not_matter()
        {
            (await SkipAsync(Caller.Of(_member), _booking.Id, Day.AddHours(22), Day.AddDays(-1))).ShouldSucceed(English.Updated("TrainGroupParticipantUnavailableDate"));

            Skips().ShouldBe(["2031-03-05"]);
        }

        // Each is looked at against what is there, and one that will not do adds none.
        [Fact]
        public async Task Several_dates_go_together_or_not_at_all()
        {
            AtTheGym(Day.AddDays(-1));

            object At(DateTime day)
            {
                return new { trainGroupParticipantId = _booking.Id, unavailableDate = day };
            }

            (await PostAsync<List<TrainGroupParticipantUnavailableDate>>(Caller.Of(_member), Url, new[] { At(Day), At(Day.AddDays(1)) }))
                .ShouldBeBad(English.Of(TranslationKeys.This_date_is_not_part_of_the_booking));
            Skips().ShouldBeEmpty();

            (await PostAsync<List<TrainGroupParticipantUnavailableDate>>(Caller.Of(_member), Url, new[] { At(Day), At(Day.AddDays(7)), At(Day) }))
                .ShouldSucceed(English.Updated("TrainGroupParticipantUnavailableDate"));
            Skips().ShouldBe(["2031-03-05", "2031-03-05", "2031-03-12"]);
        }


        // ================================================================
        // Taking it back
        // ================================================================

        private TrainGroupParticipantUnavailableDate SkipOf(User member, DateTime day)
        {
            return Seed.Skip(Seed.Recurring(_wednesday, member, day.AddDays(-70)), day);
        }

        private Task<ApiResult<TrainGroupParticipantUnavailableDate>> RejoinAsync(Caller caller, int id, DateTime? now = null)
        {
            AtTheGym(now ?? Day.AddDays(-1));

            return DeleteAsync<TrainGroupParticipantUnavailableDate>(caller, $"{Url}/{id}");
        }

        [Fact]
        public async Task A_member_takes_a_date_back()
        {
            TrainGroupParticipantUnavailableDate skip = SkipOf(_other, Day);

            (await RejoinAsync(Caller.Of(_other), skip.Id)).ShouldSucceed(English.Deleted("TrainGroupParticipantUnavailableDate"));

            Skips().ShouldBeEmpty();
        }

        [Fact]
        public async Task Staff_take_anybodys_date_back()
        {
            (await RejoinAsync(Staff(_member), SkipOf(_other, Day).Id)).ShouldSucceed(English.Deleted("TrainGroupParticipantUnavailableDate"));
        }

        [Fact]
        public async Task Somebody_elses_date_cannot_be_taken_back()
        {
            TrainGroupParticipantUnavailableDate skip = SkipOf(_other, Day);

            (await RejoinAsync(Caller.Of(_member), skip.Id)).ShouldBeBad(English.NotAuthorized);
            (await RejoinAsync(Caller.WithNoId(), skip.Id)).ShouldBeBad(English.NotAuthorized);

            Skips().Count.ShouldBe(1);
        }

        // Minutes from the start. A session that has started stays as it was for a member; there is no
        // twelve hours about coming to one after all.
        [Theory]
        [InlineData(-24 * 60, true)]
        [InlineData(-60, true)]
        [InlineData(-1, true)]
        [InlineData(0, false)]
        [InlineData(5 * 60, false)]
        [InlineData(24 * 60, false)]
        public async Task A_session_that_has_started_is_only_for_staff_to_take_back(int minutesFromStart, bool isForMembers)
        {
            DateTime now = Day.AddHours(18).AddMinutes(minutesFromStart);

            ApiResult<TrainGroupParticipantUnavailableDate> result = await RejoinAsync(Caller.Of(_other), SkipOf(_other, Day).Id, now);

            if (isForMembers)
                result.ShouldSucceed(English.Deleted("TrainGroupParticipantUnavailableDate"));
            else
                result.ShouldBeBad(English.Of(TranslationKeys.This_session_has_already_started));

            Database.Execute("DELETE FROM TrainGroupParticipants");
            (await RejoinAsync(Staff(_member), SkipOf(_other, Day).Id, now)).ShouldSucceed(English.Deleted("TrainGroupParticipantUnavailableDate"));
        }

        // Two places. Whoever holds one that day is counted: recurring, one-off, of any of the days.
        [Theory]
        [InlineData(0, false, true)]
        [InlineData(1, false, true)]
        [InlineData(2, false, false)]
        [InlineData(3, false, false)]
        // Staff cannot put somebody where there is no place either.
        [InlineData(1, true, true)]
        [InlineData(2, true, false)]
        public async Task A_date_can_be_taken_back_while_there_is_a_place(int taken, bool isStaff, bool isTakenBack)
        {
            Database.Execute("DELETE FROM TrainGroupParticipants");
            DateTime day = Day;
            TrainGroupParticipantUnavailableDate skip = SkipOf(_other, day);

            for (int i = 0; i < taken; i++)
                if (i % 2 == 0)
                    Seed.Recurring(_wednesday, Seed.User(), day.AddDays(-7));
                else
                    Seed.OneOff(_wednesday, Seed.User(), day);

            // Neither of these holds a place that day, nor does a booking in another group.
            Seed.Recurring(_wednesday, Seed.User(), day.AddDays(7));
            Seed.OneOff(_wednesday, Seed.User(), day, x => x.RemovedOn = day.AddDays(-1));
            Seed.OneOff(Seed.Weekly(Seed.Group(_member), DayOfWeek.Wednesday), Seed.User(), day);

            ApiResult<TrainGroupParticipantUnavailableDate> result = await RejoinAsync(isStaff ? Staff(_member) : Caller.Of(_other), skip.Id);

            if (isTakenBack)
                result.ShouldSucceed(English.Deleted("TrainGroupParticipantUnavailableDate"));
            else
                result.ShouldBeBad(English.Of(TranslationKeys.Maximum_amount_of_participants_has_been_reached));

            Skips().Count.ShouldBe(isTakenBack ? 0 : 1);
        }

        [Fact]
        public async Task A_date_that_is_not_there_is_not_found()
        {
            (await RejoinAsync(Staff(_member), 99)).ShouldFail(English.NotFound("TrainGroupParticipantUnavailableDate"));
        }

        // The database keeps the two together; this one was pulled apart by hand.
        [Fact]
        public async Task A_date_of_a_booking_that_is_gone_is_not_found()
        {
            Database.Execute("""
                PRAGMA foreign_keys = OFF;
                INSERT INTO TrainGroupParticipantUnavailableDates (Id, UnavailableDate, TrainGroupParticipantId, CreatedBy_Id, CreatedBy_FullName, CreatedOn)
                VALUES (7, '2031-03-05 00:00:00', 99, '', '', '2031-03-05 00:00:00');
                PRAGMA foreign_keys = ON;
                """);

            (await RejoinAsync(Staff(_member), 7)).ShouldBeBad(English.NotFound("TrainGroupParticipant"));
        }


        // ================================================================
        // Reading and listing
        // ================================================================

        private Task<ApiResult<TrainGroupParticipantUnavailableDateDto>> ReadAsync(Caller caller, object id)
        {
            return GetAsync<TrainGroupParticipantUnavailableDateDto>(caller, $"{Url}/{id}");
        }

        [Fact]
        public async Task A_date_is_read_by_the_member_and_by_staff()
        {
            TrainGroupParticipantUnavailableDate skip = Seed.Skip(_booking, Day);

            foreach (Caller caller in new[] { Caller.Of(_member), Staff(_other) })
            {
                TrainGroupParticipantUnavailableDateDto dto = (await ReadAsync(caller, skip.Id)).Value;

                dto.Id.ShouldBe(skip.Id);
                dto.UnavailableDate.ShouldBe(Day);
                dto.TrainGroupParticipantId.ShouldBe(_booking.Id);
            }
        }

        [Fact]
        public async Task Somebody_elses_is_answered_like_one_that_is_not_there()
        {
            TrainGroupParticipantUnavailableDate skip = Seed.Skip(_booking, Day);

            (await ReadAsync(Caller.Of(_other), skip.Id)).ShouldFail(English.NotFound("TrainGroupParticipantUnavailableDate"));
            (await ReadAsync(Caller.WithNoId(), skip.Id)).ShouldFail(English.NotFound("TrainGroupParticipantUnavailableDate"));
            (await ReadAsync(Caller.Of(_other, "TrainGroupParticipantUnavailableDates_View", "TrainGroups_View"), skip.Id)).ShouldFail(English.NotFound("TrainGroupParticipantUnavailableDate"));
            (await ReadAsync(Staff(_other), 99)).ShouldFail(English.NotFound("TrainGroupParticipantUnavailableDate"));
        }

        [Fact]
        public async Task Reading_an_id_that_is_no_number_breaks()
        {
            (await ReadAsync(Staff(_other), "x")).ShouldBreak();
        }

        private async Task<string> GridAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            DataTableDto<TrainGroupParticipantUnavailableDateDto> answer = (await PostAsync<DataTableDto<TrainGroupParticipantUnavailableDateDto>>(
                caller, Url + "/GetDataTable", Grid<TrainGroupParticipantUnavailableDateDto>(filters))).Value;

            answer.TotalRecords.ShouldBe(answer.Data!.Count());

            return string.Join(" ", answer.Data!.OrderBy(x => x.UnavailableDate).Select(x => x.UnavailableDate.ToString("MM-dd")));
        }

        [Fact]
        public async Task The_grid_gives_a_member_their_own_and_staff_everybodys()
        {
            TrainGroupParticipant others = Seed.Recurring(_wednesday, _other, Day.AddDays(-28));
            Seed.Skip(_booking, Day);
            Seed.Skip(_booking, Day.AddDays(7));
            Seed.Skip(others, Day.AddDays(14));

            (await GridAsync(Caller.Of(_member))).ShouldBe("03-05 03-12");
            (await GridAsync(Caller.Of(_other))).ShouldBe("03-19");
            (await GridAsync(Caller.Of(_other, "TrainGroupParticipantUnavailableDates_View", "TrainGroups_View"))).ShouldBe("03-19");
            (await GridAsync(Staff(_other))).ShouldBe("03-05 03-12 03-19");
            (await GridAsync(Caller.WithNoId())).ShouldBe("");
        }

        [Fact]
        public async Task A_filter_does_not_open_somebody_elses()
        {
            Seed.Skip(_booking, Day);
            DataTableFilterDto ofTheBooking = Filter("TrainGroupParticipantId", _booking.Id.ToString(), DataTableFiltersEnum.equals);

            (await GridAsync(Caller.Of(_other), ofTheBooking)).ShouldBe("");
            (await GridAsync(Caller.Of(_member), ofTheBooking)).ShouldBe("03-05");
        }


        // ================================================================
        // Editing
        // ================================================================

        // Moving one would go past the twelve hours and everything else.
        [Fact]
        public async Task A_date_is_never_edited()
        {
            TrainGroupParticipantUnavailableDate skip = Seed.Skip(_booking, Day);
            object form = new { id = skip.Id, trainGroupParticipantId = _booking.Id, unavailableDate = Day.AddDays(7) };

            foreach (Caller caller in new[] { Caller.Of(_member), Caller.Of(_other), Staff(_other), Caller.Of(_other, "TrainGroupParticipantUnavailableDates_Edit") })
                (await PutAsync<TrainGroupParticipantUnavailableDate>(caller, $"{Url}/{skip.Id}", form)).ShouldFail(English.NotAuthorized);

            Skips().ShouldBe(["2031-03-05"]);
        }
    }
}
