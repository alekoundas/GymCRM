using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class WorkoutPlanRecordingConfiguration : IEntityTypeConfiguration<WorkoutPlanRecording>
    {
        public void Configure(EntityTypeBuilder<WorkoutPlanRecording> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.WorkoutPlanTitle)
                .IsRequired(false)
                .HasMaxLength(500);

            // Deliberately SetNull, not Cascade: deleting a plan must keep its history.
            builder.HasOne(x => x.WorkoutPlan)
                .WithMany(x => x.Recordings)
                .HasForeignKey(x => x.WorkoutPlanId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.WorkoutPlanId, x.StartedOn });
        }
    }
}
