using API.Controllers;
using API.Tests.Host;
using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Lookup;
using Core.Dtos.WorkoutPlanRule;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The rules plans are trained by: how many weeks, and how often in each.
    /// </summary>
    public sealed class WorkoutPlanRulesControllerTests : ApiTest
    {
        private const string Url = "api/WorkoutPlanRules";

        private readonly User _maria;
        private readonly User _admin;

        public WorkoutPlanRulesControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User();
            _admin = Seed.User();
        }

        private Caller Admin
        {
            get { return Caller.Of(_admin, "WorkoutPlanRules_View", "WorkoutPlanRules_Add", "WorkoutPlanRules_Edit", "WorkoutPlanRules_Delete"); }
        }

        // Every permission on the rules but the one.
        private Caller Without(string action)
        {
            return Caller.Of(_admin, new[] { "View", "Add", "Edit", "Delete" }.Where(x => x != action).Select(x => "WorkoutPlanRules_" + x).ToArray());
        }

        private List<WorkoutPlanRule> All()
        {
            return Database.Read(db => db.WorkoutPlanRules.Include(x => x.Weeks).OrderBy(x => x.Id).ToList());
        }

        private static string Describe(IEnumerable<WorkoutPlanRuleWeek> weeks)
        {
            return string.Join(", ", weeks.OrderBy(x => x.WeekNumber).Select(x => $"{x.WeekNumber}: {x.MaxRecordings} {x.Message}"));
        }

        private WorkoutPlanRecording Running(WorkoutPlanRule rule, int minutesAgo = 5, bool isStopped = false)
        {
            DateTime startedOn = DateTime.UtcNow.AddMinutes(-minutesAgo);

            return Seed.Recording(Seed.Plan(_maria, rule, 1), startedOn, completedOn: isStopped ? startedOn.AddSeconds(1) : null);
        }


        // ================================================================
        // Reading one
        // ================================================================

        private Task<ApiResult<WorkoutPlanRuleDto>> GetAsync(Caller caller, object id)
        {
            return GetAsync<WorkoutPlanRuleDto>(caller, $"{Url}/{id}");
        }

        [Fact]
        public async Task A_rule_is_read_with_its_weeks_in_order()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners");
            Database.Change(db =>
            {
                WorkoutPlanRule stored = db.WorkoutPlanRules.Single();
                stored.AwayGapDays = 20;
                stored.CreatedOn = new DateTime(2025, 1, 2);
                foreach (int number in new[] { 3, 1, 2 })
                    stored.Weeks.Add(new WorkoutPlanRuleWeek { WeekNumber = number, MaxRecordings = number + 1, Message = "Week " + number });
            });
            Seed.Plan(_maria, rule, 1);
            Seed.Plan(_admin, rule, 1);
            Seed.Plan(_admin);

            WorkoutPlanRuleDto dto = (await GetAsync(Admin, rule.Id)).Value;

            dto.Id.ShouldBe(rule.Id);
            dto.Name.ShouldBe("Beginners");
            dto.AwayGapDays.ShouldBe(20);
            dto.CreatedOn.ShouldBe(new DateTime(2025, 1, 2));
            dto.WeekCount.ShouldBe(3);
            dto.WorkoutPlanCount.ShouldBe(2);
            dto.IsLocked.ShouldBeFalse();
            dto.Weeks.Select(x => $"{x.WeekNumber} {x.MaxRecordings} {x.Message} {x.WorkoutPlanRuleId == rule.Id}").ShouldBe(["1 2 Week 1 True", "2 3 Week 2 True", "3 4 Week 3 True"]);
        }

        [Fact]
        public async Task Reading_one_needs_the_view_permission()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);

            (await GetAsync(Without("View"), rule.Id)).ShouldFail(English.NotAuthorized);
            (await GetAsync(Caller.Of(_admin, "WorkoutPlanRules_View"), rule.Id)).ShouldSucceed();
        }

        [Theory]
        [InlineData("99")]
        [InlineData("0")]
        [InlineData("x")]
        [InlineData("1.5")]
        [InlineData("99999999999")]
        public async Task A_rule_that_is_not_there_is_not_found(string id)
        {
            Seed.Rule("Beginners", 1);

            (await GetAsync(Admin, id)).ShouldFail(English.NotFound("WorkoutPlanRule"));
            (await PutAsync<WorkoutPlanRule>(Admin, $"{Url}/{id}", new { name = "Changed" })).ShouldFail(English.NotFound("WorkoutPlanRule"));
            (await DeleteAsync<WorkoutPlanRule>(Admin, $"{Url}/{id}")).ShouldFail(English.NotFound("WorkoutPlanRule"));

            All().Single().Name.ShouldBe("Beginners");
        }

        // Somebody is training by it now: started less than three hours ago and not stopped.
        [Theory]
        [InlineData(5, false, true)]
        [InlineData(179, false, true)]
        [InlineData(181, false, false)]
        [InlineData(5, true, false)]
        public async Task A_rule_is_locked_while_somebody_trains_by_it(int minutesAgo, bool isStopped, bool isLocked)
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);
            WorkoutPlanRule other = Seed.Rule("Others", 1);
            Running(rule, minutesAgo, isStopped);

            (await GetAsync(Admin, rule.Id)).Value.IsLocked.ShouldBe(isLocked);
            (await GetAsync(Admin, other.Id)).Value.IsLocked.ShouldBeFalse();

            List<WorkoutPlanRuleDto> rows = (await GridAsync()).Data!.ToList();
            rows.Single(x => x.Id == rule.Id).IsLocked.ShouldBe(isLocked);
            rows.Single(x => x.Id == other.Id).IsLocked.ShouldBeFalse();

            (await DeleteAsync<WorkoutPlanRule>(Admin, $"{Url}/{other.Id}")).ShouldSucceed(English.Deleted("WorkoutPlanRule"));
            ApiResult<WorkoutPlanRule> deleted = await DeleteAsync<WorkoutPlanRule>(Admin, $"{Url}/{rule.Id}");
            if (isLocked)
                deleted.ShouldFail(English.Of(TranslationKeys.The_rule_is_in_use_by_a_running_recording));
            else
                deleted.ShouldSucceed(English.Deleted("WorkoutPlanRule"));
        }

        // The recording is of a plan that is gone, or of one that follows no rule any more.
        [Fact]
        public async Task A_recording_of_no_plan_or_of_one_with_no_rule_locks_nothing()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);
            Running(rule);
            Seed.Recording(Seed.Plan(_maria), DateTime.UtcNow.AddMinutes(-5));
            Database.Execute("UPDATE WorkoutPlanRecordings SET WorkoutPlanId = NULL WHERE Id = 1");

            (await GetAsync(Admin, rule.Id)).Value.IsLocked.ShouldBeFalse();
            (await GridAsync()).Data!.Single().IsLocked.ShouldBeFalse();
        }


        // ================================================================
        // Writing a new one
        // ================================================================

        private static object Week(int number, int max = 1, string message = "")
        {
            return new { weekNumber = number, maxRecordings = max, message };
        }

        private Task<ApiResult<List<WorkoutPlanRule>>> AddAsync(Caller caller, params object[] rules)
        {
            return PostAsync<List<WorkoutPlanRule>>(caller, Url, rules);
        }

        [Fact]
        public async Task Rules_are_written_with_their_weeks()
        {
            DateTime before = DateTime.UtcNow;

            ApiResult<List<WorkoutPlanRule>> result = await AddAsync(Admin,
                new { name = "Beginners", awayGapDays = 20, weeks = new[] { Week(1, 2, "Easy"), Week(2, 3, "Harder") } },
                new { name = "Others" });

            result.ShouldSucceed(English.Of(TranslationKeys._0_created_successfully, "WorkoutPlanRule"));
            result.Value.Select(x => x.Name).ShouldBe(["Beginners", "Others"]);

            List<WorkoutPlanRule> rules = All();
            rules.Select(x => x.Name).ShouldBe(["Beginners", "Others"]);
            rules[0].Id.ShouldBe(result.Value[0].Id);
            rules[0].AwayGapDays.ShouldBe(20);
            rules[0].CreatedBy_Id.ShouldBe(_admin.Id.ToString());
            rules[0].CreatedOn.ShouldBeInRange(before, DateTime.UtcNow);
            Describe(rules[0].Weeks).ShouldBe("1: 2 Easy, 2: 3 Harder");
            rules[1].AwayGapDays.ShouldBeNull();
            rules[1].Weeks.ShouldBeEmpty();
        }

        [Fact]
        public async Task Writing_one_needs_the_add_permission()
        {
            (await AddAsync(Without("Add"), new { name = "Beginners" })).ShouldFail(English.NotAuthorized);

            All().ShouldBeEmpty();
            (await AddAsync(Caller.WithNoId("WorkoutPlanRules_Add"), new { name = "Beginners" })).ShouldSucceed(English.Of(TranslationKeys._0_created_successfully, "WorkoutPlanRule"));
            All().Single().CreatedBy_Id.ShouldBe("");
        }

        [Fact]
        public async Task None_at_all_is_taken_for_done()
        {
            ApiResult<List<WorkoutPlanRule>> result = await AddAsync(Admin);

            result.ShouldSucceed(English.Of(TranslationKeys._0_created_successfully, "WorkoutPlanRule"));
            result.Value.ShouldBeEmpty();
        }

        // Letter for letter: another case is another name.
        [Theory]
        [InlineData("Beginners", false)]
        [InlineData("beginners", true)]
        [InlineData(" Beginners", true)]
        [InlineData("Beginner", true)]
        public async Task A_name_there_is_already_is_refused(string name, bool isAllowed)
        {
            Seed.Rule("Beginners", 1);
            WorkoutPlanRule other = Seed.Rule("Others", 1);

            ApiResult<List<WorkoutPlanRule>> added = await AddAsync(Admin, new { name });
            if (isAllowed)
            {
                added.ShouldSucceed(English.Of(TranslationKeys._0_created_successfully, "WorkoutPlanRule"));
                return;
            }

            added.ShouldFail(English.Of(TranslationKeys._0_already_exists, name));
            (await PutAsync<WorkoutPlanRule>(Admin, $"{Url}/{other.Id}", new { name })).ShouldFail(English.Of(TranslationKeys._0_already_exists, name));
            All().Select(x => x.Name).ShouldBe(["Beginners", "Others"]);
        }

        [Fact]
        public async Task One_rule_that_is_refused_refuses_them_all()
        {
            Seed.Rule("Beginners", 1);

            (await AddAsync(Admin, new { name = "New" }, new { name = "Beginners" })).ShouldFail(English.Of(TranslationKeys._0_already_exists, "Beginners"));
            (await AddAsync(Admin, new { name = "New" }, new { name = "Newer", awayGapDays = 0 })).ShouldFail(English.Invalid);

            All().Count.ShouldBe(1);
        }

        // They are held against those that are there already, and the database against each other.
        [Fact]
        public async Task The_same_name_or_week_twice_in_one_request_breaks()
        {
            (await AddAsync(Admin, new { name = "Beginners" }, new { name = "Beginners" })).ShouldBreak();
            (await AddAsync(Admin, new { name = "Beginners", weeks = new[] { Week(1), Week(1) } })).ShouldBreak();

            All().ShouldBeEmpty();
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(1, true)]
        [InlineData(365, true)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(366, false)]
        public async Task Away_is_between_a_day_and_a_year(int? days, bool isAllowed)
        {
            WorkoutPlanRule rule = Seed.Rule("Others", 1);

            ApiResult<List<WorkoutPlanRule>> added = await AddAsync(Admin, new { name = "Beginners", awayGapDays = days });
            ApiResult<WorkoutPlanRule> changed = await PutAsync<WorkoutPlanRule>(Admin, $"{Url}/{rule.Id}", new { name = "Others", awayGapDays = days });

            if (isAllowed)
            {
                added.ShouldSucceed(English.Of(TranslationKeys._0_created_successfully, "WorkoutPlanRule"));
                changed.ShouldSucceed(English.Updated("WorkoutPlanRule"));
                All().ShouldAllBe(x => x.AwayGapDays == days);
                return;
            }

            added.ShouldFail(English.Invalid);
            changed.ShouldFail(English.Invalid);
            All().Single().AwayGapDays.ShouldBeNull();
        }

        // The message is the one for something left out.
        [Theory]
        [InlineData(0, 1, "WeekNumber")]
        [InlineData(16, 1, "WeekNumber")]
        [InlineData(1, 0, "MaxRecordings")]
        [InlineData(1, 6, "MaxRecordings")]
        public async Task A_week_is_one_of_fifteen_and_has_up_to_five(int number, int max, string field)
        {
            WorkoutPlanRule rule = Seed.Rule("Others", 1);

            (await AddAsync(Admin, new { name = "Beginners", weeks = new[] { Week(number, max) } })).ShouldBeBad(English.Required(field));
            (await PutAsync<WorkoutPlanRule>(Admin, $"{Url}/{rule.Id}", new { name = "Others", weeks = new[] { Week(number, max) } })).ShouldBeBad(English.Required(field));
        }

        [Fact]
        public async Task The_last_week_and_the_most_recordings_are_allowed()
        {
            (await AddAsync(Admin, new { name = "Beginners", weeks = new[] { Week(1, 5), Week(15, 1) } })).ShouldSucceed(English.Of(TranslationKeys._0_created_successfully, "WorkoutPlanRule"));
        }

        [Fact]
        public async Task A_rule_needs_a_name()
        {
            WorkoutPlanRule rule = Seed.Rule("Others", 1);

            (await AddAsync(Admin, new { name = "" })).ShouldBeBad(English.Required("Name"));
            (await PutAsync<WorkoutPlanRule>(Admin, $"{Url}/{rule.Id}", new { name = "" })).ShouldBeBad(English.Required("Name"));
        }


        // ================================================================
        // Changing one
        // ================================================================

        private Task<ApiResult<WorkoutPlanRule>> ChangeAsync(WorkoutPlanRule rule, string name, params object[] weeks)
        {
            return PutAsync<WorkoutPlanRule>(Admin, $"{Url}/{rule.Id}", new { id = rule.Id, name, awayGapDays = 30, weeks });
        }

        // A week that stays keeps its row; one that is left out goes, and a new one is added.
        [Fact]
        public async Task The_weeks_become_the_ones_that_are_sent()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1, 2, 3);
            WorkoutPlanRule other = Seed.Rule("Others", 4);
            int idOfSecond = rule.Weeks.Single(x => x.WeekNumber == 2).Id;

            ApiResult<WorkoutPlanRule> result = await ChangeAsync(rule, "Changed", Week(4, 5, "New"), Week(2, 4, "Kept"));

            result.ShouldSucceed(English.Updated("WorkoutPlanRule"));
            result.Value.Name.ShouldBe("Changed");

            WorkoutPlanRule changed = All().Single(x => x.Id == rule.Id);
            changed.Name.ShouldBe("Changed");
            changed.AwayGapDays.ShouldBe(30);
            Describe(changed.Weeks).ShouldBe("2: 4 Kept, 4: 5 New");
            changed.Weeks.Single(x => x.WeekNumber == 2).Id.ShouldBe(idOfSecond);
            changed.CreatedOn.ShouldBe(rule.CreatedOn);
            Describe(All().Single(x => x.Id == other.Id).Weeks).ShouldBe("1: 4 Message 1");
            Database.Read(db => db.WorkoutPlanRuleWeeks.Count()).ShouldBe(3);
        }

        [Fact]
        public async Task No_weeks_leave_none()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1, 2);

            (await ChangeAsync(rule, "Beginners")).ShouldSucceed(English.Updated("WorkoutPlanRule"));

            All().Single().Weeks.ShouldBeEmpty();
        }

        // The last one counts.
        [Fact]
        public async Task A_week_sent_twice_is_one_week()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);

            (await ChangeAsync(rule, "Beginners", Week(1, 2, "First"), Week(1, 3, "Second"), Week(2, 4, "Third"), Week(2, 5, "Fourth"))).ShouldSucceed(English.Updated("WorkoutPlanRule"));

            Describe(All().Single().Weeks).ShouldBe("1: 3 Second, 2: 5 Fourth");
        }

        [Fact]
        public async Task A_rule_keeps_its_own_name()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);

            (await ChangeAsync(rule, "Beginners", Week(1))).ShouldSucceed(English.Updated("WorkoutPlanRule"));
            // By the address: whatever id the body has.
            (await PutAsync<WorkoutPlanRule>(Admin, $"{Url}/{rule.Id}", new { id = 77, name = "Beginners" })).ShouldSucceed(English.Updated("WorkoutPlanRule"));
        }

        // It is read when a workout starts, and a running one keeps its week.
        [Fact]
        public async Task A_rule_may_be_changed_while_somebody_trains_by_it()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1, 2);
            WorkoutPlanRecording recording = Running(rule);

            (await ChangeAsync(rule, "Changed", Week(2))).ShouldSucceed(English.Updated("WorkoutPlanRule"));

            Database.Read(db => db.WorkoutPlanRecordings.Single()).WeekNumber.ShouldBe(recording.WeekNumber);
            Database.Read(db => db.WorkoutPlans.Single()).CurrentWeek.ShouldBe(1);
        }

        [Fact]
        public async Task Changing_one_needs_the_edit_permission()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);

            (await PutAsync<WorkoutPlanRule>(Without("Edit"), $"{Url}/{rule.Id}", new { name = "Changed" })).ShouldFail(English.NotAuthorized);

            All().Single().Name.ShouldBe("Beginners");
        }

        // A request cannot leave it out; the code allows for it all the same.
        [Fact]
        public async Task A_week_with_no_message_has_an_empty_one()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);
            WorkoutPlanRulesController controller = Direct.Controller<WorkoutPlanRulesController>(Services, "WorkoutPlanRules", ["WorkoutPlanRules_Edit"]);
            WorkoutPlanRuleDto dto = new WorkoutPlanRuleDto
            {
                Name = "Beginners",
                Weeks = [new WorkoutPlanRuleWeekDto { WeekNumber = 1, Message = null! }, new WorkoutPlanRuleWeekDto { WeekNumber = 2, Message = null! }]
            };

            ActionResult<ApiResponse<WorkoutPlanRule>> response = await controller.Put(rule.Id.ToString(), dto);

            response.Value.ShouldNotBeNull().IsSucceed.ShouldBeTrue();
            Describe(All().Single().Weeks).ShouldBe("1: 1 , 2: 1 ");
        }

        [Fact]
        public async Task And_a_request_that_leaves_it_out_is_refused()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);

            (await PutAsync<WorkoutPlanRule>(Admin, $"{Url}/{rule.Id}", new { name = "Beginners", weeks = new[] { new { weekNumber = 1, maxRecordings = 1, message = (string?)null } } }))
                .ShouldBeBad("The Message field is required.");
        }


        // ================================================================
        // Deleting one
        // ================================================================

        [Fact]
        public async Task A_deleted_rule_leaves_its_plans_without_one()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1, 2);
            WorkoutPlan plan = Seed.Plan(_maria, rule, 2);

            ApiResult<WorkoutPlanRule> result = await DeleteAsync<WorkoutPlanRule>(Admin, $"{Url}/{rule.Id}");

            result.ShouldSucceed(English.Deleted("WorkoutPlanRule"));
            result.Value.Id.ShouldBe(rule.Id);
            All().ShouldBeEmpty();
            Database.Read(db => db.WorkoutPlanRuleWeeks.Count()).ShouldBe(0);
            WorkoutPlan kept = Database.Read(db => db.WorkoutPlans.Single());
            kept.Id.ShouldBe(plan.Id);
            kept.WorkoutPlanRuleId.ShouldBeNull();
            kept.CurrentWeek.ShouldBe(2);
        }

        [Fact]
        public async Task Deleting_one_needs_the_delete_permission()
        {
            WorkoutPlanRule rule = Seed.Rule("Beginners", 1);

            (await DeleteAsync<WorkoutPlanRule>(Without("Delete"), $"{Url}/{rule.Id}")).ShouldFail(English.NotAuthorized);

            All().Count.ShouldBe(1);
        }


        // ================================================================
        // The dropdown
        // ================================================================

        private async Task<LookupDto> LookupAsync(string id = "", string value = "", int skip = 0, int take = 10)
        {
            // Anybody signed in may look them up.
            return (await PostAsync<LookupDto>(Caller.Of(_maria), Url + "/Lookup", new { skip, take, filter = new { id, value } })).Value;
        }

        [Theory]
        [InlineData("", "Beginners, Αρχάριοι, Προχωρημένοι")]
        [InlineData("beg", "Beginners")]
        [InlineData("ΑΡΧΑΡ", "Αρχάριοι")]
        [InlineData("προχωρη", "Προχωρημένοι")]
        [InlineData("none", "")]
        public async Task Rules_are_found_by_part_of_their_name_in_the_order_of_it(string value, string expected)
        {
            foreach (string name in new[] { "Προχωρημένοι", "Beginners", "Αρχάριοι" })
                Seed.Rule(name, 1);

            LookupDto lookup = await LookupAsync(value: value);

            string.Join(", ", lookup.Data.Select(x => x.Value)).ShouldBe(expected);
            lookup.TotalRecords.ShouldBe(lookup.Data.Count);
        }

        [Fact]
        public async Task One_is_found_by_its_id()
        {
            WorkoutPlanRule first = Seed.Rule("First", 1);
            Seed.Rule("Second", 1);

            LookupOptionDto option = (await LookupAsync(id: first.Id.ToString())).Data.Single();

            option.Id.ShouldBe(first.Id.ToString());
            option.Value.ShouldBe("First");
            (await LookupAsync(id: "99")).Data.ShouldBeEmpty();
            // One that is no number is as if it had not been sent.
            (await LookupAsync(id: "x")).Data.Count.ShouldBe(2);
            (await LookupAsync(id: first.Id.ToString(), value: "Second")).Data.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(0, 2, "A B")]
        [InlineData(2, 2, "C")]
        [InlineData(3, 2, "")]
        [InlineData(0, 0, "")]
        public async Task A_page_counts_them_all(int skip, int take, string expected)
        {
            foreach (string name in new[] { "C", "A", "B" })
                Seed.Rule(name, 1);

            LookupDto lookup = await LookupAsync(skip: skip, take: take);

            string.Join(" ", lookup.Data.Select(x => x.Value)).ShouldBe(expected);
            lookup.TotalRecords.ShouldBe(3);
        }

        [Fact]
        public async Task A_dropdown_needs_a_filter()
        {
            (await PostAsync<LookupDto>(Caller.Of(_maria), Url + "/Lookup", new { take = 10 })).ShouldBeBad(English.Required("Filter"));
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<DataTableDto<WorkoutPlanRuleDto>> GridAsync(params DataTableFilterDto[] filters)
        {
            // Anybody signed in may list them.
            return (await PostAsync<DataTableDto<WorkoutPlanRuleDto>>(Caller.Of(_maria), Url + "/GetDataTable", Grid<WorkoutPlanRuleDto>(filters))).Value;
        }

        [Fact]
        public async Task Each_row_says_how_many_plans_follow_it_and_its_weeks_in_order()
        {
            WorkoutPlanRule first = Seed.Rule("First");
            Database.Change(db =>
            {
                foreach (int number in new[] { 2, 3, 1 })
                    db.WorkoutPlanRules.Single().Weeks.Add(new WorkoutPlanRuleWeek { WeekNumber = number, MaxRecordings = 1, Message = "" });
            });
            WorkoutPlanRule second = Seed.Rule("Second", 1);
            Seed.Plan(_maria, first, 1);
            Seed.Plan(_admin, first, 1);
            Seed.Plan(_admin);

            DataTableDto<WorkoutPlanRuleDto> grid = await GridAsync();

            grid.TotalRecords.ShouldBe(2);
            WorkoutPlanRuleDto row = grid.Data!.Single(x => x.Id == first.Id);
            row.WorkoutPlanCount.ShouldBe(2);
            row.WeekCount.ShouldBe(3);
            row.Weeks.Select(x => x.WeekNumber).ShouldBe([1, 2, 3]);
            grid.Data!.Single(x => x.Id == second.Id).WorkoutPlanCount.ShouldBe(0);
        }

        // One row alone says as much as two.
        [Fact]
        public async Task It_filters_by_name()
        {
            Seed.Plan(_maria, Seed.Rule("Beginners", 1), 1);
            Seed.Rule("Others", 1);

            WorkoutPlanRuleDto row = (await GridAsync(Filter("name", "BEG"))).Data!.Single();

            row.Name.ShouldBe("Beginners");
            row.WorkoutPlanCount.ShouldBe(1);
        }

        // SQLite hands them over in order as it happens. The code does not rely on it.
        [Fact]
        public async Task The_weeks_of_a_row_are_in_order_whatever_order_they_come_in()
        {
            OpenController controller = Direct.Controller<OpenController>(Services, "WorkoutPlanRules", []);
            WorkoutPlanRuleDto row = new WorkoutPlanRuleDto { Weeks = [new WorkoutPlanRuleWeekDto { WeekNumber = 2 }, new WorkoutPlanRuleWeekDto { WeekNumber = 3 }, new WorkoutPlanRuleWeekDto { WeekNumber = 1 }] };

            await controller.UpdateAsync([new WorkoutPlanRule()], [row]);

            row.Weeks.Select(x => x.WeekNumber).ShouldBe([1, 2, 3]);
        }

        private sealed class OpenController : WorkoutPlanRulesController
        {
            public OpenController(IDataService dataService, IMapper mapper, IStringLocalizer localizer) : base(dataService, mapper, localizer)
            {
            }

            public Task UpdateAsync(List<WorkoutPlanRule> rules, List<WorkoutPlanRuleDto> rows)
            {
                return DataTableResultUpdate(rules, rows);
            }
        }

        [Fact]
        public async Task An_empty_grid_is_an_empty_grid()
        {
            (await GridAsync()).TotalRecords.ShouldBe(0);
        }
    }
}
