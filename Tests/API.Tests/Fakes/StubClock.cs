namespace API.Tests.Fakes
{
    /// <summary>
    /// The app's clock: the real one, until a test says what time it is.
    /// </summary>
    public sealed class StubClock : TimeProvider
    {
        private static readonly TimeZoneInfo Athens = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

        private DateTimeOffset? _now;

        public void Reset()
        {
            _now = null;
        }

        public void Set(DateTimeOffset now)
        {
            _now = now;
        }

        /// <summary>
        /// By the clock on the wall of the gym, which is in Athens.
        /// </summary>
        public void SetAtTheGym(DateTime now)
        {
            DateTime wall = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);

            _now = new DateTimeOffset(wall, Athens.GetUtcOffset(wall));
        }

        public override DateTimeOffset GetUtcNow()
        {
            return (_now ?? base.GetUtcNow()).ToUniversalTime();
        }
    }
}
