using API.Controllers;
using API.Tests.Host;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Lookup;
using Core.Dtos.TrainGroupCategory;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Mvc;

namespace API.Tests.Controllers
{
    public sealed class TrainGroupCategoriesControllerTests : ApiTest
    {
        private const string Url = "api/TrainGroupCategories";

        private readonly User _user;
        private readonly Caller _admin;
        private readonly TrainGroupCategory _reformer;
        private readonly TrainGroupCategory _yoga;

        public TrainGroupCategoriesControllerTests(ApiFixture api) : base(api)
        {
            _user = Seed.User();
            _admin = Caller.Of(_user, "TrainGroupCategories_View", "TrainGroupCategories_Add", "TrainGroupCategories_Edit", "TrainGroupCategories_Delete");
            _reformer = Seed.Category("Ρεφόρμερ", "#aabbcc");
            _yoga = Seed.Category("Yoga");
        }

        private List<string> Categories()
        {
            return Database.Read(db => db.TrainGroupCategories.OrderBy(x => x.Id).Select(x => $"{x.Name} [{x.Color}]").ToList());
        }


        // ================================================================
        // The dropdown
        // ================================================================

        private async Task<LookupDto> LookupAsync(string id, string value, int skip = 0, int take = 10)
        {
            ApiResult<LookupDto> result = await PostAsync<LookupDto>(Caller.Of(_user), Url + "/Lookup", new { skip, take, filter = new { id, value } });

            result.ShouldSucceed();

            return result.Value;
        }

        [Theory]
        [InlineData("", "Yoga Ρεφόρμερ")]
        [InlineData("ρεφορ", "Ρεφόρμερ")]
        [InlineData("YO", "Yoga")]
        [InlineData("x", "")]
        public async Task Categories_are_found_by_part_of_their_name_in_the_order_of_it(string value, string expected)
        {
            LookupDto answer = await LookupAsync("", value);

            string.Join(" ", answer.Data.Select(x => x.Value)).ShouldBe(expected);
            answer.TotalRecords.ShouldBe(answer.Data.Count);
        }

        [Fact]
        public async Task Each_comes_with_its_id_and_colour()
        {
            List<LookupOptionDto> options = (await LookupAsync("", "")).Data;

            options.Select(x => $"{x.Id} {x.Value} [{x.Color}]").ShouldBe([$"{_yoga.Id} Yoga []", $"{_reformer.Id} Ρεφόρμερ [#aabbcc]"]);
        }

        [Fact]
        public async Task One_is_found_by_its_id()
        {
            (await LookupAsync(_yoga.Id.ToString(), "")).Data.Single().Value.ShouldBe("Yoga");
            (await LookupAsync(_yoga.Id.ToString(), "ρεφ")).Data.ShouldBeEmpty();
            (await LookupAsync("99", "")).Data.ShouldBeEmpty();
            // Not a number is no id at all.
            (await LookupAsync("x", "")).Data.Count.ShouldBe(2);
        }

        [Theory]
        [InlineData(0, 1, "Yoga")]
        [InlineData(1, 1, "Ρεφόρμερ")]
        [InlineData(2, 1, "")]
        [InlineData(0, 0, "")]
        public async Task A_page_counts_them_all(int skip, int take, string expected)
        {
            LookupDto answer = await LookupAsync("", "", skip, take);

            string.Join(" ", answer.Data.Select(x => x.Value)).ShouldBe(expected);
            answer.TotalRecords.ShouldBe(2);
        }


        // ================================================================
        // Adding
        // ================================================================

        private Task<ApiResult<List<TrainGroupCategory>>> AddAsync(params object[] categories)
        {
            return PostAsync<List<TrainGroupCategory>>(_admin, Url, categories);
        }

        [Theory]
        [InlineData("#AABBCC", "#aabbcc")]
        [InlineData("#aAbB0c", "#aabb0c")]
        [InlineData("", "")]
        public async Task A_colour_is_kept_in_small_letters(string color, string expected)
        {
            (await AddAsync(new { name = "Barre", color })).ShouldSucceed(English.Updated("TrainGroupCategory"));

            Categories().Last().ShouldBe($"Barre [{expected}]");
        }

        [Fact]
        public async Task A_category_that_names_no_colour_has_none()
        {
            (await AddAsync(new { name = "Barre" })).ShouldSucceed(English.Updated("TrainGroupCategory"));

            Categories().Last().ShouldBe("Barre []");
        }

        [Theory]
        [InlineData("red")]
        [InlineData("#fff")]
        [InlineData(" ")]
        public async Task Something_that_is_not_a_colour_is_refused(string color)
        {
            (await AddAsync(new { name = "Barre", color })).ShouldBeBad(English.Invalid);

            Categories().Count.ShouldBe(2);
        }

        // However it is typed: two tabs of one name would be no use to anybody.
        [Theory]
        [InlineData("Yoga", "Yoga")]
        [InlineData("YOGA", "YOGA")]
        [InlineData("  yoga  ", "yoga")]
        [InlineData("ρεφορμερ", "ρεφορμερ")]
        [InlineData("ΡΕΦΌΡΜΕΡ", "ΡΕΦΌΡΜΕΡ")]
        public async Task A_name_there_is_already_is_refused(string name, string named)
        {
            (await AddAsync(new { name })).ShouldBeBad(English.Of(TranslationKeys._0_already_exists, named));

            Categories().Count.ShouldBe(2);
        }

