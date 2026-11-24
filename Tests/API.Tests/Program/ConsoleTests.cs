using System.Net;
using API.Tests.Host;
using Core.Dtos;
using Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tests.Shared;

namespace API.Tests.Program
{
    /// <summary>
    /// What the app writes to the console, which is what the server keeps as its log. There is one
    /// console to a process, so none of these runs beside another test.
    /// </summary>
    [Collection(ProcessState.Name)]
    public sealed class ConsoleTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly StringWriter _console = new StringWriter();

        public void Dispose()
        {
            Console.SetOut(TextWriter.Null);
            _database.Dispose();
        }

        private string Written
        {
            get { return _console.ToString(); }
        }

        private void Listen()
        {
            Console.SetOut(TextWriter.Synchronized(_console));
        }

        // A line may be written a moment after the answer has gone out.
        private async Task WaitForAsync(string text)
        {
            for (int i = 0; i < 200 && !Written.Contains(text); i++)
                await Task.Delay(20, TestToken.Ct);
        }

        // Which request and how it ended. The token it came with only on a developer's machine.
        [Theory]
        [InlineData("Production", false)]
        [InlineData("Staging", false)]
        [InlineData("Development", true)]
        public async Task The_headers_are_written_down_on_a_developers_machine_alone(string environment, bool isWritten)
        {
            await using ApiHost host = await ApiHost.StartAsync(Settings.For(_database, environment), ApiFixture.Quieten);
            using HttpClient client = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = host.Address };
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/Subscriptions/Balance");
            Caller.Of(new User { Id = Guid.NewGuid() }).Sign(request);
            request.Headers.TryAddWithoutValidation("Origin", Settings.Issuer);
            Listen();

            (await client.SendAsync(request, TestToken.Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
            await WaitForAsync(isWritten ? "Response Headers: " : "[END]");

            Written.ShouldContain("[START] Request: GET /api/Subscriptions/Balance");
            Written.ShouldContain("Origin: " + Settings.Issuer);
            Written.ShouldContain("[END] Response: 200");

            Written.Contains("Headers: ").ShouldBe(isWritten);
            Written.Contains("Authorization: Bearer ").ShouldBe(isWritten);
            Written.Contains("Response Headers: Content-Type: application/json").ShouldBe(isWritten);
        }

        // Bad forms are turned away by the app's own filter. The answer that is set up for the one that
        // comes with ASP.NET Core is never given, and has nothing in it: the reasons go to the console.
        [Fact]
        public async Task The_answer_that_is_never_given_is_an_empty_one()
        {
            await using ApiHost host = await ApiHost.StartAsync(Settings.For(_database), ApiFixture.Quieten);
            ApiBehaviorOptions options = host.Services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            ActionContext context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            context.ModelState.AddModelError("$.trainerId", "One");
            context.ModelState.AddModelError("Name", "Two");
            Listen();

            ApiResponse<object> response = options.InvalidModelStateResponseFactory(context).ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<ApiResponse<object>>();

            options.SuppressModelStateInvalidFilter.ShouldBeTrue();
            response.IsSucceed.ShouldBeFalse();
            response.Messages["error"].ShouldBeEmpty();
            Written.ShouldContain("ModelState.IsValid: False");
            Written.ShouldContain("Key: $.trainerId, Errors: One");
            Written.ShouldContain("Key: Name, Errors: Two");
        }
    }
}
