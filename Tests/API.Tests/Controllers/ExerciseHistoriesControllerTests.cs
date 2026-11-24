using API.Controllers;
using API.Tests.Host;
using AutoMapper;
using Business.Services;
using Business.Services.Email;
using Core.Dtos.DataTable;
using Core.Dtos.ExerciseHistory;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Localization;

namespace API.Tests.Controllers
{
    /// <summary>
    /// What an exercise was before each change. Its permissions are those of the plans.
    /// </summary>
    public sealed class ExerciseHistoriesControllerTests : ApiTest
    {
        private const string Url = "api/ExerciseHistories";

        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _admin;
        private readonly WorkoutPlan _plan;
        private readonly Exercise _squat;
        private readonly ExerciseHistory _history;

        public ExerciseHistoriesControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User(x => x.Email = "maria@gym.test");
            _nikos = Seed.User();
            _admin = Seed.User();
            _plan = Seed.Plan(_maria, change: x => x.Title = "Legs");
            _squat = Seed.Exercise(_plan, x => x.Name = "Squat");
            _history = Seed.History(_squat, x => { x.Weight = "70"; x.Sets = "4"; x.Reps = "6"; x.Description = "Low"; x.GroupNumber = 2; x.GroupExerciseOrderNumber = 3; x.CreatedOn = new DateTime(2025, 1, 2); });
        }

        private Caller Admin
        {
            get { return Caller.Of(_admin, "WorkoutPlans_View", "WorkoutPlans_Add", "WorkoutPlans_Edit", "WorkoutPlans_Delete", "WorkoutPlansAdmin_View", "WorkoutPlansAdmin_Edit"); }
        }

        // What the app gives a member.
        private static Caller Member(User user)
        {
            return Caller.Of(user, "WorkoutPlans_View", "WorkoutPlans_Edit");
        }

        private ExerciseHistory TheirHistory(User user, string name, string planTitle = "Arms")
        {
            return Seed.History(Seed.Exercise(Seed.Plan(user, change: x => x.Title = planTitle), x => x.Name = name));
        }


        // ================================================================
        // Reading one
        // ================================================================

        [Fact]
        public async Task One_is_its_members_and_an_admins_to_read()
        {
            string url = $"{Url}/{_history.Id}";

            ExerciseHistoryDto dto = (await GetAsync<ExerciseHistoryDto>(Member(_maria), url)).Value;
            dto.Id.ShouldBe(_history.Id);
            dto.Name.ShouldBe("Squat");
            dto.Weight.ShouldBe("70");
            dto.Sets.ShouldBe("4");
            dto.Reps.ShouldBe("6");
            dto.Description.ShouldBe("Low");
            dto.GroupNumber.ShouldBe(2);
            dto.GroupExerciseOrderNumber.ShouldBe(3);
            dto.ExerciseId.ShouldBe(_squat.Id);
            dto.CreatedOn.ShouldBe(new DateTime(2025, 1, 2));
            dto.WorkoutPlanTitle.ShouldBe("");

            (await GetAsync<ExerciseHistoryDto>(Caller.Of(_nikos, "WorkoutPlans_View", "WorkoutPlansAdmin_View"), url)).ShouldSucceed();
            (await GetAsync<ExerciseHistoryDto>(Member(_nikos), url)).ShouldFail(English.NotFound("ExerciseHistory"));
            (await GetAsync<ExerciseHistoryDto>(Caller.Of(_nikos, "WorkoutPlans_View", "WorkoutPlansAdmin_Edit"), url)).ShouldFail(English.NotFound("ExerciseHistory"));
            (await GetAsync<ExerciseHistoryDto>(Caller.WithNoId("WorkoutPlans_View"), url)).ShouldFail(English.NotFound("ExerciseHistory"));
            (await GetAsync<ExerciseHistoryDto>(Caller.Of(_maria, "WorkoutPlans_Edit", "WorkoutPlansAdmin_View"), url)).ShouldFail(English.NotAuthorized);
        }

        [Theory]
        [InlineData("99")]
        [InlineData("x")]
        [InlineData("99999999999")]
        public async Task One_that_is_not_there_is_not_found(string id)
        {
            (await GetAsync<ExerciseHistoryDto>(Admin, $"{Url}/{id}")).ShouldFail(English.NotFound("ExerciseHistory"));
        }


        // ================================================================
        // Writing them
        // ================================================================

