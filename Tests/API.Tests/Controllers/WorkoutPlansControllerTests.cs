using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.WorkoutPlan;
using Core.Enums;
using Core.Models;
using Core.Translations;
using Microsoft.EntityFrameworkCore;

namespace API.Tests.Controllers
{
    /// <summary>
    /// A member's plans: theirs to read and to put away, the trainer's to write.
    /// </summary>
    public sealed class WorkoutPlansControllerTests : ApiTest
    {
        private const string Url = "api/WorkoutPlans";

        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _admin;
        private readonly WorkoutPlanRule _rule;

        public WorkoutPlansControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User(x => { x.FirstName = "Maria"; x.LastName = "Papa"; x.Email = "maria@gym.test"; x.UserName = "maria"; x.MedicalHistory = "private"; x.Address = "street"; x.ProfileImage = [1, 2]; });
            _nikos = Seed.User();
            _admin = Seed.User();
            _rule = Seed.Rule("Beginners", 2, 3);
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

        private WorkoutPlan Read(int id)
        {
            return Database.Read(db => db.WorkoutPlans.Include(x => x.Exercises).Single(x => x.Id == id));
        }

        private static DateTime MinutesAgo(int minutes)
        {
            return DateTime.UtcNow.AddMinutes(-minutes);
        }


        // ================================================================
        // Reading one
        // ================================================================

        [Fact]
        public async Task A_plan_is_read_with_everything_the_page_shows()
        {
            WorkoutPlan plan = Seed.Plan(_maria, _rule, 2, x => { x.Title = "Legs"; x.Description = "Heavy"; x.IsCircular = true; x.CreatedOn = new DateTime(2025, 1, 2); });
            Exercise squat = Seed.Exercise(plan, x => { x.Name = "Squat"; x.Sets = "3"; x.Reps = "10"; x.Weight = "80"; x.Description = "Low"; x.VideoUrl = "https://video"; x.GroupNumber = 2; x.GroupExerciseOrderNumber = 4; });

            WorkoutPlanDto dto = (await GetAsync<WorkoutPlanDto>(Member(_maria), $"{Url}/{plan.Id}")).Value;

            dto.Id.ShouldBe(plan.Id);
            dto.Title.ShouldBe("Legs");
            dto.Description.ShouldBe("Heavy");
            dto.IsCircular.ShouldBeTrue();
            dto.IsInactive.ShouldBeFalse();
            dto.UserId.ShouldBe(_maria.Id.ToString());
            dto.CreatedOn.ShouldBe(new DateTime(2025, 1, 2));
            dto.WorkoutPlanRuleId.ShouldBe(_rule.Id);
            dto.WorkoutPlanRuleName.ShouldBe("Beginners");
            dto.CurrentWeek.ShouldBe(2);
            dto.WeekCount.ShouldBe(2);
            dto.CurrentWeekMessage.ShouldBe("Message 2");
            dto.IsRunning.ShouldBeFalse();
            dto.ElapsedSeconds.ShouldBeNull();
            dto.HasIncompleteRecording.ShouldBeFalse();
            dto.LastRecordingOn.ShouldBeNull();
            dto.LastRecordingDurationSeconds.ShouldBeNull();
            dto.RecordingCount.ShouldBe(0);

            Core.Dtos.Exercise.ExerciseDto exercise = dto.Exercises.Single();
            exercise.Id.ShouldBe(squat.Id);
            exercise.Name.ShouldBe("Squat");
            exercise.Sets.ShouldBe("3");
            exercise.Reps.ShouldBe("10");
            exercise.Weight.ShouldBe("80");
            exercise.Description.ShouldBe("Low");
            exercise.VideoUrl.ShouldBe("https://video");
            exercise.GroupNumber.ShouldBe(2);
            exercise.GroupExerciseOrderNumber.ShouldBe(4);
            exercise.WorkoutPlanId.ShouldBe(plan.Id);
        }

        // Who it is for, and nothing else about them.
        [Fact]
        public async Task The_member_is_a_name_and_a_face()
        {
            WorkoutPlan plan = Seed.Plan(_maria);

            ApiResult<WorkoutPlanDto> result = await GetAsync<WorkoutPlanDto>(Admin, $"{Url}/{plan.Id}");

            WorkoutPlanOwnerDto owner = result.Value.User.ShouldNotBeNull();
            owner.Id.ShouldBe(_maria.Id.ToString());
            owner.UserName.ShouldBe("maria");
            owner.FirstName.ShouldBe("Maria");
            owner.LastName.ShouldBe("Papa");
            owner.ProfileImage.ShouldBe([1, 2]);
            result.Body.ShouldNotContain("maria@gym.test");
            result.Body.ShouldNotContain("private");
            result.Body.ShouldNotContain("street");
        }

