using System.Globalization;
using Business.Services;
using Business.Tests.Fakes;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Tests.Shared;

namespace Business.Tests.Services
{
    /// <summary>
    /// A member's sessions: one for every sign-in, each with a token that is replaced as it is used.
    /// </summary>
    public sealed class RefreshTokenServiceTests : IDisposable
    {
        private const string Provider = "REFRESHTOKENPROVIDER";
        private const int MaxSessions = 20;

        private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

        private readonly TestDatabase _database = new TestDatabase();
        private readonly Clock _clock = new Clock();
        private readonly IdentityServices _identity;
        private readonly RefreshTokenService _service;
        private readonly User _maria;
        private readonly User _nikos;

        public RefreshTokenServiceTests()
        {
            _identity = new IdentityServices(_database);
            _service = new RefreshTokenService(_identity.Users, DataServices.On(_database), _clock);

            Seed seed = new Seed(_database);
            _maria = seed.User();
            _nikos = seed.User();
        }

        public void Dispose()
        {
            _identity.Dispose();
            _database.Dispose();
        }

        private List<IdentityUserToken<Guid>> Rows(User user)
        {
            return _database.Read(db => db.UserTokens.Where(x => x.UserId == user.Id).ToList());
        }

        private List<string> Sessions(User user)
        {
            return Rows(user).Where(x => x.LoginProvider == Provider).Select(x => x.Name).ToList();
        }

        private string Value(string token)
        {
            return _database.Read(db => db.UserTokens.Single(x => x.Name == Session(token)).Value).ShouldNotBeNull();
        }

        private static string Session(string token)
        {
            return token.Split('.')[0];
        }

        private static string Number(string? token)
        {
            return token.ShouldNotBeNull().Split('.')[1];
        }

        private static string Signed(string token)
        {
            return token.Split('.')[2];
        }

        // The token that follows, a minute later.
        private async Task<string> NextAsync(User user, string token)
        {
            _clock.Pass(Minute);

            return (await _service.RenewAsync(user, token)).ShouldNotBeNull();
        }

        private IdentityUserToken<Guid> Keep(User user, string provider, string name, string value)
        {
            return _database.Add(new IdentityUserToken<Guid> { UserId = user.Id, LoginProvider = provider, Name = name, Value = value });
        }


        // ================================================================
        // Signing in
        // ================================================================

        [Fact]
        public async Task A_sign_in_begins_a_session_with_its_first_token()
        {
            string token = await _service.StartAsync(_maria);

            Number(token).ShouldBe("1");
            Guid.TryParseExact(Session(token), "N", out _).ShouldBeTrue();

            IdentityUserToken<Guid> row = Rows(_maria).Single();
            row.LoginProvider.ShouldBe(Provider);
            row.Name.ShouldBe(Session(token));
            row.Value.ShouldBe($"1 {_clock.Now.Ticks}");
        }

        [Fact]
        public async Task What_is_kept_is_no_token()
        {
            string token = await _service.StartAsync(_maria);

            IdentityUserToken<Guid> row = Rows(_maria).Single();
            (row.Name + row.Value).ShouldNotContain(Signed(token));
            (await _service.RenewAsync(_maria, $"{row.Name}.1.{row.Value}")).ShouldBeNull();
        }

        [Fact]
        public async Task Every_sign_in_begins_a_session_of_its_own()
        {
            string phone = await _service.StartAsync(_maria);
            string laptop = await _service.StartAsync(_maria);
            string other = await _service.StartAsync(_nikos);

            Sessions(_maria).ShouldBe([Session(phone), Session(laptop)], ignoreOrder: true);
            Sessions(_nikos).ShouldBe([Session(other)]);

            (await NextAsync(_maria, phone)).ShouldStartWith(Session(phone) + ".2.");
            (await NextAsync(_maria, laptop)).ShouldStartWith(Session(laptop) + ".2.");
            (await NextAsync(_nikos, other)).ShouldStartWith(Session(other) + ".2.");
        }

        [Fact]
        public async Task Twenty_sessions_fit_and_the_next_ends_the_oldest()
        {
            List<string> tokens = [];
            for (int i = 0; i < MaxSessions; i++)
            {
                tokens.Add(await _service.StartAsync(_maria));
                _clock.Pass(TimeSpan.FromSeconds(1));
            }

            Sessions(_maria).ShouldBe(tokens.Select(Session), ignoreOrder: true);

            tokens.Add(await _service.StartAsync(_maria));

            Sessions(_maria).ShouldBe(tokens.Skip(1).Select(Session), ignoreOrder: true);
            (await _service.RenewAsync(_maria, tokens[0])).ShouldBeNull();
            (await _service.RenewAsync(_maria, tokens[1])).ShouldNotBeNull();
        }

