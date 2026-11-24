using System.Net;
using System.Text;
using API.Tests.Host;
using Core.Dtos.Subscription;
using Core.Dtos.TrainGroupΑttendance;
using Core.Models;
using Core.Translations;
using Microsoft.Net.Http.Headers;

namespace API.Tests.Program
{
    /// <summary>
    /// What happens to every request before and after its controller.
    /// </summary>
    public sealed class PipelineTests : ApiTest
    {
        // Every language there is a file for.
        public static readonly TheoryData<string> Languages =
            ["ar", "cs", "da", "de", "el", "en", "es", "fi", "fr", "it", "ja", "ko", "nl", "no", "pl", "pt", "ru", "sq", "sv", "tr", "zh"];

        private readonly User _user;

        public PipelineTests(ApiFixture api) : base(api)
        {
            _user = Seed.User();
        }


        // ================================================================
        // The language
        // ================================================================

        // Refused, for the text.
        private async Task<string> RefusalAsync(string? language, string query = "")
        {
            ApiResult<SubscriptionDto> result = await GetAsync<SubscriptionDto>(Caller.Of(_user), "api/Subscriptions/1" + query, language);

            return result.Messages["error"].Single();
        }

        [Theory]
        [MemberData(nameof(Languages))]
        public async Task A_request_is_answered_in_its_language(string language)
        {
            (await RefusalAsync(language)).ShouldBe(Language.Text(language, TranslationKeys.User_is_not_authorized_to_perform_this_action));
        }

        [Theory]
        // None, one the app is not in, and nonsense.
        [InlineData(null, "en")]
        [InlineData("", "en")]
        [InlineData("hu", "en")]
        [InlineData("x-y-z", "en")]
        [InlineData("*", "en")]
        // One of a country is the language of it.
        [InlineData("el-GR", "el")]
        [InlineData("de-AT", "de")]
        [InlineData("pt-BR", "pt")]
        [InlineData("EL", "el")]
        // The one that is wanted most and can be had.
        [InlineData("el, en;q=0.8", "el")]
        [InlineData("de;q=0.5, el;q=0.9", "el")]
        [InlineData("hu, el;q=0.5", "el")]
        // Only the first three are looked at.
        [InlineData("hu, bg, ro, el", "en")]
        public async Task The_language_is_the_one_that_is_asked_for(string? header, string expected)
        {
            (await RefusalAsync(header)).ShouldBe(Language.Text(expected, TranslationKeys.User_is_not_authorized_to_perform_this_action));
        }

        // In the address, and by a cookie, before the header.
        [Fact]
        public async Task The_address_and_a_cookie_may_say_so_too()
        {
            string greek = Language.Text("el", TranslationKeys.User_is_not_authorized_to_perform_this_action);

            (await RefusalAsync("de", "?culture=el")).ShouldBe(greek);
            (await RefusalAsync("de", "?ui-culture=el")).ShouldBe(greek);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(Address, "api/Subscriptions/1"));
            Caller.Of(_user).Sign(request);
            request.Headers.TryAddWithoutValidation("Cookie", ".AspNetCore.Culture=" + Uri.EscapeDataString("c=el|uic=el"));
            (await SendAsync(request)).Body.ShouldContain(greek);
        }

        // Dates are written with the signs of the language, and those of two of them make them
        // unreadable: whoever picks Danish or Finnish gets no date at all.
        [Theory]
        [InlineData("en", "2031-03-05T18:30:15.250Z")]
        [InlineData("el", "2031-03-05T18:30:15.250Z")]
        [InlineData("ar", "2031-03-05T18:30:15.250Z")]
        [InlineData("da", "2031-03-05T18.30.15.250Z")]
        [InlineData("fi", "2031-03-05T18.30.15.250Z")]
        public async Task Dates_are_written_in_the_language_too(string language, string expected)
        {
            Subscription subscription = Seed.Subscription(_user, 5, change: x => x.CreatedOn = new DateTime(2031, 3, 5, 18, 30, 15, 250));

            using HttpResponseMessage response = await RawAsync(HttpMethod.Get, Caller.Of(_user, "Subscriptions_View"), $"api/Subscriptions/{subscription.Id}", null, language);

            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain($"\"createdOn\":\"{expected}\"");
        }


        // ================================================================
        // Other sites
        // ================================================================

