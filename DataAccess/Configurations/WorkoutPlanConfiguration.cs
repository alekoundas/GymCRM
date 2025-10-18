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


            // Relationship with User (one-to-many)
            builder.HasOne(x => x.User)
                .WithMany(x=>x.WorkoutPlans)
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed

        }
    }
}
