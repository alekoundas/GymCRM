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

        public ApiDbContext(DbContextOptions<ApiDbContext> options, IConfiguration configuration)
        : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<TrainGroup> TrainGroups { get; set; }
        public DbSet<PhoneNumber> PhoneNumbers { get; set; }
        public DbSet<TrainGroupDate> TrainGroupDates { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<TrainGroupParticipant> TrainGroupParticipants { get; set; }
        public DbSet<TrainGroupDateCancellationSubscriber> TrainGroupCancellationSubscribers { get; set; }


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

            builder.ApplyConfiguration(new UserConfiguration());
            builder.ApplyConfiguration(new TrainGroupConfiguration());
            builder.ApplyConfiguration(new PhoneNumberConfiguration());
            builder.ApplyConfiguration(new TrainGroupDateConfiguration());
            builder.ApplyConfiguration(new TrainGroupParticipantConfiguration());
            builder.ApplyConfiguration(new TrainGroupDateCancellationSubscriberConfiguration());
            builder.ApplyConfiguration(new TrainGroupParticipantUnavailableDateConfiguration());
        }

        public void RunMigrations()
        {
            this.Database.Migrate();
        }
    }
}
