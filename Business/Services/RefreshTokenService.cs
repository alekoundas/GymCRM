using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Business.Services
{
    // Every sign-in begins a session of its own, so the phone's does not end the laptop's.
    // Using the session's latest token moves it on to its next one, and from then on only
    // the latest two are good.
    //
    // Two, because the answer to a renewal may get lost on its way to a phone and two tabs
    // may ask at the same moment: whoever still has the one before is given the latest. A
    // token older than that was followed by one that has been used, so there are two copies
    // of the session about. Nobody can tell which is the member's, and it ends for both.
    //
    // A session is a row in AspNetUserTokens: the number of its latest token and when it was
    // made. The tokens themselves are never written down. They are signed, and say which
    // session they are of and which number they have.
    public class RefreshTokenService : IRefreshTokenService
    {
        private const string Provider = "REFRESHTOKENPROVIDER";

        // What the one token of a member was called before there were sessions.
        private const string BeforeSessions = "RefreshToken";

        // The ones used longest ago make way. More than anybody has devices, and signing out
        // ends them all.
        private const int MaxSessions = 20;

        // A browser that opens again with three tabs asks three times over for everything on
        // them. Without this, a request that was held up for a second would come in with a
        // token that is two behind by then.
        private static readonly TimeSpan MovesOnAfter = TimeSpan.FromMinutes(1);

        private readonly UserManager<User> _userManager;
        private readonly IDataService _dataService;
        private readonly TimeProvider _timeProvider;

        public RefreshTokenService(UserManager<User> userManager, IDataService dataService, TimeProvider timeProvider)
        {
            _userManager = userManager;
            _dataService = dataService;
            _timeProvider = timeProvider;
        }

        public async Task<string> StartAsync(User user)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            List<IdentityUserToken<Guid>> sessions = await context.UserTokens
                .AsNoTracking()
                .Where(x => x.UserId == user.Id && x.LoginProvider == Provider)
                .ToListAsync();

            List<string> ended = sessions
                .Where(x => x.Name != BeforeSessions)
                .OrderByDescending(x => Read(x.Value).MadeOn)
                .Skip(MaxSessions - 1)
                .Select(x => x.Name)
                .Append(BeforeSessions)
                .ToList();

            await context.UserTokens
                .Where(x => x.UserId == user.Id && x.LoginProvider == Provider && ended.Contains(x.Name))
                .ExecuteDeleteAsync();

            string sessionId = Guid.NewGuid().ToString("N");
            context.UserTokens.Add(new IdentityUserToken<Guid>()
            {
                UserId = user.Id,
                LoginProvider = Provider,
                Name = sessionId,
                Value = Write(1)
            });
            await context.SaveChangesAsync();

            return await IssueAsync(user, sessionId, 1);
        }

        public async Task<string?> RenewAsync(User user, string refreshToken)
        {
            string[] parts = refreshToken.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                return await RenewFromBeforeSessionsAsync(user, refreshToken);

            string sessionId = parts[0];
            if (!await _userManager.VerifyUserTokenAsync(user, Provider, Purpose(sessionId, number), parts[2]))
                return null;

            using ApiDbContext context = _dataService.GetDbContext();

            IQueryable<IdentityUserToken<Guid>> session = context.UserTokens
                .Where(x => x.UserId == user.Id && x.LoginProvider == Provider && x.Name == sessionId);

            string? value = await session.Select(x => x.Value).FirstOrDefaultAsync();
            if (value == null)
                return null;

            (int latest, DateTime madeOn) = Read(value);

            if (number != latest && number != latest - 1)
            {
                await session.ExecuteDeleteAsync();
                return null;
            }

            if (number != latest || Now() - madeOn < MovesOnAfter)
                return await IssueAsync(user, sessionId, latest);

            string next = Write(latest + 1);
            int moved = await session
                .Where(x => x.Value == value)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.Value, next));

            // Another renewal was quicker, and this token is the one before the latest by now.
            if (moved == 0)
                return await RenewAsync(user, refreshToken);

            return await IssueAsync(user, sessionId, latest + 1);
        }

        public async Task EndAllAsync(User user)
        {
            using ApiDbContext context = _dataService.GetDbContext();

            await context.UserTokens
                .Where(x => x.UserId == user.Id && x.LoginProvider == Provider)
                .ExecuteDeleteAsync();
        }


        // A token from before there were sessions is good for what is left of its week and
        // begins one, so nobody is signed out by the release that brings this. None are made
        // any more: this can go a week after it.
        private async Task<string?> RenewFromBeforeSessionsAsync(User user, string refreshToken)
        {
            return await _userManager.VerifyUserTokenAsync(user, Provider, BeforeSessions, refreshToken)
                ? await StartAsync(user)
                : null;
        }

        private async Task<string> IssueAsync(User user, string sessionId, int number)
        {
            string signed = await _userManager.GenerateUserTokenAsync(user, Provider, Purpose(sessionId, number));

            return string.Create(CultureInfo.InvariantCulture, $"{sessionId}.{number}.{signed}");
        }

        // Part of what is signed, so a token cannot be passed off as one of another session
        // or with another number.
        private static string Purpose(string sessionId, int number) =>
            string.Create(CultureInfo.InvariantCulture, $"RefreshToken {sessionId} {number}");

        private string Write(int number) =>
            string.Create(CultureInfo.InvariantCulture, $"{number} {Now().Ticks}");

        private static (int Number, DateTime MadeOn) Read(string? value)
        {
            string[] parts = (value ?? "").Split(' ');

            return (
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                new DateTime(long.Parse(parts[1], CultureInfo.InvariantCulture), DateTimeKind.Utc));
        }

        private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
