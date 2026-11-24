using Business.Services;
using Business.Tests.Fakes;
using Core.Dtos.WorkoutPlanRecording;
using Core.Enums;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

namespace Business.Tests.Services
{
    /// <summary>
    /// Starting and stopping a workout, and which week it is recorded on.
    /// </summary>
    public sealed class WorkoutPlanRecordingServiceTests : IDisposable
    {
        private static readonly DateTime Now = new DateTime(2031, 3, 5, 12, 0, 0, DateTimeKind.Utc);

        private readonly TestDatabase _database = new TestDatabase();
        private readonly Seed _seed;
        private readonly WorkoutPlanRecordingService _service;
        private readonly User _member;
        private readonly User _other;

        public WorkoutPlanRecordingServiceTests()
        {
            _seed = new Seed(_database);
            _service = new WorkoutPlanRecordingService(DataServices.On(_database));
            _member = _seed.User();
            _other = _seed.User();
        }

        public void Dispose()
        {
            _database.Dispose();
        }


        // ================================================================
        // The numbers
        // ================================================================

        [Fact]
        public void A_recording_lapses_after_three_hours_and_a_member_is_away_after_twelve_days()
        {
            WorkoutPlanRecordingService.LapseAfterHours.ShouldBe(3);
            WorkoutPlanRecordingService.AwayGapDays.ShouldBe(12);
        }

        [Fact]
        public void A_rule_may_have_its_own_idea_of_away()
        {
            WorkoutPlanRecordingService.ResolveAwayGapDays(null).ShouldBe(12);
            WorkoutPlanRecordingService.ResolveAwayGapDays(new WorkoutPlanRule()).ShouldBe(12);
            WorkoutPlanRecordingService.ResolveAwayGapDays(new WorkoutPlanRule { AwayGapDays = 1 }).ShouldBe(1);
            WorkoutPlanRecordingService.ResolveAwayGapDays(new WorkoutPlanRule { AwayGapDays = 365 }).ShouldBe(365);
            WorkoutPlanRecordingService.ResolveAwayGapDays(new WorkoutPlanRule { AwayGapDays = 0 }).ShouldBe(0);
        }


        // ================================================================
        // Running, lapsed, done
        // ================================================================

        private static readonly TimeSpan Limit = TimeSpan.FromHours(3);

        // How long ago it was started, and whether it can still be stopped.
        public static readonly TheoryData<TimeSpan, bool> Ages = new TheoryData<TimeSpan, bool>
        {
            { TimeSpan.FromHours(-1), true },
            { TimeSpan.Zero, true },
            { TimeSpan.FromMinutes(90), true },
            { Limit - TimeSpan.FromTicks(1), true },
            { Limit, true },
            { Limit + TimeSpan.FromTicks(1), false },
            { TimeSpan.FromDays(30), false },
        };

        [Theory]
        [MemberData(nameof(Ages))]
        public void An_open_recording_runs_for_three_hours_and_is_incomplete_after(TimeSpan age, bool isRunning)
        {
            WorkoutPlanRecording recording = new WorkoutPlanRecording { StartedOn = Now - age };

            _service.IsRunning(recording, Now).ShouldBe(isRunning);
            _service.IsIncomplete(recording, Now).ShouldBe(!isRunning);
        }

        [Theory]
        [MemberData(nameof(Ages))]
        public void A_stopped_recording_neither_runs_nor_is_incomplete(TimeSpan age, bool isRunning)
        {
            WorkoutPlanRecording recording = new WorkoutPlanRecording { StartedOn = Now - age, CompletedOn = Now };

            _service.IsRunning(recording, Now).ShouldBeFalse($"running before it was stopped: {isRunning}");
            _service.IsIncomplete(recording, Now).ShouldBeFalse();
        }

        private static WorkoutPlanRecording Recording(DateTime startedOn, DateTime? completedOn)
        {
            return new WorkoutPlanRecording
            {
                Id = 4,
                StartedOn = startedOn,
                CompletedOn = completedOn,
                DurationSeconds = completedOn == null ? null : 77,
                WeekNumber = 2,
                WorkoutPlanId = 9,
                WorkoutPlanTitle = "Legs",
                UserId = new Guid("11111111-2222-3333-4444-555555555555"),
                CreatedOn = Now.AddDays(-1)
            };
        }

