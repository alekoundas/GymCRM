using System.Diagnostics;
using API.Controllers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace API.Tests.Host
{
    /// <summary>
    /// The whole app, started in this process by its own Program.cs on a free port of this machine:
    /// the same services, the same pipeline, the same json. Settings go in as command line
    /// arguments, which win over appsettings.json, so nothing in the app is different for a test.
    ///
    /// Microsoft's test host does the same from a package of its own; this is what it comes down to.
    /// .NET announces every host it builds, which is how the running one is got hold of.
    /// </summary>
    internal sealed class ApiHost : IAsyncDisposable
    {
        private static readonly TimeSpan Patience = TimeSpan.FromMinutes(1);

        // Which start a host being built belongs to. Program.cs runs on the thread that set it.
        private static readonly AsyncLocal<ApiHost?> Current = new AsyncLocal<ApiHost?>();

        private readonly TaskCompletionSource<IHost> _started = new TaskCompletionSource<IHost>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Action<IServiceCollection> _configure;

        static ApiHost()
        {
            // The app writes a few lines for every request.
            Console.SetOut(TextWriter.Null);

            DiagnosticListener.AllListeners.Subscribe(new ListenerObserver());
        }

        private ApiHost(Action<IServiceCollection> configure)
        {
            _configure = configure;
        }

        public IServiceProvider Services { get; private set; } = null!;

        public Uri Address { get; private set; } = null!;

        /// <summary>
        /// Comes back once the app answers requests, or throws what stopped it from starting.
        /// </summary>
        public static async Task<ApiHost> StartAsync(IEnumerable<string> arguments, Action<IServiceCollection> configure)
        {
            ApiHost host = new ApiHost(configure);

            // Program.cs blocks until the app stops, so it gets a thread of its own.
            Thread thread = new Thread(() => host.Run(arguments.ToArray())) { IsBackground = true, Name = "Program.cs" };
            thread.Start();

            IHost running = await host._started.Task.WaitAsync(Patience);

            host.Services = running.Services;
            host.Address = new Uri(running.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());

            return host;
        }

        /// <summary>
        /// Stops the app and waits for Program.cs to come to its end.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

            await _ended.Task.WaitAsync(Patience);
        }

        private void Run(string[] arguments)
        {
            Current.Value = this;

            try
            {
                typeof(AuthController).Assembly.EntryPoint!.Invoke(null, [arguments]);
                _ended.TrySetResult();
            }
            catch (Exception ex)
            {
                _started.TrySetException(ex.InnerException ?? ex);
                _ended.TrySetException(ex.InnerException ?? ex);
            }
        }

        private void OnBuilding(IHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => _configure(services));
        }

        private void OnBuilt(IHost host)
        {
            IHostApplicationLifetime lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            lifetime.ApplicationStarted.Register(() => _started.TrySetResult(host));
        }

        private sealed class ListenerObserver : IObserver<DiagnosticListener>
        {
            public void OnNext(DiagnosticListener listener)
            {
                if (listener.Name == "Microsoft.Extensions.Hosting")
                    listener.Subscribe(new HostObserver());
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }
        }

        private sealed class HostObserver : IObserver<KeyValuePair<string, object?>>
        {
            public void OnNext(KeyValuePair<string, object?> message)
            {
                if (message.Key == "HostBuilding")
                    Current.Value?.OnBuilding((IHostBuilder)message.Value!);

                if (message.Key == "HostBuilt")
                    Current.Value?.OnBuilt((IHost)message.Value!);
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }
        }
    }
}
