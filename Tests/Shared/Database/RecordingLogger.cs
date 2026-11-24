using Microsoft.Extensions.Logging;

namespace Tests.Shared
{
    /// <summary>
    /// Keeps what was logged, one line each.
    /// </summary>
    internal sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = new List<string>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string failed = exception == null ? string.Empty : $" ({exception.GetType().Name})";

            lock (Lines)
                Lines.Add($"{logLevel}: {formatter(state, exception)}{failed}");
        }
    }
}
