using API.Controllers;
using API.Tests.Host;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.PhoneNumber;
using Core.Dtos.Subscription;
using Core.Dtos.UserStatus;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The grid every controller inherits: its sorts, its filters and its pages, as the client sends
    /// them. Through the phone numbers and the subscriptions, which have a column of every type.
    /// </summary>
    public sealed class GenericControllerGridTests : ApiTest
    {
        private const string Numbers = "api/PhoneNumbers/GetDataTable";
        private const string Subscriptions = "api/Subscriptions/GetDataTable";

        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        private readonly User _maria;
        private readonly User _nikos;
        private readonly Caller _caller;

        public GenericControllerGridTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User();
            _nikos = Seed.User();
            _caller = Caller.Of(_maria, "Users_View", "SubscriptionsAdmin_View");

            AddNumber(_maria, "30", true, 0);
            AddNumber(_nikos, "10", true, 1);
            AddNumber(_maria, "20", false, 2);
        }

        private void AddNumber(User user, string number, bool isPrimary, int days)
        {
            Database.Add(new PhoneNumber { UserId = user.Id, Number = number, IsPrimary = isPrimary, CreatedOn = SomeDay.AddDays(days), CreatedBy_FullName = isPrimary ? "Ελένη" : "Νίκος" });
        }

        private Task<ApiResult<DataTableDto<PhoneNumberDto>>> NumbersAsync(DataTableDto<PhoneNumberDto> grid)
        {
            return PostAsync<DataTableDto<PhoneNumberDto>>(_caller, Numbers, grid);
        }

        private async Task<string> FilteredAsync(params DataTableFilterDto[] filters)
        {
            DataTableDto<PhoneNumberDto> grid = Grid<PhoneNumberDto>(filters);
            grid.Sorts = [new DataTableSortDto { FieldName = "number", Order = 1 }];

            DataTableDto<PhoneNumberDto> answer = (await NumbersAsync(grid)).Value;

            // The total is of the filtered rows.
            answer.TotalRecords.ShouldBe(answer.Data!.Count());

            return string.Join(" ", answer.Data!.Select(x => x.Number));
        }

        private async Task<string> SortedAsync(params (string Field, int Order)[] sorts)
        {
            return string.Join(" ", (await NumbersAsync(SortedGrid<PhoneNumberDto>(sorts))).Value.Data!.Select(x => x.Number));
        }


        // ================================================================
        // Nothing asked
        // ================================================================

        [Fact]
        public async Task A_grid_that_asks_for_nothing_gets_every_row()
        {
            ApiResult<DataTableDto<PhoneNumberDto>> result = await NumbersAsync(Grid<PhoneNumberDto>());

            result.ShouldSucceed();
            result.Value.TotalRecords.ShouldBe(3);
            result.Value.PageCount.ShouldBe(1);
            result.Value.Data!.Select(x => x.Number).ShouldBe(["30", "10", "20"]);
        }

        // The question comes back with the answer in it.
        [Fact]
        public async Task The_grid_comes_back_as_it_was_sent_with_its_rows()
        {
            DataTableDto<PhoneNumberDto> grid = Grid<PhoneNumberDto>(Filter("number", "0"));
            grid.Rows = 2;
            grid.Page = 1;
            grid.First = 2;
            grid.PageCount = 77;
            grid.TotalRecords = 88;
            grid.Sorts = [new DataTableSortDto { FieldName = "number", Order = 1 }];

            DataTableDto<PhoneNumberDto> answer = (await NumbersAsync(grid)).Value;

            answer.Rows.ShouldBe(2);
            answer.Page.ShouldBe(1);
            answer.First.ShouldBe(2);
            answer.PageCount.ShouldBe(2);
            answer.TotalRecords.ShouldBe(3);
            answer.Sorts.Single().FieldName.ShouldBe("number");
            answer.Filters.Single().Value.ShouldBe("0");
            answer.Data!.Select(x => x.Number).ShouldBe(["30"]);
        }

        [Fact]
        public async Task No_rows_are_an_empty_page()
        {
            Database.Reset();

            DataTableDto<PhoneNumberDto> answer = (await NumbersAsync(Grid<PhoneNumberDto>())).Value;

            answer.Data!.ShouldBeEmpty();
            answer.TotalRecords.ShouldBe(0);
            answer.PageCount.ShouldBe(0);
        }


        // ================================================================
        // Pages
        // ================================================================

        [Theory]
        [InlineData(0, 2, "10 20", 2)]
        [InlineData(1, 2, "30", 2)]
        [InlineData(2, 2, "", 2)]
        [InlineData(0, 1, "10", 3)]
        [InlineData(2, 1, "30", 3)]
        [InlineData(0, 3, "10 20 30", 1)]
        [InlineData(0, 100, "10 20 30", 1)]
        // A page that makes no sense is every row, and so many pages.
        [InlineData(-1, 2, "10 20 30", 2)]
        [InlineData(0, -1, "10 20 30", -3)]
        public async Task A_page_is_so_many_rows_from_so_many_pages_on(int page, int rows, string expected, int pages)
        {
            DataTableDto<PhoneNumberDto> grid = SortedGrid<PhoneNumberDto>(("number", 1));
            grid.Page = page;
            grid.Rows = rows;

            DataTableDto<PhoneNumberDto> answer = (await NumbersAsync(grid)).Value;

            string.Join(" ", answer.Data!.Select(x => x.Number)).ShouldBe(expected);
            answer.TotalRecords.ShouldBe(3);
            answer.PageCount.ShouldBe(pages);
        }

        // Three rows in pages of none is no number of pages at all. .NET 8 makes that the least
        // number there is; .NET 9 makes it the greatest, so this is the test to change with it.
        [Fact]
        public async Task A_page_of_no_rows_has_none()
        {
            DataTableDto<PhoneNumberDto> grid = Grid<PhoneNumberDto>();
            grid.Rows = 0;

            DataTableDto<PhoneNumberDto> answer = (await NumbersAsync(grid)).Value;

            answer.Data!.ShouldBeEmpty();
            answer.TotalRecords.ShouldBe(3);
            answer.PageCount.ShouldBe(int.MinValue);
        }


        // ================================================================
        // Sorts
        // ================================================================

        [Theory]
        [InlineData("number", 1, "10 20 30")]
        [InlineData("number", -1, "30 20 10")]
        [InlineData("Number", 1, "10 20 30")]
        [InlineData("number", 5, "10 20 30")]
        [InlineData("createdOn", 1, "30 10 20")]
        [InlineData("createdOn", -1, "20 10 30")]
        [InlineData("id", -1, "20 10 30")]
        // Neither up nor down is as they come.
        [InlineData("number", 0, "30 10 20")]
        public async Task One_column_sorts_up_or_down(string field, int order, string expected)
        {
            (await SortedAsync((field, order))).ShouldBe(expected);
        }

        // Two are primary; the second column decides between them.
        [Theory]
        [InlineData("isPrimary", 1, "number", "20 10 30")]
        [InlineData("isPrimary", -1, "number", "30 10 20")]
        [InlineData("isPrimary", -1, "createdOn", "10 30 20")]
        public async Task A_second_column_decides_where_the_first_is_equal(string first, int order, string second, string expected)
        {
            (await SortedAsync((first, order), (second, order))).ShouldBe(expected);
        }

        // The second column goes the way of the first, whatever it asks for itself.
        [Theory]
        [InlineData(1, "20 10 30")]
        [InlineData(-1, "30 10 20")]
        public async Task Every_column_goes_the_way_of_the_first(int order, string expected)
        {
            (await SortedAsync(("isPrimary", order), ("number", 1))).ShouldBe(expected);
            (await SortedAsync(("isPrimary", order), ("number", -1))).ShouldBe(expected);
            (await SortedAsync(("isPrimary", order), ("number", 0))).ShouldBe(expected);
        }

        [Fact]
        public async Task When_the_first_goes_no_way_none_of_them_does()
        {
            (await SortedAsync(("isPrimary", 0), ("number", 1))).ShouldBe("30 10 20");
        }

        [Fact]
        public async Task Three_columns_decide_one_after_another()
        {
            AddNumber(_nikos, "10", true, 5);

            (await SortedAsync(("isPrimary", -1), ("number", 1), ("createdOn", 1))).ShouldBe("30 10 10 20");
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("")]
        [InlineData("user.missing")]
        public async Task A_sort_by_a_column_that_is_not_there_breaks(string field)
        {
            (await NumbersAsync(SortedGrid<PhoneNumberDto>((field, 1)))).ShouldBreak();
        }

        [Fact]
        public async Task A_second_sort_by_a_column_that_is_not_there_breaks()
        {
            (await NumbersAsync(SortedGrid<PhoneNumberDto>(("number", 1), ("missing", 1)))).ShouldBreak();
            (await NumbersAsync(SortedGrid<PhoneNumberDto>(("number", 1), ("", 1)))).ShouldBreak();
        }

        // A name of one letter is still a name.
        [Fact]
        public async Task A_column_may_be_named_by_a_path()
        {
            (await SortedAsync(("user.userName", -1), ("number", 1))).ShouldBe("10 30 20");
        }


        // ================================================================
        // Filters
        // ================================================================

        [Theory]
        [InlineData("number", "1", "10")]
        [InlineData("number", "0", "10 20 30")]
        [InlineData("Number", "2", "20")]
        [InlineData("number", "9", "")]
        [InlineData("createdBy_FullName", "ελενη", "10 30")]
        [InlineData("createdBy_FullName", "ΝΙΚΟΣ", "20")]
        // A box that was cleared.
        [InlineData("number", "", "10 20 30")]
        [InlineData("number", null, "10 20 30")]
        public async Task Contains_keeps_the_rows_with_the_text(string field, string? value, string expected)
        {
            (await FilteredAsync(Filter(field, value))).ShouldBe(expected);
        }

        [Theory]
        [InlineData("number", "10", "10")]
        [InlineData("number", "1", "")]
        [InlineData("number", "", "")]
        [InlineData("isPrimary", "true", "10 30")]
        [InlineData("isPrimary", "False", "20")]
        // No value is no filter; the word null is the value null.
        [InlineData("number", null, "10 20 30")]
        [InlineData("number", "null", "")]
        public async Task Equals_keeps_the_rows_with_the_value(string field, string? value, string expected)
        {
            (await FilteredAsync(Filter(field, value, DataTableFiltersEnum.equals))).ShouldBe(expected);
        }

        [Theory]
        [InlineData("number", "10", "20 30")]
        [InlineData("number", "9", "10 20 30")]
        [InlineData("isPrimary", "true", "20")]
        [InlineData("number", null, "10 20 30")]
        [InlineData("number", "null", "10 20 30")]
        public async Task NotEquals_keeps_every_other_row(string field, string? value, string expected)
        {
            (await FilteredAsync(Filter(field, value, DataTableFiltersEnum.notEquals))).ShouldBe(expected);
        }

        [Fact]
        public async Task A_member_is_filtered_by_their_id()
        {
            (await FilteredAsync(Filter("UserId", _maria.Id.ToString(), DataTableFiltersEnum.equals))).ShouldBe("20 30");
            (await FilteredAsync(Filter("UserId", Guid.NewGuid().ToString(), DataTableFiltersEnum.equals))).ShouldBe("");
        }

        // Only under exactly that name: any other id is text to it, and text is not an id.
        [Theory]
        [InlineData("userId")]
        [InlineData("USERID")]
        public async Task A_member_under_another_spelling_breaks(string field)
        {
            (await NumbersAsync(Grid<PhoneNumberDto>(Filter(field, _maria.Id.ToString(), DataTableFiltersEnum.equals)))).ShouldBreak();
        }

        [Theory]
        [InlineData("not an id")]
        [InlineData("null")]
        [InlineData("")]
        public async Task A_member_that_is_not_an_id_breaks(string value)
        {
            (await NumbersAsync(Grid<PhoneNumberDto>(Filter("UserId", value, DataTableFiltersEnum.equals)))).ShouldBreak();
        }

        [Theory]
        [InlineData("10 30", "10 30")]
        [InlineData("10", "10")]
        [InlineData("10 9", "10")]
        [InlineData("9", "")]
        // None chosen is no filter.
        [InlineData("", "10 20 30")]
        public async Task In_keeps_the_rows_with_any_of_the_values(string values, string expected)
        {
            string[] list = values.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            (await FilteredAsync(Filter("number", list, DataTableFiltersEnum.@in))).ShouldBe(expected);
        }

        [Fact]
        public async Task In_takes_ids()
        {
            (await FilteredAsync(Filter("userId", [_nikos.Id.ToString()], DataTableFiltersEnum.@in))).ShouldBe("10");
            (await FilteredAsync(Filter("userId", [_nikos.Id.ToString(), _maria.Id.ToString()], DataTableFiltersEnum.@in))).ShouldBe("10 20 30");
        }

        [Theory]
        [InlineData("2031-03-05T00:00:00.000Z", "2031-03-06T00:00:00.000Z", "10 30")]
        [InlineData("2031-03-06", "2031-03-07", "10 20")]
        [InlineData("2031-03-08", "2031-03-09", "")]
        public async Task Between_keeps_the_rows_inside(string from, string to, string expected)
        {
            (await FilteredAsync(Filter("createdOn", [from, to], DataTableFiltersEnum.between))).ShouldBe(expected);
        }

        // It takes two edges; with any other number it is passed over.
        [Theory]
        [InlineData("")]
        [InlineData("2031-03-08")]
        [InlineData("2031-03-08 2031-03-09 2031-03-10")]
        public async Task Between_with_other_than_two_dates_is_no_filter(string values)
        {
            string[] list = values.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            (await FilteredAsync(Filter("createdOn", list, DataTableFiltersEnum.between))).ShouldBe("10 20 30");
        }

        // The client has them all in its list; the server knows five.
        [Theory]
        [InlineData(DataTableFiltersEnum.startsWith)]
        [InlineData(DataTableFiltersEnum.notContains)]
        [InlineData(DataTableFiltersEnum.endsWith)]
        [InlineData(DataTableFiltersEnum.notIn)]
        [InlineData(DataTableFiltersEnum.lt)]
        [InlineData(DataTableFiltersEnum.lte)]
        [InlineData(DataTableFiltersEnum.gt)]
        [InlineData(DataTableFiltersEnum.gte)]
        [InlineData(DataTableFiltersEnum.dateIs)]
        [InlineData(DataTableFiltersEnum.dateIsNot)]
        [InlineData(DataTableFiltersEnum.dateBefore)]
        [InlineData(DataTableFiltersEnum.dateAfter)]
        [InlineData(DataTableFiltersEnum.custom)]
        public async Task A_kind_of_filter_the_server_does_not_know_is_no_filter(DataTableFiltersEnum type)
        {
            DataTableFilterDto filter = Filter("number", "10", type);
            filter.Values = ["10", "20"];

            (await FilteredAsync(filter)).ShouldBe("10 20 30");
        }

        [Fact]
        public async Task Several_filters_keep_the_rows_all_of_them_keep()
        {
            (await FilteredAsync(
                Filter("number", "0"),
                Filter("isPrimary", "true", DataTableFiltersEnum.equals),
                Filter("number", ["10", "20"], DataTableFiltersEnum.@in))).ShouldBe("10");
        }

        [Theory]
        [InlineData("missing", "x", DataTableFiltersEnum.contains)]
        [InlineData("missing", "x", DataTableFiltersEnum.equals)]
        [InlineData("missing", "x", DataTableFiltersEnum.notEquals)]
        [InlineData("createdOn", "x", DataTableFiltersEnum.contains)]
        [InlineData("isPrimary", "x", DataTableFiltersEnum.equals)]
        [InlineData("isPrimary", "null", DataTableFiltersEnum.equals)]
        [InlineData("id", "", DataTableFiltersEnum.equals)]
        [InlineData("", "x", DataTableFiltersEnum.contains)]
        // Even with nothing to filter by, the name is looked at.
        [InlineData("", null, DataTableFiltersEnum.contains)]
        public async Task A_filter_that_makes_no_sense_breaks(string field, string? value, DataTableFiltersEnum type)
        {
            (await NumbersAsync(Grid<PhoneNumberDto>(Filter(field, value, type)))).ShouldBreak();
        }

        [Theory]
        [InlineData("createdOn", "x 2031-03-05", DataTableFiltersEnum.between)]
        [InlineData("createdOn", "2031-03-05 2031-03-04", DataTableFiltersEnum.between)]
        [InlineData("number", "2031-03-05 2031-03-06", DataTableFiltersEnum.between)]
        [InlineData("userId", "x", DataTableFiltersEnum.@in)]
        [InlineData("id", "x", DataTableFiltersEnum.@in)]
        [InlineData("missing", "x", DataTableFiltersEnum.@in)]
        public async Task A_filter_of_values_that_make_no_sense_breaks(string field, string values, DataTableFiltersEnum type)
        {
            (await NumbersAsync(Grid<PhoneNumberDto>(Filter(field, values.Split(' '), type)))).ShouldBreak();
        }


        // ================================================================
        // Columns that may be empty
        // ================================================================

        private async Task<string> AmountsAsync(params DataTableFilterDto[] filters)
        {
            DataTableDto<SubscriptionDto> grid = Grid<SubscriptionDto>(filters);
            grid.Sorts = [new DataTableSortDto { FieldName = "amount", Order = 1 }];

            DataTableDto<SubscriptionDto> answer = (await PostAsync<DataTableDto<SubscriptionDto>>(_caller, Subscriptions, grid)).Value;

            return string.Join(" ", answer.Data!.Select(x => x.Amount));
        }

        [Theory]
        [InlineData("5", DataTableFiltersEnum.equals, "1")]
        [InlineData("null", DataTableFiltersEnum.equals, "2 3")]
        [InlineData("", DataTableFiltersEnum.equals, "2 3")]
        [InlineData("5", DataTableFiltersEnum.notEquals, "2 3")]
        [InlineData("null", DataTableFiltersEnum.notEquals, "1")]
        public async Task A_column_that_may_be_empty_is_filtered_by_the_word_null(string value, DataTableFiltersEnum type, string expected)
        {
            Seed.Subscription(_maria, 1, change: x => x.RequestedAmount = 5);
            Seed.Subscription(_maria, 2);
            Seed.Subscription(_maria, 3);

            (await AmountsAsync(Filter("requestedAmount", value, type))).ShouldBe(expected);
        }

        [Fact]
        public async Task A_date_that_may_be_empty_is_never_between_two_others()
        {
            Seed.Subscription(_maria, 1, change: x => x.DecidedOn = SomeDay);
            Seed.Subscription(_maria, 2);

            (await AmountsAsync(Filter("decidedOn", ["2031-03-05", "2031-03-06"], DataTableFiltersEnum.between))).ShouldBe("1");
        }


        // ================================================================
        // Who may ask
        // ================================================================

        // The one thing a controller inherits without a permission: what may not be seen has to be
        // left out by the controller itself.
        [Fact]
        public async Task A_grid_asks_for_no_permission()
        {
            Seed.UserStatus(x => x.Name = "Active");

            DataTableDto<UserStatusDto> answer = (await PostAsync<DataTableDto<UserStatusDto>>(Caller.Of(_maria), "api/UserStatuses/GetDataTable", Grid<UserStatusDto>())).Value;

            answer.Data!.Single().Name.ShouldBe("Active");
        }

        // Json that says null is refused before it gets here.
        [Fact]
        public async Task A_filter_with_no_list_at_all_is_no_filter()
        {
            PhoneNumbersController controller = Direct.Controller<PhoneNumbersController>(Services, "PhoneNumbers", ["Users_View"]);

            foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.@in, DataTableFiltersEnum.between, DataTableFiltersEnum.custom })
            {
                ApiResponse<DataTableDto<PhoneNumberDto>> response = await controller.GetDataTable(Grid<PhoneNumberDto>(new DataTableFilterDto { FieldName = "Number", FilterType = type, Values = null! }));

                response.Data.ShouldNotBeNull().TotalRecords.ShouldBe(3, type.ToString());
            }
        }

        [Fact]
        public async Task A_request_that_says_so_is_refused()
        {
            (await PostAsync<DataTableDto<PhoneNumberDto>>(_caller, Numbers, new { rows = 10, filters = new[] { new { fieldName = "number", values = (string[]?)null } } })).ShouldBeBad("The Values field is required.");
        }
    }
}
