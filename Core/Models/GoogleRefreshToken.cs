namespace Core.Models
{
    public class GoogleRefreshToken : BaseModel
    {
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
