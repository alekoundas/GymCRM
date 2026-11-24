using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tests.Shared;

namespace DataAccess.Tests.Fakes
{
    /// <summary>
    /// The services the initialiser takes from the app: Identity's managers on a test's database,
    /// either of which can be told to go wrong.
    /// </summary>
    internal sealed class IdentityServices : IDisposable
    {
        private readonly ServiceProvider _provider;

        public IdentityServices(TestDatabase database)
        {
            ServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IConfiguration>(database.Configuration);
            services.AddDbContext<ApiDbContext>();
            services.AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<ApiDbContext>();

            // A real hash takes a tenth of a second on purpose. Nothing here is about how strong it is.
            services.Configure<PasswordHasherOptions>(x => x.IterationCount = 1000);

            services.AddSingleton(Roles);
            services.AddSingleton(Users);
            services.AddScoped<RoleManager<Role>, RiggedRoleManager>();
            services.AddScoped<UserManager<User>, RiggedUserManager>();

            _provider = services.BuildServiceProvider();
        }

        public RiggedRoles Roles { get; } = new RiggedRoles();

        public RiggedUsers Users { get; } = new RiggedUsers();

        public RecordingLogger<ApiDbContextInitialiser> Log { get; } = new RecordingLogger<ApiDbContextInitialiser>();

        public ApiDbContextInitialiser Initialiser(TestDatabase database)
        {
            return new ApiDbContextInitialiser(Log, database.Factory, _provider);
        }

        public void Dispose()
        {
            _provider.Dispose();
        }
    }

    /// <summary>
    /// What to have the role manager do instead of the real thing. Null is the real thing.
    /// </summary>
    internal sealed class RiggedRoles
    {
        public Func<string, IdentityResult?>? Create { get; set; }

        public Func<string, IdentityResult?>? AddClaim { get; set; }

        public Func<string, bool?>? Exists { get; set; }

        public Func<string, bool>? IsMissing { get; set; }

        public Func<Role, IList<global::System.Security.Claims.Claim>?>? Claims { get; set; }
    }

    internal sealed class RiggedUsers
    {
        public Func<string, IdentityResult?>? Create { get; set; }

        public Func<string, IdentityResult?>? AddToRole { get; set; }

        public Func<string, IList<string>?>? Roles { get; set; }

        public List<string> RemovedFromRoles { get; } = new List<string>();
    }

    internal sealed class RiggedRoleManager : RoleManager<Role>
    {
        private readonly RiggedRoles _rigged;

        public RiggedRoleManager(
            RiggedRoles rigged,
            IRoleStore<Role> store,
            IEnumerable<IRoleValidator<Role>> validators,
            ILookupNormalizer normalizer,
            IdentityErrorDescriber errors,
            ILogger<RoleManager<Role>> logger) : base(store, validators, normalizer, errors, logger)
        {
            _rigged = rigged;
        }

        public override Task<bool> RoleExistsAsync(string roleName)
        {
            bool? answer = _rigged.Exists?.Invoke(roleName);

            return answer == null ? base.RoleExistsAsync(roleName) : Task.FromResult(answer.Value);
        }

        public override Task<Role?> FindByNameAsync(string roleName)
        {
            return _rigged.IsMissing?.Invoke(roleName) == true ? Task.FromResult<Role?>(null) : base.FindByNameAsync(roleName);
        }

        public override Task<IdentityResult> CreateAsync(Role role)
        {
            IdentityResult? answer = _rigged.Create?.Invoke(role.Name!);

            return answer == null ? base.CreateAsync(role) : Task.FromResult(answer);
        }

        public override Task<IdentityResult> AddClaimAsync(Role role, global::System.Security.Claims.Claim claim)
        {
            IdentityResult? answer = _rigged.AddClaim?.Invoke(claim.Value);

            return answer == null ? base.AddClaimAsync(role, claim) : Task.FromResult(answer);
        }

        public override Task<IList<global::System.Security.Claims.Claim>> GetClaimsAsync(Role role)
        {
            IList<global::System.Security.Claims.Claim>? answer = _rigged.Claims?.Invoke(role);

            return answer == null ? base.GetClaimsAsync(role) : Task.FromResult(answer);
        }
    }

    internal sealed class RiggedUserManager : UserManager<User>
    {
        private readonly RiggedUsers _rigged;

        public RiggedUserManager(
            RiggedUsers rigged,
            IUserStore<User> store,
            IOptions<IdentityOptions> options,
            IPasswordHasher<User> hasher,
            IEnumerable<IUserValidator<User>> userValidators,
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer normalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<User>> logger) : base(store, options, hasher, userValidators, passwordValidators, normalizer, errors, services, logger)
        {
            _rigged = rigged;
        }

        public override Task<IdentityResult> CreateAsync(User user, string password)
        {
            IdentityResult? answer = _rigged.Create?.Invoke(user.UserName!);

            return answer == null ? base.CreateAsync(user, password) : Task.FromResult(answer);
        }

        public override Task<IdentityResult> AddToRoleAsync(User user, string role)
        {
            IdentityResult? answer = _rigged.AddToRole?.Invoke(user.UserName!);

            return answer == null ? base.AddToRoleAsync(user, role) : Task.FromResult(answer);
        }

        public override Task<IList<string>> GetRolesAsync(User user)
        {
            IList<string>? answer = _rigged.Roles?.Invoke(user.UserName!);

            return answer == null ? base.GetRolesAsync(user) : Task.FromResult(answer);
        }

        public override Task<IdentityResult> RemoveFromRolesAsync(User user, IEnumerable<string> roles)
        {
            _rigged.RemovedFromRoles.Add($"{user.UserName}: {string.Join(", ", roles)}");

            return Task.FromResult(IdentityResult.Success);
        }
    }
}
