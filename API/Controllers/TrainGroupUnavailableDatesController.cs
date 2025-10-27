using AutoMapper;
using Business.Services;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
using Core.Dtos.TrainGroupUnavailableDate;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TrainGroupUnavailableDatesController : GenericController<TrainGroupUnavailableDate, TrainGroupUnavailableDateDto, TrainGroupUnavailableDateAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;

        public TrainGroupUnavailableDatesController(
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
    }
}
