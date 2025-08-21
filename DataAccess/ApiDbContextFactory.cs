using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess
{
    public class ApiDbContextFactory : IDbContextFactory<ApiDbContext>
    {
        private readonly IConfiguration _configuration;

        public ApiDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ApiDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
            optionsBuilder.UseSqlite(_configuration.GetConnectionString("DefaultConnection"));
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
            return new ApiDbContext(optionsBuilder.Options, _configuration);
        }
    }
}
