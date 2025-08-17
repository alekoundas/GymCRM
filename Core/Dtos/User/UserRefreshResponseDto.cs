namespace Core.Dtos
{
    public class UserRefreshResponseDto
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}
