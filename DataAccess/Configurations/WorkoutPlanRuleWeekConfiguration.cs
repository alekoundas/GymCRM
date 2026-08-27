using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class WorkoutPlanRuleWeekConfiguration : IEntityTypeConfiguration<WorkoutPlanRuleWeek>
    {
        public void Configure(EntityTypeBuilder<WorkoutPlanRuleWeek> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Message)
                .IsRequired(false)
                .HasMaxLength(1000);

            builder.HasIndex(x => new { x.WorkoutPlanRuleId, x.WeekNumber }).IsUnique();
        }
    }
}
