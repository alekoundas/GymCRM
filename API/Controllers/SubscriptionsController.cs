using AutoMapper;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Subscription;
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
    public class SubscriptionsController : GenericController<Subscription, SubscriptionDto, SubscriptionAddDto>
    {
        private const string AdminViewClaim = "SubscriptionsAdmin_View";
        private const string AdminAddClaim = "SubscriptionsAdmin_Add";
        private const string AdminEditClaim = "SubscriptionsAdmin_Edit";
        private const string AdminDeleteClaim = "SubscriptionsAdmin_Delete";

        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer,
            ISubscriptionService subscriptionService) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
            _subscriptionService = subscriptionService;
        }


        // The inherited write endpoints are closed off deliberately. They take a whole
        // dto and would let anybody holding Subscriptions_Add post an approved row with
        // any amount on it. Everything below goes through an endpoint that decides the
        // status and the amount itself.
        public override Task<ActionResult<ApiResponse<List<Subscription>>>> Post([FromBody] List<SubscriptionAddDto> entityDtos)
        {
            return Task.FromResult<ActionResult<ApiResponse<List<Subscription>>>>(
                new ApiResponse<List<Subscription>>().SetErrorResponse(
                    _localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));
        }

        public override Task<ActionResult<ApiResponse<Subscription>>> Put(string? id, [FromBody] SubscriptionDto entityDto)
        {
            return Task.FromResult<ActionResult<ApiResponse<Subscription>>>(
                new ApiResponse<Subscription>().SetErrorResponse(
                    _localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]));
        }


        // POST: api/Subscriptions/Add - an administrator granting lessons outright.
        [HttpPost("Add")]
        public async Task<ApiResponse<SubscriptionDto>> Add([FromBody] SubscriptionAddDto dto)
        {
            if (!User.HasClaim("Permission", AdminAddClaim))
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            if (dto.Amount == 0)
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys._0_must_be_a_positive_number, nameof(dto.Amount)]);

            if (!Guid.TryParse(dto.UserId, out Guid userId))
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(User)]);

            DateTime nowUtc = DateTime.UtcNow;

            Subscription subscription = new Subscription
            {
                UserId = userId,
                RequestedAmount = null,
                Amount = dto.Amount,
                Status = SubscriptionStatusEnum.APPROVED,
                AdminComment = dto.AdminComment,
                // Approved the moment it is written, so the monthly chart can read one
                // column whether the entry came from here or from a request.
                DecidedOn = nowUtc,
                CreatedOn = nowUtc,
                CreatedBy_Id = GetCallerId()?.ToString() ?? string.Empty
            };

            await _dataService.Subscriptions.AddAsync(subscription);

            return new ApiResponse<SubscriptionDto>().SetSuccessResponse(_mapper.Map<SubscriptionDto>(subscription));
        }


        // POST: api/Subscriptions/Request - a member asking. Nothing the caller sends
        // can make this grant anything: the status and the amount are set here.
        [HttpPost("Request")]
        public async Task<ApiResponse<SubscriptionDto>> Request([FromBody] SubscriptionRequestDto dto)
        {
            if (!IsUserAuthorized("Add"))
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            Guid? callerId = GetCallerId();
            if (callerId == null)
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_loged_in]);

            if (dto.RequestedAmount <= 0)
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys._0_must_be_a_positive_number, nameof(dto.RequestedAmount)]);

            bool hasPending = await _dataService.Subscriptions
                .AnyAsync(x => x.UserId == callerId.Value && x.Status == SubscriptionStatusEnum.PENDING);

            if (hasPending)
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys.A_request_is_already_pending]);

            Subscription subscription = new Subscription
            {
                UserId = callerId.Value,
                RequestedAmount = dto.RequestedAmount,
                Amount = 0,
                Status = SubscriptionStatusEnum.PENDING,
                MemberComment = dto.MemberComment,
                CreatedOn = DateTime.UtcNow,
                CreatedBy_Id = callerId.Value.ToString()
            };

            await _dataService.Subscriptions.AddAsync(subscription);

            return new ApiResponse<SubscriptionDto>().SetSuccessResponse(_mapper.Map<SubscriptionDto>(subscription));
        }


        // POST: api/Subscriptions/Decide - approving or rejecting one pending request.
        [HttpPost("Decide")]
        public async Task<ApiResponse<SubscriptionDto>> Decide([FromBody] SubscriptionDecideDto dto)
        {
            if (!User.HasClaim("Permission", AdminEditClaim))
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            Subscription? subscription = await _dataService.Subscriptions
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (subscription == null)
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(Subscription)]);

            // Only ever from pending, so a second click cannot grant the lessons twice.
            if (subscription.Status != SubscriptionStatusEnum.PENDING)
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys.The_request_has_already_been_decided]);

            if (dto.IsApproved && dto.Amount <= 0)
                return new ApiResponse<SubscriptionDto>().SetErrorResponse(_localizer[TranslationKeys._0_must_be_a_positive_number, nameof(dto.Amount)]);

            subscription.Status = dto.IsApproved
                ? SubscriptionStatusEnum.APPROVED
                : SubscriptionStatusEnum.REJECTED;

            subscription.Amount = dto.IsApproved ? dto.Amount : 0;
            subscription.AdminComment = dto.AdminComment;
            subscription.DecidedOn = DateTime.UtcNow;

            await _dataService.UpdateAsync(subscription);

            return new ApiResponse<SubscriptionDto>().SetSuccessResponse(_mapper.Map<SubscriptionDto>(subscription));
        }


        // GET: api/Subscriptions/Balance - the caller's own remaining lessons.
        [HttpGet("Balance")]
        public async Task<ApiResponse<int>> Balance()
        {
            Guid? callerId = GetCallerId();
            if (callerId == null)
                return new ApiResponse<int>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_loged_in]);

            return new ApiResponse<int>().SetSuccessResponse(await _subscriptionService.GetBalanceAsync(callerId.Value));
        }


        public override async Task<ActionResult<ApiResponse<Subscription>>> Delete(string? id)
        {
            Subscription? subscription = await _dataService.Subscriptions
                .FirstOrDefaultAsync(x => x.Id.ToString() == id);

            if (subscription == null)
                return new ApiResponse<Subscription>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(Subscription)]);

            if (User.HasClaim("Permission", AdminDeleteClaim))
                return await base.Delete(id);

            // A member may only take back their own request, and only while nobody has
            // acted on it. Anything decided is history and not theirs to remove.
            Guid? callerId = GetCallerId();

            if (callerId == null || subscription.UserId != callerId.Value
                || subscription.Status != SubscriptionStatusEnum.PENDING)
                return new ApiResponse<Subscription>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            return await base.Delete(id);
        }


        protected override void DataTableQueryUpdate(IGenericRepository<Subscription> query, DataTableDto<SubscriptionDto> dataTable)
        {
            query = query.Include(x => x.User);

            Guid? scopeUserId = GetScopeToCallerId(AdminViewClaim);
            if (scopeUserId != null)
                query = query.Where(x => x.UserId == scopeUserId.Value);
        }
    }
}
