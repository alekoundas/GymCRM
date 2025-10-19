using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasKey(x => x.Id);

            builder.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
              .IsRequired();


            // Relationship with UserStatus (zero-to-many)
            builder.HasOne(x => x.UserStatus)
              .WithMany(x => x.Users)
              .HasForeignKey(x => x.UserStatusId)
              .IsRequired(false)
              .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        }
    }
}
