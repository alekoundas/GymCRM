using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DataAccess
{
    public class ApiDbContextInitialiser
    {
        private readonly ILogger<ApiDbContextInitialiser> _logger;
        private readonly IDbContextFactory<ApiDbContext> _dbContextFactory;
        private readonly IServiceProvider _serviceProvider;

        public ApiDbContextInitialiser(
            ILogger<ApiDbContextInitialiser> logger,
            IDbContextFactory<ApiDbContext> dbContextFactory,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _serviceProvider = serviceProvider;
        }

        public async Task RunMigrationsAsync()
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running migrations.");
                throw;
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

                await TrySeedRolesAndClaimsAsync(roleManager);
                await TrySeedAdminUserAsync(userManager, roleManager);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private async Task TrySeedRolesAndClaimsAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            var administratorRole = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Administrator", NormalizedName = "ADMINISTRATOR" };

            if (!await roleManager.RoleExistsAsync(administratorRole.Name))
            {
                var roleResult = await roleManager.CreateAsync(administratorRole);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");

                var claims = new List<Claim>
                {
                    new Claim("Permission", "ContactInformations_View"),
                    new Claim("Permission", "ContactInformations_Add"),
                    new Claim("Permission", "ContactInformations_Edit"),
                    new Claim("Permission", "ContactInformations_Delete"),
                    new Claim("Permission", "Roles_View"),
                    new Claim("Permission", "Roles_Add"),
                    new Claim("Permission", "Roles_Edit"),
                    new Claim("Permission", "Roles_Delete"),
                    new Claim("Permission", "Users_View"),
                    new Claim("Permission", "Users_Add"),
                    new Claim("Permission", "Users_Edit"),
                    new Claim("Permission", "Users_Delete"),
                    new Claim("Permission", "TrainGroup_View"),
                    new Claim("Permission", "TrainGroup_Add"),
                    new Claim("Permission", "TrainGroup_Edit"),
                    new Claim("Permission", "TrainGroup_Delete"),
                    new Claim("Permission", "TrainGroupDate_View"),
                    new Claim("Permission", "TrainGroupDate_Add"),
                    new Claim("Permission", "TrainGroupDate_Edit"),
                    new Claim("Permission", "TrainGroupDate_Delete"),
                };

                foreach (var claim in claims)
                {
                    var claimResult = await roleManager.AddClaimAsync(administratorRole, claim);
                    if (!claimResult.Succeeded)
                        throw new InvalidOperationException($"Failed to add claim {claim.Value}: {string.Join(", ", claimResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private async Task TrySeedAdminUserAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            var administrator = new User
            {
                Id = Guid.NewGuid(),
                UserName = "Admin",
                Email = "Admin@Admin.Admin",
                FirstName = "Admin",
                LastName = "User"
            };

            if (await userManager.FindByNameAsync(administrator.UserName) == null)
            {
                var userResult = await userManager.CreateAsync(administrator, "P@ssw0rd");
                if (!userResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create user: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");

                if (await roleManager.RoleExistsAsync("Administrator"))
                {
                    // Enforce single role
                    var currentRoles = await userManager.GetRolesAsync(administrator);
                    if (currentRoles.Any())
                        await userManager.RemoveFromRolesAsync(administrator, currentRoles);

                    var roleResult = await userManager.AddToRoleAsync(administrator, "Administrator");
                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException($"Failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}