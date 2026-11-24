using API.Controllers;
using API.Tests.Host;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.User;
using Core.Enums;
using Core.Models;

namespace API.Tests.Controllers.Users
{
    /// <summary>
    /// The grid of the members' page. It has its own sorts and filters beside the ones every other
    /// grid shares.
    /// </summary>
    public sealed class UsersGridTests : ApiTest
    {
        private const string Url = "api/Users/GetDataTable";

        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _eleni;
        private readonly Role _trainer;
        private readonly Role _member;
        private readonly UserStatus _active;

        public UsersGridTests(ApiFixture api) : base(api)
        {
            _active = Seed.UserStatus(x => x.Name = "Active");
            _maria = Seed.User(x => { x.UserName = "maria"; x.FirstName = "Μαρία"; x.LastName = "Papa"; x.UserStatusId = _active.Id; x.CreatedOn = new DateTime(2025, 1, 1); });
            _nikos = Seed.User(x => { x.UserName = "nikos"; x.FirstName = "Nikos"; x.LastName = "Papa"; x.CreatedOn = new DateTime(2025, 2, 1); });
            _eleni = Seed.User(x => { x.UserName = "eleni"; x.FirstName = "Eleni"; x.LastName = "Alfa"; x.CreatedOn = new DateTime(2025, 3, 1); });
            _trainer = Seed.Role("Trainer");
            _member = Seed.Role("SimpleUser");
            Seed.UserRole(_maria, _member);
            Seed.UserRole(_nikos, _trainer);
            Seed.UserRole(_eleni, _member);
        }

        private Caller Admin
        {
            get { return Caller.Of(_nikos, "Users_View"); }
        }

        private async Task<DataTableDto<UserDto>> GridAsync(DataTableDto<UserDto> grid)
        {
            ApiResult<DataTableDto<UserDto>> result = await PostAsync<DataTableDto<UserDto>>(Admin, Url, grid);

            result.ShouldSucceed();

            return result.Value;
        }

        private async Task<string> FilteredAsync(params DataTableFilterDto[] filters)
        {
            DataTableDto<UserDto> grid = await GridAsync(Grid<UserDto>(filters));

            // The total is of the filtered rows.
            grid.TotalRecords.ShouldBe(grid.Data!.Count());

            return string.Join(" ", grid.Data!.Select(x => x.UserName).Order(StringComparer.Ordinal));
        }

        private async Task<string> SortedAsync(params (string Field, int Order)[] sorts)
        {
            return string.Join(" ", (await GridAsync(SortedGrid<UserDto>(sorts))).Data!.Select(x => x.UserName));
        }

        private void Lessons(User user, int granted, int attended)
        {
            Seed.Subscription(user, granted);
            for (int i = 0; i < attended; i++)
                Seed.Attendance(user, new DateTime(2025, 1, 1));
        }


        // ================================================================
        // Who may, and what is in it
        // ================================================================

        [Fact]
        public async Task It_needs_the_permission_to_view_members()
        {
            (await PostAsync<DataTableDto<UserDto>>(Caller.Of(_nikos, "Users_Add", "Users_Edit", "Users_Delete", "TrainGroupParticipants_View"), Url, Grid<UserDto>())).ShouldFail(English.NotAuthorized);
            (await PostAsync<DataTableDto<UserDto>>(Caller.WithNoId("Users_View"), Url, Grid<UserDto>())).Value.TotalRecords.ShouldBe(3);
        }

        [Fact]
        public async Task Each_row_is_a_member_with_their_status_role_and_lessons()
        {
            Lessons(_maria, 10, 3);
            Seed.Subscription(_maria, 50, SubscriptionStatusEnum.PENDING);
            Lessons(_eleni, 0, 2);

            DataTableDto<UserDto> grid = await GridAsync(Grid<UserDto>());

            grid.TotalRecords.ShouldBe(3);
            grid.PageCount.ShouldBe(1);
            UserDto maria = grid.Data!.Single(x => x.UserName == "maria");
            maria.FirstName.ShouldBe("Μαρία");
            maria.UserStatus.ShouldNotBeNull().Name.ShouldBe("Active");
            maria.UserRoles.Single().Role.Name.ShouldBe("SimpleUser");
            maria.SubscriptionBalance.ShouldBe(7);
            grid.Data!.Single(x => x.UserName == "nikos").SubscriptionBalance.ShouldBe(0);
            grid.Data!.Single(x => x.UserName == "nikos").UserStatus.ShouldBeNull();
            grid.Data!.Single(x => x.UserName == "eleni").SubscriptionBalance.ShouldBe(-2);
        }

