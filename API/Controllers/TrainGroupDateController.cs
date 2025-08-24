
using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroupDate;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupDateController : GenericController<TrainGroupDate, TrainGroupDateDto, TrainGroupDateAddDto>
    {
        private readonly IDataService _dataService;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupDateController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
        }



        // POST: api/TrainGroupDate/TimeSlots
        [HttpPost("TimeSlots")]
        public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<List<TimeSlotResponseDto>>().SetErrorResponse("error", "User is not authorized to perform this action.");

            var aaa = await _dataService.TrainGroupDates.ToListAsync();

            DateOnly selectedDate = DateOnly.FromDateTime(timeSlotRequestDto.SelectedDate);
            string dayOfWeekString = selectedDate.DayOfWeek.ToString();
            DayOfWeekEnum dayOfWeekEnum = (DayOfWeekEnum)selectedDate.DayOfWeek;

            List<TrainGroupDate>? timeslots = await _dataService.TrainGroupDates
                .Include(x => x.TrainGroup)
                .Where(x =>
                    x.FixedDay == selectedDate
                    || x.RecurrenceDayOfMonth == selectedDate.Day
                    || (x.RecurrenceDayOfWeek != null && x.RecurrenceDayOfWeek == dayOfWeekEnum))
                .ToListAsync();


            List<TimeSlotResponseDto>? timeSlotRequestDtos = timeslots
                .Select(x => new TimeSlotResponseDto()
                {
                    Duration = new DateTime(2000, 1, 1, x.TrainGroup.Duration.Hours, x.TrainGroup.Duration.Minutes, 0),
                    StartOn = new DateTime(2000, 1, 1, x.TrainGroup.StartOn.Hours, x.TrainGroup.StartOn.Minutes, 0),
                    DisplayDate = x.TrainGroup.StartOn.ToString(@"hh\:mm"),
                    TrainGroupDateId = x.TrainGroupId
                })
                .ToList();

            if (timeSlotRequestDtos == null)
            {
                return new ApiResponse<List<TimeSlotResponseDto>>().SetErrorResponse("error", $"Requested data not found!");
            }

            return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        }

        // Override Get to add custom logic
        //public override async Task<ActionResult<ApiResponse<TrainGroup>>> Get(int id)
        //{
        //    // Custom logic (e.g., include related data)
        //    var repository = _dataService.GetGenericRepository<TrainGroup>();
        //    var entity = await repository.FindAsync(id, include: source => source
        //        .Include(t => t.Trainer)
        //        .Include(t => t.RepeatingParticipants)
        //        .Include(t => t.TrainGroupDates));

        //    if (entity == null)
        //    {
        //        return NotFound(new ApiResponse<TrainGroup>().SetErrorResponse("TrainGroup", "Requested TrainGroup not found!"));
        //    }

        //    return Ok(new ApiResponse<TrainGroup>().SetSuccessResponse(entity));
        //}

        //// Add a custom endpoint
        //[HttpPost("{id}/add-participant")]
        //public async Task<ActionResult<ApiResponse<TrainGroup>>> AddParticipant(int id, [FromBody] Guid participantId)
        //{
        //    if (!IsUserAuthorized("AddParticipant"))
        //        return Unauthorized(new ApiResponse<TrainGroup>().SetErrorResponse("Authorization", "User is not authorized to perform this action."));

        //    // Custom logic to add a participant
        //    // Example: Add participant to RepeatingParticipants
        //    var trainGroup = await _dataService.GetGenericRepository<TrainGroup>().FindAsync(id);
        //    if (trainGroup == null)
        //        return NotFound(new ApiResponse<TrainGroup>().SetErrorResponse("TrainGroup", "Requested TrainGroup not found!"));

        //    var user = await _dataService.GetGenericRepository<User>().FindAsync(participantId);
        //    if (user == null)
        //        return NotFound(new ApiResponse<TrainGroup>().SetErrorResponse("User", "Requested User not found!"));

        //    trainGroup.RepeatingParticipants.Add(user);
        //    await _dataService.SaveChangesAsync();

        //    return Ok(new ApiResponse<TrainGroup>().SetSuccessResponse(trainGroup));
        //}
    }
}
