namespace Core.Dtos.Identity
{
    public class IdentityRoleDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; } 
        public string? NormalizedName { get; set; }
        public List<IdentityClaimDto> Claims { get; set; } = new List<IdentityClaimDto>();
    }
}