        [Theory]
        [MemberData(nameof(Ages))]
        public void An_open_recording_is_sent_with_what_is_worked_out_about_it(TimeSpan age, bool isRunning)
        {
            WorkoutPlanRecordingDto dto = _service.ToDto(Recording(Now - age, null), Now);

            dto.Id.ShouldBe(4);
            dto.StartedOn.ShouldBe(Now - age);
            dto.CompletedOn.ShouldBeNull();
            dto.DurationSeconds.ShouldBeNull();
            dto.WeekNumber.ShouldBe(2);
            dto.WorkoutPlanId.ShouldBe(9);
            dto.WorkoutPlanTitle.ShouldBe("Legs");
            dto.UserId.ShouldBe("11111111-2222-3333-4444-555555555555");
            dto.User.ShouldBeNull();
            dto.CreatedOn.ShouldBe(Now.AddDays(-1));
            dto.IsRunning.ShouldBe(isRunning);
            dto.IsIncomplete.ShouldBe(!isRunning);
            dto.ElapsedSeconds.ShouldBe(isRunning ? (int)age.TotalSeconds : null);
        }

        [Fact]
        public void A_stopped_recording_is_sent_with_its_duration_and_no_running_time()
        {
            WorkoutPlanRecordingDto dto = _service.ToDto(Recording(Now.AddMinutes(-50), Now.AddMinutes(-1)), Now);

            dto.CompletedOn.ShouldBe(Now.AddMinutes(-1));
            dto.DurationSeconds.ShouldBe(77);
            dto.IsRunning.ShouldBeFalse();
            dto.IsIncomplete.ShouldBeFalse();
            dto.ElapsedSeconds.ShouldBeNull();
        }

        [Fact]
        public void A_recording_whose_plan_is_gone_is_sent_without_one()
        {
            WorkoutPlanRecording recording = Recording(Now, null);
            recording.WorkoutPlanId = null;

            _service.ToDto(recording, Now).WorkoutPlanId.ShouldBeNull();
        }

        [Fact]
        public void Half_a_second_is_not_counted_yet()
        {
            _service.ToDto(Recording(Now.AddSeconds(-59.9), null), Now).ElapsedSeconds.ShouldBe(59);
        }


        // ================================================================
        // How many in a row on one week
        // ================================================================

        // The newest first: how many days ago each was, and its week.
        private static List<WorkoutPlanRecording> History(params (double DaysAgo, int Week)[] recordings)
        {
            return recordings.Select(x => new WorkoutPlanRecording { StartedOn = Now.AddDays(-x.DaysAgo), WeekNumber = x.Week }).ToList();
        }

        private static int Count(List<WorkoutPlanRecording> newestFirst, int week = 2, int gap = 12)
        {
            return WorkoutPlanRecordingService.CountConsecutiveOnWeek(newestFirst, week, Now, gap);
        }

        [Fact]
        public void Nothing_recorded_is_none()
        {
            Count(History()).ShouldBe(0);
        }

        [Fact]
        public void Recordings_of_the_week_one_after_another_are_counted()
        {
            Count(History((1, 2))).ShouldBe(1);
            Count(History((1, 2), (3, 2), (5, 2))).ShouldBe(3);
        }

        [Fact]
        public void The_count_stops_at_another_week()
        {
            Count(History((1, 1), (3, 2))).ShouldBe(0);
            Count(History((1, 2), (3, 1), (5, 2))).ShouldBe(1);
            Count(History((1, 2), (3, 2), (5, 3), (6, 2))).ShouldBe(2);
        }

        // The gap is measured from now to the newest, then between one and the next.
        [Fact]
        public void The_count_stops_at_a_gap_longer_than_allowed()
        {
            Count(History((12, 2))).ShouldBe(1);
            Count(History((12.001, 2))).ShouldBe(0);

            Count(History((1, 2), (13, 2))).ShouldBe(2);
            Count(History((1, 2), (13.001, 2))).ShouldBe(1);

            Count(History((1, 2), (12, 2), (24, 2), (36, 2))).ShouldBe(4);
            Count(History((1, 2), (12, 2), (24.5, 2), (36, 2))).ShouldBe(2);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(365, 4)]
        public void The_gap_is_the_one_asked_for(int gap, int expected)
        {
            Count(History((1, 2), (3, 2), (6, 2), (100, 2)), gap: gap).ShouldBe(expected);
        }

