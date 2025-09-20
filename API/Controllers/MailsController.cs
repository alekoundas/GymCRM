using AutoMapper;
using Business.Services;
using Core.Dtos.Mail;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class MailsController : GenericController<Mail, MailDto, MailAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public MailsController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
            _mapper = mapper;
        }
    }
}