        private object Body(string weight)
        {
            return new { id = _history.Id, name = "Squat", sets = "4", reps = "6", weight, groupNumber = 2, groupExerciseOrderNumber = 3, exerciseId = _squat.Id, createdOn = new DateTime(2025, 1, 2) };
        }

        private string StoredWeight()
        {
            return Database.Read(db => db.ExerciseHistories.Single(x => x.Id == _history.Id)).Weight;
        }

        // Members may change their weights, and that alone would let any of them rewrite anybody's.
        [Fact]
        public async Task Changing_one_needs_the_admins_permission_and_the_one_to_edit()
        {
            (await PutAsync<ExerciseHistory>(Member(_maria), $"{Url}/{_history.Id}", Body("1"))).ShouldFail(English.NotAuthorized);
            (await PutAsync<ExerciseHistory>(Caller.Of(_admin, "WorkoutPlansAdmin_Edit", "WorkoutPlans_View"), $"{Url}/{_history.Id}", Body("1"))).ShouldFail(English.NotAuthorized);
            StoredWeight().ShouldBe("70");

            (await PutAsync<ExerciseHistory>(Caller.Of(_admin, "WorkoutPlansAdmin_Edit", "WorkoutPlans_Edit"), $"{Url}/{_history.Id}", Body("75"))).ShouldSucceed(English.Updated("ExerciseHistory"));
            StoredWeight().ShouldBe("75");
            (await PutAsync<ExerciseHistory>(Admin, $"{Url}/99", Body("1"))).ShouldFail(English.NotFound("ExerciseHistory"));
        }

        [Fact]
        public async Task Adding_and_deleting_one_need_the_permissions_of_the_plans()
        {
            object[] rows = [new { name = "Squat", sets = "4", reps = "6", weight = "72", groupNumber = 1, groupExerciseOrderNumber = 1, exerciseId = _squat.Id }];

            (await PostAsync<List<ExerciseHistory>>(Member(_maria), Url, rows)).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<ExerciseHistory>>(Admin, Url, rows)).ShouldSucceed(English.Updated("ExerciseHistory"));
            Database.Read(db => db.ExerciseHistories.Count()).ShouldBe(2);