        // Each is looked at against what is there, not against the others.
        [Fact]
        public async Task The_same_name_twice_in_one_go_is_taken_twice()
        {
            (await AddAsync(new { name = "Barre" }, new { name = "Barre" })).ShouldSucceed(English.Updated("TrainGroupCategory"));

            Categories().Count.ShouldBe(4);
        }

        [Fact]
        public async Task One_that_will_not_do_adds_none()
        {
            (await AddAsync(new { name = "Barre" }, new { name = "Yoga" })).ShouldBeBad(English.Of(TranslationKeys._0_already_exists, "Yoga"));

            Categories().Count.ShouldBe(2);
        }

        [Fact]
        public async Task A_category_needs_a_name_of_a_hundred_characters_at_most()
        {
            (await AddAsync(new { name = " " })).ShouldBeBad(English.Required("Name"));
            (await AddAsync(new { name = new string('x', 101) })).ShouldBeBad(English.Of(TranslationKeys._0_cannot_exceed_100_characters, "Name"));
            (await AddAsync(new { name = new string('x', 100) })).ShouldSucceed(English.Updated("TrainGroupCategory"));
        }


        // ================================================================
        // Editing
        // ================================================================

        private Task<ApiResult<TrainGroupCategory>> EditAsync(TrainGroupCategory category, string name, string color)
        {
            return PutAsync<TrainGroupCategory>(_admin, $"{Url}/{category.Id}", new { id = category.Id, name, color });
        }

        [Fact]
        public async Task A_category_is_renamed_and_recoloured()
        {
            (await EditAsync(_yoga, "Hatha", "#FF0000")).ShouldSucceed(English.Updated("TrainGroupCategory"));

            Categories().ShouldBe(["Ρεφόρμερ [#aabbcc]", "Hatha [#ff0000]"]);
        }

        [Fact]
        public async Task A_category_may_keep_its_own_name_in_another_case()
        {
            (await EditAsync(_yoga, "YOGA", "")).ShouldSucceed(English.Updated("TrainGroupCategory"));

            Categories().Last().ShouldBe("YOGA []");
        }

        [Theory]
        [InlineData("Ρεφόρμερ")]
        [InlineData(" ρεφορμερ ")]
        public async Task A_category_may_not_take_the_name_of_another(string name)
        {
            (await EditAsync(_yoga, name, "")).ShouldBeBad(English.Of(TranslationKeys._0_already_exists, name.Trim()));

            Categories().Last().ShouldBe("Yoga []");
        }


        // ================================================================
        // Deleting
        // ================================================================

        [Fact]
        public async Task A_deleted_category_leaves_its_groups_without_one()
        {
            Seed.Group(_user, x => x.TrainGroupCategoryId = _yoga.Id);

            (await DeleteAsync<TrainGroupCategory>(_admin, $"{Url}/{_yoga.Id}")).ShouldSucceed(English.Deleted("TrainGroupCategory"));

            Database.Read(db => db.TrainGroups.Single()).TrainGroupCategoryId.ShouldBeNull();
        }


        // ================================================================
        // Who may
        // ================================================================

        [Fact]
        public async Task Changing_them_needs_the_permission_and_looking_them_up_does_not()
        {
            Caller anybody = Caller.Of(_user);

            (await GetAsync<TrainGroupCategoryDto>(anybody, $"{Url}/{_yoga.Id}")).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<TrainGroupCategory>>(anybody, Url, new[] { new { name = "Barre" } })).ShouldFail(English.NotAuthorized);
            (await PutAsync<TrainGroupCategory>(anybody, $"{Url}/{_yoga.Id}", new { id = _yoga.Id, name = "Barre", color = "" })).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<TrainGroupCategory>(anybody, $"{Url}/{_yoga.Id}")).ShouldFail(English.NotAuthorized);

            (await PostAsync<DataTableDto<TrainGroupCategoryDto>>(anybody, Url + "/GetDataTable", Grid<TrainGroupCategoryDto>())).Value.TotalRecords.ShouldBe(2);
        }


        // ================================================================
        // What no request can reach
        // ================================================================

        // Json that says null is refused before it gets here, and so is a colour with spaces around it.
        [Theory]
        [InlineData(null, "")]
        [InlineData(" #AABBCC ", "#aabbcc")]
        public async Task A_colour_is_tidied_up(string? color, string expected)
        {
            TrainGroupCategoriesController controller = Direct.Controller<TrainGroupCategoriesController>(Services, "TrainGroupCategories", ["TrainGroupCategories_Add"]);

            ActionResult<ApiResponse<List<TrainGroupCategory>>> response = await controller.Post([new TrainGroupCategoryAddDto { Name = "Barre", Color = color! }]);

            response.Value.ShouldNotBeNull().IsSucceed.ShouldBeTrue();
            Categories().Last().ShouldBe($"Barre [{expected}]");
        }
    }
}
