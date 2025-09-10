using AutoMapper;
using Business.Services;
using Core.Dtos.PhoneNumber;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class PhoneNumbersController : GenericController<PhoneNumber, PhoneNumberDto, PhoneNumberAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public PhoneNumbersController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
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