        [Fact]
        public async Task A_plan_with_no_rule_has_no_weeks()
        {
            WorkoutPlanDto dto = (await GetAsync<WorkoutPlanDto>(Admin, $"{Url}/{Seed.Plan(_maria).Id}")).Value;

            dto.WorkoutPlanRuleId.ShouldBeNull();
            dto.WorkoutPlanRuleName.ShouldBe("");
            dto.WeekCount.ShouldBe(0);
            dto.CurrentWeekMessage.ShouldBe("");
        }

        // Unset, or gone from the rule.
        [Theory]
        [InlineData(null, "")]
        [InlineData(0, "")]
        [InlineData(1, "Message 1")]
        [InlineData(2, "Message 2")]
        [InlineData(3, "")]
        public async Task A_week_the_rule_does_not_have_has_no_message(int? week, string message)
        {
            WorkoutPlanDto dto = (await GetAsync<WorkoutPlanDto>(Admin, $"{Url}/{Seed.Plan(_maria, _rule, week).Id}")).Value;

            dto.CurrentWeek.ShouldBe(week);
            dto.CurrentWeekMessage.ShouldBe(message);
            dto.WeekCount.ShouldBe(2);
        }

        // Somebody else's answers like one that is not there.
        [Fact]
        public async Task A_plan_is_its_members_and_an_admins_to_read()
        {
            WorkoutPlan plan = Seed.Plan(_maria);
            string url = $"{Url}/{plan.Id}";

            (await GetAsync<WorkoutPlanDto>(Member(_maria), url)).ShouldSucceed();
            (await GetAsync<WorkoutPlanDto>(Caller.Of(_nikos, "WorkoutPlans_View", "WorkoutPlansAdmin_View"), url)).ShouldSucceed();

            (await GetAsync<WorkoutPlanDto>(Member(_nikos), url)).ShouldFail(English.NotFound("WorkoutPlan"));
            (await GetAsync<WorkoutPlanDto>(Caller.Of(_nikos, "WorkoutPlans_View", "WorkoutPlansAdmin_Edit"), url)).ShouldFail(English.NotFound("WorkoutPlan"));
            (await GetAsync<WorkoutPlanDto>(Caller.WithNoId("WorkoutPlans_View"), url)).ShouldFail(English.NotFound("WorkoutPlan"));
            (await GetAsync<WorkoutPlanDto>(Admin, $"{Url}/99")).ShouldFail(English.NotFound("WorkoutPlan"));

            (await GetAsync<WorkoutPlanDto>(Caller.Of(_maria, "WorkoutPlansAdmin_View"), url)).ShouldFail(English.NotAuthorized);
            (await GetAsync<WorkoutPlanDto>(Admin, $"{Url}/x")).ShouldBreak();
        }

        // Gone from their list, and still there for somebody who knows its number.
        [Fact]
        public async Task A_plan_that_was_put_away_can_still_be_read()
        {
            WorkoutPlan plan = Seed.Plan(_maria, change: x => x.IsInactive = true);

            (await GetAsync<WorkoutPlanDto>(Member(_maria), $"{Url}/{plan.Id}")).Value.IsInactive.ShouldBeTrue();
        }


        // ================================================================
        // What its recordings say about it
        // ================================================================

        [Theory]
        // Minutes since it was started; whether it was stopped; running, incomplete.
        [InlineData(1, false, true, false)]
        [InlineData(179, false, true, false)]
        [InlineData(181, false, false, true)]
        [InlineData(60 * 24 * 30, false, false, true)]
        [InlineData(1, true, false, false)]
        [InlineData(181, true, false, false)]
        public async Task A_plan_is_running_for_three_hours_unless_it_is_stopped(int minutes, bool isStopped, bool isRunning, bool isIncomplete)
        {
            WorkoutPlan plan = Seed.Plan(_maria);
            DateTime startedOn = MinutesAgo(minutes);
            Seed.Recording(plan, startedOn, completedOn: isStopped ? startedOn.AddSeconds(30) : null);

            WorkoutPlanDto dto = (await GetAsync<WorkoutPlanDto>(Admin, $"{Url}/{plan.Id}")).Value;

            dto.IsRunning.ShouldBe(isRunning);
            dto.HasIncompleteRecording.ShouldBe(isIncomplete);
            dto.LastRecordingOn.ShouldNotBeNull().ShouldBe(startedOn, TimeSpan.FromMilliseconds(1));
            dto.LastRecordingDurationSeconds.ShouldBe(isStopped ? 30 : null);
            dto.RecordingCount.ShouldBe(1);

            if (isRunning)
                dto.ElapsedSeconds.ShouldNotBeNull().ShouldBeInRange(minutes * 60, minutes * 60 + 30);
            else
                dto.ElapsedSeconds.ShouldBeNull();
        }

