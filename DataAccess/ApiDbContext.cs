using Core.Models;
using DataAccess.Configurations;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess
{
    public class ApiDbContext : IdentityDbContext<
        User,                          // User type
        Role,                          // Role type
        Guid,                          // Key type
        IdentityUserClaim<Guid>,       // User claim type (default)
        UserRole,                      // Custom user-role type
        IdentityUserLogin<Guid>,       // User login type (default)
        IdentityRoleClaim<Guid>,       // Role claim type (default)
        IdentityUserToken<Guid>        // User token type (default)
    >, IDataProtectionKeyContext
    {
        private readonly IConfiguration _configuration;

        public ApiDbContext(DbContextOptions<ApiDbContext> options, IConfiguration configuration)
        : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<Mail> Mails { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<TrainGroup> TrainGroups { get; set; }
        public DbSet<UserStatus> UserStatuses { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<PhoneNumber> PhoneNumbers { get; set; }
        public DbSet<TrainGroupDate> TrainGroupDates { get; set; }
        public DbSet<TrainGroupParticipant> TrainGroupParticipants { get; set; }
        public DbSet<TrainGroupUnavailableDate> TrainGroupUnavailableDates { get; set; }
        public DbSet<TrainGroupParticipantUnavailableDate> TrainGroupParticipantUnavailableDates { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<GoogleRefreshToken> GoogleRefreshTokens { get; set; }

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

            builder.ApplyConfiguration(new MailConfiguration());
            builder.ApplyConfiguration(new UserConfiguration());
            //builder.ApplyConfiguration(new RoleConfiguration());
            builder.ApplyConfiguration(new UserRoleConfiguration());
            builder.ApplyConfiguration(new ExerciseConfiguration());
            builder.ApplyConfiguration(new UserStatusConfiguration());
            builder.ApplyConfiguration(new TrainGroupConfiguration());
            builder.ApplyConfiguration(new PhoneNumberConfiguration());
            builder.ApplyConfiguration(new WorkoutPlanConfiguration());
            builder.ApplyConfiguration(new TrainGroupDateConfiguration());
            builder.ApplyConfiguration(new TrainGroupParticipantConfiguration());
            builder.ApplyConfiguration(new TrainGroupUnavailableDateConfiguration());
            builder.ApplyConfiguration(new TrainGroupParticipantUnavailableDateConfiguration());


            builder.ApplyConfiguration(new GoogleRefreshTokenConfiguration());
        }

        public void RunMigrations()
        {
            this.Database.Migrate();
        }
    }
}
