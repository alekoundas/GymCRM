
using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroupDate;
using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupDatesController : GenericController<TrainGroupDate, TrainGroupDateDto, TrainGroupDateAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;

        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupDatesController(
            IDataService dataService, 
            IMapper mapper,
            IStringLocalizer localizer) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer; 
        }



        // POST: api/TrainGroupDate/TimeSlots
        [HttpPost("TimeSlots")]
        public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        {
            DateTime selectedDate = timeSlotRequestDto.SelectedDate.Date;

            List<TrainGroupDate>? trainGroupDates = await _dataService.TrainGroupDates
                    .Include(x => x.TrainGroupParticipants)
                    .Include(x => x.TrainGroup.TrainGroupDates)
                    .Include(x => x.TrainGroup.TrainGroupParticipants)
                    .ThenInclude<TrainGroupParticipant, IEnumerable<TrainGroupParticipantUnavailableDate>>(x => x.TrainGroupParticipantUnavailableDates)
                    .Where(x =>
                        x.FixedDay == selectedDate
                        || x.RecurrenceDayOfMonth == selectedDate.Day
                        || x.RecurrenceDayOfWeek == selectedDate.DayOfWeek)
                    .ToListAsync(true);

            List<TimeSlotResponseDto>? timeSlotRequestDtos = trainGroupDates
                .GroupBy(x => x.TrainGroup)
                .Select(x => new TimeSlotResponseDto()
                {
                    Title = x.Key.Title,
                    Description = x.Key.Description,
                    Duration = x.Key.Duration,
                    StartOn = x.Key.StartOn,
                    TrainerId = x.Key.TrainerId,
                    TrainGroupId = x.Key.Id,
                    RecurrenceDates = x.Key.TrainGroupDates
                        .Where(y => y.RecurrenceDayOfMonth.HasValue || y.RecurrenceDayOfWeek.HasValue)
                        .Select(y =>
                            new TimeSlotRecurrenceDateDto()
                            {
                                TrainGroupDateId = y.Id,
                                TrainGroupDateType = y.TrainGroupDateType,
                                Date = y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                                    ? new DateTime(2000, 1, 2 + (int)y.RecurrenceDayOfWeek!.Value)
                                    : new DateTime(2000, 1, y.RecurrenceDayOfMonth!.Value),
                                IsUserJoined = y.TrainGroupParticipants.Where(z => z.SelectedDate == null).Any(z => z.UserId == new Guid(timeSlotRequestDto.UserId)),
                            }
                        )
                        .Concat(
                            x.Key.TrainGroupDates
                            .Where(y => y.FixedDay.HasValue)
                            .Where(y => y.FixedDay == selectedDate)
                            .Select(y =>
                                new TimeSlotRecurrenceDateDto()
                                {
                                    TrainGroupDateId = y.Id,
                                    TrainGroupDateType = y.TrainGroupDateType,
                                    Date = y.FixedDay!.Value,
                                    IsUserJoined = y.TrainGroupParticipants.Any(z => z.UserId == new Guid(timeSlotRequestDto.UserId)),
                                }
                            )
                        )
                        .Concat(
                                x.Key.TrainGroupDates.Any(y => y.FixedDay == selectedDate)
                                ?
                                    new List<TimeSlotRecurrenceDateDto>()
                                :
                                    new List<TimeSlotRecurrenceDateDto>()
                                    {
                                        new TimeSlotRecurrenceDateDto()
                                        {
                                            TrainGroupDateId = x.Key.TrainGroupDates.First(y =>
                                                y.FixedDay == selectedDate
                                                || y.RecurrenceDayOfMonth== selectedDate.Day
                                                || y.RecurrenceDayOfWeek == selectedDate.DayOfWeek)
                                                .Id,
                                            TrainGroupDateType = null,
                                            Date = selectedDate,
                                            IsUserJoined = x.Key.TrainGroupParticipants
                                                .Where(y => y.UserId == new Guid(timeSlotRequestDto.UserId))
                                                .Where(y => y.SelectedDate.HasValue)
                                                .Where(y => y.SelectedDate == selectedDate)
                                                .Any(),
                                        }
                                    }
                        )
                        .ToList(),
                    SpotsLeft =
                    (
                        x.Key.MaxParticipants -
                        (
                            x.Key.TrainGroupParticipants
                                .Where(y => !y.TrainGroupParticipantUnavailableDates.Any(z => z.TrainGroupParticipantId == y.Id && z.UnavailableDate == selectedDate))
                                .Where(y =>
                                    y.SelectedDate == selectedDate
                                    || y.TrainGroupDate?.FixedDay == selectedDate
                                    || y.TrainGroupDate?.RecurrenceDayOfMonth == selectedDate.Day
                                    || y.TrainGroupDate?.RecurrenceDayOfWeek == selectedDate.DayOfWeek
                                )
                                .Count()
                        )
                    ),
                })
                .ToList();

            if (timeSlotRequestDtos == null)
                return new ApiResponse<List<TimeSlotResponseDto>>().SetErrorResponse($"Requested data not found!");

            return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        }

        protected override bool CustomValidatePOST(TrainGroupDateAddDto entityDto, out string[] errors)
        {
            TrainGroupDate trainGroupDate = _mapper.Map<TrainGroupDate>(entityDto);
            errors = ValidateTrainGroupDate(trainGroupDate);
            return errors.Count() > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupDateDto entityDto, out string[] errors)
        {
            TrainGroupDate trainGroupDate = _mapper.Map<TrainGroupDate>(entityDto);
            errors = ValidateTrainGroupDate(trainGroupDate, entityDto.Id);
            return errors.Count() > 0;
        }

        private string[] ValidateTrainGroupDate(TrainGroupDate dto, int? excludeId = null)
        {
            var errorList = new List<string>();

            // Fetch existing TrainGroupDates from database
            var trainGroupDatesQuery = _dataService.TrainGroupDates
                .Where(x => x.TrainGroupId == dto.TrainGroupId);

            if (excludeId.HasValue)
                trainGroupDatesQuery = trainGroupDatesQuery.Where(x => x.Id != excludeId.Value);

            List<TrainGroupDate> trainGroupDates = trainGroupDatesQuery.ToList();

            // Check for date mixing (DAY_OF_WEEK and DAY_OF_MONTH)
            bool hasDayOfWeek = trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK);
            bool hasDayOfMonth = trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH);
            if ((hasDayOfWeek && dto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH) ||
                (hasDayOfMonth && dto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK))
            {
                errorList.Add("DAY_OF_WEEK and DAY_OF_MONTH cannot be mixed. Please select only one type.");
            }

            // Cache DAY_OF_WEEK and DAY_OF_MONTH for efficient conflict checks
            var dayOfWeekDates = trainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK && x.RecurrenceDayOfWeek.HasValue)
                .Select(x => x.RecurrenceDayOfWeek)
                .ToHashSet();
            var dayOfMonthDates = trainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH && x.RecurrenceDayOfMonth.HasValue)
                .Select(x => x.RecurrenceDayOfMonth)
                .ToHashSet();

            // Check for fixed date conflicts
            if (dto.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && dto.FixedDay.HasValue)
            {
                if (dayOfWeekDates.Contains(dto.FixedDay.Value.DayOfWeek))
                {
                    errorList.Add($"FixedDate {dto.FixedDay.Value:yyyy-MM-dd} has the same day of week as an existing DAY_OF_WEEK entry.");
                }
                if (dayOfMonthDates.Contains(dto.FixedDay.Value.Day))
                {
                    errorList.Add($"FixedDate {dto.FixedDay.Value:yyyy-MM-dd} has the same day as an existing DAY_OF_MONTH entry.");
                }
            }

            // Check for duplicates
            var mappedDto = _mapper.Map<TrainGroupDate>(dto);
            var allDates = new List<TrainGroupDate>(trainGroupDates) { mappedDto };
            var duplicates = allDates
                .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay })
                .Where(g => g.Count() > 1)
                .ToList();
            if (duplicates.Any())
            {
                errorList.Add("Duplicate TrainGroupDate found.");
            }

            return errorList.ToArray();
        }


        //protected override bool CustomValidatePOST(TrainGroupDateAddDto entityDto, out string[] errors)
        //{
        //    List<string> errorList = new List<string>();
        //    List<TrainGroupDate> trainGroupDates = new List<TrainGroupDate>();

        //    trainGroupDates = _dataService.TrainGroupDates
        //        .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
        //        .ToList();


        //    // Check required fields
        //    //if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && entityDto.FixedDay == null)
        //    //    errorList.Add("Each TrainGroupDate must have FixedDay set.");
        //    //if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH && entityDto.RecurrenceDayOfMonth == null)
        //    //    errorList.Add("Each TrainGroupDate must have RecurrenceDayOfMonth set.");
        //    //if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK && entityDto.RecurrenceDayOfWeek == null)
        //    //    errorList.Add("Each TrainGroupDate must have RecurrenceDayOfWeek set.");

        //    if (errorList.Count > 0)
        //    {
        //        errors = errorList.ToArray();
        //        return errors.Length > 0;
        //    }

        //    // Check for date mixing
        //    bool isDayOfWeekAndMonthMixing =
        //        (
        //            trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
        //            && entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
        //        )
        //        ||
        //        (
        //            trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
        //            && entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
        //        );

        //    if (isDayOfWeekAndMonthMixing)
        //        errorList.Add("Day of week and Day of Month doesnt mix! Please select only one of those types.");



        //    // Check for fixed date validity
        //    bool isFixedDateValid =
        //        trainGroupDates
        //        .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
        //        .Any(x => x.RecurrenceDayOfWeek?.DayOfWeek == entityDto.FixedDay?.DayOfWeek);

        //    if (isFixedDateValid)
        //        errorList.Add("Fixed Date has the same Day with a Day Of Week row!");



        //    // Check for fixed date validity
        //    isFixedDateValid =
        //        trainGroupDates
        //        .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
        //        .Any(x => x.RecurrenceDayOfMonth?.Day == entityDto.FixedDay?.Day);

        //    if (isFixedDateValid)
        //        errorList.Add("Fixed Date has the same Day with a Day Of Month row!");


        //    // Check for duplicates
        //    trainGroupDates.Add(_mapper.Map<TrainGroupDate>(entityDto));
        //    var duplicates = trainGroupDates
        //       .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay }) // Group by composite key
        //       .Where(g => g.Count() > 1)                                                       // Find groups with more than one item
        //       .ToList();
        //    if (duplicates.Count() > 0)
        //        errorList.Add("Duplicate rows found!");


        //    errors = errorList.ToArray();
        //    return errors.Length > 0;
        //}

        //protected override bool CustomValidatePUT(TrainGroupDateDto entityDto, out string[] errors)
        //{
        //    List<string> errorList = new List<string>();
        //    List<TrainGroupDate> trainGroupDates = new List<TrainGroupDate>();

        //    trainGroupDates = _dataService.TrainGroupDates
        //        .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
        //        .Where(x => x.Id != entityDto.Id)
        //        .ToList();


        //    // Check required fields
        //    //if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && entityDto.FixedDay == null)
        //    //    errorList.Add("Each TrainGroupDate must have FixedDay set.");
        //    //if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH && entityDto.RecurrenceDayOfMonth == null)
        //    //    errorList.Add("Each TrainGroupDate must have RecurrenceDayOfMonth set.");
        //    //if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK && entityDto.RecurrenceDayOfWeek == null)
        //    //    errorList.Add("Each TrainGroupDate must have RecurrenceDayOfWeek set.");

        //    if (errorList.Count > 0)
        //    {
        //        errors = errorList.ToArray();
        //        return errors.Length > 0;
        //    }

        //    // Check for date mixing
        //    bool isDayOfWeekAndMonthMixing =
        //        (
        //            trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
        //            && entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
        //        )
        //        ||
        //        (
        //            trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
        //            && entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
        //        );

        //    if (isDayOfWeekAndMonthMixing)
        //        errorList.Add("Day of week and Day of Month doesnt mix! Please select only one of those types.");



        //    // Check for fixed date validity
        //    bool isFixedDateValid =
        //        trainGroupDates
        //        .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
        //        .Any(x => x.RecurrenceDayOfWeek?.DayOfWeek == entityDto.FixedDay?.DayOfWeek);

        //    if (isFixedDateValid)
        //        errorList.Add("Fixed Date has the same Day with a Day Of Week row!");



        //    // Check for fixed date validity
        //    isFixedDateValid =
        //        trainGroupDates
        //        .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
        //        .Any(x => x.RecurrenceDayOfMonth?.Day == entityDto.FixedDay?.Day);

        //    if (isFixedDateValid)
        //        errorList.Add("Fixed Date has the same Day with a Day Of Month row!");


        //    // Check for duplicates
        //    trainGroupDates.Add(_mapper.Map<TrainGroupDate>(entityDto));
        //    var duplicates = trainGroupDates
        //       .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay }) // Group by composite key
        //       .Where(g => g.Count() > 1)                                                       // Find groups with more than one item
        //       .ToList();
        //    if (duplicates.Count() > 0)
        //        errorList.Add("Duplicate rows found!");


        //    errors = errorList.ToArray();
        //    return errors.Length > 0;
        //}

    }
}
