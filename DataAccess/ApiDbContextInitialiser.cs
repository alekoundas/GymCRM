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

                await TrySeedAdminRolesAndClaimsAsync(roleManager);
                await TrySeedTrainerRolesAndClaimsAsync(roleManager);
                await TrySeedSimpleUserRolesAndClaimsAsync(roleManager);

                await TrySeedAdminUserAsync(userManager, roleManager);
                await TrySeedTrainerUserAsync(userManager, roleManager);
                await TrySeedSimpleUserUserAsync(userManager, roleManager);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private async Task TrySeedAdminRolesAndClaimsAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Administrator", NormalizedName = "ADMINISTRATOR" };

            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                var roleResult = await roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");

                var claims = new List<Claim>
                {
                    new Claim("Permission", "Roles_View"),
                    new Claim("Permission", "Roles_Add"),
                    new Claim("Permission", "Roles_Edit"),
                    new Claim("Permission", "Roles_Delete"),

                    new Claim("Permission", "Users_View"),
                    new Claim("Permission", "Users_Add"),
                    new Claim("Permission", "Users_Edit"),
                    new Claim("Permission", "Users_Delete"),

                    new Claim("Permission", "Mails_View"),
                    new Claim("Permission", "Mails_Add"),
                    new Claim("Permission", "Mails_Edit"),
                    new Claim("Permission", "Mails_Delete"),

                    //new Claim("Permission", "Claims_View"),
                    //new Claim("Permission", "Claims_Add"),
                    //new Claim("Permission", "Claims_Edit"),
                    //new Claim("Permission", "Claims_Delete"),
                    new Claim("Permission", "TrainGroups_View"),
                    new Claim("Permission", "TrainGroups_Add"),
                    new Claim("Permission", "TrainGroups_Edit"),
                    new Claim("Permission", "TrainGroups_Delete"),

                    new Claim("Permission", "TrainGroupDates_View"),
                    new Claim("Permission", "TrainGroupDates_Add"),
                    new Claim("Permission", "TrainGroupDates_Edit"),
                    new Claim("Permission", "TrainGroupDates_Delete"),

                    new Claim("Permission", "TrainGroupParticipants_View"),
                    new Claim("Permission", "TrainGroupParticipants_Add"),
                    new Claim("Permission", "TrainGroupParticipants_Edit"),
                    new Claim("Permission", "TrainGroupParticipants_Delete"),
                };

                foreach (var claim in claims)
                {
                    var claimResult = await roleManager.AddClaimAsync(role, claim);
                    if (!claimResult.Succeeded)
                        throw new InvalidOperationException($"Failed to add claim {claim.Value}: {string.Join(", ", claimResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private async Task TrySeedTrainerRolesAndClaimsAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "Trainer", NormalizedName = "TRAINER" };

            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                var roleResult = await roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");

                var claims = new List<Claim>
                {
                    new Claim("Permission", "Mails_View"),
                    new Claim("Permission", "Mails_Add"),
                    new Claim("Permission", "Mails_Edit"),
                    new Claim("Permission", "Mails_Delete"),

                    new Claim("Permission", "TrainGroups_View"),
                    new Claim("Permission", "TrainGroups_Add"),
                    new Claim("Permission", "TrainGroups_Edit"),
                    new Claim("Permission", "TrainGroups_Delete"),

                    new Claim("Permission", "TrainGroupDates_View"),
                    new Claim("Permission", "TrainGroupDates_Add"),
                    new Claim("Permission", "TrainGroupDates_Edit"),
                    new Claim("Permission", "TrainGroupDates_Delete"),

                    new Claim("Permission", "TrainGroupParticipants_View"),
                    new Claim("Permission", "TrainGroupParticipants_Add"),
                    new Claim("Permission", "TrainGroupParticipants_Edit"),
                    new Claim("Permission", "TrainGroupParticipants_Delete")
                };

                foreach (var claim in claims)
                {
                    var claimResult = await roleManager.AddClaimAsync(role, claim);
                    if (!claimResult.Succeeded)
                        throw new InvalidOperationException($"Failed to add claim {claim.Value}: {string.Join(", ", claimResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private async Task TrySeedSimpleUserRolesAndClaimsAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "SimpleUser", NormalizedName = "SIMPLEUSER" };

            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                var roleResult = await roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");

                var claims = new List<Claim>
                {
                };

                foreach (var claim in claims)
                {
                    var claimResult = await roleManager.AddClaimAsync(role, claim);
                    if (!claimResult.Succeeded)
                        throw new InvalidOperationException($"Failed to add claim {claim.Value}: {string.Join(", ", claimResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private async Task TrySeedAdminUserAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "Admin",
                Email = "Admin@Admin.Admin",
                FirstName = "Admin",
                LastName = "User"
            };

            if (await userManager.FindByNameAsync(user.UserName) == null)
            {
                var userResult = await userManager.CreateAsync(user, "P@ssw0rd");
                if (!userResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create user: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");

                if (await roleManager.RoleExistsAsync("Administrator"))
                {
                    // Enforce single role
                    var currentRoles = await userManager.GetRolesAsync(user);
                    if (currentRoles.Any())
                        await userManager.RemoveFromRolesAsync(user, currentRoles);

                    var roleResult = await userManager.AddToRoleAsync(user, "Administrator");
                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException($"Failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private async Task TrySeedTrainerUserAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "Trainer",
                Email = "Trainer@Trainer.Trainer",
                FirstName = "Trainer",
                LastName = "User"
            };

            if (await userManager.FindByNameAsync(user.UserName) == null)
            {
                var userResult = await userManager.CreateAsync(user, "P@ssw0rd");
                if (!userResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create user: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");

                if (await roleManager.RoleExistsAsync("Trainer"))
                {
                    // Enforce single role
                    var currentRoles = await userManager.GetRolesAsync(user);
                    if (currentRoles.Any())
                        await userManager.RemoveFromRolesAsync(user, currentRoles);

                    var roleResult = await userManager.AddToRoleAsync(user, "Trainer");
                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException($"Failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private async Task TrySeedSimpleUserUserAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "SimpleUser",
                Email = "SimpleUser@SimpleUser.SimpleUser",
                FirstName = "Simple",
                LastName = "User"
            };

            if (await userManager.FindByNameAsync(user.UserName) == null)
            {
                var userResult = await userManager.CreateAsync(user, "P@ssw0rd");
                if (!userResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create user: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");

                if (await roleManager.RoleExistsAsync("SimpleUser"))
                {
                    // Enforce single role
                    var currentRoles = await userManager.GetRolesAsync(user);
                    if (currentRoles.Any())
                        await userManager.RemoveFromRolesAsync(user, currentRoles);

                    var roleResult = await userManager.AddToRoleAsync(user, "SimpleUser");
                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException($"Failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}