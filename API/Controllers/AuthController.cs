using AutoMapper;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.User;
using Core.Models;
using Core.System;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserService _userService;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly TokenSettings _tokenSettings;
        private readonly IStringLocalizer _localizer;

        public AuthController(
            IMapper mapper,
            ILogger<AuthController> logger,
            IUserService userService,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IEmailService emailService,
            TokenSettings tokenSettings,
            IStringLocalizer localizer)
        {
            _logger = logger;
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _emailService = emailService;
            _tokenSettings = tokenSettings;
            _localizer = localizer;
        }


        // POST: api/Users/RefreshToken
        [HttpPost("RefreshToken")]
        public async Task<ApiResponse<UserRefreshResponseDto>> RefreshToken(UserRefreshRequestDto request)
        {
            return await _userService.RefreshToken(request);
        }

        // POST: api/Users/ForgotPassword
        [HttpPost("PasswordForgot")]
        public async Task<ApiResponse<bool>> PasswordForgot([FromBody] UserPasswordForgotDto dto)
        {
            User? user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Don't reveal if email doesn't exist for security
                return new ApiResponse<bool>().SetSuccessResponse(_localizer[TranslationKeys.Password_reset_email_sent]);
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
                emailBody
            );

            return new ApiResponse<bool>().SetSuccessResponse(_localizer[TranslationKeys.Password_reset_email_sent]);
        }

        // POST: api/Users/ResetPassword
        [HttpPost("PasswordReset")]
        public async Task<ApiResponse<bool>> PasswordReset([FromBody] UserPasswordResetDto request)
        {
            User? user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Invalid_email]);

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Password_reset_failed_0,errors] );
            }
            return new ApiResponse<bool>().SetSuccessResponse("Password changed successfully.");

        }

        // POST: api/Users/ChangePassword
        [HttpPost("PasswordChange")]
        [Authorize]
        public async Task<ApiResponse<bool>> PasswordChange([FromBody] UserPasswordChangeDto request)
        {
            //if (!ModelState.IsValid)
            //    return new ApiResponse<bool>().SetErrorResponse("Invalid model state.");

            // Get current user from claims
            User? user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "User"]);

            // Validate old password
            var oldPasswordValid = await _userManager.CheckPasswordAsync(user, request.OldPassword);
            if (!oldPasswordValid)
                return new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.Old_password_is_incorrect]);

            // Validate new password fields are equal
            if (request.NewPassword != request.ConfirmNewPassword)
                return new ApiResponse<bool>().SetErrorResponse("Passwords dont match.");

            // Validate new password length (Identity default is 6 chars, but customizable)
            //if (request.newPassword.Length < 6)
            //    return new ApiResponse<bool>().SetErrorResponse("error", "New password must be at least 6 characters");

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (result.Succeeded)
                return new ApiResponse<bool>().SetSuccessResponse(_localizer[TranslationKeys.Password_changed_successfully]);
            else
                return new ApiResponse<bool>().SetErrorResponse(string.Join(", ", result.Errors.Select(e => e.Description)));
        }


        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<ApiResponse<bool>> Register(UserRegisterRequestDto request)
        {
            List<PhoneNumber> phoneNumbers = _mapper.Map<List<PhoneNumber>>(request.PhoneNumbers);
            User user = new User()
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumbers = phoneNumbers,
            };

            // Create user.
            string? result = await _userService.AddNewUser(user, request.Password);
            if (result != null)
                return new ApiResponse<bool>().SetErrorResponse(result);

            // Check if SimpleUser role exists, if not create it.
            var role = new Role { Id = Guid.NewGuid(), Name = "SimpleUser", NormalizedName = "SIMPLEUSER" };
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
                return new ApiResponse<UserLoginResponseDto>().SetErrorResponse(_localizer[TranslationKeys.User_Name_Email_not_found]);


            string? result = await _userService.SignInUser(user, request.Password);
            if (result == null)
            {
                UserLoginResponseDto token = await _userService.GenerateUserToken(user);
                return new ApiResponse<UserLoginResponseDto>().SetSuccessResponse(token);
            }
            else
                return new ApiResponse<UserLoginResponseDto>().SetErrorResponse(result);
        }


        // POST: api/Users/Logout
        [HttpPost("Logout")]
        [Authorize]
        public async Task<ApiResponse<bool>> Logout()
        {
            if (User.Identity?.IsAuthenticated == null || !User.Identity.IsAuthenticated)
                return new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys.User_is_not_loged_in]);

            string username = User.Claims.First(x => x.Type == "UserName").Value;
            User? user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return new ApiResponse<bool>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "User"]);

            string? result = await _userService.SignOutUser(user);
            if (result == null)
                return new ApiResponse<bool>().SetSuccessResponse(true);
            else
                return new ApiResponse<bool>().SetErrorResponse(result);
        }
    }
}
