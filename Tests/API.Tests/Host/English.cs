using Core.Translations;

namespace API.Tests.Host
{
    /// <summary>
    /// What a message says in English. The tests name messages by their keys, so a text may be
    /// reworded without a test failing, and the wrong message may not be sent without one.
    /// </summary>
    internal static class English
    {
        private static readonly Dictionary<string, string> Texts = Language.Texts("en");

        public static readonly string NotAuthorized = Of(TranslationKeys.User_is_not_authorized_to_perform_this_action);

        public static readonly string NotSignedIn = Of(TranslationKeys.User_is_not_loged_in);

        public static readonly string Invalid = Of(TranslationKeys.Invalid_data_provided);

        public static string Of(string key)
        {
            return Texts[key];
        }

        public static string Of(string key, string argument)
        {
            return string.Format(Texts[key], Texts.GetValueOrDefault(argument, argument));
        }

        public static string NotFound(string what)
        {
            return Of(TranslationKeys.Requested_0_not_found, what);
        }

        public static string Updated(string what)
        {
            return Of(TranslationKeys._0_updated_successfully, what);
        }

        public static string Deleted(string what)
        {
            return Of(TranslationKeys._0_deleted_successfully, what);
        }

        public static string Required(string what)
        {
            return Of(TranslationKeys._0_is_required, what);
        }
    }
}
