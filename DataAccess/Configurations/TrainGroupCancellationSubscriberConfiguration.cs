using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class TrainGroupCancellationSubscriberConfiguration : IEntityTypeConfiguration<TrainGroupCancellationSubscriber>
    {
        public void Configure(EntityTypeBuilder<TrainGroupCancellationSubscriber> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);


            // Properties
            builder.Property(x => x.SelectedDate)
                .IsRequired();

            // Relationship with TrainGroupDate (many-to-one)
            builder.HasOne(x => x.TrainGroupDate)
                .WithMany(x => x.TrainGroupCancellationSubscribers)
                .HasForeignKey(x => x.TrainGroupDateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent date is deleted

            // Relationship with CancellationSubscriber (User) (many-to-one)
            builder.HasOne(x => x.CancellationSubscriber)
                .WithMany(x=>x.TrainGroupCancellationSubscribers)
                .HasForeignKey(x => x.CancellationSubscriberId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        }
    }
}
