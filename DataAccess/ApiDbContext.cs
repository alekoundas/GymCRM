using Core.Models;
using DataAccess.Configurations;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess
{
    public class ApiDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
    {
        private readonly IConfiguration _configuration;
        //private readonly ApiDbContextInitialiser _apiDbContextInitialiser;

        public ApiDbContext(DbContextOptions<ApiDbContext> options, IConfiguration configuration)//, ApiDbContextInitialiser apiDbContextInitialiser)
        : base(options)
        {
            _configuration = configuration;
            //_apiDbContextInitialiser = apiDbContextInitialiser;
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<ContactInformation> ContactInformations { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            //optionsBuilder.UseSqlite("Data Source=" + Directory.GetCurrentDirectory() + "/sqlite.db");
            optionsBuilder.UseSqlite(_configuration.GetConnectionString("DefaultConnection"));


            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new CustomerConfiguration());
            builder.ApplyConfiguration(new ContactInformationConfiguration());
            builder.ApplyConfiguration(new UserConfiguration());
        }

        public void RunMigrations()
        {
            this.Database.Migrate();
        }

        //public async Task TrySeedInitialData()
        //{
        //    await _apiDbContextInitialiser.SeedAsync();
        //}
    }
}
