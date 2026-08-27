using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class WorkoutPlanRuleConfiguration : IEntityTypeConfiguration<WorkoutPlanRule>
    {
        public void Configure(EntityTypeBuilder<WorkoutPlanRule> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired(true)
                .HasMaxLength(200);

            builder.HasIndex(x => x.Name).IsUnique();

            builder.HasMany(x => x.Weeks)
                .WithOne(x => x.WorkoutPlanRule)
                .HasForeignKey(x => x.WorkoutPlanRuleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
