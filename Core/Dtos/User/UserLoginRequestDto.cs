namespace Core.Dtos
{
    public class UserLoginRequestDto
    {
        public string UserNameOrEmail { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
