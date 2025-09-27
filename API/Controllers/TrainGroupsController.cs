using AutoMapper;
using Business.Repository;
using Business.Services;
using Core.Dtos.TrainGroup;
using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TrainGroupsController : GenericController<TrainGroup, TrainGroupDto, TrainGroupAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupController> _logger;

        public TrainGroupsController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        protected override bool CustomValidatePOST(TrainGroupAddDto entityDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            // Check for date mixing
            bool isDayOfWeekAndMonthMixing =
                entityDto.TrainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                && entityDto.TrainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH);
            if (isDayOfWeekAndMonthMixing)
                errorList.Add("Day of week and Day of Month doesnt mix! Please select only one of those types.");



            // Check for fixed date validity
            bool isFixedDateValid =
                entityDto.TrainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                .Any(x =>
                    entityDto.TrainGroupDates
                        .Where(y => y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                        .Any(y => x.FixedDay!.Value.DayOfWeek == y.RecurrenceDayOfWeek!.Value.DayOfWeek)
                );
            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same Day of week with an existing day of week row!");



            // Check for fixed date validity
            isFixedDateValid =
                entityDto.TrainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                .Any(x =>
                    entityDto.TrainGroupDates
                        .Where(y => y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                        .Any(y => x.FixedDay!.Value.DayOfWeek == y.RecurrenceDayOfMonth!.Value.DayOfWeek)
                );
            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same day with an existing day of month row!");



            // Check for duplicates Dates
            var duplicates = entityDto.TrainGroupDates
               .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay }) // Group by composite key
               .Where(g => g.Count() > 1)                                                       // Find groups with more than one item
               .ToList();
            if (duplicates.Count() > 0)
                errorList.Add("Duplicate rows found!");



            // Check for duplicate participants
            var duplicateParticipants = entityDto.TrainGroupParticipants
               .GroupBy(x => new { x.SelectedDate, x.TrainGroupDateId }) // Group by composite key
               .Where(g => g.Count() > 1)                               // Find groups with more than one item
               .ToList();
            if (duplicates.Count() > 0)
                errorList.Add("Duplicate participants found!");



            // Check for TrainGroup participant selected date validity
            if (entityDto.TrainGroupParticipants.Count() > 0)
            {
                bool isTrainGroupParticipantValid =
                    !entityDto.TrainGroupParticipants
                        .Where(x => x.SelectedDate.HasValue)
                        .Any(x =>
                            entityDto.TrainGroupDates.Any(y =>
                                x.SelectedDate == y.FixedDay
                                || x.SelectedDate!.Value.Day == y.RecurrenceDayOfMonth?.Day
                                || x.SelectedDate!.Value.DayOfWeek == y.RecurrenceDayOfWeek?.DayOfWeek)
                        );
                if (isTrainGroupParticipantValid)
                    errorList.Add("Participant selected date doesnt match any of the Train Group Dates!");
            }

            errors = errorList.ToArray();
            return errors.Length > 0;
        }


        protected override void DataTableQueryUpdate(IGenericRepository<TrainGroup> query)
        {
            query = query.Include(x => x.Trainer);
        }
    }
}
