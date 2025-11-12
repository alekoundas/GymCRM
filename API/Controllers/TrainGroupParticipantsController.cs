using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.TrainGroup;
using Core.Enums;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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
            List<TrainGroupParticipant> emailDatesAdd = new List<TrainGroupParticipant>();
            List<TrainGroupParticipant> emailDatesRemove = new List<TrainGroupParticipant>();

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
            var customerParticipants = existingParticipants
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .ToList(); // Create a copy to avoid modifying collection during iteration

            foreach (TrainGroupParticipant existingParticipant in customerParticipants.ToList())
            {
                TrainGroupParticipant? incomingParticipant = incomingParticipants
                    .FirstOrDefault(x =>
                        x.TrainGroupDateId == existingParticipant.TrainGroupDateId &&
                        x.SelectedDate == existingParticipant.SelectedDate);

                // Remove unchanged incoming Participants
                if (incomingParticipant != null)
                    incomingParticipants.Remove(incomingParticipant);

                // Remove deleted existing Participants except if selected date is not existant in reccuring date.
                else if (existingParticipant.SelectedDate != null && existingParticipant.SelectedDate.Value != updateDto.SelectedDate)
                {
                    List<TrainGroupParticipant> tempIncomingParticipants = _mapper.Map<List<TrainGroupParticipant>>(updateDto.TrainGroupParticipantDtos);

                    bool hasAddedReccuringDate = tempIncomingParticipants
                        .Where(x => x.TrainGroupDateId == existingParticipant.TrainGroupDateId)
                        .Any(x => x.SelectedDate == null);

                    var incomingDateIds = _mapper.Map<List<TrainGroupParticipant>>(updateDto.TrainGroupParticipantDtos).Select(y => y.TrainGroupDateId).ToList();

                    bool hasReccuringDayOfWeek = existingEntity.TrainGroupDates
                        .Where(x => incomingDateIds.Contains(x.Id))
                        .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                        .Where(x => x.RecurrenceDayOfWeek == existingParticipant.SelectedDate.Value.DayOfWeek)
                        .Any(x => x.TrainGroupParticipants.Any(y => y.UserId.ToString() == updateDto.UserId && y.SelectedDate == null));

                    bool hasReccuringDayOfMonth = existingEntity.TrainGroupDates
                        .Where(x => incomingDateIds.Contains(x.Id))
                        .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                        .Where(x => x.RecurrenceDayOfMonth == existingParticipant.SelectedDate.Value.Day)
                        .Any(x => x.TrainGroupParticipants.Any(y => y.UserId.ToString() == updateDto.UserId && y.SelectedDate == null));

                    if (hasReccuringDayOfWeek || hasReccuringDayOfMonth || hasAddedReccuringDate)
                    {
                        existingParticipants.Remove(existingParticipant);
                        dbContext.Remove(existingParticipant);
                        emailDatesRemove.Add(existingParticipant);
                    }
                }
                // Remove deleted existing Participants
                else
                {
                    existingParticipants.Remove(existingParticipant);
                    dbContext.Remove(existingParticipant);
                    emailDatesRemove.Add(existingParticipant);
                }
            }

            // 12-HOUR REMOVAL VALIDATION
            // Check each removal before processing
            var tempParticipants = customerParticipants.Where(x => x.SelectedDate == null ? true : x.SelectedDate == updateDto.SelectedDate).ToList();
            foreach (TrainGroupParticipant existingParticipant in tempParticipants) // ToList() to copy for safety
            {
                TrainGroupParticipant? incomingParticipant = _mapper.Map<List<TrainGroupParticipant>>(updateDto.TrainGroupParticipantDtos)
                   .FirstOrDefault(x =>
                       x.TrainGroupDateId == existingParticipant.TrainGroupDateId &&
                       x.SelectedDate == existingParticipant.SelectedDate);

                if (incomingParticipant == null)
                {
                    DateTime slotStartUtc;
                    int offsetMin = updateDto.ClientTimezoneOffsetMinutes; // From client: new Date().getTimezoneOffset()
                    double offsetH = -(offsetMin / 60.0);
                    DateTime nowUtc = DateTime.UtcNow;

                    if (existingParticipant.SelectedDate.HasValue)
                    {
                        // One-off: Use stored full UTC datetime (assumes time is set as in frontend)
                        slotStartUtc = existingParticipant.SelectedDate.Value;
                        slotStartUtc = slotStartUtc.AddHours(existingParticipant.TrainGroup.StartOn.Hour);
                        slotStartUtc = slotStartUtc.AddMinutes(existingParticipant.TrainGroup.StartOn.Minute);
                    }
                    else
                    {
                        // Recurring: Compute next upcoming occurrence with local time overlaid
                        slotStartUtc = CalculateNextOccurrenceDateTime(existingParticipant.TrainGroupDate, nowUtc);
                    }

                    if (slotStartUtc > nowUtc.AddHours(offsetH) && slotStartUtc <= nowUtc.AddHours(offsetH).AddHours(12))
                    {
                        dbContext.Dispose();
                        return new ApiResponse<TrainGroup>().SetErrorResponse(_localizer[TranslationKeys.Cannot_remove_a_session_starting_within_12_hours]); // Add key to TranslationKeys
                    }
                }
            }

            // Validate if any of the incoming participants is already joined.
            // Check if any of the incoming participants contains selected date, then check for recurring dates.
            //bool isAlreadyParticipant = incomingParticipants.Any(x =>
            //    existingParticipants
            //        .Where(x => x.UserId == new Guid(updateDto.UserId))
            //        .Any(y => x.TrainGroupDateId == y.TrainGroupDateId)
            //);

            //if (isAlreadyParticipant)
            //{
            //    dbContext.Dispose();
            //    return new ApiResponse<TrainGroup>().SetErrorResponse(_localizer[TranslationKeys.Participant_already_joined]);
            //}


            // Validate max participants
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {
                int numberOfParticipants = 0;


                numberOfParticipants = existingParticipants
                    .Where(x => x.TrainGroupDateId == incomingParticipant.TrainGroupDateId)
                    .Where(x => x.SelectedDate == null || x.SelectedDate == updateDto.SelectedDate)
                    .Where(x => !x.TrainGroupParticipantUnavailableDates.Any(y =>
                        y.UnavailableDate == x.TrainGroupDate.FixedDay
                        || y.UnavailableDate.Day == x.TrainGroupDate.RecurrenceDayOfMonth
                        || y.UnavailableDate.DayOfWeek == x.TrainGroupDate.RecurrenceDayOfWeek))
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
                emailDatesAdd.Add(incomingParticipant);
            }


            await dbContext.SaveChangesAsync();
            dbContext.Dispose();

            User? user = _dataService.Users.Where(x => x.Id == new Guid(updateDto.UserId))
                .FirstOrDefault();

            if (user != null)
            {
                await _emailService.SendBookingEmailAsync(
                    user,
                    emailDatesAdd,
                    emailDatesRemove
                );
            }


            //if (futureUnavailableDates.Count > 0)
            //    return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity, "warning", "Future unavailable dates that cannot be booked: " + futureUnavailableDates.Select(x => x.UnavailableDate.ToUniversalTime().ToString()).ToList().ToString());

            return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity);
        }

        // Computes next occurrence datetime in UTC
        private DateTime CalculateNextOccurrenceDateTime(TrainGroupDate trainGroupDate, DateTime nowUtc)
        {
            DateTime nextLocalDate;
            switch (trainGroupDate.TrainGroupDateType)
            {
                case TrainGroupDateTypeEnum.DAY_OF_WEEK:
                    // Assume RecurrenceDayOfWeek is DayOfWeek enum or convertible
                    DayOfWeek targetDow = Enum.Parse<DayOfWeek>(trainGroupDate.RecurrenceDayOfWeek.ToString());
                    int inputDowNum = (int)nowUtc.DayOfWeek;
                    int targetDowNum = (int)targetDow;
                    int daysToAdd = (targetDowNum - inputDowNum + 7) % 7;
                    if (daysToAdd == 0) daysToAdd = 7; // Next occurrence if today (but adjust if same-day allowed; here next for safety)
                    nextLocalDate = nowUtc.Date.AddDays(daysToAdd);
                    break;
                case TrainGroupDateTypeEnum.DAY_OF_MONTH:
                    int targetDay = trainGroupDate.RecurrenceDayOfMonth ?? throw new ArgumentException("Missing RecurrenceDayOfMonth");
                    int year = nowUtc.Year;
                    int month = nowUtc.Month;
                    if (nowUtc.Day > targetDay)
                    {
                        month++;
                        if (month > 12) { month = 1; year++; }
                    }
                    nextLocalDate = new DateTime(year, month, targetDay, 0, 0, 0);
                    // JS/.NET auto-rollover for invalid (e.g., Jan 31 -> Feb 3? Wait, new Date rolls to next month)
                    break;
                case TrainGroupDateTypeEnum.FIXED_DAY:
                    nextLocalDate = trainGroupDate.FixedDay!.Value;
                    break;
                default:
                    throw new ArgumentException($"Unsupported TrainGroupDateType: {trainGroupDate.TrainGroupDateType}");
            }

            DateTime localStart = nextLocalDate.AddHours(trainGroupDate.TrainGroup.StartOn.Hour);
            localStart = localStart.AddMinutes(trainGroupDate.TrainGroup.StartOn.Minute);

            return localStart;
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
            query = query.Include(x => x.User).ThenInclude<User, UserStatus>(x => x.UserStatus!);


            List<DataTableFilterDto> customFilters = dataTable.Filters.Where(x => x.FilterType == DataTableFiltersEnum.custom).ToList();

            if (customFilters.Any())
            {
                DataTableFilterDto? filter = customFilters.FirstOrDefault(x => x.FieldName == "ParticipantGridSelectedDate");
                if (filter != null)
                {
                    DateTime selectedDate = DateTime.Parse(filter?.Value!);
                    query = query.Where(x =>
                        x.SelectedDate!=null?
                            (x.SelectedDate == selectedDate)
                        :
                            (x.TrainGroupDate.FixedDay == selectedDate 
                            ||x.TrainGroupDate.RecurrenceDayOfWeek == selectedDate.DayOfWeek 
                            ||x.TrainGroupDate.RecurrenceDayOfMonth == selectedDate.Month
                            )
                    );
                }
            }
        }
    }
}
