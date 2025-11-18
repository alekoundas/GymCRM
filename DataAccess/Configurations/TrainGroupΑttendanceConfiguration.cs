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

            // Relationship with TrainGroup (one-to-many)
            builder.HasOne(x => x.TrainGroup)
                .WithMany(x => x.TrainGroupΑttendances)
                .HasForeignKey(x => x.TrainGroupId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Relationship with User (one-to-many)
            builder.HasOne(x => x.User)
                .WithMany(x => x.TrainGroupΑttendances)
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
