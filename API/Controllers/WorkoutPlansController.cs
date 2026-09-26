using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.WorkoutPlan;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class WorkoutPlansController : GenericController<WorkoutPlan, WorkoutPlanDto, WorkoutPlanAddDto>
    {
        // Sort/filter names that are computed from the recordings, not columns on WorkoutPlan.
        private const string AdminViewClaim = "WorkoutPlansAdmin_View";

        private const string LastRecordingOnField = "lastRecordingOn";
        private const string IsRunningField = "isRunning";
        // A filter only, never a column, so it cannot clash with sorting on IsInactive.
        private const string ActiveOnlyField = "activeOnly";

        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer _localizer;
        private readonly IWorkoutPlanRecordingService _recordingService;

        public WorkoutPlansController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer,
            IEmailService emailService,
            IWorkoutPlanRecordingService recordingService) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _emailService = emailService;
            _localizer = localizer;
            _recordingService = recordingService;
        }


        public override async Task<ActionResult<ApiResponse<WorkoutPlanDto>>> Get(string? id)
        {
            WorkoutPlan? entity = await _dataService.GetGenericRepository<WorkoutPlan>()
                .Include(x => x.User)
                .Include(x => x.Exercises)
                .Include(x => x.WorkoutPlanRule!).ThenInclude<WorkoutPlanRule, ICollection<WorkoutPlanRuleWeek>>(x => x.Weeks)
                .FilterByColumnEquals("Id", id).FirstOrDefaultAsync();

            WorkoutPlanDto entityDto = _mapper.Map<WorkoutPlanDto>(entity);
            if (entityDto == null || entity == null)
            {
                string className = typeof(WorkoutPlan).Name;
                return new ApiResponse<WorkoutPlanDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, className]);
            }

            await FillRecordingFieldsAsync(new List<WorkoutPlan> { entity }, new List<WorkoutPlanDto> { entityDto });

            return new ApiResponse<WorkoutPlanDto>().SetSuccessResponse(entityDto);
        }

        // PUT: api/WorkoutPlans/5
        // Whether a plan is active changes through Deactivate and Activate only. The form
        // sends the whole plan back, so without this a save would quietly re-activate it.
        public override async Task<ActionResult<ApiResponse<WorkoutPlan>>> Put(string? id, [FromBody] WorkoutPlanDto entityDto)
        {
            if (int.TryParse(id, out int planId))
            {
                using ApiDbContext context = _dataService.GetDbContext();
                bool? isInactive = await context.WorkoutPlans
                    .Where(x => x.Id == planId)
                    .Select(x => (bool?)x.IsInactive)
                    .FirstOrDefaultAsync();

                if (isInactive != null)
                    entityDto.IsInactive = isInactive.Value;
            }

            return await base.Put(id, entityDto);
        }

        // POST: api/WorkoutPlans/5/Deactivate
        // Hides the plan from the member's list. A member may do it to their own plan and
        // cannot undo it; the admin can, with Activate.
        [HttpPost("{id}/Deactivate")]
        public async Task<ActionResult<ApiResponse<bool>>> Deactivate(int id) => await SetInactiveAsync(id, true);

        // POST: api/WorkoutPlans/5/Activate
        [HttpPost("{id}/Activate")]
        public async Task<ActionResult<ApiResponse<bool>>> Activate(int id) => await SetInactiveAsync(id, false);

        private async Task<ActionResult<ApiResponse<bool>>> SetInactiveAsync(int id, bool isInactive)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            WorkoutPlan? plan = await context.WorkoutPlans.FirstOrDefaultAsync(x => x.Id == id);
            if (plan == null)
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(WorkoutPlan)]));

            bool isAdmin = User.HasClaim("Permission", "WorkoutPlansAdmin_Edit");
            bool isOwnPlan = plan.UserId == GetCallerId();

            if (!isAdmin && !(isInactive && isOwnPlan))
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));

            plan.IsInactive = isInactive;
            await context.SaveChangesAsync();

            return new ApiResponse<bool>().SetSuccessResponse(true,
                _localizer[isInactive ? TranslationKeys.Workout_plan_deactivated : TranslationKeys.Workout_plan_activated]);
        }

        // The week and the rule have to agree, and only the server can be sure of it.
        // The dialog fetches the rule's weeks to build its dropdown, so there is a
        // moment where the form still holds the previous rule's week - saving inside
        // that moment used to persist a week the new rule never had, and the plan
        // then read as orphaned.
        protected override bool CustomValidatePUT(WorkoutPlanDto entity, out string[] errors)
        {
            return HasWeekOutsideRule(entity.WorkoutPlanRuleId, entity.CurrentWeek, out errors);
        }

        protected override bool CustomValidatePOST(WorkoutPlanAddDto entity, out string[] errors)
        {
            return HasWeekOutsideRule(entity.WorkoutPlanRuleId, entity.CurrentWeek, out errors);
        }

        private bool HasWeekOutsideRule(int? workoutPlanRuleId, int? currentWeek, out string[] errors)
        {
            errors = Array.Empty<string>();

            if (workoutPlanRuleId == null)
                return false;

            using ApiDbContext context = _dataService.GetDbContext();

            bool weekExists = currentWeek != null
                && context.WorkoutPlanRuleWeeks.Any(x =>
                    x.WorkoutPlanRuleId == workoutPlanRuleId.Value
                    && x.WeekNumber == currentWeek.Value);

            if (weekExists)
                return false;

            errors = [_localizer[TranslationKeys.Selected_week_does_not_exist_in_the_rule]];
            return true;
        }

        protected override void DataTableQueryUpdate(IGenericRepository<WorkoutPlan> query, DataTableDto<WorkoutPlanDto> dataTable)
        {
            query = query.Include(x => x.User).ThenInclude<User, UserStatus>(x => x.UserStatus!);
            query = query.Include(x => x.WorkoutPlanRule!);

            Guid? scopeUserId = GetScopeToCallerId(AdminViewClaim);
            if (scopeUserId != null)
            {
                query = query.Where(x => x.UserId == scopeUserId.Value);

                // A member does not see the plans they have deactivated.
                query = query.Where(x => !x.IsInactive);
            }

            // The member page asks for active plans only. Staff open it too, and without
            // this they would still see plans they had just deactivated there.
            DataTableFilterDto? activeOnlyFilter = dataTable.Filters
                .FirstOrDefault(x => string.Equals(x.FieldName, ActiveOnlyField, StringComparison.OrdinalIgnoreCase));

            if (activeOnlyFilter?.Value != null && bool.TryParse(activeOnlyFilter.Value, out bool activeOnly) && activeOnly)
                query = query.Where(x => !x.IsInactive);

            // "Is a recording live for this plan" is an EXISTS over the children, so the
            // reflection-based helpers in GetDataTable cannot express it. Same for the
            // "last used" sort, which is a MAX. Both are claimed below so the generic
            // loops skip them, and EF translates these straight to SQL - no denormalised
            // columns to keep in sync.
            DateTime cutOff = DateTime.UtcNow.AddHours(-WorkoutPlanRecordingService.LapseAfterHours);

            DataTableFilterDto? runningFilter = dataTable.Filters
                .FirstOrDefault(x => string.Equals(x.FieldName, IsRunningField, StringComparison.OrdinalIgnoreCase));

            if (runningFilter?.Value != null && bool.TryParse(runningFilter.Value, out bool wantsRunning))
            {
                query = wantsRunning
                    ? query.Where(x => x.Recordings.Any(r => r.CompletedOn == null && r.StartedOn > cutOff))
                    : query.Where(x => !x.Recordings.Any(r => r.CompletedOn == null && r.StartedOn > cutOff));
            }

            DataTableSortDto? lastUsedSort = dataTable.Sorts
                .FirstOrDefault(x => string.Equals(x.FieldName, LastRecordingOnField, StringComparison.OrdinalIgnoreCase));

            if (lastUsedSort != null)
            {
                if (lastUsedSort.Order > 0)
                    query = query.OrderBy(x => x.Recordings.Max(r => (DateTime?)r.StartedOn));
                else
                    query = query.OrderByDescending(x => x.Recordings.Max(r => (DateTime?)r.StartedOn));
            }
        }

        protected override HashSet<string> GetHandledDataTableFields()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                LastRecordingOnField,
                IsRunningField,
                ActiveOnlyField
            };
        }

        protected override async Task DataTableResultUpdate(List<WorkoutPlan> entities, List<WorkoutPlanDto> entityDtos)
        {
            await FillRecordingFieldsAsync(entities, entityDtos);
        }

        // Fills the plan fields that come from the rule and the recordings - the week message,
        // whether a recording is live, and when the plan was last used.
        private async Task FillRecordingFieldsAsync(List<WorkoutPlan> entities, List<WorkoutPlanDto> entityDtos)
        {
            if (entities.Count == 0)
                return;

            using ApiDbContext context = _dataService.GetDbContext();

            DateTime nowUtc = DateTime.UtcNow;
            List<int> planIds = entities.Select(x => x.Id).ToList();

            // One query for the whole page rather than one per row.
            List<WorkoutPlanRecording> recordings = await context.WorkoutPlanRecordings
                .AsNoTracking()
                .Where(x => x.WorkoutPlanId != null && planIds.Contains(x.WorkoutPlanId.Value))
                .ToListAsync();

            List<int> ruleIds = entities
                .Where(x => x.WorkoutPlanRuleId != null)
                .Select(x => x.WorkoutPlanRuleId!.Value)
                .Distinct()
                .ToList();

            List<WorkoutPlanRuleWeek> weeks = ruleIds.Count == 0
                ? new List<WorkoutPlanRuleWeek>()
                : await context.WorkoutPlanRuleWeeks
                    .AsNoTracking()
                    .Where(x => ruleIds.Contains(x.WorkoutPlanRuleId))
                    .ToListAsync();

            for (int i = 0; i < entities.Count && i < entityDtos.Count; i++)
            {
                WorkoutPlan entity = entities[i];
                WorkoutPlanDto entityDto = entityDtos[i];

                List<WorkoutPlanRecording> planRecordings = recordings
                    .Where(x => x.WorkoutPlanId == entity.Id)
                    .OrderByDescending(x => x.StartedOn)
                    .ToList();

                WorkoutPlanRecording? running = planRecordings.FirstOrDefault(x => _recordingService.IsRunning(x, nowUtc));

                entityDto.IsRunning = running != null;
                entityDto.ElapsedSeconds = running != null
                    ? (int)(nowUtc - running.StartedOn).TotalSeconds
                    : null;
                // Only the latest one matters. Flagging any lapsed recording ever
                // would leave the warning up for good, even after a clean session.
                WorkoutPlanRecording? latest = planRecordings.FirstOrDefault();

                entityDto.HasIncompleteRecording =
                    latest != null && _recordingService.IsIncomplete(latest, nowUtc);
                entityDto.LastRecordingOn = latest?.StartedOn;
                entityDto.LastRecordingDurationSeconds = latest?.DurationSeconds;
                entityDto.RecordingCount = planRecordings.Count;

                if (entity.WorkoutPlanRuleId != null)
                {
                    List<WorkoutPlanRuleWeek> ruleWeeks = weeks
                        .Where(x => x.WorkoutPlanRuleId == entity.WorkoutPlanRuleId.Value)
                        .ToList();

                    entityDto.WeekCount = ruleWeeks.Count;
                    entityDto.CurrentWeekMessage = ruleWeeks
                        .FirstOrDefault(x => x.WeekNumber == entity.CurrentWeek)?.Message ?? string.Empty;
                }
            }
        }
    }
}
