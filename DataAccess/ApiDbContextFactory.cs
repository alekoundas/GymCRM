using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess
{
    public class ApiDbContextFactory : IDbContextFactory<ApiDbContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ApiDbContextInitialiser _apiDbContextInitialiser;

        public ApiDbContextFactory(IConfiguration configuration, ApiDbContextInitialiser apiDbContextInitialiser)
        {
            _configuration = configuration;
            _apiDbContextInitialiser = apiDbContextInitialiser;
        }

        public ApiDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
            optionsBuilder.UseSqlite(_configuration.GetConnectionString("DefaultConnection"));
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
            return new ApiDbContext(optionsBuilder.Options, _configuration, _apiDbContextInitialiser);
        }
    }
}
