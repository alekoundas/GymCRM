using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class ExerciseHistoryConfiguration : IEntityTypeConfiguration<ExerciseHistory>
    {
        public void Configure(EntityTypeBuilder<ExerciseHistory> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            // Properties
            builder.Property(x => x.Name)
               .IsRequired(true)
               .HasMaxLength(500);

            builder.Property(x => x.Description)
               .IsRequired(false)
               .HasMaxLength(500);

            builder.Property(x => x.Sets)
                .IsRequired(true)
                .HasMaxLength(500);

            builder.Property(x => x.Reps)
                .IsRequired(true)
                .HasMaxLength(500);

            builder.Property(x => x.Weight)
                .IsRequired(true)
                .HasMaxLength(500);

            builder.Property(x => x.GroupNumber)
              .IsRequired(true);

            builder.Property(x => x.GroupExerciseOrderNumber)
              .IsRequired(true);


            // Relationship with User (one-to-many)
            builder.HasOne(x => x.Exercise)
                .WithMany(x => x.ExerciseHistories)
                .HasForeignKey(x => x.ExerciseId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed

        }
    }
}
