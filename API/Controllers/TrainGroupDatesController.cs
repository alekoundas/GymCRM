
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
using Microsoft.EntityFrameworkCore;
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
        // The sessions on one day, for the booking page and the admin calendar: how many
        // places are left, and where the member stands on each. Both counts come from
        // BookingRules, so a booking that starts next month or ended last week no longer
        // takes a place today.
        [HttpPost("TimeSlots")]
        public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        {
            DateTime selectedDate = timeSlotRequestDto.SelectedDate.Date;
            DateTime nextDay = selectedDate.AddDays(1);

            // A member only ever sees where they themselves stand. Staff book on others'
            // behalf, so they may ask after anybody.
            Guid userId = GetCallerId() ?? Guid.Empty;
            if (Guid.TryParse(timeSlotRequestDto.UserId, out Guid requestedId) && User.HasClaim("Permission", BookingRules.StaffPermission))
                userId = requestedId;

            using var dbContext = _dataService.GetDbContext();
            List<TrainGroup> trainGroups = await dbContext.TrainGroups
                .Include(x => x.Trainer)
                .Include(x => x.TrainGroupDates)
                .Include(x => x.TrainGroupUnavailableDates)
                .Include(x => x.TrainGroupParticipants)
                .ThenInclude(x => x.TrainGroupParticipantUnavailableDates)
                .Where(x => x.TrainGroupDates.Any(y =>
                    (y.FixedDay >= selectedDate && y.FixedDay < nextDay)
                    || y.RecurrenceDayOfMonth == selectedDate.Day
                    || y.RecurrenceDayOfWeek == selectedDate.DayOfWeek))
                .AsSplitQuery()
                .ToListAsync();

            List<TimeSlotResponseDto> timeSlotResponseDtos = trainGroups
                .Select(x => ToTimeSlot(x, selectedDate, userId))
                .ToList();

            return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotResponseDtos);
        }

        private TimeSlotResponseDto ToTimeSlot(TrainGroup trainGroup, DateTime day, Guid userId)
        {
            // The group date that runs on the day. A fixed day wins over a weekday, as
            // the two can never fall on the same date.
            TrainGroupDate sessionDate =
                trainGroup.TrainGroupDates.FirstOrDefault(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY && BookingRules.OccursOn(x, day))
                ?? trainGroup.TrainGroupDates.First(x => BookingRules.OccursOn(x, day));

            List<TrainGroupParticipant> mine = trainGroup.TrainGroupParticipants.Where(x => x.UserId == userId).ToList();
            TrainGroupParticipant? booked = mine.FirstOrDefault(x => BookingRules.IsBooked(x, day));
            TrainGroupParticipantUnavailableDate? skipped = booked?.TrainGroupParticipantUnavailableDates.FirstOrDefault(x => x.UnavailableDate.Date == day);
            TrainGroupUnavailableDate? cancelled = trainGroup.TrainGroupUnavailableDates.FirstOrDefault(x => x.UnavailableDate.Date == day);
            DateTime dayUtc = DateTime.SpecifyKind(day, DateTimeKind.Utc);

            // Every weekday (or day of the month) of the group, each marked with whether
            // the member already has a recurring booking on it that is still running.
            List<TimeSlotRecurrenceDateDto> recurrenceDates = trainGroup.TrainGroupDates
                .Where(x => x.TrainGroupDateType != TrainGroupDateTypeEnum.FIXED_DAY)
                .Select(x =>
                {
                    TrainGroupParticipant? running = mine.FirstOrDefault(y =>
                        y.SelectedDate == null
                        && y.TrainGroupDateId == x.Id
                        && (y.RecurringEndOnDate == null || y.RecurringEndOnDate.Value.Date > day));

                    return new TimeSlotRecurrenceDateDto()
                    {
                        TrainGroupDateId = x.Id,
                        TrainGroupDateType = x.TrainGroupDateType,
                        Date = x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                            ? new DateTime(2000, 1, 2 + (int)x.RecurrenceDayOfWeek!.Value)
                            : new DateTime(2000, 1, x.RecurrenceDayOfMonth!.Value),
                        RecurrenceDayOfWeek = (int?)x.RecurrenceDayOfWeek,
                        RecurrenceDayOfMonth = x.RecurrenceDayOfMonth,
                        IsUserJoined = running != null,
                        TrainGroupParticipantId = running?.Id
                    };
                })
                .ToList();

            // The day itself, as a single session.
            bool isFixedDay = sessionDate.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY;
            bool isBookedOnce = booked != null && !BookingRules.IsRecurring(booked);
            recurrenceDates.Add(new TimeSlotRecurrenceDateDto()
            {
                TrainGroupDateId = sessionDate.Id,
                TrainGroupDateType = isFixedDay ? TrainGroupDateTypeEnum.FIXED_DAY : null,
                Date = dayUtc,
                IsOneOff = true,
                IsUserJoined = isBookedOnce,
                TrainGroupParticipantId = isBookedOnce ? booked!.Id : null
            });

            return new TimeSlotResponseDto()
            {
                Id = trainGroup.Id,
                Title = trainGroup.Title,
                Description = trainGroup.Description,
                Duration = trainGroup.Duration,
                StartOn = trainGroup.StartOn,
                TrainerId = trainGroup.TrainerId,
                Trainer = _mapper.Map<UserDto>(trainGroup.Trainer),
                TrainerFullName = (trainGroup.Trainer.FirstName + " " + trainGroup.Trainer.LastName).Trim(),
                TrainGroupId = trainGroup.Id,
                TrainGroupDateId = sessionDate.Id,
                MaxParticipants = trainGroup.MaxParticipants,
                SpotsLeft = trainGroup.MaxParticipants - trainGroup.TrainGroupParticipants.Count(x => BookingRules.HoldsPlace(x, day)),
                IsUnavailableTrainGroup = cancelled != null,
                UnavailableTrainGroupId = cancelled?.Id,
                BookedParticipantId = booked?.Id,
                IsBookedRecurring = booked != null && BookingRules.IsRecurring(booked),
                SkippedUnavailableDateId = skipped?.Id,
                RecurrenceDates = recurrenceDates
            };
        }

        // POST: api/TrainGroupDate/TimeSlots
        //[HttpPost("TimeSlots")]
        //public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        //{
        //    DateTime selectedDate = timeSlotRequestDto.SelectedDate.Date;
        //    Guid userId = new Guid(timeSlotRequestDto.UserId);   // parse once (tiny CPU win, same result)

        //    using var dbContext = _dataService.GetDbContext();
        //    List<TrainGroupDate>? trainGroupDates = await dbContext.TrainGroupDates
        //     .AsNoTracking()                                      // keeps the big performance win
        //     .Include(x => x.TrainGroup.Trainer)
        //     .Include(x => x.TrainGroupParticipants)              // needed for RecurrenceDates IsUserJoined on TrainGroupDate level
        //     .Include(x => x.TrainGroup.TrainGroupUnavailableDates)
        //     .Include(x => x.TrainGroup.TrainGroupParticipants)   // needed for SpotsLeft
        //         .ThenInclude(x => x.TrainGroupParticipantUnavailableDates)
        //     .Where(x =>
        //         x.FixedDay == selectedDate
        //         || x.RecurrenceDayOfMonth == selectedDate.Day
        //         || x.RecurrenceDayOfWeek == selectedDate.DayOfWeek)
        //     .AsSplitQuery()                                      // prevents the huge Cartesian-product query
        //     .ToListAsync();

        //    List<TimeSlotResponseDto>? timeSlotRequestDtos = trainGroupDates
        //        .GroupBy(x => x.TrainGroup)
        //        .Select(x => new TimeSlotResponseDto()
        //        {
        //            Title = x.Key.Title,
        //            Description = x.Key.Description,
        //            Duration = x.Key.Duration,
        //            StartOn = x.Key.StartOn,
        //            TrainerId = x.Key.TrainerId,
        //            Trainer = _mapper.Map<UserDto>(x.Key.Trainer),
        //            TrainGroupId = x.Key.Id,
        //            IsUnavailableTrainGroup = x.Key.TrainGroupUnavailableDates.Any(y => y.UnavailableDate == timeSlotRequestDto.SelectedDate),
        //            UnavailableTrainGroupId = x.Key.TrainGroupUnavailableDates.FirstOrDefault(y => y.UnavailableDate == timeSlotRequestDto.SelectedDate)?.Id,
        //            //IsUnavailableTrainGroup = false,
        //            //UnavailableTrainGroupId = null,
        //            RecurrenceDates = x.Key.TrainGroupDates
        //                .Where(y => y.RecurrenceDayOfMonth.HasValue || y.RecurrenceDayOfWeek.HasValue)
        //                .Select(y =>
        //                    new TimeSlotRecurrenceDateDto()
        //                    {
        //                        TrainGroupDateId = y.Id,
        //                        TrainGroupDateType = y.TrainGroupDateType,
        //                        Date = y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
        //                            ? new DateTime(2000, 1, 2 + (int)y.RecurrenceDayOfWeek!.Value)
        //                            : new DateTime(2000, 1, y.RecurrenceDayOfMonth!.Value),
        //                        IsUserJoined = y.TrainGroupParticipants.Where(z => z.SelectedDate == null).Any(z => z.UserId == new Guid(timeSlotRequestDto.UserId)),
        //                    }
        //                )
        //                .Concat(
        //                    x.Key.TrainGroupDates
        //                    .Where(y => y.FixedDay.HasValue)
        //                    .Where(y => y.FixedDay == selectedDate)
        //                    .Select(y =>
        //                        new TimeSlotRecurrenceDateDto()
        //                        {
        //                            TrainGroupDateId = y.Id,
        //                            TrainGroupDateType = y.TrainGroupDateType,
        //                            Date = y.FixedDay!.Value,
        //                            IsUserJoined = y.TrainGroupParticipants.Any(z => z.UserId == new Guid(timeSlotRequestDto.UserId)),
        //                        }
        //                    )
        //                )
        //                .Concat(
        //                        x.Key.TrainGroupDates.Any(y => y.FixedDay == selectedDate)
        //                        ?
        //                            new List<TimeSlotRecurrenceDateDto>()
        //                        :
        //                            new List<TimeSlotRecurrenceDateDto>()
        //                            {
        //                                new TimeSlotRecurrenceDateDto()
        //                                {
        //                                    TrainGroupDateId = x.Key.TrainGroupDates.First(y =>
        //                                        y.FixedDay == selectedDate
        //                                        || y.RecurrenceDayOfMonth== selectedDate.Day
        //                                        || y.RecurrenceDayOfWeek == selectedDate.DayOfWeek)
        //                                        .Id,
        //                                    TrainGroupDateType = null,
        //                                    Date = selectedDate,
        //                                    IsUserJoined = x.Key.TrainGroupParticipants
        //                                        .Where(y => y.UserId == new Guid(timeSlotRequestDto.UserId))
        //                                        .Where(y => y.SelectedDate.HasValue)
        //                                        .Where(y => y.SelectedDate == selectedDate)
        //                                        .Any(),
        //                                }
        //                            }
        //                )
        //                .ToList(),
        //            SpotsLeft =
        //            (
        //                x.Key.MaxParticipants -
        //                x.Key.TrainGroupParticipants
        //                        .Where(y => !y.TrainGroupParticipantUnavailableDates.Any(z => z.TrainGroupParticipantId == y.Id && z.UnavailableDate == selectedDate))
        //                        .Where(y =>
        //                          y.SelectedDate != null ?
        //                                (y.SelectedDate == selectedDate)
        //                            :
        //                                (y.TrainGroupDate.FixedDay == selectedDate
        //                                || y.TrainGroupDate.RecurrenceDayOfWeek == selectedDate.DayOfWeek
        //                                || y.TrainGroupDate.RecurrenceDayOfMonth == selectedDate.Month)
        //                        )
        //                        //.Select(y => y.UserId)
        //                        //.Distinct()
        //                        .Count()
        //            ),
        //        })
        //        .ToList();

        //    return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        //}

        //    [HttpPost("TimeSlots")]
        //    public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots(
        //[FromBody] TimeSlotRequestDto timeSlotRequestDto)
        //    {
        //        using var dbContext = _dataService.GetDbContext();

        //        Guid userId = new Guid(timeSlotRequestDto.UserId);
        //        DateTime selectedDate = timeSlotRequestDto.SelectedDate.Date;

        //        List<TrainGroup> trainGroups = await dbContext.TrainGroups
        //            .AsNoTracking()
        //            .AsSplitQuery()
        //            .Include(g => g.Trainer)
        //            .Include(g => g.TrainGroupUnavailableDates)
        //            .Include(g => g.TrainGroupDates)
        //                .ThenInclude(d => d.TrainGroupParticipants)
        //                    .ThenInclude(p => p.TrainGroupParticipantUnavailableDates)
        //            // Only groups that have at least one date matching the selected day
        //            // and at least one participant on that date
        //            .Where(g => g.TrainGroupDates.Any(d =>
        //                d.FixedDay == selectedDate
        //                || d.RecurrenceDayOfMonth == selectedDate.Day
        //                || d.RecurrenceDayOfWeek == selectedDate.DayOfWeek))
        //            .ToListAsync();

        //        List<TimeSlotResponseDto> timeSlotRequestDtos = trainGroups
        //            .Select(g =>
        //            {
        //                List<TrainGroupDate> groupDates = g.TrainGroupDates.ToList();
        //                bool hasFixedDay = groupDates.Any(d => d.FixedDay == selectedDate);

        //                return new TimeSlotResponseDto
        //                {
        //                    Title = g.Title,
        //                    Description = g.Description,
        //                    Duration = g.Duration,
        //                    StartOn = g.StartOn,
        //                    TrainerId = g.TrainerId,
        //                    Trainer = _mapper.Map<UserDto>(g.Trainer),
        //                    TrainGroupId = g.Id,

        //                    IsUnavailableTrainGroup = g.TrainGroupUnavailableDates
        //                        .Any(y => y.UnavailableDate == timeSlotRequestDto.SelectedDate),

        //                    UnavailableTrainGroupId = g.TrainGroupUnavailableDates
        //                        .FirstOrDefault(y => y.UnavailableDate == timeSlotRequestDto.SelectedDate)?.Id,

        //                    RecurrenceDates = groupDates
        //                        // Recurring slots
        //                        .Where(y => y.RecurrenceDayOfMonth.HasValue || y.RecurrenceDayOfWeek.HasValue)
        //                        .Select(y => new TimeSlotRecurrenceDateDto
        //                        {
        //                            TrainGroupDateId = y.Id,
        //                            TrainGroupDateType = y.TrainGroupDateType,
        //                            Date = y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
        //                                ? new DateTime(2000, 1, 2 + (int)y.RecurrenceDayOfWeek!.Value)
        //                                : new DateTime(2000, 1, y.RecurrenceDayOfMonth!.Value),
        //                            IsUserJoined = y.TrainGroupParticipants
        //                                .Where(z => z.SelectedDate == null)
        //                                .Any(z => z.UserId == userId),
        //                        })
        //                        // Fixed-day slots for the selected date
        //                        .Concat(
        //                            groupDates
        //                                .Where(y => y.FixedDay.HasValue && y.FixedDay == selectedDate)
        //                                .Select(y => new TimeSlotRecurrenceDateDto
        //                                {
        //                                    TrainGroupDateId = y.Id,
        //                                    TrainGroupDateType = y.TrainGroupDateType,
        //                                    Date = y.FixedDay!.Value,
        //                                    IsUserJoined = y.TrainGroupParticipants
        //                                        .Any(z => z.UserId == userId),
        //                                })
        //                        )
        //                        // One-off slot: only when there is NO fixed-day date for this day
        //                        .Concat(
        //                            hasFixedDay
        //                                ? new List<TimeSlotRecurrenceDateDto>()
        //                                : new List<TimeSlotRecurrenceDateDto>
        //                                {
        //                            new TimeSlotRecurrenceDateDto
        //                            {
        //                                TrainGroupDateId = groupDates
        //                                    .First(y =>
        //                                        y.FixedDay == selectedDate
        //                                        || y.RecurrenceDayOfMonth == selectedDate.Day
        //                                        || y.RecurrenceDayOfWeek == selectedDate.DayOfWeek)
        //                                    .Id,
        //                                TrainGroupDateType = null,
        //                                Date         = selectedDate,
        //                                IsUserJoined = g.TrainGroupParticipants
        //                                    .Where(y => y.UserId == userId)
        //                                    .Where(y => y.SelectedDate.HasValue)
        //                                    .Any(y => y.SelectedDate == selectedDate),
        //                            }
        //                                }
        //                        )
        //                        .ToList(),

        //                    SpotsLeft =
        //                        g.MaxParticipants -
        //                        g.TrainGroupParticipants
        //                            .Where(y => !y.TrainGroupParticipantUnavailableDates
        //                                .Any(z => z.TrainGroupParticipantId == y.Id
        //                                       && z.UnavailableDate == selectedDate))
        //                            .Where(y =>
        //                                y.SelectedDate != null
        //                                    ? y.SelectedDate == selectedDate
        //                                    : y.TrainGroupDate.FixedDay == selectedDate
        //                                      || y.TrainGroupDate.RecurrenceDayOfWeek == selectedDate.DayOfWeek
        //                                      || y.TrainGroupDate.RecurrenceDayOfMonth == selectedDate.Day) 
        //                            .Count()
        //                };
        //            })
        //            .ToList();

        //        return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        //    }

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
