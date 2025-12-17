using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
using Core.Enums;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TrainGroupParticipantUnavailableDatesController : GenericController<TrainGroupParticipantUnavailableDate, TrainGroupParticipantUnavailableDateDto, TrainGroupParticipantUnavailableDateAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;

        public TrainGroupParticipantUnavailableDatesController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
        }

        protected override bool IsUserAuthorized(string action)
        {
            return true;
        }
        protected override bool CustomValidatePOST(TrainGroupParticipantUnavailableDateAddDto entityDto, out string[] errors)
        {
            errors = Array.Empty<string>();
            DateTime slotStartUtc;
            int offsetMin = entityDto.ClientTimezoneOffsetMinutes; // From client: new Date().getTimezoneOffset()
            double offsetH = -(offsetMin / 60.0);
            DateTime nowUtc = DateTime.UtcNow;
            TrainGroupParticipant? trainGroupParticipant = _dataService.TrainGroupParticipants
                .Include(x => x.TrainGroup)
                .Include(x => x.TrainGroupDate.TrainGroup)
                .Where(x => x.Id == entityDto.TrainGroupParticipantId)
                .FirstOrDefault();

            if (trainGroupParticipant == null)
                return true;

            if (!entityDto.IsAdminPage)
            {
                if (trainGroupParticipant?.SelectedDate != null)
                {
                    // One-off: Use stored full UTC datetime (assumes time is set as in frontend)
                    slotStartUtc = trainGroupParticipant.SelectedDate.Value;
                    slotStartUtc = slotStartUtc.AddHours(trainGroupParticipant.TrainGroup.StartOn.Hour);
                    slotStartUtc = slotStartUtc.AddMinutes(trainGroupParticipant.TrainGroup.StartOn.Minute);
                }
                else
                {
                    // Recurring: Compute next upcoming occurrence with local time overlaid
                    slotStartUtc = CalculateNextOccurrenceDateTime(trainGroupParticipant!.TrainGroupDate, entityDto.UnavailableDate);
                }

                if (slotStartUtc > nowUtc.AddHours(offsetH) && slotStartUtc <= nowUtc.AddHours(offsetH).AddHours(12))
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
            TrainGroupParticipant? trainGroupParticipant = _dataService.TrainGroupParticipants
                .Include(x => x.TrainGroup)
                .Where(x => x.Id == entity.TrainGroupParticipantId)
                .FirstOrDefault();

            if (trainGroupParticipant == null)
                return true;

            int participantsCount = _dataService.TrainGroupParticipants
                .Where(x => x.TrainGroupDateId == trainGroupParticipant.TrainGroupDateId)
                .Where(x => !x.TrainGroupParticipantUnavailableDates.Any(z => z.UnavailableDate == entity.UnavailableDate))
                .Where(x =>
                  x.SelectedDate != null ?
                        (x.SelectedDate == entity.UnavailableDate)
                    :
                        (x.TrainGroupDate.FixedDay == entity.UnavailableDate
                        || x.TrainGroupDate.RecurrenceDayOfWeek == entity.UnavailableDate.DayOfWeek
                        || x.TrainGroupDate.RecurrenceDayOfMonth == entity.UnavailableDate.Month)
                )
                .Count();

            int maxParticipantsCount = trainGroupParticipant.TrainGroup.MaxParticipants;

            if (maxParticipantsCount - participantsCount <= 0)
            {
                errors = [_localizer[TranslationKeys.Maximum_amount_of_participants_has_been_reached]];
                return true;
            }

            return false;
        }


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
                    //if (daysToAdd == 0) daysToAdd = 7; // Next occurrence if today (but adjust if same-day allowed; here next for safety)
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
    }
}
