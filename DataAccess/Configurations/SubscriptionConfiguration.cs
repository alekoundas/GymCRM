using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Models;

namespace DataAccess.Configurations
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.MemberComment)
                .IsRequired(false)
                .HasMaxLength(1000);

            builder.Property(x => x.AdminComment)
                .IsRequired(false)
                .HasMaxLength(1000);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // The balance reads every approved row a member has, and the pending list
            // is looked up by status, so both go through this.
            builder.HasIndex(x => new { x.UserId, x.Status });
        }
    }
}