        // Only the latest is the one that can be left open: an old one would be flagged for good.
        [Fact]
        public async Task The_latest_recording_is_the_one_that_counts()
        {
            WorkoutPlan plan = Seed.Plan(_maria);
            DateTime latest = MinutesAgo(60 * 24);
            Seed.Recording(plan, MinutesAgo(60 * 24 * 3));
            Seed.Recording(plan, latest, completedOn: latest.AddSeconds(75));
            Seed.Recording(plan, MinutesAgo(60 * 24 * 2));
            Seed.Recording(Seed.Plan(_maria), MinutesAgo(1));

            WorkoutPlanDto dto = (await GetAsync<WorkoutPlanDto>(Admin, $"{Url}/{plan.Id}")).Value;

            dto.IsRunning.ShouldBeFalse();
            dto.HasIncompleteRecording.ShouldBeFalse();
            dto.LastRecordingOn.ShouldNotBeNull().ShouldBe(latest, TimeSpan.FromMilliseconds(1));
            dto.LastRecordingDurationSeconds.ShouldBe(75);
            dto.RecordingCount.ShouldBe(3);
        }

        [Fact]
        public async Task A_running_one_is_found_behind_a_later_one_that_was_stopped()
        {
            WorkoutPlan plan = Seed.Plan(_maria);
            Seed.Recording(plan, MinutesAgo(30));
            Seed.Recording(plan, MinutesAgo(20), completedOn: MinutesAgo(10));

            WorkoutPlanDto dto = (await GetAsync<WorkoutPlanDto>(Admin, $"{Url}/{plan.Id}")).Value;

            dto.IsRunning.ShouldBeTrue();
            dto.ElapsedSeconds.ShouldNotBeNull().ShouldBeInRange(30 * 60, 30 * 60 + 30);
            dto.LastRecordingDurationSeconds.ShouldBe(600);
        }


        // ================================================================
        // Writing a new one
        // ================================================================

        private static object Exercise(string name)
        {
            return new { name, sets = "3", reps = "10", weight = "50", description = "", videoUrl = "", groupNumber = 1, groupExerciseOrderNumber = 1 };
        }

        private Task<ApiResult<List<WorkoutPlan>>> AddAsync(Caller caller, params object[] rows)
        {
            return PostAsync<List<WorkoutPlan>>(caller, Url, rows);
        }

        [Fact]
        public async Task A_plan_is_written_with_its_exercises()
        {
            ApiResult<List<WorkoutPlan>> result = await AddAsync(Admin, new
            {
                title = "Legs",
                description = "Heavy",
                isCircular = true,
                userId = _maria.Id,
                workoutPlanRuleId = _rule.Id,
                currentWeek = 2,
                exercises = new[] { Exercise("Squat"), Exercise("Lunge") }
            });

            result.ShouldSucceed(English.Updated("WorkoutPlan"));
            WorkoutPlan plan = Read(result.Value.Single().Id);
            plan.Title.ShouldBe("Legs");
            plan.Description.ShouldBe("Heavy");
            plan.IsCircular.ShouldBeTrue();
            plan.IsInactive.ShouldBeFalse();
            plan.UserId.ShouldBe(_maria.Id);
            plan.WorkoutPlanRuleId.ShouldBe(_rule.Id);
            plan.CurrentWeek.ShouldBe(2);
            plan.Exercises.Select(x => x.Name).ShouldBe(["Squat", "Lunge"], ignoreOrder: true);
        }

        [Fact]
        public async Task Writing_one_needs_the_add_permission()
        {
            Caller others = Caller.Of(_admin, "WorkoutPlans_View", "WorkoutPlans_Edit", "WorkoutPlans_Delete", "WorkoutPlansAdmin_View", "WorkoutPlansAdmin_Edit", "WorkoutPlansAdmin_Add");

            (await AddAsync(others, new { title = "Legs", userId = _maria.Id })).ShouldFail(English.NotAuthorized);

            Database.Read(db => db.WorkoutPlans.Count()).ShouldBe(0);
            // It is enough.
            (await AddAsync(Caller.Of(_nikos, "WorkoutPlans_Add"), new { title = "Legs", userId = _maria.Id })).ShouldSucceed(English.Updated("WorkoutPlan"));
        }

