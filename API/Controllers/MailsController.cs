using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Mail;
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
    public class MailsController : GenericController<Mail, MailDto, MailAddDto>
    {
        // Status is an enum. The reflection helpers in GetDataTable convert filter
        // values with Convert.ChangeType, which cannot turn "SENT" into one, so the
        // field is claimed here and both its filter and its sort are done typed.
        private const string StatusField = "status";

        // Mail bodies are personal, so without this a member could read the whole gym's.
        private const string AdminViewClaim = "Mails_View";

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

            if (users.Count == 0)
                return new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, typeof(User).Name]);

            // Written down and handed to the background sender. Sending here meant one
            // Gmail round trip per recipient on the request thread, which timed the
            // browser out long before a list this size was through.
            await _emailService.QueueEmailAsync(users, dto.Subject, dto.Body);

            return new ApiResponse<bool>().SetSuccessResponse(true, _localizer[TranslationKeys._0_emails_queued, users.Count.ToString()]);
        }

        // POST: api/controller/DeleteAll
        [HttpPost("DeleteAll")]
        public virtual async Task<ActionResult<ApiResponse<bool>>> DeleteAll()
        {
            if (!IsUserAuthorized("Delete"))
                return new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            string className = typeof(Mail).Name;
            List<Mail> mails = await _dataService.Mails.ToListAsync();

            int result = await _dataService.Mails.RemoveRangeAsync(mails);
            return new ApiResponse<bool>().SetSuccessResponse(true, _localizer[TranslationKeys._0_deleted_successfully, className]);
        }



        protected override void DataTableQueryUpdate(IGenericRepository<Mail> query, DataTableDto<MailDto> dataTable)
        {
            query = query.Include(x => x.User);

            Guid? scopeUserId = GetScopeToCallerId(AdminViewClaim);
            if (scopeUserId != null)
                query = query.Where(x => x.UserId == scopeUserId.Value);

            DataTableFilterDto? statusFilter = dataTable.Filters
                .FirstOrDefault(x => string.Equals(x.FieldName, StatusField, StringComparison.OrdinalIgnoreCase));

            if (statusFilter?.Value != null && Enum.TryParse(statusFilter.Value, true, out MailStatusEnum status))
                query = query.Where(x => x.Status == status);

            DataTableSortDto? statusSort = dataTable.Sorts
                .FirstOrDefault(x => string.Equals(x.FieldName, StatusField, StringComparison.OrdinalIgnoreCase));

            if (statusSort != null)
            {
                if (statusSort.Order > 0)
                    query = query.OrderBy(x => x.Status);
                else
                    query = query.OrderByDescending(x => x.Status);
            }
        }

        protected override HashSet<string> GetHandledDataTableFields()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                StatusField
            };
        }

    }
}