        [Fact]
        public async Task The_session_that_makes_way_is_the_one_used_longest_ago()
        {
            List<string> tokens = [];
            for (int i = 0; i < MaxSessions; i++)
            {
                tokens.Add(await _service.StartAsync(_maria));
                _clock.Pass(TimeSpan.FromSeconds(1));
            }
            await NextAsync(_maria, tokens[0]);

            await _service.StartAsync(_maria);

            Sessions(_maria).ShouldContain(Session(tokens[0]));
            Sessions(_maria).ShouldNotContain(Session(tokens[1]));
            Sessions(_maria).Count.ShouldBe(MaxSessions);
        }

        // Older and newer ones of somebody else, and what is kept for something else.
        [Fact]
        public async Task Only_the_members_own_sessions_count_and_make_way()
        {
            List<string> others = [await _service.StartAsync(_nikos)];
            Keep(_maria, "Elsewhere", "Something", "not a session");

            List<string> tokens = [];
            for (int i = 0; i < MaxSessions; i++)
            {
                _clock.Pass(TimeSpan.FromSeconds(1));
                tokens.Add(await _service.StartAsync(_maria));
            }

            _clock.Pass(TimeSpan.FromSeconds(1));
            others.Add(await _service.StartAsync(_nikos));
            tokens.Add(await _service.StartAsync(_maria));

            Sessions(_maria).ShouldBe(tokens.Skip(1).Select(Session), ignoreOrder: true);
            Sessions(_nikos).ShouldBe(others.Select(Session), ignoreOrder: true);
            Rows(_maria).Single(x => x.LoginProvider == "Elsewhere").Value.ShouldBe("not a session");
        }


        // ================================================================
        // Going on
        // ================================================================

        // Three tabs that open together ask three times over.
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(59_999)]
        public async Task Within_a_minute_a_session_stays_where_it_is(int milliseconds)
        {
            string token = await _service.StartAsync(_maria);
            string before = Value(token);
            _clock.Pass(TimeSpan.FromMilliseconds(milliseconds));

            string again = (await _service.RenewAsync(_maria, token)).ShouldNotBeNull();

            again.ShouldNotBe(token);
            Session(again).ShouldBe(Session(token));
            Number(again).ShouldBe("1");
            Value(token).ShouldBe(before);
        }

        [Theory]
        [InlineData(60_000)]
        [InlineData(60_001)]
        [InlineData(1000 * 60 * 60 * 24 * 6)]
        public async Task From_a_minute_on_it_moves_to_its_next_token(int milliseconds)
        {
            string token = await _service.StartAsync(_maria);
            _clock.Pass(TimeSpan.FromMilliseconds(milliseconds));

            string next = (await _service.RenewAsync(_maria, token)).ShouldNotBeNull();

            Session(next).ShouldBe(Session(token));
            Number(next).ShouldBe("2");
            Value(token).ShouldBe($"2 {_clock.Now.Ticks}");
            Rows(_maria).Count.ShouldBe(1);
        }

        // The minute counts from the last time it moved, not from the sign-in.
        [Fact]
        public async Task Every_token_has_a_minute_of_its_own()
        {
            string second = await NextAsync(_maria, await _service.StartAsync(_maria));

            _clock.Pass(Minute - TimeSpan.FromMilliseconds(1));
            Number(await _service.RenewAsync(_maria, second)).ShouldBe("2");

            _clock.Pass(TimeSpan.FromMilliseconds(1));
            Number(await _service.RenewAsync(_maria, second)).ShouldBe("3");
        }

        [Fact]
        public async Task A_clock_that_was_put_back_moves_nothing()
        {
            string token = await _service.StartAsync(_maria);
            _clock.Pass(TimeSpan.FromHours(-1));

            Number(await _service.RenewAsync(_maria, token)).ShouldBe("1");
        }

        [Fact]
        public async Task A_session_goes_on_for_as_long_as_it_is_used()
        {
            string token = await _service.StartAsync(_maria);

            for (int i = 2; i <= 50; i++)
            {
                token = await NextAsync(_maria, token);
                Number(token).ShouldBe(i.ToString(CultureInfo.InvariantCulture));
            }

            Rows(_maria).Single().Value.ShouldBe($"50 {_clock.Now.Ticks}");
        }

