using Core.Models;

namespace Business.Services
{
    public interface IRefreshTokenService
    {
        // Begins a session for somebody who has just signed in, and returns its first token.
        Task<string> StartAsync(User user);

        // The session's next token, or null when they have to sign in again.
        Task<string?> RenewAsync(User user, string refreshToken);

        Task EndAllAsync(User user);
    }
}
