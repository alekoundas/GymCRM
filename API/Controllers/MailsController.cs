using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.Mail;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class MailsController : GenericController<Mail, MailDto, MailAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer _localizer;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public MailsController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer,
            IEmailService emailService) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _emailService = emailService;
            _localizer = localizer;
        }


        // POST: api/Mails/Send
        [HttpPost("Send")]
        public async Task<ApiResponse<bool>> Send([FromBody] MailSendDto dto)
        {
            List<Guid> userIds = dto.UserIds.Select(y => new Guid(y)).ToList();
            List<User> users = await _dataService.Users
                .Where(x => userIds.Any(y => y == x.Id))
                .ToListAsync();

            foreach (string email in users.Select(x => x.Email))
                await _emailService.SendEmailAsync(
                    email,
                    dto.Subject,
                    dto.Body
                );

            return new ApiResponse<bool>().SetSuccessResponse(true, "Password reset email sent.");
        }


        protected override void DataTableQueryUpdate(IGenericRepository<Mail> query)
        {
            query = query.Include(x => x.User);
        }

    }
}
