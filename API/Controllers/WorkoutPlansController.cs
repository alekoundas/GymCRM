using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos.DataTable;
using Core.Dtos.WorkoutPlan;
using Core.Enums;
using Core.Models;
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
        protected override void DataTableQueryUpdate(IGenericRepository<WorkoutPlan> query, DataTableDto<WorkoutPlanDto> dataTable)
        {
            query = query.Include(x => x.User);
        }
    }
}
