using AutoMapper;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
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
    // Skipping one date of a recurring booking, and rejoining it.
    [Authorize]
    [Route("api/[controller]")]
    public class TrainGroupParticipantUnavailableDatesController : GenericController<TrainGroupParticipantUnavailableDate, TrainGroupParticipantUnavailableDateDto, TrainGroupParticipantUnavailableDateAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;
        private readonly TimeProvider _timeProvider;

        public TrainGroupParticipantUnavailableDatesController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer,
            TimeProvider timeProvider) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
            _timeProvider = timeProvider;
        }

        // Members skip and rejoin their own dates without holding any claim for this
        // controller, so the fence is on the row instead: the booking has to be the
        // caller's own, unless the caller is staff. See CanChange below.
        protected override bool IsUserAuthorized(string action)
        {
            return true;
        }

        // GET: api/TrainGroupParticipantUnavailableDates/5
        // Somebody else's date answers like a missing one.
        public override async Task<ActionResult<ApiResponse<TrainGroupParticipantUnavailableDateDto>>> Get(string? id)
        {
            TrainGroupParticipantUnavailableDate? entity = await _dataService.GetGenericRepository<TrainGroupParticipantUnavailableDate>()
                .Include(x => x.TrainGroupParticipant)
                .FilterByColumnEquals("Id", id)
                .FirstOrDefaultAsync();

            if (entity == null || !CanChange(entity.TrainGroupParticipant))
                return new ApiResponse<TrainGroupParticipantUnavailableDateDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(TrainGroupParticipantUnavailableDate)]);

            return new ApiResponse<TrainGroupParticipantUnavailableDateDto>().SetSuccessResponse(_mapper.Map<TrainGroupParticipantUnavailableDateDto>(entity));
        }

        // A date is skipped and taken back, never edited. The generic PUT would move one past
        // everything POST and DELETE look at: whose it is, the 12 hours, whether there is a place.
        public override Task<ActionResult<ApiResponse<TrainGroupParticipantUnavailableDate>>> Put(string? id, [FromBody] TrainGroupParticipantUnavailableDateDto entityDto)
        {
            return Task.FromResult<ActionResult<ApiResponse<TrainGroupParticipantUnavailableDate>>>(
                new ApiResponse<TrainGroupParticipantUnavailableDate>().SetErrorResponse(
                    _localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));
        }

        protected override bool CustomValidatePOST(TrainGroupParticipantUnavailableDateAddDto entityDto, out string[] errors)
        {
            errors = Array.Empty<string>();

            TrainGroupParticipant? participant = LoadParticipant(entityDto.TrainGroupParticipantId);
            if (participant == null)
            {
                errors = [_localizer[TranslationKeys.Requested_0_not_found, nameof(TrainGroupParticipant)]];
                return true;
            }

            if (!CanChange(participant))
            {
                errors = [_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]];
                return true;
            }

            // Only a date the booking actually holds a place on can be skipped.
            DateTime day = entityDto.UnavailableDate.Date;
            if (!BookingRules.IsRecurring(participant) || !BookingRules.HoldsPlace(participant, day))
            {
                errors = [_localizer[TranslationKeys.This_date_is_not_part_of_the_booking]];
                return true;
            }

            // A page may say it is an admin page; only a staff caller makes it count.
            if (!(entityDto.IsAdminPage && IsStaff()))
            {
                DateTime gymNow = BookingRules.GymNow(_timeProvider);
                DateTime sessionStart = BookingRules.SessionStart(participant.TrainGroup, day);

                if (sessionStart <= gymNow)
                {
                    errors = [_localizer[TranslationKeys.This_session_has_already_started]];
                    return true;
                }

                if (sessionStart <= gymNow.AddHours(BookingRules.MemberChangeWindowHours))
                {
                    errors = [_localizer[TranslationKeys.Cannot_remove_a_session_starting_within_12_hours]];
                    return true;
                }
            }

            return false;
        }


        protected override bool CustomValidateDELETE(TrainGroupParticipantUnavailableDate entity, out string[] errors)
        {
            errors = Array.Empty<string>();

            TrainGroupParticipant? participant = LoadParticipant(entity.TrainGroupParticipantId);
            if (participant == null)
            {
                errors = [_localizer[TranslationKeys.Requested_0_not_found, nameof(TrainGroupParticipant)]];
                return true;
            }

            if (!CanChange(participant))
            {
                errors = [_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]];
                return true;
            }

            // A session that has started stays as it was for a member.
            DateTime day = entity.UnavailableDate.Date;
            if (!IsStaff() && BookingRules.SessionStart(participant.TrainGroup, day) <= BookingRules.GymNow(_timeProvider))
            {
                errors = [_localizer[TranslationKeys.This_session_has_already_started]];
                return true;
            }

            // Rejoining takes a place back, so there has to be one.
            using ApiDbContext context = _dataService.GetDbContext();
            int participantsCount = context.TrainGroupParticipants
                .Where(x => x.TrainGroupId == participant.TrainGroupId)
                .Where(BookingRules.HoldsPlaceOn(day))
                .Count();

            if (participantsCount >= participant.TrainGroup.MaxParticipants)
            {
                errors = [_localizer[TranslationKeys.Maximum_amount_of_participants_has_been_reached]];
                return true;
            }

            return false;
        }


        protected override void DataTableQueryUpdate(IGenericRepository<TrainGroupParticipantUnavailableDate> query, DataTableDto<TrainGroupParticipantUnavailableDateDto> dataTable)
        {
            Guid? scopeUserId = GetScopeToCallerId(BookingRules.StaffPermission);
            if (scopeUserId != null)
                query = query.Where(x => x.TrainGroupParticipant.UserId == scopeUserId.Value);
        }


        private bool IsStaff() => User.HasClaim("Permission", BookingRules.StaffPermission);

        private bool CanChange(TrainGroupParticipant participant) => IsStaff() || participant.UserId == GetCallerId();

        private TrainGroupParticipant? LoadParticipant(int trainGroupParticipantId)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            return context.TrainGroupParticipants
                .AsNoTracking()
                .Include(x => x.TrainGroup)
                .Include(x => x.TrainGroupDate)
                .Include(x => x.TrainGroupParticipantUnavailableDates)
                .FirstOrDefault(x => x.Id == trainGroupParticipantId);
        }
    }
}
