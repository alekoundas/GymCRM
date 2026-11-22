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

            Guid? callerId = GetCallerId();
            if (callerId == null)
                return new ApiResponse<WorkoutPlanStartContextDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_loged_in]);

            WorkoutPlanStartContextDto? startContext = await _recordingService.GetStartContextAsync(
                workoutPlanId, callerId.Value, IsRecordingsAdmin());

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

            WorkoutPlanRecordingResult result = await _recordingService.StartAsync(
                request.WorkoutPlanId, request.WeekNumber, callerId.Value, IsRecordingsAdmin());

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

            WorkoutPlanRecordingResult result = await _recordingService.StopAsync(id, callerId.Value, IsRecordingsAdmin());

            if (!result.IsSucceed || result.Recording == null)
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(_localizer[result.ErrorKey, result.ErrorArgument]);

            return new ApiResponse<WorkoutPlanRecordingDto>().SetSuccessResponse(_recordingService.ToDto(result.Recording, DateTime.UtcNow));
        }

        // GET: api/WorkoutPlanRecordings/5 - the member's own, or anyone's for a recordings admin.
        public override async Task<ActionResult<ApiResponse<WorkoutPlanRecordingDto>>> Get(string? id)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            WorkoutPlanRecording? entity = await _dataService.GetGenericRepository<WorkoutPlanRecording>()
                .FilterByColumnEquals("Id", id).FirstOrDefaultAsync();

            if (entity == null || (entity.UserId != GetCallerId() && !IsRecordingsAdmin()))
                return new ApiResponse<WorkoutPlanRecordingDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(WorkoutPlanRecording)]);

            return new ApiResponse<WorkoutPlanRecordingDto>().SetSuccessResponse(_recordingService.ToDto(entity, DateTime.UtcNow));
        }

        // Recordings are only ever created by Start, which checks the plan, the week, the
        // caller and that nothing is running. The generic create would skip all of that,
        // and members hold the Add permission so that they can press Start.
        public override Task<ActionResult<ApiResponse<List<WorkoutPlanRecording>>>> Post([FromBody] List<WorkoutPlanRecordingAddDto> entityDtos)
        {
            ActionResult<ApiResponse<List<WorkoutPlanRecording>>> refused =
                new ApiResponse<List<WorkoutPlanRecording>>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            return Task.FromResult(refused);
        }

        // Whoever may see every member's recordings may also read a member's start
        // context and start and stop their workout for them. Start and Stop still
        // need the Add permission as well.
        private bool IsRecordingsAdmin()
        {
            return User.HasClaim("Permission", AdminViewClaim);
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
