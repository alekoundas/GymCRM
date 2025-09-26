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
            builder.HasIndex(ur => ur.UserId);
            builder.HasIndex(ur => ur.RoleId);

            // Relationship with User (one-to-many)
            builder.HasOne(x => x.User)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);// Delete if parent User is deleted

            // Relationship with Role (one-to-many)
            builder.HasOne(x => x.Role)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent Role is deleted

        }
    }
}
