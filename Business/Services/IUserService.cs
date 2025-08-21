using Core.Dtos;
using Core.Models;

namespace Business.Services
{
    public interface IUserService
    {
        Task<string?> SignInUser(User user, string password);
        Task<string?> SignOutUser(User user);
        Task<string?> AddNewUser(User user, string password);

        Task AssignSingleRoleAsync(User user, string roleName);
        Task<UserLoginResponseDto> GenerateUserToken(User user);
        Task<ApiResponse<UserRefreshResponseDto>> RefreshToken(UserRefreshRequestDto request);

    }
}
