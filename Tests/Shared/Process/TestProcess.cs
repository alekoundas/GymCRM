using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;

namespace Tests.Shared
{
    /// <summary>
    /// What every test process starts as, whatever machine it runs on:
    /// 1) UTC, as the server is. The api turns stored times with the machine's own zone, so on a
    ///    laptop in Athens the same test would otherwise read two hours apart from the server.
    /// 2) English, the language a request without one gets.
    /// 3) No network. Every address but this machine's own goes to a proxy that is not there.
    /// </summary>
    internal static class TestProcess
    {
        // Nothing listens on the discard port, so a call that tries to leave the machine fails at once.
        private const string DeadProxy = "http://127.0.0.1:9";

        [ModuleInitializer]
        internal static void Start()
        {
            LocalTimeZone.Set(LocalTimeZone.Utc);

            CultureInfo english = new CultureInfo("en");
            CultureInfo.CurrentCulture = english;
            CultureInfo.CurrentUICulture = english;
            CultureInfo.DefaultThreadCurrentCulture = english;
            CultureInfo.DefaultThreadCurrentUICulture = english;

            HttpClient.DefaultProxy = new WebProxy(DeadProxy);
        }
    }
}
