using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

        public UsersController(
            IDataService dataService,
            IMapper mapper,
            ILogger<UsersController> logger,
            IUserService userService,
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _dataService = dataService;
            _logger = logger;
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }


        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<User>> Get(string? id)
        {
            if (id == null)
                return new ApiResponse<User>().SetErrorResponse("errors", "Role ID not set!");

            User? user = await _dataService.Users.FirstOrDefaultAsync(x => x.Id.ToString() == id);
            if (user == null)
                return new ApiResponse<User>().SetErrorResponse("errors", "Role not found!");

            return new ApiResponse<User>().SetSuccessResponse(user);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<ApiResponse<bool>> Update(Guid id, UserUpdateRequestDto request)
        {
            if (id.ToString() != request.Id)
                return new ApiResponse<bool>().SetErrorResponse("errors", "ID mismatch");


            User? user = await _dataService.Users.FirstOrDefaultAsync(x => x.Id.ToString() == request.Id);
            if (user == null)
                return new ApiResponse<bool>().SetErrorResponse("errors", "User not found");


            IdentityRole<Guid>? role = await _dataService.Roles.FirstOrDefaultAsync(x => x.Id.ToString() == request.RoleId);
            if (role?.Name == null)
                return new ApiResponse<bool>().SetErrorResponse("errors", "Role not found");


            // Update user properties
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.UserName = request.UserName;

            await _userService.AssignSingleRoleAsync(user, role.Name);
            await _userManager.UpdateAsync(user);

            return new ApiResponse<bool>().SetSuccessResponse(true, "success", "User updated successfully");
        }

        // POST: api/Users/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<UserDto>>> GetDataTable([FromBody] DataTableDto<UserDto> dataTable)
        {

            // Retrieve Data.
            List<User> applicationUsers = await _dataService.Users.ToListAsync();
            applicationUsers.ForEach(async x =>
            {
                List<string> rolelist = (await _userManager.GetRolesAsync(x)).ToList();

                if (rolelist.Count > 0)
                {
                    string userRole = (await _userManager.GetRolesAsync(x)).First();
                    IdentityRole<Guid>? role = await _roleManager.FindByNameAsync(userRole);
                    //if (role != null)
                    //    x.RoleId = role.Id;
                }
            });
            List<UserDto> userDto = _mapper.Map<List<UserDto>>(applicationUsers);


            dataTable.Data = userDto;
            dataTable.PageCount = 0;
            return new ApiResponse<DataTableDto<UserDto>>().SetSuccessResponse(dataTable);
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

            string? result = await _userService.AddNewUser(user, request.Password);
            if (result == null)
                return new ApiResponse<bool>().SetSuccessResponse(true);
            else
                return new ApiResponse<bool>().SetErrorResponse("error", result);
        }


        // POST: api/Users/Login
        [HttpPost("Login")]
        public async Task<ApiResponse<UserLoginResponseDto>> Login(UserLoginRequestDto request)
        {

            User? user = await _userManager.FindByEmailAsync(request.UserNameOrEmail);
            if(user == null)
                user = await _userManager.FindByNameAsync(request.UserNameOrEmail);

            if (user == null)
                return new ApiResponse<UserLoginResponseDto>().SetErrorResponse("email", "User Name/Email not found");


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
