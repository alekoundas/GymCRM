using System.Reflection;
using System.Text.RegularExpressions;
using Core.Translations;
using Newtonsoft.Json;

namespace Core.Tests.Translations
{
    /// <summary>
    /// The text of every message, in every language. A key missing from one language shows the
    /// member the key itself; a placeholder missing from one drops the name it was for.
    /// </summary>
    public sealed partial class TranslationFileTests
    {
        private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "Translations");

        // The same list, in the same order, as the languages the api accepts.
        public static readonly TheoryData<string> Languages =
        [
            "ar", "cs", "da", "de", "el", "en", "es", "fi", "fr", "it", "ja", "ko", "nl", "no", "pl", "pt", "ru", "sq", "sv", "tr", "zh"
        ];

        private static Dictionary<string, string> Read(string language)
        {
            string text = File.ReadAllText(Path.Combine(Folder, language + ".json"));

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(text).ShouldNotBeNull();
        }

        private static Dictionary<string, string> Keys()
        {
            return typeof(TranslationKeys)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .ToDictionary(x => x.Name, x => (string)x.GetRawConstantValue()!);
        }

        [GeneratedRegex(@"\{\d+\}")]
        private static partial Regex Placeholder();

        private static List<string> PlaceholdersOf(string text)
        {
            return Placeholder().Matches(text).Select(x => x.Value).Order().ToList();
        }

        [Fact]
        public void There_is_a_file_for_every_language_and_no_other()
        {
            List<string> files = Directory.GetFiles(Folder, "*.json").Select(x => Path.GetFileNameWithoutExtension(x)).Order().ToList();

            files.ShouldBe(Languages.Select(x => x.Data).Order());
        }

        [Theory]
        [MemberData(nameof(Languages))]
        public void Every_language_has_exactly_the_keys_english_has(string language)
        {
            Read(language).Keys.Order().ShouldBe(Read("en").Keys.Order());
        }

        [Theory]
        [MemberData(nameof(Languages))]
        public void No_message_is_empty(string language)
        {
            Read(language).Where(x => string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Key).ShouldBeEmpty();
        }

        [Theory]
        [MemberData(nameof(Languages))]
        public void Every_message_has_the_placeholders_its_english_one_has(string language)
        {
            Dictionary<string, string> english = Read("en");

            List<string> different = Read(language)
                .Where(x => !PlaceholdersOf(x.Value).SequenceEqual(PlaceholdersOf(english[x.Key])))
                .Select(x => x.Key)
                .ToList();

            different.ShouldBeEmpty();
        }

        // A stray brace would make string.Format throw while the member waits for the answer.
        [Theory]
        [MemberData(nameof(Languages))]
        public void Every_message_can_be_formatted(string language)
        {
            foreach (KeyValuePair<string, string> message in Read(language))
                Should.NotThrow(() => string.Format(message.Value, "an argument"), message.Key);
        }

        // {0} is the only placeholder any caller fills.
        [Fact]
        public void No_message_has_a_second_placeholder()
        {
            Read("en").Where(x => PlaceholdersOf(x.Value).Any(y => y != "{0}")).Select(x => x.Key).ShouldBeEmpty();
        }

        [Fact]
        public void Every_key_in_the_code_is_in_the_file_and_the_other_way_round()
        {
            Keys().Values.Order().ShouldBe(Read("en").Keys.Order());
        }

        [Fact]
        public void Every_key_in_the_code_is_spelt_as_it_is_named()
        {
            Keys().Where(x => x.Key != x.Value).ShouldBeEmpty();
        }

        // The label of a placeholder is another key, looked up the same way. These are the names the
        // validation messages are filled with.
        [Theory]
        [InlineData("Title", "Title")]
        [InlineData("StartOn", "Start On")]
        [InlineData("MaxParticipants", "Max Participants")]
        [InlineData("UserName", "User Name")]
        [InlineData("_0_is_required", "{0} is required!")]
        [InlineData("Requested_0_not_found", "Requested {0} not found!")]
        public void English_reads_as_it_did(string key, string text)
        {
            Read("en")[key].ShouldBe(text);
        }

        // Written in both alphabets by mistake once: a Latin P in place of the Greek Ρ.
        [Fact]
        public void Greek_is_written_in_greek_letters()
        {
            Regex mixed = MixedWord();

            List<string> wrong = Read("el")
                .SelectMany(x => x.Value.Split(' ').Select(word => new { x.Key, Word = word }))
                .Where(x => mixed.IsMatch(x.Word))
                .Select(x => $"{x.Key}: {x.Word}")
                .ToList();

            wrong.ShouldBeEmpty();
        }

        // A word with a Greek letter and a Latin one right beside each other.
        [GeneratedRegex(@"[A-Za-z][Ͱ-Ͽ]|[Ͱ-Ͽ][A-Za-z]")]
        private static partial Regex MixedWord();
    }
}
