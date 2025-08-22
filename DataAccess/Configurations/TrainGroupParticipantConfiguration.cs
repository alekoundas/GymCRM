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

            // Properties
            builder.Property(x => x.SelectedDate)
                .IsRequired();

            // Relationship with TrainGroupDate (many-to-one)
            builder.HasOne(x => x.TrainGroupDate)
                .WithMany(x => x.TrainGroupParticipants)
                .HasForeignKey(x => x.TrainGroupDateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent date is deleted

            // Relationship with Participant (User)
            builder.HasOne(x => x.Participant)
                .WithMany(x=>x.TrainGroupParticipants)
                .HasForeignKey(x => x.ParticipantId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        }
    }
}
