using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class TrainGroupDateConfiguration : IEntityTypeConfiguration<TrainGroupDate>
    {
        public void Configure(EntityTypeBuilder<TrainGroupDate> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            // Properties
            // Store enum as string
            builder.Property(x => x.RepeatingTrainGroupType)
                .HasConversion<string>(); 

            builder.Property(x => x.StartOn)
                .IsRequired();

            builder.Property(x => x.FixedDay)
                .IsRequired(false); // Nullable

            builder.Property(x => x.RecurrenceDayOfWeek)
                .IsRequired(false); // Nullable, range 0-6 validated by data annotation

            builder.Property(x => x.RecurrenceDayOfMonth)
                .IsRequired(false); // Nullable, range 1-31 validated by data annotation


            // Relationship with TrainGroup (many-to-one)
            builder.HasOne(x => x.TrainGroup)
                .WithMany(x => x.TrainGroupDates)
                .HasForeignKey(x => x.TrainGroupId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent group is deleted

            // Relationship with Participants (one-to-many)
            builder.HasMany(x => x.TrainGroupParticipants)
                .WithOne(x => x.TrainGroupDate)
                .HasForeignKey(x => x.TrainGroupDateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete participants if date is deleted

            // Relationship with CancellationSubscribers (one-to-many)
            builder.HasMany(x => x.TrainGroupCancellationSubscribers)
                .WithOne(x => x.TrainGroupDate)
                .HasForeignKey(x => x.TrainGroupDateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete subscribers if date is deleted
        }
    }
}
