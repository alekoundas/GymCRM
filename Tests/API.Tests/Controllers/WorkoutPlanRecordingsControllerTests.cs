using API.Controllers;
using API.Tests.Host;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.WorkoutPlanRecording;
using Core.Enums;
using Core.Models;
using Core.Translations;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Starting and stopping a workout. What may be started is the service's to say, and is tested
    /// with it: here is who may ask, and what they are answered.
    /// </summary>
    public sealed class WorkoutPlanRecordingsControllerTests : ApiTest
    {
        private const string Url = "api/WorkoutPlanRecordings";

        private readonly User _maria;
        private readonly User _nikos;
        private readonly WorkoutPlanRule _rule;
        private readonly WorkoutPlan _plan;

        public WorkoutPlanRecordingsControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User(x => x.Email = "maria@gym.test");
            _nikos = Seed.User();
            _rule = Seed.Rule("Beginners", 2, 3);
            _plan = Seed.Plan(_maria, _rule, 1, x => x.Title = "Legs");
        }

        // What the app gives a member.
        private static Caller Member(User user)
        {
            return Caller.Of(user, "WorkoutPlanRecordings_View", "WorkoutPlanRecordings_Add");
        }

        private static Caller Admin(User user)
        {
            return Caller.Of(user, "WorkoutPlanRecordings_View", "WorkoutPlanRecordings_Add", "WorkoutPlanRecordings_Edit", "WorkoutPlanRecordings_Delete", "WorkoutPlanRecordingsAdmin_View");
        }

        private List<WorkoutPlanRecording> All()
        {
            return Database.Read(db => db.WorkoutPlanRecordings.OrderBy(x => x.Id).ToList());
        }

        private static DateTime MinutesAgo(int minutes)
        {
            return DateTime.UtcNow.AddMinutes(-minutes);
        }


        // ================================================================
        // What the start dialog asks first
        // ================================================================

        private Task<ApiResult<WorkoutPlanStartContextDto>> ContextAsync(Caller caller, object planId)
        {
            return GetAsync<WorkoutPlanStartContextDto>(caller, $"{Url}/StartContext/{planId}");
        }

        [Fact]
        public async Task The_member_is_told_where_the_plan_stands()
        {
            ApiResult<WorkoutPlanStartContextDto> result = await ContextAsync(Member(_maria), _plan.Id);

            result.ShouldSucceed();
            result.Value.WorkoutPlanId.ShouldBe(_plan.Id);
            result.Value.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.FirstEver);
            result.Value.HasRule.ShouldBeTrue();
            result.Value.RuleName.ShouldBe("Beginners");
            result.Value.WeekCount.ShouldBe(2);
            result.Value.AvailableWeeks.ShouldBe([1, 2]);
            result.Value.CurrentWeek.ShouldBe(1);
            result.Value.CurrentWeekMessage.ShouldBe("Message 1");
            result.Value.MaxRecordings.ShouldBe(2);
            result.Value.RunningRecording.ShouldBeNull();
        }

        [Fact]
        public async Task A_running_recording_comes_with_it()
        {
            WorkoutPlanRecording recording = Seed.Recording(_plan, MinutesAgo(5));

            WorkoutPlanStartContextDto context = (await ContextAsync(Member(_maria), _plan.Id)).Value;

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.Running);
            WorkoutPlanRecordingDto running = context.RunningRecording.ShouldNotBeNull();
            running.Id.ShouldBe(recording.Id);
            running.IsRunning.ShouldBeTrue();
            running.ElapsedSeconds.ShouldNotBeNull().ShouldBeInRange(300, 330);
        }

        [Fact]
        public async Task Asking_is_for_the_member_and_the_admin()
        {
            (await ContextAsync(Admin(_nikos), _plan.Id)).ShouldSucceed();
            (await ContextAsync(Caller.Of(_nikos, "WorkoutPlanRecordings_View", "WorkoutPlanRecordingsAdmin_View"), _plan.Id)).ShouldSucceed();

            (await ContextAsync(Member(_nikos), _plan.Id)).ShouldFail(English.NotFound("WorkoutPlan"));
            (await ContextAsync(Admin(_nikos), 99)).ShouldFail(English.NotFound("WorkoutPlan"));
            (await ContextAsync(Caller.Of(_maria, "WorkoutPlanRecordings_Add", "WorkoutPlanRecordingsAdmin_View"), _plan.Id)).ShouldFail(English.NotAuthorized);
            (await ContextAsync(Caller.WithNoId("WorkoutPlanRecordings_View", "WorkoutPlanRecordingsAdmin_View"), _plan.Id)).ShouldFail(English.NotSignedIn);
            (await ContextAsync(Member(_maria), "x")).ShouldBeBad("The value 'x' is not valid.");
        }


        // ================================================================
        // Starting
        // ================================================================

        private Task<ApiResult<WorkoutPlanRecordingDto>> StartAsync(Caller caller, int planId, int week = 1)
        {
            return PostAsync<WorkoutPlanRecordingDto>(caller, Url + "/Start", new { workoutPlanId = planId, weekNumber = week });
        }

        [Fact]
        public async Task A_member_starts_their_plan()
        {
            DateTime before = DateTime.UtcNow;

            ApiResult<WorkoutPlanRecordingDto> result = await StartAsync(Member(_maria), _plan.Id, 2);

            result.ShouldSucceed();
            WorkoutPlanRecording recording = All().Single();
            recording.StartedOn.ShouldBeInRange(before, DateTime.UtcNow);
            recording.CompletedOn.ShouldBeNull();
            recording.WeekNumber.ShouldBe(2);
            recording.WorkoutPlanTitle.ShouldBe("Legs");
            recording.UserId.ShouldBe(_maria.Id);
            recording.CreatedBy_Id.ShouldBe(_maria.Id.ToString());
            Database.Read(db => db.WorkoutPlans.Single()).CurrentWeek.ShouldBe(2);

            result.Value.Id.ShouldBe(recording.Id);
            result.Value.IsRunning.ShouldBeTrue();
            result.Value.ElapsedSeconds.ShouldNotBeNull().ShouldBeInRange(0, 10);
            result.Value.UserId.ShouldBe(_maria.Id.ToString());
        }

        [Fact]
        public async Task An_admin_starts_it_for_them()
        {
            (await StartAsync(Admin(_nikos), _plan.Id)).ShouldSucceed();

            WorkoutPlanRecording recording = All().Single();
            recording.UserId.ShouldBe(_maria.Id);
            recording.CreatedBy_Id.ShouldBe(_nikos.Id.ToString());
        }

        [Fact]
        public async Task What_the_service_refuses_is_said_in_words()
        {
            (await StartAsync(Member(_maria), 99)).ShouldFail(English.NotFound("WorkoutPlan"));
            (await StartAsync(Member(_nikos), _plan.Id)).ShouldFail(English.NotAuthorized);
            (await StartAsync(Member(_maria), _plan.Id, 9)).ShouldFail(English.Of(TranslationKeys.Selected_week_does_not_exist_in_the_rule));
            (await StartAsync(Member(_maria), Seed.Plan(_maria).Id)).ShouldFail(English.Of(TranslationKeys.Workout_plan_has_no_rule_assigned));

            await StartAsync(Member(_maria), _plan.Id);
            (await StartAsync(Member(_maria), _plan.Id)).ShouldFail(English.Of(TranslationKeys.A_recording_is_already_running_for_this_plan));
            All().Count.ShouldBe(1);
        }

        [Fact]
        public async Task Starting_needs_the_add_permission_and_an_id()
        {
            (await StartAsync(Caller.Of(_maria, "WorkoutPlanRecordings_View", "WorkoutPlanRecordingsAdmin_View"), _plan.Id)).ShouldFail(English.NotAuthorized);
            (await StartAsync(Caller.WithNoId("WorkoutPlanRecordings_Add", "WorkoutPlanRecordingsAdmin_View"), _plan.Id)).ShouldFail(English.NotSignedIn);

            All().ShouldBeEmpty();
        }


        // ================================================================
        // Stopping
        // ================================================================

        private Task<ApiResult<WorkoutPlanRecordingDto>> StopAsync(Caller caller, object id)
        {
            return PostAsync<WorkoutPlanRecordingDto>(caller, $"{Url}/Stop/{id}", null);
        }

        [Fact]
        public async Task A_member_stops_it_and_so_does_an_admin()
        {
            foreach (Caller caller in new[] { Member(_maria), Admin(_nikos) })
            {
                WorkoutPlanRecording recording = Seed.Recording(_plan, MinutesAgo(5));

                ApiResult<WorkoutPlanRecordingDto> result = await StopAsync(caller, recording.Id);

                result.ShouldSucceed();
                result.Value.Id.ShouldBe(recording.Id);
                result.Value.IsRunning.ShouldBeFalse();
                result.Value.ElapsedSeconds.ShouldBeNull();
                result.Value.DurationSeconds.ShouldNotBeNull().ShouldBeInRange(300, 330);
                result.Value.CompletedOn.ShouldNotBeNull();
                All().Last().DurationSeconds.ShouldBe(result.Value.DurationSeconds);
            }
        }

        [Fact]
        public async Task What_cannot_be_stopped_is_said_in_words()
        {
            WorkoutPlanRecording lapsed = Seed.Recording(_plan, MinutesAgo(181));
            WorkoutPlanRecording running = Seed.Recording(_plan, MinutesAgo(5));

            (await StopAsync(Member(_maria), 99)).ShouldFail(English.NotFound("WorkoutPlanRecording"));
            (await StopAsync(Member(_nikos), running.Id)).ShouldFail(English.NotAuthorized);
            (await StopAsync(Member(_maria), lapsed.Id)).ShouldFail(English.Of(TranslationKeys.The_recording_can_no_longer_be_stopped));
            (await StopAsync(Member(_maria), "x")).ShouldBeBad("The value 'x' is not valid.");

            All().ShouldAllBe(x => x.CompletedOn == null);
        }

        [Fact]
        public async Task Stopping_needs_the_add_permission_and_an_id()
        {
            WorkoutPlanRecording running = Seed.Recording(_plan, MinutesAgo(5));

            (await StopAsync(Caller.Of(_maria, "WorkoutPlanRecordings_View", "WorkoutPlanRecordings_Edit", "WorkoutPlanRecordingsAdmin_View"), running.Id)).ShouldFail(English.NotAuthorized);
            (await StopAsync(Caller.WithNoId("WorkoutPlanRecordings_Add", "WorkoutPlanRecordingsAdmin_View"), running.Id)).ShouldFail(English.NotSignedIn);

            All().Single().CompletedOn.ShouldBeNull();
        }

        // Done, and nothing to show for it: the real service never says so.
        [Fact]
        public async Task A_service_that_hands_nothing_back_is_an_error()
        {
            WorkoutPlanRecordingsController controller = Direct.Controller<WorkoutPlanRecordingsController>(Services, "WorkoutPlanRecordings", ["WorkoutPlanRecordings_Add"], _maria, new EmptyHanded());

            (await controller.Start(new WorkoutPlanStartRequestDto())).Messages["error"].ShouldBe([English.Invalid]);
            (await controller.Stop(1)).Messages["error"].ShouldBe([English.Invalid]);
        }

        private sealed class EmptyHanded : IWorkoutPlanRecordingService
        {
            public Task<WorkoutPlanRecordingResult> StartAsync(int workoutPlanId, int weekNumber, Guid callerId, bool callerIsAdmin)
            {
                return Task.FromResult(new WorkoutPlanRecordingResult { IsSucceed = true, ErrorKey = TranslationKeys.Invalid_data_provided });
            }

            public Task<WorkoutPlanRecordingResult> StopAsync(int recordingId, Guid callerId, bool callerIsAdmin)
            {
                return Task.FromResult(new WorkoutPlanRecordingResult { IsSucceed = true, ErrorKey = TranslationKeys.Invalid_data_provided });
            }

            public Task<WorkoutPlanStartContextDto?> GetStartContextAsync(int workoutPlanId, Guid callerId, bool callerIsAdmin)
            {
                throw new NotSupportedException();
            }

            public bool IsRunning(WorkoutPlanRecording recording, DateTime nowUtc)
            {
                throw new NotSupportedException();
            }

            public bool IsIncomplete(WorkoutPlanRecording recording, DateTime nowUtc)
            {
                throw new NotSupportedException();
            }

            public WorkoutPlanRecordingDto ToDto(WorkoutPlanRecording recording, DateTime nowUtc)
            {
                throw new NotSupportedException();
            }
        }


        // ================================================================
        // Reading one
        // ================================================================

        private Task<ApiResult<WorkoutPlanRecordingDto>> ReadAsync(Caller caller, object id)
        {
            return GetAsync<WorkoutPlanRecordingDto>(caller, $"{Url}/{id}");
        }

        [Fact]
        public async Task One_is_read_with_what_is_worked_out_about_it()
        {
            WorkoutPlanRecording running = Seed.Recording(_plan, MinutesAgo(5), 2);
            WorkoutPlanRecording lapsed = Seed.Recording(_plan, MinutesAgo(181));
            WorkoutPlanRecording stopped = Seed.Recording(_plan, MinutesAgo(60), completedOn: MinutesAgo(50));

            WorkoutPlanRecordingDto first = (await ReadAsync(Member(_maria), running.Id)).Value;
            first.IsRunning.ShouldBeTrue();
            first.IsIncomplete.ShouldBeFalse();
            first.ElapsedSeconds.ShouldNotBeNull().ShouldBeInRange(300, 330);
            first.WeekNumber.ShouldBe(2);
            first.WorkoutPlanTitle.ShouldBe("Legs");

            WorkoutPlanRecordingDto second = (await ReadAsync(Member(_maria), lapsed.Id)).Value;
            second.IsRunning.ShouldBeFalse();
            second.IsIncomplete.ShouldBeTrue();
            second.ElapsedSeconds.ShouldBeNull();

            WorkoutPlanRecordingDto third = (await ReadAsync(Member(_maria), stopped.Id)).Value;
            third.IsRunning.ShouldBeFalse();
            third.IsIncomplete.ShouldBeFalse();
            third.DurationSeconds.ShouldBe(600);
        }

        [Fact]
        public async Task One_is_its_members_and_an_admins_to_read()
        {
            WorkoutPlanRecording recording = Seed.Recording(_plan, MinutesAgo(5));

            (await ReadAsync(Admin(_nikos), recording.Id)).ShouldSucceed();
            (await ReadAsync(Member(_nikos), recording.Id)).ShouldFail(English.NotFound("WorkoutPlanRecording"));
            (await ReadAsync(Caller.WithNoId("WorkoutPlanRecordings_View"), recording.Id)).ShouldFail(English.NotFound("WorkoutPlanRecording"));
            (await ReadAsync(Admin(_nikos), 99)).ShouldFail(English.NotFound("WorkoutPlanRecording"));
            (await ReadAsync(Caller.Of(_maria, "WorkoutPlanRecordings_Add", "WorkoutPlanRecordingsAdmin_View"), recording.Id)).ShouldFail(English.NotAuthorized);
            (await ReadAsync(Admin(_nikos), "x")).ShouldBreak();
        }


        // ================================================================
        // Writing them outright
        // ================================================================

        // They are only ever begun by Start, which asks everything this would not.
        [Fact]
        public async Task Adding_one_outright_is_refused_to_everybody()
        {
            (await PostAsync<List<WorkoutPlanRecording>>(Admin(_nikos), Url, new[] { new { workoutPlanId = _plan.Id, weekNumber = 1 } })).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<WorkoutPlanRecording>>(Member(_maria), Url, Array.Empty<object>())).ShouldFail(English.NotAuthorized);

            All().ShouldBeEmpty();
        }

        // Members have neither permission.
        [Fact]
        public async Task Changing_and_deleting_one_is_the_admins()
        {
            WorkoutPlanRecording recording = Seed.Recording(_plan, MinutesAgo(60), completedOn: MinutesAgo(50));
            DateTime startedOn = new DateTime(2025, 1, 2, 10, 0, 0, DateTimeKind.Utc);
            object body = new { id = recording.Id, startedOn, completedOn = startedOn.AddMinutes(1), durationSeconds = 60, weekNumber = 2, workoutPlanId = _plan.Id, workoutPlanTitle = "Legs", userId = _maria.Id, createdOn = startedOn };

            (await PutAsync<WorkoutPlanRecording>(Member(_maria), $"{Url}/{recording.Id}", body)).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<WorkoutPlanRecording>(Member(_maria), $"{Url}/{recording.Id}")).ShouldFail(English.NotAuthorized);
            All().Single().WeekNumber.ShouldBe(1);

            (await PutAsync<WorkoutPlanRecording>(Admin(_nikos), $"{Url}/{recording.Id}", body)).ShouldSucceed(English.Updated("WorkoutPlanRecording"));
            All().Single().WeekNumber.ShouldBe(2);
            All().Single().StartedOn.ShouldBe(startedOn);
            All().Single().DurationSeconds.ShouldBe(60);

            (await DeleteAsync<WorkoutPlanRecording>(Admin(_nikos), $"{Url}/{recording.Id}")).ShouldSucceed(English.Deleted("WorkoutPlanRecording"));
            All().ShouldBeEmpty();
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<DataTableDto<WorkoutPlanRecordingDto>> GridAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            return (await PostAsync<DataTableDto<WorkoutPlanRecordingDto>>(caller, Url + "/GetDataTable", Grid<WorkoutPlanRecordingDto>(filters))).Value;
        }

        [Fact]
        public async Task An_admin_sees_everybodys_and_a_member_their_own()
        {
            WorkoutPlanRecording maria = Seed.Recording(_plan, MinutesAgo(60));
            WorkoutPlanRecording nikos = Seed.Recording(Seed.Plan(_nikos), MinutesAgo(60));

            (await GridAsync(Admin(_nikos))).Data!.Select(x => x.Id!.Value).ShouldBe([maria.Id, nikos.Id], ignoreOrder: true);

            DataTableDto<WorkoutPlanRecordingDto> own = await GridAsync(Member(_maria));
            own.Data!.Single().Id.ShouldBe(maria.Id);
            own.Data!.Single().User.ShouldNotBeNull().Email.ShouldBe("maria@gym.test");
            own.TotalRecords.ShouldBe(1);

            (await GridAsync(Member(_maria), Filter("UserId", _nikos.Id.ToString(), DataTableFiltersEnum.equals))).TotalRecords.ShouldBe(0);
            (await GridAsync(Caller.WithNoId("WorkoutPlanRecordings_View"))).TotalRecords.ShouldBe(0);
        }

        [Fact]
        public async Task Each_row_says_whether_it_runs_and_for_how_long()
        {
            Seed.Recording(_plan, MinutesAgo(5));
            Seed.Recording(_plan, MinutesAgo(181));
            Seed.Recording(_plan, MinutesAgo(60), completedOn: MinutesAgo(50));

            List<WorkoutPlanRecordingDto> rows = (await GridAsync(Member(_maria))).Data!.OrderBy(x => x.Id).ToList();

            rows.Select(x => $"{x.IsRunning} {x.IsIncomplete} {x.ElapsedSeconds != null} {x.DurationSeconds}").ShouldBe(["True False True ", "False True False ", "False False False 600"]);
            rows[0].ElapsedSeconds.ShouldNotBeNull().ShouldBeInRange(300, 330);
        }

        [Fact]
        public async Task It_sorts_by_how_long_they_took()
        {
            WorkoutPlanRecording longer = Seed.Recording(_plan, MinutesAgo(60), completedOn: MinutesAgo(50));
            WorkoutPlanRecording shorter = Seed.Recording(_plan, MinutesAgo(60), completedOn: MinutesAgo(55));

            List<WorkoutPlanRecordingDto> rows = (await PostAsync<DataTableDto<WorkoutPlanRecordingDto>>(Member(_maria), Url + "/GetDataTable", SortedGrid<WorkoutPlanRecordingDto>(("durationSeconds", 1)))).Value.Data!.ToList();

            rows.Select(x => x.Id!.Value).ShouldBe([shorter.Id, longer.Id]);
        }

        [Fact]
        public async Task An_empty_grid_is_an_empty_grid()
        {
            (await GridAsync(Admin(_nikos))).TotalRecords.ShouldBe(0);
        }
    }
}
