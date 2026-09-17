using AutoMapper;
using Business.Repository;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Lookup;
using Core.Dtos.TrainGroup;
using Core.Enums;
using Core.Models;
using Core.System;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TrainGroupsController : GenericController<TrainGroup, TrainGroupDto, TrainGroupAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;
        //private readonly ILogger<TrainGroupController> _logger;

        public TrainGroupsController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _mapper = mapper;
            _localizer = localizer;
        }

        protected override bool CustomValidatePOST(TrainGroupAddDto entityDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            // Check for date mixing
            bool isDayOfWeekAndMonthMixing =
                entityDto.TrainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                && entityDto.TrainGroupDates.Any(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH);
            if (isDayOfWeekAndMonthMixing)
                errorList.Add(_localizer[TranslationKeys.Day_of_week_and_day_of_month_doesnt_mix]);



            // Check for fixed date validity
            bool isFixedDateValid =
                entityDto.TrainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                .Any(x =>
                    entityDto.TrainGroupDates
                        .Where(y => y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK)
                        .Any(y => x.FixedDay!.Value.DayOfWeek == y.RecurrenceDayOfWeek)
                );
            if (isFixedDateValid)
                errorList.Add(_localizer[TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_week_entry, ""]);



            // Check for fixed date validity
            isFixedDateValid =
                entityDto.TrainGroupDates
                .Where(x => x.TrainGroupDateType == TrainGroupDateTypeEnum.FIXED_DAY)
                .Any(x =>
                    entityDto.TrainGroupDates
                        .Where(y => y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH)
                        .Any(y => x.FixedDay!.Value.Day == y.RecurrenceDayOfMonth)
                );
            if (isFixedDateValid)
                errorList.Add(_localizer[TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_month_entry, ""]);



            // Check for duplicates Dates
            var duplicates = entityDto.TrainGroupDates
               .GroupBy(x => new { x.RecurrenceDayOfWeek, x.RecurrenceDayOfMonth, x.FixedDay }) // Group by composite key
               .Where(g => g.Count() > 1)                                                       // Find groups with more than one item
               .ToList();
            if (duplicates.Count() > 0)
                errorList.Add(_localizer[TranslationKeys.Duplicate_train_group_date_found]);



            // Check for duplicate participants
            var duplicateParticipants = entityDto.TrainGroupParticipants
               .GroupBy(x => new { x.SelectedDate, x.TrainGroupDateId }) // Group by composite key
               .Where(g => g.Count() > 1)                               // Find groups with more than one item
               .ToList();
            if (duplicates.Count() > 0)
                errorList.Add(_localizer[TranslationKeys.Duplicate_participant_found]);


            // Validate Participant Selected Date.
            if (entityDto.TrainGroupParticipants.Any())
                foreach (var trainGroupDate in entityDto.TrainGroupDates)
                {
                    if (trainGroupDate.TrainGroupDateType != TrainGroupDateTypeEnum.FIXED_DAY)
                    {
                        // Check for TrainGroup participant selected date validity
                        bool isTrainGroupParticipantValid =
                            !trainGroupDate
                            .TrainGroupParticipants
                            .Where(x => x.SelectedDate.HasValue)
                            .Any(x =>
                                    x.SelectedDate!.Value.Day == trainGroupDate.RecurrenceDayOfMonth ||
                                    x.SelectedDate!.Value.DayOfWeek == trainGroupDate.RecurrenceDayOfWeek
                            );

                        if (isTrainGroupParticipantValid)
                            errorList.Add(_localizer[TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates]);
                    }
                    else
                    {
                        // Check for selected date NOT equal to fixed date.
                        bool isTrainGroupParticipantValid = trainGroupDate.TrainGroupParticipants.Any(x => x.SelectedDate.HasValue);

                        if (isTrainGroupParticipantValid)
                            errorList.Add(_localizer[TranslationKeys.Fixed_date_doesnt_allow_one_off_participants]);
                    }

                }

            errors = errorList.ToArray();
            return errors.Length > 0;
        }


        // POST: api/TrainGroups/Lookup
        // What the dropdowns read. Every other lookup in the app is written out per
        // controller like this, because each one decides what its own label is.
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            IQueryable<TrainGroup> query = context.TrainGroups.AsNoTracking();

            if (lookupDto.Filter.Id.Length > 0 && int.TryParse(lookupDto.Filter.Id, out int filterId))
                query = query.Where(x => x.Id == filterId);

            if (lookupDto.Filter.Value.Length > 0)
                query = query.Where(x => TextNormalizer.Normalize(x.Title).Contains(TextNormalizer.Normalize(lookupDto.Filter.Value)));

            lookupDto.TotalRecords = await query.CountAsync();

            lookupDto.Data = await query
                .OrderBy(x => x.Title)
                .Skip(lookupDto.Skip)
                .Take(lookupDto.Take)
                .Select(x => new LookupOptionDto()
                {
                    Id = x.Id.ToString(),
                    Value = x.Title
                })
                .ToListAsync();

            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }


        protected override void DataTableQueryUpdate(IGenericRepository<TrainGroup> query, DataTableDto<TrainGroupDto> dataTable)
        {
            query = query.Include(x => x.Trainer);
        }
    }
}
