using AutoMapper;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.Exercise;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ExercisesController : GenericController<Exercise, ExerciseDto, ExerciseAddDto>
    {
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
