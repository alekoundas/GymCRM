using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class TrainGroupCategoryConfiguration : IEntityTypeConfiguration<TrainGroupCategory>
    {
        public void Configure(EntityTypeBuilder<TrainGroupCategory> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired(true)
                .HasMaxLength(100);
        }
    }
}
