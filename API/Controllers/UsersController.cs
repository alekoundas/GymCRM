using AutoMapper;
using Azure;
using Business.Services;
using Core.Dtos;
using Core.Dtos.AutoComplete;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Dtos.Lookup;
using Core.Dtos.TrainGroupDate;
using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using static Google.Apis.Requests.BatchRequest;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;

        public UsersController(
            IDataService dataService,
            IMapper mapper,
            ILogger<UsersController> logger,
            IUserService userService,
            UserManager<User> userManager)
        {
            _dataService = dataService;
            _logger = logger;
            _userService = userService;
            _userManager = userManager;
            _mapper = mapper;
        }


        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<UserDto>> Get(string? id)
        {
            if (id == null)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "User ID not set!");

            User? user = await _dataService.Users
                .Include(x => x.UserRoles)
                .ThenInclude<UserRole, Role>(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == new Guid(id));


            if (user == null)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "User not found!");

            UserDto userDto = _mapper.Map<UserDto>(user);
            return new ApiResponse<UserDto>().SetSuccessResponse(userDto);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]

        public async Task<ApiResponse<UserDto>> Update(string id, UserDto request)
        {
            if (id != request.Id)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "ID mismatch");

            if (request.UserRoles.Count() == 0)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "Role is required!");

            User? user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == new Guid(request.Id));
            if (user == null)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "User not found");

            string? roleName = request.UserRoles[0].Role.Name;
            bool roleExists = await _dataService.Roles.AnyAsync(x => x.Name == roleName);
            if (!roleExists)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "Role doesnt exist!");

            // Update user role.
            await _userService.AssignSingleRoleAsync(user, roleName!);

            // Update user properties.
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.UserName = request.UserName;
            user.Email = request.Email; // Only admin can update email.
            user.UserName = request.UserName;
            user.ProfileImage = request.ProfileImage;

            UserDto userDto = _mapper.Map<UserDto>(user);
            IdentityResult response = await _userManager.UpdateAsync(user);

            if (response.Succeeded)
                return new ApiResponse<UserDto>().SetSuccessResponse(userDto, "success", "User updated successfully");
            else
                return new ApiResponse<UserDto>().SetErrorResponse("error", response.Errors.First().Description);
        }


        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<ApiResponse<IdentityUser>> Delete(string? id)
        {
            if (id == null || id.Count() == 0)
                return new ApiResponse<IdentityUser>().SetErrorResponse("error", "User name not not set!");

            User? user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return new ApiResponse<IdentityUser>().SetErrorResponse("error", "User not found!");


            var response = await _userManager.DeleteAsync(user);
            if (response.Succeeded)
                return new ApiResponse<IdentityUser>().SetSuccessResponse("success", $"User {user.Email} deleted successfully");
            return new ApiResponse<IdentityUser>().SetErrorResponse("error", response.Errors.First().Description);

        }

        // POST: api/Users/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<UserDto>>> GetDataTable([FromBody] DataTableDto<UserDto> dataTable)
        {
            List<Expression<Func<User, bool>>>? filterQuery = new List<Expression<Func<User, bool>>>();

            var query = _dataService.Users
                .Include(x => x.UserRoles)
                .ThenInclude<UserRole, Role>(x => x.Role);

            // Handle Sorting of DataTable.
            if (dataTable.Sorts.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.Sorts.First();
                if (dataTableSort.Order > 0)
                    query.OrderBy(dataTableSort.FieldName, OrderDirectionEnum.ASCENDING);
                else if (dataTableSort.Order < 0)
                    query.OrderBy(dataTableSort.FieldName, OrderDirectionEnum.DESCENDING);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.Sorts.Skip(1))
                {
                    if (dataTableSort.Order > 0)
                        query.ThenBy(sortInfo.FieldName, OrderDirectionEnum.ASCENDING);
                    else if (dataTableSort.Order < 0)
                        query.ThenBy(sortInfo.FieldName, OrderDirectionEnum.DESCENDING);
                }
            }


            // Handle Filtering of DataTable.
            foreach (DataTableFilterDto filter in dataTable.Filters)
            {
                string fieldName = filter.FieldName.Substring(0, 1).ToUpper() + filter.FieldName.Substring(1, filter.FieldName.Length - 1);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.contains)
                    query.FilterByColumnContains(filter.FieldName, filter.Value.ToLower());

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.equals)
                    query.FilterByColumnEquals(filter.FieldName, filter.Value);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.notEquals)
                    query.FilterByColumnNotEquals(filter.FieldName, filter.Value);

                if (filter.Values?.Count() > 0 && filter.FilterType == DataTableFiltersEnum.@in)
                    query.FilterByColumnIn(filter.FieldName, filter.Values);

                if (filter.Values?.Count() == 2 && filter.FilterType == DataTableFiltersEnum.between)
                    query.FilterByColumnDateBetween(filter.FieldName, filter.Values[0], filter.Values[1]);

                if (filter.FilterType == DataTableFiltersEnum.custom)
                    if (fieldName == "RoleId" && filter.Values.Count() > 0)
                        query.Where(x => x.UserRoles.Any(y => filter.Values.Any(z => z == y.RoleId.ToString())));
            }


            // Handle pagination.
            int skip = dataTable.Page  * dataTable.Rows;
            int take = dataTable.Rows;
            query.AddPagging(skip, take);


            // Retrieve Data.
            List<User> result = await query.ToListAsync();
            List<UserDto> resultDto = _mapper.Map<List<UserDto>>(result);


            // Assign Id to RoleId
            //for (int i = 0; i < result.Count(); i++)
            //{
            //    string? roleName = (await _userManager.GetRolesAsync(result[i])).FirstOrDefault();
            //    Role? role = _roleManager.Roles.FirstOrDefault(x => x.Name == roleName);
            //    if (role != null)
            //        resultDto[i].RoleId = role.Id.ToString();
            //}



            foreach (var filter in dataTable.Filters)
            {
                string fieldName = filter.FieldName.Substring(0, 1).ToUpper() + filter.FieldName.Substring(1, filter.FieldName.Length - 1);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.contains)
                    query.FilterByColumnContains(filter.FieldName, filter.Value.ToLower());

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.equals)
                    query.FilterByColumnEquals(filter.FieldName, filter.Value);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.notEquals)
                    query.FilterByColumnNotEquals(filter.FieldName, filter.Value);

                if (filter.Values?.Count() > 0 && filter.FilterType == DataTableFiltersEnum.@in)
                    query.FilterByColumnIn(filter.FieldName, filter.Values);

                if (filter.Values?.Count() == 2 && filter.FilterType == DataTableFiltersEnum.between)
                    query.FilterByColumnDateBetween(filter.FieldName, filter.Values[0], filter.Values[1]);

                if (filter.FilterType == DataTableFiltersEnum.custom)
                    if (fieldName == "RoleId" && filter.Values.Count > 0)
                        query.Where(x => x.UserRoles.Any(y => filter.Values.Any(z => z == y.RoleId.ToString())));
            }


            int rowCount = await query.CountAsync();
            int totalRecords = rowCount;

            dataTable.Data = resultDto;
            dataTable.TotalRecords = totalRecords;
            dataTable.PageCount = (int)Math.Ceiling((double)totalRecords / dataTable.Rows);

            return new ApiResponse<DataTableDto<UserDto>>().SetSuccessResponse(dataTable);

        }

        // POST: api/Users/lookup
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {

            var query = _dataService.GetGenericRepository<User>();

            if (lookupDto.Filter.Id.Length > 0)
                query.Where(x => x.Id == new Guid(lookupDto.Filter.Id));

            if (lookupDto.Filter.Value.Length > 0)
                query.Where(x =>
                x.FirstName.ToLower().Contains(lookupDto.Filter.Value.ToLower()) ||
                x.LastName.ToLower().Contains(lookupDto.Filter.Value.ToLower()) ||
                x.Email.ToLower().Contains(lookupDto.Filter.Value.ToLower()) ||
                x.PhoneNumbers.Any(y => y.Number.Contains(lookupDto.Filter.Value.ToLower())) ||
                x.UserName!.ToLower().Contains(lookupDto.Filter.Value.ToLower())
            );

            // Handle Pagging.
            query.AddPagging(lookupDto.Skip, lookupDto.Take);

            // Retrieve Data.
            List<User> result = await query.ToListAsync();
            lookupDto.Data = result
              .Select(x =>
                  new LookupOptionDto()
                  {
                      Id = x.Id.ToString(),
                      Value = x.UserName,
                      ProfileImage = x.ProfileImage
                  })
              .ToList();




            if (lookupDto.Filter.Id.Length > 0)
                query.Where(x => x.Id == new Guid(lookupDto.Filter.Id));

            if (lookupDto.Filter.Value.Length > 0)
                query.Where(x =>
                x.FirstName.ToLower().Contains(lookupDto.Filter.Value.ToLower()) ||
                x.LastName.ToLower().Contains(lookupDto.Filter.Value.ToLower()) ||
                x.Email.ToLower().Contains(lookupDto.Filter.Value.ToLower()) ||
                x.PhoneNumbers.Any(y => y.Number.Contains(lookupDto.Filter.Value.ToLower())) ||
                x.UserName!.ToLower().Contains(lookupDto.Filter.Value.ToLower())
            );
            lookupDto.TotalRecords = await query.CountAsync();


            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }


        // POST: api/users/AutoComplete
        [HttpPost("AutoComplete")]
        public async Task<ApiResponse<AutoCompleteDto<UserDto>>> AutoComplete([FromBody] AutoCompleteDto<UserDto> autoCompleteDto)
        {
            var query = _dataService.GetGenericRepository<User>();
            string searchValue = autoCompleteDto.SearchValue.ToLower();
            if (autoCompleteDto.SearchValue.Length > 0)
                query.Where(x =>
                x.FirstName.ToLower().Contains(searchValue) ||
                x.LastName.ToLower().Contains(searchValue) ||
                x.Email.ToLower().Contains(searchValue) ||
                x.PhoneNumbers.Any(y => y.Number.Contains(searchValue)) ||
                x.UserName!.ToLower().Contains(searchValue)
            );

            // Handle Pagging.
            query.AddPagging(autoCompleteDto.Skip, autoCompleteDto.Take);

            // Retrieve Data.
            List<User> result = await query.ToListAsync();
            List<UserDto> customerDto = _mapper.Map<List<UserDto>>(result);

            if (autoCompleteDto.SearchValue.Length > 0)
                query.Where(x =>
                x.FirstName.ToLower().Contains(searchValue) ||
                x.LastName.ToLower().Contains(searchValue) ||
                x.Email.ToLower().Contains(searchValue) ||
                x.PhoneNumbers.Any(y => y.Number.Contains(searchValue)) ||
                x.UserName!.ToLower().Contains(searchValue)
            );

            autoCompleteDto.Suggestions = customerDto;
            autoCompleteDto.TotalRecords = await query.CountAsync();

            return new ApiResponse<AutoCompleteDto<UserDto>>().SetSuccessResponse(autoCompleteDto);
        }


        // POST: api/Users/TimeSlots
        [HttpPost("TimeSlots")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        {
            List<TrainGroupParticipant>? trainGroupParticipants = await _dataService.TrainGroupParticipants
                    .Include(x => x.TrainGroup)
                    .Include(x => x.TrainGroupDate)
                    .Where(x => x.UserId == new Guid(timeSlotRequestDto.UserId))
                    .Where(x => !x.TrainGroupParticipantUnavailableDates.Any(y =>
                        y.TrainGroupParticipantId == x.Id &&
                        y.UnavailableDate >= timeSlotRequestDto.SelectedDate &&
                        y.UnavailableDate <= timeSlotRequestDto.SelectedDate.AddDays(6)
                    ))
                    .Where(x =>
                        (
                            x.SelectedDate.HasValue &&
                            x.SelectedDate >= timeSlotRequestDto.SelectedDate &&
                            x.SelectedDate <= timeSlotRequestDto.SelectedDate.AddDays(6)
                        )
                        ||
                        (
                            x.TrainGroupDate != null &&
                            x.TrainGroupDate.FixedDay.HasValue &&
                            x.TrainGroupDate.FixedDay >= timeSlotRequestDto.SelectedDate &&
                            x.TrainGroupDate.FixedDay <= timeSlotRequestDto.SelectedDate.AddDays(6)
                        )
                        ||
                        (
                            x.TrainGroupDate != null &&
                            x.TrainGroupDate.RecurrenceDayOfMonth.HasValue &&
                            x.TrainGroupDate.RecurrenceDayOfMonth.Value.Date >= timeSlotRequestDto.SelectedDate.Date &&
                            x.TrainGroupDate.RecurrenceDayOfMonth.Value.Date <= timeSlotRequestDto.SelectedDate.Date.AddDays(6)
                        )
                        ||
                        (
                            x.TrainGroupDate != null &&
                            x.TrainGroupDate.RecurrenceDayOfWeek.HasValue
                        )
                    )
                    .ToListAsync(true);

            List<TimeSlotResponseDto>? timeSlotRequestDtos = trainGroupParticipants
                .Select(x => x.TrainGroup)
                .Distinct()
                .Select(x => new TimeSlotResponseDto()
                {
                    Title = x.Title,
                    Description = x.Description,
                    Duration = x.Duration,
                    StartOn = x.StartOn,
                    TrainerId = x.TrainerId,
                    TrainGroupId = x.Id,
                    TrainGroupDateId = 0, /// TODO check if needed
                    RecurrenceDates = x.TrainGroupDates
                        .Where(y => y.RecurrenceDayOfMonth.HasValue || y.RecurrenceDayOfWeek.HasValue)
                        .Select(y =>
                            new TimeSlotRecurrenceDateDto()
                            {
                                TrainGroupDateId = y.Id,
                                TrainGroupDateType = y.TrainGroupDateType,
                                Date = y.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
                                    ? y.RecurrenceDayOfWeek!.Value
                                    : y.RecurrenceDayOfMonth!.Value,
                                IsUserJoined = true,
                                TrainGroupParticipantId = y.TrainGroupParticipants.FirstOrDefault(z => z.UserId == new Guid(timeSlotRequestDto.UserId))?.Id
                            }
                        )
                        .Concat(
                            x.TrainGroupDates
                            .Where(y => y.FixedDay.HasValue)
                            .Select(y =>
                                 new TimeSlotRecurrenceDateDto()
                                 {
                                     TrainGroupDateId = y.Id,
                                     TrainGroupDateType = y.TrainGroupDateType,
                                     Date = y.FixedDay!.Value,
                                     IsUserJoined = true,
                                     TrainGroupParticipantId = y.TrainGroupParticipants.FirstOrDefault(z => z.UserId == new Guid(timeSlotRequestDto.UserId))?.Id
                                 }
                            )
                        )
                        .Concat(
                            x.TrainGroupParticipants
                            .Where(y => y.SelectedDate.HasValue)
                            .Select(y =>
                                 new TimeSlotRecurrenceDateDto()
                                 {
                                     TrainGroupDateId = y.Id,
                                     TrainGroupDateType = null,
                                     Date = y.SelectedDate!.Value,
                                     IsUserJoined = true,
                                     TrainGroupParticipantId = y.Id
                                 }
                            )
                        )
                        .ToList(),
                    SpotsLeft = 0 // Not needed here.
                })
                .ToList();

            if (timeSlotRequestDtos == null)
                return new ApiResponse<List<TimeSlotResponseDto>>().SetErrorResponse("error", $"Requested data not found!");

            return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        }
    }
}
