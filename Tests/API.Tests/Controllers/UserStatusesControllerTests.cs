using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.Lookup;
using Core.Dtos.UserStatus;
using Core.Models;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The dropdown of the member statuses, and the members they are of. Everything else about them is in
    /// GenericControllerTests.
    /// </summary>
    public sealed class UserStatusesControllerTests : ApiTest
    {
        private readonly Caller _anybody;

        public UserStatusesControllerTests(ApiFixture api) : base(api)
        {
            _anybody = Caller.Of(Seed.User());

            foreach (string name in new[] { "Ενεργός", "Ανενεργός", "Paused" })
                Seed.UserStatus(x => { x.Name = name; x.Color = "#00ff00"; });
        }

        private async Task<LookupDto> LookupAsync(string id, string value, int skip = 0, int take = 10)
        {
            ApiResult<LookupDto> result = await PostAsync<LookupDto>(_anybody, "api/UserStatuses/Lookup", new { skip, take, filter = new { id, value } });

            result.ShouldSucceed();

            return result.Value;
        }

        [Theory]
        [InlineData("", "Ενεργός Ανενεργός Paused")]
        [InlineData("ενεργος", "Ενεργός Ανενεργός")]
        [InlineData("ΑΝΕΝ", "Ανενεργός")]
        [InlineData("PAU", "Paused")]
        [InlineData("x", "")]
        public async Task Statuses_are_found_by_part_of_their_name(string value, string expected)
        {
            LookupDto answer = await LookupAsync("", value);

            string.Join(" ", answer.Data.Select(x => x.Value)).ShouldBe(expected);
            answer.TotalRecords.ShouldBe(answer.Data.Count);
        }

        [Fact]
        public async Task One_status_is_found_by_its_id()
        {
            UserStatus status = Database.Read(db => db.UserStatuses.Single(x => x.Name == "Paused"));

            LookupDto answer = await LookupAsync(status.Id.ToString(), "");

            LookupOptionDto option = answer.Data.Single();
            option.Id.ShouldBe(status.Id.ToString());
            option.Value.ShouldBe("Paused");
            option.UserColor.ShouldBe("#00ff00");
            answer.TotalRecords.ShouldBe(1);
        }

        // Both have to fit.
        [Fact]
        public async Task An_id_and_a_name_together_find_only_what_fits_both()
        {
            UserStatus status = Database.Read(db => db.UserStatuses.Single(x => x.Name == "Paused"));

            (await LookupAsync(status.Id.ToString(), "pau")).Data.Count.ShouldBe(1);
            (await LookupAsync(status.Id.ToString(), "ενεργ")).Data.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("99")]
        [InlineData("not an id")]
        public async Task An_id_nobody_has_finds_nothing(string id)
        {
            LookupDto answer = await LookupAsync(id, "");

            answer.Data.ShouldBeEmpty();
            answer.TotalRecords.ShouldBe(0);
        }

        [Theory]
        [InlineData(0, 2, 2)]
        [InlineData(2, 2, 1)]
        [InlineData(3, 2, 0)]
        [InlineData(0, 0, 0)]
        // A page that makes no sense is every row.
        [InlineData(-1, 2, 3)]
        public async Task A_page_counts_them_all(int skip, int take, int expected)
        {
            LookupDto answer = await LookupAsync("", "", skip, take);

            answer.Data.Count.ShouldBe(expected);
            answer.TotalRecords.ShouldBe(3);
            answer.Skip.ShouldBe(skip);
            answer.Take.ShouldBe(take);
        }

        [Fact]
        public async Task A_dropdown_needs_a_filter()
        {
            (await PostAsync<LookupDto>(_anybody, "api/UserStatuses/Lookup", new { take = 10 })).ShouldBeBad(English.Required("Filter"));
        }

        [Fact]
        public async Task The_grid_lists_them()
        {
            DataTableDto<UserStatusDto> answer = (await PostAsync<DataTableDto<UserStatusDto>>(_anybody, "api/UserStatuses/GetDataTable", SortedGrid<UserStatusDto>(("id", 1)))).Value;

            answer.Data!.Select(x => x.Name).ShouldBe(["Ενεργός", "Ανενεργός", "Paused"]);
        }


        // ================================================================
        // The members it is the status of
        // ================================================================

        private User MemberOf(string status)
        {
            return Seed.User(x =>
            {
                x.UserStatusId = Database.Read(db => db.UserStatuses.Single(y => y.Name == status).Id);
                x.Email = "maria@gym.test";
                x.MedicalHistory = "private";
            });
        }

        // Anybody signed in may ask for them, so they come by themselves.
        [Fact]
        public async Task They_do_not_come_with_it()
        {
            User member = MemberOf("Paused");

            ApiResult<DataTableDto<UserStatusDto>> grid = await PostAsync<DataTableDto<UserStatusDto>>(_anybody, "api/UserStatuses/GetDataTable", Grid<UserStatusDto>());
            ApiResult<UserStatusDto> one = await GetAsync<UserStatusDto>(Caller.Of(member, "UserStatuses_View"), $"api/UserStatuses/{member.UserStatusId}");

            grid.Value.Data!.ShouldAllBe(x => x.Users.Count == 0);
            one.Value.Users.ShouldBeEmpty();
            (grid.Body + one.Body).ShouldNotContain("maria@gym.test");
            (grid.Body + one.Body).ShouldNotContain("private");
        }

        // Nor are they written with it.
        [Fact]
        public async Task A_member_sent_along_is_not_written()
        {
            User member = MemberOf("Paused");
            object[] users =
            [
                new { id = member.Id, userName = "changed", email = "changed@gym.test", firstName = "Changed", lastName = "Changed" },
                new { userName = "new", email = "new@gym.test", firstName = "New", lastName = "New" }
            ];

            (await PostAsync<object>(Caller.Of(member, "UserStatuses_Add"), "api/UserStatuses", new[] { new { name = "Another", color = "#112233", users } })).ShouldSucceed(English.Updated("UserStatus"));
            (await PutAsync<object>(Caller.Of(member, "UserStatuses_Edit"), $"api/UserStatuses/{member.UserStatusId}", new { id = member.UserStatusId, name = "Renamed", color = "#112233", users })).ShouldSucceed(English.Updated("UserStatus"));

            Database.Read(db => db.UserStatuses.Select(x => x.Name).ToList()).ShouldBe(["Ενεργός", "Ανενεργός", "Renamed", "Another"], ignoreOrder: true);
            Database.Read(db => db.Users.Select(x => x.Email).ToList()).ShouldNotContain("new@gym.test");
            Database.Read(db => db.Users.Single(x => x.Id == member.Id)).Email.ShouldBe("maria@gym.test");
            Database.Read(db => db.Users.Count()).ShouldBe(2);
        }
    }
}
