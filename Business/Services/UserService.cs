using Core.Dtos;
using Core.Models;
using Core.System;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Business.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly TokenSettings _tokenSettings;
        private readonly SignInManager<User> _signInManager;

        public UserService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IRefreshTokenService refreshTokenService,
            TokenSettings tokenSettings,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _refreshTokenService = refreshTokenService;
            _tokenSettings = tokenSettings;
        }


        public async Task<string?> SignInUser(User user, string password)
        {
            SignInResult result = await _signInManager.CheckPasswordSignInAsync(user, password, true);
            if (!result.Succeeded)
                return "Login unsuccessful!";

            return null;
        }

        public async Task<string?> SignOutUser(User user)
        {
            IdentityResult result = await _userManager.UpdateSecurityStampAsync(user);

            if (!result.Succeeded)
                return "Logout unsuccessful!";

            await _refreshTokenService.EndAllAsync(user);
            return null;
        }

        public async Task<string?> AddNewUser(User user, string password)
        {
            IdentityResult result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return string.Join(", ", result.Errors.Select(e => e.Description));

            return null;
        }


        public async Task AssignSingleRoleAsync(User user, string roleName)
        {
            List<string> userRoles = (await _userManager.GetRolesAsync(user)).ToList();
            if (userRoles.Count() > 0)
                await _userManager.RemoveFromRolesAsync(user, userRoles);

            // Assign the single role
            IdentityResult addResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addResult.Succeeded)
                throw new InvalidOperationException($"Failed to assign role: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
        }

        public async Task<UserLoginResponseDto> GenerateUserToken(User user)
        {
            return new UserLoginResponseDto
            {
                AccessToken = await GenerateAccessTokenAsync(user),
                RefreshToken = await _refreshTokenService.StartAsync(user)
            };
        }

        private async Task<string> GenerateAccessTokenAsync(User user)
        {
            // Get user claims 
            List<Claim> claims = (await _userManager.GetClaimsAsync(user)).ToList();

            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // Add role-based claims
            foreach (var roleName in userRoles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    claims.AddRange(roleClaims);
                }
            }

            // Get roles and create role claims
            //List<string> roles = (await _userManager.GetRolesAsync(user)).ToList();
            //List<Claim> roleClaims = roles
            //    .Where(x => !string.IsNullOrEmpty(x))
            //    .Select(x => new Claim(ClaimTypes.Role, x))
            //    .ToList();

            //claims.AddRange(roleClaims);

            return GetToken(_tokenSettings, user, claims, userRoles);
        }

        public async Task<ApiResponse<UserRefreshResponseDto>> RefreshToken(UserRefreshRequestDto request)
        {
            var principal = GetPrincipalFromExpiredToken(_tokenSettings, request.AccessToken);
            if (principal == null || principal.FindFirst(ClaimTypes.Name)?.Value == null)
            {
                return new ApiResponse<UserRefreshResponseDto>().SetErrorResponse("User not found");
            }
            else
            {
                var user = await _userManager.FindByNameAsync(principal.FindFirst(ClaimTypes.Name)?.Value ?? "");
                if (user == null)
                {
                    return new ApiResponse<UserRefreshResponseDto>().SetErrorResponse("User not found");
                }
                else
                {
                    string? refreshToken = await _refreshTokenService.RenewAsync(user, request.RefreshToken);
                    if (refreshToken == null)
                        return new ApiResponse<UserRefreshResponseDto>().SetErrorResponse("Please Login again.");

                    return new ApiResponse<UserRefreshResponseDto>()
                        .SetSuccessResponse(new UserRefreshResponseDto() { AccessToken = await GenerateAccessTokenAsync(user), RefreshToken = refreshToken });
                }
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(TokenSettings appSettings, string token)
        {
            TokenValidationParameters tokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudiences = appSettings.Audiences,
                    ValidIssuers = appSettings.Issuers,
                    ValidateLifetime = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.SecretKey))
                };

            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("GetPrincipalFromExpiredToken Token is not validated");

            return principal;
        }

        private string GetToken(TokenSettings appSettings, User user, List<Claim> roleClaims, IList<string> userRoles)
        {
            SymmetricSecurityKey secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.SecretKey));
            SigningCredentials signInCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);


            List<Claim> userClaims = new List<Claim>();
            userClaims.Add(new("Id", user.Id.ToString()));
            userClaims.Add(new("userName", user.UserName ?? ""));


            
            userClaims.Add(new Claim(ClaimTypes.Name, user.UserName ?? ""));
            userClaims.Add(new("UserName", user.UserName ?? ""));
            userClaims.Add(new("RoleName", userRoles.FirstOrDefault() ?? ""));
            userClaims.AddRange(roleClaims); new Claim(ClaimTypes.Name, user.UserName ?? "");
            userClaims.AddRange(appSettings.Audiences.Select(x => new Claim(JwtRegisteredClaimNames.Aud, x)));

            JwtSecurityToken tokeOptions =
                new JwtSecurityToken(
                    issuer: appSettings.Issuer,
                    audience: appSettings.Audience,
                    claims: userClaims,
                    expires: DateTime.UtcNow.AddSeconds(appSettings.TokenExpireSeconds),
                    signingCredentials: signInCredentials
                );

            string tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);
            return tokenString;
        }
    }
}