        // The rule and the week have to agree. With no rule any week will do.
        [Theory]
        [InlineData(false, null, true)]
        [InlineData(false, 1, true)]
        [InlineData(false, 99, true)]
        [InlineData(true, null, false)]
        [InlineData(true, 0, false)]
        [InlineData(true, 1, true)]
        [InlineData(true, 2, true)]
        [InlineData(true, 3, false)]
        public async Task The_week_has_to_be_one_of_the_rules(bool hasRule, int? week, bool isAllowed)
        {
            Seed.Rule("Others", 1, 1, 1);
            WorkoutPlan plan = Seed.Plan(_maria);
            int? ruleId = hasRule ? _rule.Id : null;

            ApiResult<List<WorkoutPlan>> added = await AddAsync(Admin, new { title = "Legs", userId = _maria.Id, workoutPlanRuleId = ruleId, currentWeek = week });
            ApiResult<WorkoutPlan> changed = await PutAsync<WorkoutPlan>(Admin, $"{Url}/{plan.Id}", new { id = plan.Id, title = "Changed", userId = _maria.Id, workoutPlanRuleId = ruleId, currentWeek = week });

            if (isAllowed)
            {
                added.ShouldSucceed(English.Updated("WorkoutPlan"));
                changed.ShouldSucceed(English.Updated("WorkoutPlan"));
                return;
            }

            added.ShouldBeBad(English.Of(TranslationKeys.Selected_week_does_not_exist_in_the_rule));
            changed.ShouldBeBad(English.Of(TranslationKeys.Selected_week_does_not_exist_in_the_rule));
            Database.Read(db => db.WorkoutPlans.Single()).Title.ShouldBe(plan.Title);
        }

        [Fact]
        public async Task A_rule_that_is_not_there_has_no_weeks()
        {
            (await AddAsync(Admin, new { title = "Legs", userId = _maria.Id, workoutPlanRuleId = 99, currentWeek = 1 })).ShouldBeBad(English.Of(TranslationKeys.Selected_week_does_not_exist_in_the_rule));
        }

