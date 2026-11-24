using Microsoft.Extensions.Localization;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// Gives each text back as its key in brackets, so a mail shows which texts it is made of.
    /// </summary>
    internal sealed class FakeLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name]
        {
            get { return new LocalizedString(name, $"[{name}]"); }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get { return new LocalizedString(name, $"[{name}: {string.Join(", ", arguments)}]"); }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotSupportedException();
        }
    }
}
