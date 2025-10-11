using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;

namespace Business.Services.Translator
{
    public class TranslatorFactory : IStringLocalizerFactory
    {
        private readonly IDistributedCache _cache;
        private readonly string _translationsPath;
        public TranslatorFactory(IDistributedCache cache, string translationsPath)
        {
            _cache = cache;
            _translationsPath = translationsPath;
        }


        public IStringLocalizer Create(Type resourceSource) => new Translator(_cache, _translationsPath);

        public IStringLocalizer Create(string baseName, string location) => new Translator(_cache, _translationsPath);


        //public IStringLocalizer Create(Type resourceSource) =>
        //    new Translator(_cache, _translationsPath, resourceSource?.Name ?? "Global");

        //public IStringLocalizer Create(string baseName, string location)
        //{
        //    bool isGlobal = baseName == "Global";
        //    return new Translator(_cache, _translationsPath, isGlobal);
        //}
    }
}
