using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class TrainGroupUnavailableDateConfiguration : IEntityTypeConfiguration<TrainGroupUnavailableDate>
    {
        public void Configure(EntityTypeBuilder<TrainGroupUnavailableDate> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);


            // Properties
            builder.Property(x => x.UnavailableDate)
              .IsRequired();

            // Relationship with User (one-to-many)
            builder.HasOne(x => x.TrainGroup)
                .WithMany(x=>x.TrainGroupUnavailableDates)
                .HasForeignKey(x => x.TrainGroupId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent TrainGroupParticipant is deleted
        }
    }
}
