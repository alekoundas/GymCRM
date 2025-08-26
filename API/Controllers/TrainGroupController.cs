using AutoMapper;
using Business;
using Business.Services;
using Core.Dtos;
using Core.Dtos.TrainGroup;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupController : GenericController<TrainGroup, TrainGroupDto, TrainGroupAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupController> _logger;

        public TrainGroupController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        // PUT: api/controller/5
        [HttpPut("{id}")]
        public override async Task<ActionResult<ApiResponse<TrainGroup>>> Put(int id, [FromBody] TrainGroupDto entityDto)
        {
            if (!IsUserAuthorized("Edit"))
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", "User is not authorized to perform this action.");

            if (!ModelState.IsValid)
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", "Invalid data provided.");

            TrainGroup entity = _mapper.Map<TrainGroup>(entityDto);


            ApiDbContext dbContext = _dataService.GetDbContext();

            // Load existing entity with related data
            TrainGroup? existingEntity = await dbContext.Set<TrainGroup>()
                .Include(x => x.TrainGroupDates)
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (existingEntity == null)
            {
                string className = typeof(TrainGroup).Name;
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Requested {className} not found!");
            }


            // Update scalar properties
            dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);

            // Map incoming TrainGroupDates to existing ones
            var incomingDates = entity.TrainGroupDates.ToList();
            var existingDates = existingEntity.TrainGroupDates.ToList();

            // Remove deleted TrainGroupDates
            foreach (var existingDate in existingDates)
            {
                if (!incomingDates.Any(d => d.Id == existingDate.Id && d.Id > 0))
                {
                    dbContext.Remove(existingDate);
                }
            }

            // Update or add TrainGroupDates
            foreach (var incomingDate in incomingDates)
            {
                var existingDate = existingDates.FirstOrDefault(d => d.Id == incomingDate.Id && incomingDate.Id > 0);
                if (existingDate != null)
                {
                    // Update existing TrainGroupDate
                    dbContext.Entry(existingDate).CurrentValues.SetValues(incomingDate);
                }
                else
                {
                    // Add new TrainGroupDate
                    incomingDate.Id = 0;
                    incomingDate.TrainGroupId = entity.Id;
                    dbContext.Add(incomingDate);
                    existingEntity.TrainGroupDates.Add(incomingDate);
                }




            }
                await dbContext.SaveChangesAsync();
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity);
        }


        protected override bool CustomValidatePOST(TrainGroupAddDto entityAddDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            foreach (var groupDate in entityAddDto.TrainGroupDates)
            {
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.FIXED_DAY && groupDate.FixedDay == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.DAY_OF_MONTH && groupDate.RecurrenceDayOfMonth == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.DAY_OF_WEEK && groupDate.RecurrenceDayOfWeek == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
            }
            errors = errorList.ToArray();
            return errors.Length > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupDto entityAddDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            foreach (var groupDate in entityAddDto.TrainGroupDates)
            {
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.FIXED_DAY && groupDate.FixedDay == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.DAY_OF_MONTH && groupDate.RecurrenceDayOfMonth == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
                if (groupDate.TrainGroupDateType == Core.Enums.TrainGroupDateTypeEnum.DAY_OF_WEEK && groupDate.RecurrenceDayOfWeek == null)
                    errorList.Add("Each TrainGroupDate must have at least one of FixedDay, RecurrenceDayOfMonth, or RecurrenceDayOfWeek set.");
            }
            errors = errorList.ToArray();
            return errors.Length > 0;
        }

    }
}
