using AutoMapper;
using Business.Services;
using Core.Dtos.TrainGroup;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupController : GenericController<TrainGroup, TrainGroupDto, TrainGroupAddDto>
    {
        private readonly IDataService _dataService;
        //private readonly ILogger<TrainGroupController> _logger;

        public TrainGroupController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
        }




        protected override bool CustomValidatePOST(TrainGroupAddDto entityAddDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            foreach (var groupDate in entityAddDto.TrainGroupDates)
            {
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.FIXED_DAY && groupDate.FixedDay == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.DAY_OF_MONTH && groupDate.RecurrenceDayOfMonth == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.DAY_OF_WEEK && groupDate.RecurrenceDayOfWeek == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
            }
            errors = errorList.ToArray();
            return errors.Length > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupDto entityAddDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            foreach (var groupDate in entityAddDto.TrainGroupDates)
            {
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.FIXED_DAY && groupDate.FixedDay == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.DAY_OF_MONTH && groupDate.RecurrenceDayOfMonth == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.DAY_OF_WEEK && groupDate.RecurrenceDayOfWeek == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
            }
            errors = errorList.ToArray();
            return errors.Length > 0;
        }

    }
}