        // Its answer was lost on the way, or another tab asked at the same moment.
        [Theory]
        [InlineData(0)]
        [InlineData(60 * 60 * 24)]
        public async Task The_token_before_the_latest_is_given_the_latest(int seconds)
        {
            string first = await _service.StartAsync(_maria);
            string second = await NextAsync(_maria, first);
            string before = Value(first);
            _clock.Pass(TimeSpan.FromSeconds(seconds));

            string again = (await _service.RenewAsync(_maria, first)).ShouldNotBeNull();

            Number(again).ShouldBe("2");
            Value(first).ShouldBe(before);

            // They are both the latest now.
            Number(await NextAsync(_maria, again)).ShouldBe("3");
            Number(await _service.RenewAsync(_maria, second)).ShouldBe("3");
        }


        // ================================================================
        // A copy of it
        // ================================================================

        [Fact]
        public async Task A_token_two_behind_ends_the_session()
        {
            string first = await _service.StartAsync(_maria);
            string second = await NextAsync(_maria, first);
            string third = await NextAsync(_maria, second);

            (await _service.RenewAsync(_maria, first)).ShouldBeNull();

            Rows(_maria).ShouldBeEmpty();
            (await _service.RenewAsync(_maria, third)).ShouldBeNull();
            (await _service.RenewAsync(_maria, second)).ShouldBeNull();
        }

        [Fact]
        public async Task And_no_other()
        {
            string copied = await _service.StartAsync(_maria);
            string laptop = await _service.StartAsync(_maria);
            string other = await _service.StartAsync(_nikos);
            await NextAsync(_maria, await NextAsync(_maria, copied));

            (await _service.RenewAsync(_maria, copied)).ShouldBeNull();

            Sessions(_maria).ShouldBe([Session(laptop)]);
            (await _service.RenewAsync(_maria, laptop)).ShouldNotBeNull();
            (await _service.RenewAsync(_nikos, other)).ShouldNotBeNull();
        }

        // Whoever copied it goes on with it while the member is away.
        [Fact]
        public async Task A_thief_is_out_as_soon_as_the_member_is_back()
        {
            string stolen = await _service.StartAsync(_maria);
            string theirs = stolen;
            for (int i = 0; i < 10; i++)
                theirs = await NextAsync(_maria, theirs);

            (await _service.RenewAsync(_maria, stolen)).ShouldBeNull();

            (await _service.RenewAsync(_maria, theirs)).ShouldBeNull();
        }

        // The session was put back to an older state, by a copy of the database.
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task A_token_ahead_of_its_session_ends_it(int latest)
        {
            string third = await NextAsync(_maria, await NextAsync(_maria, await _service.StartAsync(_maria)));
            _database.Change(db => db.UserTokens.Single().Value = $"{latest} {_clock.Now.Ticks}");

            (await _service.RenewAsync(_maria, third)).ShouldBeNull();

            Rows(_maria).ShouldBeEmpty();
        }


        // ================================================================
        // What is refused, and ends nothing
        // ================================================================

        private async Task ShouldBeRefusedAsync(User user, string token)
        {
            int before = _database.Read(db => db.UserTokens.Count());

            (await _service.RenewAsync(user, token)).ShouldBeNull();

            _database.Read(db => db.UserTokens.Count()).ShouldBe(before);
        }

        [Fact]
        public async Task Somebody_elses_token()
        {
            string token = await _service.StartAsync(_nikos);
            await _service.StartAsync(_maria);

            await ShouldBeRefusedAsync(_maria, token);

            (await _service.RenewAsync(_nikos, token)).ShouldNotBeNull();
        }

        [Fact]
        public async Task A_token_with_another_number()
        {
            string first = await _service.StartAsync(_maria);
            string second = await NextAsync(_maria, first);

            await ShouldBeRefusedAsync(_maria, $"{Session(first)}.2.{Signed(first)}");
            await ShouldBeRefusedAsync(_maria, $"{Session(second)}.1.{Signed(second)}");
        }

        [Fact]
        public async Task A_token_of_another_session()
        {
            string phone = await _service.StartAsync(_maria);
            string laptop = await _service.StartAsync(_maria);

            await ShouldBeRefusedAsync(_maria, $"{Session(laptop)}.1.{Signed(phone)}");
        }

