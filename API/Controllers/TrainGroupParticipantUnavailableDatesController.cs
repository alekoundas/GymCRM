using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
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
    }
}
