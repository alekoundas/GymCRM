namespace Business.Tests.Fakes
{
    /// <summary>
    /// A clock that stands still until it is told to go on.
    /// </summary>
    internal sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        public void Pass(TimeSpan time)
        {
            Now += time;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return Now;
        }
    }
}