        private async Task<(HttpStatusCode Status, string Body, HttpResponseMessage Response)> SendAsync(HttpRequestMessage request)
        {
            using HttpClient client = new HttpClient(new SocketsHttpHandler { UseProxy = false, UseCookies = false, AllowAutoRedirect = false });

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), response);
        }

        private static string? HeaderOf(HttpResponseMessage response, string name)
        {
            return response.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Join(", ", values) : null;
        }

        // A page of one of the sites in the settings may read the answers, with the member's cookies
        // and whatever headers and methods it likes. A page of any other may not.
        [Theory]
        [InlineData(Settings.Issuer, true)]
        [InlineData(Settings.OtherIssuer, true)]
        [InlineData("http://other.test", false)]
        [InlineData("http://gym.test:8080", false)]
        [InlineData("http://sub.gym.test", false)]
        [InlineData("null", false)]
        public async Task Only_pages_of_the_site_read_the_answers(string origin, bool isAllowed)
        {
            using HttpRequestMessage question = new HttpRequestMessage(HttpMethod.Options, new Uri(Address, "api/Subscriptions/Add"));
            question.Headers.TryAddWithoutValidation(HeaderNames.Origin, origin);
            question.Headers.TryAddWithoutValidation(HeaderNames.AccessControlRequestMethod, "DELETE");
            question.Headers.TryAddWithoutValidation(HeaderNames.AccessControlRequestHeaders, "authorization,x-anything");

            (HttpStatusCode status, _, HttpResponseMessage response) = await SendAsync(question);

            status.ShouldBe(HttpStatusCode.NoContent);
            HeaderOf(response, HeaderNames.AccessControlAllowOrigin).ShouldBe(isAllowed ? origin : null);
            HeaderOf(response, HeaderNames.AccessControlAllowCredentials).ShouldBe(isAllowed ? "true" : null);
            HeaderOf(response, HeaderNames.AccessControlAllowMethods).ShouldBe(isAllowed ? "DELETE" : null);
            HeaderOf(response, HeaderNames.AccessControlAllowHeaders).ShouldBe(isAllowed ? "authorization,x-anything" : null);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(Address, "api/Subscriptions/Balance"));
            request.Headers.TryAddWithoutValidation(HeaderNames.Origin, origin);
            Caller.Of(_user).Sign(request);

            (status, _, response) = await SendAsync(request);

            status.ShouldBe(HttpStatusCode.OK);
            HeaderOf(response, HeaderNames.AccessControlAllowOrigin).ShouldBe(isAllowed ? origin : null);
        }

        // Refused, and the page may still read why.
        [Fact]
        public async Task The_site_reads_a_refusal_as_well()
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(Address, "api/Subscriptions/Balance"));
            request.Headers.TryAddWithoutValidation(HeaderNames.Origin, Settings.Issuer);

            (HttpStatusCode status, _, HttpResponseMessage response) = await SendAsync(request);

            status.ShouldBe(HttpStatusCode.Unauthorized);
            HeaderOf(response, HeaderNames.AccessControlAllowOrigin).ShouldBe(Settings.Issuer);
        }


        // ================================================================
        // Addresses there are not
        // ================================================================

        // The pages are not there in a test, or it would be the first of them.
        [Theory]
        [InlineData("")]
        [InlineData("some/page")]
        [InlineData("api/Nothing")]
        [InlineData("file.txt")]
        [InlineData("swagger/index.html")]
        [InlineData("swagger/v1/swagger.json")]
        public async Task An_address_there_is_not_is_not_found(string url)
        {
            using HttpResponseMessage response = await RawAsync(HttpMethod.Get, Caller.Of(_user), url);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task A_method_there_is_not_is_not_allowed()
        {
            using HttpResponseMessage response = await RawAsync(HttpMethod.Patch, Caller.Of(_user), "api/Subscriptions/1");

            response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        }


        // ================================================================
        // Bodies
        // ================================================================

        private static StringContent Json(string body)
        {
            return new StringContent(body, Encoding.UTF8, "application/json");
        }

        private Caller Trainer
        {
            get { return Caller.Of(_user, "TrainGroups_Add"); }
        }

        private Task<ApiResult<List<TrainGroupΑttendance>>> AttendanceAsync(string date)
        {
            return PostAsync<List<TrainGroupΑttendance>>(Trainer, "api/TrainGroupAttendances", Json($"[{{\"userId\":\"{_user.Id}\",\"trainGroupTitle\":\"x\",\"attendanceDate\":{date}}}]"));
        }

        // A date that makes no sense is a bad request. One that is null, or is no text, is not seen
        // coming where there has to be one.
        [Fact]
        public async Task A_date_has_to_be_one()
        {
            (await AttendanceAsync("\"2031-03-05\"")).ShouldSucceed(English.Updated("TrainGroupΑttendance"));
            (await AttendanceAsync("\"x\"")).Status.ShouldBe(HttpStatusCode.BadRequest);
            (await AttendanceAsync("\"\"")).Status.ShouldBe(HttpStatusCode.BadRequest);
            (await AttendanceAsync("null")).ShouldBreak();
            (await AttendanceAsync("5")).Status.ShouldBe(HttpStatusCode.BadRequest);
        }

        // Left out it is the first moment there is, though it is marked as required.
        [Fact]
        public async Task A_date_that_is_left_out_is_year_one()
        {
            (await PostAsync<List<TrainGroupΑttendance>>(Trainer, "api/TrainGroupAttendances", new[] { new { userId = _user.Id, trainGroupTitle = "x" } })).ShouldSucceed(English.Updated("TrainGroupΑttendance"));

            Database.Read(db => db.TrainGroupΑttendances.Single()).AttendanceDate.ShouldBe(DateTime.MinValue);
        }

        // By its name or its number, and back by its name.
        [Theory]
        [InlineData("\"equals\"", true)]
        [InlineData("\"EQUALS\"", true)]
        [InlineData("4", true)]
        [InlineData("\"4\"", true)]
        [InlineData("99", true)]
        [InlineData("\"other\"", false)]
        [InlineData("null", false)]
        public async Task A_kind_is_a_name_or_a_number(string kind, bool isRead)
        {
            ApiResult<object> result = await PostAsync<object>(Caller.Of(_user), "api/Subscriptions/GetDataTable", Json($"{{\"rows\":10,\"filters\":[{{\"fieldName\":\"amount\",\"filterType\":{kind}}}]}}"));

            result.Status.ShouldBe(isRead ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            if (isRead && kind != "99")
                result.Body.ShouldContain("\"filterType\":\"equals\"");
        }

        // In any case, and what the app does not know is passed over.
        [Fact]
        public async Task Names_are_read_in_any_case()
        {
            (await PostAsync<SubscriptionDto>(Caller.Of(_user, "Subscriptions_Add"), "api/Subscriptions/Request", Json("{\"REQUESTEDAMOUNT\":5,\"MemberComment\":\"x\",\"other\":{}}"))).Value.RequestedAmount.ShouldBe(5);
        }

        [Fact]
        public async Task A_number_may_come_as_text()
        {
            (await PostAsync<SubscriptionDto>(Caller.Of(_user, "Subscriptions_Add"), "api/Subscriptions/Request", Json("{\"requestedAmount\":\"5\"}"))).Value.RequestedAmount.ShouldBe(5);
        }

        // What is nothing is not sent.
        [Fact]
        public async Task Values_that_are_nothing_are_left_out()
        {
            Subscription subscription = Seed.Subscription(_user, 5);

            string body = (await GetAsync<SubscriptionDto>(Caller.Of(_user, "Subscriptions_View"), $"api/Subscriptions/{subscription.Id}")).Body;

            body.ShouldNotContain("null");
            body.ShouldNotContain("decidedOn");
            body.ShouldContain("\"status\":\"APPROVED\"");
        }

        [Theory]
        [InlineData("{")]
        [InlineData("[]")]
        [InlineData("\"x\"")]
        [InlineData("{\"requestedAmount\":\"five\"}")]
        [InlineData("{\"requestedAmount\":5.5}")]
        [InlineData("{\"requestedAmount\":99999999999}")]
        public async Task A_body_that_makes_no_sense_is_refused(string body)
        {
            ApiResult<SubscriptionDto> result = await PostAsync<SubscriptionDto>(Caller.Of(_user, "Subscriptions_Add"), "api/Subscriptions/Request", Json(body));

            result.Status.ShouldBe(HttpStatusCode.BadRequest);
            result.IsSucceed.ShouldBeFalse();
            result.Messages["error"].ShouldNotBeEmpty();
        }

        // A status of a member with a status, and so on.
        private Task<ApiResult<object>> AddStatusAsync(int levels)
        {
            string body = "{\"name\":\"x\",\"color\":\"#112233\"}";
            for (int i = 0; i < levels; i++)
                body = "{\"name\":\"x\",\"color\":\"#112233\",\"users\":[{\"userName\":\"x\",\"email\":\"x@gym.test\",\"firstName\":\"x\",\"lastName\":\"x\",\"userStatus\":" + body + "}]}";

            return PostAsync<object>(Caller.Of(_user, "UserStatuses_Add"), "api/UserStatuses", Json("[" + body + "]"));
        }

        [Fact]
        public async Task A_body_may_be_some_levels_deep()
        {
            (await AddStatusAsync(7)).ShouldSucceed(English.Updated("UserStatus"));
        }

        // One deeper than that is not read at all: with no end to it the stack would run out, and the app
        // would be down for everybody.
        [Theory]
        [InlineData(11)]
        [InlineData(5000)]
        public async Task A_body_that_goes_too_deep_breaks_and_the_app_goes_on(int levels)
        {
            (await AddStatusAsync(levels)).ShouldBreak();

            Database.Read(db => db.UserStatuses.Count()).ShouldBe(0);
            (await AddStatusAsync(0)).ShouldSucceed(English.Updated("UserStatus"));
        }

        [Fact]
        public async Task A_body_of_another_kind_is_refused()
        {
            using HttpResponseMessage response = await RawAsync(HttpMethod.Post, Caller.Of(_user, "Subscriptions_Add"), "api/Subscriptions/Request", new StringContent("requestedAmount=5", Encoding.UTF8, "application/x-www-form-urlencoded"));

            response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        }
    }
}
