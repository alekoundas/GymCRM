using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.PhoneNumber;
using Business.Repository;
using Core.Dtos.DataTable;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class PhoneNumbersController : GenericController<PhoneNumber, PhoneNumberDto, PhoneNumberAddDto>
    {
        private const string ViewClaim = "Users_View";
        private const string EditClaim = "Users_Edit";

        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;

        //private readonly ILogger<TrainGroupDateController> _logger;

        public PhoneNumbersController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
        }


        // GET: api/PhoneNumbers/5
        // Somebody else's number answers like a missing one.
        public override async Task<ActionResult<ApiResponse<PhoneNumberDto>>> Get(string? id)
        {
            PhoneNumber? entity = await _dataService.PhoneNumbers.FilterByColumnEquals("Id", id).FirstOrDefaultAsync();

            if (entity == null || !CanAccess(entity.UserId, ViewClaim))
                return new ApiResponse<PhoneNumberDto>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found, nameof(PhoneNumber)]);

            return new ApiResponse<PhoneNumberDto>().SetSuccessResponse(_mapper.Map<PhoneNumberDto>(entity));
        }

        // PUT: api/controller/5
        [HttpPut("{id}")]
        public override async Task<ActionResult<ApiResponse<PhoneNumber>>> Put(string? id, [FromBody] PhoneNumberDto entityDto)
        {
            // The number is looked for below among those of the member the form names, so
            // asking after that member is asking after the number.
            if (!CanAccess(new Guid(entityDto.UserId), EditClaim))
                return new ApiResponse<PhoneNumber>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]);

            //if (!ModelState.IsValid)
            //    return new ApiResponse<PhoneNumber>().SetErrorResponse("Invalid data provided.");

            PhoneNumber entity = _mapper.Map<PhoneNumber>(entityDto);


            List<PhoneNumber>? phoneNumbers = await _dataService.PhoneNumbers
                .Where(x => x.UserId == new Guid(entityDto.UserId))
                .ToListAsync();

            PhoneNumber? existingEntity = phoneNumbers.FirstOrDefault(x => x.Id == entityDto.Id);
            if (existingEntity == null)
            {
                string className = typeof(PhoneNumber).Name;
                return new ApiResponse<PhoneNumber>().SetErrorResponse(_localizer[TranslationKeys.Requested_0_not_found,className]);
            }

            PhoneNumber? alreadyExistingPrimaryNumber = phoneNumbers
                .Where(x => x.Id != entity.Id)
                .FirstOrDefault(x => x.IsPrimary);

            // Remove primary from other phone number
            if (entityDto.IsPrimary && alreadyExistingPrimaryNumber != null)
            {
                alreadyExistingPrimaryNumber.IsPrimary = false;
                await _dataService.UpdateAsync(alreadyExistingPrimaryNumber);
            }

            if (!entityDto.IsPrimary && alreadyExistingPrimaryNumber == null)
                return new ApiResponse<PhoneNumber>().SetErrorResponse(_localizer[TranslationKeys.At_least_1_primary_phone_number_is_required]);

            await _dataService.UpdateAsync(entity);
            return new ApiResponse<PhoneNumber>().SetSuccessResponse(entity);
        }

        protected override bool CustomValidatePOST(PhoneNumberAddDto entityDto, out string[] errors)
        {
            errors = Array.Empty<string>();

            if (CanAccess(new Guid(entityDto.UserId), EditClaim))
                return false;

            errors = [_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]];
            return true;
        }

        protected override bool CustomValidateDELETE(PhoneNumber entity, out string[] errors)
        {
            if (!CanAccess(entity.UserId, EditClaim))
            {
                errors = [_localizer[TranslationKeys.User_is_not_authorized_to_perform_this_action]];
                return true;
            }

            if(entity.IsPrimary)
            {
                errors = new string[] { _localizer[TranslationKeys.Primary_phone_number_cannot_be_deleted]};
                return true;
            }

            errors = Array.Empty<string>();
            return false;
        }


        // Members keep their own numbers without holding any claim for this controller, so
        // the fence is on the row instead: the number has to be the caller's own, or the
        // caller one of the staff who may view or edit members.
        protected override bool IsUserAuthorized(string action)
        {
            return true;
        }

        private bool CanAccess(Guid userId, string staffClaim) => userId == GetCallerId() || User.HasClaim("Permission", staffClaim);

        protected override void DataTableQueryUpdate(IGenericRepository<PhoneNumber> query, DataTableDto<PhoneNumberDto> dataTable)
        {
            // Anyone who can list users can see their numbers; everybody else gets theirs.
            Guid? scopeUserId = GetScopeToCallerId(ViewClaim);
            if (scopeUserId != null)
                query = query.Where(x => x.UserId == scopeUserId.Value);
        }

    }
}
