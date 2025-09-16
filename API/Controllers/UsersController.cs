using AutoMapper;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Dtos.Lookup;
using Core.Dtos.TrainGroupDate;
using Core.Dtos.User;
using Core.Enums;
using Core.Models;
using Core.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly TokenSettings _tokenSettings;

        public UsersController(
            IDataService dataService,
            IMapper mapper,
            ILogger<UsersController> logger,
            IUserService userService,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IEmailService emailService,
            TokenSettings tokenSettings)
        {
            _dataService = dataService;
            _logger = logger;
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _emailService = emailService;
            _tokenSettings = tokenSettings;
        }


        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<UserDto>> Get(string? id)
        {
            if (id == null)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "User ID not set!");

            User? user = await _dataService.Users.FirstOrDefaultAsync(x => x.Id == new Guid(id));


            if (user == null)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "User not found!");

            UserDto userDto = _mapper.Map<UserDto>(user);

            IList<string> userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Count() == 1)
                userDto.RoleId = userRoles.First();

            return new ApiResponse<UserDto>().SetSuccessResponse(userDto);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]

        public async Task<ApiResponse<UserDto>> Update(string id, UserDto request)
        {
            if (id != request.Id)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "ID mismatch");


            User? user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == new Guid(request.Id));
            if (user == null)
                return new ApiResponse<UserDto>().SetErrorResponse("error", "User not found");


            //IdentityRole<Guid>? role = await _dataService.Roles.FirstOrDefaultAsync(x => x.Id.ToString() == request.RoleId);
            //if (role?.Name == null)
            //    return new ApiResponse<bool>().SetErrorResponse("error", "User not found");


            // Update user properties
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            //user.Email = request.Email; Shouldnt update email
            user.UserName = request.UserName;
            user.ProfileImage = request.ProfileImage;

            UserDto userDto = _mapper.Map<UserDto>(user);
            //await _userService.AssignSingleRoleAsync(user, role.Name);
            IdentityResult response = await _userManager.UpdateAsync(user);
            if (response.Succeeded)
                return new ApiResponse<UserDto>().SetSuccessResponse(userDto, "success", "User updated successfully");
            else
                return new ApiResponse<UserDto>().SetErrorResponse("error", "User update error");
        }

        // POST: api/Users/lookup
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {
            List<User> users;
            if (lookupDto.Filter?.Id != null && lookupDto.Filter?.Id.Length > 0)
                users = await _dataService.Users
                    .Where(x => lookupDto.Filter.Id.ToLower().Contains(x.Id.ToString().ToLower()))
                    .ToListAsync();
            else
                users = await _dataService.Users.ToListAsync();


            lookupDto.Data = users
                .Select(x => new LookupOptionDto() { Id = x.Id.ToString(), Value = x.UserName ?? "" })
                .ToList();

            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }

        // POST: api/Users/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<UserDto>>> GetDataTable([FromBody] DataTableDto<UserDto> dataTable)
        {
            List<Expression<Func<User, bool>>>? filterQuery = new List<Expression<Func<User, bool>>>();

            var query = _dataService.Users;

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
            foreach (var filter in dataTable.Filters)
            {
                if (filter.FieldName == "FirstName" && filter.Value.Length > 0)
                    filterQuery.Add(x => x.FirstName.Contains(filter.Value));

                if (filter.FieldName == "LastName" && filter.Value.Length > 0)
                    filterQuery.Add(x => x.LastName.Contains(filter.Value));
            }



            // Handle pagination.
            int skip = (dataTable.Page - 1) * dataTable.Rows;
            int take = dataTable.Rows;

            query.AddPagging(skip, take);


            // Retrieve Data.
            List<User> result = await query.ToListAsync();
            List<UserDto> resultDto = _mapper.Map<List<UserDto>>(result);


            // Assign Id to RoleId
            for (int i = 0; i < result.Count(); i++)
            {
                string? roleName = (await _userManager.GetRolesAsync(result[i])).FirstOrDefault();
                IdentityRole<Guid>? role = _roleManager.Roles.FirstOrDefault(x => x.Name == roleName);
                if (role != null)
                    resultDto[i].RoleId = role.Id.ToString();
            }



            //TODDO add filter
            int rows = await _dataService.Users.CountAsync();

            dataTable.Data = resultDto;
            dataTable.PageCount = rows;

            return new ApiResponse<DataTableDto<UserDto>>().SetSuccessResponse(dataTable);

        }

        // POST: api/Users/ForgotPassword
        [HttpPost("PasswordForgot")]
        public async Task<ApiResponse<bool>> PasswordForgot([FromBody] UserPasswordForgotDto dto)
        {
            User? user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Don't reveal if email doesn't exist for security
                return new ApiResponse<bool>().SetErrorResponse("error", "Password reset email didnt sent!");
            }


            string? token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // URL-encode email and token to handle special characters
            string encodedEmail = Uri.EscapeDataString(dto.Email);
            string encodedToken = Uri.EscapeDataString(token);
            string baseUrl = _tokenSettings.Issuer; // Adjust base URL based on environment (e.g., prod: https://alexps.gr, dev: http://localhost:5173)
            string resetLink = $"{baseUrl}/users/reset-password?email={encodedEmail}&token={encodedToken}";

            string emailBody = $@"<h2>Password Reset Request</h2>
                              <p>Please click the link below to reset your password:</p>
                              <p><a href=""{resetLink}"">Reset Password</a></p>
                              <p>This link expires in 24 hours.</p>";

            await _emailService.SendEmailAsync(
                dto.Email,
                "Reset Your Password",
                emailBody,
                emailBody // HTML version
            );

            return new ApiResponse<bool>().SetSuccessResponse(true, "success", "Password reset email sent.");
        }

        // POST: api/Users/ResetPassword
        [HttpPost("PasswordReset")]
        public async Task<ApiResponse<bool>> PasswordReset([FromBody] UserPasswordResetDto request)
        {
            User? user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return new ApiResponse<bool>().SetErrorResponse("error", "Invalid email or token.");

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ApiResponse<bool>().SetErrorResponse("error", $"Password reset failed: {errors}");
            }
            return new ApiResponse<bool>().SetSuccessResponse(true, "success", "Password changed successfully.");

        }

        // POST: api/Users/ChangePassword
        [HttpPost("PasswordChange")]
        [Authorize] // Requires authentication
        public async Task<ApiResponse<bool>> PasswordChange([FromBody] UserPasswordChangeDto request)
        {
            if (!ModelState.IsValid)
                return new ApiResponse<bool>().SetErrorResponse("error", "Invalid model state.");

            // Get current user from claims
            User? user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return new ApiResponse<bool>().SetErrorResponse("error", "User not found.");

            // Validate old password
            var oldPasswordValid = await _userManager.CheckPasswordAsync(user, request.OldPassword);
            if (!oldPasswordValid)
                return new ApiResponse<bool>().SetErrorResponse("error", "Old password is incorrect.");

            // Validate new password fields are equal
            if (request.NewPassword != request.ConfirmNewPassword)
                return new ApiResponse<bool>().SetErrorResponse("error", "Passwords dont match.");

            // Validate new password length (Identity default is 6 chars, but customizable)
            //if (request.newPassword.Length < 6)
            //    return new ApiResponse<bool>().SetErrorResponse("error", "New password must be at least 6 characters");

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (result.Succeeded)
                return new ApiResponse<bool>().SetSuccessResponse(true, "success", "Password changed successfully.");
            else
                return new ApiResponse<bool>().SetErrorResponse("error", string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<ApiResponse<bool>> Register(UserRegisterRequestDto request)
        {
            User user = new User()
            {
                UserName = request.UserName,
                Email = request.Email,
            };

            // Create user.
            string? result = await _userService.AddNewUser(user, request.Password);
            if (result != null)
                return new ApiResponse<bool>().SetErrorResponse("error", result);

            // Check if SimpleUser role exists, if not create it.
            var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "SimpleUser", NormalizedName = "SIMPLEUSER" };
            if (!await _roleManager.RoleExistsAsync(role.Name))
                await _roleManager.CreateAsync(role);

            // Assign role.
            await _userService.AssignSingleRoleAsync(user, role.Name);


            return new ApiResponse<bool>().SetSuccessResponse(true);
        }


        // POST: api/Users/Login
        [HttpPost("Login")]
        //[Authorize]
        public async Task<ApiResponse<UserLoginResponseDto>> Login(UserLoginRequestDto request)
        {

            User? user = await _userManager.FindByEmailAsync(request.UserNameOrEmail);
            if (user == null)
                user = await _userManager.FindByNameAsync(request.UserNameOrEmail);

            if (user == null)
                return new ApiResponse<UserLoginResponseDto>().SetErrorResponse("error", "User Name/Email not found");


            string? result = await _userService.SignInUser(user, request.Password);
            if (result == null)
            {
                UserLoginResponseDto token = await _userService.GenerateUserToken(user);
                return new ApiResponse<UserLoginResponseDto>().SetSuccessResponse(token);
            }
            else
                return new ApiResponse<UserLoginResponseDto>().SetErrorResponse("error", result);
        }

        // POST: api/Users/RefreshToken
        [HttpPost("RefreshToken")]
        public async Task<ApiResponse<UserRefreshResponseDto>> RefreshToken(UserRefreshRequestDto request)
        {
            return await _userService.RefreshToken(request);
        }


        // POST: api/Users/Logout
        [HttpPost("Logout")]
        [Authorize]
        public async Task<ApiResponse<bool>> Logout()
        {
            if (User.Identity?.IsAuthenticated == null || !User.Identity.IsAuthenticated)
                return new ApiResponse<bool>().SetErrorResponse("error", "User is not loged in!");

            string username = User.Claims.First(x => x.Type == "UserName").Value;
            User? user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return new ApiResponse<bool>().SetErrorResponse("error", "User doesnt exist!");

            string? result = await _userService.SignOutUser(user);
            if (result == null)
                return new ApiResponse<bool>().SetSuccessResponse(true);
            else
                return new ApiResponse<bool>().SetErrorResponse("error", result);
        }

        [HttpPost("Profile")]
        [Authorize(Roles = "Administrator")]
        public ApiResponse<bool> Profile()
        {
            return new ApiResponse<bool>().SetSuccessResponse(true);
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


        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<ApiResponse<IdentityUser>> Delete(string? id)
        {
            if (id == null || id.Count() == 0)
                return new ApiResponse<IdentityUser>().SetErrorResponse("error", "User name not not set!");

            User? user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return new ApiResponse<IdentityUser>().SetErrorResponse("error", "User not found!");


            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return new ApiResponse<IdentityUser>().SetSuccessResponse("success", $"User {user.Email} deleted successfully");

            return new ApiResponse<IdentityUser>().SetErrorResponse("error", result.Errors.ToString() ?? "");
        }
    }
}
