using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroup;
using Core.Dtos.TrainGroupDate;
using Core.Enums;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
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

        // PUT: api/controller/5
        //[HttpPut("{id}")]
        //public override async Task<ActionResult<ApiResponse<TrainGroup>>> Put(int id, [FromBody] TrainGroupDto entityDto)
        //{
        //    if (!IsUserAuthorized("Edit"))
        //        return new ApiResponse<TrainGroup>().SetErrorResponse("error", "User is not authorized to perform this action.");

        //    if (!ModelState.IsValid)
        //        return new ApiResponse<TrainGroup>().SetErrorResponse("error", "Invalid data provided.");

        //    if (CustomValidatePUT(entityDto, out string[] errors))
        //        return BadRequest(new ApiResponse<TrainGroupDto>().SetErrorResponse("error", errors));

        //    TrainGroup entity = _mapper.Map<TrainGroup>(entityDto);
        //    ApiDbContext dbContext = _dataService.GetDbContext();

        //    // Load existing entity with related data
        //    TrainGroup? existingEntity = await dbContext.Set<TrainGroup>()
        //        .Include(x => x.TrainGroupDates)
        //        .Where(x => x.Id == id)
        //        .FirstOrDefaultAsync();

        //    if (existingEntity == null)
        //    {
        //        string className = typeof(TrainGroup).Name;
        //        return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Requested {className} not found!");
        //    }


        //    // Update scalar properties
        //    dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);

        //    // Map incoming TrainGroupDates to existing ones
        //    var incomingDates = entity.TrainGroupDates.ToList();
        //    var existingDates = existingEntity.TrainGroupDates.ToList();

        //    // Remove deleted TrainGroupDates
        //    foreach (TrainGroupDate existingDate in existingDates)
        //    {
        //        if (!incomingDates.Any(d => d.Id == existingDate.Id && d.Id > 0))
        //        {
        //            dbContext.Remove(existingDate);
        //        }
        //    }

        //    // Update or add TrainGroupDates
        //    foreach (TrainGroupDate incomingDate in incomingDates)
        //    {
        //        TrainGroupDate? existingDate = existingDates.FirstOrDefault(d => d.Id == incomingDate.Id && incomingDate.Id > 0);
        //        if (existingDate != null)
        //        {
        //            // Update existing TrainGroupDate
        //            dbContext.Entry(existingDate).CurrentValues.SetValues(incomingDate);
        //        }
        //        else
        //        {
        //            // Add new TrainGroupDate
        //            incomingDate.Id = 0;
        //            incomingDate.TrainGroupId = entity.Id;
        //            dbContext.Add(incomingDate);
        //            existingEntity.TrainGroupDates.Add(incomingDate);
        //        }
        //    }


        //    await dbContext.SaveChangesAsync();
        //    dbContext.Dispose();
        //    return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity);
        //}


        protected override bool CustomValidatePOST(TrainGroupAddDto entityAddDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            // Check required fields
            foreach (var groupDate in entityAddDto.TrainGroupDates)
            {
                if (groupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && groupDate.FixedDay == null)
                    errorList.Add("Each TrainGroupDate must have FixedDay set.");
                if (groupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH && groupDate.RecurrenceDayOfMonth == null)
                    errorList.Add("Each TrainGroupDate must have RecurrenceDayOfMonth set.");
                if (groupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK && groupDate.RecurrenceDayOfWeek == null)
                    errorList.Add("Each TrainGroupDate must have RecurrenceDayOfWeek set.");
            }
            if (errorList.Count > 0)
            {
                errors = errorList.ToArray();
                return errors.Length > 0;
            }


            // Check for date mixing
            bool isDayOfWeekAndMonthMixing =
                entityAddDto.TrainGroupDates
                .Any(x =>
                    x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                    || x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
                );
            if (isDayOfWeekAndMonthMixing)
                errorList.Add("Day of week and Day of Month doesnt mix! Please select only one of those types.");



            // Check for date mixing
            bool isFixedDateValid =
                entityAddDto.TrainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                .Any(x =>
                    entityAddDto.TrainGroupDates
                        .Where(y => y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                        .Any(y => x.FixedDay!.Value.DayOfWeek == y.RecurrenceDayOfWeek!.Value.DayOfWeek)
                );
            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same Day of week with a Day of Week row!");



            // Check for fixed date validity
            isFixedDateValid =
                entityAddDto.TrainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                .Any(x =>
                    entityAddDto.TrainGroupDates
                        .Where(y => y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                        .Any(y => x.FixedDay!.Value.DayOfWeek == y.RecurrenceDayOfMonth!.Value.DayOfWeek)
                );
            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same Day of week with a Day of Month row!");



            // Check for duplicates
            var duplicates = entityAddDto.TrainGroupDates
               .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay }) // Group by composite key
               .Where(g => g.Count() > 1)                                                       // Find groups with more than one item
               .ToList();
            if (duplicates.Count() > 0)
                errorList.Add("Duplicate rows found!");



            // Check for TrainGroup participant selected date validity
            bool isTrainGroupParticipantValid =
                entityAddDto.TrainGroupParticipants
                    .Where(x => x.SelectedDate.HasValue)
                    .Any(x =>
                        entityAddDto.TrainGroupDates.Any(y =>
                            x.SelectedDate == y.FixedDay
                            || x.SelectedDate!.Value.Day == y.RecurrenceDayOfMonth?.Day
                            || x.SelectedDate!.Value.DayOfWeek == y.RecurrenceDayOfWeek?.DayOfWeek)
                    );
            if (isTrainGroupParticipantValid)
                errorList.Add("Duplicate rows found!");

            errors = errorList.ToArray();
            return errors.Length > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupDto entityDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            // Check required fields
            foreach (var groupDate in entityDto.TrainGroupDates)
            {
                if (groupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && groupDate.FixedDay == null)
                    errorList.Add("Each TrainGroupDate must have FixedDay set.");
                if (groupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH && groupDate.RecurrenceDayOfMonth == null)
                    errorList.Add("Each TrainGroupDate must have RecurrenceDayOfMonth set.");
                if (groupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK && groupDate.RecurrenceDayOfWeek == null)
                    errorList.Add("Each TrainGroupDate must have RecurrenceDayOfWeek set.");
            }
            if (errorList.Count > 0)
            {
                errors = errorList.ToArray();
                return errors.Length > 0;
            }


            // Check for date mixing
            bool isDayOfWeekAndMonthMixing =
                entityDto.TrainGroupDates
                .Any(x =>
                    x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                    || x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
                );
            if (isDayOfWeekAndMonthMixing)
                errorList.Add("Day of week and Day of Month doesnt mix! Please select only one of those types.");



            // Check for date mixing
            bool isFixedDateValid =
                entityDto.TrainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                .Any(x =>
                    entityDto.TrainGroupDates
                        .Where(y => y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                        .Any(y => x.FixedDay!.Value.DayOfWeek == y.RecurrenceDayOfWeek!.Value.DayOfWeek)
                );
            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same Day of week with a Day of Week row!");



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
                errorList.Add("Fixed Date has the same Day of week with a Day of Month row!");



            // Check for duplicates
            var duplicates = entityDto.TrainGroupDates
               .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay }) // Group by composite key
               .Where(g => g.Count() > 1)                                                       // Find groups with more than one item
               .ToList();
            if (duplicates.Count() > 0)
                errorList.Add("Duplicate rows found!");



            // Check for TrainGroup participant selected date validity
            bool isTrainGroupParticipantValid =
                entityDto.TrainGroupParticipants
                    .Where(x => x.SelectedDate.HasValue)
                    .Any(x =>
                        entityDto.TrainGroupDates.Any(y =>
                            x.SelectedDate == y.FixedDay
                            || x.SelectedDate!.Value.Day == y.RecurrenceDayOfMonth?.Day
                            || x.SelectedDate!.Value.DayOfWeek == y.RecurrenceDayOfWeek?.DayOfWeek)
                    );
            if (isTrainGroupParticipantValid)
                errorList.Add("Duplicate rows found!");

            errors = errorList.ToArray();
            return errors.Length > 0;
        }

    }
}
