using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class GoogleRefreshTokenConfiguration : IEntityTypeConfiguration<GoogleRefreshToken>
    {
        public void Configure(EntityTypeBuilder<GoogleRefreshToken> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);
        }
    }
}
