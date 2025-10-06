using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.PhoneNumber;
using Core.Models;
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
                return new ApiResponse<PhoneNumber>().SetErrorResponse("User is not authorized to perform this action.");

            if (!ModelState.IsValid)
                return new ApiResponse<PhoneNumber>().SetErrorResponse("Invalid data provided.");

            PhoneNumber entity = _mapper.Map<PhoneNumber>(entityDto);


            List<PhoneNumber>? phoneNumbers = await _dataService.PhoneNumbers
                .Where(x => x.UserId == new Guid(entityDto.UserId))
                .ToListAsync();

            PhoneNumber? existingEntity = phoneNumbers.FirstOrDefault(x => x.Id == entityDto.Id);
            if (existingEntity == null)
            {
                string className = typeof(PhoneNumber).Name;
                return new ApiResponse<PhoneNumber>().SetErrorResponse($"Requested {className} not found!");
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
                return new ApiResponse<PhoneNumber>().SetErrorResponse($"At least 1 primary phone number is required!");

            await _dataService.UpdateAsync(entity);
            return new ApiResponse<PhoneNumber>().SetSuccessResponse(entity);
        }

        protected override bool CustomValidateDELETE(PhoneNumber entity, out string[] errors)
        {
            if(entity.IsPrimary)
            {
                errors = new string[] { "Primary phone number cannot be deleted! Please set another phone number as primary first." };
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