        [Fact]
        public void A_gap_of_nought_still_counts_one_from_this_very_moment()
        {
            Count(History((0, 2), (0, 2)), gap: 0).ShouldBe(2);
        }

        // Week by week, gap by gap, against counting them by hand: three recordings, each on one of
        // two weeks and each either soon after the one before or too long after it.
        [Fact]
        public void Every_history_of_three_is_counted_until_the_first_that_does_not_belong()
        {
            foreach (int weeks in Enumerable.Range(0, 8))
                foreach (int gaps in Enumerable.Range(0, 8))
                {
                    List<WorkoutPlanRecording> history = new List<WorkoutPlanRecording>();
                    DateTime cursor = Now;
                    int expected = 0;
                    bool isOver = false;

                    for (int i = 0; i < 3; i++)
                    {
                        bool isTheWeek = (weeks >> i & 1) == 1;
                        bool isInTime = (gaps >> i & 1) == 1;

                        cursor = cursor.AddDays(isInTime ? -5 : -6);
                        history.Add(new WorkoutPlanRecording { StartedOn = cursor, WeekNumber = isTheWeek ? 2 : 1 });

                        isOver |= !isTheWeek || !isInTime;
                        if (!isOver)
                            expected++;
                    }

                    Count(history, gap: 5).ShouldBe(expected, $"weeks {weeks}, gaps {gaps}");
                }
        }


        // ================================================================
        // The week after
        // ================================================================

        // Every week there could be, and some there could not, of every rule up to fifteen weeks.
        [Fact]
        public void The_week_after_is_the_next_one_and_after_the_last_the_first()
        {
            for (int count = -2; count <= 15; count++)
                foreach (int? week in new int?[] { null }.Concat(Enumerable.Range(-2, 20).Select(x => (int?)x)))
                {
                    bool isInTheRule = week >= 1 && week <= count;
                    int expected = isInTheRule && week < count ? week!.Value + 1 : 1;

                    WorkoutPlanRecordingService.ResolveNextWeek(week, count).ShouldBe(expected, $"week {week} of {count}");
                }
        }


        // ================================================================
        // What the Start dialog is told
        // ================================================================

        private Task<WorkoutPlanStartContextDto?> ContextAsync(WorkoutPlan plan)
        {
            return _service.GetStartContextAsync(plan.Id, plan.UserId, false);
        }

        private static DateTime DaysAgo(double days)
        {
            // A minute's slack, so the time the test takes never reaches an edge.
            return DateTime.UtcNow.AddDays(-days).AddMinutes(1);
        }

        private WorkoutPlanRecording Done(WorkoutPlan plan, double daysAgo, int week)
        {
            DateTime startedOn = DaysAgo(daysAgo);

            return _seed.Recording(plan, startedOn, week, startedOn.AddMinutes(50));
        }