        [Fact]
        public async Task A_token_that_was_changed()
        {
            string token = await _service.StartAsync(_maria);
            char last = token[^1];

            await ShouldBeRefusedAsync(_maria, token[..^1] + (last == 'A' ? 'B' : 'A'));
            await ShouldBeRefusedAsync(_maria, token + ".x");
            await ShouldBeRefusedAsync(_maria, Signed(token));
            await ShouldBeRefusedAsync(_maria, $"{Session(token)}.1.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("wrong")]
        [InlineData("..")]
        [InlineData("a.1")]
        [InlineData("a.b.c")]
        [InlineData("a..c")]
        [InlineData("a.1.c")]
        [InlineData("a.-1.c")]
        [InlineData("a.+1.c")]
        [InlineData("a. 1.c")]
        [InlineData("a.1,0.c")]
        [InlineData("a.99999999999.c")]
        [InlineData("a.1.c.d")]
        public async Task Something_that_is_no_token(string token)
        {
            await _service.StartAsync(_maria);

            await ShouldBeRefusedAsync(_maria, token);
        }

        [Fact]
        public async Task A_token_of_a_session_that_is_over()
        {
            string token = await _service.StartAsync(_maria);
            await _service.EndAllAsync(_maria);

            await ShouldBeRefusedAsync(_maria, token);
        }

        [Fact]
        public async Task A_token_that_has_run_out()
        {
            using IdentityServices shortLived = new IdentityServices(_database, refreshTokenSeconds: -1);
            RefreshTokenService service = new RefreshTokenService(shortLived.Users, DataServices.On(_database), _clock);
            string token = await service.StartAsync(_maria);

            (await service.RenewAsync(_maria, token)).ShouldBeNull();

            Sessions(_maria).ShouldBe([Session(token)]);
        }

        // Another password, or signed out.
        [Fact]
        public async Task A_token_from_before_the_account_was_stamped_anew()
        {
            string token = await _service.StartAsync(_maria);
            _maria.SecurityStamp = "another";

            await ShouldBeRefusedAsync(_maria, token);
        }


        // ================================================================
        // At the same moment
        // ================================================================

        private RefreshTokenService Interrupted(Meanwhile meanwhile)
        {
            return new RefreshTokenService(_identity.Users, DataServices.On(_database, meanwhile), _clock);
        }

        [Fact]
        public async Task Whoever_comes_second_is_given_what_the_first_got()
        {
            string token = await _service.StartAsync(_maria);
            string? first = null;
            Meanwhile meanwhile = new Meanwhile(_database, async () => first = await _service.RenewAsync(_maria, token));
            _clock.Pass(Minute);

            string? second = await Interrupted(meanwhile).RenewAsync(_maria, token);

            meanwhile.HasHappened.ShouldBeTrue();
            Number(first).ShouldBe("2");
            Number(second).ShouldBe("2");
            Value(token).ShouldBe($"2 {_clock.Now.Ticks}");
        }

        // The session moved on twice between this one's read and its write.
        [Fact]
        public async Task Whoever_comes_late_does_not_put_the_session_back()
        {
            string token = await _service.StartAsync(_maria);
            Meanwhile meanwhile = new Meanwhile(_database, async () => await NextAsync(_maria, await NextAsync(_maria, token)));
            _clock.Pass(Minute);

            (await Interrupted(meanwhile).RenewAsync(_maria, token)).ShouldBeNull();

            meanwhile.HasHappened.ShouldBeTrue();
            Rows(_maria).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_session_that_is_ended_in_between_is_over()
        {
            string token = await _service.StartAsync(_maria);
            Meanwhile meanwhile = new Meanwhile(_database, () => _service.EndAllAsync(_maria));
            _clock.Pass(Minute);

            (await Interrupted(meanwhile).RenewAsync(_maria, token)).ShouldBeNull();

            meanwhile.HasHappened.ShouldBeTrue();
            Rows(_maria).ShouldBeEmpty();
        }

        [Fact]
        public async Task Ten_at_once_move_it_on_once()
        {
            string token = await _service.StartAsync(_maria);
            _clock.Pass(Minute);

            string?[] renewed = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => _service.RenewAsync(_maria, token))));

            renewed.Select(Number).ShouldAllBe(x => x == "2");
            Value(token).ShouldBe($"2 {_clock.Now.Ticks}");
        }


        // ================================================================
        // Signing out
        // ================================================================

