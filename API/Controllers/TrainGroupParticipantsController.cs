using AutoMapper;
using Business.Services;
using Core.Dtos;
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
    public class TrainGroupParticipantsController : GenericController<TrainGroupParticipant, TrainGroupParticipantDto, TrainGroupParticipantAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupParticipantsController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
            _mapper = mapper;
        }


        // Handle Booking. 
        // PUT: api/controller/5
        [HttpPost("UpdateParticipants")]
        public async Task<ActionResult<ApiResponse<TrainGroup>>> UpdateParticipants([FromBody] TrainGroupParticipantUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", "Invalid data provided.");


            ApiDbContext dbContext = _dataService.GetDbContext();

            // Load existing entity with related data
            TrainGroup? existingEntity = await dbContext.Set<TrainGroup>()
                .Include(x => x.TrainGroupParticipants)
                .Include(x => x.TrainGroupDates)
                .ThenInclude(x => x.TrainGroupParticipants)
                .Where(x => x.Id == updateDto.TrainGroupId)
                .FirstOrDefaultAsync();

            if (existingEntity == null)
            {
                string className = typeof(TrainGroup).Name;
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Requested {className} not found!");
            }


            // Validate user selection.
            // Cant have curent date selected with a recurring date be the same date.
            bool isCurrentDateWithConflict = updateDto.TrainGroupParticipantDtos
                .Where(x => x.SelectedDate != null)
                .Any(x => updateDto.TrainGroupParticipantDtos
                    .Where(y => y.SelectedDate == null)
                    .Any(y => existingEntity.TrainGroupDates
                        .Where(z => z.Id == y.TrainGroupDateId)
                        .Any(z => z.RecurrenceDayOfMonth?.Day == x.SelectedDate?.Day || z.RecurrenceDayOfWeek?.DayOfWeek == x.SelectedDate?.DayOfWeek)
                    )
                );

            if (isCurrentDateWithConflict)
            {
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Current date is already selected in a Recurrence date! Please select one of them.");
            }


            List<TrainGroupParticipant> incomingParticipants = _mapper.Map<List<TrainGroupParticipant>>(updateDto.TrainGroupParticipantDtos);
            List<TrainGroupParticipant> existingParticipants = existingEntity.TrainGroupParticipants
                .Where(x => x.SelectedDate.HasValue ? x.SelectedDate == updateDto.SelectedDate : true)
                .ToList();



            // If selected date exists, assign TrainGroupDateId.
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
                if (incomingParticipant.SelectedDate.HasValue && incomingParticipant.TrainGroupDateId == -1)
                {
                    List<TrainGroupDate> selectedTrainGroupDate = existingEntity
                            .TrainGroupDates
                            .Where(x =>
                                x.FixedDay == updateDto.SelectedDate ||
                                x.RecurrenceDayOfMonth?.Day == updateDto.SelectedDate.Day ||
                                x.RecurrenceDayOfWeek?.DayOfWeek == updateDto.SelectedDate.DayOfWeek)
                            .ToList();

                    if (selectedTrainGroupDate.Count() == 1)
                        incomingParticipant.TrainGroupDateId = selectedTrainGroupDate.First().Id;
                    else
                    {
                        dbContext.Dispose();
                        return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Something unexpected happend! Please contact Administrator.");
                    }
                }


            // Handle Deletions and Unchanged participants.
            var participantsToRemove = existingParticipants
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .ToList(); // Create a copy to avoid modifying collection during iteration

            foreach (TrainGroupParticipant existingParticipant in participantsToRemove)
            {
                TrainGroupParticipant? incomingParticipant = incomingParticipants
                    .FirstOrDefault(x => x.SelectedDate == existingParticipant.SelectedDate && x.TrainGroupDateId == existingParticipant.TrainGroupDateId);

                // Remove unchanged incoming Participants
                if (incomingParticipant != null)
                    incomingParticipants.Remove(incomingParticipant);
                // Remove deleted existing Participants
                else
                {
                    existingParticipants.Remove(existingParticipant);
                    dbContext.Remove(existingParticipant);
                }
            }

            // Validate if any of the incoming participants is already joined.
            // Check if any of the incoming participants contains selected date, then check for recurring dates.
            bool isAlreadyParticipant = incomingParticipants.Any(x =>
                existingParticipants
                    .Where(x => x.UserId == new Guid(updateDto.UserId))
                    .Any(y => x.TrainGroupDateId == y.TrainGroupDateId)
            );

            if (isAlreadyParticipant)
            {
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Participant already joined!");
            }


            // Validate max participants
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {
                int numberOfParticipants = existingParticipants
                    .Where(x => incomingParticipant.TrainGroupDateId == x.TrainGroupDateId)
                    .Where(x => !incomingParticipant.TrainGroupParticipantUnavailableDates.Any(y =>
                        y.UnavailableDate == x.TrainGroupDate.FixedDay ||
                        y.UnavailableDate.Day == x.TrainGroupDate.RecurrenceDayOfMonth?.Day ||
                        y.UnavailableDate.DayOfWeek == x.TrainGroupDate.RecurrenceDayOfMonth?.DayOfWeek))
                    .ToList()
                    .Count();

                if (numberOfParticipants >= existingEntity.MaxParticipants)
                {
                    dbContext.Dispose();
                    return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Maximum amount of participants reached for ");
                }
            }


            // Add TrainGroup Participants
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {
                // Check which future dates user cant book.
                // Only applies to DAY_OF_MONTH and DAY_OF_WEEK
                if (incomingParticipant.SelectedDate == null)
                {
                    List<TrainGroupParticipantUnavailableDate> futureUnavailableDates =
                    existingParticipants
                        .Where(x => x.SelectedDate != null)
                        .Where(x => x.SelectedDate >= DateTime.UtcNow)
                        .Where(x =>
                            x.TrainGroup.MaxParticipants -
                            x.TrainGroup.TrainGroupParticipants
                                .Where(y => y.TrainGroupDate.TrainGroupDateType != TrainGroupDateTypeEnum.FIXED_DAY)
                                .Where(y =>
                                    y.TrainGroupDate.RecurrenceDayOfMonth?.Day == x.SelectedDate!.Value.Day ||
                                    y.TrainGroupDate.RecurrenceDayOfWeek?.DayOfWeek == x.SelectedDate!.Value.DayOfWeek ||
                                    y.SelectedDate == x.SelectedDate!.Value
                                )
                                .Count()
                            <= 0
                        )
                        .Select(x => new TrainGroupParticipantUnavailableDate() { UnavailableDate = x.SelectedDate!.Value })
                        .ToList();

                    incomingParticipant.TrainGroupParticipantUnavailableDates = futureUnavailableDates;
                }

                // Add new Participant
                incomingParticipant.Id = 0;
                dbContext.Add(incomingParticipant);
            }


            await dbContext.SaveChangesAsync();
            dbContext.Dispose();

            //if (futureUnavailableDates.Count > 0)
            //    return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity, "warning", "Future unavailable dates that cannot be booked: " + futureUnavailableDates.Select(x => x.UnavailableDate.ToUniversalTime().ToString()).ToList().ToString());

            return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity);
        }

        protected override bool CustomValidatePOST(TrainGroupParticipantAddDto entityDto, out string[] errors)
        {
            TrainGroupParticipant trainGroupParticipant = _mapper.Map<TrainGroupParticipant>(entityDto);
            errors = ValidateTrainGroupParticipant(trainGroupParticipant);
            return errors.Count() > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupParticipantDto entityDto, out string[] errors)
        {
            TrainGroupParticipant trainGroupParticipant = _mapper.Map<TrainGroupParticipant>(entityDto);
            errors = ValidateTrainGroupParticipant(trainGroupParticipant, entityDto.Id);
            return errors.Count() > 0;
        }

        private string[] ValidateTrainGroupParticipant(TrainGroupParticipant participantDto, int? excludeParticipantId = null)
        {
            List<string> errorList = new List<string>();

            // Load TrainGroupDates once and reuse
            List<TrainGroupDate> trainGroupDates = _dataService.TrainGroupDates
                .Where(x => x.TrainGroupId == participantDto.TrainGroupId)
                .ToList();

            if (participantDto.SelectedDate.HasValue)
            {
                // Validate selected date matches a TrainGroupDate
                bool isDateValid = trainGroupDates.Any(x =>
                    x.FixedDay == participantDto.SelectedDate ||
                    x.RecurrenceDayOfMonth?.Day == participantDto.SelectedDate.Value.Day ||
                    x.RecurrenceDayOfWeek?.DayOfWeek == participantDto.SelectedDate.Value.DayOfWeek);

                if (!isDateValid)
                    errorList.Add("Participant selected date doesn't match any of the Train Group Dates!");

                // Check for overlap with FixedDate
                if (trainGroupDates.Any(x => x.FixedDay == participantDto.SelectedDate))
                    errorList.Add("FixedDate doesn't allow one-off Participants, please add via Train Group date participants table!");
            }

            // Validate if user is already a participant
            bool isAlreadyParticipant = _dataService.TrainGroupParticipants
                .Where(x => x.UserId == participantDto.UserId)
                .Where(x => x.TrainGroupId == participantDto.TrainGroupId)
                .Where(x => excludeParticipantId == null || x.Id != excludeParticipantId) // Used in PUT
                .Where(x => x.SelectedDate == null || x.SelectedDate.Value.Date >= DateTime.UtcNow.Date)
                .Any(x => x.TrainGroupDateId == participantDto.TrainGroupDateId);
           

            if (isAlreadyParticipant)
                errorList.Add("Participant already joined!");

            return errorList.ToArray();
        }
    }
}
