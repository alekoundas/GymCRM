using Core.System;

namespace Core.Tests.System
{
    /// <summary>
    /// Both sides of every search go through this, so what it folds together is what a search finds.
    /// </summary>
    public sealed class TextNormalizerTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Nothing_is_an_empty_text(string? text)
        {
            TextNormalizer.Normalize(text).ShouldBe(string.Empty);
        }

        [Theory]
        [InlineData("Maria", "maria")]
        [InlineData("MARIA", "maria")]
        [InlineData("maria papadopoulou", "maria papadopoulou")]
        [InlineData("  Maria  ", "  maria  ")]
        [InlineData("user_01@gym.test", "user_01@gym.test")]
        [InlineData("+30 210-1234567", "+30 210-1234567")]
        [InlineData("100%", "100%")]
        public void Text_without_accents_is_only_lowered(string text, string expected)
        {
            TextNormalizer.Normalize(text).ShouldBe(expected);
        }

        [Theory]
        [InlineData('ά', 'α')]
        [InlineData('έ', 'ε')]
        [InlineData('ή', 'η')]
        [InlineData('ί', 'ι')]
        [InlineData('ό', 'ο')]
        [InlineData('ύ', 'υ')]
        [InlineData('ώ', 'ω')]
        [InlineData('ϊ', 'ι')]
        [InlineData('ϋ', 'υ')]
        [InlineData('ΐ', 'ι')]
        [InlineData('ΰ', 'υ')]
        [InlineData('ς', 'σ')]
        [InlineData('Ά', 'α')]
        [InlineData('Έ', 'ε')]
        [InlineData('Ή', 'η')]
        [InlineData('Ί', 'ι')]
        [InlineData('Ό', 'ο')]
        [InlineData('Ύ', 'υ')]
        [InlineData('Ώ', 'ω')]
        [InlineData('Ϊ', 'ι')]
        [InlineData('Ϋ', 'υ')]
        public void Every_accented_greek_letter_is_its_plain_one(char letter, char expected)
        {
            TextNormalizer.Normalize(letter.ToString()).ShouldBe(expected.ToString());
            TextNormalizer.Normalize("x" + letter + "y").ShouldBe("x" + expected + "y");
        }

        [Fact]
        public void Every_plain_greek_letter_is_only_lowered()
        {
            TextNormalizer.Normalize("ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ").ShouldBe("αβγδεζηθικλμνξοπρστυφχψω");
            TextNormalizer.Normalize("αβγδεζηθικλμνξοπρστυφχψω").ShouldBe("αβγδεζηθικλμνξοπρστυφχψω");
        }

        // The same name as it may be typed.
        [Theory]
        [InlineData("Ελένη")]
        [InlineData("ΕΛΕΝΗ")]
        [InlineData("ελενη")]
        [InlineData("ΕΛΈΝΗ")]
        [InlineData("ελένη")]
        public void One_name_typed_any_way_is_one_text(string name)
        {
            TextNormalizer.Normalize(name).ShouldBe("ελενη");
        }

        [Theory]
        [InlineData("Νίκος", "νικοσ")]
        [InlineData("ΝΙΚΟΣ", "νικοσ")]
        [InlineData("Ταΰγετος", "ταυγετοσ")]
        [InlineData("Καΐρης", "καιρησ")]
        public void The_last_sigma_and_the_double_accents_are_folded_too(string text, string expected)
        {
            TextNormalizer.Normalize(text).ShouldBe(expected);
        }

        [Theory]
        [InlineData("José", "jose")]
        [InlineData("Müller", "muller")]
        [InlineData("Ångström", "angstrom")]
        [InlineData("Čapek", "capek")]
        [InlineData("naïve", "naive")]
        [InlineData("Señor", "senor")]
        public void Latin_accents_are_dropped(string text, string expected)
        {
            TextNormalizer.Normalize(text).ShouldBe(expected);
        }

        // The accent as a character of its own, which is how some keyboards and macOS send it.
        [Theory]
        [InlineData("Ελένη", "ελενη")]
        [InlineData("José", "jose")]
        public void An_accent_sent_as_its_own_character_is_dropped(string text, string expected)
        {
            TextNormalizer.Normalize(text).ShouldBe(expected);
        }

        [Theory]
        [InlineData("日本語")]
        [InlineData("straße")]
        [InlineData("😀")]
        public void Letters_with_nothing_to_drop_stay_as_they_are(string text)
        {
            TextNormalizer.Normalize(text).ShouldBe(text);
        }

        [Theory]
        [InlineData("Ελένη Παπαδοπούλου")]
        [InlineData("José Ångström")]
        [InlineData("Ταΰγετος ΝΙΚΟΣ")]
        public void Doing_it_twice_changes_nothing_more(string text)
        {
            string once = TextNormalizer.Normalize(text);

            TextNormalizer.Normalize(once).ShouldBe(once);
        }
    }
}