        [Fact]
        public async Task A_plan_that_is_not_there_has_no_context()
        {
            (await _service.GetStartContextAsync(99, _member.Id, false)).ShouldBeNull();
            (await _service.GetStartContextAsync(99, _member.Id, true)).ShouldBeNull();
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, true, true)]
        // Somebody else's plan reads as one that is not there.
        [InlineData(false, false, false)]
        public async Task A_plan_is_its_members_and_an_admins_to_read(bool isOwn, bool isAdmin, bool mayRead)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1));

            WorkoutPlanStartContextDto? context = await _service.GetStartContextAsync(plan.Id, isOwn ? _member.Id : _other.Id, isAdmin);

            (context != null).ShouldBe(mayRead);
        }

        [Fact]
        public async Task A_plan_with_no_rule_cannot_be_started()
        {
            WorkoutPlan plan = _seed.Plan(_member, currentWeek: 3);
            Done(plan, 1, 3);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.WorkoutPlanId.ShouldBe(plan.Id);
            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.NoRule);
            context.HasRule.ShouldBeFalse();
            context.RuleName.ShouldBe("");
            context.CurrentWeek.ShouldBe(3);
            context.WeekCount.ShouldBe(0);
            context.NextWeek.ShouldBe(0);
            context.AvailableWeeks.ShouldBeEmpty();
            context.LastRecordingOn.ShouldBeNull();
            context.RunningRecording.ShouldBeNull();
        }

        [Theory]
        // Where the trainer put it, or the first week when that is no week of the rule.
        [InlineData(null, 1, "", 0)]
        [InlineData(1, 1, "Message 1", 2)]
        [InlineData(2, 2, "Message 2", 1)]
        [InlineData(3, 3, "Message 3", 3)]
        [InlineData(4, 1, "", 0)]
        [InlineData(0, 1, "", 0)]
        public async Task A_plan_never_recorded_starts_where_it_was_put(int? currentWeek, int nextWeek, string message, int max)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 2, 1, 3), currentWeek);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.FirstEver);
            context.HasRule.ShouldBeTrue();
            context.RuleName.ShouldBe("Rule");
            context.WeekCount.ShouldBe(3);
            context.AvailableWeeks.ShouldBe([1, 2, 3]);
            context.CurrentWeek.ShouldBe(currentWeek);
            context.CurrentWeekMessage.ShouldBe(message);
            context.MaxRecordings.ShouldBe(max);
            context.NextWeek.ShouldBe(nextWeek);
            context.RecordingsOnCurrentWeek.ShouldBe(0);
            context.RemainingOnCurrentWeek.ShouldBe(0);
            context.LastRecordingOn.ShouldBeNull();
            context.DaysSinceLastRecording.ShouldBeNull();
            context.RunningRecording.ShouldBeNull();
        }

        [Fact]
        public async Task The_weeks_are_offered_in_their_order_whatever_order_they_were_written_in()
        {
            WorkoutPlanRule rule = _seed.Rule("Rule");
            foreach (int week in new[] { 3, 1, 2 })
                _database.Add(new WorkoutPlanRuleWeek { WorkoutPlanRuleId = rule.Id, WeekNumber = week, MaxRecordings = 1 });

            (await ContextAsync(_seed.Plan(_member, rule))).ShouldNotBeNull().AvailableWeeks.ShouldBe([1, 2, 3]);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        [InlineData(3, 1)]
        [InlineData(null, 1)]
        [InlineData(9, 1)]
        public async Task A_running_recording_comes_before_everything_else(int? currentWeek, int nextWeek)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1, 1), currentWeek);
            Done(plan, 30, 1);
            WorkoutPlanRecording running = _seed.Recording(plan, DateTime.UtcNow.AddMinutes(-90), 2);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.Running);
            context.NextWeek.ShouldBe(nextWeek);
            context.WeekCount.ShouldBe(3);
            context.AvailableWeeks.ShouldBe([1, 2, 3]);

            WorkoutPlanRecordingDto recording = context.RunningRecording.ShouldNotBeNull();
            recording.Id.ShouldBe(running.Id);
            recording.IsRunning.ShouldBeTrue();
            recording.ElapsedSeconds.ShouldNotBeNull().ShouldBeInRange(90 * 60, 90 * 60 + 60);

            // Nothing is worked out beyond it.
            context.CurrentWeekMessage.ShouldBe("");
            context.MaxRecordings.ShouldBe(0);
            context.LastRecordingOn.ShouldBeNull();
        }

        [Fact]
        public async Task A_recording_that_lapsed_is_not_running_and_still_counts()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1), currentWeek: 1);
            _seed.Recording(plan, DateTime.UtcNow.AddHours(-3).AddMinutes(-1), 1);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.AtMax);
            context.RunningRecording.ShouldBeNull();
        }

        [Fact]
        public async Task A_recording_just_short_of_lapsing_is_running()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1), currentWeek: 1);
            _seed.Recording(plan, DateTime.UtcNow.AddHours(-3).AddMinutes(1), 1);

            (await ContextAsync(plan)).ShouldNotBeNull().Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.Running);
        }

        // A week's limit against how many of it were recorded.
        [Theory]
        [InlineData(1, 1, WorkoutPlanStartScenarioEnum.AtMax, 0)]
        [InlineData(1, 2, WorkoutPlanStartScenarioEnum.AtMax, 0)]
        [InlineData(2, 1, WorkoutPlanStartScenarioEnum.UnderMax, 1)]
        [InlineData(2, 2, WorkoutPlanStartScenarioEnum.AtMax, 0)]
        [InlineData(5, 2, WorkoutPlanStartScenarioEnum.UnderMax, 3)]
        [InlineData(5, 4, WorkoutPlanStartScenarioEnum.UnderMax, 1)]
        [InlineData(5, 5, WorkoutPlanStartScenarioEnum.AtMax, 0)]
        [InlineData(5, 7, WorkoutPlanStartScenarioEnum.AtMax, 0)]
        public async Task A_week_has_so_many_recordings_in_it(int max, int recorded, WorkoutPlanStartScenarioEnum scenario, int remaining)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", max, 1), currentWeek: 1);
            for (int i = 1; i <= recorded; i++)
                Done(plan, i, 1);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(scenario);
            context.RecordingsOnCurrentWeek.ShouldBe(recorded);
            context.RemainingOnCurrentWeek.ShouldBe(remaining);
            context.MaxRecordings.ShouldBe(max);
            context.CurrentWeekMessage.ShouldBe("Message 1");
            context.NextWeek.ShouldBe(2);
        }

        // Whichever was written down first.
        [Theory]
        [InlineData(2.5, 40.5)]
        [InlineData(40.5, 2.5)]
        public async Task The_latest_recording_is_the_one_that_counts(double first, double second)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 2), currentWeek: 1);
            Done(plan, first, 1);
            Done(plan, second, 1);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.UnderMax);
            context.DaysSinceLastRecording.ShouldBe(2);
            context.LastRecordingOn.ShouldNotBeNull().ShouldBe(DateTime.UtcNow.AddDays(-2.5), TimeSpan.FromMinutes(1));
            // The other is from before the gap.
            context.RecordingsOnCurrentWeek.ShouldBe(1);
        }

        // A week moved by hand starts its count afresh.
        [Fact]
        public async Task Recordings_of_the_week_before_do_not_count()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 2), currentWeek: 2);
            Done(plan, 3, 1);
            Done(plan, 5, 1);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.UnderMax);
            context.RecordingsOnCurrentWeek.ShouldBe(0);
            context.RemainingOnCurrentWeek.ShouldBe(2);
            context.NextWeek.ShouldBe(1);
        }

        [Theory]
        // The built-in gap is twelve days.
        [InlineData(null, 11.9, WorkoutPlanStartScenarioEnum.UnderMax)]
        [InlineData(null, 12.0, WorkoutPlanStartScenarioEnum.UnderMax)]
        [InlineData(null, 12.1, WorkoutPlanStartScenarioEnum.AwayTooLong)]
        [InlineData(null, 100.0, WorkoutPlanStartScenarioEnum.AwayTooLong)]
        // A rule may shorten or lengthen it.
        [InlineData(1, 1.0, WorkoutPlanStartScenarioEnum.UnderMax)]
        [InlineData(1, 1.1, WorkoutPlanStartScenarioEnum.AwayTooLong)]
        [InlineData(30, 12.1, WorkoutPlanStartScenarioEnum.UnderMax)]
        [InlineData(30, 30.1, WorkoutPlanStartScenarioEnum.AwayTooLong)]
        public async Task Nothing_recorded_for_longer_than_the_gap_is_away_too_long(int? gap, double daysAgo, WorkoutPlanStartScenarioEnum scenario)
        {
            WorkoutPlanRule rule = _seed.Rule("Rule", 5, 1);
            _database.Change(db => db.WorkoutPlanRules.Single().AwayGapDays = gap);
            WorkoutPlan plan = _seed.Plan(_member, rule, currentWeek: 1);
            WorkoutPlanRecording last = Done(plan, daysAgo, 1);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(scenario);
            context.LastRecordingOn.ShouldNotBeNull().ShouldBe(last.StartedOn, TimeSpan.FromSeconds(1));
            context.DaysSinceLastRecording.ShouldBe((int)(daysAgo - 0.001));
            context.NextWeek.ShouldBe(2);
            context.CurrentWeekMessage.ShouldBe("Message 1");

            // Away, nothing is counted.
            context.RecordingsOnCurrentWeek.ShouldBe(scenario == WorkoutPlanStartScenarioEnum.UnderMax ? 1 : 0);
        }

        [Fact]
        public async Task Away_comes_before_a_week_that_is_gone()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1), currentWeek: 9);
            Done(plan, 13, 9);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.AwayTooLong);
            context.NextWeek.ShouldBe(1);
            context.MaxRecordings.ShouldBe(0);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(3)]
        public async Task A_week_the_rule_no_longer_has_starts_again_from_the_first(int? currentWeek)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1), currentWeek);
            Done(plan, 2, 3);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.Orphaned);
            context.NextWeek.ShouldBe(1);
            context.MaxRecordings.ShouldBe(0);
            context.CurrentWeekMessage.ShouldBe("");
            context.RecordingsOnCurrentWeek.ShouldBe(0);
            context.RemainingOnCurrentWeek.ShouldBe(0);
            context.DaysSinceLastRecording.ShouldBe(1);
        }

        [Fact]
        public async Task A_rule_with_no_weeks_at_all_orphans_every_plan()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule"), currentWeek: 1);
            Done(plan, 2, 1);

            WorkoutPlanStartContextDto context = (await ContextAsync(plan)).ShouldNotBeNull();

            context.Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.Orphaned);
            context.WeekCount.ShouldBe(0);
            context.AvailableWeeks.ShouldBeEmpty();
        }

        [Fact]
        public async Task Another_plans_recordings_do_not_count()
        {
            WorkoutPlanRule rule = _seed.Rule("Rule", 1);
            WorkoutPlan plan = _seed.Plan(_member, rule, currentWeek: 1);
            WorkoutPlan another = _seed.Plan(_member, rule, currentWeek: 1);
            Done(another, 1, 1);
            _seed.Recording(another, DateTime.UtcNow.AddMinutes(-5), 1);

            (await ContextAsync(plan)).ShouldNotBeNull().Scenario.ShouldBe(WorkoutPlanStartScenarioEnum.FirstEver);
        }


        // ================================================================
        // Starting
        // ================================================================

        private int RecordingCount()
        {
            return _database.Read(db => db.WorkoutPlanRecordings.Count());
        }

        private static void ShouldFail(WorkoutPlanRecordingResult result, string key, string argument = "")
        {
            result.IsSucceed.ShouldBeFalse();
            result.ErrorKey.ShouldBe(key);
            result.ErrorArgument.ShouldBe(argument);
            result.Recording.ShouldBeNull();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        // At the desk, for the member.
        [InlineData(false, true)]
        public async Task Starting_writes_a_recording_under_the_member_and_moves_the_plan(bool isOwn, bool isAdmin)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1, 1), currentWeek: 1);
            User caller = isOwn ? _member : _other;
            DateTime before = DateTime.UtcNow;

            WorkoutPlanRecordingResult result = await _service.StartAsync(plan.Id, 3, caller.Id, isAdmin);

            result.IsSucceed.ShouldBeTrue();
            result.ErrorKey.ShouldBe("");
            result.ErrorArgument.ShouldBe("");

            WorkoutPlanRecording recording = _database.Read(db => db.WorkoutPlanRecordings.Single());
            recording.Id.ShouldBe(result.Recording.ShouldNotBeNull().Id);
            recording.StartedOn.ShouldBeInRange(before, DateTime.UtcNow);
            recording.CreatedOn.ShouldBe(recording.StartedOn);
            recording.CompletedOn.ShouldBeNull();
            recording.DurationSeconds.ShouldBeNull();
            recording.WeekNumber.ShouldBe(3);
            recording.WorkoutPlanId.ShouldBe(plan.Id);
            recording.WorkoutPlanTitle.ShouldBe(plan.Title);
            recording.UserId.ShouldBe(_member.Id);
            recording.CreatedBy_Id.ShouldBe(caller.Id.ToString());
            recording.CreatedBy_FullName.ShouldBe("");

            _database.Read(db => db.WorkoutPlans.Single()).CurrentWeek.ShouldBe(3);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task A_plan_that_is_not_there_cannot_be_started(bool isAdmin)
        {
            ShouldFail(await _service.StartAsync(99, 1, _member.Id, isAdmin), TranslationKeys.Requested_0_not_found, "WorkoutPlan");
        }

        [Fact]
        public async Task Somebody_elses_plan_cannot_be_started()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1), currentWeek: 1);

            ShouldFail(await _service.StartAsync(plan.Id, 1, _other.Id, false), TranslationKeys.User_is_not_authorized_to_perform_this_action);
            RecordingCount().ShouldBe(0);
        }

        [Fact]
        public async Task A_plan_with_no_rule_is_refused()
        {
            WorkoutPlan plan = _seed.Plan(_member, currentWeek: 1);

            ShouldFail(await _service.StartAsync(plan.Id, 1, _member.Id, false), TranslationKeys.Workout_plan_has_no_rule_assigned);
            RecordingCount().ShouldBe(0);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(int.MaxValue)]
        public async Task A_week_the_rule_does_not_have_is_refused(int week)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1), currentWeek: 1);

            ShouldFail(await _service.StartAsync(plan.Id, week, _member.Id, false), TranslationKeys.Selected_week_does_not_exist_in_the_rule);
            RecordingCount().ShouldBe(0);
            _database.Read(db => db.WorkoutPlans.Single()).CurrentWeek.ShouldBe(1);
        }

        [Theory]
        // Still open and inside the three hours.
        [InlineData(0, false, true)]
        [InlineData(179, false, true)]
        // Lapsed, or stopped.
        [InlineData(181, false, false)]
        [InlineData(10, true, false)]
        public async Task A_plan_that_is_running_is_not_started_again(int minutesAgo, bool isStopped, bool isRefused)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1), currentWeek: 1);
            DateTime startedOn = DateTime.UtcNow.AddMinutes(-minutesAgo);
            _seed.Recording(plan, startedOn, 1, isStopped ? startedOn.AddMinutes(5) : null);

            WorkoutPlanRecordingResult result = await _service.StartAsync(plan.Id, 2, _member.Id, false);

            if (isRefused)
                ShouldFail(result, TranslationKeys.A_recording_is_already_running_for_this_plan);
            else
                result.IsSucceed.ShouldBeTrue();

            RecordingCount().ShouldBe(isRefused ? 1 : 2);
        }

        [Fact]
        public async Task Another_plan_running_does_not_stop_this_one()
        {
            WorkoutPlanRule rule = _seed.Rule("Rule", 1);
            WorkoutPlan plan = _seed.Plan(_member, rule);
            _seed.Recording(_seed.Plan(_member, rule), DateTime.UtcNow, 1);

            (await _service.StartAsync(plan.Id, 1, _member.Id, false)).IsSucceed.ShouldBeTrue();
        }

        // The limit and the gap are the dialog's to keep to; Start itself takes any week of the rule.
        [Fact]
        public async Task A_full_week_can_still_be_started()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1), currentWeek: 1);
            Done(plan, 1, 1);

            (await _service.StartAsync(plan.Id, 1, _member.Id, false)).IsSucceed.ShouldBeTrue();
        }

        [Fact]
        public async Task A_deactivated_plan_can_still_be_started()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1), 1, x => x.IsInactive = true);

            (await _service.StartAsync(plan.Id, 1, _member.Id, false)).IsSucceed.ShouldBeTrue();
        }


        // ================================================================
        // Stopping
        // ================================================================

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public async Task Stopping_writes_when_and_for_how_long(bool isOwn, bool isAdmin)
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1), 1);
            WorkoutPlanRecording running = _seed.Recording(plan, DateTime.UtcNow.AddMinutes(-50), 1);
            DateTime before = DateTime.UtcNow;

            WorkoutPlanRecordingResult result = await _service.StopAsync(running.Id, isOwn ? _member.Id : _other.Id, isAdmin);

            result.IsSucceed.ShouldBeTrue();
            result.Recording.ShouldNotBeNull().Id.ShouldBe(running.Id);

            WorkoutPlanRecording stopped = _database.Read(db => db.WorkoutPlanRecordings.Single());
            stopped.CompletedOn.ShouldNotBeNull().ShouldBeInRange(before, DateTime.UtcNow);
            stopped.DurationSeconds.ShouldBe((int)(stopped.CompletedOn!.Value - stopped.StartedOn).TotalSeconds);
            stopped.DurationSeconds.ShouldNotBeNull().ShouldBeInRange(50 * 60, 50 * 60 + 60);

            // And nothing else about it.
            stopped.StartedOn.ShouldBe(running.StartedOn, TimeSpan.FromMilliseconds(1));
            stopped.WeekNumber.ShouldBe(1);
            stopped.UserId.ShouldBe(_member.Id);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task A_recording_that_is_not_there_cannot_be_stopped(bool isAdmin)
        {
            ShouldFail(await _service.StopAsync(99, _member.Id, isAdmin), TranslationKeys.Requested_0_not_found, "WorkoutPlanRecording");
        }

        [Fact]
        public async Task Somebody_elses_recording_cannot_be_stopped()
        {
            WorkoutPlanRecording running = _seed.Recording(_seed.Plan(_member), DateTime.UtcNow.AddMinutes(-5));

            ShouldFail(await _service.StopAsync(running.Id, _other.Id, false), TranslationKeys.User_is_not_authorized_to_perform_this_action);
            _database.Read(db => db.WorkoutPlanRecordings.Single()).CompletedOn.ShouldBeNull();
        }

        [Theory]
        // Just before it lapses, and after.
        [InlineData(179, false, true)]
        [InlineData(181, false, false)]
        [InlineData(60 * 24 * 30, false, false)]
        // Stopped already.
        [InlineData(10, true, false)]
        public async Task A_recording_that_is_over_stays_as_it_is(int minutesAgo, bool isStopped, bool isStoppable)
        {
            DateTime startedOn = DateTime.UtcNow.AddMinutes(-minutesAgo);
            DateTime? completedOn = isStopped ? startedOn.AddMinutes(5) : null;
            WorkoutPlanRecording recording = _seed.Recording(_seed.Plan(_member), startedOn, 1, completedOn);

            foreach (bool isAdmin in isStoppable ? new[] { false } : new[] { false, true })
            {
                WorkoutPlanRecordingResult result = await _service.StopAsync(recording.Id, _member.Id, isAdmin);

                if (isStoppable)
                    result.IsSucceed.ShouldBeTrue();
                else
                    ShouldFail(result, TranslationKeys.The_recording_can_no_longer_be_stopped);
            }

            WorkoutPlanRecording after = _database.Read(db => db.WorkoutPlanRecordings.Single());
            if (!isStoppable)
                after.DurationSeconds.ShouldBe(isStopped ? 300 : null);
        }

        [Fact]
        public async Task A_recording_whose_plan_is_gone_can_still_be_stopped()
        {
            WorkoutPlan plan = _seed.Plan(_member);
            WorkoutPlanRecording running = _seed.Recording(plan, DateTime.UtcNow.AddMinutes(-5));
            _database.Change(db => db.WorkoutPlans.Where(x => x.Id == plan.Id).ExecuteDelete());

            (await _service.StopAsync(running.Id, _member.Id, false)).IsSucceed.ShouldBeTrue();
        }

        [Fact]
        public async Task Started_stopped_and_started_again()
        {
            WorkoutPlan plan = _seed.Plan(_member, _seed.Rule("Rule", 1, 1), 1);

            WorkoutPlanRecordingResult first = await _service.StartAsync(plan.Id, 1, _member.Id, false);
            (await _service.StartAsync(plan.Id, 2, _member.Id, false)).IsSucceed.ShouldBeFalse();
            (await _service.StopAsync(first.Recording!.Id, _member.Id, false)).IsSucceed.ShouldBeTrue();
            (await _service.StopAsync(first.Recording!.Id, _member.Id, false)).IsSucceed.ShouldBeFalse();
            (await _service.StartAsync(plan.Id, 2, _member.Id, false)).IsSucceed.ShouldBeTrue();

            using ApiDbContext db = _database.Open();
            db.WorkoutPlanRecordings.Count().ShouldBe(2);
            db.WorkoutPlans.Single().CurrentWeek.ShouldBe(2);
        }
    }
}
