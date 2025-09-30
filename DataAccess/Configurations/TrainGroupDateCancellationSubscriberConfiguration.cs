//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Microsoft.EntityFrameworkCore;
//using Core.Models;

//namespace DataAccess.Configurations
//{
//    public class TrainGroupDateCancellationSubscriberConfiguration : IEntityTypeConfiguration<TrainGroupDateCancellationSubscriber>
//    {
//        public void Configure(EntityTypeBuilder<TrainGroupDateCancellationSubscriber> builder)
//        {
//            builder.HasIndex(x => x.Id).IsUnique();
//            builder.HasKey(x => x.Id);

//            // Properties
//            builder.Property(x => x.SelectedDate)
//                .IsRequired();

//            // Relationship with TrainGroupDate (one-to-many)
//            builder.HasOne(x => x.TrainGroupDate)
//                .WithMany(x => x.TrainGroupDateCancellationSubscribers)
//                .HasForeignKey(x => x.TrainGroupDateId)
//                .IsRequired()
//                .OnDelete(DeleteBehavior.Cascade); // Delete if parent TrainGroupDate is deleted

//            // Relationship with User (one-to-many)
//            builder.HasOne(x => x.User)
//                .WithMany(x=>x.TrainGroupDatesCancellationSubscriber)
//                .HasForeignKey(x => x.UserId)
//                .IsRequired()
//                .OnDelete(DeleteBehavior.Cascade); // Delete if user is deleted.
//        }
//    }
//}
