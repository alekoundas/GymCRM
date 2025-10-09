
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos
{
    public class UserLoginResponseDto
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public byte[]? ProfileImage { get; set; }
    }
}
