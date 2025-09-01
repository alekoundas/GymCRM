
using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroup;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupDateParticipantsController : GenericController<TrainGroupDateParticipant, TrainGroupDateParticipantDto, TrainGroupDateParticipantDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupDateParticipantsController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
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