            (await DeleteAsync<ExerciseHistory>(Member(_maria), $"{Url}/{_history.Id}")).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<ExerciseHistory>(Admin, $"{Url}/{_history.Id}")).ShouldSucceed(English.Deleted("ExerciseHistory"));
            Database.Read(db => db.ExerciseHistories.Single()).Weight.ShouldBe("72");
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<DataTableDto<ExerciseHistoryDto>> GridAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            ApiResult<DataTableDto<ExerciseHistoryDto>> result = await PostAsync<DataTableDto<ExerciseHistoryDto>>(caller, Url + "/GetDataTable", Grid<ExerciseHistoryDto>(filters));

            result.ShouldSucceed();
            result.Value.TotalRecords.ShouldBe(result.Value.Data!.Count());

            return result.Value;
        }

        private async Task<string> NamesAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            return string.Join(" ", (await GridAsync(caller, filters)).Data!.Select(x => x.Name).Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task An_admin_sees_everybodys_and_a_member_that_of_their_plans()
        {
            TheirHistory(_nikos, "Curl");

            (await NamesAsync(Admin)).ShouldBe("Curl Squat");
            (await NamesAsync(Caller.Of(_nikos, "WorkoutPlansAdmin_View"))).ShouldBe("Curl Squat");
            (await NamesAsync(Member(_maria))).ShouldBe("Squat");
            (await NamesAsync(Caller.Of(_nikos, "WorkoutPlansAdmin_Edit"))).ShouldBe("Curl");
            (await NamesAsync(Caller.WithNoId("WorkoutPlans_View"))).ShouldBe("");
        }

        [Fact]
        public async Task Each_row_says_whose_plan_it_is_and_which()
        {
            TheirHistory(_nikos, "Curl", "Arms");

            List<ExerciseHistoryDto> rows = (await GridAsync(Admin)).Data!.OrderBy(x => x.Id).ToList();

            rows.Select(x => $"{x.Name} {x.WorkoutPlanTitle}").ShouldBe(["Squat Legs", "Curl Arms"]);
            rows[0].WorkoutPlanId.ShouldBe(_plan.Id);
            rows[0].UserId.ShouldBe(_maria.Id.ToString());
            rows[0].User.ShouldNotBeNull().Email.ShouldBe("maria@gym.test");
            rows[1].UserId.ShouldBe(_nikos.Id.ToString());
        }

        // As the grid of the exercises does.
        [Fact]
        public async Task It_filters_by_members()
        {
            User eleni = Seed.User();
            TheirHistory(_nikos, "Curl");
            TheirHistory(eleni, "Plank");
            string maria = _maria.Id.ToString();
            string nikos = _nikos.Id.ToString();

            foreach (string field in new[] { "userId", "UserId" })
                foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.@in, DataTableFiltersEnum.custom, DataTableFiltersEnum.equals })
                {
                    (await NamesAsync(Admin, Filter(field, [maria], type))).ShouldBe("Squat");
                    (await NamesAsync(Admin, Filter(field, [maria, nikos], type))).ShouldBe("Curl Squat");
                    (await NamesAsync(Admin, Filter(field, [maria, "x"], type))).ShouldBe("Squat");
                    (await NamesAsync(Admin, Filter(field, ["x"], type))).ShouldBe("Curl Plank Squat");
                    (await NamesAsync(Admin, Filter(field, [Guid.Empty.ToString()], type))).ShouldBe("Curl Plank Squat");
                    (await NamesAsync(Admin, Filter(field, Array.Empty<string>(), type))).ShouldBe("Curl Plank Squat");
                    (await NamesAsync(Admin, Filter(field, [Guid.NewGuid().ToString()], type))).ShouldBe("");
                    (await NamesAsync(Admin, Filter(field, maria, type))).ShouldBe("Curl Plank Squat");
                }

            (await NamesAsync(Member(_maria), Filter("userId", [nikos], DataTableFiltersEnum.@in))).ShouldBe("");
        }

        [Theory]
        [InlineData("προγ", "Plank")]
        [InlineData("leg", "Squat")]
        [InlineData("none", "")]
        [InlineData("", "Plank Squat")]
        [InlineData("  ", "Plank Squat")]
        [InlineData(null, "Plank Squat")]
        public async Task It_filters_by_the_plans_title(string? title, string expected)
        {
            TheirHistory(_nikos, "Plank", "Πρόγραμμα");

            foreach (string field in new[] { "workoutPlanTitle", "WorkoutPlanTitle" })
                foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.contains, DataTableFiltersEnum.equals })
                    (await NamesAsync(Admin, Filter(field, title, type))).ShouldBe(expected);
        }

        [Fact]
        public async Task It_filters_and_sorts_by_its_own_columns()
        {
            Exercise lunge = Seed.Exercise(_plan, x => x.Name = "Lunge");
            Seed.History(lunge);

            (await NamesAsync(Admin, Filter("ExerciseId", lunge.Id.ToString(), DataTableFiltersEnum.equals))).ShouldBe("Lunge");
            (await PostAsync<DataTableDto<ExerciseHistoryDto>>(Admin, Url + "/GetDataTable", SortedGrid<ExerciseHistoryDto>(("userId", 1), ("name", -1)))).Value.Data!.Select(x => x.Name).ShouldBe(["Squat", "Lunge"]);
        }


        // ================================================================
        // What no request can reach
        // ================================================================

        // It is always of an exercise, and that of a plan. Were one read without them, the row would say
        // nothing of them.
        [Fact]
        public async Task A_row_whose_exercise_was_not_read_says_nothing_of_it()
        {
            OpenController controller = Direct.Controller<OpenController>(Services, "ExerciseHistories", []);
            List<ExerciseHistoryDto> rows = [new ExerciseHistoryDto(), new ExerciseHistoryDto(), new ExerciseHistoryDto()];

            await controller.UpdateAsync(
                [
                    new ExerciseHistory(),
                    new ExerciseHistory { Exercise = new Exercise() },
                    new ExerciseHistory { Exercise = new Exercise { WorkoutPlan = new WorkoutPlan { Id = 7, Title = "Legs", UserId = _maria.Id } } }
                ],
                rows);

            rows.Select(x => $"{x.WorkoutPlanId} {x.WorkoutPlanTitle} {x.UserId}").ShouldBe(["0  ", "0  ", $"7 Legs {_maria.Id}"]);
        }

        private sealed class OpenController : ExerciseHistoriesController
        {
            public OpenController(IDataService dataService, IMapper mapper, IStringLocalizer localizer, IEmailService emailService) : base(dataService, mapper, localizer, emailService)
            {
            }

            public Task UpdateAsync(List<ExerciseHistory> history, List<ExerciseHistoryDto> rows)
            {
                return DataTableResultUpdate(history, rows);
            }
        }
    }
}
