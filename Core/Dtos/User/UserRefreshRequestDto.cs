namespace Core.Dtos
{
    public class UserRefreshRequestDto
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}
