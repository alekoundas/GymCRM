using Core.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace API.Services
{
    // Refresh tokens with options of their own. There is one set of options for every token
    // provider that is not told otherwise, so what was set for refresh tokens also counted
    // for password links: they were good for a week instead of the 24 hours the mail says.
    public class RefreshTokenProviderOptions : DataProtectionTokenProviderOptions
    {
    }

    public class RefreshTokenProvider : DataProtectorTokenProvider<User>
    {
        public RefreshTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<RefreshTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<User>> logger) : base(dataProtectionProvider, options, logger)
        {
        }
    }
}
