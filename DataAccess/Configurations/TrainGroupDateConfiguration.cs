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
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.HasKey(x => x.Id);

            // Properties
            // Store enum as string
            builder.Property(x => x.TrainGroupDateType)
                .HasConversion<string>()
                .IsRequired(true);

            builder.Property(x => x.FixedDay)
                .IsRequired(false); 

            builder.Property(x => x.RecurrenceDayOfWeek)
                .IsRequired(false); 

            builder.Property(x => x.RecurrenceDayOfMonth)
                .IsRequired(false); 


            // Relationship with TrainGroup (one-to-many)
            builder.HasOne(x => x.TrainGroup)
                .WithMany(x => x.TrainGroupDates)
                .HasForeignKey(x => x.TrainGroupId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent group is deleted

            // Relationship with Participants (one-to-many)
            //builder.HasMany(x => x.TrainGroupDateParticipants)
            //    .WithOne(x => x.TrainGroupDate)
            //    .HasForeignKey(x => x.TrainGroupDateId)
            //    .IsRequired()
            //    .OnDelete(DeleteBehavior.Cascade); // Delete participants if date is deleted

            // Relationship with CancellationSubscribers (one-to-many)
            //builder
            //    .HasMany(x => x.TrainGroupDateCancellationSubscribers)
            //    .WithOne(x => x.TrainGroupDate)
            //    .HasForeignKey(x => x.TrainGroupDateId)
            //    .IsRequired()
            //    .OnDelete(DeleteBehavior.Cascade); // Delete subscribers if date is deleted
        }
    }
}
