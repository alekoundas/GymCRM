using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos.DataTable;
using Core.Dtos.ExerciseHistory;
using Core.Models;
using Core.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
