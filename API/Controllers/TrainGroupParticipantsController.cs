using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Enums;
using Core.Models;
using Core.Translations;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Globalization;

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

        private readonly ISubscriptionService _subscriptionService;

        public TrainGroupParticipantsController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer,
            IEmailService emailService,
            ISubscriptionService subscriptionService) : base(dataService, mapper, localizer)
        {
            _subscriptionService = subscriptionService;
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
            _emailService = emailService;
        }


        // POST: api/TrainGroupParticipants/Book
        // Adding only. Each ticked option becomes a row of its own, and all of them go in
        // or none do. Returns the dates a recurring booking could not have because they
        // were already full - those are saved as skipped dates the member can rejoin
        // from the profile calendar if a place opens.
        [HttpPost("Book")]
        public async Task<ActionResult<ApiResponse<List<DateTime>>>> Book([FromBody] TrainGroupParticipantBookDto dto)
        {
            Guid? callerId = GetCallerId();
            if (callerId == null)
                return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));

            Guid userId = callerId.Value;
            if (!string.IsNullOrWhiteSpace(dto.UserId))
            {
                if (!Guid.TryParse(dto.UserId, out Guid requestedId))
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Invalid_data_provided]));

                if (requestedId != callerId.Value && !IsStaff())
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));

                userId = requestedId;
            }

            if (!dto.IsOneOff && dto.RecurringTrainGroupDateIds.Count == 0)
                return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Select_at_least_one_date_to_book]));

            DateTime day = dto.SelectedDate.Date;
            DateTime clientNow = BookingRules.ClientNow(dto.ClientTimezoneOffsetMinutes);

            using ApiDbContext context = _dataService.GetDbContext();

            TrainGroup? trainGroup = await context.TrainGroups
                .Include(x => x.TrainGroupDates)
                .Include(x => x.TrainGroupUnavailableDates)
                .Include(x => x.TrainGroupParticipants)
                .ThenInclude(x => x.TrainGroupParticipantUnavailableDates)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == dto.TrainGroupId);

            if (trainGroup == null)
                return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, nameof(TrainGroup)]));

            // The group date that runs on the day the member picked.
            TrainGroupDate? sessionDate =
                trainGroup.TrainGroupDates.FirstOrDefault(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && BookingRules.OccursOn(x, day))
                ?? trainGroup.TrainGroupDates.FirstOrDefault(x => x.TrainGroupDateType != TrainGroupDateTypeEnum.FIXED_DAY && BookingRules.OccursOn(x, day));

            if (sessionDate == null)
                return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates]));

            if (BookingRules.SessionStart(trainGroup, day) <= clientNow)
                return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.This_session_has_already_started]));

            List<TrainGroupDate> recurringDates = new List<TrainGroupDate>();
            foreach (int trainGroupDateId in dto.RecurringTrainGroupDateIds.Distinct())
            {
                TrainGroupDate? trainGroupDate = trainGroup.TrainGroupDates
                    .FirstOrDefault(x => x.Id == trainGroupDateId && x.TrainGroupDateType != TrainGroupDateTypeEnum.FIXED_DAY);

                if (trainGroupDate == null)
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Invalid_data_provided]));

                recurringDates.Add(trainGroupDate);
            }

            // The recurring booking already covers the picked day.
            if (dto.IsOneOff && recurringDates.Any(x => x.Id == sessionDate.Id))
                return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Current_date_is_already_selected_in_a_Recurrence_date]));

            string callerName = await GetCallerFullNameAsync(context);
            List<TrainGroupParticipant> members = trainGroup.TrainGroupParticipants.ToList();
            List<TrainGroupParticipant> mine = members.Where(x => x.UserId == userId).ToList();

            // Everything the member holds in other groups, for the same-hour clash check.
            List<TrainGroupParticipant> elsewhere = await context.TrainGroupParticipants
                .Include(x => x.TrainGroup)
                .Include(x => x.TrainGroupDate)
                .Include(x => x.TrainGroupParticipantUnavailableDates)
                .Where(x => x.UserId == userId && x.TrainGroupId != trainGroup.Id)
                .AsSplitQuery()
                .ToListAsync();

            TimeSpan startTime = trainGroup.StartOn.TimeOfDay;
            bool ClashesOn(DateTime date) => elsewhere.Any(x => x.TrainGroup.StartOn.TimeOfDay == startTime && BookingRules.HoldsPlace(x, date));

            List<TrainGroupParticipant> added = new List<TrainGroupParticipant>();
            List<TrainGroupParticipant> absorbed = new List<TrainGroupParticipant>();
            List<DateTime> fullDates = new List<DateTime>();

            if (dto.IsOneOff)
            {
                if (trainGroup.TrainGroupUnavailableDates.Any(x => x.UnavailableDate.Date == day))
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.This_session_has_been_cancelled_by_the_gym]));

                if (mine.Any(x => BookingRules.IsBooked(x, day)))
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Participant_already_joined]));

                // A single session is refused outright when it is full. Only a recurring
                // booking goes ahead around the full dates.
                if (members.Count(x => BookingRules.HoldsPlace(x, day)) >= trainGroup.MaxParticipants)
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Maximum_amount_of_participants_has_been_reached]));

                if (ClashesOn(day))
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.User_already_has_a_booking_for_the_same_time_and_date_on_a_different_train_group]));

                // A fixed-day group is booked through its date, with no selected date of its own.
                bool isFixedDay = sessionDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY;
                added.Add(new TrainGroupParticipant()
                {
                    SelectedDate = isFixedDay ? null : DateTime.SpecifyKind(day, DateTimeKind.Utc),
                    TrainGroupDateId = sessionDate.Id,
                    TrainGroupDate = sessionDate,
                    TrainGroupId = trainGroup.Id,
                    TrainGroup = trainGroup,
                    UserId = userId,
                    CreatedBy_Id = callerId.Value.ToString(),
                    CreatedBy_FullName = callerName
                });
            }

            foreach (TrainGroupDate trainGroupDate in recurringDates)
            {
                DateTime? firstDay = BookingRules.NextOccurrence(trainGroupDate, day);
                if (firstDay == null)
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Invalid_data_provided]));

                DateTime start = firstDay.Value;

                // One recurring booking per day of a group at a time. An old one that has
                // ended stays, and a new one may start once it has.
                bool overlaps = mine.Any(x =>
                    x.SelectedDate == null
                    && x.TrainGroupDateId == trainGroupDate.Id
                    && (x.RecurringEndOnDate == null || x.RecurringEndOnDate.Value.Date > start));

                if (overlaps)
                    return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.Participant_already_joined]));

                // The member's own one-offs on this day of the group from here on are now
                // covered by the recurring booking, so they are cancelled rather than left
                // to hold a second place on the same date.
                foreach (TrainGroupParticipant oneOff in mine.Where(x =>
                    x.SelectedDate != null
                    && x.RemovedOn == null
                    && x.TrainGroupDateId == trainGroupDate.Id
                    && x.SelectedDate.Value.Date >= start))
                {
                    MarkRemoved(oneOff, callerName);
                    absorbed.Add(oneOff);
                }

                TrainGroupParticipant booking = new TrainGroupParticipant()
                {
                    SelectedDate = null,
                    RecurringStartOnDate = DateTime.SpecifyKind(start, DateTimeKind.Utc),
                    TrainGroupDateId = trainGroupDate.Id,
                    TrainGroupDate = trainGroupDate,
                    TrainGroupId = trainGroup.Id,
                    TrainGroup = trainGroup,
                    UserId = userId,
                    CreatedBy_Id = callerId.Value.ToString(),
                    CreatedBy_FullName = callerName
                };

                // Every change to how full this day of the group is happens on a date we
                // know - somebody's one-off, start, end or skip. After the last of them the
                // count stays the same for good, so the dates up to there, plus one past it,
                // are all that need looking at.
                DateTime lastChange = LastChangeOnOrAfter(members, start);
                DateTime lastClashChange = LastChangeOnOrAfter(elsewhere, start);
                DateTime horizon = lastChange > lastClashChange ? lastChange : lastClashChange;

                DateTime? occurrence = start;
                for (int i = 0; i < 1000 && occurrence != null; i++)
                {
                    DateTime date = occurrence.Value;
                    bool isPastLastChange = date > horizon;

                    if (ClashesOn(date))
                        return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.User_already_has_a_booking_for_the_same_time_and_date_on_a_different_train_group]));

                    bool isFull = members.Count(x => BookingRules.HoldsPlace(x, date)) >= trainGroup.MaxParticipants;
                    bool isCancelledByGym = trainGroup.TrainGroupUnavailableDates.Any(x => x.UnavailableDate.Date == date);

                    // Full from here to forever: a booking that would be skipped on every
                    // date is no booking.
                    if (isFull && isPastLastChange)
                        return BadRequest(new ApiResponse<List<DateTime>>().SetErrorResponse(_localizer[TranslationKeys.This_session_is_full_on_every_upcoming_date]));

                    if (isFull && !isCancelledByGym)
                    {
                        booking.TrainGroupParticipantUnavailableDates.Add(new TrainGroupParticipantUnavailableDate()
                        {
                            UnavailableDate = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                            CreatedBy_Id = callerId.Value.ToString(),
                            CreatedBy_FullName = callerName
                        });
                        fullDates.Add(date);
                    }

                    if (isPastLastChange)
                        break;

                    occurrence = BookingRules.NextOccurrence(trainGroupDate, date.AddDays(1));
                }

                members.Add(booking);
                mine.Add(booking);
                added.Add(booking);
            }

            context.TrainGroupParticipants.AddRange(added);
            await context.SaveChangesAsync();

            User? user = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            if (user != null)
                try
                {
                    await _emailService.SendBookingEmailAsync(user, added, absorbed);
                }
                catch (Exception)
                {
                    // The booking stands whether or not the mail goes.
                }

            List<DateTime> result = fullDates
                .Distinct()
                .OrderBy(x => x)
                .Select(x => DateTime.SpecifyKind(x, DateTimeKind.Utc))
                .ToList();

            return new ApiResponse<List<DateTime>>().SetSuccessResponse(result, _localizer[TranslationKeys.Booking_saved]);
        }


        // POST: api/TrainGroupParticipants/5/End
        // Stops a recurring booking from a date on. The row stays with its end date, so
        // every session before it is still on the member's calendar.
        [HttpPost("{id}/End")]
        public async Task<ActionResult<ApiResponse<bool>>> End(int id, [FromBody] TrainGroupParticipantRemoveDto dto)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            TrainGroupParticipant? participant = await LoadForChangeAsync(context, id);
            if (participant == null)
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(TrainGroupParticipant)]));

            if (!CanChange(participant))
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));

            if (!BookingRules.IsRecurring(participant))
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Invalid_data_provided]));

            DateTime clientNow = BookingRules.ClientNow(dto.ClientTimezoneOffsetMinutes);
            DateTime fromDate = (dto.FromDate ?? clientNow).Date;

            if (participant.RecurringEndOnDate != null && participant.RecurringEndOnDate.Value.Date <= fromDate)
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.This_booking_has_already_ended]));

            if (!MayBypassWindow(dto.IsAdminPage))
            {
                if (fromDate < clientNow.Date)
                    return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.This_session_has_already_started]));

                // The first session the member gives up has to be more than 12 hours off.
                DateTime? firstLost = FirstSessionOnOrAfter(participant, fromDate);
                if (firstLost != null)
                {
                    DateTime sessionStart = BookingRules.SessionStart(participant.TrainGroup, firstLost.Value);
                    if (sessionStart <= clientNow)
                        return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.This_session_has_already_started]));

                    if (sessionStart <= clientNow.AddHours(BookingRules.MemberChangeWindowHours))
                        return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Cannot_remove_a_session_starting_within_12_hours]));
                }
            }

            participant.RecurringEndOnDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            MarkRemoved(participant, await GetCallerFullNameAsync(context));
            await context.SaveChangesAsync();

            await SendRemovalEmailAsync(context, participant);

            return new ApiResponse<bool>().SetSuccessResponse(true, _localizer[TranslationKeys.Booking_removed]);
        }


        // POST: api/TrainGroupParticipants/5/Cancel
        // Cancels a one-off or a fixed-day booking. Kept, marked removed, and no longer
        // counted towards the group's size.
        [HttpPost("{id}/Cancel")]
        public async Task<ActionResult<ApiResponse<bool>>> Cancel(int id, [FromBody] TrainGroupParticipantRemoveDto dto)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            TrainGroupParticipant? participant = await LoadForChangeAsync(context, id);
            if (participant == null)
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(TrainGroupParticipant)]));

            if (!CanChange(participant))
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));

            if (BookingRules.IsRecurring(participant))
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Invalid_data_provided]));

            if (participant.RemovedOn != null)
                return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.This_booking_is_already_cancelled]));

            if (!MayBypassWindow(dto.IsAdminPage))
            {
                DateTime clientNow = BookingRules.ClientNow(dto.ClientTimezoneOffsetMinutes);
                DateTime day = (participant.SelectedDate ?? participant.TrainGroupDate.FixedDay ?? DateTime.MinValue).Date;
                DateTime sessionStart = BookingRules.SessionStart(participant.TrainGroup, day);

                if (sessionStart <= clientNow)
                    return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.This_session_has_already_started]));

                if (sessionStart <= clientNow.AddHours(BookingRules.MemberChangeWindowHours))
                    return BadRequest(new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Cannot_remove_a_session_starting_within_12_hours]));
            }

            MarkRemoved(participant, await GetCallerFullNameAsync(context));
            await context.SaveChangesAsync();

            await SendRemovalEmailAsync(context, participant);

            return new ApiResponse<bool>().SetSuccessResponse(true, _localizer[TranslationKeys.Booking_removed]);
        }


        // POST: api/TrainGroupParticipants/Bookings
        // Everything a member has booked, running, coming up or ended, for the booking
        // page's list.
        [HttpPost("Bookings")]
        public async Task<ActionResult<ApiResponse<List<TrainGroupParticipantBookingDto>>>> Bookings([FromBody] TrainGroupParticipantBookingsRequestDto dto)
        {
            Guid? callerId = GetCallerId();
            if (callerId == null)
                return BadRequest(new ApiResponse<List<TrainGroupParticipantBookingDto>>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));

            Guid userId = callerId.Value;
            if (!string.IsNullOrWhiteSpace(dto.UserId))
            {
                if (!Guid.TryParse(dto.UserId, out Guid requestedId) || (requestedId != callerId.Value && !IsStaff()))
                    return BadRequest(new ApiResponse<List<TrainGroupParticipantBookingDto>>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));

                userId = requestedId;
            }

            DateTime clientNow = BookingRules.ClientNow(dto.ClientTimezoneOffsetMinutes);

            using ApiDbContext context = _dataService.GetDbContext();

            List<TrainGroupParticipant> participants = await context.TrainGroupParticipants
                .AsNoTracking()
                .Include(x => x.TrainGroup)
                .ThenInclude(x => x.Trainer)
                .Include(x => x.TrainGroupDate)
                .Include(x => x.TrainGroupParticipantUnavailableDates)
                .Where(x => x.UserId == userId)
                .AsSplitQuery()
                .ToListAsync();

            List<TrainGroupParticipantBookingDto> result = participants
                .Select(x => ToBookingDto(x, clientNow))
                .ToList();

            return new ApiResponse<List<TrainGroupParticipantBookingDto>>().SetSuccessResponse(result);
        }


        // DELETE: api/TrainGroupParticipants/5
        // What the admin grids call. Staff taking somebody out ends the booking from today,
        // the same as the member would, so what came before stays on their calendar.
        public override async Task<ActionResult<ApiResponse<TrainGroupParticipant>>> Delete(string? id)
        {
            // Members hold the Delete claim too, for their own cancellations - which go
            // through End and Cancel. This one is for staff alone.
            if (!IsUserAuthorized("Delete") || !IsStaff())
                return new ApiResponse<TrainGroupParticipant>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            if (!int.TryParse(id, out int participantId))
                return new ApiResponse<TrainGroupParticipant>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(TrainGroupParticipant)]);

            using ApiDbContext context = _dataService.GetDbContext();

            TrainGroupParticipant? participant = await LoadForChangeAsync(context, participantId);
            if (participant == null)
                return new ApiResponse<TrainGroupParticipant>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(TrainGroupParticipant)]);

            if (BookingRules.IsRecurring(participant))
            {
                DateTime today = DateTime.UtcNow.Date;
                if (participant.RecurringEndOnDate == null || participant.RecurringEndOnDate.Value.Date > today)
                    participant.RecurringEndOnDate = DateTime.SpecifyKind(today, DateTimeKind.Utc);
            }

            MarkRemoved(participant, await GetCallerFullNameAsync(context));
            await context.SaveChangesAsync();

            // Just the row's own values - the loaded group would drag its trainer's
            // account along into the response.
            return new ApiResponse<TrainGroupParticipant>().SetSuccessResponse(
                new TrainGroupParticipant() { Id = participant.Id, TrainGroupId = participant.TrainGroupId, UserId = participant.UserId },
                _localizer[TranslationKeys.Booking_removed]);
        }


        // PUT: api/TrainGroupParticipants/5
        // Only what the admin forms edit. The generic PUT writes every column from the dto,
        // which would wipe the dates a booking ended on and the day it was created.
        public override async Task<ActionResult<ApiResponse<TrainGroupParticipant>>> Put(string? id, [FromBody] TrainGroupParticipantDto entityDto)
        {
            if (!IsUserAuthorized("Edit"))
                return new ApiResponse<TrainGroupParticipant>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            if (CustomValidatePUT(entityDto, out string[] errors))
                return BadRequest(new ApiResponse<TrainGroupParticipant>().SetErrorResponse(errors));

            if (!int.TryParse(id, out int participantId) || !Guid.TryParse(entityDto.UserId, out Guid userId))
                return new ApiResponse<TrainGroupParticipant>().SetErrorResponse(_localizer[TranslationKeys.Invalid_data_provided]);

            using ApiDbContext context = _dataService.GetDbContext();

            TrainGroupParticipant? existing = await context.TrainGroupParticipants.FirstOrDefaultAsync(x => x.Id == participantId);
            if (existing == null)
                return new ApiResponse<TrainGroupParticipant>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(TrainGroupParticipant)]);

            existing.UserId = userId;
            existing.SelectedDate = entityDto.SelectedDate;
            existing.TrainGroupDateId = entityDto.TrainGroupDateId;
            await context.SaveChangesAsync();

            return new ApiResponse<TrainGroupParticipant>().SetSuccessResponse(existing, _localizer[TranslationKeys._0_updated_successfully, nameof(TrainGroupParticipant)]);
        }


        // Added from the admin screens. A recurring one starts the day it is added - left
        // empty it would fall back to CreatedOn, which is the same day but only until the
        // row is next edited.
        protected override Task BeforeAddAsync(List<TrainGroupParticipant> entities)
        {
            DateTime today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            string callerId = GetCallerId()?.ToString() ?? string.Empty;

            foreach (TrainGroupParticipant entity in entities)
            {
                entity.CreatedBy_Id = callerId;
                if (entity.SelectedDate == null && entity.RecurringStartOnDate == null)
                    entity.RecurringStartOnDate = today;
            }

            return Task.CompletedTask;
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

            // Already booked: a one-off against whatever covers its date, a recurring one
            // against a recurring booking on the same day that is still running. Ended
            // and cancelled rows are history and do not count.
            using ApiDbContext context = _dataService.GetDbContext();

            IQueryable<TrainGroupParticipant> existing = context.TrainGroupParticipants
                .Where(x => x.UserId == participantDto.UserId)
                .Where(x => x.TrainGroupId == participantDto.TrainGroupId)
                .Where(x => x.TrainGroupDateId == participantDto.TrainGroupDateId)
                .Where(x => excludeParticipantId == null || x.Id != excludeParticipantId); // Used in PUT

            bool isAlreadyParticipant = participantDto.SelectedDate.HasValue
                ? existing.Where(BookingRules.IsBookedOn(participantDto.SelectedDate.Value)).Any()
                : existing.Where(x => x.SelectedDate == null).Where(BookingRules.IsOpenOn(DateTime.UtcNow.Date)).Any();

            if (isAlreadyParticipant)
                errorList.Add(_localizer[TranslationKeys.Participant_already_joined]);

            return errorList.ToArray();
        }


        // The date the grid was filtered by, kept so the attendance flags below can be
        // worked out for the same day the rows were chosen for.
        private DateTime? _selectedDate;

        // Attendance is stored as a full timestamp and the older rows carry a time of
        // day, so days are compared rather than instants.
        protected override async Task DataTableResultUpdate(List<TrainGroupParticipant> entities, List<TrainGroupParticipantDto> entityDtos)
        {
            if (entities.Count == 0)
                return;

            List<Guid> userIds = entities.Select(x => x.UserId).Distinct().ToList();

            // Owed lessons, whatever date the grid happens to be showing.
            Dictionary<Guid, int> balances = await _subscriptionService.GetBalancesAsync(userIds);

            for (int i = 0; i < entities.Count && i < entityDtos.Count; i++)
                if (balances.TryGetValue(entities[i].UserId, out int balance))
                    entityDtos[i].SubscriptionBalance = balance;

            // Whether attendance was taken only means something alongside a date.
            if (_selectedDate == null)
                return;

            DateTime day = _selectedDate.Value.Date;
            DateTime nextDay = day.AddDays(1);

            List<int> trainGroupIds = entities.Select(x => x.TrainGroupId).Distinct().ToList();

            using ApiDbContext context = _dataService.GetDbContext();

            List<Guid> attendedUserIds = await context.TrainGroupΑttendances
                .AsNoTracking()
                .Where(x => x.TrainGroupId != null && trainGroupIds.Contains(x.TrainGroupId.Value)
                         && userIds.Contains(x.UserId)
                         && x.AttendanceDate >= day
                         && x.AttendanceDate < nextDay)
                .Select(x => x.UserId)
                .ToListAsync();

            for (int i = 0; i < entities.Count && i < entityDtos.Count; i++)
                entityDtos[i].HasAttendance = attendedUserIds.Contains(entities[i].UserId);
        }

        protected override void DataTableQueryUpdate(IGenericRepository<TrainGroupParticipant> query, DataTableDto<TrainGroupParticipantDto> dataTable)
        {
            query = query.Include(x => x.User).ThenInclude<User, UserStatus>(x => x.UserStatus!);

            Guid? scopeUserId = GetScopeToCallerId("TrainGroupParticipants_View");
            if (scopeUserId != null)
                query = query.Where(x => x.UserId == scopeUserId.Value);


            DataTableFilterDto? filter = dataTable.Filters
                .Where(x => x.FilterType == DataTableFiltersEnum.custom)
                .FirstOrDefault(x => x.FieldName == "ParticipantGridSelectedDate");

            if (filter != null && !string.IsNullOrWhiteSpace(filter.Value))
            {
                // The page sends midnight UTC for the day it shows. Parsed without
                // RoundtripKind it would come back in the server's own zone and land on
                // a different hour, or on a different day west of Greenwich.
                DateTime selectedDate = DateTime.Parse(filter.Value, null, DateTimeStyles.RoundtripKind).Date;
                _selectedDate = selectedDate;

                // Who is in the session that day - the rule every count uses.
                query = query.Where(BookingRules.HoldsPlaceOn(selectedDate));
            }
            else
            {
                // A group's current participants. Ended and cancelled bookings are kept
                // for the calendar but are no longer anybody's to manage here.
                query = query.Where(BookingRules.IsOpenOn(DateTime.UtcNow.Date));
            }
        }


        private bool IsStaff() => User.HasClaim("Permission", BookingRules.StaffPermission);

        private bool CanChange(TrainGroupParticipant participant) => IsStaff() || participant.UserId == GetCallerId();

        // A page may say it is an admin page; only a staff caller makes it count.
        private bool MayBypassWindow(bool isAdminPage) => isAdminPage && IsStaff();

        private static async Task<TrainGroupParticipant?> LoadForChangeAsync(ApiDbContext context, int id) =>
            await context.TrainGroupParticipants
                .Include(x => x.TrainGroup)
                .Include(x => x.TrainGroupDate)
                .Include(x => x.TrainGroupParticipantUnavailableDates)
                .FirstOrDefaultAsync(x => x.Id == id);

        private void MarkRemoved(TrainGroupParticipant participant, string callerName)
        {
            participant.RemovedOn = DateTime.UtcNow;
            participant.RemovedBy_Id = GetCallerId()?.ToString() ?? string.Empty;
            participant.RemovedBy_FullName = callerName;
        }

        private async Task<string> GetCallerFullNameAsync(ApiDbContext context)
        {
            Guid? callerId = GetCallerId();
            if (callerId == null)
                return string.Empty;

            User? caller = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == callerId.Value);
            return caller == null ? string.Empty : (caller.FirstName + " " + caller.LastName).Trim();
        }

        private async Task SendRemovalEmailAsync(ApiDbContext context, TrainGroupParticipant participant)
        {
            User? user = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == participant.UserId);
            if (user == null)
                return;

            try
            {
                await _emailService.SendBookingEmailAsync(user, new List<TrainGroupParticipant>(), new List<TrainGroupParticipant> { participant });
            }
            catch (Exception)
            {
                // The change stands whether or not the mail goes.
            }
        }

        // The first session a recurring booking still holds on or after a day, if any.
        private static DateTime? FirstSessionOnOrAfter(TrainGroupParticipant participant, DateTime from)
        {
            DateTime? day = BookingRules.NextOccurrence(participant.TrainGroupDate, from);
            for (int i = 0; i < 400 && day != null; i++)
            {
                if (participant.RecurringEndOnDate != null && day.Value >= participant.RecurringEndOnDate.Value.Date)
                    return null;

                if (BookingRules.HoldsPlace(participant, day.Value))
                    return day;

                day = BookingRules.NextOccurrence(participant.TrainGroupDate, day.Value.AddDays(1));
            }

            return null;
        }

        // The latest date on or after `from` at which any of these bookings starts, ends,
        // falls or skips - `from` itself when there is none.
        private static DateTime LastChangeOnOrAfter(IEnumerable<TrainGroupParticipant> participants, DateTime from)
        {
            return participants
                .SelectMany(x => new DateTime?[] { x.SelectedDate, x.RecurringStartOnDate, x.RecurringEndOnDate, x.TrainGroupDate?.FixedDay }
                    .Concat(x.TrainGroupParticipantUnavailableDates.Select(y => (DateTime?)y.UnavailableDate)))
                .Where(x => x != null)
                .Select(x => x!.Value.Date)
                .Where(x => x >= from)
                .DefaultIfEmpty(from)
                .Max();
        }

        private static DateTime? AsUtc(DateTime? value) =>
            value == null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);

        private static TrainGroupParticipantBookingDto ToBookingDto(TrainGroupParticipant participant, DateTime clientNow)
        {
            DateTime today = clientNow.Date;
            bool isRecurring = BookingRules.IsRecurring(participant);

            TrainGroupParticipantBookingDto dto = new TrainGroupParticipantBookingDto()
            {
                Id = participant.Id,
                TrainGroupId = participant.TrainGroupId,
                TrainGroupDateId = participant.TrainGroupDateId,
                TrainGroupDateType = participant.TrainGroupDate.TrainGroupDateType,
                Title = participant.TrainGroup.Title,
                TrainerFullName = (participant.TrainGroup.Trainer?.FirstName + " " + participant.TrainGroup.Trainer?.LastName).Trim(),
                StartOn = participant.TrainGroup.StartOn,
                Duration = participant.TrainGroup.Duration,
                IsOneOff = !isRecurring,
                RemovedOn = AsUtc(participant.RemovedOn),
                RemovedBy_FullName = participant.RemovedBy_FullName
            };

            if (!isRecurring)
            {
                DateTime? date = (participant.SelectedDate ?? participant.TrainGroupDate.FixedDay)?.Date;
                dto.Date = AsUtc(date);

                if (date != null && participant.RemovedOn == null && BookingRules.SessionStart(participant.TrainGroup, date.Value) > clientNow)
                    dto.NextSessionDate = AsUtc(date);

                return dto;
            }

            DateTime start = (participant.RecurringStartOnDate ?? participant.CreatedOn).Date;
            DateTime? end = participant.RecurringEndOnDate?.Date;

            dto.RecurrenceDayOfWeek = (int?)participant.TrainGroupDate.RecurrenceDayOfWeek;
            dto.RecurrenceDayOfMonth = participant.TrainGroupDate.RecurrenceDayOfMonth;
            dto.StartOnDate = AsUtc(start);
            dto.EndOnDate = AsUtc(end);

            DateTime? day = BookingRules.NextOccurrence(participant.TrainGroupDate, today > start ? today : start);
            for (int i = 0; i < 400 && day != null && (end == null || day < end); i++)
            {
                if (BookingRules.HoldsPlace(participant, day.Value) && BookingRules.SessionStart(participant.TrainGroup, day.Value) > clientNow)
                {
                    dto.NextSessionDate = AsUtc(day);
                    break;
                }

                day = BookingRules.NextOccurrence(participant.TrainGroupDate, day.Value.AddDays(1));
            }

            if (end != null)
                for (DateTime previous = end.Value.AddDays(-1); previous >= start; previous = previous.AddDays(-1))
                    if (BookingRules.HoldsPlace(participant, previous))
                    {
                        dto.LastSessionDate = AsUtc(previous);
                        break;
                    }

            dto.UpcomingSkips = participant.TrainGroupParticipantUnavailableDates
                .Where(x => x.UnavailableDate.Date >= today && (end == null || x.UnavailableDate.Date < end))
                .OrderBy(x => x.UnavailableDate)
                .Select(x => new TrainGroupParticipantSkipDto() { Id = x.Id, Date = DateTime.SpecifyKind(x.UnavailableDate.Date, DateTimeKind.Utc) })
                .ToList();

            return dto;
        }
    }
}