        [Theory]
        [InlineData(0, 2, "eleni maria", 2)]
        [InlineData(1, 2, "nikos", 2)]
        [InlineData(2, 2, "", 2)]
        [InlineData(0, 3, "eleni maria nikos", 1)]
        public async Task A_page_counts_them_all(int page, int rows, string expected, int pages)
        {
            DataTableDto<UserDto> grid = await GridAsync(new DataTableDto<UserDto> { Page = page, Rows = rows, Sorts = [new DataTableSortDto { FieldName = "userName", Order = 1 }] });

            string.Join(" ", grid.Data!.Select(x => x.UserName)).ShouldBe(expected);
            grid.TotalRecords.ShouldBe(3);
            grid.PageCount.ShouldBe(pages);
        }


        // ================================================================
        // Sorting
        // ================================================================

        [Theory]
        [InlineData("userName", 1, "eleni maria nikos")]
        [InlineData("UserName", -1, "nikos maria eleni")]
        [InlineData("createdOn", 1, "maria nikos eleni")]
        [InlineData("createdOn", -5, "eleni nikos maria")]
        public async Task It_sorts_by_a_column(string field, int order, string expected)
        {
            (await SortedAsync((field, order))).ShouldBe(expected);
        }

        // Every other column goes the way of the first.
        [Theory]
        [InlineData(1, 1, "eleni maria nikos")]
        [InlineData(1, -1, "eleni maria nikos")]
        [InlineData(-1, -1, "nikos maria eleni")]
        [InlineData(-1, 1, "nikos maria eleni")]
        public async Task A_second_column_decides_where_the_first_is_equal(int first, int second, string expected)
        {
            (await SortedAsync(("lastName", first), ("userName", second))).ShouldBe(expected);
        }

        [Fact]
        public async Task A_sort_that_goes_no_way_sorts_nothing()
        {
            (await SortedAsync(("userName", 0), ("createdOn", 1))).Split(' ').Order().ShouldBe(["eleni", "maria", "nikos"]);
        }

        // The lessons left: granted, less attended. A request is not a lesson.
        [Theory]
        [InlineData(1, "eleni nikos maria")]
        [InlineData(9, "eleni nikos maria")]
        [InlineData(-1, "maria nikos eleni")]
        [InlineData(0, "maria nikos eleni")]
        public async Task It_sorts_by_the_lessons_left(int order, string expected)
        {
            Lessons(_maria, 10, 3);
            Lessons(_nikos, 2, 1);
            Lessons(_eleni, 5, 6);
            Seed.Subscription(_eleni, 100, SubscriptionStatusEnum.PENDING);
            Seed.Subscription(_eleni, 100, SubscriptionStatusEnum.REJECTED);

            foreach (string field in new[] { "subscriptionBalance", "SubscriptionBalance" })
                (await SortedAsync((field, order))).ShouldBe(expected);

            // Whatever else was asked for, before it or after.
            (await SortedAsync(("subscriptionBalance", order), ("userName", 1))).ShouldBe(expected);
            (await SortedAsync(("userName", 1), ("subscriptionBalance", order))).ShouldBe(expected);
        }

        [Fact]
        public async Task It_sorts_by_lessons_over_every_page()
        {
            Lessons(_maria, 1, 0);
            Lessons(_nikos, 3, 0);
            Lessons(_eleni, 2, 0);

            async Task<string> PageAsync(int page)
            {
                return (await GridAsync(new DataTableDto<UserDto> { Page = page, Rows = 1, Sorts = [new DataTableSortDto { FieldName = "subscriptionBalance", Order = -1 }] })).Data!.Single().UserName;
            }

            (await PageAsync(0)).ShouldBe("nikos");
            (await PageAsync(1)).ShouldBe("eleni");
            (await PageAsync(2)).ShouldBe("maria");
        }

        [Fact]
        public async Task A_sort_by_something_that_is_no_column_breaks()
        {
            (await PostAsync<DataTableDto<UserDto>>(Admin, Url, SortedGrid<UserDto>(("other", 1)))).ShouldBreak();
        }


        // ================================================================
        // Filtering
        // ================================================================

        [Theory]
        // Without its accents and in any case.
        [InlineData("firstName", "μαρια", "maria")]
        [InlineData("firstName", "ΜΑΡΊΑ", "maria")]
        [InlineData("userName", "i", "eleni maria nikos")]
        [InlineData("LastName", "papa", "maria nikos")]
        [InlineData("userName", "none", "")]
        [InlineData("userName", "", "eleni maria nikos")]
        [InlineData("userName", null, "eleni maria nikos")]
        public async Task Contains_keeps_the_rows_with_the_text(string field, string? value, string expected)
        {
            (await FilteredAsync(Filter(field, value))).ShouldBe(expected);
        }

        [Fact]
        public async Task Equals_and_not_equals()
        {
            (await FilteredAsync(Filter("UserName", "maria", DataTableFiltersEnum.equals))).ShouldBe("maria");
            (await FilteredAsync(Filter("UserName", "maria", DataTableFiltersEnum.notEquals))).ShouldBe("eleni nikos");
            (await FilteredAsync(Filter("UserName", (string?)null, DataTableFiltersEnum.equals))).ShouldBe("eleni maria nikos");
            (await FilteredAsync(Filter("UserName", (string?)null, DataTableFiltersEnum.notEquals))).ShouldBe("eleni maria nikos");
            (await FilteredAsync(Filter("UserStatusId", _active.Id.ToString(), DataTableFiltersEnum.equals))).ShouldBe("maria");
            (await FilteredAsync(Filter("UserStatusId", "null", DataTableFiltersEnum.equals))).ShouldBe("eleni nikos");
            (await FilteredAsync(Filter("UserStatusId", "null", DataTableFiltersEnum.notEquals))).ShouldBe("maria");
        }

