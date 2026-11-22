using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.ExerciseHistory;
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
    public class ExerciseHistoriesController : GenericController<ExerciseHistory, ExerciseHistoryDto, ExerciseHistoryDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer _localizer;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public ExerciseHistoriesController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer,
            IEmailService emailService) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _emailService = emailService;
            _localizer = localizer;
        }

        //
        //      The grid behind the admin's exercise history page. A member only ever
        //      reads the history of their own plans' exercises - this grid had no
        //      scoping at all, so anyone could read everybody's.
        //

        private const string UserIdField = "userId";
        private const string WorkoutPlanTitleField = "workoutPlanTitle";

        protected override void DataTableQueryUpdate(IGenericRepository<ExerciseHistory> query, DataTableDto<ExerciseHistoryDto> dataTable)
        {
            query = query.Include(x => x.Exercise.WorkoutPlan).ThenInclude<WorkoutPlan, User>(x => x.User);

            Guid? scopeUserId = GetScopeToCallerId("WorkoutPlansAdmin_View");
            if (scopeUserId != null)
                query = query.Where(x => x.Exercise.WorkoutPlan.UserId == scopeUserId.Value);

            List<Guid> userIds = dataTable.Filters
                .Where(x => string.Equals(x.FieldName, UserIdField, StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.Values)
                .Select(x => Guid.TryParse(x, out Guid id) ? id : Guid.Empty)
                .Where(x => x != Guid.Empty)
                .ToList();
            if (userIds.Count > 0)
                query = query.Where(x => userIds.Contains(x.Exercise.WorkoutPlan.UserId));

            string? title = dataTable.Filters
                .FirstOrDefault(x => string.Equals(x.FieldName, WorkoutPlanTitleField, StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrWhiteSpace(title))
            {
                string normalized = TextNormalizer.Normalize(title);
                query = query.Where(x => TextNormalizer.Normalize(x.Exercise.WorkoutPlan.Title).Contains(normalized));
            }
        }

        protected override HashSet<string> GetHandledDataTableFields() =>
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { UserIdField, WorkoutPlanTitleField };

        protected override Task DataTableResultUpdate(List<ExerciseHistory> entities, List<ExerciseHistoryDto> entityDtos)
        {
            for (int i = 0; i < entities.Count && i < entityDtos.Count; i++)
            {
                WorkoutPlan? plan = entities[i].Exercise?.WorkoutPlan;
                if (plan == null)
                    continue;

                entityDtos[i].WorkoutPlanId = plan.Id;
                entityDtos[i].WorkoutPlanTitle = plan.Title;
                entityDtos[i].UserId = plan.UserId.ToString();
                entityDtos[i].User = _mapper.Map<Core.Dtos.User.UserDto>(plan.User);
            }

            return Task.CompletedTask;
        }


        // GET: api/ExerciseHistories/5 - the plan's member or a plans admin.
        public override async Task<ActionResult<ApiResponse<ExerciseHistoryDto>>> Get(string? id)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<ExerciseHistoryDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            int.TryParse(id, out int historyId);
            using ApiDbContext context = _dataService.GetDbContext();

            ExerciseHistory? entity = await context.ExerciseHistories
                .Include(x => x.Exercise).ThenInclude(x => x.WorkoutPlan)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == historyId);

            if (entity == null
                || (!User.HasClaim("Permission", "WorkoutPlansAdmin_View") && entity.Exercise?.WorkoutPlan?.UserId != GetCallerId()))
                return new ApiResponse<ExerciseHistoryDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(ExerciseHistory)]);

            return new ApiResponse<ExerciseHistoryDto>().SetSuccessResponse(_mapper.Map<ExerciseHistoryDto>(entity));
        }

        // The history is written by Exercises Put whenever an exercise changes, and the
        // app only ever reads it. Members hold WorkoutPlans_Edit for their weights, which
        // on its own let any of them rewrite anybody's history rows here.
        public override async Task<ActionResult<ApiResponse<ExerciseHistory>>> Put(string? id, [FromBody] ExerciseHistoryDto entityDto)
        {
            if (!User.HasClaim("Permission", "WorkoutPlansAdmin_Edit"))
                return new ApiResponse<ExerciseHistory>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            return await base.Put(id, entityDto);
        }

        protected override bool IsUserAuthorized(string action)
        {
            string controllerName = "WorkoutPlans";
            string claimName = controllerName + "_" + action;
            bool hasClaim = User.HasClaim("Permission", claimName);
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return hasClaim;
        }
    }
}
