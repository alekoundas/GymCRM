using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class TrainGroupParticipantConfiguration : IEntityTypeConfiguration<TrainGroupDateParticipant>
    {
        public void Configure(EntityTypeBuilder<TrainGroupDateParticipant> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            

            // Relationship with TrainGroupDate (one-to-many)
            builder.HasOne(x => x.TrainGroupDate)
                .WithMany(x => x.TrainGroupDateParticipants)
                .HasForeignKey(x => x.TrainGroupDateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent TrainGroupDate is deleted

            // Relationship with User (one-to-many)
            builder.HasOne(x => x.User)
                .WithMany(x=>x.TrainGroupDatesParticipant)
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if user TrainGroupDate is deleted
        }
    }
}
