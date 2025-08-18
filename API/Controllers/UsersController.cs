using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Business.Services;
using Core.Models;
using Core.System;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenSettings _tokenSettings;

        public UsersController(
            IDataService dataService,
            IMapper mapper,
            ILogger<UsersController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            TokenSettings tokenSettings)
        {
            _dataService = dataService;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenSettings = tokenSettings;
            _mapper = mapper;
        }


        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<ApplicationUser>> Get(string? id)
        {
            if (id == null)
                return new ApiResponse<ApplicationUser>().SetErrorResponse("errors", "Role ID not set!");

            ApplicationUser? applicationUser = await _userManager.FindByIdAsync(id);
            if (applicationUser == null)
                return new ApiResponse<ApplicationUser>().SetErrorResponse("errors", "Role not found!");

            return new ApiResponse<ApplicationUser>().SetSuccessResponse(applicationUser, "success", "Role not found!");
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, ApplicationUser applicationUser)
        {
            if (id != applicationUser.Id)
                return BadRequest();

            if (applicationUser.Id == null)
                return BadRequest();


            ApplicationUser? user = await _userManager.FindByIdAsync(applicationUser.Id);
            if (user == null)
                return BadRequest();

            user.RoleId = applicationUser.RoleId;
            IdentityRole? role = await _roleManager.FindByIdAsync(applicationUser.RoleId);
            if (role == null)
                return BadRequest();


            List<string> userRoles = (await _userManager.GetRolesAsync(user)).ToList();
            await _userManager.RemoveFromRolesAsync(user, userRoles);
            await _userManager.AddToRoleAsync(user, role.Name);
            await _userManager.UpdateAsync(user);

            //await _userManager.RemoveFromRoleAsync()

            return Ok();
        }

        // POST: api/Users/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<UserDto>>> GetDataTable([FromBody] DataTableDto<UserDto> dataTable)
        {
            //Func<IQueryable<IdentityRole>, IOrderedQueryable<IdentityRole>>? orderByQuery = null;
            //List<Func<IOrderedQueryable<IdentityRole>, IOrderedQueryable<IdentityRole>>>? thenOrderByQuery = new List<Func<IOrderedQueryable<IdentityRole>, IOrderedQueryable<IdentityRole>>>();
            //Expression<Func<IdentityRole, bool>>? filterQuery = new Expression<Func<IdentityRole, bool>>();


            //// Handle Sorting of DataTable.
            //if (dataTable.MultiSortMeta?.Count() > 0)
            //{
            //    // Create the first OrderBy().
            //    DataTableSortDto? dataTableSort = dataTable.MultiSortMeta.First();
            //    if (dataTableSort.Order > 0)
            //        orderByQuery = x => x.OrderByColumn(dataTableSort.Field);
            //    else if (dataTableSort.Order < 0)
            //        orderByQuery = x => x.OrderByColumnDescending(dataTableSort.Field);

            //    // Create the rest OrderBy methods as ThenBy() if any.
            //    foreach (var sortInfo in dataTable.MultiSortMeta.Skip(1))
            //    {
            //        if (dataTableSort.Order > 0)
            //            thenOrderByQuery.Add(x => x.ThenByColumn(sortInfo.Field));
            //        else if (dataTableSort.Order < 0)
            //            thenOrderByQuery.Add(x => x.ThenByColumnDescending(sortInfo.Field));
            //    }
            //}


            // Handle Filtering of DataTable.
            //if (dataTable.Filters?.FirstName?.Value != null && dataTable.Filters?.FirstName.Value.Length > 0)
            //    filterQuery.Add(x => x.FirstName.Contains(dataTable.Filters.FirstName.Value));

            //if (dataTable.Filters?.LastName?.Value != null && dataTable.Filters?.LastName.Value.Length > 0)
            //    filterQuery.Add(x => x.LastName.Contains(dataTable.Filters.LastName.Value));



            // Retrieve Data.
            List<ApplicationUser> applicationUsers = await _dataService.Query.Users.ToListAsync();
            applicationUsers.ForEach(async x =>
            {
                List<string> rolelist = (await _userManager.GetRolesAsync(x)).ToList();

                if (rolelist.Count > 0)
                {
                    string userRole = (await _userManager.GetRolesAsync(x)).First();
                    IdentityRole? role = await _roleManager.FindByNameAsync(userRole);
                    if (role != null)
                        x.RoleId = await _roleManager.GetRoleIdAsync(role);
                }
            });


            //    (
            //    filterQuery,
            //    orderByQuery,
            //    thenOrderByQuery,
            //    null,
            //    dataTable.PageCount.Value,
            //    dataTable.Page.Value
            //);
            List<UserDto> userDto = _mapper.Map<List<UserDto>>(applicationUsers);

            //IdentityRoleDto.SelectMany(x => x.ContactInformations).ToList().ForEach(x => x.IdentityRole = null);
            //IdentityRoleDto.SelectMany(x => x.Tickets).ToList().ForEach(x => x.IdentityRole = null);

            //int rows = await _dataService.IdentityRoles.co(filterQuery);

            dataTable.Data = userDto;
            dataTable.PageCount = 0;
            return new ApiResponse<DataTableDto<UserDto>>().SetSuccessResponse(dataTable);
        }

        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<ApiResponse<bool>> Register(UserRegisterRequestDto request)
        {
            ApplicationUser user = new ApplicationUser()
            {
                UserName = request.UserName,
                Email = request.Email,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
                return new ApiResponse<bool>().SetSuccessResponse(true);
            else
                return new ApiResponse<bool>().SetErrorResponse(GetRegisterErrors(result));
        }

        private Dictionary<string, string[]> GetRegisterErrors(IdentityResult result)
        {
            var errorDictionary = new Dictionary<string, string[]>(1);

            foreach (var error in result.Errors)
            {
                string[] newDescriptions;

                if (errorDictionary.TryGetValue(error.Code, out var descriptions))
                {
                    newDescriptions = new string[descriptions.Length + 1];
                    Array.Copy(descriptions, newDescriptions, descriptions.Length);
                    newDescriptions[descriptions.Length] = error.Description;
                }
                else
                    newDescriptions = [error.Description];

                errorDictionary[error.Code] = newDescriptions;
            }

            return errorDictionary;
        }

        // POST: api/Users/Login
        [HttpPost("Login")]
        public async Task<ApiResponse<UserLoginResponseDto>> Login(UserLoginRequestDto request)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(request.Email);
                if (user == null)
                    return new ApiResponse<UserLoginResponseDto>().SetErrorResponse("email", "Email not found");
            }


            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
            if (result.Succeeded)
            {
                var token = await GenerateUserToken(user);
                return new ApiResponse<UserLoginResponseDto>().SetSuccessResponse(token);
            }
            else
                return new ApiResponse<UserLoginResponseDto>().SetErrorResponse("password", result.ToString());
        }

        // POST: api/Users/RefreshToken
        [HttpPost("RefreshToken")]
        public async Task<ApiResponse<UserRefreshResponseDto>> RefreshToken(UserRefreshRequestDto request)
        {
            var principal = GetPrincipalFromExpiredToken(_tokenSettings, request.AccessToken);
            if (principal == null || principal.FindFirst("UserName")?.Value == null)
            {
                return new ApiResponse<UserRefreshResponseDto>().SetErrorResponse("email", "User not found");
            }
            else
            {
                var user = await _userManager.FindByNameAsync(principal.FindFirst("UserName")?.Value ?? "");
                if (user == null)
                {
                    return new ApiResponse<UserRefreshResponseDto>().SetErrorResponse("email", "User not found");
                }
                else
                {
                    if (!await _userManager.VerifyUserTokenAsync(user, "REFRESHTOKENPROVIDER", "RefreshToken", request.RefreshToken))
                        return new ApiResponse<UserRefreshResponseDto>().SetErrorResponse("token", "Refresh token expired");

                    var token = await GenerateUserToken(user);
                    return new ApiResponse<UserRefreshResponseDto>()
                        .SetSuccessResponse(new UserRefreshResponseDto() { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken });
                }
            }
        }


        // POST: api/Users/Logout
        [HttpPost("Logout")]
        public async Task<ApiResponse<bool>> Logout()
        {
            if (User.Identity?.IsAuthenticated == null || !User.Identity.IsAuthenticated)
                return new ApiResponse<bool>().SetSuccessResponse(true);

            string username = User.Claims.First(x => x.Type == "UserName").Value;
            ApplicationUser? appUser = await _dataService.Users.FirstOrDefaultAsync(x => x.UserName == username);

            if (appUser != null)
                await _userManager.UpdateSecurityStampAsync(appUser);

            return new ApiResponse<bool>().SetSuccessResponse(true);
        }

        [HttpPost("Profile")]
        [Authorize(Roles = "Administrator")]
        public ApiResponse<bool> Profile()
        {
            return new ApiResponse<bool>().SetSuccessResponse(true);

        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<ApiResponse<IdentityUser>> Delete(string? id)
        {
            if (id == null || id.Count() == 0)
                return new ApiResponse<IdentityUser>().SetErrorResponse("error", "User name not not set!");

            ApplicationUser? user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return new ApiResponse<IdentityUser>().SetErrorResponse("error", "User not found!");


            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return new ApiResponse<IdentityUser>().SetSuccessResponse("success", $"User {user.Email} deleted successfully");

            return new ApiResponse<IdentityUser>().SetErrorResponse("error", result.Errors.ToString() ?? "");
        }



        private async Task<UserLoginResponseDto> GenerateUserToken(Core.Models.ApplicationUser user)
        {
            List<Claim> claims = (from ur in _dataService.Query.UserRoles
                                  where ur.UserId == user.Id
                                  join r in _dataService.Query.Roles on ur.RoleId equals r.Id
                                  join rc in _dataService.Query.RoleClaims on r.Id equals rc.RoleId
                                  select rc)
              .Where(rc => !string.IsNullOrEmpty(rc.ClaimValue) && !string.IsNullOrEmpty(rc.ClaimType))
              .Select(rc => new Claim(rc.ClaimType!, rc.ClaimValue!))
              .Distinct()
              .ToList();

            List<Claim> roleClaims = (from ur in _dataService.Query.UserRoles
                                      where ur.UserId == user.Id
                                      join r in _dataService.Query.Roles on ur.RoleId equals r.Id
                                      select r)
              .Where(r => !string.IsNullOrEmpty(r.Name))
              .Select(r => new Claim(ClaimTypes.Role, r.Name!))
              .Distinct()
              .ToList();

            claims.AddRange(roleClaims);

            string token = GetToken(_tokenSettings, user, claims);
            await _userManager.RemoveAuthenticationTokenAsync(user, "REFRESHTOKENPROVIDER", "RefreshToken");
            string refreshToken = await _userManager.GenerateUserTokenAsync(user, "REFRESHTOKENPROVIDER", "RefreshToken");
            await _userManager.SetAuthenticationTokenAsync(user, "REFRESHTOKENPROVIDER", "RefreshToken", refreshToken);

            return new UserLoginResponseDto() { AccessToken = token, RefreshToken = refreshToken };
        }

        private string GetToken(TokenSettings appSettings, Core.Models.ApplicationUser user, List<Claim> roleClaims)
        {
            SymmetricSecurityKey secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.SecretKey));
            SigningCredentials signInCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            List<Claim> userClaims = new List<Claim>();
            userClaims.Add(new("Id", user.Id.ToString()));
            userClaims.Add(new("UserName", user.UserName ?? ""));
            userClaims.AddRange(roleClaims);
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
    }
}
