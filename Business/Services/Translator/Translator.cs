using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;

namespace Business.Services.Translator
{
    public class Translator : IStringLocalizer
    {
        private readonly IDistributedCache _cache;
        private readonly string _cultureName;
        private readonly string _translationsPath;

        public Translator(IDistributedCache cache, string translationsPath)
        {
            _cache = cache;
            _cultureName = CultureInfo.CurrentCulture.Name;
            _translationsPath = translationsPath;
        }

        public LocalizedString this[string name]
        {
            get
            {
                string value = GetString(name);
                LocalizedString aaa = new LocalizedString(name, value ?? name, value == null);
                return aaa;
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                LocalizedString? actualValue = this[name];
                return actualValue.ResourceNotFound
                    ? actualValue
                    : new LocalizedString(name, string.Format(actualValue.Value, arguments), false);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            string? filePath = GetFilePath(_cultureName);
            if (filePath != null)
            {
                Dictionary<string, string>? translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filePath));
                if (translations == null) yield break;
                foreach (var kvp in translations)
                {
                    yield return new LocalizedString(kvp.Key, kvp.Value, false);
                }
            }
        }

        private string GetString(string key)
        {
            string cacheKey = $"locale_{_cultureName}_{key}";
            string? cachedValue = _cache.GetString(cacheKey);
            if (!string.IsNullOrEmpty(cachedValue)) return cachedValue;

            string? filePath = GetFilePath(_cultureName);
            if (filePath != null)
            {
                Dictionary<string, string>? translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filePath));
                if (translations == null) return "";

                translations.TryGetValue(key, out string? value);
                if (value != null) _cache.SetString(cacheKey, value);

                return value ?? key;
            }

            return "";
        }


        private string? GetFilePath(string culture)
        {
            // Release.
            string filePath = Path.Combine(_translationsPath, "Translations", $"{culture}.json");
            if (File.Exists(filePath))
                return filePath;

            // Debug.
            filePath = "/" + Path.Combine("src", "Core", "Translations", $"{culture}.json");
            if (File.Exists(filePath))
                return filePath;

            return null;
        }
    }
}
