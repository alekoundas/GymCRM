using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            // Composite primary key
            //builder.HasKey(ur => new { ur.UserId, ur.RoleId });

            // Indexes (optional for performance)
            //builder.HasIndex(x => x.UserId);
            //builder.HasIndex(x => x.RoleId);

            // Relationship with User (one-to-many)
            builder.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);// Delete if parent User is deleted

            // Relationship with Role (one-to-many)
            builder.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent Role is deleted

        }
    }
}
