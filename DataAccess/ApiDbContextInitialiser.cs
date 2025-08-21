using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DataAccess
{
    public class ApiDbContextInitialiser
    {
        private readonly ILogger<ApiDbContextInitialiser> _logger;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ClaimsIdentity _claimsIdentity;



        public ApiDbContextInitialiser(
            ILogger<ApiDbContextInitialiser> logger,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ClaimsIdentity claimsIdentity)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _claimsIdentity = claimsIdentity;
        }


        public async Task SeedAsync()
        {
            try
            {
                TrySeedClaimsAsync();
                await TrySeedAdminUserAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
       

        private void TrySeedClaimsAsync()
        {
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Customers_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Customers_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Customers_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Customers_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Tickets_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Tickets_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Tickets_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Tickets_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "ContactInformations_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "ContactInformations_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "ContactInformations_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "ContactInformations_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Roles_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Roles_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Roles_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Roles_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Makers_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Makers_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Makers_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Makers_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "MakerModels_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "MakerModels_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "MakerModels_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "MakerModels_Delete"));

        }

        private async Task TrySeedAdminUserAsync()
        {
            // Default roles
            var administratorRole = new IdentityRole("Administrator");

            if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
            {
                var role = await _roleManager.CreateAsync(administratorRole);
                if (role != null)
                    _claimsIdentity.Claims
                        .Select(x => x.Value)
                        .ToList()
                        .ForEach(async x =>
                            await _roleManager.AddClaimAsync(administratorRole, _claimsIdentity.Claims.First(y => y.Value == x))
                         );
            }

            // Default users
            var administrator = new User { UserName = "Admin", Email = "Admin@Admin.Admin" };

            if (_userManager.Users.All(u => u.UserName != administrator.UserName))
            {
                await _userManager.CreateAsync(administrator, "P@ssw0rd");
                if (!string.IsNullOrWhiteSpace(administratorRole.Name))
                {
                    await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
                }
            }
        }
    }
}