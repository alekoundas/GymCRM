
using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroup;
using Core.Dtos.TrainGroupDate;
using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupDatesController : GenericController<TrainGroupDate, TrainGroupDateDto, TrainGroupDateAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupDatesController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
            _mapper = mapper;
        }



        // POST: api/TrainGroupDate/TimeSlots
        [HttpPost("TimeSlots")]
        public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        {
            //if (!IsUserAuthorized("View"))
            //    return new ApiResponse<List<TimeSlotResponseDto>>().SetErrorResponse("error", "User is not authorized to perform this action.");


            DateOnly selectedDate = DateOnly.FromDateTime(timeSlotRequestDto.SelectedDate);
            DayOfWeekEnum dayOfWeekEnum = (DayOfWeekEnum)selectedDate.DayOfWeek;

            List<TrainGroupDate>? trainGroupDates = await _dataService.TrainGroupDates
                    .Include(x => x.TrainGroup.TrainGroupDates)
                    .Include(x => x.TrainGroup.TrainGroupParticipants)
                    .Include(x => x.TrainGroupParticipants)
                    .Where(x =>
                        x.FixedDay == timeSlotRequestDto.SelectedDate
                        || x.RecurrenceDayOfMonth!.Value.Day == timeSlotRequestDto.SelectedDate.Day
                        || x.RecurrenceDayOfWeek!.Value.DayOfWeek == timeSlotRequestDto.SelectedDate.DayOfWeek)
                    .ToListAsync(true);

            List<TimeSlotResponseDto>? timeSlotRequestDtos = trainGroupDates
                .GroupBy(x=> x.TrainGroup)
                .Select(x => new TimeSlotResponseDto()
                {
                    Title = x.Key.Title,
                    Description = x.Key.Description,
                    Duration = x.Key.Duration,
                    StartOn = x.Key.StartOn,
                    TrainerId = x.Key.TrainerId,
                    TrainGroupId = x.Key.Id,
                    //TrainGroupDateId = x.Id,
                    RecurrenceDates = x
                        .Where(y => y.RecurrenceDayOfMonth.HasValue || y.RecurrenceDayOfWeek.HasValue)
                        .Select(y =>
                            new TimeSlotRecurrenceDateDto()
                            {
                                TrainGroupDateId = y.Id,
                                TrainGroupDateType = y.TrainGroupDateType,
                                Date = y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                                    ? y.RecurrenceDayOfWeek!.Value
                                    : y.RecurrenceDayOfMonth!.Value,
                                IsUserJoined = y.TrainGroupParticipants.Any(z => z.UserId == new Guid(timeSlotRequestDto.UserId)),
                                TrainGroupParticipantId = y.TrainGroupParticipants.FirstOrDefault(z => z.UserId == new Guid(timeSlotRequestDto.UserId))?.Id
                            }
                        )
                        .Concat(
                            x
                            .Where(y => y.FixedDay.HasValue)
                            .Select(y =>
                                new TimeSlotRecurrenceDateDto()
                                {
                                    TrainGroupDateId = y.Id,
                                    TrainGroupDateType = y.TrainGroupDateType,
                                    Date = y.FixedDay!.Value,
                                    IsUserJoined = y.TrainGroupParticipants.Any(z => z.UserId == new Guid(timeSlotRequestDto.UserId)),
                                    TrainGroupParticipantId = y.TrainGroupParticipants.FirstOrDefault(z => z.UserId == new Guid(timeSlotRequestDto.UserId))?.Id
                                }
                            )
                        )
                        .Concat(
                            new List<TimeSlotRecurrenceDateDto>()
                            {
                                new TimeSlotRecurrenceDateDto()
                                {
                                    TrainGroupDateId = null,
                                    TrainGroupDateType = null,
                                    Date = timeSlotRequestDto.SelectedDate,
                                    IsUserJoined = x.Key.TrainGroupParticipants
                                        .Where(y => y.UserId == new Guid(timeSlotRequestDto.UserId))
                                        .Where(y => y.SelectedDate.HasValue)
                                        .Where(y => y.SelectedDate!.Value == timeSlotRequestDto.SelectedDate)
                                        .Any(),
                                    TrainGroupParticipantId =x.Key.TrainGroupParticipants
                                        .Where(y => y.UserId == new Guid(timeSlotRequestDto.UserId))
                                        .Where(y => y.SelectedDate.HasValue)
                                        .Where(y => y.SelectedDate!.Value == timeSlotRequestDto.SelectedDate)
                                        .FirstOrDefault()?.Id,
                                }
                            }
                        )
                        .ToList(),
                    SpotsLeft =
                    (
                        x.Key.MaxParticipants -
                        (
                            x.Key.TrainGroupParticipants
                                .Where(y =>
                                    y.SelectedDate == timeSlotRequestDto.SelectedDate
                                    || y.TrainGroupDate?.FixedDay == timeSlotRequestDto.SelectedDate
                                    || y.TrainGroupDate?.RecurrenceDayOfMonth?.Day == timeSlotRequestDto.SelectedDate.Day
                                    || y.TrainGroupDate?.RecurrenceDayOfWeek?.DayOfWeek == timeSlotRequestDto.SelectedDate.DayOfWeek
                                )
                                //.DistinctBy(y => y.UserId)
                                .Count()
                        )
                    ),
                })
                .ToList();

            if (timeSlotRequestDtos == null)
                return new ApiResponse<List<TimeSlotResponseDto>>().SetErrorResponse("error", $"Requested data not found!");

            return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        }


        protected override bool CustomValidatePOST(TrainGroupDateAddDto entityDto, out string[] errors)
        {
            List<string> errorList = new List<string>();
            List<TrainGroupDate> trainGroupDates = new List<TrainGroupDate>();

            trainGroupDates = _dataService.TrainGroupDates
                .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
                .ToList();


            // Check required fields
            if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && entityDto.FixedDay == null)
                errorList.Add("Each TrainGroupDate must have FixedDay set.");
            if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH && entityDto.RecurrenceDayOfMonth == null)
                errorList.Add("Each TrainGroupDate must have RecurrenceDayOfMonth set.");
            if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK && entityDto.RecurrenceDayOfWeek == null)
                errorList.Add("Each TrainGroupDate must have RecurrenceDayOfWeek set.");

            if (errorList.Count > 0)
            {
                errors = errorList.ToArray();
                return errors.Length > 0;
            }

            // Check for date mixing
            bool isDayOfWeekAndMonthMixing =
                (
                    trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                    && entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
                )
                ||
                (
                    trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                    && entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                );

            if (isDayOfWeekAndMonthMixing)
                errorList.Add("Day of week and Day of Month doesnt mix! Please select only one of those types.");



            // Check for fixed date validity
            bool isFixedDateValid =
                trainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                .Any(x => x.RecurrenceDayOfWeek?.DayOfWeek == entityDto.FixedDay?.DayOfWeek);

            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same Day with a Day Of Week row!");



            // Check for fixed date validity
            isFixedDateValid =
                trainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                .Any(x => x.RecurrenceDayOfMonth?.Day == entityDto.FixedDay?.Day);

            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same Day with a Day Of Month row!");


            // Check for duplicates
            trainGroupDates.Add(_mapper.Map<TrainGroupDate>(entityDto));
            var duplicates = trainGroupDates
               .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay }) // Group by composite key
               .Where(g => g.Count() > 1)                                                       // Find groups with more than one item
               .ToList();
            if (duplicates.Count() > 0)
                errorList.Add("Duplicate rows found!");


            errors = errorList.ToArray();
            return errors.Length > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupDateDto entityDto, out string[] errors)
        {
            List<string> errorList = new List<string>();
            List<TrainGroupDate> trainGroupDates = new List<TrainGroupDate>();

            trainGroupDates = _dataService.TrainGroupDates
                .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
                .Where(x => x.Id != entityDto.Id)
                .ToList();


            // Check required fields
            if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && entityDto.FixedDay == null)
                errorList.Add("Each TrainGroupDate must have FixedDay set.");
            if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH && entityDto.RecurrenceDayOfMonth == null)
                errorList.Add("Each TrainGroupDate must have RecurrenceDayOfMonth set.");
            if (entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK && entityDto.RecurrenceDayOfWeek == null)
                errorList.Add("Each TrainGroupDate must have RecurrenceDayOfWeek set.");

            if (errorList.Count > 0)
            {
                errors = errorList.ToArray();
                return errors.Length > 0;
            }

            // Check for date mixing
            bool isDayOfWeekAndMonthMixing =
                (
                    trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                    && entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
                )
                ||
                (
                    trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                    && entityDto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                );

            if (isDayOfWeekAndMonthMixing)
                errorList.Add("Day of week and Day of Month doesnt mix! Please select only one of those types.");



            // Check for fixed date validity
            bool isFixedDateValid =
                trainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                .Any(x => x.RecurrenceDayOfWeek?.DayOfWeek == entityDto.FixedDay?.DayOfWeek);

            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same Day with a Day Of Week row!");



            // Check for fixed date validity
            isFixedDateValid =
                trainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                .Any(x => x.RecurrenceDayOfMonth?.Day == entityDto.FixedDay?.Day);

            if (isFixedDateValid)
                errorList.Add("Fixed Date has the same Day with a Day Of Month row!");


            // Check for duplicates
            trainGroupDates.Add(_mapper.Map<TrainGroupDate>(entityDto));
            var duplicates = trainGroupDates
               .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay }) // Group by composite key
               .Where(g => g.Count() > 1)                                                       // Find groups with more than one item
               .ToList();
            if (duplicates.Count() > 0)
                errorList.Add("Duplicate rows found!");


            errors = errorList.ToArray();
            return errors.Length > 0;
        }

    }
}
