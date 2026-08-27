using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
   public class WorkoutPlanConfiguration : IEntityTypeConfiguration<WorkoutPlan>
    {
        public void Configure(EntityTypeBuilder<WorkoutPlan> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            // Properties
            builder.Property(x => x.Title)
               .IsRequired(true)
               .HasMaxLength(500);

            builder.Property(x => x.Description)
                .IsRequired(false)
                .HasMaxLength(500);


            // Relationship with User (one-to-many)
            builder.HasOne(x => x.User)
                .WithMany(x=>x.WorkoutPlans)
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed

            // Optional rule. SetNull rather than Restrict: deleting a rule leaves the
            // plans without one, which simply disables Start until an admin reassigns.
            builder.HasOne(x => x.WorkoutPlanRule)
                .WithMany(x => x.WorkoutPlans)
                .HasForeignKey(x => x.WorkoutPlanRuleId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