        [Fact]
        public async Task One_plan_that_is_refused_refuses_them_all()
        {
            (await AddAsync(Admin, new { title = "Legs", userId = _maria.Id }, new { title = "Arms", userId = _maria.Id, workoutPlanRuleId = _rule.Id })).ShouldBeBad(English.Of(TranslationKeys.Selected_week_does_not_exist_in_the_rule));

            Database.Read(db => db.WorkoutPlans.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task A_plan_needs_a_title_a_member_and_whole_exercises()
        {
            (await AddAsync(Admin, new { title = "", userId = "" })).ShouldBeBad(English.Required("Title"), English.Required("UserId"));
            (await AddAsync(Admin, new { title = "Legs", userId = _maria.Id, exercises = new[] { new { name = "", sets = "", reps = "", weight = "" } } }))
                .ShouldBeBad(English.Required("Name"), English.Required("Sets"), English.Required("Reps"), English.Required("Weight"));
            (await AddAsync(Admin, new { title = "Legs", userId = "x" })).ShouldBreak();
            (await AddAsync(Admin, new { title = "Legs", userId = Guid.NewGuid() })).ShouldBreak();
        }


        // ================================================================
        // Changing one
        // ================================================================

        private Task<ApiResult<WorkoutPlan>> ChangeAsync(Caller caller, object id, object body)
        {
            return PutAsync<WorkoutPlan>(caller, $"{Url}/{id}", body);
        }

        private static object Body(WorkoutPlan plan, string title = "Changed", bool? isInactive = null)
        {
            return new { id = plan.Id, title, description = "", userId = plan.UserId, isInactive = isInactive ?? plan.IsInactive, createdOn = plan.CreatedOn };
        }

        [Fact]
        public async Task An_admin_changes_a_plan()
        {
            WorkoutPlan plan = Seed.Plan(_maria, change: x => x.CreatedBy_Id = "maker");

            ApiResult<WorkoutPlan> result = await ChangeAsync(Admin, plan.Id, new
            {
                id = plan.Id,
                title = "Changed",
                description = "New",
                isCircular = true,
                userId = _nikos.Id,
                workoutPlanRuleId = _rule.Id,
                currentWeek = 2,
                createdOn = new DateTime(2025, 1, 2)
            });

            result.ShouldSucceed(English.Updated("WorkoutPlan"));
            WorkoutPlan changed = Read(plan.Id);
            changed.Title.ShouldBe("Changed");
            changed.Description.ShouldBe("New");
            changed.IsCircular.ShouldBeTrue();
            changed.UserId.ShouldBe(_nikos.Id);
            changed.WorkoutPlanRuleId.ShouldBe(_rule.Id);
            changed.CurrentWeek.ShouldBe(2);
            changed.CreatedOn.ShouldBe(new DateTime(2025, 1, 2));
            // Not something the form has.
            changed.CreatedBy_Id.ShouldBe("");
        }

        // Both: the admin's permission, and the one anybody needs to change a plan.
        [Fact]
        public async Task Changing_one_needs_both_permissions()
        {
            WorkoutPlan plan = Seed.Plan(_maria);

            (await ChangeAsync(Member(_maria), plan.Id, Body(plan))).ShouldFail(English.NotAuthorized);
            (await ChangeAsync(Caller.Of(_admin, "WorkoutPlansAdmin_Edit", "WorkoutPlans_View"), plan.Id, Body(plan))).ShouldFail(English.NotAuthorized);

            Read(plan.Id).Title.ShouldBe(plan.Title);
            (await ChangeAsync(Caller.Of(_admin, "WorkoutPlansAdmin_Edit", "WorkoutPlans_Edit"), plan.Id, Body(plan))).ShouldSucceed(English.Updated("WorkoutPlan"));
        }

        // The form sends the whole plan back, and would otherwise bring it out again.
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task A_change_neither_puts_a_plan_away_nor_brings_it_back(bool isInactive, bool sent)
        {
            WorkoutPlan plan = Seed.Plan(_maria, change: x => x.IsInactive = isInactive);

            (await ChangeAsync(Admin, plan.Id, Body(plan, isInactive: sent))).ShouldSucceed(English.Updated("WorkoutPlan"));

            Read(plan.Id).IsInactive.ShouldBe(isInactive);
            Read(plan.Id).Title.ShouldBe("Changed");
        }

        // They have an address of their own.
        [Fact]
        public async Task The_exercises_in_the_form_are_not_saved_with_it()
        {
            WorkoutPlan plan = Seed.Plan(_maria);
            Exercise squat = Seed.Exercise(plan, x => x.Name = "Squat");

            await ChangeAsync(Admin, plan.Id, new
            {
                id = plan.Id,
                title = "Changed",
                userId = _maria.Id,
                exercises = new object[]
                {
                    new { id = squat.Id, name = "Changed", sets = "1", reps = "1", weight = "1", workoutPlanId = plan.Id },
                    Exercise("New")
                }
            });

            Read(plan.Id).Exercises.Select(x => x.Name).ShouldBe(["Squat"]);
        }

        [Fact]
        public async Task The_member_in_the_form_is_not_saved_with_it()
        {
            WorkoutPlan plan = Seed.Plan(_maria);

            await ChangeAsync(Admin, plan.Id, new { id = plan.Id, title = "Changed", userId = _maria.Id, user = new { id = _maria.Id, userName = "x", firstName = "Changed", lastName = "x" } });

            Database.Read(db => db.Users.Single(x => x.Id == _maria.Id)).FirstName.ShouldBe("Maria");
        }

        // The address says whether it is there and whether it was put away, and the body says which is
        // written.
        [Fact]
        public async Task The_body_says_which_plan_is_changed()
        {
            WorkoutPlan first = Seed.Plan(_maria, change: x => x.IsInactive = true);
            WorkoutPlan second = Seed.Plan(_maria);

            (await ChangeAsync(Admin, first.Id, Body(second))).ShouldSucceed(English.Updated("WorkoutPlan"));

            Read(first.Id).Title.ShouldBe(first.Title);
            Read(second.Id).Title.ShouldBe("Changed");
            Read(second.Id).IsInactive.ShouldBeTrue();
        }

        [Fact]
        public async Task A_plan_that_is_not_there_is_not_found()
        {
            (await ChangeAsync(Admin, 99, new { id = 99, title = "Changed", userId = _maria.Id })).ShouldFail(English.NotFound("WorkoutPlan"));
            (await ChangeAsync(Admin, "x", new { id = 99, title = "Changed", userId = _maria.Id })).ShouldBreak();
        }


        // ================================================================
        // Putting one away, and bringing it back
        // ================================================================

        private Task<ApiResult<bool>> SetAsync(Caller caller, object id, string what)
        {
            return PostAsync<bool>(caller, $"{Url}/{id}/{what}", null);
        }

        // The member, somebody else, and an admin; and what they may do.
        [Theory]
        [InlineData("member", "Deactivate", true)]
        [InlineData("member", "Activate", false)]
        [InlineData("other", "Deactivate", false)]
        [InlineData("other", "Activate", false)]
        [InlineData("admin", "Deactivate", true)]
        [InlineData("admin", "Activate", true)]
        [InlineData("nobody", "Deactivate", false)]
        [InlineData("nobody", "Activate", false)]
        public async Task A_member_puts_their_own_away_and_an_admin_brings_it_back(string who, string what, bool isAllowed)
        {
            foreach (bool isInactive in new[] { false, true })
            {
                WorkoutPlan plan = Seed.Plan(_maria, change: x => x.IsInactive = isInactive);
                Caller caller = who switch
                {
                    "member" => Caller.Of(_maria),
                    "other" => Member(_nikos),
                    "admin" => Caller.Of(_admin, "WorkoutPlansAdmin_Edit"),
                    _ => Caller.WithNoId("WorkoutPlans_Edit")
                };

                ApiResult<bool> result = await SetAsync(caller, plan.Id, what);

                if (!isAllowed)
                {
                    result.ShouldBeBad(English.NotAuthorized);
                    Read(plan.Id).IsInactive.ShouldBe(isInactive);
                    continue;
                }

                result.ShouldSucceed(English.Of(what == "Deactivate" ? TranslationKeys.Workout_plan_deactivated : TranslationKeys.Workout_plan_activated));
                result.Data.ShouldBeTrue();
                Read(plan.Id).IsInactive.ShouldBe(what == "Deactivate");
                Read(plan.Id).Title.ShouldBe(plan.Title);
            }
        }

        [Theory]
        [InlineData("Activate")]
        [InlineData("Deactivate")]
        public async Task A_plan_that_is_not_there_is_neither(string what)
        {
            (await SetAsync(Admin, 99, what)).ShouldBeBad(English.NotFound("WorkoutPlan"));
            (await SetAsync(Caller.Of(_maria), 99, what)).ShouldBeBad(English.NotFound("WorkoutPlan"));
            (await SetAsync(Admin, "x", what)).ShouldBeBad("The value 'x' is not valid.");
        }


        // ================================================================
        // Deleting one
        // ================================================================

        [Fact]
        public async Task A_deleted_plan_takes_its_exercises_and_leaves_its_recordings()
        {
            WorkoutPlan plan = Seed.Plan(_maria);
            Seed.Exercise(plan);
            Seed.Recording(plan, MinutesAgo(60));

            (await DeleteAsync<WorkoutPlan>(Member(_maria), $"{Url}/{plan.Id}")).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<WorkoutPlan>(Admin, $"{Url}/{plan.Id}")).ShouldSucceed(English.Deleted("WorkoutPlan"));

            Database.Read(db => db.WorkoutPlans.Count()).ShouldBe(0);
            Database.Read(db => db.Exercises.Count()).ShouldBe(0);
            Database.Read(db => db.WorkoutPlanRecordings.Single()).WorkoutPlanId.ShouldBeNull();
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<DataTableDto<WorkoutPlanDto>> GridAsync(Caller caller, DataTableDto<WorkoutPlanDto> grid)
        {
            ApiResult<DataTableDto<WorkoutPlanDto>> result = await PostAsync<DataTableDto<WorkoutPlanDto>>(caller, Url + "/GetDataTable", grid);

            result.ShouldSucceed();

            return result.Value;
        }

        private async Task<string> TitlesAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            DataTableDto<WorkoutPlanDto> grid = await GridAsync(caller, Grid<WorkoutPlanDto>(filters));
            grid.TotalRecords.ShouldBe(grid.Data!.Count());

            return string.Join(" ", grid.Data!.Select(x => x.Title).Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task An_admin_sees_every_plan_and_a_member_their_own_that_are_not_put_away()
        {
            Seed.Plan(_maria, change: x => x.Title = "A");
            Seed.Plan(_maria, change: x => { x.Title = "B"; x.IsInactive = true; });
            Seed.Plan(_nikos, change: x => x.Title = "C");
            Seed.Plan(_nikos, change: x => { x.Title = "D"; x.IsInactive = true; });

            (await TitlesAsync(Admin)).ShouldBe("A B C D");
            (await TitlesAsync(Caller.Of(_nikos, "WorkoutPlansAdmin_View"))).ShouldBe("A B C D");
            (await TitlesAsync(Member(_maria))).ShouldBe("A");
            (await TitlesAsync(Caller.Of(_nikos, "WorkoutPlansAdmin_Edit"))).ShouldBe("C");
            (await TitlesAsync(Caller.WithNoId("WorkoutPlans_View"))).ShouldBe("");
            (await TitlesAsync(Member(_maria), Filter("UserId", _nikos.Id.ToString(), DataTableFiltersEnum.equals))).ShouldBe("");
        }

        // On the members' page an admin, too, sees what is not put away.
        [Theory]
        [InlineData("true", "A")]
        [InlineData("True", "A")]
        [InlineData(" true ", "A")]
        [InlineData("false", "A B")]
        [InlineData("x", "A B")]
        [InlineData("1", "A B")]
        [InlineData("", "A B")]
        [InlineData(null, "A B")]
        public async Task It_can_be_asked_for_the_active_ones(string? value, string expected)
        {
            Seed.Plan(_maria, change: x => x.Title = "A");
            Seed.Plan(_maria, change: x => { x.Title = "B"; x.IsInactive = true; });

            foreach (string field in new[] { "activeOnly", "ActiveOnly" })
                foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.custom, DataTableFiltersEnum.equals, DataTableFiltersEnum.contains })
                    (await TitlesAsync(Admin, Filter(field, value, type))).ShouldBe(expected);
        }

        // Started and not stopped, less than three hours ago.
        [Theory]
        [InlineData("true", "Running")]
        [InlineData("TRUE", "Running")]
        [InlineData("false", "Lapsed None Stopped")]
        [InlineData("x", "Lapsed None Running Stopped")]
        [InlineData("", "Lapsed None Running Stopped")]
        [InlineData(null, "Lapsed None Running Stopped")]
        public async Task It_can_be_asked_for_the_running_ones(string? value, string expected)
        {
            Seed.Plan(_maria, change: x => x.Title = "None");
            WorkoutPlan running = Seed.Plan(_maria, change: x => x.Title = "Running");
            WorkoutPlan lapsed = Seed.Plan(_maria, change: x => x.Title = "Lapsed");
            WorkoutPlan stopped = Seed.Plan(_maria, change: x => x.Title = "Stopped");
            Seed.Recording(running, MinutesAgo(179));
            Seed.Recording(running, MinutesAgo(60 * 24));
            Seed.Recording(lapsed, MinutesAgo(181));
            Seed.Recording(stopped, MinutesAgo(20), completedOn: MinutesAgo(10));

            foreach (string field in new[] { "isRunning", "IsRunning" })
                foreach (DataTableFiltersEnum type in new[] { DataTableFiltersEnum.equals, DataTableFiltersEnum.custom })
                    (await TitlesAsync(Admin, Filter(field, value, type))).ShouldBe(expected);
        }

        [Fact]
        public async Task The_filters_go_together()
        {
            WorkoutPlan wanted = Seed.Plan(_maria, change: x => x.Title = "Wanted");
            WorkoutPlan inactive = Seed.Plan(_maria, change: x => { x.Title = "Wanted too"; x.IsInactive = true; });
            WorkoutPlan theirs = Seed.Plan(_nikos, change: x => x.Title = "Wanted as well");
            Seed.Plan(_maria, change: x => x.Title = "Wanted idle");
            foreach (WorkoutPlan plan in new[] { wanted, inactive, theirs })
                Seed.Recording(plan, MinutesAgo(5));

            (await TitlesAsync(Admin, Filter("isRunning", "true"), Filter("activeOnly", "true"), Filter("title", "wanted"), Filter("UserId", _maria.Id.ToString(), DataTableFiltersEnum.equals))).ShouldBe("Wanted");
        }

        // By the latest recording of each. One with none counts as the oldest of all.
        [Theory]
        [InlineData(1, "None Old New")]
        [InlineData(3, "None Old New")]
        [InlineData(-1, "New Old None")]
        [InlineData(0, "New Old None")]
        public async Task It_sorts_by_when_a_plan_was_last_used(int order, string expected)
        {
            WorkoutPlan latest = Seed.Plan(_maria, change: x => x.Title = "New");
            Seed.Plan(_maria, change: x => x.Title = "None");
            WorkoutPlan old = Seed.Plan(_maria, change: x => x.Title = "Old");
            Seed.Recording(old, MinutesAgo(50));
            Seed.Recording(latest, MinutesAgo(90));
            Seed.Recording(latest, MinutesAgo(40));

            foreach (string field in new[] { "lastRecordingOn", "LastRecordingOn" })
                string.Join(" ", (await GridAsync(Admin, SortedGrid<WorkoutPlanDto>((field, order)))).Data!.Select(x => x.Title)).ShouldBe(expected);
        }

        [Fact]
        public async Task It_sorts_by_a_column_of_its_own()
        {
            Seed.Plan(_maria, change: x => x.Title = "B");
            Seed.Plan(_maria, change: x => x.Title = "A");

            (await GridAsync(Admin, SortedGrid<WorkoutPlanDto>(("title", 1)))).Data!.Select(x => x.Title).ShouldBe(["A", "B"]);
            (await GridAsync(Admin, SortedGrid<WorkoutPlanDto>(("title", -1)))).Data!.Select(x => x.Title).ShouldBe(["B", "A"]);
        }

        // Every row with its own, in the order of the page.
        [Fact]
        public async Task Each_row_says_what_its_recordings_and_its_rule_say()
        {
            UserStatus status = Seed.UserStatus(x => x.Name = "Active");
            Database.Change(db => db.Users.Single(x => x.Id == _maria.Id).UserStatusId = status.Id);
            WorkoutPlan running = Seed.Plan(_maria, _rule, 2, x => x.Title = "A");
            WorkoutPlan stopped = Seed.Plan(_maria, Seed.Rule("Others", 1, 1, 1), 3, x => x.Title = "B");
            WorkoutPlan none = Seed.Plan(_nikos, change: x => x.Title = "C");
            Seed.Recording(running, MinutesAgo(5));
            Seed.Recording(stopped, MinutesAgo(60), completedOn: MinutesAgo(59));
            Seed.Recording(stopped, MinutesAgo(90), completedOn: MinutesAgo(89));

            List<WorkoutPlanDto> rows = (await GridAsync(Admin, SortedGrid<WorkoutPlanDto>(("title", 1)))).Data!.ToList();

            rows.Select(x => $"{x.Title} {x.IsRunning} {x.RecordingCount} {x.LastRecordingDurationSeconds} {x.WorkoutPlanRuleName} {x.WeekCount} {x.CurrentWeekMessage}")
                .ShouldBe(["A True 1  Beginners 2 Message 2", "B False 2 60 Others 3 Message 3", "C False 0   0 "]);
            rows[0].User.ShouldNotBeNull().FirstName.ShouldBe("Maria");
            rows[0].User!.UserStatus.ShouldNotBeNull().Name.ShouldBe("Active");
            rows[2].Id.ShouldBe(none.Id);
            rows[2].User.ShouldNotBeNull().UserStatus.ShouldBeNull();
            rows.ShouldAllBe(x => x.Exercises.Count == 0);
        }

        // Their status comes with them, and nothing more than a name and a face does.
        [Fact]
        public async Task A_member_with_a_status_is_in_the_grid_with_a_name_and_a_face()
        {
            UserStatus status = Seed.UserStatus(x => x.Name = "Active");
            Database.Change(db => db.Users.Single(x => x.Id == _maria.Id).UserStatusId = status.Id);
            Database.Change(db => db.Users.Single(x => x.Id == _nikos.Id).UserStatusId = status.Id);
            Seed.Plan(_maria);

            ApiResult<DataTableDto<WorkoutPlanDto>> result = await PostAsync<DataTableDto<WorkoutPlanDto>>(Admin, Url + "/GetDataTable", Grid<WorkoutPlanDto>());

            WorkoutPlanOwnerDto member = result.Value.Data!.Single().User.ShouldNotBeNull();
            member.FirstName.ShouldBe("Maria");
            member.UserStatus.ShouldNotBeNull().Name.ShouldBe("Active");
            member.UserStatus.Users.ShouldBeEmpty();
            result.Body.ShouldNotContain("maria@gym.test");
            result.Body.ShouldNotContain("street");
            result.Body.ShouldNotContain("private");
        }

        [Fact]
        public async Task An_empty_grid_is_an_empty_grid()
        {
            DataTableDto<WorkoutPlanDto> grid = await GridAsync(Admin, Grid<WorkoutPlanDto>());

            grid.Data!.ShouldBeEmpty();
            grid.TotalRecords.ShouldBe(0);
        }
    }
}
