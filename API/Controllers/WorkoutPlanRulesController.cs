using AutoMapper;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Lookup;
using Core.Dtos.WorkoutPlanRule;
using Core.Models;
using Core.System;
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
    public class WorkoutPlanRulesController : GenericController<WorkoutPlanRule, WorkoutPlanRuleDto, WorkoutPlanRuleAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;

        public WorkoutPlanRulesController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
        }

        public override async Task<ActionResult<ApiResponse<WorkoutPlanRuleDto>>> Get(string? id)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<WorkoutPlanRuleDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            using ApiDbContext context = _dataService.GetDbContext();

            int.TryParse(id, out int ruleId);

            WorkoutPlanRule? rule = await context.WorkoutPlanRules
                .Include(x => x.Weeks)
                .Include(x => x.WorkoutPlans)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ruleId);

            if (rule == null)
                return new ApiResponse<WorkoutPlanRuleDto>().SetErrorResponse(
                    _localizer[TranslationKeys.Requested_0_not_found, nameof(WorkoutPlanRule)]);

            WorkoutPlanRuleDto ruleDto = _mapper.Map<WorkoutPlanRuleDto>(rule);
            ruleDto.Weeks = ruleDto.Weeks.OrderBy(x => x.WeekNumber).ToList();
            ruleDto.IsLocked = await IsRuleLockedAsync(context, rule.Id);

            return new ApiResponse<WorkoutPlanRuleDto>().SetSuccessResponse(ruleDto);
        }

        public override async Task<ActionResult<ApiResponse<List<WorkoutPlanRule>>>> Post([FromBody] List<WorkoutPlanRuleAddDto> entityDtos)
        {
            if (!IsUserAuthorized("Add"))
                return new ApiResponse<List<WorkoutPlanRule>>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            using ApiDbContext context = _dataService.GetDbContext();

            List<WorkoutPlanRule> created = new List<WorkoutPlanRule>();

            foreach (WorkoutPlanRuleAddDto entityDto in entityDtos)
            {
                if (await context.WorkoutPlanRules.AnyAsync(x => x.Name == entityDto.Name))
                    return new ApiResponse<List<WorkoutPlanRule>>().SetErrorResponse(_localizer[TranslationKeys._0_already_exists, entityDto.Name]);

                WorkoutPlanRule rule = _mapper.Map<WorkoutPlanRule>(entityDto);
                rule.CreatedOn = DateTime.UtcNow;
                rule.CreatedBy_Id = GetCallerId()?.ToString() ?? string.Empty;

                context.WorkoutPlanRules.Add(rule);
                created.Add(rule);
            }

            await context.SaveChangesAsync();

            return new ApiResponse<List<WorkoutPlanRule>>().SetSuccessResponse(
                created, _localizer[TranslationKeys._0_created_successfully, nameof(WorkoutPlanRule)]);
        }

        public override async Task<ActionResult<ApiResponse<WorkoutPlanRule>>> Put(string? id, [FromBody] WorkoutPlanRuleDto entityDto)
        {
            if (!IsUserAuthorized("Edit"))
                return new ApiResponse<WorkoutPlanRule>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            using ApiDbContext context = _dataService.GetDbContext();

            int.TryParse(id, out int ruleId);

            WorkoutPlanRule? rule = await context.WorkoutPlanRules
                .Include(x => x.Weeks)
                .FirstOrDefaultAsync(x => x.Id == ruleId);

            if (rule == null)
                return new ApiResponse<WorkoutPlanRule>().SetErrorResponse(
                    _localizer[TranslationKeys.Requested_0_not_found, nameof(WorkoutPlanRule)]);

            // A rule may not change while any plan using it has a live recording.
            if (await IsRuleLockedAsync(context, rule.Id))
                return new ApiResponse<WorkoutPlanRule>().SetErrorResponse(_localizer[TranslationKeys.The_rule_is_in_use_by_a_running_recording]);

            if (await context.WorkoutPlanRules.AnyAsync(x => x.Name == entityDto.Name && x.Id != rule.Id))
                return new ApiResponse<WorkoutPlanRule>().SetErrorResponse(_localizer[TranslationKeys._0_already_exists, entityDto.Name]);

            rule.Name = entityDto.Name;

            // Reconcile the week rows rather than replacing them, so untouched weeks keep their ids.
            List<int> keptWeekNumbers = entityDto.Weeks.Select(x => x.WeekNumber).ToList();

            List<WorkoutPlanRuleWeek> removed = rule.Weeks
                .Where(x => !keptWeekNumbers.Contains(x.WeekNumber))
                .ToList();

            context.WorkoutPlanRuleWeeks.RemoveRange(removed);

            foreach (WorkoutPlanRuleWeekDto weekDto in entityDto.Weeks)
            {
                WorkoutPlanRuleWeek? week = rule.Weeks.FirstOrDefault(x => x.WeekNumber == weekDto.WeekNumber);

                if (week == null)
                {
                    rule.Weeks.Add(new WorkoutPlanRuleWeek
                    {
                        WeekNumber = weekDto.WeekNumber,
                        Message = weekDto.Message ?? string.Empty,
                        MaxRecordings = weekDto.MaxRecordings,
                        CreatedOn = DateTime.UtcNow
                    });
                }
                else
                {
                    week.Message = weekDto.Message ?? string.Empty;
                    week.MaxRecordings = weekDto.MaxRecordings;
                }
            }

            await context.SaveChangesAsync();

            return new ApiResponse<WorkoutPlanRule>().SetSuccessResponse(
                rule, _localizer[TranslationKeys._0_updated_successfully, nameof(WorkoutPlanRule)]);
        }

        public override async Task<ActionResult<ApiResponse<WorkoutPlanRule>>> Delete(string? id)
        {
            if (!IsUserAuthorized("Delete"))
                return new ApiResponse<WorkoutPlanRule>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            using ApiDbContext context = _dataService.GetDbContext();

            int.TryParse(id, out int ruleId);

            WorkoutPlanRule? rule = await context.WorkoutPlanRules
                .FirstOrDefaultAsync(x => x.Id == ruleId);

            if (rule == null)
                return new ApiResponse<WorkoutPlanRule>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(WorkoutPlanRule)]);

            if (await IsRuleLockedAsync(context, rule.Id))
                return new ApiResponse<WorkoutPlanRule>().SetErrorResponse(_localizer[TranslationKeys.The_rule_is_in_use_by_a_running_recording]);

            // Plans referencing the rule are left in place - the FK is SetNull, which
            // simply disables Start for them until an admin assigns another rule.
            context.WorkoutPlanRules.Remove(rule);
            await context.SaveChangesAsync();

            return new ApiResponse<WorkoutPlanRule>().SetSuccessResponse(
                rule, _localizer[TranslationKeys._0_deleted_successfully, nameof(WorkoutPlanRule)]);
        }

        // POST: api/WorkoutPlanRules/Lookup
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            IQueryable<WorkoutPlanRule> query = context.WorkoutPlanRules.AsNoTracking();

            if (lookupDto.Filter.Id.Length > 0 && int.TryParse(lookupDto.Filter.Id, out int filterId))
                query = query.Where(x => x.Id == filterId);

            if (lookupDto.Filter.Value.Length > 0)
                query = query.Where(x => TextNormalizer.Normalize(x.Name).Contains(TextNormalizer.Normalize(lookupDto.Filter.Value)));

            lookupDto.TotalRecords = await query.CountAsync();

            lookupDto.Data = await query
                .OrderBy(x => x.Name)
                .Skip(lookupDto.Skip)
                .Take(lookupDto.Take)
                .Select(x => new LookupOptionDto()
                {
                    Id = x.Id.ToString(),
                    Value = x.Name
                })
                .ToListAsync();

            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }

        protected override void DataTableQueryUpdate(IGenericRepository<WorkoutPlanRule> query, DataTableDto<WorkoutPlanRuleDto> dataTable)
        {
            query = query.Include(x => x.Weeks);
        }

        protected override async Task DataTableResultUpdate(List<WorkoutPlanRule> entities, List<WorkoutPlanRuleDto> entityDtos)
        {
            if (entities.Count == 0)
                return;

            using ApiDbContext context = _dataService.GetDbContext();

            List<int> ruleIds = entities.Select(x => x.Id).ToList();
            DateTime cutOff = DateTime.UtcNow.AddHours(-WorkoutPlanRecordingService.LapseAfterHours);

            // One query for the whole page rather than one per row.
            Dictionary<int, int> planCounts = await context.WorkoutPlans
                .Where(x => x.WorkoutPlanRuleId != null && ruleIds.Contains(x.WorkoutPlanRuleId.Value))
                .GroupBy(x => x.WorkoutPlanRuleId!.Value)
                .Select(g => new { RuleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RuleId, x => x.Count);

            List<int> lockedRuleIds = await context.WorkoutPlanRecordings
                .Where(x => x.CompletedOn == null
                    && x.StartedOn > cutOff
                    && x.WorkoutPlan != null
                    && x.WorkoutPlan.WorkoutPlanRuleId != null
                    && ruleIds.Contains(x.WorkoutPlan.WorkoutPlanRuleId.Value))
                .Select(x => x.WorkoutPlan!.WorkoutPlanRuleId!.Value)
                .Distinct()
                .ToListAsync();

            for (int i = 0; i < entities.Count && i < entityDtos.Count; i++)
            {
                int ruleId = entities[i].Id;
                entityDtos[i].WorkoutPlanCount = planCounts.TryGetValue(ruleId, out int count) ? count : 0;
                entityDtos[i].IsLocked = lockedRuleIds.Contains(ruleId);
                entityDtos[i].Weeks = entityDtos[i].Weeks.OrderBy(x => x.WeekNumber).ToList();
            }
        }

        private static async Task<bool> IsRuleLockedAsync(ApiDbContext context, int ruleId)
        {
            DateTime cutOff = DateTime.UtcNow.AddHours(-WorkoutPlanRecordingService.LapseAfterHours);

            return await context.WorkoutPlanRecordings.AnyAsync(x =>
                x.CompletedOn == null
                && x.StartedOn > cutOff
                && x.WorkoutPlan != null
                && x.WorkoutPlan.WorkoutPlanRuleId == ruleId);
        }

    }
}
