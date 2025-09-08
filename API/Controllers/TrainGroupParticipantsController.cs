using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Enums;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupParticipantsController :   GenericController<TrainGroupParticipant, TrainGroupParticipantDto, TrainGroupParticipantAddDto>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupParticipantsController(IDataService dataService, IMapper mapper): base(dataService, mapper)
        {
            _dataService = dataService;
            _mapper = mapper;
        }


        // Handle Booking. 
        // PUT: api/controller/5
        [HttpPost("UpdateParticipants")]
        public async Task<ActionResult<ApiResponse<TrainGroup>>> UpdateParticipants([FromBody] TrainGroupParticipantUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", "Invalid data provided.");


            ApiDbContext dbContext = _dataService.GetDbContext();

            // Load existing entity with related data
            TrainGroup? existingEntity = await dbContext.Set<TrainGroup>()
                .Include(x => x.TrainGroupParticipants)
                .Include(x => x.TrainGroupDates)
                .ThenInclude(x => x.TrainGroupParticipants)
                .Where(x => x.Id == updateDto.TrainGroupId)
                .FirstOrDefaultAsync();

            if (existingEntity == null)
            {
                string className = typeof(TrainGroup).Name;
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Requested {className} not found!");
            }


            // Validate user selection.
            // Cant have curent date selected with a recurring date be the same date.
            bool isCurrentDateWithConflict = updateDto.TrainGroupParticipantDtos
                .Where(x => x.SelectedDate != null)
                .Any(x => updateDto.TrainGroupParticipantDtos
                    .Where(y => y.SelectedDate == null)
                    .Any(y => existingEntity.TrainGroupDates
                        .Where(z => z.Id == y.TrainGroupDateId)
                        .Any(z =>z.RecurrenceDayOfMonth?.Day == x.SelectedDate?.Day || z.RecurrenceDayOfWeek?.DayOfWeek == x.SelectedDate?.DayOfWeek
                        )
                    )
                );

            if (isCurrentDateWithConflict)
            {
                dbContext.Dispose();
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Current date is already selected in a Recurrence date! Please select one of them.");
            }





            List<TrainGroupParticipant> incomingParticipants = _mapper.Map<List<TrainGroupParticipant>>(updateDto.TrainGroupParticipantDtos);
            List<TrainGroupParticipant> existingTrainGroupParticipants = existingEntity.TrainGroupParticipants
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .Where(x => x.SelectedDate == updateDto.SelectedDate)
                .ToList();
            List<TrainGroupParticipant> existingTrainGroupDateParticipants = existingEntity.TrainGroupDates
                .SelectMany(x => x.TrainGroupParticipants)
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .ToList();

            // Remove deleted TrainGroupParticipants
            foreach (TrainGroupParticipant existingParticipant in existingTrainGroupDateParticipants)
            {
                TrainGroupParticipant? incomingParticipant = incomingParticipants.FirstOrDefault(x =>
                    x.UserId == existingParticipant.UserId
                    && x.TrainGroupId == existingParticipant.TrainGroupId
                    && x.SelectedDate == existingParticipant.SelectedDate
                    && x.TrainGroupDateId == existingParticipant.TrainGroupDateId);

                if (incomingParticipant != null)
                    incomingParticipants.Remove(incomingParticipant);
                else
                    dbContext.Remove(existingParticipant);
            }

            // Remove deleted TrainGroupDateParticipants
            foreach (TrainGroupParticipant existingParticipant in existingTrainGroupParticipants)
            {
                bool alreadyExists = incomingParticipants.Any(x =>
                    x.UserId == existingParticipant.UserId
                    && x.TrainGroupId == existingParticipant.TrainGroupId
                    && x.SelectedDate == existingParticipant.SelectedDate
                    && x.TrainGroupDateId == existingParticipant.TrainGroupDateId);

                if (alreadyExists)
                    incomingParticipants.Remove(incomingParticipants.First(x => x.Id == existingParticipant.Id));
                else
                    dbContext.Remove(existingParticipant);
            }



            // Validate Add participants for MaxParticipant
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {

                List<TrainGroupParticipant> sss = dbContext.Set<TrainGroupParticipant>()
                        .Include(x => x.TrainGroupDate)
                        .Include(x => x.TrainGroup)
                        .Where(x => x.TrainGroupId == updateDto.TrainGroupId)
                        .ToList()
                        .Where(x => incomingParticipant.SelectedDate.HasValue
                            ?
                                incomingParticipant.SelectedDate == x.SelectedDate
                                || incomingParticipant.SelectedDate == x.TrainGroupDate?.FixedDay
                                || incomingParticipant.SelectedDate.Value.Day == x.TrainGroupDate?.RecurrenceDayOfMonth?.Day
                                || incomingParticipant.SelectedDate.Value.DayOfWeek == x.TrainGroupDate?.RecurrenceDayOfWeek?.DayOfWeek
                            : true
                        )
                        .Where(x => incomingParticipant.TrainGroupDateId != null
                            ? incomingParticipant.TrainGroupDateId == x.TrainGroupDateId
                                || existingEntity.TrainGroupDates.First(y => y.Id == incomingParticipant.TrainGroupDateId).RecurrenceDayOfMonth?.Day == x.SelectedDate?.Day
                                || existingEntity.TrainGroupDates.First(y => y.Id == incomingParticipant.TrainGroupDateId).RecurrenceDayOfWeek?.DayOfWeek == x.SelectedDate?.DayOfWeek
                                || existingEntity.TrainGroupDates.First(y => y.Id == incomingParticipant.TrainGroupDateId).FixedDay == x.SelectedDate
                            : true
                        )
                        .ToList();

                if (sss.Count() >= existingEntity.MaxParticipants)
                {
                    dbContext.Dispose();
                    return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Maximum amount of participants reached for ");
                }

            }

            List<TrainGroupParticipant> futureUnavailableDates = new List<TrainGroupParticipant>();

            // Add TrainGroup Participants
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {

                // Add new Participant
                incomingParticipant.Id = 0;
                dbContext.Add(incomingParticipant);


                // Check which future dates user cant book.
                if (incomingParticipant.SelectedDate == null)
                {
                    futureUnavailableDates =
                    dbContext.Set<TrainGroupParticipant>()
                        .Include(x => x.TrainGroupDate)
                        .Include(x => x.TrainGroup)
                        .Where(x => x.TrainGroupId == updateDto.TrainGroupId)
                        .Where(x => x.SelectedDate != null)
                        .Where(x => x.SelectedDate >= DateTime.UtcNow)
                        .ToList()
                        .Where(x =>
                            x.TrainGroup.MaxParticipants -
                            (
                                x.TrainGroup.TrainGroupDates
                                    .Where(y =>
                                       (y.RecurrenceDayOfMonth?.Day == x.SelectedDate!.Value.Day) ||
                                       (y.RecurrenceDayOfWeek?.DayOfWeek == x.SelectedDate!.Value.DayOfWeek)
                                    )
                                    .SelectMany(y =>
                                    y.TrainGroupParticipants
                                    )
                                    .Count()
                                +
                                x.TrainGroup.TrainGroupParticipants
                                   .Where(y =>
                                       y.TrainGroupDate?.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                                       || y.TrainGroupDate?.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_MONTH
                                   )
                                   .Where(y =>
                                       (y.TrainGroupDate?.RecurrenceDayOfMonth?.Day == x.SelectedDate!.Value.Day) ||
                                       (y.TrainGroupDate?.RecurrenceDayOfWeek?.DayOfWeek == x.SelectedDate!.Value.DayOfWeek)
                                   )
                                   .Count()
                                )
                                <= 0
                        )
                        .ToList();
                }
            }


            await dbContext.SaveChangesAsync();
            dbContext.Dispose();

            if (futureUnavailableDates.Count > 0)

                return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity, "warning", "Future unavailable dates that cannot be booked: " + futureUnavailableDates.Select(x => x.SelectedDate?.ToUniversalTime().ToString()).ToList().ToString());

            return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity);
        }

        
        protected override bool CustomValidatePOST(TrainGroupParticipantAddDto entityAddDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            bool isAlreadyParticipant = _dataService.TrainGroupParticipants
                  .Where(x => x.UserId == new Guid(entityAddDto.UserId))
                  .Where(x => x.TrainGroupId == entityAddDto.TrainGroupId)
                  .Any(x =>
                      x.SelectedDate == entityAddDto.SelectedDate
                      || (x.TrainGroupDate != null && x.TrainGroupDate.RecurrenceDayOfWeek != null && entityAddDto.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfWeek.Value.DayOfWeek == entityAddDto.SelectedDate.Value.DayOfWeek)
                      || (x.TrainGroupDate != null && x.TrainGroupDate.RecurrenceDayOfMonth != null && entityAddDto.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfMonth.Value.Day == entityAddDto.SelectedDate.Value.Day)
                  );

            if (isAlreadyParticipant)
                errorList.Add("Participant already joined!");

            errors = errorList.ToArray();
            return errors.Length > 0;
        }

        protected override bool CustomValidatePUT(TrainGroupParticipantDto entityDto, out string[] errors)
        {
            List<string> errorList = new List<string>();

            bool isAlreadyParticipant = _dataService.TrainGroupParticipants
                  .Where(x => x.UserId == new Guid(entityDto.UserId))
                  .Where(x => x.TrainGroupId == entityDto.TrainGroupId)
                  .Any(x =>
                      x.SelectedDate == entityDto.SelectedDate
                      || (x.TrainGroupDate != null && x.TrainGroupDate.RecurrenceDayOfWeek != null && entityDto.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfWeek.Value.DayOfWeek == entityDto.SelectedDate.Value.DayOfWeek)
                      || (x.TrainGroupDate != null && x.TrainGroupDate.RecurrenceDayOfMonth != null && entityDto.SelectedDate != null && x.TrainGroupDate.RecurrenceDayOfMonth.Value.Day == entityDto.SelectedDate.Value.Day)
                  );

            if (isAlreadyParticipant)
                errorList.Add("Participant already joined!");

            errors = errorList.ToArray();
            return errors.Length > 0;
        }



        //// POST: api/controller/GetDataTable
        //[HttpPost("GetDataTable")]
        //public async Task<ApiResponse<DataTableDto<TrainGroupParticipantDto>>> GetDataTable([FromBody] DataTableDto<TrainGroupParticipantDto> dataTable)
        //{
        //    var query = _dataService.GetGenericRepository<TrainGroupParticipant>();

        //    // Handle Sorting of DataTable.
        //    if (dataTable.Sorts.Count() > 0)
        //    {
        //        // Create the first OrderBy().
        //        DataTableSortDto? dataTableSort = dataTable.Sorts.First();
        //        if (dataTableSort.Order > 0)
        //            query.OrderBy(dataTableSort.FieldName.Substring(0, 1).ToUpper() + dataTableSort.FieldName.Substring(1, dataTableSort.FieldName.Length), OrderDirectionEnum.ASCENDING);
        //        else if (dataTableSort.Order < 0)
        //            query.OrderBy(dataTableSort.FieldName.Substring(0, 1).ToUpper() + dataTableSort.FieldName.Substring(1, dataTableSort.FieldName.Length), OrderDirectionEnum.DESCENDING);

        //        // Create the rest OrderBy methods as ThenBy() if any.
        //        foreach (var sortInfo in dataTable.Sorts.Skip(1))
        //        {
        //            if (dataTableSort.Order > 0)
        //                query.ThenBy(sortInfo.FieldName.Substring(0, 1).ToUpper() + sortInfo.FieldName.Substring(1, sortInfo.FieldName.Length), OrderDirectionEnum.ASCENDING);
        //            else if (dataTableSort.Order < 0)
        //                query.ThenBy(sortInfo.FieldName.Substring(0, 1).ToUpper() + sortInfo.FieldName.Substring(1, sortInfo.FieldName.Length), OrderDirectionEnum.DESCENDING);
        //        }
        //    }

        //    // Handle Filtering of DataTable.
        //    foreach (var filter in dataTable.Filters.Where(x => x.FilterType == DataTableFiltersEnum.equals))
        //        query.FilterEqualsByColumn(filter.FieldName, filter.Value);

        //    foreach (var filter in dataTable.Filters.Where(x => x.FilterType == DataTableFiltersEnum.contains))
        //        query.FilterByColumnContains(filter.FieldName, filter.Value);

        //    // Handle Pagging of DataTable.
        //    int skip = (dataTable.Page - 1) * dataTable.Rows;
        //    int take = dataTable.Rows;
        //    query.AddPagging(skip, take);



        //    // Retrieve Data.
        //    List<TrainGroupParticipant> result = await query.ToListAsync();
        //    List<TrainGroupParticipantDto> customerDto = _mapper.Map<List<TrainGroupParticipantDto>>(result);

        //    //TODO add filter
        //    int rows = await _dataService
        //        .GetGenericRepository<TrainGroupParticipant>()
        //        //.Where()
        //        .CountAsync();

        //    dataTable.Data = customerDto;
        //    dataTable.PageCount = rows;

        //    return new ApiResponse<DataTableDto<TrainGroupParticipantDto>>().SetSuccessResponse(dataTable);
        //}
    }
}
