namespace Core.System
{
    public class TokenSettings
    {
        public string Issuer { get; set; } = "";
        public List<string> Issuers { get; set; } = new List<string>();
        public string Audience { get; set; } = "";
        public List<string> Audiences { get; set; } = new List<string>();
        public string SecretKey { get; set; } = "";
        public int TokenExpireSeconds { get; set; }
        public int RefreshTokenExpireSeconds { get; set; }
        public bool ValidateIssuer { get; set; } = true;
        public bool ValidateAudience { get; set; } = true;
        public bool ValidateLifetime { get; set; } = true;
    }
}
