using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class TrainGroupParticipantUnavailableDateConfiguration : IEntityTypeConfiguration<TrainGroupParticipantUnavailableDate>
    {
        public void Configure(EntityTypeBuilder<TrainGroupParticipantUnavailableDate> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);


            // Properties
            builder.Property(x => x.UnavailableDate)
              .IsRequired();

            // Relationship with User (one-to-many)
            builder.HasOne(x => x.TrainGroupParticipant)
                .WithMany(x=>x.TrainGroupParticipantUnavailableDates)
                .HasForeignKey(x => x.TrainGroupParticipantId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent TrainGroupParticipant is deleted
        }
    }
}
