using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
   public class TrainGroupConfiguration : IEntityTypeConfiguration<TrainGroup>
    {
        public void Configure(EntityTypeBuilder<TrainGroup> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            // Properties
            builder.Property(x => x.Duration)
                .IsRequired();

            builder.Property(x => x.MaxParticipants)
                .IsRequired();

            builder.Property(x => x.StartOn)
                .IsRequired();

            builder.Property(x => x.Title)
               .IsRequired(false)
               .HasMaxLength(500);

            builder.Property(x => x.Description)
                .IsRequired(false)
                .HasMaxLength(2000);


            // Relationship with Trainer (User)
            builder.HasOne(x => x.Trainer)
                .WithMany(x=>x.Trainers)
                .HasForeignKey(x => x.TrainerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Relationship with RepeatingParticipants (many-to-many with User)
            builder.HasMany(x => x.RepeatingParticipants)
                .WithMany(x=>x.RepeatingTrainGroups)
                .UsingEntity(j => j.ToTable("TrainGroupRepeatingParticipants"));

            // Relationship with TrainGroupDates (one-to-many)
            builder.HasMany(x => x.TrainGroupDates)
                .WithOne(x => x.TrainGroup)
                .HasForeignKey(x=> x.TrainGroupId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete dates if group is deleted
        }
    }
}
