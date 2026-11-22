using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Exercise;
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
    public class ExercisesController : GenericController<Exercise, ExerciseDto, ExerciseAddDto>
    {
        private const string AdminViewClaim = "WorkoutPlansAdmin_View";
        private const string AdminEditClaim = "WorkoutPlansAdmin_Edit";

        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer _localizer;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public ExercisesController(
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

        // PUT: api/controller/5
        [HttpPut("{id}")]
        public override async Task<ActionResult<ApiResponse<Exercise>>> Put(string? id, [FromBody] ExerciseDto entityDto)
        {
            if (!IsUserAuthorized("Edit"))
                return new ApiResponse<Exercise>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            string className = typeof(Exercise).Name;
            Exercise entity = _mapper.Map<Exercise>(entityDto);


            Exercise? existingEntity = await _dataService.GetGenericRepository<Exercise>().FilterByColumnEquals("Id", id).FirstOrDefaultAsync();
            if (existingEntity == null)
                return new ApiResponse<Exercise>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, className]);

            // Members hold WorkoutPlans_Edit to change their weights. It gives them their own
            // plans' exercises only; somebody else's answers like a missing one.
            bool isPlansAdmin = User.HasClaim("Permission", AdminEditClaim);
            if (!isPlansAdmin && await GetPlanOwnerIdAsync(existingEntity.WorkoutPlanId) != GetCallerId())
                return new ApiResponse<Exercise>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, className]);

            // And the weight is all a member changes - the trainer writes the rest, and the
            // form already locks it. Built from the stored row rather than the request, so
            // a hand-made request cannot rename the exercise, move it into another plan, or
            // send a different id in the body from the one that was checked above.
            if (!isPlansAdmin)
                entity = WithWeight(existingEntity, entityDto.Weight);

            if (
                existingEntity.Name != entity.Name ||
                existingEntity.Description != entity.Description ||
                existingEntity.Sets != entity.Sets ||
                existingEntity.Reps != entity.Reps ||
                existingEntity.Weight != entity.Weight)
            {
                await _dataService.ExerciseHistories.AddAsync(
                    new ExerciseHistory
                    {
                        Name = entity.Name,
                        Description=entity.Description,
                        Sets=entity.Sets,
                        Reps=entity.Reps,
                        Weight=entity.Weight,
                        GroupNumber=entity.GroupNumber,
                        GroupExerciseOrderNumber=entity.GroupExerciseOrderNumber,
                        ExerciseId=entity.Id,
                    });

            }

            _dataService.Update(entity);
            return new ApiResponse<Exercise>().SetSuccessResponse(entity, _localizer[TranslationKeys._0_updated_successfully, className]);
        }

        // GET: api/Exercises/5 - same rule as Put: the plan's member or a plans admin.
        public override async Task<ActionResult<ApiResponse<ExerciseDto>>> Get(string? id)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<ExerciseDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            Exercise? entity = await _dataService.GetGenericRepository<Exercise>().FilterByColumnEquals("Id", id).FirstOrDefaultAsync();

            if (entity == null
                || (!User.HasClaim("Permission", AdminViewClaim) && await GetPlanOwnerIdAsync(entity.WorkoutPlanId) != GetCallerId()))
                return new ApiResponse<ExerciseDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, typeof(Exercise).Name]);

            return new ApiResponse<ExerciseDto>().SetSuccessResponse(_mapper.Map<ExerciseDto>(entity));
        }

        private async Task<Guid?> GetPlanOwnerIdAsync(int workoutPlanId)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            return await context.WorkoutPlans
                .Where(x => x.Id == workoutPlanId)
                .Select(x => (Guid?)x.UserId)
                .FirstOrDefaultAsync();
        }

        private static Exercise WithWeight(Exercise stored, string weight)
        {
            return new Exercise
            {
                Id = stored.Id,
                CreatedBy_Id = stored.CreatedBy_Id,
                CreatedBy_FullName = stored.CreatedBy_FullName,
                CreatedOn = stored.CreatedOn,
                Name = stored.Name,
                Description = stored.Description,
                Sets = stored.Sets,
                Reps = stored.Reps,
                Weight = weight ?? string.Empty,
                VideoUrl = stored.VideoUrl,
                GroupNumber = stored.GroupNumber,
                GroupExerciseOrderNumber = stored.GroupExerciseOrderNumber,
                WorkoutPlanId = stored.WorkoutPlanId
            };
        }


        //
        //      The grid behind the admin's exercise history page. A member only ever
        //      reads the exercises of their own plans.
        //

        private const string UserIdField = "userId";
        private const string WorkoutPlanTitleField = "workoutPlanTitle";

        protected override void DataTableQueryUpdate(IGenericRepository<Exercise> query, DataTableDto<ExerciseDto> dataTable)
        {
            query = query.Include(x => x.WorkoutPlan).ThenInclude<WorkoutPlan, User>(x => x.User);

            Guid? scopeUserId = GetScopeToCallerId("WorkoutPlansAdmin_View");
            if (scopeUserId != null)
                query = query.Where(x => x.WorkoutPlan.UserId == scopeUserId.Value);

            List<Guid> userIds = dataTable.Filters
                .Where(x => string.Equals(x.FieldName, UserIdField, StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.Values)
                .Select(x => Guid.TryParse(x, out Guid id) ? id : Guid.Empty)
                .Where(x => x != Guid.Empty)
                .ToList();
            if (userIds.Count > 0)
                query = query.Where(x => userIds.Contains(x.WorkoutPlan.UserId));

            string? title = dataTable.Filters
                .FirstOrDefault(x => string.Equals(x.FieldName, WorkoutPlanTitleField, StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrWhiteSpace(title))
            {
                string normalized = TextNormalizer.Normalize(title);
                query = query.Where(x => TextNormalizer.Normalize(x.WorkoutPlan.Title).Contains(normalized));
            }
        }

        protected override HashSet<string> GetHandledDataTableFields() =>
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { UserIdField, WorkoutPlanTitleField };

        protected override Task DataTableResultUpdate(List<Exercise> entities, List<ExerciseDto> entityDtos)
        {
            for (int i = 0; i < entities.Count && i < entityDtos.Count; i++)
            {
                WorkoutPlan? plan = entities[i].WorkoutPlan;
                if (plan == null)
                    continue;

                entityDtos[i].WorkoutPlanTitle = plan.Title;
                entityDtos[i].IsWorkoutPlanInactive = plan.IsInactive;
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
