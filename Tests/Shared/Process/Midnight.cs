namespace Tests.Shared
{
    /// <summary>
    /// The app reads the date off the clock. A test that works out "today" a moment before it calls
    /// the app would be a day out if midnight fell between the two, so it waits for it to pass.
    /// </summary>
    internal static class Midnight
    {
        private static readonly TimeSpan Margin = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Today in UTC, with at least twenty seconds of it left.
        /// </summary>
        public static DateTime Today()
        {
            TimeSpan left = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            if (left < Margin)
                Thread.Sleep(left + TimeSpan.FromMilliseconds(50));

            return DateTime.UtcNow.Date;
        }
    }
}
