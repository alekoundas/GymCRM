
using AutoMapper;
using Business.Services;
using Core.Dtos;
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
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupDatesController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
        }



        // POST: api/TrainGroupDate/TimeSlots
        [HttpPost("TimeSlots")]
        public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<List<TimeSlotResponseDto>>().SetErrorResponse("error", "User is not authorized to perform this action.");


            DateOnly selectedDate = DateOnly.FromDateTime(timeSlotRequestDto.SelectedDate);
            DayOfWeekEnum dayOfWeekEnum = (DayOfWeekEnum)selectedDate.DayOfWeek;

            List<TrainGroupDate>? timeslots = await _dataService.TrainGroupDates
                .Include(x => x.TrainGroup.TrainGroupDates)
                .Include(x => x.TrainGroupDateParticipants)
                .Where(x =>
                    x.FixedDay == timeSlotRequestDto.SelectedDate
                    || x.RecurrenceDayOfMonth!.Value.Day == timeSlotRequestDto.SelectedDate.Day
                    || x.RecurrenceDayOfWeek!.Value.DayOfWeek == timeSlotRequestDto.SelectedDate.DayOfWeek)
                .ToListAsync(true);


            List<TimeSlotResponseDto>? timeSlotRequestDtos = timeslots
                .Select(x => new TimeSlotResponseDto()
                {
                    Title = x.TrainGroup.Title,
                    Description = x.TrainGroup.Description,
                    Duration = x.TrainGroup.Duration,
                    StartOn = x.TrainGroup.StartOn,
                    TrainerId = x.TrainGroup.TrainerId,
                    TrainGroupId = x.TrainGroupId,
                    TrainGroupDateId = x.Id,
                    RecurrenceDates = x.TrainGroup.TrainGroupDates
                        .Where(y => y.RecurrenceDayOfMonth.HasValue || y.RecurrenceDayOfWeek.HasValue)
                        .Select(y =>
                            new TimeSlotRecurrenceDateDto()
                            {
                                TrainGroupDateId = y.Id,
                                TrainGroupDateType = y.TrainGroupDateType,
                                Date = y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                                    ? y.RecurrenceDayOfWeek!.Value
                                    : y.RecurrenceDayOfMonth!.Value,
                            }
                        )
                        .ToList(),
                    SpotsLeft =
                    (
                        x.TrainGroup.MaxParticipants -
                        x.TrainGroup.TrainGroupDates
                        .Where(y =>
                            (y.FixedDay.HasValue ? y.FixedDay.Value == timeSlotRequestDto.SelectedDate : false)
                            || (y.RecurrenceDayOfMonth.HasValue ? y.RecurrenceDayOfMonth.Value.Day == timeSlotRequestDto.SelectedDate.Day : false)
                            || (y.RecurrenceDayOfWeek.HasValue ? y.RecurrenceDayOfWeek.Value.DayOfWeek == timeSlotRequestDto.SelectedDate.DayOfWeek : false)
                         )
                        .SelectMany(y => y.TrainGroupDateParticipants)
                        .Where(y => y.SelectedDate == null || y.SelectedDate == timeSlotRequestDto.SelectedDate)
                        .DistinctBy(y => y.UserId)
                        .Count()
                    ),
                })
                .DistinctBy(x => x.TrainGroupId)
                .ToList();

            if (timeSlotRequestDtos == null)
                return new ApiResponse<List<TimeSlotResponseDto>>().SetErrorResponse("error", $"Requested data not found!");

            return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        }

    }
}
