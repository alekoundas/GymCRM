using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class TrainGroupΑttendanceConfiguration : IEntityTypeConfiguration<TrainGroupΑttendance>
    {
        public void Configure(EntityTypeBuilder<TrainGroupΑttendance> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            // Properties
            builder.Property(x => x.AttendanceDate)
               .IsRequired(true);

            builder.Property(x => x.TrainGroupTitle).HasMaxLength(100);
            builder.Property(x => x.TrainGroupDescription).HasMaxLength(500);
            builder.Property(x => x.TrainerFullName).HasMaxLength(200);

            // Relationship with TrainGroup (one-to-many)
            // The attendance survives the group. Cascading here deleted the record that
            // somebody trained - and with it the count a member's remaining subscriptions
            // are worked out from, so deleting a group quietly handed credits back.
            builder.HasOne(x => x.TrainGroup)
                .WithMany(x => x.TrainGroupΑttendances)
                .HasForeignKey(x => x.TrainGroupId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);


            // Relationship with User (one-to-many)
            builder.HasOne(x => x.User)
                .WithMany(x => x.TrainGroupΑttendances)
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
