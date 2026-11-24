using System.Globalization;

namespace Tests.Shared
{
    /// <summary>
    /// Another language until disposed, for the code this test calls and nothing beside it.
    /// </summary>
    internal sealed class Culture : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        private Culture(string name)
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
        }

        public static Culture Use(string name)
        {
            return new Culture(name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previous;
        }
    }
}
