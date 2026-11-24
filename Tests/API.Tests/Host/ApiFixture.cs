using System.Net.Http.Json;
using API.Services;
using API.Tests.Fakes;
using Business.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tests.Shared;

namespace API.Tests.Host
{
    /// <summary>
    /// The app running on a database of its own, for one class of tests. It is the real one but for
    /// four things: mails are kept instead of sent, Google is a stub, the background sender is left
    /// out, which would otherwise send a queued mail under the test that looks for it, and passwords
    /// are hashed quickly.
    /// </summary>
    public sealed class ApiFixture : IAsyncLifetime
    {
        private ApiHost? _host;
        private HttpClient? _client;

        public TestDatabase Database { get; } = new TestDatabase();

        public RecordingEmailService Emails { get; } = new RecordingEmailService();

        public StubGoogle Google { get; } = new StubGoogle();

        public IServiceProvider Services
        {
            get { return _host!.Services; }
        }

        public Uri Address
        {
            get { return _host!.Address; }
        }

        public async ValueTask InitializeAsync()
        {
            _host = await ApiHost.StartAsync(Settings.For(Database), Configure);
            _client = new HttpClient(new SocketsHttpHandler { UseProxy = false, AllowAutoRedirect = false }) { BaseAddress = _host.Address };
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();

            if (_host != null)
                await _host.DisposeAsync();

            Database.Dispose();
        }

        /// <summary>
        /// Nothing left of the test before.
        /// </summary>
        public void Reset()
        {
            Database.Reset();
            Emails.Reset();
            Google.Reset();
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, Caller caller, string? language = null)
        {
            using HttpRequestMessage request = new HttpRequestMessage(method, url);
            caller.Sign(request);

            if (language != null)
                request.Headers.TryAddWithoutValidation("Accept-Language", language);

            if (body is HttpContent content)
                request.Content = content;
            else if (body != null)
                request.Content = JsonContent.Create(body, body.GetType(), options: ApiResult.Json);

            return await _client!.SendAsync(request, TestContext.Current.CancellationToken);
        }

        /// <summary>
        /// Without its log and without the background sender. A real hash of a password takes a tenth
        /// of a second on purpose, and starting makes three: nothing here is about how strong they are.
        /// </summary>
        internal static void Quieten(IServiceCollection services)
        {
            services.RemoveAll<ILoggerProvider>();
            services.Remove(services.Single(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(MailQueueBackgroundService)));
            services.Configure<PasswordHasherOptions>(x => x.IterationCount = 1000);
        }

        private void Configure(IServiceCollection services)
        {
            Quieten(services);

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);

            services.ConfigureHttpClientDefaults(x => x.ConfigurePrimaryHttpMessageHandler(() => Google));
        }
    }
}
