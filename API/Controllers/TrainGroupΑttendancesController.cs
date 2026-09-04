using AutoMapper;
using Business.Repository;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
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


        // POST: api/TrainGroupAttendances
        public override async Task<ActionResult<ApiResponse<List<TrainGroupΑttendance>>>> Post([FromBody] List<TrainGroupΑttendanceAddDto> entityDtos)
        {
            ActionResult<ApiResponse<List<TrainGroupΑttendance>>> response = await base.Post(entityDtos);

            // Only attendance that actually went in is worth telling anybody about.
            if (response.Value?.IsSucceed == true)
                await QueueAttendanceEmailsAsync(entityDtos);

            return response;
        }

        // Attendance is taken for a whole group on one date, so everyone in that batch
        // reads the same mail and the lot goes down as one insert. Queued, never sent
        // here - the trainer is waiting on the grid, not on Google.
        private async Task QueueAttendanceEmailsAsync(List<TrainGroupΑttendanceAddDto> entityDtos)
        {
            List<Guid> userIds = entityDtos.Select(x => new Guid(x.UserId)).Distinct().ToList();
            List<User> users = await _dataService.Users
                .Where(x => userIds.Contains(x.Id))
                .ToListAsync();

            List<int> trainGroupIds = entityDtos.Select(x => x.TrainGroupId).Distinct().ToList();
            List<TrainGroup> trainGroups = await _dataService.TrainGroups
                .Where(x => trainGroupIds.Contains(x.Id))
                .ToListAsync();

            foreach (var batch in entityDtos.GroupBy(x => new { x.TrainGroupId, x.AttendanceDate }))
            {
                List<User> batchUsers = batch
                    .Select(x => users.FirstOrDefault(y => y.Id == new Guid(x.UserId)))
                    .Where(x => x != null)
                    .Select(x => x!)
                    .ToList();

                if (batchUsers.Count == 0)
                    continue;

                string trainGroupTitle = trainGroups
                    .FirstOrDefault(x => x.Id == batch.Key.TrainGroupId)?.Title ?? string.Empty;

                // TODO add if payments are lees than 1.
                //await _emailService.QueueEmailAsync(
                //    batchUsers,
                //    _localizer[TranslationKeys.Attendance_Confirmation],
                //    GetAttendanceEmailBody(trainGroupTitle, batch.Key.AttendanceDate)
                //);
            }
        }

        private string GetAttendanceEmailBody(string trainGroupTitle, DateTime attendanceDate)
        {
            string emailBody = @"
                <html>
                <head>
                    <style>
                        body { font-family: Arial, sans-serif; color: #333; line-height: 1.6; }
                        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                        h2 { color: #2c3e50; }
                        .section { margin-bottom: 20px; }
                        ul { list-style-type: none; padding: 0; }
                        li { padding: 5px 0; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>" + _localizer[TranslationKeys.Attendance_Information] + "</h2>";

            emailBody += "<div class='section'>";
            emailBody += "<p>" + _localizer[TranslationKeys.Your_attendance_was_recorded] + "</p>";
            emailBody += "<ul>";
            emailBody += "<li><strong>" + _localizer[TranslationKeys.Train_group] + ":</strong> " + trainGroupTitle + "</li>";
            emailBody += "<li><strong>" + _localizer[TranslationKeys.Attendance_date] + ":</strong> " + attendanceDate.ToString("dd/MM/yyyy") + "</li>";
            emailBody += "</ul>";
            emailBody += "</div>";

            emailBody += @"
                    </div>
                </body>
                </html>";

            return emailBody;
        }


        protected override void DataTableQueryUpdate(IGenericRepository<TrainGroupΑttendance> query, DataTableDto<TrainGroupΑttendanceDto> dataTable)
        {
            query = query
                .Include(x => x.User)
                .Include(x => x.TrainGroup);

            // Same claim the rest of this controller authorises against.
            Guid? scopeUserId = GetScopeToCallerId("TrainGroups_View");
            if (scopeUserId != null)
                query = query.Where(x => x.UserId == scopeUserId.Value);
        }

    }
}
