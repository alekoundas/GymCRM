using System.Reflection;

namespace Tests.Shared
{
    /// <summary>
    /// The machine's time zone, as this process sees it. .NET has no way to set it on Windows, so
    /// the zone it has cached is replaced. DateTime.Now alone still asks Windows; the app never
    /// reads it.
    /// </summary>
    internal static class LocalTimeZone
    {
        // A zone of its own rather than TimeZoneInfo.Utc, which .NET treats as a special case. The
        // server's zone is an ordinary one that happens to be at nought.
        public static readonly TimeZoneInfo Utc = TimeZoneInfo.CreateCustomTimeZone("Etc/UTC", TimeSpan.Zero, "UTC", "UTC");

        public static readonly TimeZoneInfo Athens = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

        public static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        public static void Set(TimeZoneInfo zone)
        {
            const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            object cache = typeof(TimeZoneInfo).GetField("s_cachedData", Hidden)!.GetValue(null)!;
            cache.GetType().GetField("_localTimeZone", Hidden)!.SetValue(cache, zone);
        }

        /// <summary>
        /// Another zone until disposed. Only from the ProcessState collection: the whole process changes.
        /// </summary>
        public static IDisposable Use(TimeZoneInfo zone)
        {
            TimeZoneInfo previous = TimeZoneInfo.Local;
            Set(zone);

            return new Restore(previous);
        }

        private sealed class Restore : IDisposable
        {
            private readonly TimeZoneInfo _previous;

            public Restore(TimeZoneInfo previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                Set(_previous);
            }
        }
    }
}
