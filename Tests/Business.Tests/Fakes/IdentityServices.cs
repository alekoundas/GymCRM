using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tests.Shared;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// ASP.NET Core's Identity on a test's database, set up the way Program.cs does it.
    /// </summary>
    internal sealed class IdentityServices : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public IdentityServices(TestDatabase database, int refreshTokenSeconds = 3600)
        {
            ServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddAuthentication();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddSingleton<IConfiguration>(database.Configuration);
            services.AddDbContext<ApiDbContext>();

            services.AddIdentityCore<User>(options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireDigit = true;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                })
                .AddRoles<Role>()
                .AddSignInManager()
                .AddEntityFrameworkStores<ApiDbContext>()
                .AddTokenProvider<DataProtectorTokenProvider<User>>("REFRESHTOKENPROVIDER");

            services.Configure<DataProtectionTokenProviderOptions>(x => x.TokenLifespan = TimeSpan.FromSeconds(refreshTokenSeconds));

            // A real hash takes a tenth of a second on purpose. Nothing here is about how strong it is.
            services.Configure<PasswordHasherOptions>(x => x.IterationCount = 1000);

            services.AddSingleton(this);
            services.AddScoped<RoleManager<Role>, ForgetfulRoleManager>();

            _provider = services.BuildServiceProvider();
            _scope = _provider.CreateScope();
        }

        public UserManager<User> Users
        {
            get { return _scope.ServiceProvider.GetRequiredService<UserManager<User>>(); }
        }

        public RoleManager<Role> Roles
        {
            get { return _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>(); }
        }

        public SignInManager<User> SignIn
        {
            get { return _scope.ServiceProvider.GetRequiredService<SignInManager<User>>(); }
        }

        /// <summary>
        /// A role the manager cannot find, though its members still have it.
        /// </summary>
        public string? ForgottenRole { get; set; }

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
        }

        private sealed class ForgetfulRoleManager : RoleManager<Role>
        {
            private readonly IdentityServices _services;

            public ForgetfulRoleManager(
                IdentityServices services,
                IRoleStore<Role> store,
                IEnumerable<IRoleValidator<Role>> validators,
                ILookupNormalizer normalizer,
                IdentityErrorDescriber errors,
                ILogger<RoleManager<Role>> logger) : base(store, validators, normalizer, errors, logger)
            {
                _services = services;
            }

            public override Task<Role?> FindByNameAsync(string roleName)
            {
                return roleName == _services.ForgottenRole ? Task.FromResult<Role?>(null) : base.FindByNameAsync(roleName);
            }
        }
    }
}
