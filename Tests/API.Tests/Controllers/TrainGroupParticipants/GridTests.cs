using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace API.Tests.Controllers.TrainGroupParticipants
{
    /// <summary>
    /// Who is in a group: on the day the grid is on, or at all.
    /// </summary>
    public sealed class GridTests : ApiTest
    {
        private const string Url = "api/TrainGroupParticipants/GetDataTable";

        // A Wednesday, the 5th.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _maria;
        private readonly User _nikos;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;

        public GridTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User(x => x.FirstName = "Maria");
            _nikos = Seed.User(x => x.FirstName = "Nikos");
            _group = Seed.Group(Seed.User());
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
        }

        private async Task<DataTableDto<TrainGroupParticipantDto>> GridAsync(Caller? caller = null, params DataTableFilterDto[] filters)
        {
            ApiResult<DataTableDto<TrainGroupParticipantDto>> result = await PostAsync<DataTableDto<TrainGroupParticipantDto>>(caller ?? Staff(_maria), Url, Grid<TrainGroupParticipantDto>(filters));

            result.ShouldSucceed();

            return result.Value;
        }

        private static DataTableFilterDto On(string? day)
        {
            return Filter("ParticipantGridSelectedDate", day, DataTableFiltersEnum.custom);
        }

        private async Task<int[]> IdsOnAsync(string? day)
        {
            return (await GridAsync(null, On(day))).Data!.Select(x => x.Id).Order().ToArray();
        }


        // ================================================================
        // Whose rows
        // ================================================================

        [Fact]
        public async Task Staff_see_everybody_and_a_member_sees_themselves()
        {
            TrainGroupParticipant maria = Seed.OneOff(_wednesday, _maria, Day);
            TrainGroupParticipant nikos = Seed.OneOff(_wednesday, _nikos, Day);

            (await GridAsync()).Data!.Select(x => x.Id).ShouldBe([maria.Id, nikos.Id], ignoreOrder: true);

            DataTableDto<TrainGroupParticipantDto> own = await GridAsync(Caller.Of(_nikos, "TrainGroupParticipants_Delete"));
            own.Data!.Single().Id.ShouldBe(nikos.Id);
            own.TotalRecords.ShouldBe(1);

            (await GridAsync(Caller.Of(_nikos), On("2031-03-05T00:00:00.000Z"))).Data!.Single().Id.ShouldBe(nikos.Id);
            (await GridAsync(Caller.WithNoId())).TotalRecords.ShouldBe(0);
        }

        // A filter cannot reach past it.
        [Fact]
        public async Task A_member_cannot_filter_to_somebody_else()
        {
            Seed.OneOff(_wednesday, _maria, Day);

            (await GridAsync(Caller.Of(_nikos), Filter("UserId", _maria.Id.ToString(), DataTableFiltersEnum.equals))).TotalRecords.ShouldBe(0);
        }

        [Fact]
        public async Task The_member_and_their_status_come_along()
        {
            UserStatus status = Seed.UserStatus(x => x.Name = "Active");
            Database.Change(db => db.Users.Single(x => x.Id == _maria.Id).UserStatusId = status.Id);
            Seed.OneOff(_wednesday, _maria, Day);

            TrainGroupParticipantDto row = (await GridAsync()).Data!.Single();

            row.User.ShouldNotBeNull().FirstName.ShouldBe("Maria");
            row.User.UserStatus.ShouldNotBeNull().Name.ShouldBe("Active");
        }

        [Fact]
        public async Task It_filters_by_group_and_by_date()
        {
            TrainGroupDate friday = Seed.Weekly(_group, DayOfWeek.Friday);
            TrainGroupDate elsewhere = Seed.Weekly(Seed.Group(_maria), DayOfWeek.Wednesday);
            TrainGroupParticipant wanted = Seed.Recurring(_wednesday, _maria, Day);
            TrainGroupParticipant onFriday = Seed.Recurring(friday, _maria, Day);
            Seed.Recurring(elsewhere, _maria, Day);

            (await GridAsync(null, Filter("TrainGroupId", _group.Id.ToString(), DataTableFiltersEnum.equals))).Data!.Select(x => x.Id).ShouldBe([wanted.Id, onFriday.Id], ignoreOrder: true);
            (await GridAsync(null, Filter("TrainGroupDateId", _wednesday.Id.ToString(), DataTableFiltersEnum.equals))).Data!.Single().Id.ShouldBe(wanted.Id);
            (await GridAsync(null, Filter("TrainGroupId", _group.Id.ToString(), DataTableFiltersEnum.equals), On("2031-03-05T00:00:00.000Z"))).Data!.Single().Id.ShouldBe(wanted.Id);
        }


        // ================================================================
        // At all
        // ================================================================

        [Fact]
        public async Task With_no_date_it_lists_the_bookings_that_are_still_open()
        {
            DateTime today = Midnight.Today();

            List<TrainGroupParticipant> open =
            [
                Seed.OneOff(_wednesday, _maria, Day),
                Seed.OneOff(_wednesday, _maria, new DateTime(2020, 1, 1)),
                Seed.Participant(Seed.Fixed(_group, Day), _maria),
                Seed.Recurring(_wednesday, _maria, Day.AddDays(-70)),
                Seed.Recurring(_wednesday, _maria, null),
                Seed.Recurring(_wednesday, _maria, today.AddDays(-70), today.AddDays(1)),
                Seed.Recurring(_wednesday, _maria, today.AddDays(-70), today.AddHours(1)),
                // Ended from later on, and said so already.
                Seed.Recurring(_wednesday, _maria, today.AddDays(-70), today.AddDays(70), x => x.RemovedOn = today)
            ];

            Seed.OneOff(_wednesday, _maria, Day, x => x.RemovedOn = today);
            Seed.Participant(Seed.Fixed(_group, Day), _maria, x => x.RemovedOn = today);
            Seed.Recurring(_wednesday, _maria, today.AddDays(-70), today);
            Seed.Recurring(_wednesday, _maria, today.AddDays(-70), today.AddDays(-1));

            foreach (DataTableFilterDto[] filters in new[]
            {
                Array.Empty<DataTableFilterDto>(),
                [On(null)],
                [On("")],
                [On("  ")],
                // Somebody else's filter, and the right name under the wrong kind.
                [Filter("Other", "2031-03-05", DataTableFiltersEnum.custom)]
            })
            {
                DataTableDto<TrainGroupParticipantDto> grid = await GridAsync(null, filters);

                grid.Data!.Select(x => x.Id).ShouldBe(open.Select(x => x.Id), ignoreOrder: true);
                grid.TotalRecords.ShouldBe(open.Count);
            }
        }

        // It is no column, so as any other kind of filter it is looked for among them.
        [Fact]
        public async Task The_date_as_another_kind_of_filter_breaks_the_grid()
        {
            (await PostAsync<DataTableDto<TrainGroupParticipantDto>>(Staff(_maria), Url, Grid<TrainGroupParticipantDto>(Filter("ParticipantGridSelectedDate", "2031-03-05", DataTableFiltersEnum.equals)))).ShouldBreak();
        }


        // ================================================================
        // On a day
        // ================================================================

        [Fact]
        public async Task On_a_day_it_lists_who_is_in_the_session()
        {
            List<TrainGroupParticipant> there =
            [
                Seed.OneOff(_wednesday, _maria, Day),
                Seed.OneOff(_wednesday, _maria, Day.AddHours(23)),
                Seed.Participant(Seed.Fixed(_group, Day), _maria),
                Seed.Participant(Seed.Monthly(_group, 5), _maria, x => x.RecurringStartOnDate = Day),
                Seed.Recurring(_wednesday, _maria, Day),
                Seed.Recurring(_wednesday, _maria, Day.AddDays(-70), Day.AddDays(1)),
                Seed.Recurring(_wednesday, _maria, null, change: x => x.CreatedOn = Day.AddDays(-1))
            ];

            Seed.OneOff(_wednesday, _maria, Day.AddDays(7));
            Seed.OneOff(_wednesday, _maria, Day, x => x.RemovedOn = Day);
            Seed.Recurring(_wednesday, _maria, Day.AddDays(7));
            Seed.Recurring(_wednesday, _maria, Day.AddDays(-70), Day);
            Seed.Skip(Seed.Recurring(_wednesday, _maria, Day.AddDays(-70)), Day);
            Seed.Recurring(_wednesday, _maria, null, change: x => x.CreatedOn = Day.AddDays(1));

            (await IdsOnAsync("2031-03-05T00:00:00.000Z")).ShouldBe(there.Select(x => x.Id).Order());
        }

        // Midnight in UTC is how the page sends it. Whatever else it says, its own date is the day.
        [Theory]
        [InlineData("2031-03-05")]
        [InlineData("2031-03-05T00:00:00")]
        [InlineData("2031-03-05T23:59:59.999Z")]
        [InlineData("2031-03-05T00:00:00.000Z")]
        [InlineData("Wed, 05 Mar 2031 00:00:00 GMT")]
        public async Task The_day_is_the_date_that_is_sent(string day)
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _maria, Day);

            (await IdsOnAsync(day)).ShouldBe([booking.Id]);
        }

        [Theory]
        [InlineData("x")]
        [InlineData("2031-02-30")]
        public async Task A_day_that_is_no_date_breaks_the_grid(string day)
        {
            (await PostAsync<DataTableDto<TrainGroupParticipantDto>>(Staff(_maria), Url, Grid<TrainGroupParticipantDto>(On(day)))).ShouldBreak();
        }

        [Fact]
        public async Task The_first_of_two_dates_counts()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _maria, Day);
            Seed.OneOff(_wednesday, _maria, Day.AddDays(7));

            (await GridAsync(null, On("2031-03-05"), On("2031-03-12"))).Data!.Single().Id.ShouldBe(booking.Id);
        }


        // ================================================================
        // What each row says besides
        // ================================================================

        [Fact]
        public async Task The_lessons_owed_are_on_the_row_whatever_the_day()
        {
            Seed.Subscription(_maria, 10);
            Seed.Subscription(_maria, -3);
            Seed.Subscription(_maria, 100, SubscriptionStatusEnum.PENDING);
            TrainGroupParticipant maria = Seed.OneOff(_wednesday, _maria, Day);
            TrainGroupParticipant nikos = Seed.OneOff(_wednesday, _nikos, Day);

            foreach (DataTableFilterDto[] filters in new[] { Array.Empty<DataTableFilterDto>(), [On("2031-03-05")] })
            {
                List<TrainGroupParticipantDto> rows = (await GridAsync(null, filters)).Data!.ToList();

                rows.Single(x => x.Id == maria.Id).SubscriptionBalance.ShouldBe(7);
                rows.Single(x => x.Id == nikos.Id).SubscriptionBalance.ShouldBe(0);
            }
        }

        // The whole of the day, and nothing on either side.
        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(60 * 18, true)]
        [InlineData(60 * 24 - 1, true)]
        [InlineData(60 * 24, false)]
        public async Task Whether_they_came_is_on_the_row_when_there_is_a_day(int minutes, bool hasAttendance)
        {
            Seed.OneOff(_wednesday, _maria, Day);
            Seed.OneOff(_wednesday, _nikos, Day);
            Seed.Attendance(_maria, Day.AddMinutes(minutes), _group);

            List<TrainGroupParticipantDto> rows = (await GridAsync(null, On("2031-03-05T00:00:00.000Z"))).Data!.ToList();

            rows.Single(x => x.UserId == _maria.Id.ToString()).HasAttendance.ShouldBe(hasAttendance);
            rows.Single(x => x.UserId == _nikos.Id.ToString()).HasAttendance.ShouldBeFalse();
        }

        [Fact]
        public async Task Without_a_day_nobody_has_come()
        {
            Seed.OneOff(_wednesday, _maria, Day);
            Seed.Attendance(_maria, Day, _group);
            Seed.Attendance(_maria, Midnight.Today(), _group);

            (await GridAsync()).Data!.Single().HasAttendance.ShouldBeFalse();
        }

        [Fact]
        public async Task Attendance_elsewhere_or_written_by_hand_is_not_attendance_here()
        {
            Seed.OneOff(_wednesday, _maria, Day);
            Seed.Attendance(_maria, Day, Seed.Group(_nikos));
            Seed.Attendance(_maria, Day);

            (await GridAsync(null, On("2031-03-05"))).Data!.Single().HasAttendance.ShouldBeFalse();
        }

        // It is the member's attendance in any group the page is on.
        [Fact]
        public async Task Attendance_in_one_group_shows_on_the_members_row_in_another()
        {
            TrainGroup other = Seed.Group(_nikos);
            Seed.OneOff(_wednesday, _maria, Day);
            Seed.OneOff(Seed.Weekly(other, DayOfWeek.Wednesday), _maria, Day);
            Seed.Attendance(_maria, Day, other);

            (await GridAsync(null, On("2031-03-05"))).Data!.ShouldAllBe(x => x.HasAttendance);
        }

        [Fact]
        public async Task An_empty_grid_is_an_empty_grid()
        {
            DataTableDto<TrainGroupParticipantDto> grid = await GridAsync(null, On("2031-03-05"));

            grid.Data!.ShouldBeEmpty();
            grid.TotalRecords.ShouldBe(0);
            grid.PageCount.ShouldBe(0);
        }


        // ================================================================
        // Pages and order
        // ================================================================

        [Fact]
        public async Task The_rows_are_counted_past_the_page()
        {
            for (int i = 0; i < 5; i++)
                Seed.OneOff(_wednesday, _maria, Day);

            DataTableDto<TrainGroupParticipantDto> grid = (await PostAsync<DataTableDto<TrainGroupParticipantDto>>(Staff(_maria), Url,
                new DataTableDto<TrainGroupParticipantDto> { Rows = 2, Page = 2, Filters = [On("2031-03-05")] })).Value;

            grid.Data!.Count().ShouldBe(1);
            grid.TotalRecords.ShouldBe(5);
            grid.PageCount.ShouldBe(3);
        }

        [Fact]
        public async Task It_sorts_by_a_column()
        {
            TrainGroupParticipant later = Seed.OneOff(_wednesday, _maria, Day.AddDays(7));
            TrainGroupParticipant sooner = Seed.OneOff(_wednesday, _maria, Day);

            async Task<int[]> SortedAsync(int order)
            {
                return (await PostAsync<DataTableDto<TrainGroupParticipantDto>>(Staff(_maria), Url, SortedGrid<TrainGroupParticipantDto>(("selectedDate", order)))).Value.Data!.Select(x => x.Id).ToArray();
            }

            (await SortedAsync(1)).ShouldBe([sooner.Id, later.Id]);
            (await SortedAsync(-1)).ShouldBe([later.Id, sooner.Id]);
        }
    }
}
