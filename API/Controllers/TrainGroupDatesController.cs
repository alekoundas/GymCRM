
using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroupDate;
using Core.Dtos.User;
using Core.Enums;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupDatesController : GenericController<TrainGroupDate, TrainGroupDateDto, TrainGroupDateAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;

        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupDatesController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
        }



        // POST: api/TrainGroupDate/TimeSlots
        [HttpPost("TimeSlots")]
        public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        {
            DateTime selectedDate = timeSlotRequestDto.SelectedDate.Date;

            List<TrainGroupDate>? trainGroupDates = await _dataService.TrainGroupDates
                    .Include(x => x.TrainGroup.Trainer)
                    .Include(x => x.TrainGroupParticipants)
                    .Include(x => x.TrainGroup.TrainGroupDates)
                    .Include(x => x.TrainGroup.TrainGroupUnavailableDates)
                    .Include(x => x.TrainGroup.TrainGroupParticipants)
                    .ThenInclude<TrainGroupParticipant, IEnumerable<TrainGroupParticipantUnavailableDate>>(x => x.TrainGroupParticipantUnavailableDates)
                    .Where(x =>
                        x.FixedDay == selectedDate
                        || x.RecurrenceDayOfMonth == selectedDate.Day
                        || x.RecurrenceDayOfWeek == selectedDate.DayOfWeek)
                    .ToListAsync(true);

            List<TimeSlotResponseDto>? timeSlotRequestDtos = trainGroupDates
                .GroupBy(x => x.TrainGroup)
                .Select(x => new TimeSlotResponseDto()
                {
                    Title = x.Key.Title,
                    Description = x.Key.Description,
                    Duration = x.Key.Duration,
                    StartOn = x.Key.StartOn,
                    TrainerId = x.Key.TrainerId,
                    Trainer = _mapper.Map<UserDto>(x.Key.Trainer),
                    TrainGroupId = x.Key.Id,
                    IsUnavailableTrainGroup = x.Key.TrainGroupUnavailableDates.Any(y => y.UnavailableDate == timeSlotRequestDto.SelectedDate),
                    UnavailableTrainGroupId = x.Key.TrainGroupUnavailableDates.FirstOrDefault(y => y.UnavailableDate == timeSlotRequestDto.SelectedDate)?.Id,
                    //IsUnavailableTrainGroup = false,
                    //UnavailableTrainGroupId = null,
                    RecurrenceDates = x.Key.TrainGroupDates
                        .Where(y => y.RecurrenceDayOfMonth.HasValue || y.RecurrenceDayOfWeek.HasValue)
                        .Select(y =>
                            new TimeSlotRecurrenceDateDto()
                            {
                                TrainGroupDateId = y.Id,
                                TrainGroupDateType = y.TrainGroupDateType,
                                Date = y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                                    ? new DateTime(2000, 1, 2 + (int)y.RecurrenceDayOfWeek!.Value)
                                    : new DateTime(2000, 1, y.RecurrenceDayOfMonth!.Value),
                                IsUserJoined = y.TrainGroupParticipants.Where(z => z.SelectedDate == null).Any(z => z.UserId == new Guid(timeSlotRequestDto.UserId)),
                            }
                        )
                        .Concat(
                            x.Key.TrainGroupDates
                            .Where(y => y.FixedDay.HasValue)
                            .Where(y => y.FixedDay == selectedDate)
                            .Select(y =>
                                new TimeSlotRecurrenceDateDto()
                                {
                                    TrainGroupDateId = y.Id,
                                    TrainGroupDateType = y.TrainGroupDateType,
                                    Date = y.FixedDay!.Value,
                                    IsUserJoined = y.TrainGroupParticipants.Any(z => z.UserId == new Guid(timeSlotRequestDto.UserId)),
                                }
                            )
                        )
                        .Concat(
                                x.Key.TrainGroupDates.Any(y => y.FixedDay == selectedDate)
                                ?
                                    new List<TimeSlotRecurrenceDateDto>()
                                :
                                    new List<TimeSlotRecurrenceDateDto>()
                                    {
                                        new TimeSlotRecurrenceDateDto()
                                        {
                                            TrainGroupDateId = x.Key.TrainGroupDates.First(y =>
                                                y.FixedDay == selectedDate
                                                || y.RecurrenceDayOfMonth== selectedDate.Day
                                                || y.RecurrenceDayOfWeek == selectedDate.DayOfWeek)
                                                .Id,
                                            TrainGroupDateType = null,
                                            Date = selectedDate,
                                            IsUserJoined = x.Key.TrainGroupParticipants
                                                .Where(y => y.UserId == new Guid(timeSlotRequestDto.UserId))
                                                .Where(y => y.SelectedDate.HasValue)
                                                .Where(y => y.SelectedDate == selectedDate)
                                                .Any(),
                                        }
                                    }
                        )
                        .ToList(),
                    SpotsLeft =
                    (
                        x.Key.MaxParticipants -
                        x.Key.TrainGroupParticipants
                                .Where(y => !y.TrainGroupParticipantUnavailableDates.Any(z => z.TrainGroupParticipantId == y.Id && z.UnavailableDate == selectedDate))
                                .Where(y =>
                                  y.SelectedDate != null ?
                                        (y.SelectedDate == selectedDate)
                                    :
                                        (y.TrainGroupDate.FixedDay == selectedDate
                                        || y.TrainGroupDate.RecurrenceDayOfWeek == selectedDate.DayOfWeek
                                        || y.TrainGroupDate.RecurrenceDayOfMonth == selectedDate.Month)
                                )
                                //.Select(y => y.UserId)
                                //.Distinct()
                                .Count()
                    ),
                })
                .ToList();

            return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        }

        protected override bool CustomValidatePOST(TrainGroupDateAddDto entityDto, out string[] errors)
        {
            TrainGroupDate trainGroupDate = _mapper.Map<TrainGroupDate>(entityDto);
            errors = ValidateTrainGroupDate(trainGroupDate);
            return errors.Count() > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupDateDto entityDto, out string[] errors)
        {
            TrainGroupDate trainGroupDate = _mapper.Map<TrainGroupDate>(entityDto);
            errors = ValidateTrainGroupDate(trainGroupDate, entityDto.Id);
            return errors.Count() > 0;
        }

        private string[] ValidateTrainGroupDate(TrainGroupDate dto, int? excludeId = null)
        {
            var errorList = new List<string>();

            // Fetch existing TrainGroupDates from database
            var trainGroupDatesQuery = _dataService.TrainGroupDates
                .Where(x => x.TrainGroupId == dto.TrainGroupId);

            if (excludeId.HasValue)
                trainGroupDatesQuery = trainGroupDatesQuery.Where(x => x.Id != excludeId.Value);

            List<TrainGroupDate> trainGroupDates = trainGroupDatesQuery.ToList();

            // Check for date mixing (DAY_OF_WEEK and DAY_OF_MONTH)
            bool hasDayOfWeek = trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK);
            bool hasDayOfMonth = trainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH);
            if ((hasDayOfWeek && dto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH) ||
                (hasDayOfMonth && dto.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK))
            {
                errorList.Add(_localizer[TranslationKeys.Day_of_week_and_day_of_month_doesnt_mix]);
            }

            // Cache DAY_OF_WEEK and DAY_OF_MONTH for efficient conflict checks
            var dayOfWeekDates = trainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK && x.RecurrenceDayOfWeek.HasValue)
                .Select(x => x.RecurrenceDayOfWeek)
                .ToHashSet();
            var dayOfMonthDates = trainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH && x.RecurrenceDayOfMonth.HasValue)
                .Select(x => x.RecurrenceDayOfMonth)
                .ToHashSet();

            // Check for fixed date conflicts
            if (dto.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && dto.FixedDay.HasValue)
            {
                if (dayOfWeekDates.Contains(dto.FixedDay.Value.DayOfWeek))
                {
                    errorList.Add(_localizer[TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_week_entry, $"{dto.FixedDay.Value:yyyy-MM-dd}"]);
                }
                if (dayOfMonthDates.Contains(dto.FixedDay.Value.Day))
                {
                    errorList.Add(_localizer[TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_month_entry, $"{dto.FixedDay.Value:yyyy-MM-dd}"]);
                }
            }

            // Check for duplicates
            var mappedDto = _mapper.Map<TrainGroupDate>(dto);
            var allDates = new List<TrainGroupDate>(trainGroupDates) { mappedDto };
            var duplicates = allDates
                .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay })
                .Where(g => g.Count() > 1)
                .ToList();
            if (duplicates.Any())
                errorList.Add(_localizer[TranslationKeys.Duplicate_train_group_date_found]);

            return errorList.ToArray();
        }

    }
}
