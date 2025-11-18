using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos.DataTable;
using Core.Dtos.TrainGroupΑttendance;
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
    public class TrainGroupAttendancesController : GenericController<TrainGroupΑttendance, TrainGroupΑttendanceDto, TrainGroupΑttendanceAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer _localizer;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupAttendancesController(
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


        protected override bool IsUserAuthorized(string action)
        {
            string controllerName = "TrainGroups";
            string claimName = controllerName + "_" + action;
            bool hasClaim = User.HasClaim("Permission", claimName);
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return hasClaim;
        }

        protected override bool CustomValidatePOST(TrainGroupΑttendanceAddDto entity, out string[] errors)
        {
            errors = Array.Empty<string>();

            bool isAlreadyParticipating = _dataService.TrainGroupΑttendances
                .Where(x => x.TrainGroupId == entity.TrainGroupId)
                .Where(x => x.UserId == new Guid(entity.UserId))
                .Any(x => x.AttendanceDate == entity.AttendanceDate);

            if (isAlreadyParticipating)
            {
                errors = [_localizer[TranslationKeys.Duplicate_participant_found]];
                return true;
            }

            return false;
        }


        protected override void DataTableQueryUpdate(IGenericRepository<TrainGroupΑttendance> query, DataTableDto<TrainGroupΑttendanceDto> dataTable)
        {
            query = query.Include(x => x.User);
        }

    }
}
