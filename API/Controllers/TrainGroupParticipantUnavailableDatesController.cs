using AutoMapper;
using Business.Services;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TrainGroupParticipantUnavailableDatesController : GenericController<TrainGroupParticipantUnavailableDate, TrainGroupParticipantUnavailableDateDto, TrainGroupParticipantUnavailableDateAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;

        public TrainGroupParticipantUnavailableDatesController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        protected override bool IsUserAuthorized(string action)
        {
            return true;
        }
    }
}
