using System.Net;
using System.Text;
using System.Text.Json;
using API.Controllers;
using API.Services;
using API.Tests.Host;
using Business.Services.Email;
using Business.Services.Translator;
using Core.System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Tests.Shared;

namespace API.Tests.Program
{
    /// <summary>
    /// What the app is made of and how it comes up.
    /// </summary>
    public sealed class StartupTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        private Task<ApiHost> StartAsync(string environment = "Production", Action<IServiceCollection>? configure = null)
        {
            return ApiHost.StartAsync(Settings.For(_database, environment), services =>
            {
                ApiFixture.Quieten(services);
                configure?.Invoke(services);
            });
        }

        private static HttpClient Client(ApiHost host)
        {
            return new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = host.Address };
        }


        // ================================================================
        // Its services
        // ================================================================

        // Each service of the app's own: what is used for it and for how long.
        [Fact]
        public async Task The_services_are_the_ones_that_were_approved()
        {
            StringBuilder text = new StringBuilder();

            await using ApiHost host = await ApiHost.StartAsync(Settings.For(_database), services =>
            {
                foreach (ServiceDescriptor service in services.Where(IsOwn).OrderBy(x => x.ServiceType.FullName, StringComparer.Ordinal).ThenBy(x => x.ImplementationType?.FullName, StringComparer.Ordinal))
                    text.AppendLine($"{Name(service.ServiceType)} -> {(service.ImplementationType == null ? "made by hand" : Name(service.ImplementationType))}, {service.Lifetime}");

                ApiFixture.Quieten(services);
            });

            ApprovedFile.ShouldMatch(text.ToString(), "Services");
        }

        private static bool IsOwn(ServiceDescriptor service)
        {
            return new[] { service.ServiceType, service.ImplementationType }.Any(x => x != null && (x.Namespace ?? "").Split('.')[0] is "API" or "Business" or "Core" or "DataAccess"
                && !(x!.Namespace ?? "").StartsWith("API.Tests", StringComparison.Ordinal));
        }

        private static string Name(Type type)
        {
            string name = type.Name.Split('`')[0];

            return type.IsGenericType ? $"{name}<{string.Join(", ", type.GetGenericArguments().Select(x => x.Name))}>" : name;
        }

        // Every controller and every service can be made.
        [Fact]
        public async Task Everything_there_is_can_be_made()
        {
            await using ApiHost host = await StartAsync();
            using IServiceScope scope = host.Services.CreateScope();

            foreach (Type controller in typeof(AuthController).Assembly.GetTypes().Where(x => typeof(ControllerBase).IsAssignableFrom(x) && !x.IsAbstract && !x.IsGenericTypeDefinition))
                ActivatorUtilities.CreateInstance(scope.ServiceProvider, controller).ShouldNotBeNull(controller.Name);

            scope.ServiceProvider.GetRequiredService<IEmailService>().ShouldBeOfType<GmailEmailService>();
            scope.ServiceProvider.GetRequiredService<IStringLocalizer>().ShouldBeOfType<Translator>();
            scope.ServiceProvider.GetRequiredService<IStringLocalizerFactory>().ShouldBeOfType<TranslatorFactory>();
            scope.ServiceProvider.GetRequiredService<TimeProvider>().ShouldBe(TimeProvider.System);
        }


        // ================================================================
        // Its options
        // ================================================================

        [Fact]
        public async Task Accounts_and_passwords_are_held_to_the_rules()
        {
            await using ApiHost host = await StartAsync();
            IdentityOptions options = host.Services.GetRequiredService<IOptions<IdentityOptions>>().Value;

            options.User.RequireUniqueEmail.ShouldBeTrue();
            options.Password.RequiredLength.ShouldBe(8);
            options.Password.RequireNonAlphanumeric.ShouldBeTrue();
            options.Password.RequireUppercase.ShouldBeTrue();
            options.Password.RequireLowercase.ShouldBeTrue();
            options.Password.RequireDigit.ShouldBeTrue();
            options.Lockout.DefaultLockoutTimeSpan.ShouldBe(TimeSpan.FromMinutes(5));
            options.Lockout.MaxFailedAccessAttempts.ShouldBe(5);
            options.Lockout.AllowedForNewUsers.ShouldBeTrue();
            options.Tokens.ProviderMap.Keys.Order().ShouldBe(["Default", "REFRESHTOKENPROVIDER"]);
        }

        // As its mail says, however long a refresh token lives.
        [Fact]
        public async Task A_password_link_lives_for_a_day()
        {
            await using ApiHost host = await StartAsync();
            DataProtectionTokenProviderOptions options = host.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;

            options.TokenLifespan.ShouldBe(TimeSpan.FromHours(24));
            options.Name.ShouldBe("Default");
            host.Services.GetRequiredService<IOptions<IdentityOptions>>().Value.Tokens.PasswordResetTokenProvider.ShouldBe("Default");
        }

        [Fact]
        public async Task A_refresh_token_lives_for_as_long_as_the_settings_say()
        {
            await using ApiHost host = await StartAsync();
            RefreshTokenProviderOptions options = host.Services.GetRequiredService<IOptions<RefreshTokenProviderOptions>>().Value;

            options.TokenLifespan.ShouldBe(TimeSpan.FromSeconds(3600));
            // The name it always had: it is part of how they are signed, and the ones that are out stay good.
            options.Name.ShouldBe("REFRESHTOKENPROVIDER");
        }

        [Fact]
        public async Task Tokens_are_checked_for_everything()
        {
            await using ApiHost host = await StartAsync();
            JwtBearerOptions options = host.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

            options.RequireHttpsMetadata.ShouldBeFalse();
            options.TokenValidationParameters.ValidateIssuer.ShouldBeTrue();
            options.TokenValidationParameters.ValidateAudience.ShouldBeTrue();
            options.TokenValidationParameters.ValidateLifetime.ShouldBeTrue();
            options.TokenValidationParameters.ValidateIssuerSigningKey.ShouldBeTrue();
            options.TokenValidationParameters.RequireExpirationTime.ShouldBeTrue();
            options.TokenValidationParameters.ClockSkew.ShouldBe(TimeSpan.Zero);
            options.TokenValidationParameters.ValidIssuers.ShouldBe([Settings.Issuer, Settings.OtherIssuer]);
            options.TokenValidationParameters.ValidAudiences.ShouldBe([Settings.Issuer, Settings.OtherIssuer]);
        }

        [Fact]
        public async Task The_token_settings_are_the_settings()
        {
            await using ApiHost host = await StartAsync();
            TokenSettings settings = host.Services.GetRequiredService<TokenSettings>();

            settings.Issuer.ShouldBe(Settings.Issuer);
            settings.Issuers.ShouldBe([Settings.Issuer, Settings.OtherIssuer]);
            settings.Audiences.ShouldBe([Settings.Issuer, Settings.OtherIssuer]);
            settings.SecretKey.ShouldBe(Settings.SecretKey);
            settings.TokenExpireSeconds.ShouldBe(600);
            settings.RefreshTokenExpireSeconds.ShouldBe(3600);
        }

        // So that a link or a token is still good after the app was started again.
        [Fact]
        public async Task The_keys_are_kept_in_the_database()
        {
            await using (ApiHost host = await StartAsync())
            {
                string kept = host.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Protect("x");
                kept.ShouldNotBe("x");
            }

            Database(db => db.DataProtectionKeys.Count()).ShouldBe(1);
        }

        private TResult Database<TResult>(Func<DataAccess.ApiDbContext, TResult> read)
        {
            return _database.Read(read);
        }


        // ================================================================
        // Coming up
        // ================================================================

        // From nothing at all.
        [Fact]
        public async Task It_makes_its_database_and_fills_it()
        {
            using TestDatabase database = new TestDatabase(isMigrated: false);

            await using ApiHost host = await ApiHost.StartAsync(Settings.For(database), ApiFixture.Quieten);

            database.Read(db => db.Roles.Select(x => x.Name).ToList()).ShouldBe(["Administrator", "Trainer", "SimpleUser"], ignoreOrder: true);
            database.Read(db => db.Users.Any(x => x.UserName == "Admin")).ShouldBeTrue();
        }

        // It comes up all the same, and then answers nothing.
        [Fact]
        public async Task Without_a_key_every_request_breaks()
        {
            List<string> settings = [.. Settings.For(_database).Where(x => !x.StartsWith("--TokenSettings:SecretKey", StringComparison.Ordinal)), "--TokenSettings:SecretKey="];
            await using ApiHost host = await ApiHost.StartAsync(settings, ApiFixture.Quieten);
            using HttpClient client = Client(host);

            foreach (string url in new[] { "api/Subscriptions/Balance", "api/Auth/Google", "nothing" })
                (await client.GetAsync(url, TestToken.Ct)).StatusCode.ShouldBe(HttpStatusCode.InternalServerError, url);
        }

        // None of them at all.
        [Fact]
        public async Task Without_token_settings_it_does_not_come_up()
        {
            List<string> settings = Settings.For(_database).Where(x => !x.StartsWith("--TokenSettings", StringComparison.Ordinal)).ToList();

            await Should.ThrowAsync<ArgumentNullException>(() => ApiHost.StartAsync([.. settings.Where(x => !x.StartsWith("--contentRoot", StringComparison.Ordinal)), "--contentRoot=" + Path.GetTempPath()], ApiFixture.Quieten));
        }


        // ================================================================
        // On a developer's machine
        // ================================================================

        // Anywhere else it does not tell what it can do.
        [Theory]
        [InlineData("Production", HttpStatusCode.NotFound)]
        [InlineData("Development", HttpStatusCode.OK)]
        public async Task The_api_describes_itself_on_a_developers_machine_alone(string environment, HttpStatusCode expected)
        {
            await using ApiHost host = await StartAsync(environment);
            using HttpClient client = Client(host);

            using HttpResponseMessage description = await client.GetAsync("swagger/v1/swagger.json", TestToken.Ct);
            using HttpResponseMessage page = await client.GetAsync("swagger/index.html", TestToken.Ct);

            description.StatusCode.ShouldBe(expected);
            page.StatusCode.ShouldBe(expected);

            if (expected == HttpStatusCode.NotFound)
                return;

            // With its name, every address, and a box for the token.
            using JsonDocument json = JsonDocument.Parse(await description.Content.ReadAsStringAsync(TestToken.Ct));
            JsonElement root = json.RootElement;
            root.GetProperty("info").GetProperty("title").GetString().ShouldBe("App Api");
            root.GetProperty("info").GetProperty("version").GetString().ShouldBe("v1");
            root.GetProperty("paths").EnumerateObject().Count().ShouldBeGreaterThan(80);
            JsonElement token = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
            token.GetProperty("type").GetString().ShouldBe("http");
            token.GetProperty("scheme").GetString().ShouldBe("bearer");
            token.GetProperty("bearerFormat").GetString().ShouldBe("JWT");
            token.GetProperty("description").GetString().ShouldBe("Please enter token");
            root.GetProperty("security")[0].TryGetProperty("Bearer", out _).ShouldBeTrue();
        }


        // ================================================================
        // The pages
        // ================================================================

        // Built by the client and put beside the app. Every address that is no file and no action of the
        // api's is the first page, which finds out for itself what was asked for.
        [Theory]
        [InlineData("", HttpStatusCode.OK, "first page")]
        [InlineData("index.html", HttpStatusCode.OK, "first page")]
        [InlineData("some/page", HttpStatusCode.OK, "first page")]
        [InlineData("api/nothing", HttpStatusCode.OK, "first page")]
        [InlineData("assets/app.js", HttpStatusCode.OK, "script")]
        [InlineData("assets/nothing.js", HttpStatusCode.NotFound, "")]
        [InlineData("api/Subscriptions/Balance", HttpStatusCode.Unauthorized, "")]
        public async Task Every_other_address_is_the_first_page(string url, HttpStatusCode status, string body)
        {
            DirectoryInfo pages = Directory.CreateTempSubdirectory("gymcrm-pages-");

            try
            {
                pages.CreateSubdirectory("assets");
                await File.WriteAllTextAsync(Path.Combine(pages.FullName, "index.html"), "first page", TestToken.Ct);
                await File.WriteAllTextAsync(Path.Combine(pages.FullName, "assets", "app.js"), "script", TestToken.Ct);

                await using ApiHost host = await ApiHost.StartAsync([.. Settings.For(_database), "--webroot=" + pages.FullName], ApiFixture.Quieten);
                using HttpClient client = Client(host);
                using HttpResponseMessage response = await client.GetAsync(url, TestToken.Ct);

                response.StatusCode.ShouldBe(status);
                (await response.Content.ReadAsStringAsync(TestToken.Ct)).ShouldBe(body);
            }
            finally
            {
                pages.Delete(recursive: true);
            }
        }


        // ================================================================
        // Behind nginx
        // ================================================================

        // It is nginx that the request comes from, and that says who it was and how they came.
        [Fact]
        public async Task It_believes_what_nginx_says_of_a_request()
        {
            LastInLine last = new LastInLine();
            await using ApiHost host = await StartAsync(configure: services => services.AddSingleton<IStartupFilter>(last));
            using HttpClient client = Client(host);
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "nothing.txt");
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.7");
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
            request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "other.test");

            await client.SendAsync(request, TestToken.Ct);

            last.Scheme.ShouldBe("https");
            last.Address.ShouldBe("203.0.113.7");
            // Not under which name.
            last.Host.ShouldBe("127.0.0.1");
        }

        // Comes after everything else, for an address there is not.
        private sealed class LastInLine : IStartupFilter
        {
            public string? Scheme { get; private set; }

            public string? Address { get; private set; }

            public string? Host { get; private set; }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    next(app);

                    app.Run(context =>
                    {
                        Scheme = context.Request.Scheme;
                        Address = context.Connection.RemoteIpAddress?.ToString();
                        Host = context.Request.Host.Host;

                        return Task.CompletedTask;
                    });
                };
            }
        }
    }
}
