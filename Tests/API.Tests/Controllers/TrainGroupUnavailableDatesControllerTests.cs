using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
using Core.Models;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The gym calling a session off, which is a change to the group it is of.
    /// </summary>
    public sealed class TrainGroupUnavailableDatesControllerTests : ApiTest
    {
        private const string Url = "api/TrainGroupUnavailableDates";

        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _user;
        private readonly Caller _editor;
        private readonly TrainGroup _group;

        public TrainGroupUnavailableDatesControllerTests(ApiFixture api) : base(api)
        {
            _user = Seed.User();
            _editor = Caller.Of(_user, "TrainGroups_Edit");
            _group = Seed.Group(_user);
        }

        private List<string> CalledOff()
        {
            return Database.Read(db => db.TrainGroupUnavailableDates.OrderBy(x => x.Id).Select(x => x.TrainGroupId + " " + x.UnavailableDate.ToString("yyyy-MM-dd")).ToList());
        }

        private Task<ApiResult<List<TrainGroupUnavailableDate>>> CallOffAsync(Caller caller, int groupId)
        {
            return PostAsync<List<TrainGroupUnavailableDate>>(caller, Url, new[] { new { trainGroupId = groupId, unavailableDate = Day } });
        }

        private Task<ApiResult<TrainGroupUnavailableDate>> MoveAsync(Caller caller, TrainGroupUnavailableDate date)
        {
            return PutAsync<TrainGroupUnavailableDate>(caller, $"{Url}/{date.Id}", new { id = date.Id, trainGroupId = date.TrainGroupId, unavailableDate = date.UnavailableDate.AddDays(1) });
        }

        [Fact]
        public async Task A_session_is_called_off()
        {
            (await CallOffAsync(_editor, _group.Id)).ShouldSucceed(English.Updated("TrainGroupUnavailableDate"));

            CalledOff().ShouldBe([$"{_group.Id} 2031-03-05"]);
        }

        // Nobody looks whether it was.
        [Fact]
        public async Task A_session_can_be_called_off_twice()
        {
            Seed.Cancelled(_group, Day);

            await CallOffAsync(_editor, _group.Id);

            CalledOff().Count.ShouldBe(2);
        }

        [Fact]
        public async Task A_session_of_a_group_that_is_not_there_breaks()
        {
            (await CallOffAsync(_editor, 99)).ShouldBreak();
        }

        [Fact]
        public async Task A_session_is_called_back_on()
        {
            TrainGroupUnavailableDate date = Seed.Cancelled(_group, Day);

            (await DeleteAsync<TrainGroupUnavailableDate>(_editor, $"{Url}/{date.Id}")).ShouldSucceed(English.Deleted("TrainGroupUnavailableDate"));

            CalledOff().ShouldBeEmpty();
        }

        [Fact]
        public async Task It_is_moved_to_another_day()
        {
            (await MoveAsync(_editor, Seed.Cancelled(_group, Day))).ShouldSucceed(English.Updated("TrainGroupUnavailableDate"));

            CalledOff().ShouldBe([$"{_group.Id} 2031-03-06"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("TrainGroups_View TrainGroups_Add TrainGroups_Delete")]
        [InlineData("TrainGroupUnavailableDates_View TrainGroupUnavailableDates_Add TrainGroupUnavailableDates_Edit TrainGroupUnavailableDates_Delete")]
        [InlineData("TrainGroupDates_Edit TrainGroupParticipants_View TrainGroupParticipants_Edit")]
        [InlineData("traingroups_edit")]
        public async Task It_takes_the_permission_to_edit_groups_and_no_other_will_do(string permissions)
        {
            TrainGroupUnavailableDate date = Seed.Cancelled(_group, Day);
            Caller caller = Caller.Of(_user, permissions.Split(' '));

            (await CallOffAsync(caller, _group.Id)).ShouldFail(English.NotAuthorized);
            (await MoveAsync(caller, date)).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<TrainGroupUnavailableDate>(caller, $"{Url}/{date.Id}")).ShouldFail(English.NotAuthorized);

            CalledOff().ShouldBe([$"{_group.Id} 2031-03-05"]);
        }

        [Fact]
        public async Task Whoever_may_see_groups_may_read_it()
        {
            TrainGroupUnavailableDate date = Seed.Cancelled(_group, Day);

            (await GetAsync<TrainGroupUnavailableDateDto>(Caller.Of(_user, "TrainGroups_View"), $"{Url}/{date.Id}")).Value.UnavailableDate.ShouldBe(Day);
            (await GetAsync<TrainGroupUnavailableDateDto>(_editor, $"{Url}/{date.Id}")).ShouldFail(English.NotAuthorized);
            (await GetAsync<TrainGroupUnavailableDateDto>(Caller.Of(_user), $"{Url}/{date.Id}")).ShouldFail(English.NotAuthorized);
        }

        // Every member sees on the booking page which sessions are off.
        [Fact]
        public async Task The_grid_lists_them_for_anybody()
        {
            Seed.Cancelled(_group, Day);

            (await PostAsync<DataTableDto<TrainGroupUnavailableDateDto>>(Caller.Of(_user), Url + "/GetDataTable", Grid<TrainGroupUnavailableDateDto>())).Value.TotalRecords.ShouldBe(1);
        }
    }
}
