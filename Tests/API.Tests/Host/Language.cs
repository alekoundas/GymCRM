using System.Collections.Concurrent;
using System.Text.Json;

namespace API.Tests.Host
{
    /// <summary>
    /// What the messages say in each of the app's languages, read from the files the app reads.
    /// </summary>
    internal static class Language
    {
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Files = new ConcurrentDictionary<string, Dictionary<string, string>>();

        public static Dictionary<string, string> Texts(string language)
        {
            return Files.GetOrAdd(language, Read);
        }

        public static string Text(string language, string key)
        {
            return Texts(language)[key];
        }

        private static Dictionary<string, string> Read(string language)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Translations", language + ".json")))!;
        }
    }
}
