using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.WorkoutPlan;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class WorkoutPlansController : GenericController<WorkoutPlan, WorkoutPlanDto, WorkoutPlanAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer _localizer;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public WorkoutPlansController(
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


        public override async Task<ActionResult<ApiResponse<WorkoutPlanDto>>> Get(string? id)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<WorkoutPlanDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            WorkoutPlan? entity = await _dataService.GetGenericRepository<WorkoutPlan>()
                .Include(x=>x.Exercises)
                .FilterByColumnEquals("Id", id).FirstOrDefaultAsync();

            WorkoutPlanDto entityDto = _mapper.Map<WorkoutPlanDto>(entity);
            if (entityDto == null)
            {
                string className = typeof(WorkoutPlan).Name;
                return new ApiResponse<WorkoutPlanDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, className]);
            }

            return new ApiResponse<WorkoutPlanDto>().SetSuccessResponse(entityDto);
        }

        protected override void DataTableQueryUpdate(IGenericRepository<WorkoutPlan> query, DataTableDto<WorkoutPlanDto> dataTable)
        {
            query = query.Include(x => x.User);
        }
    }
}
