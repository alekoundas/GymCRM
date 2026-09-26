using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.Lookup;
using Core.Dtos.TrainGroupCategory;
using Core.Models;
using Core.System;
using Core.Translations;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TrainGroupCategoriesController : GenericController<TrainGroupCategory, TrainGroupCategoryDto, TrainGroupCategoryAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IStringLocalizer _localizer;

        public TrainGroupCategoriesController(
            IDataService dataService,
            IMapper mapper,
            IStringLocalizer localizer) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _localizer = localizer;
        }

        // POST: api/TrainGroupCategories/Lookup
        // The train group form's dropdown and the admin calendar's tabs.
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            IQueryable<TrainGroupCategory> query = context.TrainGroupCategories.AsNoTracking();

            if (lookupDto.Filter.Id.Length > 0 && int.TryParse(lookupDto.Filter.Id, out int filterId))
                query = query.Where(x => x.Id == filterId);

            if (lookupDto.Filter.Value.Length > 0)
                query = query.Where(x => TextNormalizer.Normalize(x.Name).Contains(TextNormalizer.Normalize(lookupDto.Filter.Value)));

            lookupDto.TotalRecords = await query.CountAsync();

            lookupDto.Data = await query
                .OrderBy(x => x.Name)
                .Skip(lookupDto.Skip)
                .Take(lookupDto.Take)
                .Select(x => new LookupOptionDto() { Id = x.Id.ToString(), Value = x.Name })
                .ToListAsync();

            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }

        protected override bool CustomValidatePOST(TrainGroupCategoryAddDto entityDto, out string[] errors) =>
            IsDuplicate(entityDto.Name, null, out errors);

        protected override bool CustomValidatePUT(TrainGroupCategoryDto entityDto, out string[] errors) =>
            IsDuplicate(entityDto.Name, entityDto.Id, out errors);

        // Two tabs with the same name on the calendar would be no use to anybody.
        private bool IsDuplicate(string name, int? excludeId, out string[] errors)
        {
            errors = Array.Empty<string>();
            using ApiDbContext context = _dataService.GetDbContext();

            string normalized = TextNormalizer.Normalize(name.Trim());
            bool exists = context.TrainGroupCategories
                .Where(x => excludeId == null || x.Id != excludeId)
                .Any(x => TextNormalizer.Normalize(x.Name) == normalized);

            if (exists)
                errors = [_localizer[TranslationKeys._0_already_exists, name.Trim()]];

            return exists;
        }
    }
}
