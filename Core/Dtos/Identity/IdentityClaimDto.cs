namespace Core.Dtos.Identity
{
    public class IdentityClaimDto
    {
        public string Controller { get; set; } = "";
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }

    }
}