        // A member has no member.
        [Fact]
        public async Task A_filter_by_member_breaks()
        {
            (await PostAsync<DataTableDto<UserDto>>(Admin, Url, Grid<UserDto>(Filter("UserId", _maria.Id.ToString(), DataTableFiltersEnum.equals)))).ShouldBreak();
        }

        [Fact]
        public async Task In_keeps_the_rows_with_any_of_the_values()
        {
            (await FilteredAsync(Filter("UserName", ["maria", "nikos"], DataTableFiltersEnum.@in))).ShouldBe("maria nikos");
            (await FilteredAsync(Filter("UserStatusId", [_active.Id.ToString()], DataTableFiltersEnum.@in))).ShouldBe("maria");
            (await FilteredAsync(Filter("UserName", Array.Empty<string>(), DataTableFiltersEnum.@in))).ShouldBe("eleni maria nikos");
        }

        [Fact]
        public async Task Between_keeps_the_rows_inside()
        {
            (await FilteredAsync(Filter("CreatedOn", ["2025-01-15", "2025-02-15"], DataTableFiltersEnum.between))).ShouldBe("nikos");
            (await FilteredAsync(Filter("CreatedOn", ["2025-01-15"], DataTableFiltersEnum.between))).ShouldBe("eleni maria nikos");
        }

        // Any of the roles that are picked. The ids are compared letter for letter, in small ones.
        [Fact]
        public async Task It_filters_by_role()
        {
            string trainer = _trainer.Id.ToString();
            string member = _member.Id.ToString();

            (await FilteredAsync(Filter("roleId", [trainer], DataTableFiltersEnum.custom))).ShouldBe("nikos");
            (await FilteredAsync(Filter("roleId", [member], DataTableFiltersEnum.custom))).ShouldBe("eleni maria");
            (await FilteredAsync(Filter("roleId", [trainer, member], DataTableFiltersEnum.custom))).ShouldBe("eleni maria nikos");
            (await FilteredAsync(Filter("roleId", ["x"], DataTableFiltersEnum.custom))).ShouldBe("");
            (await FilteredAsync(Filter("roleId", [trainer.ToUpperInvariant()], DataTableFiltersEnum.custom))).ShouldBe("");
            (await FilteredAsync(Filter("roleId", Array.Empty<string>(), DataTableFiltersEnum.custom))).ShouldBe("eleni maria nikos");
            // Under another name it is nothing the server knows.
            (await FilteredAsync(Filter("RoleId", [trainer], DataTableFiltersEnum.custom))).ShouldBe("eleni maria nikos");
        }

        [Fact]
        public async Task Several_filters_keep_the_rows_all_of_them_keep()
        {
            (await FilteredAsync(Filter("lastName", "papa"), Filter("roleId", [_member.Id.ToString()], DataTableFiltersEnum.custom))).ShouldBe("maria");
        }

        // A kind the server does not know.
        [Theory]
        [InlineData(DataTableFiltersEnum.startsWith)]
        [InlineData(DataTableFiltersEnum.gt)]
        [InlineData(DataTableFiltersEnum.dateIs)]
        public async Task Another_kind_of_filter_is_none(DataTableFiltersEnum type)
        {
            (await FilteredAsync(Filter("userName", "maria", type))).ShouldBe("eleni maria nikos");
        }

        [Fact]
        public async Task A_filter_by_something_that_is_no_column_breaks()
        {
            (await PostAsync<DataTableDto<UserDto>>(Admin, Url, Grid<UserDto>(Filter("other", "x")))).ShouldBreak();
        }

        [Fact]
        public async Task No_rows_are_an_empty_page()
        {
            DataTableDto<UserDto> grid = await GridAsync(Grid<UserDto>(Filter("userName", "none")));

            grid.Data!.ShouldBeEmpty();
            grid.PageCount.ShouldBe(0);
        }

        // Json that says null is refused before it gets here.
        [Fact]
        public async Task A_filter_with_no_list_at_all_is_no_filter()
        {
            UsersController controller = Direct.Controller<UsersController>(Services, "Users", ["Users_View"]);

            foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.@in, DataTableFiltersEnum.between })
            {
                ApiResponse<DataTableDto<UserDto>> response = await controller.GetDataTable(Grid<UserDto>(new DataTableFilterDto { FieldName = "UserName", FilterType = type, Values = null! }));

                response.Data.ShouldNotBeNull().TotalRecords.ShouldBe(3, type.ToString());
            }
        }
    }
}
