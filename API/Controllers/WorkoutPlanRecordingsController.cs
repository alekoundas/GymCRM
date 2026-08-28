using AutoMapper;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.WorkoutPlanRecording;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class WorkoutPlanRecordingsController : GenericController<WorkoutPlanRecording, WorkoutPlanRecordingDto, WorkoutPlanRecordingAddDto>
    {
        private const string AdminViewClaim = "WorkoutPlanRecordingsAdmin_View";

        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;
        private readonly IWorkoutPlanRecordingService _recordingService;

        public WorkoutPlanRecordingsController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer,
            IWorkoutPlanRecordingService recordingService) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
            _recordingService = recordingService;
        }

        // Everything the Start dialog needs to pick its branch, resolved server-side
        // so the client never re-implements the counting rule.
        [HttpGet("StartContext/{workoutPlanId}")]
        public async Task<ApiResponse<WorkoutPlanStartContextDto>> StartContext(int workoutPlanId)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<WorkoutPlanStartContextDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            WorkoutPlanStartContextDto? startContext = await _recordingService.GetStartContextAsync(workoutPlanId);

            if (startContext == null)
                return new ApiResponse<WorkoutPlanStartContextDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(WorkoutPlan)]);

            return new ApiResponse<WorkoutPlanStartContextDto>().SetSuccessResponse(startContext);
        }

        [HttpPost("Start")]
        public async Task<ApiResponse<WorkoutPlanRecordingDto>> Start([FromBody] WorkoutPlanStartRequestDto request)
        {
            if (!IsUserAuthorized("Add"))
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            Guid? callerId = GetCallerId();
            if (callerId == null)
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_loged_in]);

            WorkoutPlanRecordingResult result = await _recordingService.StartAsync(request.WorkoutPlanId, request.WeekNumber, callerId.Value);

            if (!result.IsSucceed || result.Recording == null)
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(_localizer[result.ErrorKey, result.ErrorArgument]);

            return new ApiResponse<WorkoutPlanRecordingDto>().SetSuccessResponse(_recordingService.ToDto(result.Recording, DateTime.UtcNow));
        }

        [HttpPost("Stop/{id}")]
        public async Task<ApiResponse<WorkoutPlanRecordingDto>> Stop(int id)
        {
            if (!IsUserAuthorized("Add"))
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(
                    _localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            Guid? callerId = GetCallerId();
            if (callerId == null)
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_loged_in]);

            WorkoutPlanRecordingResult result = await _recordingService.StopAsync(id, callerId.Value);

            if (!result.IsSucceed || result.Recording == null)
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(_localizer[result.ErrorKey, result.ErrorArgument]);

            return new ApiResponse<WorkoutPlanRecordingDto>().SetSuccessResponse(_recordingService.ToDto(result.Recording, DateTime.UtcNow));
        }

        protected override void DataTableQueryUpdate(IGenericRepository<WorkoutPlanRecording> query, DataTableDto<WorkoutPlanRecordingDto> dataTable)
        {
            query = query.Include(x => x.User);

            Guid? scopeUserId = GetScopeToCallerId(AdminViewClaim);
            if (scopeUserId != null)
                query = query.Where(x => x.UserId == scopeUserId.Value);
        }

        protected override Task DataTableResultUpdate(List<WorkoutPlanRecording> entities, List<WorkoutPlanRecordingDto> entityDtos)
        {
            DateTime nowUtc = DateTime.UtcNow;

            for (int i = 0; i < entities.Count && i < entityDtos.Count; i++)
            {
                WorkoutPlanRecording entity = entities[i];
                bool isRunning = _recordingService.IsRunning(entity, nowUtc);

                entityDtos[i].IsRunning = isRunning;
                entityDtos[i].IsIncomplete = _recordingService.IsIncomplete(entity, nowUtc);
                entityDtos[i].ElapsedSeconds = isRunning
                    ? (int)(nowUtc - entity.StartedOn).TotalSeconds
                    : null;
            }

            return Task.CompletedTask;
        }

    }
}
