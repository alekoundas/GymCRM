using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.AutoComplete;
using Core.Dtos.DataTable;
using Core.Dtos.Lookup;
using Core.Dtos.TrainGroupDate;
using Core.Dtos.User;
using Core.Enums;
using Core.Models;
using Core.System;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq.Expressions;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        // The page shrinks a profile picture to a few tens of KB before sending it; this
        // is the ceiling for anything that arrives some other way.
        private const int MaxProfileImageBytes = 1024 * 1024;

        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer _localizer;

        private readonly ISubscriptionService _subscriptionService;

        public UsersController(
            IDataService dataService,
            IMapper mapper,
            ILogger<UsersController> logger,
            IUserService userService,
            UserManager<User> userManager,
            IStringLocalizer localizer,
            ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
            _dataService = dataService;
            _logger = logger;
            _userService = userService;
            _userManager = userManager;
            _mapper = mapper;
            _localizer = localizer;
        }


        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<UserDto>> Get(string? id)
        {
            if (id == null)
                return new ApiResponse<UserDto>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "User"]);

            User? user = await _dataService.Users
                .Include(x => x.UserStatus)
                .Include(x => x.UserRoles)
                .ThenInclude<UserRole, Role>(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == new Guid(id));


            if (user == null)
                return new ApiResponse<UserDto>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "User"]);

            UserDto userDto = _mapper.Map<UserDto>(user);
            return new ApiResponse<UserDto>().SetSuccessResponse(userDto);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]

        public async Task<ApiResponse<UserDto>> Update(string id, UserDto request)
        {
            if (id != request.Id)
                return new ApiResponse<UserDto>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "User"]);

            if (request.UserRoles.Count() == 0)
                return new ApiResponse<UserDto>().SetErrorResponse(_localizer[TranslationKeys._0_is_required, "Role"]);

            User? user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == new Guid(request.Id));
            if (user == null)
                return new ApiResponse<UserDto>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "User"]);

            // A new picture has to be a real JPG, PNG or WebP of reasonable size. Only a
            // changed one is checked, so an older large picture does not block saving
            // the rest of the profile.
            bool isNewImage = !(request.ProfileImage ?? Array.Empty<byte>()).AsSpan()
                .SequenceEqual(user.ProfileImage ?? Array.Empty<byte>());
            if (isNewImage && !ImageValidator.IsAllowed(request.ProfileImage, MaxProfileImageBytes))
                return new ApiResponse<UserDto>().SetErrorResponse(_localizer[TranslationKeys.Image_must_be_a_JPG_PNG_or_WebP_of_up_to_0_MB, MaxProfileImageBytes / (1024 * 1024)]);

            string? roleName = request.UserRoles[0].Role.Name;
            bool roleExists = await _dataService.Roles.AnyAsync(x => x.Name == roleName);
            if (!roleExists)
                return new ApiResponse<UserDto>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "Role"]);

            // Update user role.
            await _userService.AssignSingleRoleAsync(user, roleName!);

            // Update user properties.
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.UserName = request.UserName;
            user.Email = request.Email; // Only admin can update email.
            user.UserName = request.UserName;
            user.ProfileImage = request.ProfileImage;
            user.UserStatusId = request.UserStatusId;
            user.Address = request.Address;
            user.MedicalHistory = request.MedicalHistory;

            UserDto userDto = _mapper.Map<UserDto>(user);
            IdentityResult response = await _userManager.UpdateAsync(user);

            if (response.Succeeded)
                return new ApiResponse<UserDto>().SetSuccessResponse(userDto, _localizer[TranslationKeys._0_updated_successfully, "User"]);
            else
                return new ApiResponse<UserDto>().SetErrorResponse(response.Errors.First().Description);
        }


        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<ApiResponse<IdentityUser>> Delete(string? id)
        {
            if (id == null || id.Count() == 0)
                return new ApiResponse<IdentityUser>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, $"User"]);

            User? user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return new ApiResponse<IdentityUser>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, $"User"]);


            var response = await _userManager.DeleteAsync(user);
            if (response.Succeeded)
                return new ApiResponse<IdentityUser>().SetSuccessResponse(_localizer[TranslationKeys._0_deleted_successfully, $"User {user.Email} "]);
            return new ApiResponse<IdentityUser>().SetErrorResponse(response.Errors.First().Description);

        }

        // Worked out per request rather than stored, so it is sorted apart from the rest.
        private const string SubscriptionBalanceField = "subscriptionBalance";

        // POST: api/Users/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<UserDto>>> GetDataTable([FromBody] DataTableDto<UserDto> dataTable)
        {
            List<Expression<Func<User, bool>>>? filterQuery = new List<Expression<Func<User, bool>>>();

            var query = _dataService.Users
                .Include(x => x.UserStatus)
                .Include(x => x.UserRoles)
                .ThenInclude<UserRole, Role>(x => x.Role);

            // The balance is not a column - it is approved subscriptions less attendances -
            // so the reflection sort below has no property to read. It is ordered as an
            // expression instead, and here rather than after the query, because sorting
            // what came back would only sort the page the caller happens to be on.
            DataTableSortDto? balanceSort = dataTable.Sorts
                .FirstOrDefault(x => string.Equals(x.FieldName, SubscriptionBalanceField, StringComparison.OrdinalIgnoreCase));

            if (balanceSort != null)
            {
                if (balanceSort.Order > 0)
                    query.OrderBy(x => x.Subscriptions
                            .Where(y => y.Status == SubscriptionStatusEnum.APPROVED)
                            .Sum(y => y.Amount)
                        - x.TrainGroupΑttendances.Count());
                else
                    query.OrderByDescending(x => x.Subscriptions
                            .Where(y => y.Status == SubscriptionStatusEnum.APPROVED)
                            .Sum(y => y.Amount)
                        - x.TrainGroupΑttendances.Count());
            }

            // Handle Sorting of DataTable.
            if (balanceSort == null && dataTable.Sorts.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.Sorts.First();
                string fieldName = dataTableSort.FieldName.Substring(0, 1).ToUpper() + dataTableSort.FieldName.Substring(1, dataTableSort.FieldName.Length - 1);

                if (dataTableSort.Order > 0)
                    query.OrderBy(fieldName, OrderDirectionEnum.ASCENDING);
                else if (dataTableSort.Order < 0)
                    query.OrderBy(fieldName, OrderDirectionEnum.DESCENDING);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.Sorts.Skip(1))
                {
                    fieldName = sortInfo.FieldName.Substring(0, 1).ToUpper() + sortInfo.FieldName.Substring(1, sortInfo.FieldName.Length - 1);
                    if (dataTableSort.Order > 0)
                        query.ThenBy(fieldName, OrderDirectionEnum.ASCENDING);
                    else if (dataTableSort.Order < 0)
                        query.ThenBy(fieldName, OrderDirectionEnum.DESCENDING);
                }
            }

            foreach (var filter in dataTable.Filters)
            {
                string fieldName = filter.FieldName.Substring(0, 1).ToUpper() + filter.FieldName.Substring(1, filter.FieldName.Length - 1);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.contains)
                    query.FilterByColumnContains(filter.FieldName, filter.Value);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.equals)
                    if (filter.FieldName == "UserId")
                        query.FilterByColumnEquals(filter.FieldName, new Guid(filter.Value));
                    else
                        query.FilterByColumnEquals(filter.FieldName, filter.Value != "null" ? filter.Value : null);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.notEquals)
                    query.FilterByColumnNotEquals(filter.FieldName, filter.Value != "null" ? filter.Value : null);

                if (filter.Values?.Count() > 0 && filter.FilterType == DataTableFiltersEnum.@in)
                    query.FilterByColumnIn(filter.FieldName, filter.Values);

                if (filter.Values?.Count() == 2 && filter.FilterType == DataTableFiltersEnum.between)
                    query.FilterByColumnDateBetween(filter.FieldName, filter.Values[0], filter.Values[1]);

                if (filter.FilterType == DataTableFiltersEnum.custom)
                {
                    if (filter.FieldName == "roleId" && filter.Values!.Count > 0)
                        query.Where(x => x.UserRoles.Any(y => filter.Values!.Contains(y.RoleId.ToString().ToLower())));
                }
            }



            // Handle pagination.
            int skip = dataTable.Page * dataTable.Rows;
            int take = dataTable.Rows;
            query.AddPagging(skip, take);


            // Retrieve Data.
            List<User> result = await query.ToListAsync();
            List<UserDto> resultDto = _mapper.Map<List<UserDto>>(result);


            foreach (var filter in dataTable.Filters)
            {
                string fieldName = filter.FieldName.Substring(0, 1).ToUpper() + filter.FieldName.Substring(1, filter.FieldName.Length - 1);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.contains)
                    query.FilterByColumnContains(filter.FieldName, filter.Value);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.equals)
                    if (filter.FieldName == "UserId")
                        query.FilterByColumnEquals(filter.FieldName, new Guid(filter.Value));
                    else
                        query.FilterByColumnEquals(filter.FieldName, filter.Value != "null" ? filter.Value : null);

                if (filter.Value != null && filter.FilterType == DataTableFiltersEnum.notEquals)
                    query.FilterByColumnNotEquals(filter.FieldName, filter.Value != "null" ? filter.Value : null);

                if (filter.Values?.Count() > 0 && filter.FilterType == DataTableFiltersEnum.@in)
                    query.FilterByColumnIn(filter.FieldName, filter.Values);

                if (filter.Values?.Count() == 2 && filter.FilterType == DataTableFiltersEnum.between)
                    query.FilterByColumnDateBetween(filter.FieldName, filter.Values[0], filter.Values[1]);

                if (filter.FilterType == DataTableFiltersEnum.custom)
                {
                    if (filter.FieldName == "roleId" && filter.Values!.Count > 0)
                        query.Where(x => x.UserRoles.Any(y => filter.Values!.Contains(y.RoleId.ToString().ToLower())));
                }
            }


            int rowCount = await query.CountAsync();
            int totalRecords = rowCount;

            // One query for the whole page rather than one per row. Filled here and not
            // in DataTableResultUpdate: this controller answers /GetDataTable with its
            // own method, so that hook never runs for users.
            Dictionary<Guid, int> balances = await _subscriptionService.GetBalancesAsync(
                result.Select(x => x.Id).ToList());

            for (int i = 0; i < result.Count && i < resultDto.Count; i++)
                if (balances.TryGetValue(result[i].Id, out int balance))
                    resultDto[i].SubscriptionBalance = balance;

            dataTable.Data = resultDto;
            dataTable.TotalRecords = totalRecords;
            dataTable.PageCount = (int)Math.Ceiling((double)totalRecords / dataTable.Rows);

            return new ApiResponse<DataTableDto<UserDto>>().SetSuccessResponse(dataTable);

        }

        // POST: api/Users/lookup
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {

            var query = _dataService.GetGenericRepository<User>()
                .Include(x => x.UserStatus);

            // Both sides normalised, so "ελενη" finds "Ελένη". Sqlite's own lower()
            // leaves Greek untouched, which is why this cannot just be ToLower().
            string searchValue = TextNormalizer.Normalize(lookupDto.Filter.Value);

            if (lookupDto.Filter.Id.Length > 0)
                query.Where(x => x.Id == new Guid(lookupDto.Filter.Id));

            if (lookupDto.Filter.Value.Length > 0)
                query.Where(x =>
                TextNormalizer.Normalize(x.FirstName).Contains(searchValue) ||
                TextNormalizer.Normalize(x.LastName).Contains(searchValue) ||
                TextNormalizer.Normalize(x.Email).Contains(searchValue) ||
                x.PhoneNumbers.Any(y => y.Number.Contains(searchValue)) ||
                TextNormalizer.Normalize(x.UserName!).Contains(searchValue)
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
                      FirstName = x.FirstName,
                      LastName = x.LastName,
                      ProfileImage = x.ProfileImage,
                      UserColor = x.UserStatus?.Color
                  })
              .ToList();




            if (lookupDto.Filter.Id.Length > 0)
                query.Where(x => x.Id == new Guid(lookupDto.Filter.Id));

            if (lookupDto.Filter.Value.Length > 0)
                query.Where(x =>
                TextNormalizer.Normalize(x.FirstName).Contains(searchValue) ||
                TextNormalizer.Normalize(x.LastName).Contains(searchValue) ||
                TextNormalizer.Normalize(x.Email).Contains(searchValue) ||
                x.PhoneNumbers.Any(y => y.Number.Contains(searchValue)) ||
                TextNormalizer.Normalize(x.UserName!).Contains(searchValue)
            );
            lookupDto.TotalRecords = await query.CountAsync();


            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }


        // POST: api/users/AutoComplete
        [HttpPost("AutoComplete")]
        public async Task<ApiResponse<AutoCompleteDto<UserDto>>> AutoComplete([FromBody] AutoCompleteDto<UserDto> autoCompleteDto)
        {
            var query = _dataService.GetGenericRepository<User>();
            // Both sides normalised, so "ελενη" finds "Ελένη". Sqlite's own lower()
            // leaves Greek untouched, which is why this cannot just be ToLower().
            string searchValue = TextNormalizer.Normalize(autoCompleteDto.SearchValue);
            if (autoCompleteDto.SearchValue.Length > 0)
                query.Where(x =>
                    TextNormalizer.Normalize(x.FirstName).Contains(searchValue) ||
                    TextNormalizer.Normalize(x.LastName).Contains(searchValue) ||
                    TextNormalizer.Normalize(x.Email).Contains(searchValue) ||
                    x.PhoneNumbers.Any(y => y.Number.Contains(searchValue)) ||
                    TextNormalizer.Normalize(x.UserName!).Contains(searchValue)
                );

            // Handle Pagging.
            query.AddPagging(autoCompleteDto.Skip, autoCompleteDto.Take);

            // Retrieve Data.
            List<User> result = await query.ToListAsync();
            List<UserDto> customerDto = _mapper.Map<List<UserDto>>(result);

            if (autoCompleteDto.SearchValue.Length > 0)
                query.Where(x =>
                    TextNormalizer.Normalize(x.FirstName).Contains(searchValue) ||
                    TextNormalizer.Normalize(x.LastName).Contains(searchValue) ||
                    TextNormalizer.Normalize(x.Email).Contains(searchValue) ||
                    x.PhoneNumbers.Any(y => y.Number.Contains(searchValue)) ||
                    TextNormalizer.Normalize(x.UserName!).Contains(searchValue)
                );

            autoCompleteDto.Suggestions = customerDto;
            autoCompleteDto.TotalRecords = await query.CountAsync();

            return new ApiResponse<AutoCompleteDto<UserDto>>().SetSuccessResponse(autoCompleteDto);
        }

        // POST: api/Users/TimeSlots
        //[HttpPost("TimeSlots")]
        //[Authorize]
        //public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        //{
        //    using var dbContext = _dataService.GetDbContext();
        //    DateTime selectedDateStart = timeSlotRequestDto.SelectedDate.Date;
        //    DateTime selectedDateEnd = timeSlotRequestDto.SelectedDate.Date.AddDays(6);
        //    List<TrainGroupDate>? trainGroupDates = await dbContext.TrainGroupDates
        //        //.AsSplitQuery()                    // ← Major performance boost for multiple collections
        //        //.AsNoTracking()                    // ← No change tracking needed for this read
        //        .Include(x => x.TrainGroup.Trainer)
        //        .Include(x => x.TrainGroupParticipants)
        //        .Include(x => x.TrainGroup.TrainGroupDates)
        //        .Include(x => x.TrainGroup.TrainGroupUnavailableDates)
        //        .Include(x => x.TrainGroup.TrainGroupParticipants)
        //        .ThenInclude(x => x.TrainGroupParticipantUnavailableDates)
        //        .Where(x => x.TrainGroupParticipants.Any(y => y.UserId == new Guid(timeSlotRequestDto.UserId)))
        //        .Where(x =>
        //            (x.FixedDay >= selectedDateStart && x.FixedDay <= selectedDateEnd)
        //            ||
        //            (x.RecurrenceDayOfWeek >= selectedDateStart.DayOfWeek && x.RecurrenceDayOfWeek <= selectedDateEnd.DayOfWeek)
        //            ||
        //            (
        //                selectedDateStart.Month == selectedDateEnd.Month ?
        //                (x.RecurrenceDayOfMonth >= selectedDateStart.Day && x.RecurrenceDayOfMonth <= selectedDateEnd.Day) :
        //                (x.RecurrenceDayOfMonth >= selectedDateStart.Day || x.RecurrenceDayOfMonth <= selectedDateEnd.Day)
        //            )
        //        )
        //        .ToListAsync();

        //    List<TimeSlotResponseDto>? timeSlotRequestDtos = trainGroupDates
        //        .GroupBy(x => x.TrainGroup)
        //        .Distinct()
        //        .Select(x => new TimeSlotResponseDto()
        //        {
        //            Id = x.Key.Id,
        //            Title = x.Key.Title,
        //            Description = x.Key.Description,
        //            Duration = x.Key.Duration,
        //            StartOn = x.Key.StartOn,
        //            TrainerId = x.Key.TrainerId,
        //            Trainer = _mapper.Map<UserDto>(x.Key.Trainer),
        //            TrainGroupId = x.Key.Id,
        //            IsUnavailableTrainGroup = x.Key.TrainGroupUnavailableDates.Any(y => y.UnavailableDate >= selectedDateStart && y.UnavailableDate <= selectedDateEnd),
        //            UnavailableTrainGroupId = x.Key.TrainGroupUnavailableDates.FirstOrDefault(y => y.UnavailableDate >= selectedDateStart && y.UnavailableDate <= selectedDateEnd)?.Id,
        //            RecurrenceDates = x.Key.TrainGroupDates
        //            .SelectMany(y => y.TrainGroupParticipants.Where(z => z.UserId == new Guid(timeSlotRequestDto.UserId) && z.SelectedDate == null))
        //            .Where(y => y.TrainGroupDate.RecurrenceDayOfMonth.HasValue || y.TrainGroupDate.RecurrenceDayOfWeek.HasValue)
        //            .Where(y =>
        //                y.RecurringStartOnDate == null ?
        //                true :
        //                (y.RecurringStartOnDate.Value <= selectedDateStart) ||
        //                (y.RecurringStartOnDate.Value >= selectedDateStart && y.RecurringStartOnDate.Value <= selectedDateEnd)
        //            )
        //            .Where(y =>
        //                (y.TrainGroupDate.RecurrenceDayOfWeek >= selectedDateStart.DayOfWeek && y.TrainGroupDate.RecurrenceDayOfWeek <= selectedDateEnd.DayOfWeek)
        //                ||
        //                (
        //                    selectedDateStart.Month == selectedDateEnd.Month ?
        //                    (y.TrainGroupDate.RecurrenceDayOfMonth >= selectedDateStart.Day && y.TrainGroupDate.RecurrenceDayOfMonth <= selectedDateEnd.Day) :
        //                    (y.TrainGroupDate.RecurrenceDayOfMonth >= selectedDateStart.Day || y.TrainGroupDate.RecurrenceDayOfMonth <= selectedDateEnd.Day)
        //                )
        //            )
        //            .Select(y =>
        //                new TimeSlotRecurrenceDateDto()
        //                {
        //                    TrainGroupDateId = y.Id,
        //                    TrainGroupDateType = y.TrainGroupDate.TrainGroupDateType,
        //                    Date = y.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
        //                        ? new DateTime(2000, 1, 2 + (int)y.TrainGroupDate.RecurrenceDayOfWeek!.Value)
        //                        : new DateTime(2000, 1, y.TrainGroupDate.RecurrenceDayOfMonth!.Value),
        //                    IsUserJoined = y.TrainGroupParticipantUnavailableDates
        //                        .Where(z =>
        //                            z.UnavailableDate >= selectedDateStart &&
        //                            z.UnavailableDate <= selectedDateEnd
        //                        )
        //                        .Where(z =>
        //                            y.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
        //                            ? z.UnavailableDate.DayOfWeek == y.TrainGroupDate.RecurrenceDayOfWeek
        //                            : z.UnavailableDate.Day == y.TrainGroupDate.RecurrenceDayOfMonth)
        //                        .FirstOrDefault() == null,
        //                    IsOneOff = false,
        //                    IsUnavailableTrainGroup = y.TrainGroupParticipantUnavailableDates
        //                        .Where(z => z.UnavailableDate >= selectedDateStart && z.UnavailableDate <= selectedDateEnd)
        //                        .Any(z => y.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK ?
        //                            z.UnavailableDate.DayOfWeek == y.TrainGroupDate.RecurrenceDayOfWeek :
        //                            z.UnavailableDate.Day == y.TrainGroupDate.RecurrenceDayOfMonth
        //                        ),
        //                    TrainGroupParticipantId = y.Id,
        //                    TrainGroupParticipantUnavailableDateId = y.TrainGroupParticipantUnavailableDates
        //                        .Where(z =>
        //                            z.UnavailableDate >= selectedDateStart &&
        //                            z.UnavailableDate <= selectedDateEnd
        //                        )
        //                        .Where(z =>
        //                            y.TrainGroupDate.TrainGroupDateType == TrainGroupDateTypeEnum.DAY_OF_WEEK
        //                            ? z.UnavailableDate.DayOfWeek == y.TrainGroupDate.RecurrenceDayOfWeek
        //                            : z.UnavailableDate.Day == y.TrainGroupDate.RecurrenceDayOfMonth)
        //                        .FirstOrDefault()?.Id
        //                }
        //            )
        //            .Concat(
        //                x.Key.TrainGroupDates
        //                .Where(x => x.FixedDay.HasValue)
        //                .Where(y => y.TrainGroupParticipants.Any(x => x.UserId == new Guid(timeSlotRequestDto.UserId)))
        //                .Where(x => x.FixedDay >= selectedDateStart && x.FixedDay <= selectedDateEnd)
        //                .Select(y =>
        //                     new TimeSlotRecurrenceDateDto()
        //                     {
        //                         TrainGroupDateId = y.Id,
        //                         TrainGroupDateType = y.TrainGroupDateType,
        //                         Date = y.FixedDay!.Value,
        //                         IsUserJoined = true,
        //                         IsOneOff = true,
        //                         IsUnavailableTrainGroup = x.Key.TrainGroupUnavailableDates
        //                            .Where(z => z.UnavailableDate >= selectedDateStart && z.UnavailableDate <= selectedDateEnd)
        //                            .Any(z => z.UnavailableDate == y.FixedDay),
        //                         TrainGroupParticipantId = y.TrainGroupParticipants
        //                            .FirstOrDefault(z => z.UserId == new Guid(timeSlotRequestDto.UserId))?.Id,
        //                         TrainGroupParticipantUnavailableDateId = null,
        //                     }
        //                )
        //            )
        //            .Concat(
        //                x.Key.TrainGroupDates
        //                .SelectMany(y => y.TrainGroupParticipants.Where(z => z.UserId == new Guid(timeSlotRequestDto.UserId) && z.SelectedDate != null))
        //                .Where(y => y.SelectedDate >= selectedDateStart && y.SelectedDate <= selectedDateEnd)
        //                .Select(y =>
        //                    new TimeSlotRecurrenceDateDto()
        //                    {
        //                        TrainGroupDateId = y.TrainGroupDate.Id,
        //                        TrainGroupDateType = y.TrainGroupDate.TrainGroupDateType,
        //                        Date = y.SelectedDate!.Value,
        //                        IsUserJoined = true,
        //                        IsOneOff = true,
        //                        IsUnavailableTrainGroup = false,
        //                        TrainGroupParticipantId = y.Id,
        //                        TrainGroupParticipantUnavailableDateId = null
        //                    }
        //                )
        //            )
        //            .ToList(),
        //            SpotsLeft = 0 // Not needed here.
        //        })
        //        .ToList();

        //    return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(timeSlotRequestDtos);
        //}

        // POST: api/Users/TimeSlots
        // A member's week on the profile calendar. Every date between booking and leaving
        // is there - attended, missed, skipped or still to come - because bookings are no
        // longer deleted. Which of attended, missed or upcoming a date is, the page
        // decides: it knows what today is where the member is.
        [HttpPost("TimeSlots")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<TimeSlotResponseDto>>>> TimeSlots([FromBody] TimeSlotRequestDto timeSlotRequestDto)
        {
            using var dbContext = _dataService.GetDbContext();

            // A member reads their own calendar only. Staff open anybody's from the
            // member's profile.
            Guid.TryParse(User.FindFirst("Id")?.Value, out Guid callerId);
            Guid userId = callerId;
            if (Guid.TryParse(timeSlotRequestDto.UserId, out Guid requestedId) && User.HasClaim("Permission", BookingRules.StaffPermission))
                userId = requestedId;

            DateTime weekStart = timeSlotRequestDto.SelectedDate.Date;
            DateTime weekEnd = weekStart.AddDays(7);

            List<TrainGroupParticipant> participants = await dbContext.TrainGroupParticipants
                .AsNoTracking()
                .Include(p => p.TrainGroup)
                .ThenInclude(g => g.Trainer)
                .Include(p => p.TrainGroup)
                .ThenInclude(g => g.TrainGroupUnavailableDates)
                .Include(p => p.TrainGroupDate)
                .Include(p => p.TrainGroupParticipantUnavailableDates)
                .Where(p => p.UserId == userId)
                .AsSplitQuery()
                .ToListAsync();

            // Sessions that actually took place. These come from the attendance rows, which
            // outlive the group, the booking and any edit to either.
            List<TrainGroupΑttendance> attendances = await dbContext.TrainGroupΑttendances
                .AsNoTracking()
                .Where(a => a.UserId == userId
                         && a.AttendanceDate >= weekStart
                         && a.AttendanceDate < weekEnd)
                .ToListAsync();

            // Missed only means something from the day attendance started being taken.
            // Before it, a booked session with no attendance says nothing about whether
            // the member came, so those days show attendances alone.
            DateTime? firstAttendance = await dbContext.TrainGroupΑttendances.MinAsync(a => (DateTime?)a.AttendanceDate);
            DateTime historyStart = firstAttendance?.Date ?? DateTime.UtcNow.Date;

            HashSet<(int, DateTime)> attended = attendances
                .Where(a => a.TrainGroupId != null)
                .Select(a => (a.TrainGroupId!.Value, a.AttendanceDate.Date))
                .ToHashSet();

            Dictionary<int, TimeSlotResponseDto> slots = new Dictionary<int, TimeSlotResponseDto>();

            for (DateTime day = weekStart; day < weekEnd; day = day.AddDays(1))
                foreach (TrainGroupParticipant participant in participants)
                {
                    if (!BookingRules.IsBooked(participant, day))
                        continue;

                    if (day < historyStart)
                        continue;

                    // The gym called that one session off. Only that day goes - it used to
                    // take the group off the whole week.
                    if (participant.TrainGroup.TrainGroupUnavailableDates.Any(u => u.UnavailableDate.Date == day))
                        continue;

                    // Attended wins; the attendance entry below stands for the day.
                    if (attended.Contains((participant.TrainGroupId, day)))
                        continue;

                    TrainGroupParticipantUnavailableDate? skipped = participant.TrainGroupParticipantUnavailableDates
                        .FirstOrDefault(u => u.UnavailableDate.Date == day);

                    if (!slots.TryGetValue(participant.TrainGroupId, out TimeSlotResponseDto? slot))
                    {
                        TrainGroup group = participant.TrainGroup;
                        slot = new TimeSlotResponseDto
                        {
                            Id = group.Id,
                            Title = group.Title,
                            Description = group.Description,
                            Duration = group.Duration,
                            StartOn = group.StartOn,
                            TrainerId = group.TrainerId,
                            Trainer = _mapper.Map<UserDto>(group.Trainer),
                            TrainerFullName = (group.Trainer.FirstName + " " + group.Trainer.LastName).Trim(),
                            TrainGroupId = group.Id,
                            MaxParticipants = group.MaxParticipants
                        };
                        slots[participant.TrainGroupId] = slot;
                    }

                    slot.RecurrenceDates.Add(new TimeSlotRecurrenceDateDto
                    {
                        TrainGroupDateId = participant.TrainGroupDateId,
                        TrainGroupDateType = participant.TrainGroupDate.TrainGroupDateType,
                        // A real date now, midnight UTC - the page reads it with UTC getters.
                        Date = DateTime.SpecifyKind(day, DateTimeKind.Utc),
                        RecurrenceDayOfWeek = (int?)participant.TrainGroupDate.RecurrenceDayOfWeek,
                        RecurrenceDayOfMonth = participant.TrainGroupDate.RecurrenceDayOfMonth,
                        IsOneOff = !BookingRules.IsRecurring(participant),
                        IsUserJoined = skipped == null,
                        TrainGroupParticipantId = participant.Id,
                        TrainGroupParticipantUnavailableDateId = skipped?.Id
                    });
                }

            List<TimeSlotResponseDto> result = slots.Values.ToList();

            List<Guid> trainerIds = attendances
                .Where(a => a.TrainerId != null)
                .Select(a => a.TrainerId!.Value)
                .Distinct()
                .ToList();

            List<User> trainers = trainerIds.Count == 0
                ? new List<User>()
                : await dbContext.Users.AsNoTracking().Where(u => trainerIds.Contains(u.Id)).ToListAsync();

            result.AddRange(attendances.Select(a => new TimeSlotResponseDto
            {
                Id = a.TrainGroupId ?? 0,
                TrainGroupId = a.TrainGroupId ?? 0,
                Title = a.TrainGroupTitle,
                Description = a.TrainGroupDescription,
                StartOn = a.TrainGroupStartOn,
                Duration = a.TrainGroupDuration,
                TrainerId = a.TrainerId ?? Guid.Empty,
                Trainer = _mapper.Map<UserDto>(trainers.FirstOrDefault(u => u.Id == a.TrainerId)),
                TrainerFullName = a.TrainerFullName,
                IsUnavailableTrainGroup = false,
                SpotsLeft = 0,
                RecurrenceDates = new List<TimeSlotRecurrenceDateDto>
                {
                    new TimeSlotRecurrenceDateDto
                    {
                        TrainGroupDateId = null,
                        TrainGroupDateType = null,
                        Date = DateTime.SpecifyKind(a.AttendanceDate.Date, DateTimeKind.Utc),
                        IsUserJoined = true,
                        IsOneOff = true,
                        IsAttendance = true,
                        AttendanceId = a.Id
                    }
                }
            }));

            return new ApiResponse<List<TimeSlotResponseDto>>().SetSuccessResponse(result);
        }
    }
}
