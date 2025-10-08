using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.PhoneNumber;
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


        // PUT: api/controller/5
        [HttpPut("{id}")]
        public override async Task<ActionResult<ApiResponse<PhoneNumber>>> Put(string? id, [FromBody] PhoneNumberDto entityDto)
        {
            if (!IsUserAuthorized("Edit"))
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

        protected override bool CustomValidateDELETE(PhoneNumber entity, out string[] errors)
        {
            if(entity.IsPrimary)
            {
                errors = new string[] { _localizer[TranslationKeys.Primary_phone_number_cannot_be_deleted]};
                return true;
            }

            errors = Array.Empty<string>();
            return false;
        }


        protected override bool IsUserAuthorized(string action)
        {
            return true;
        }
    }
}