        [Fact]
        public async Task Every_session_of_the_member_ends_and_nobody_elses()
        {
            await _service.StartAsync(_maria);
            await _service.StartAsync(_maria);
            string other = await _service.StartAsync(_nikos);
            Keep(_maria, Provider, "RefreshToken", "from before");
            Keep(_maria, "Elsewhere", "RefreshToken", "not a session");

            await _service.EndAllAsync(_maria);

            Rows(_maria).Select(x => x.LoginProvider).ShouldBe(["Elsewhere"]);
            Sessions(_nikos).ShouldBe([Session(other)]);
        }

        [Fact]
        public async Task A_member_with_none_is_no_trouble()
        {
            await _service.EndAllAsync(_maria);

            Rows(_maria).ShouldBeEmpty();
        }


        // ================================================================
        // The tokens from before there were sessions
        // ================================================================

        private Task<string> FromBeforeAsync(User user)
        {
            return _identity.Users.GenerateUserTokenAsync(user, Provider, "RefreshToken");
        }

        [Fact]
        public async Task The_one_that_was_kept_is_gone_after_a_sign_in()
        {
            Keep(_maria, Provider, "RefreshToken", await FromBeforeAsync(_maria));

            string token = await _service.StartAsync(_maria);

            Sessions(_maria).ShouldBe([Session(token)]);
        }

        // Back again beside the sessions, after the app was put back a version and forward again.
        [Fact]
        public async Task It_is_not_counted_as_a_session()
        {
            List<string> tokens = [];
            for (int i = 0; i < MaxSessions; i++)
                tokens.Add(await NextAsync(_maria, await _service.StartAsync(_maria)));
            Keep(_maria, Provider, "RefreshToken", await FromBeforeAsync(_maria));

            tokens.Add(await _service.StartAsync(_maria));

            Sessions(_maria).ShouldBe(tokens.Skip(1).Select(Session), ignoreOrder: true);
        }

        [Fact]
        public async Task One_of_them_begins_a_session()
        {
            string old = await FromBeforeAsync(_maria);
            Keep(_maria, Provider, "RefreshToken", old);

            string token = (await _service.RenewAsync(_maria, old)).ShouldNotBeNull();

            Number(token).ShouldBe("1");
            Sessions(_maria).ShouldBe([Session(token)]);
            Number(await NextAsync(_maria, token)).ShouldBe("2");
        }

        // There was one for all of a member's devices, and each of them still has its own.
        [Fact]
        public async Task Each_of_them_does()
        {
            string phone = (await _service.RenewAsync(_maria, await FromBeforeAsync(_maria))).ShouldNotBeNull();
            string laptop = (await _service.RenewAsync(_maria, await FromBeforeAsync(_maria))).ShouldNotBeNull();

            Sessions(_maria).ShouldBe([Session(phone), Session(laptop)], ignoreOrder: true);
        }

        [Fact]
        public async Task Somebody_elses_does_not()
        {
            await ShouldBeRefusedAsync(_maria, await FromBeforeAsync(_nikos));
        }

        [Fact]
        public async Task One_that_has_run_out_does_not()
        {
            using IdentityServices shortLived = new IdentityServices(_database, refreshTokenSeconds: -1);
            RefreshTokenService service = new RefreshTokenService(shortLived.Users, DataServices.On(_database), _clock);
            string old = await shortLived.Users.GenerateUserTokenAsync(_maria, Provider, "RefreshToken");

            (await service.RenewAsync(_maria, old)).ShouldBeNull();

            Rows(_maria).ShouldBeEmpty();
        }

        [Fact]
        public async Task One_from_before_the_account_was_stamped_anew_does_not()
        {
            string old = await FromBeforeAsync(_maria);
            _maria.SecurityStamp = "another";

            await ShouldBeRefusedAsync(_maria, old);
        }

        // A link to choose a new password, say.
        [Fact]
        public async Task A_token_for_something_else_does_not()
        {
            await ShouldBeRefusedAsync(_maria, await _identity.Users.GenerateUserTokenAsync(_maria, Provider, "ResetPassword"));
        }


        // ================================================================
        // Languages
        // ================================================================

        [Theory]
        [InlineData("el")]
        [InlineData("ar")]
        [InlineData("fa")]
        [InlineData("sv")]
        [InlineData("th")]
        public async Task A_session_of_one_language_goes_on_in_another(string language)
        {
            string token;
            using (Culture.Use(language))
                token = await NextAsync(_maria, await _service.StartAsync(_maria));

            Value(token).ShouldBe($"2 {_clock.Now.Ticks.ToString(CultureInfo.InvariantCulture)}");
            Number(await NextAsync(_maria, token)).ShouldBe("3");
        }
    }
}
