using API.Controllers;
using API.Tests.Host;
using AutoMapper;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Exercise;
using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The exercises of a plan. Its permissions are those of the plans: the trainer writes them, and a
    /// member changes their own weights.
    /// </summary>
    public sealed class ExercisesControllerTests : ApiTest
    {
        private const string Url = "api/Exercises";

        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _admin;
        private readonly WorkoutPlan _plan;
        private readonly Exercise _squat;

        public ExercisesControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User(x => x.Email = "maria@gym.test");
            _nikos = Seed.User();
            _admin = Seed.User();
            _plan = Seed.Plan(_maria, change: x => x.Title = "Legs");
            _squat = Seed.Exercise(_plan, x =>
            {
                x.Name = "Squat";
                x.Description = "Low";
                x.Sets = "3";
                x.Reps = "10";
                x.Weight = "80";
                x.VideoUrl = "https://video";
                x.GroupNumber = 2;
                x.GroupExerciseOrderNumber = 4;
                x.CreatedBy_Id = "maker";
                x.CreatedBy_FullName = "Maker";
                x.CreatedOn = new DateTime(2025, 1, 2);
            });
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

        private Exercise Read(int? id = null)
        {
            return Database.Read(db => db.Exercises.Single(x => x.Id == (id ?? _squat.Id)));
        }

        private List<ExerciseHistory> History()
        {
            return Database.Read(db => db.ExerciseHistories.OrderBy(x => x.Id).ToList());
        }

        // As the form sends it, with one thing or another changed.
        private ExerciseDto Form(Action<ExerciseDto>? change = null)
        {
            ExerciseDto form = new ExerciseDto
            {
                Id = _squat.Id,
                Name = "Squat",
                Description = "Low",
                Sets = "3",
                Reps = "10",
                Weight = "80",
                VideoUrl = "https://video",
                GroupNumber = 2,
                GroupExerciseOrderNumber = 4,
                WorkoutPlanId = _plan.Id
            };
            change?.Invoke(form);

            return form;
        }

        private Task<ApiResult<Exercise>> ChangeAsync(Caller caller, ExerciseDto form, object? id = null)
        {
            return PutAsync<Exercise>(caller, $"{Url}/{id ?? _squat.Id}", form);
        }


        // ================================================================
        // A member's own weight
        // ================================================================

        [Fact]
        public async Task A_member_changes_their_weight()
        {
            ApiResult<Exercise> result = await ChangeAsync(Member(_maria), Form(x => x.Weight = "85"));

            result.ShouldSucceed(English.Updated("Exercise"));
            result.Value.Weight.ShouldBe("85");
            Read().Weight.ShouldBe("85");
        }

        // The rest is the trainer's, whatever the request says. So is which exercise: it is the one in
        // the address, which was found to be theirs.
        [Fact]
        public async Task And_nothing_else()
        {
            WorkoutPlan theirs = Seed.Plan(_nikos);
            Exercise lunge = Seed.Exercise(theirs, x => x.Name = "Lunge");

            (await ChangeAsync(Member(_maria), new ExerciseDto
            {
                Id = lunge.Id,
                Name = "Changed",
                Description = "Changed",
                Sets = "9",
                Reps = "9",
                Weight = "85",
                VideoUrl = "changed",
                GroupNumber = 9,
                GroupExerciseOrderNumber = 9,
                WorkoutPlanId = theirs.Id
            })).ShouldSucceed(English.Updated("Exercise"));

            Exercise changed = Read();
            changed.Weight.ShouldBe("85");
            changed.Name.ShouldBe("Squat");
            changed.Description.ShouldBe("Low");
            changed.Sets.ShouldBe("3");
            changed.Reps.ShouldBe("10");
            changed.VideoUrl.ShouldBe("https://video");
            changed.GroupNumber.ShouldBe(2);
            changed.GroupExerciseOrderNumber.ShouldBe(4);
            changed.WorkoutPlanId.ShouldBe(_plan.Id);
            changed.CreatedBy_Id.ShouldBe("maker");
            changed.CreatedBy_FullName.ShouldBe("Maker");
            changed.CreatedOn.ShouldBe(new DateTime(2025, 1, 2));
            Read(lunge.Id).Name.ShouldBe("Lunge");
            Read(lunge.Id).Weight.ShouldBe(lunge.Weight);
        }

        // Somebody else's answers like one that is not there.
        [Fact]
        public async Task Somebody_elses_is_not_found()
        {
            (await ChangeAsync(Member(_nikos), Form(x => x.Weight = "85"))).ShouldFail(English.NotFound("Exercise"));
            (await ChangeAsync(Caller.WithNoId("WorkoutPlans_Edit"), Form(x => x.Weight = "85"))).ShouldFail(English.NotFound("Exercise"));
            (await ChangeAsync(Caller.Of(_nikos, "WorkoutPlans_Edit", "WorkoutPlansAdmin_View"), Form(x => x.Weight = "85"))).ShouldFail(English.NotFound("Exercise"));
            (await ChangeAsync(Member(_maria), Form(), 99)).ShouldFail(English.NotFound("Exercise"));
            (await ChangeAsync(Admin, Form(), 99)).ShouldFail(English.NotFound("Exercise"));
            (await ChangeAsync(Admin, Form(), "x")).ShouldBreak();

            Read().Weight.ShouldBe("80");
            History().ShouldBeEmpty();
        }

        // The permission alone does not make it theirs, and the admin's alone is not enough.
        [Fact]
        public async Task Changing_one_needs_the_edit_permission()
        {
            (await ChangeAsync(Caller.Of(_maria, "WorkoutPlans_View"), Form(x => x.Weight = "85"))).ShouldFail(English.NotAuthorized);
            (await ChangeAsync(Caller.Of(_admin, "WorkoutPlansAdmin_Edit", "WorkoutPlansAdmin_View"), Form(x => x.Weight = "85"))).ShouldFail(English.NotAuthorized);

            Read().Weight.ShouldBe("80");
        }

        // A form with none is refused before it gets here.
        [Fact]
        public async Task A_missing_weight_is_an_empty_one()
        {
            ExercisesController controller = Direct.Controller<ExercisesController>(Services, "Exercises", ["WorkoutPlans_Edit"], _maria);

            ActionResult<ApiResponse<Exercise>> response = await controller.Put(_squat.Id.ToString(), Form(x => x.Weight = null!));

            response.Value.ShouldNotBeNull().IsSucceed.ShouldBeTrue();
            Read().Weight.ShouldBe("");
        }


        // ================================================================
        // The trainer's form
        // ================================================================

        [Fact]
        public async Task An_admin_changes_everything()
        {
            WorkoutPlan other = Seed.Plan(_nikos);

            ApiResult<Exercise> result = await ChangeAsync(Admin, new ExerciseDto
            {
                Id = _squat.Id,
                Name = "Lunge",
                Description = "Deep",
                Sets = "4",
                Reps = "8",
                Weight = "20",
                VideoUrl = "https://other",
                GroupNumber = 5,
                GroupExerciseOrderNumber = 6,
                WorkoutPlanId = other.Id
            });

            result.ShouldSucceed(English.Updated("Exercise"));
            Exercise changed = Read();
            changed.Name.ShouldBe("Lunge");
            changed.Description.ShouldBe("Deep");
            changed.Sets.ShouldBe("4");
            changed.Reps.ShouldBe("8");
            changed.Weight.ShouldBe("20");
            changed.VideoUrl.ShouldBe("https://other");
            changed.GroupNumber.ShouldBe(5);
            changed.GroupExerciseOrderNumber.ShouldBe(6);
            changed.WorkoutPlanId.ShouldBe(other.Id);
            // Not something the form has.
            changed.CreatedBy_Id.ShouldBe("");
            changed.CreatedOn.ShouldNotBe(_squat.CreatedOn);
        }

        // The address says whether it is there, and the body which is written.
        [Fact]
        public async Task The_body_says_which_an_admin_changes()
        {
            Exercise lunge = Seed.Exercise(_plan, x => x.Name = "Lunge");

            (await ChangeAsync(Admin, Form(x => { x.Id = lunge.Id; x.Name = "Changed"; }))).ShouldSucceed(English.Updated("Exercise"));

            Read().Name.ShouldBe("Squat");
            Read(lunge.Id).Name.ShouldBe("Changed");
            History().Single().ExerciseId.ShouldBe(lunge.Id);
        }

        [Fact]
        public async Task An_exercise_needs_its_name_sets_reps_and_weight()
        {
            (await ChangeAsync(Admin, Form(x => { x.Name = ""; x.Sets = ""; x.Reps = ""; x.Weight = ""; }))).ShouldBeBad(English.Required("Name"), English.Required("Sets"), English.Required("Reps"), English.Required("Weight"));
        }


        // ================================================================
        // What is kept of it
        // ================================================================

        // A change to what is done is written down; a change to where it stands, or to its video, not.
        [Theory]
        [InlineData("nothing", false)]
        [InlineData("Name", true)]
        [InlineData("Description", true)]
        [InlineData("Sets", true)]
        [InlineData("Reps", true)]
        [InlineData("Weight", true)]
        [InlineData("VideoUrl", false)]
        [InlineData("GroupNumber", false)]
        [InlineData("GroupExerciseOrderNumber", false)]
        public async Task A_change_is_kept_as_it_became(string what, bool isKept)
        {
            ExerciseDto form = Form(x =>
            {
                switch (what)
                {
                    case "Name": x.Name = "Lunge"; break;
                    case "Description": x.Description = "Deep"; break;
                    case "Sets": x.Sets = "4"; break;
                    case "Reps": x.Reps = "8"; break;
                    case "Weight": x.Weight = "85"; break;
                    case "VideoUrl": x.VideoUrl = "https://other"; break;
                    case "GroupNumber": x.GroupNumber = 7; break;
                    case "GroupExerciseOrderNumber": x.GroupExerciseOrderNumber = 7; break;
                }
            });

            (await ChangeAsync(Admin, form)).ShouldSucceed(English.Updated("Exercise"));

            if (!isKept)
            {
                History().ShouldBeEmpty();
                return;
            }

            ExerciseHistory kept = History().Single();
            kept.ExerciseId.ShouldBe(_squat.Id);
            kept.Name.ShouldBe(form.Name);
            kept.Description.ShouldBe(form.Description);
            kept.Sets.ShouldBe(form.Sets);
            kept.Reps.ShouldBe(form.Reps);
            kept.Weight.ShouldBe(form.Weight);
            kept.GroupNumber.ShouldBe(2);
            kept.GroupExerciseOrderNumber.ShouldBe(4);
        }

        [Fact]
        public async Task Every_weight_a_member_writes_is_kept()
        {
            await ChangeAsync(Member(_maria), Form(x => x.Weight = "85"));
            await ChangeAsync(Member(_maria), Form(x => x.Weight = "85"));
            await ChangeAsync(Member(_maria), Form(x => x.Weight = "90"));
            // What a member is not allowed to change is no change.
            await ChangeAsync(Member(_maria), Form(x => { x.Weight = "90"; x.Name = "Changed"; x.Sets = "9"; }));

            History().Select(x => $"{x.Name} {x.Weight}").ShouldBe(["Squat 85", "Squat 90"]);
        }


        // ================================================================
        // Reading, adding, deleting
        // ================================================================

        [Fact]
        public async Task One_is_its_members_and_an_admins_to_read()
        {
            string url = $"{Url}/{_squat.Id}";

            ExerciseDto dto = (await GetAsync<ExerciseDto>(Member(_maria), url)).Value;
            dto.Id.ShouldBe(_squat.Id);
            dto.Name.ShouldBe("Squat");
            dto.Weight.ShouldBe("80");
            dto.WorkoutPlanId.ShouldBe(_plan.Id);
            dto.User.ShouldBeNull();

            (await GetAsync<ExerciseDto>(Caller.Of(_nikos, "WorkoutPlans_View", "WorkoutPlansAdmin_View"), url)).ShouldSucceed();
            (await GetAsync<ExerciseDto>(Member(_nikos), url)).ShouldFail(English.NotFound("Exercise"));
            (await GetAsync<ExerciseDto>(Caller.Of(_nikos, "WorkoutPlans_View", "WorkoutPlansAdmin_Edit"), url)).ShouldFail(English.NotFound("Exercise"));
            (await GetAsync<ExerciseDto>(Caller.WithNoId("WorkoutPlans_View"), url)).ShouldFail(English.NotFound("Exercise"));
            (await GetAsync<ExerciseDto>(Admin, $"{Url}/99")).ShouldFail(English.NotFound("Exercise"));
            (await GetAsync<ExerciseDto>(Caller.Of(_maria, "WorkoutPlans_Edit", "WorkoutPlansAdmin_View"), url)).ShouldFail(English.NotAuthorized);
        }

        [Fact]
        public async Task Adding_one_needs_the_add_permission_of_the_plans()
        {
            object[] rows = [new { name = "Lunge", sets = "3", reps = "8", weight = "20", groupNumber = 1, groupExerciseOrderNumber = 2, workoutPlanId = _plan.Id }];

            (await PostAsync<List<Exercise>>(Caller.Of(_admin, "WorkoutPlans_Edit", "Exercises_Add"), Url, rows)).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<Exercise>>(Admin, Url, rows)).ShouldSucceed(English.Updated("Exercise"));

            Exercise added = Database.Read(db => db.Exercises.OrderBy(x => x.Id).Last());
            added.Name.ShouldBe("Lunge");
            added.WorkoutPlanId.ShouldBe(_plan.Id);
            added.GroupExerciseOrderNumber.ShouldBe(2);
            History().ShouldBeEmpty();
        }

        [Fact]
        public async Task Deleting_one_needs_the_delete_permission_of_the_plans()
        {
            (await DeleteAsync<Exercise>(Member(_maria), $"{Url}/{_squat.Id}")).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<Exercise>(Admin, $"{Url}/{_squat.Id}")).ShouldSucceed(English.Deleted("Exercise"));

            Database.Read(db => db.Exercises.Count()).ShouldBe(0);
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<DataTableDto<ExerciseDto>> GridAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            ApiResult<DataTableDto<ExerciseDto>> result = await PostAsync<DataTableDto<ExerciseDto>>(caller, Url + "/GetDataTable", Grid<ExerciseDto>(filters));

            result.ShouldSucceed();
            result.Value.TotalRecords.ShouldBe(result.Value.Data!.Count());

            return result.Value;
        }

        private async Task<string> NamesAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            return string.Join(" ", (await GridAsync(caller, filters)).Data!.Select(x => x.Name).Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task An_admin_sees_every_exercise_and_a_member_those_of_their_plans()
        {
            Seed.Exercise(Seed.Plan(_nikos), x => x.Name = "Curl");

            (await NamesAsync(Admin)).ShouldBe("Curl Squat");
            (await NamesAsync(Caller.Of(_nikos, "WorkoutPlansAdmin_View"))).ShouldBe("Curl Squat");
            (await NamesAsync(Member(_maria))).ShouldBe("Squat");
            (await NamesAsync(Caller.Of(_nikos, "WorkoutPlansAdmin_Edit"))).ShouldBe("Curl");
            (await NamesAsync(Caller.WithNoId("WorkoutPlans_View"))).ShouldBe("");
        }

        [Fact]
        public async Task Each_row_says_whose_plan_it_is_and_which()
        {
            Database.Change(db => db.WorkoutPlans.Single().IsInactive = true);
            Seed.Exercise(Seed.Plan(_nikos, change: x => x.Title = "Arms"), x => x.Name = "Curl");

            List<ExerciseDto> rows = (await GridAsync(Admin)).Data!.OrderBy(x => x.Id).ToList();

            rows.Select(x => $"{x.Name} {x.WorkoutPlanTitle} {x.IsWorkoutPlanInactive}").ShouldBe(["Squat Legs True", "Curl Arms False"]);
            rows[0].UserId.ShouldBe(_maria.Id.ToString());
            rows[0].User.ShouldNotBeNull().Email.ShouldBe("maria@gym.test");
            rows[1].UserId.ShouldBe(_nikos.Id.ToString());
        }

        // The members that are picked, whatever kind of filter it is. One that is no id is passed over,
        // and with none left everybody's are listed.
        [Fact]
        public async Task It_filters_by_members()
        {
            User eleni = Seed.User();
            Seed.Exercise(Seed.Plan(_nikos), x => x.Name = "Curl");
            Seed.Exercise(Seed.Plan(eleni), x => x.Name = "Plank");
            string maria = _maria.Id.ToString();
            string nikos = _nikos.Id.ToString();

            foreach (string field in new[] { "userId", "UserId", "USERID" })
                foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.@in, DataTableFiltersEnum.custom, DataTableFiltersEnum.equals })
                {
                    (await NamesAsync(Admin, Filter(field, [maria], type))).ShouldBe("Squat");
                    (await NamesAsync(Admin, Filter(field, [maria, nikos], type))).ShouldBe("Curl Squat");
                    (await NamesAsync(Admin, Filter(field, [maria, "x"], type))).ShouldBe("Squat");
                    (await NamesAsync(Admin, Filter(field, ["x"], type))).ShouldBe("Curl Plank Squat");
                    (await NamesAsync(Admin, Filter(field, [Guid.Empty.ToString()], type))).ShouldBe("Curl Plank Squat");
                    (await NamesAsync(Admin, Filter(field, Array.Empty<string>(), type))).ShouldBe("Curl Plank Squat");
                    (await NamesAsync(Admin, Filter(field, [Guid.NewGuid().ToString()], type))).ShouldBe("");
                    // As one value it is not looked at.
                    (await NamesAsync(Admin, Filter(field, maria, type))).ShouldBe("Curl Plank Squat");
                }

            (await NamesAsync(Admin, Filter("userId", [maria], DataTableFiltersEnum.@in), Filter("userId", [nikos], DataTableFiltersEnum.@in))).ShouldBe("Curl Squat");
        }

        [Fact]
        public async Task A_member_cannot_filter_to_somebody_else()
        {
            Seed.Exercise(Seed.Plan(_nikos), x => x.Name = "Curl");

            (await NamesAsync(Member(_maria), Filter("userId", [_nikos.Id.ToString()], DataTableFiltersEnum.@in))).ShouldBe("");
        }

        // Without its accents and in any case.
        [Theory]
        [InlineData("προγ", "Plank")]
        [InlineData("ΠΡΌΓΡΑΜΜΑ", "Plank")]
        [InlineData("leg", "Squat")]
        [InlineData("none", "")]
        [InlineData("", "Plank Squat")]
        [InlineData("  ", "Plank Squat")]
        [InlineData(null, "Plank Squat")]
        public async Task It_filters_by_the_plans_title(string? title, string expected)
        {
            Seed.Exercise(Seed.Plan(_nikos, change: x => x.Title = "Πρόγραμμα"), x => x.Name = "Plank");

            foreach (string field in new[] { "workoutPlanTitle", "WorkoutPlanTitle" })
                foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.contains, DataTableFiltersEnum.equals })
                    (await NamesAsync(Admin, Filter(field, title, type))).ShouldBe(expected);
        }

        [Fact]
        public async Task It_filters_and_sorts_by_its_own_columns()
        {
            Seed.Exercise(_plan, x => x.Name = "Lunge");

            (await NamesAsync(Admin, Filter("name", "lun"))).ShouldBe("Lunge");
            (await NamesAsync(Admin, Filter("WorkoutPlanId", _plan.Id.ToString(), DataTableFiltersEnum.equals))).ShouldBe("Lunge Squat");
            (await PostAsync<DataTableDto<ExerciseDto>>(Admin, Url + "/GetDataTable", SortedGrid<ExerciseDto>(("name", -1)))).Value.Data!.Select(x => x.Name).ShouldBe(["Squat", "Lunge"]);
            // These two are no columns, and a sort by them is passed over.
            (await PostAsync<DataTableDto<ExerciseDto>>(Admin, Url + "/GetDataTable", SortedGrid<ExerciseDto>(("userId", 1), ("workoutPlanTitle", 1), ("name", 1)))).Value.Data!.Select(x => x.Name).ShouldBe(["Lunge", "Squat"]);
        }


        // ================================================================
        // What no request can reach
        // ================================================================

        // An exercise is always of a plan. Were one read without it, the row would say nothing of it.
        [Fact]
        public async Task A_row_whose_plan_was_not_read_says_nothing_of_it()
        {
            OpenController controller = Direct.Controller<OpenController>(Services, "Exercises", []);
            List<ExerciseDto> rows = [new ExerciseDto(), new ExerciseDto()];

            await controller.UpdateAsync([new Exercise(), new Exercise { WorkoutPlan = new WorkoutPlan { Title = "Legs", UserId = _maria.Id } }], rows);

            rows.Select(x => $"{x.WorkoutPlanTitle} {x.UserId}").ShouldBe([" ", $"Legs {_maria.Id}"]);
        }

        private sealed class OpenController : ExercisesController
        {
            public OpenController(IDataService dataService, IMapper mapper, IStringLocalizer localizer, IEmailService emailService) : base(dataService, mapper, localizer, emailService)
            {
            }

            public Task UpdateAsync(List<Exercise> exercises, List<ExerciseDto> rows)
            {
                return DataTableResultUpdate(exercises, rows);
            }
        }

        // The plan goes and takes its exercises along. Were one left, it would be nobody's - and whoever
        // does not say who they are is nobody.
        [Fact]
        public async Task One_whose_plan_is_gone_is_for_whoever_is_nobody()
        {
            Database.Execute("PRAGMA foreign_keys = OFF; DELETE FROM WorkoutPlans; PRAGMA foreign_keys = ON;");

            (await GetAsync<ExerciseDto>(Member(_maria), $"{Url}/{_squat.Id}")).ShouldFail(English.NotFound("Exercise"));
            (await GetAsync<ExerciseDto>(Caller.WithNoId("WorkoutPlans_View"), $"{Url}/{_squat.Id}")).ShouldSucceed();
            (await ChangeAsync(Member(_maria), Form(x => x.Weight = "85"))).ShouldFail(English.NotFound("Exercise"));
            (await ChangeAsync(Caller.WithNoId("WorkoutPlans_Edit"), Form(x => x.Weight = "85"))).ShouldBreak();
        }
    }
}
