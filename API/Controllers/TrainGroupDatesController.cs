
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


        // PUT: api/controller/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<TrainGroupDate>>> UpdateParticipants([FromBody] TrainGroupDate entityDto)
        {
            if (!IsUserAuthorized("Edit"))
                return new ApiResponse<TrainGroupDate>().SetErrorResponse("error", "User is not authorized to perform this action.");

            if (!ModelState.IsValid)
                return new ApiResponse<TrainGroupDate>().SetErrorResponse("error", "Invalid data provided.");


            TrainGroupDate entity = _mapper.Map<TrainGroupDate>(entityDto);
            ApiDbContext dbContext = _dataService.GetDbContext();

            // Load existing entity with related data
            TrainGroupDate? existingEntity = await dbContext.Set<TrainGroupDate>()
                .Include(x => x.TrainGroupDateParticipants)
                .Where(x => x.Id == entity.Id)
                .FirstOrDefaultAsync();

            if (existingEntity == null)
            {
                string className = typeof(TrainGroupDate).Name;
                return new ApiResponse<TrainGroupDate>().SetErrorResponse("error", $"Requested {className} not found!");
            }


            // Update scalar properties
            //dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);

            // Map incoming TrainGroupDates to existing ones
            List<TrainGroupDateParticipant> incomingParticipants = entity.TrainGroupDateParticipants.ToList();
            List<TrainGroupDateParticipant> existingParticipants = existingEntity.TrainGroupDateParticipants.ToList();

            // Remove deleted TrainGroupDates
            foreach (TrainGroupDateParticipant existingParticipant in existingParticipants)
                if (!incomingParticipants.Any(d => d.Id == existingParticipant.Id && d.Id > 0))
                    dbContext.Remove(existingParticipant);

            // Update or add TrainGroupDates
            foreach (TrainGroupDateParticipant incomingParticipant in incomingParticipants)
            {
                TrainGroupDateParticipant? existingDate = existingParticipants.FirstOrDefault(d => d.Id == incomingParticipant.Id && incomingParticipant.Id > 0);
                if (existingDate != null)
                {
                    // Update existing TrainGroupDate
                    dbContext.Entry(existingDate).CurrentValues.SetValues(incomingParticipant);
                }
                else
                {
                    // Add new TrainGroupDate
                    incomingParticipant.Id = 0;
                    incomingParticipant.TrainGroupDateId = entity.Id;
                    dbContext.Add(incomingParticipant);
                    existingEntity.TrainGroupDateParticipants.Add(incomingParticipant);
                }
            }


            await dbContext.SaveChangesAsync();
            dbContext.Dispose();
            return new ApiResponse<TrainGroupDate>().SetSuccessResponse(existingEntity);
        }
    }
}
