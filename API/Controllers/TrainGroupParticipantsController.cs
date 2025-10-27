using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Mail;
using Core.Enums;
using Core.Models;
using Core.System;
using Core.Translations;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq.Dynamic.Core.Tokenizer;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupParticipantsController : GenericController<TrainGroupParticipant, TrainGroupParticipantDto, TrainGroupParticipantAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;
        private readonly IEmailService _emailService;

        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupParticipantsController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer,
            IEmailService emailService) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
            _emailService = emailService;
        }


        // Handle Booking. 
        // PUT: api/controller/5
        [HttpPost("UpdateParticipants")]
        public async Task<ActionResult<ApiResponse<TrainGroup>>> UpdateParticipants([FromBody] TrainGroupParticipantUpdateDto updateDto)
        {

            ApiDbContext dbContext = _dataService.GetDbContext();
            List<string> emailDatesAdd = new List<string>();
            List<string> emailDatesRemove = new List<string>();

            // Load existing entity with related data
            TrainGroup? existingEntity = await dbContext.Set<TrainGroup>()
                .Include(x => x.TrainGroupParticipants)
                .Include(x => x.TrainGroupDates)
                .ThenInclude(x => x.TrainGroupParticipants)
                .ThenInclude(x => x.TrainGroupParticipantUnavailableDates)
                .Where(x => x.Id == updateDto.TrainGroupId)
                .FirstOrDefaultAsync();

            if (existingEntity == null)
            {
                string className = typeof(TrainGroup).Name;
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, className]);
            }


            // Validate user selection.
            // Cant have curent date selected with a recurring date be the same date.
            bool isCurrentDateWithConflict = updateDto.TrainGroupParticipantDtos
                .Where(x => x.SelectedDate != null)
                .Any(x => updateDto.TrainGroupParticipantDtos
                    .Where(y => y.SelectedDate == null)
                    .Any(y => existingEntity.TrainGroupDates
                        .Where(z => z.Id == y.TrainGroupDateId)
                        .Any(z => z.RecurrenceDayOfMonth == x.SelectedDate?.Day || z.RecurrenceDayOfWeek == x.SelectedDate?.DayOfWeek)
                    )
                );

            if (isCurrentDateWithConflict)
            {
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetErrorResponse(_localizer[TranslationKeys.Current_date_is_already_selected_in_a_Recurrence_date]);
            }


            List<TrainGroupParticipant> incomingParticipants = _mapper.Map<List<TrainGroupParticipant>>(updateDto.TrainGroupParticipantDtos);
            List<TrainGroupParticipant> existingParticipants = existingEntity
                .TrainGroupParticipants
                .Where(x => x.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY ? x.TrainGroupDate.FixedDay == updateDto.SelectedDate : true)
                .ToList();



            // If selected date exists, assign TrainGroupDateId.
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
                if (incomingParticipant.SelectedDate.HasValue && incomingParticipant.TrainGroupDateId == -1)
                {
                    List<TrainGroupDate> selectedTrainGroupDate = existingEntity
                            .TrainGroupDates
                            .Where(x =>
                                x.FixedDay == updateDto.SelectedDate ||
                                x.RecurrenceDayOfMonth == updateDto.SelectedDate.Day ||
                                x.RecurrenceDayOfWeek == updateDto.SelectedDate.DayOfWeek)
                            .ToList();

                    if (selectedTrainGroupDate.Count() == 1)
                        incomingParticipant.TrainGroupDateId = selectedTrainGroupDate.First().Id;
                    else
                    {
                        dbContext.Dispose();
                        return new ApiResponse<TrainGroup>().SetErrorResponse($"Something unexpected happend! Please contact Administrator.");
                    }
                }


            // Handle Deletions and Unchanged participants.
            var participantsToRemove = existingParticipants
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .ToList(); // Create a copy to avoid modifying collection during iteration

            foreach (TrainGroupParticipant existingParticipant in participantsToRemove)
            {
                TrainGroupParticipant? incomingParticipant = incomingParticipants
                    .FirstOrDefault(x =>
                        x.TrainGroupDateId == existingParticipant.TrainGroupDateId &&
                        x.SelectedDate == existingParticipant.SelectedDate);

                // Remove unchanged incoming Participants
                if (incomingParticipant != null)
                    incomingParticipants.Remove(incomingParticipant);
                // Remove deleted existing Participants
                else
                {
                    existingParticipants.Remove(existingParticipant);
                    dbContext.Remove(existingParticipant);

                    string? dateString = existingParticipant.TrainGroupDate.RecurrenceDayOfMonth.ToString() ??
                        existingParticipant.TrainGroupDate.RecurrenceDayOfWeek.ToString() ??
                        existingParticipant.TrainGroupDate.FixedDay.ToString();

                    if (dateString != null)
                        emailDatesRemove.Add(dateString);
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
                return new ApiResponse<TrainGroup>().SetErrorResponse(_localizer[TranslationKeys.Participant_already_joined]);
            }


            // Validate max participants
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {
                int numberOfParticipants = 0;

                if (incomingParticipant.SelectedDate == null)
                    numberOfParticipants = existingParticipants
                    .Where(x => incomingParticipant.TrainGroupDateId == x.TrainGroupDateId)
                    //.Where(x => !x.TrainGroupParticipantUnavailableDates.Any(y =>
                    //    y.UnavailableDate == x.TrainGroupDate.FixedDay ||
                    //    y.UnavailableDate.Day == x.TrainGroupDate.RecurrenceDayOfMonth ||
                    //    y.UnavailableDate.DayOfWeek == x.TrainGroupDate.RecurrenceDayOfWeek))
                    .ToList()
                    .Count();

                else
                    numberOfParticipants = existingParticipants
                    .Where(x => incomingParticipant.TrainGroupDateId == x.TrainGroupDateId)
                    //.Where(x => incomingParticipant.SelectedDate == null ?  true: false) // If SelectedDate is not set, check only for non one-off participants
                    .Where(x => !x.TrainGroupParticipantUnavailableDates.Any(y =>
                        y.UnavailableDate == x.TrainGroupDate.FixedDay ||
                        y.UnavailableDate.Day == x.TrainGroupDate.RecurrenceDayOfMonth ||
                        y.UnavailableDate.DayOfWeek == x.TrainGroupDate.RecurrenceDayOfWeek))
                    .ToList()
                    .Count();

                if (numberOfParticipants >= existingEntity.MaxParticipants)
                {
                    dbContext.Dispose();
                    return new ApiResponse<TrainGroup>().SetErrorResponse(_localizer[TranslationKeys.Maximum_amount_of_participants_has_been_reached]);
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
                                    y.TrainGroupDate.RecurrenceDayOfMonth == x.SelectedDate!.Value.Day ||
                                    y.TrainGroupDate.RecurrenceDayOfWeek == x.SelectedDate!.Value.DayOfWeek ||
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

                string? dateString = incomingParticipant.TrainGroupDate.RecurrenceDayOfMonth.ToString() ??
                        incomingParticipant.TrainGroupDate.RecurrenceDayOfWeek.ToString() ??
                        incomingParticipant.TrainGroupDate.FixedDay.ToString();

                if (dateString != null)
                    emailDatesAdd.Add(dateString);
            }


            await dbContext.SaveChangesAsync();
            dbContext.Dispose();

            User? user = _dataService.Users.Where(x => x.Id == new Guid(updateDto.UserId))
                .FirstOrDefault();

            if (user !=null)
            {
                await _emailService.SendBookingEmailAsync(
                    user.Email,
                    emailDatesAdd,
                    emailDatesRemove
                );
            }


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
                    x.RecurrenceDayOfMonth == participantDto.SelectedDate.Value.Day ||
                    x.RecurrenceDayOfWeek == participantDto.SelectedDate.Value.DayOfWeek);

                if (!isDateValid)
                    errorList.Add(_localizer[TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates]);

                // Check for overlap with FixedDate
                if (trainGroupDates.Any(x => x.FixedDay == participantDto.SelectedDate))
                    errorList.Add(_localizer[TranslationKeys.Fixed_date_doesnt_allow_one_off_participants]);
            }

            // Validate if user is already a participant
            bool isAlreadyParticipant = _dataService.TrainGroupParticipants
                .Where(x => x.UserId == participantDto.UserId)
                .Where(x => x.TrainGroupId == participantDto.TrainGroupId)
                .Where(x => excludeParticipantId == null || x.Id != excludeParticipantId) // Used in PUT
                .Where(x => x.SelectedDate == null || x.SelectedDate.Value.Date >= DateTime.UtcNow.Date)
                .Any(x => x.TrainGroupDateId == participantDto.TrainGroupDateId);


            if (isAlreadyParticipant)
                errorList.Add(_localizer[TranslationKeys.Participant_already_joined]);

            return errorList.ToArray();
        }


        protected override void DataTableQueryUpdate(IGenericRepository<TrainGroupParticipant> query, DataTableDto<TrainGroupParticipantDto> dataTable)
        {
            query = query.Include(x => x.User);


            List<DataTableFilterDto> customFilters = dataTable.Filters.Where(x => x.FilterType == DataTableFiltersEnum.custom).ToList();

            if (customFilters.Any())
            {
                DataTableFilterDto? filter = customFilters.FirstOrDefault(x => x.FieldName == "ParticipantGridSelectedDate");
                if (filter != null)
                {
                    DateTime selectedDate = DateTime.Parse(filter?.Value!);
                    query = query.Where(x =>
                        x.SelectedDate == selectedDate ||
                        x.TrainGroupDate.FixedDay == selectedDate ||
                        x.TrainGroupDate.RecurrenceDayOfWeek == selectedDate.DayOfWeek ||
                        x.TrainGroupDate.RecurrenceDayOfMonth == selectedDate.Month);
                }
            }
        }
    }
}
