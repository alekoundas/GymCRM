using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class TrainGroupParticipantConfiguration : IEntityTypeConfiguration<TrainGroupParticipant>
    {
        public void Configure(EntityTypeBuilder<TrainGroupParticipant> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            

            // Relationship with TrainGroupDate (one-to-many)
            builder.HasOne(x => x.TrainGroupDate)
                .WithMany(x => x.TrainGroupParticipants)
                .HasForeignKey(x => x.TrainGroupDateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent TrainGroupDate is deleted

            // Relationship with TrainGroup (one-to-many)
            builder.HasOne(x => x.TrainGroup)
                .WithMany(x => x.TrainGroupParticipants)
                .HasForeignKey(x => x.TrainGroupId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent TrainGroup is deleted

            // Relationship with User (one-to-many)
            builder.HasOne(x => x.User)
                .WithMany(x=>x.TrainGroupParticipants)
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if user TrainGroupDate is deleted
        }
    }
}
