using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using API.Tests.Host;
using Core.Dtos.Subscription;
using Core.Models;
using Microsoft.IdentityModel.Tokens;

namespace API.Tests.Program
{
    /// <summary>
    /// Which tokens open the app.
    /// </summary>
    public sealed class TokenTests : ApiTest
    {
        // Anybody signed in may ask for their own.
        private const string Url = "api/Subscriptions/Balance";

        private readonly User _user;
        private readonly Claim[] _claims;

        public TokenTests(ApiFixture api) : base(api)
        {
            _user = Seed.User();
            _claims = [new Claim("Id", _user.Id.ToString())];
        }

        private async Task<HttpStatusCode> StatusAsync(string token)
        {
            using HttpResponseMessage response = await RawAsync(HttpMethod.Get, Caller.WithToken(token), Url);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
                response.Headers.WwwAuthenticate.Single().Scheme.ShouldBe("Bearer");
            }

            return response.StatusCode;
        }

        [Fact]
        public async Task The_apps_own_token_opens_it()
        {
            (await StatusAsync(Caller.Token(_claims))).ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Without_one_it_stays_shut()
        {
            using HttpResponseMessage response = await RawAsync(HttpMethod.Get, Caller.Nobody, Url);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.ShouldHaveSingleItem().ShouldBe(new AuthenticationHeaderValue("Bearer"));
        }

        // To the second: no minutes are given for clocks that differ.
        [Fact]
        public async Task A_token_is_good_until_it_expires()
        {
            (await StatusAsync(Caller.Token(_claims, expires: DateTime.UtcNow.AddSeconds(30)))).ShouldBe(HttpStatusCode.OK);
            (await StatusAsync(Caller.Token(_claims, expires: DateTime.UtcNow.AddSeconds(-2)))).ShouldBe(HttpStatusCode.Unauthorized);
            (await StatusAsync(Caller.Token(_claims, expires: DateTime.UtcNow.AddDays(-1)))).ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task The_answer_says_that_it_has_expired()
        {
            using HttpResponseMessage response = await RawAsync(HttpMethod.Get, Caller.WithToken(Caller.Token(_claims, expires: DateTime.UtcNow.AddDays(-1))), Url);

            response.Headers.WwwAuthenticate.Single().Parameter.ShouldNotBeNull().ShouldContain("invalid_token");
            response.Headers.WwwAuthenticate.Single().Parameter!.ShouldContain("The token expired at");
        }

        [Fact]
        public async Task A_token_that_never_expires_is_no_good()
        {
            (await StatusAsync(Caller.Token(_claims, hasExpiry: false))).ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Each of the sites the settings name, and no other.
        [Theory]
        [InlineData(Settings.Issuer, Settings.Issuer, true)]
        [InlineData(Settings.OtherIssuer, Settings.Issuer, true)]
        [InlineData(Settings.Issuer, Settings.OtherIssuer, true)]
        [InlineData("http://other.test", Settings.Issuer, false)]
        [InlineData(Settings.Issuer, "http://other.test", false)]
        [InlineData("", Settings.Issuer, false)]
        [InlineData(Settings.Issuer, "", false)]
        [InlineData(Settings.Issuer + "/", Settings.Issuer, false)]
        public async Task It_is_from_the_site_and_for_the_site(string issuer, string audience, bool isGood)
        {
            (await StatusAsync(Caller.Token(_claims, issuer, audience))).ShouldBe(isGood ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task It_is_signed_with_the_key()
        {
            (await StatusAsync(Caller.Token(_claims, key: Settings.SecretKey + "x"))).ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Any of the ways there are to sign with that key.
        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256)]
        [InlineData(SecurityAlgorithms.HmacSha384)]
        [InlineData(SecurityAlgorithms.HmacSha512)]
        public async Task It_may_be_signed_in_any_way(string algorithm)
        {
            (await StatusAsync(Caller.Token(_claims, algorithm: algorithm))).ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task One_that_was_changed_is_no_good()
        {
            string token = Caller.Token(_claims);
            string[] parts = token.Split('.');
            string otherPayload = Caller.Token([new Claim("Id", Guid.NewGuid().ToString())]).Split('.')[1];

            (await StatusAsync($"{parts[0]}.{otherPayload}.{parts[2]}")).ShouldBe(HttpStatusCode.Unauthorized);
            // With its signature taken off.
            (await StatusAsync($"{parts[0]}.{parts[1]}.")).ShouldBe(HttpStatusCode.Unauthorized);
            // And the header saying that there need be none.
            string none = Base64UrlEncoder.Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
            (await StatusAsync($"{none}.{parts[1]}.")).ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("x")]
        [InlineData("a.b.c")]
        [InlineData("")]
        public async Task Something_that_is_no_token_is_no_good(string token)
        {
            (await StatusAsync(token)).ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("bearer", HttpStatusCode.OK)]
        [InlineData("BEARER", HttpStatusCode.OK)]
        [InlineData("Basic", HttpStatusCode.Unauthorized)]
        [InlineData("Token", HttpStatusCode.Unauthorized)]
        public async Task It_comes_as_a_bearer_token(string scheme, HttpStatusCode expected)
        {
            using HttpClient client = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = Address };
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Url);
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, Caller.Token(_claims));

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(expected);
        }

        // It is taken at its word until it expires: the account is not looked up.
        [Fact]
        public async Task A_token_outlives_its_account()
        {
            (await GetAsync<int>(Caller.Of(new User { Id = Guid.NewGuid() }), Url)).ShouldSucceed();
        }

        // Under their name in any case, and each letter for letter.
        [Theory]
        [InlineData("Permission", "Subscriptions_View", true)]
        [InlineData("permission", "Subscriptions_View", true)]
        [InlineData("Permission", "subscriptions_view", false)]
        [InlineData("Permission", "Subscriptions_View ", false)]
        [InlineData("Permissions", "Subscriptions_View", false)]
        [InlineData(ClaimTypes.Role, "Subscriptions_View", false)]
        public async Task Its_permissions_are_those_it_was_given(string type, string value, bool isGiven)
        {
            Subscription subscription = Seed.Subscription(_user, 5);

            ApiResult<SubscriptionDto> result = await GetAsync<SubscriptionDto>(
                Caller.WithClaims(new Claim("Id", _user.Id.ToString()), new Claim(type, value)), $"api/Subscriptions/{subscription.Id}");

            if (isGiven)
                result.ShouldSucceed();
            else
                result.ShouldFail(English.NotAuthorized);
        }
    }
}
