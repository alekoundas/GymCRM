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
                        .Any(z => z.RecurrenceDayOfMonth?.Day == x.SelectedDate?.Day || z.RecurrenceDayOfWeek?.DayOfWeek == x.SelectedDate?.DayOfWeek
                        )
                    )
                );

            if (isCurrentDateWithConflict)
            {
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Current date is already selected in a Recurrence date! Please select one of them.");
            }





            List<TrainGroupParticipant> incomingParticipants = _mapper.Map<List<TrainGroupParticipant>>(updateDto.TrainGroupParticipantDtos);
            List<TrainGroupParticipant> existingTrainGroupParticipants = existingEntity.TrainGroupParticipants
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .Where(x => x.SelectedDate == updateDto.SelectedDate)
                .ToList();
            List<TrainGroupParticipant> existingTrainGroupDateParticipants = existingEntity.TrainGroupDates
                .SelectMany(x => x.TrainGroupParticipants)
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .ToList();

            // Remove deleted TrainGroupParticipants
            foreach (TrainGroupParticipant existingParticipant in existingTrainGroupDateParticipants)
            {
                TrainGroupParticipant? incomingParticipant = incomingParticipants.FirstOrDefault(x =>
                    x.UserId == existingParticipant.UserId
                    && x.TrainGroupId == existingParticipant.TrainGroupId
                    && x.SelectedDate == existingParticipant.SelectedDate
                    && x.TrainGroupDateId == existingParticipant.TrainGroupDateId);

                if (incomingParticipant != null)
                    incomingParticipants.Remove(incomingParticipant);
                else
                    dbContext.Remove(existingParticipant);
            }

            // Remove deleted TrainGroupDateParticipants
            foreach (TrainGroupParticipant existingParticipant in existingTrainGroupParticipants)
            {
                bool alreadyExists = incomingParticipants.Any(x =>
                    x.UserId == existingParticipant.UserId
                    && x.TrainGroupId == existingParticipant.TrainGroupId
                    && x.SelectedDate == existingParticipant.SelectedDate
                    && x.TrainGroupDateId == existingParticipant.TrainGroupDateId);

                if (alreadyExists)
                    incomingParticipants.Remove(incomingParticipants.First(x => x.Id == existingParticipant.Id));
                else
                    dbContext.Remove(existingParticipant);
            }



            // Validate Add participants for MaxParticipant
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {
                bool isAlreadyParticipant = dbContext.Set<TrainGroupParticipant>()
                .Where(x => x.UserId == incomingParticipant.UserId)
                .Where(x => x.TrainGroupId == incomingParticipant.TrainGroupId)
                .Where(x =>
                  x.SelectedDate == null
                  ||
                  (
                      x.SelectedDate.Value.Year >= DateTime.UtcNow.Year
                      && x.SelectedDate.Value.Month >= DateTime.UtcNow.Month
                      && x.SelectedDate.Value.Day >= DateTime.UtcNow.Day
                  )
                )
                .Any(x =>
                    x.SelectedDate == incomingParticipant.SelectedDate

                    // Validate incoming Reccurence TrainGroupDate vs SelectedDate 
                    // If TrainGroupParticipant exists with selected date greater than current date, check if incoming TrainGroupDateParticipant overlaps
                    || (x.SelectedDate != null && x.TrainGroup.TrainGroupDates.Any(y => y.Id == incomingParticipant.TrainGroupDateId && y.RecurrenceDayOfWeek != null && x.SelectedDate.Value.DayOfWeek == y.RecurrenceDayOfWeek.Value.DayOfWeek))
                    || (x.SelectedDate != null && x.TrainGroup.TrainGroupDates.Any(y => y.Id == incomingParticipant.TrainGroupDateId && y.RecurrenceDayOfMonth != null && x.SelectedDate.Value.Day == y.RecurrenceDayOfMonth.Value.Day))

                    // Validate incoming SelectedDate vs FixedDay, RecurrenceDayOfWeek, RecurrenceDayOfMonth 
                    || (x.TrainGroupDate != null && x.TrainGroupDate.FixedDay != null && incomingParticipant.SelectedDate != null && x.TrainGroupDate.FixedDay.Value == incomingParticipant.SelectedDate.Value)
                    || (x.TrainGroupDate != null && x.TrainGroupDate.RecurrenceDayOfWeek != null && incomingParticipant.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfWeek.Value.DayOfWeek == incomingParticipant.SelectedDate.Value.DayOfWeek)
                    || (x.TrainGroupDate != null && x.TrainGroupDate.RecurrenceDayOfMonth != null && incomingParticipant.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfMonth.Value.Day == incomingParticipant.SelectedDate.Value.Day)
                );

                if (isAlreadyParticipant)
                {
                    dbContext.Dispose();
                    return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Participant already joined!");
                }

                List<TrainGroupParticipant> sss = dbContext.Set<TrainGroupParticipant>()
                        .Include(x => x.TrainGroupDate)
                        .Include(x => x.TrainGroup)
                        .Where(x => x.TrainGroupId == updateDto.TrainGroupId)
                        .ToList()
                        .Where(x => incomingParticipant.SelectedDate.HasValue  // If incoming participant is TrainGroupParticipants, count TrainGroupParticipants and TrainGroupDatePatricipants
                            ?
                                incomingParticipant.SelectedDate == x.SelectedDate
                                || incomingParticipant.SelectedDate == x.TrainGroupDate?.FixedDay
                                || incomingParticipant.SelectedDate.Value.Day == x.TrainGroupDate?.RecurrenceDayOfMonth?.Day
                                || incomingParticipant.SelectedDate.Value.DayOfWeek == x.TrainGroupDate?.RecurrenceDayOfWeek?.DayOfWeek
                            : true
                        )
                        //.Where(x => incomingParticipant.TrainGroupDateId != null  // Else count TrainGroupDatePatricipants only!
                        //    ? incomingParticipant.TrainGroupDateId == x.TrainGroupDateId
                        //        || existingEntity.TrainGroupDates.First(y => y.Id == incomingParticipant.TrainGroupDateId).RecurrenceDayOfMonth?.Day == x.SelectedDate?.Day
                        //        || existingEntity.TrainGroupDates.First(y => y.Id == incomingParticipant.TrainGroupDateId).RecurrenceDayOfWeek?.DayOfWeek == x.SelectedDate?.DayOfWeek
                        //        || existingEntity.TrainGroupDates.First(y => y.Id == incomingParticipant.TrainGroupDateId).FixedDay == x.SelectedDate
                        //    : true
                        //)
                        .Where(x => incomingParticipant.TrainGroupDateId.HasValue  // Else count TrainGroupDatePatricipants only!
                            ? incomingParticipant.TrainGroupDateId == x.TrainGroupDateId
                            : true
                        )
                        .ToList();

                if (sss.Count() >= existingEntity.MaxParticipants)
                {
                    dbContext.Dispose();
                    return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Maximum amount of participants reached for ");
                }

            }


            // Add TrainGroup Participants
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {


                // Add new Participant
                incomingParticipant.Id = 0;


                // Check which future dates user cant book.
                // Only applies to DAY_OF_MONTH and DAY_OF_WEEK
                if (incomingParticipant.TrainGroupDateId != null)
                {
                    List<TrainGroupParticipantUnavailableDate> futureUnavailableDates =
                    dbContext.Set<TrainGroupParticipant>()
                        .Where(x => x.TrainGroupId == updateDto.TrainGroupId)
                        .Where(x => x.SelectedDate != null)
                        .Where(x => x.SelectedDate >= DateTime.UtcNow)
                        .Where(x =>
                            x.TrainGroup.MaxParticipants -
                            (
                                x.TrainGroup.TrainGroupParticipants

                                    .Where(y =>
                                        y.TrainGroupDate!.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                                        || y.TrainGroupDate!.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
                                        || y.SelectedDate != null
                                    )
                                    .Where(y =>
                                        (y.TrainGroupDate!.RecurrenceDayOfMonth.HasValue && y.TrainGroupDate!.RecurrenceDayOfMonth.Value.Day == x.SelectedDate!.Value.Day)
                                        || (y.TrainGroupDate!.RecurrenceDayOfWeek.HasValue && y.TrainGroupDate!.RecurrenceDayOfWeek.Value.DayOfWeek == x.SelectedDate!.Value.DayOfWeek)
                                        || (y.SelectedDate!.Value== x.SelectedDate!.Value)
                                    )
                                    .Count()
                                )
                                <= 0
                        )
                        .ToList()
                        .Select(x => new TrainGroupParticipantUnavailableDate() { UnavailableDate = x.SelectedDate!.Value })
                        .ToList();

             

                    incomingParticipant.TrainGroupParticipantUnavailableDates = futureUnavailableDates;
                }

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
            List<string> errorList = new List<string>();
            List<TrainGroupDate> trainGroupDates = new List<TrainGroupDate>();
            trainGroupDates = _dataService.TrainGroupDates
             .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
             .ToList();

            if (entityDto.SelectedDate != null)
            {
                // Check for TrainGroup participant selected date validity
                bool isTrainGroupParticipantValid = !trainGroupDates
                    .Any(x =>
                        x.FixedDay == entityDto.SelectedDate
                        || (x.RecurrenceDayOfMonth != null && x.RecurrenceDayOfMonth.Value.Day == entityDto.SelectedDate!.Value.Day)
                        || (x.RecurrenceDayOfWeek != null && x.RecurrenceDayOfWeek.Value.DayOfWeek == entityDto.SelectedDate!.Value.DayOfWeek)
                    );

                if (isTrainGroupParticipantValid)
                    errorList.Add("Participant selected date doesnt match any of the Train Group Dates!");


                // Validate TrainGroup participant selected date overlap with FixedDate 
                bool isTrainGroupParticipantOverlapping = trainGroupDates.Any(x => x.FixedDay == entityDto.SelectedDate);
                if (isTrainGroupParticipantOverlapping)
                    errorList.Add("FixedDate doesnt allow Train Group Participants, please add via Train Group DATE Participants!");
            }


            // Validate if user has already joined TrainGroupDate
            bool isAlreadyParticipant = _dataService.TrainGroupParticipants
                  .Where(x => x.UserId == new Guid(entityDto.UserId))
                  .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
                  .Where(x =>
                    x.SelectedDate == null
                    ||
                    (
                        x.SelectedDate.Value.Year >= DateTime.UtcNow.Year
                        && x.SelectedDate.Value.Month >= DateTime.UtcNow.Month
                        && x.SelectedDate.Value.Day >= DateTime.UtcNow.Day
                    )
                  )
                  .Any(x =>
                      (x.TrainGroupDate != null && entityDto.SelectedDate != null && x.SelectedDate == entityDto.SelectedDate)

                      // Validate incoming TrainGroupDateParticipant vs TrainGroupParticipant
                      // If TrainGroupParticipant exists with selected date greater than current date, check if incoming TrainGroupDateParticipant overlaps
                      || (x.SelectedDate != null && x.TrainGroup.TrainGroupDates.Any(y => y.Id == entityDto.TrainGroupDateId && y.RecurrenceDayOfWeek != null && x.SelectedDate.Value.DayOfWeek == y.RecurrenceDayOfWeek.Value.DayOfWeek))
                      || (x.SelectedDate != null && x.TrainGroup.TrainGroupDates.Any(y => y.Id == entityDto.TrainGroupDateId && y.RecurrenceDayOfMonth != null && x.SelectedDate.Value.Day == y.RecurrenceDayOfMonth.Value.Day))

                      // Validate incoming SelectedDate vs FixedDay, RecurrenceDayOfWeek, RecurrenceDayOfMonth 
                      || (x.TrainGroupDate != null && entityDto.SelectedDate != null && x.TrainGroupDate.FixedDay != null && x.TrainGroupDate.FixedDay.Value == entityDto.SelectedDate.Value)
                      || (x.TrainGroupDate != null && entityDto.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfWeek != null && x.TrainGroupDate.RecurrenceDayOfWeek.Value.DayOfWeek == entityDto.SelectedDate.Value.DayOfWeek)
                      || (x.TrainGroupDate != null && entityDto.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfMonth != null && x.TrainGroupDate.RecurrenceDayOfMonth.Value.Day == entityDto.SelectedDate.Value.Day)
                  );


            if (isAlreadyParticipant)
                errorList.Add("Participant already joined!");

            errors = errorList.ToArray();
            return errors.Length > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupParticipantDto entityDto, out string[] errors)
        {
            List<string> errorList = new List<string>();
            List<TrainGroupDate> trainGroupDates = new List<TrainGroupDate>();
            trainGroupDates = _dataService.TrainGroupDates
                .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
                .ToList();

            if (entityDto.SelectedDate != null)
            {
                // Check for TrainGroup participant selected date validity
                bool isTrainGroupParticipantValid = !trainGroupDates
                    .Any(x =>
                        x.FixedDay == entityDto.SelectedDate
                        || (x.RecurrenceDayOfMonth != null && x.RecurrenceDayOfMonth.Value.Day == entityDto.SelectedDate!.Value.Day)
                        || (x.RecurrenceDayOfWeek != null && x.RecurrenceDayOfWeek.Value.DayOfWeek == entityDto.SelectedDate!.Value.DayOfWeek)
                    );

                if (isTrainGroupParticipantValid)
                    errorList.Add("Participant selected date doesnt match any of the Train Group Dates!");


                // Validate TrainGroup participant selected date overlap with FixedDate 
                bool isTrainGroupParticipantOverlapping = trainGroupDates.Any(x => x.FixedDay == entityDto.SelectedDate);
                if (isTrainGroupParticipantOverlapping)
                    errorList.Add("FixedDate doesnt allow Train Group Participants, please add via Train Group DATE Participants!");
            }



            // Validate if user has already joined TrainGroupDate
            bool isAlreadyParticipant = _dataService.TrainGroupParticipants
                  .Where(x => x.UserId == new Guid(entityDto.UserId))
                  .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
                  .Where(x => x.Id != entityDto.Id)
                  .Where(x =>
                    x.SelectedDate == null
                    ||
                    (
                        x.SelectedDate.Value.Year >= DateTime.UtcNow.Year
                        && x.SelectedDate.Value.Month >= DateTime.UtcNow.Month
                        && x.SelectedDate.Value.Day >= DateTime.UtcNow.Day
                    )
                  )
                  .Any(x =>
                      x.SelectedDate == entityDto.SelectedDate

                      // Validate incoming Reccurence TrainGroupDate vs SelectedDate 
                      // If TrainGroupParticipant exists with selected date greater than current date, check if incoming TrainGroupDateParticipant overlaps
                      || (x.SelectedDate != null && x.TrainGroup.TrainGroupDates.Any(y => y.Id == entityDto.TrainGroupDateId && y.RecurrenceDayOfWeek != null && x.SelectedDate.Value.DayOfWeek == y.RecurrenceDayOfWeek.Value.DayOfWeek))
                      || (x.SelectedDate != null && x.TrainGroup.TrainGroupDates.Any(y => y.Id == entityDto.TrainGroupDateId && y.RecurrenceDayOfMonth != null && x.SelectedDate.Value.Day == y.RecurrenceDayOfMonth.Value.Day))

                      // Validate incoming SelectedDate vs FixedDay, RecurrenceDayOfWeek, RecurrenceDayOfMonth 
                      || (x.TrainGroupDate != null && x.TrainGroupDate.FixedDay != null && entityDto.SelectedDate != null && x.TrainGroupDate.FixedDay.Value == entityDto.SelectedDate.Value)
                      || (x.TrainGroupDate != null && x.TrainGroupDate.RecurrenceDayOfWeek != null && entityDto.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfWeek.Value.DayOfWeek == entityDto.SelectedDate.Value.DayOfWeek)
                      || (x.TrainGroupDate != null && x.TrainGroupDate.RecurrenceDayOfMonth != null && entityDto.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfMonth.Value.Day == entityDto.SelectedDate.Value.Day)
                  );

            if (isAlreadyParticipant)
                errorList.Add("Participant already joined!");

            errors = errorList.ToArray();
            return errors.Length > 0;
        }
    }
}
