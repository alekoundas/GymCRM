using Core.Dtos.WorkoutPlanRecording;
using Core.Enums;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Services
{
    public class WorkoutPlanRecordingService : IWorkoutPlanRecordingService
    {
        // An open recording older than this can never be stopped and stays without a duration.
        public const int LapseAfterHours = 3;

        // A gap longer than this resets the per-week count and offers a free week choice.
        public const int AwayGapDays = 12;

        private readonly IDataService _dataService;

        public WorkoutPlanRecordingService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public bool IsRunning(WorkoutPlanRecording recording, DateTime nowUtc)
        {
            return recording.CompletedOn == null
                && (nowUtc - recording.StartedOn).TotalHours <= LapseAfterHours;
        }

        public bool IsIncomplete(WorkoutPlanRecording recording, DateTime nowUtc)
        {
            return recording.CompletedOn == null
                && (nowUtc - recording.StartedOn).TotalHours > LapseAfterHours;
        }

        public WorkoutPlanRecordingDto ToDto(WorkoutPlanRecording recording, DateTime nowUtc)
        {
            bool isRunning = IsRunning(recording, nowUtc);

            return new WorkoutPlanRecordingDto
            {
                Id = recording.Id,
                StartedOn = recording.StartedOn,
                CompletedOn = recording.CompletedOn,
                DurationSeconds = recording.DurationSeconds,
                WeekNumber = recording.WeekNumber,
                WorkoutPlanId = recording.WorkoutPlanId,
                WorkoutPlanTitle = recording.WorkoutPlanTitle,
                UserId = recording.UserId.ToString(),
                IsRunning = isRunning,
                IsIncomplete = IsIncomplete(recording, nowUtc),
                // Computed here rather than on the client: the API DateTime converter calls
                // ToUniversalTime() on the Kind=Unspecified values SQLite returns, so an
                // integer is the only thing that survives the round trip unshifted.
                ElapsedSeconds = isRunning ? (int)(nowUtc - recording.StartedOn).TotalSeconds : null,
                CreatedOn = recording.CreatedOn
            };
        }

        // Counts consecutive recordings sitting on currentWeek, walking backwards
        // from the newest. Stops at the first recording on a different week, and at the first gap
        // longer than AwayGapDays - measured from now to the newest, then between each pair.
        public static int CountConsecutiveOnWeek(IEnumerable<WorkoutPlanRecording> newestFirst, int currentWeek, DateTime nowUtc)
        {
            int count = 0;
            DateTime cursor = nowUtc;

            foreach (WorkoutPlanRecording recording in newestFirst)
            {
                if ((cursor - recording.StartedOn).TotalDays > AwayGapDays)
                    break;

                if (recording.WeekNumber != currentWeek)
                    break;

                count++;
                cursor = recording.StartedOn;
            }

            return count;
        }

        // Where "advance" lands. Wraps past the last week, and lands on 1 when orphaned.
        public static int ResolveNextWeek(int? currentWeek, int weekCount)
        {
            if (weekCount <= 0)
                return 1;

            // Orphaned or unset: the member restarts the cycle rather than landing on an
            // arbitrary modulo of a week that no longer exists.
            if (currentWeek == null || currentWeek < 1 || currentWeek > weekCount)
                return 1;

            return currentWeek.Value >= weekCount ? 1 : currentWeek.Value + 1;
        }

        public async Task<WorkoutPlanStartContextDto?> GetStartContextAsync(int workoutPlanId)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            WorkoutPlan? plan = await context.WorkoutPlans
                .Include(x => x.WorkoutPlanRule!).ThenInclude(x => x.Weeks)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == workoutPlanId);

            if (plan == null)
                return null;

            DateTime nowUtc = DateTime.UtcNow;

            WorkoutPlanStartContextDto startContext = new WorkoutPlanStartContextDto
            {
                WorkoutPlanId = plan.Id,
                HasRule = plan.WorkoutPlanRuleId != null,
                RuleName = plan.WorkoutPlanRule?.Name ?? string.Empty,
                CurrentWeek = plan.CurrentWeek
            };

            if (plan.WorkoutPlanRule == null)
            {
                startContext.Scenario = WorkoutPlanStartScenarioEnum.NoRule;
                return startContext;
            }

            List<WorkoutPlanRuleWeek> weeks = plan.WorkoutPlanRule.Weeks
                .OrderBy(x => x.WeekNumber)
                .ToList();

            startContext.WeekCount = weeks.Count;
            startContext.AvailableWeeks = weeks.Select(x => x.WeekNumber).ToList();

            List<WorkoutPlanRecording> recordings = await context.WorkoutPlanRecordings
                .AsNoTracking()
                .Where(x => x.WorkoutPlanId == workoutPlanId)
                .OrderByDescending(x => x.StartedOn)
                .ToListAsync();

            WorkoutPlanRecording? running = recordings.FirstOrDefault(x => IsRunning(x, nowUtc));
            if (running != null)
            {
                startContext.Scenario = WorkoutPlanStartScenarioEnum.Running;
                startContext.RunningRecording = ToDto(running, nowUtc);
                startContext.NextWeek = ResolveNextWeek(plan.CurrentWeek, weeks.Count);
                return startContext;
            }

            WorkoutPlanRuleWeek? currentWeekRow = weeks.FirstOrDefault(x => x.WeekNumber == plan.CurrentWeek);
            startContext.CurrentWeekMessage = currentWeekRow?.Message ?? string.Empty;
            startContext.MaxRecordings = currentWeekRow?.MaxRecordings ?? 0;
            startContext.NextWeek = ResolveNextWeek(plan.CurrentWeek, weeks.Count);

            WorkoutPlanRecording? last = recordings.FirstOrDefault();
            if (last != null)
            {
                startContext.LastRecordingOn = last.StartedOn;
                startContext.DaysSinceLastRecording = (int)(nowUtc - last.StartedOn).TotalDays;
            }

            if (last == null)
            {
                startContext.Scenario = WorkoutPlanStartScenarioEnum.FirstEver;
                startContext.NextWeek = 1;
                return startContext;
            }

            if ((nowUtc - last.StartedOn).TotalDays > AwayGapDays)
            {
                startContext.Scenario = WorkoutPlanStartScenarioEnum.AwayTooLong;
                return startContext;
            }

            if (currentWeekRow == null)
            {
                // The rule shrank or was swapped under the plan. Existing recordings stay put,
                // the member counts as maxed out, and advancing restarts at week 1.
                startContext.Scenario = WorkoutPlanStartScenarioEnum.Orphaned;
                startContext.NextWeek = 1;
                return startContext;
            }

            startContext.RecordingsOnCurrentWeek = CountConsecutiveOnWeek(recordings, plan.CurrentWeek!.Value, nowUtc);
            startContext.RemainingOnCurrentWeek = Math.Max(0, currentWeekRow.MaxRecordings - startContext.RecordingsOnCurrentWeek);
            startContext.Scenario = startContext.RemainingOnCurrentWeek > 0
                ? WorkoutPlanStartScenarioEnum.UnderMax
                : WorkoutPlanStartScenarioEnum.AtMax;

            return startContext;
        }

        public async Task<WorkoutPlanRecordingResult> StartAsync(int workoutPlanId, int weekNumber, Guid callerId)
        {
            using ApiDbContext context = _dataService.GetDbContext();
            DateTime nowUtc = DateTime.UtcNow;

            WorkoutPlan? plan = await context.WorkoutPlans
                .Include(x => x.WorkoutPlanRule!).ThenInclude(x => x.Weeks)
                .FirstOrDefaultAsync(x => x.Id == workoutPlanId);

            if (plan == null)
                return Fail(TranslationKeys.Requested_0_not_found, nameof(WorkoutPlan));

            // Only the member the plan belongs to may record against it - an admin pressing
            // Start would otherwise file a recording under their own name.
            if (plan.UserId != callerId)
                return Fail(TranslationKeys.User_is_not_authorized_to_perform_this_action);

            if (plan.WorkoutPlanRule == null)
                return Fail(TranslationKeys.Workout_plan_has_no_rule_assigned);

            bool weekExists = plan.WorkoutPlanRule.Weeks.Any(x => x.WeekNumber == weekNumber);
            if (!weekExists)
                return Fail(TranslationKeys.Selected_week_does_not_exist_in_the_rule);

            DateTime cutOff = nowUtc.AddHours(-LapseAfterHours);
            bool alreadyRunning = await context.WorkoutPlanRecordings
                .AnyAsync(x => x.WorkoutPlanId == workoutPlanId
                    && x.CompletedOn == null
                    && x.StartedOn > cutOff);

            if (alreadyRunning)
                return Fail(TranslationKeys.A_recording_is_already_running_for_this_plan);

            WorkoutPlanRecording recording = new WorkoutPlanRecording
            {
                StartedOn = nowUtc,
                WeekNumber = weekNumber,
                WorkoutPlanId = plan.Id,
                WorkoutPlanTitle = plan.Title,
                UserId = plan.UserId,
                CreatedBy_Id = callerId.ToString(),
                CreatedOn = nowUtc
            };

            plan.CurrentWeek = weekNumber;

            context.WorkoutPlanRecordings.Add(recording);
            await context.SaveChangesAsync();

            return new WorkoutPlanRecordingResult { IsSucceed = true, Recording = recording };
        }

        public async Task<WorkoutPlanRecordingResult> StopAsync(int recordingId, Guid callerId)
        {
            using ApiDbContext context = _dataService.GetDbContext();
            DateTime nowUtc = DateTime.UtcNow;

            WorkoutPlanRecording? recording = await context.WorkoutPlanRecordings
                .FirstOrDefaultAsync(x => x.Id == recordingId);

            if (recording == null)
                return Fail(TranslationKeys.Requested_0_not_found, nameof(WorkoutPlanRecording));

            if (recording.UserId != callerId)
                return Fail(TranslationKeys.User_is_not_authorized_to_perform_this_action);

            // Past the cut-off the recording is frozen: it keeps its start, never gets a
            // duration, and still counts towards the week's limit.
            if (recording.CompletedOn != null || IsIncomplete(recording, nowUtc))
                return Fail(TranslationKeys.The_recording_can_no_longer_be_stopped);

            recording.CompletedOn = nowUtc;
            recording.DurationSeconds = (int)(nowUtc - recording.StartedOn).TotalSeconds;

            await context.SaveChangesAsync();

            return new WorkoutPlanRecordingResult { IsSucceed = true, Recording = recording };
        }

        private static WorkoutPlanRecordingResult Fail(string errorKey, string argument = "")
        {
            return new WorkoutPlanRecordingResult
            {
                IsSucceed = false,
                ErrorKey = errorKey,
                ErrorArgument = argument
            };
        }
    }
}
