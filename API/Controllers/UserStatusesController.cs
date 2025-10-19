using AutoMapper;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.Lookup;
using Core.Dtos.UserStatus;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UserStatusesController : GenericController<UserStatus, UserStatusDto, UserStatusAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer _localizer;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public UserStatusesController(
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

        // POST: api/Users/lookup
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {

            var query = _dataService.GetGenericRepository<UserStatus>();

            if (lookupDto.Filter.Id.Length > 0)
                query.Where(x => x.Id.ToString() == lookupDto.Filter.Id);

            if (lookupDto.Filter.Value.Length > 0)
                query.Where(x =>x.Name.ToLower().Contains(lookupDto.Filter.Value.ToLower()));

            // Handle Pagging.
            query.AddPagging(lookupDto.Skip, lookupDto.Take);

            // Retrieve Data.
            List<UserStatus> result = await query.ToListAsync();
            lookupDto.Data = result
              .Select(x =>
                  new LookupOptionDto()
                  {
                      Id = x.Id.ToString(),
                      Value = x.Name,
                      UserColor=x.Color
                  })
              .ToList();

            // Find count.
            if (lookupDto.Filter.Id.Length > 0)
                query.Where(x => x.Id.ToString() == lookupDto.Filter.Id);

            if (lookupDto.Filter.Value.Length > 0)
                query.Where(x => x.Name.ToLower().Contains(lookupDto.Filter.Value.ToLower()));
            lookupDto.TotalRecords = await query.CountAsync();


            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }
    }
}
