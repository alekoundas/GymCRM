using System.Globalization;
using System.Text;

namespace Core.System
{
    public static class TextNormalizer
    {
        // Lowercases and drops accents, so searching "ελενη" finds "Ελένη". SQLite
        // cannot do this itself - its lower(), LIKE and NOCASE all fold ASCII only,
        // and leave every Greek letter exactly as it was. So the application hands
        // the database this function instead, registered on each connection and
        // mapped in the model as "normalize".
        //
        // The Greek letters are listed out rather than left to Unicode decomposition
        // because decomposition needs ICU, and an image without it (alpine, or
        // InvariantGlobalization) returns the string unchanged instead of failing.
        // That would quietly half-break search: "ΕΛΕΝΗ" would still match, "Ελένη"
        // would not. Spelling them out keeps the behaviour the same everywhere.
        private static readonly Dictionary<char, char> GreekAccents = new Dictionary<char, char>
        {
            ['ά'] = 'α', // ά -> α
            ['έ'] = 'ε', // έ -> ε
            ['ή'] = 'η', // ή -> η
            ['ί'] = 'ι', // ί -> ι
            ['ό'] = 'ο', // ό -> ο
            ['ύ'] = 'υ', // ύ -> υ
            ['ώ'] = 'ω', // ώ -> ω
            ['ϊ'] = 'ι', // ϊ -> ι
            ['ϋ'] = 'υ', // ϋ -> υ
            ['ΐ'] = 'ι', // ΐ -> ι
            ['ΰ'] = 'υ', // ΰ -> υ
            ['ς'] = 'σ', // ς -> σ, the same letter for searching

            // The capitals as well, in case a runtime without ICU also declines to
            // lowercase them.
            ['Ά'] = 'α', // Ά -> α
            ['Έ'] = 'ε', // Έ -> ε
            ['Ή'] = 'η', // Ή -> η
            ['Ί'] = 'ι', // Ί -> ι
            ['Ό'] = 'ο', // Ό -> ο
            ['Ύ'] = 'υ', // Ύ -> υ
            ['Ώ'] = 'ω', // Ώ -> ω
            ['Ϊ'] = 'ι', // Ϊ -> ι
            ['Ϋ'] = 'υ', // Ϋ -> υ
        };

        public static string Normalize(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            string lowered = value.ToLowerInvariant();

            StringBuilder builder = new StringBuilder(lowered.Length);
            foreach (char character in lowered)
                builder.Append(GreekAccents.TryGetValue(character, out char replacement)
                    ? replacement
                    : character);

            // Latin accents on top, for the runtimes that can decompose. Nothing here
            // depends on it - without ICU this is a no-op and Greek is already done.
            string decomposed = builder.ToString().Normalize(NormalizationForm.FormD);

            StringBuilder result = new StringBuilder(decomposed.Length);
            foreach (char character in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    result.Append(character);

            return result.ToString();
        }
    }
}
