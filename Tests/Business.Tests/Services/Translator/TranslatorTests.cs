using Business.Services.Translator;
using Business.Tests.Fakes;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Tests.Shared;

namespace Business.Tests.Services.Translators
{
    /// <summary>
    /// Finding a text in the file of the language the request is in.
    /// </summary>
    public sealed class TranslatorTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "gymcrm-translations-" + Guid.NewGuid().ToString("N")[..8]);
        private readonly FakeCache _cache = new FakeCache();

        public TranslatorTests()
        {
            Write("en", """{ "Title": "Title", "Saved": "Saved!", "_0_is_required": "{0} is required!", "Pair": "{0} and {1}", "Empty": "" }""");
            Write("el", """{ "Title": "Τίτλος", "Saved": "Αποθηκεύτηκε!", "_0_is_required": "Το πεδίο {0} είναι υποχρεωτικό!" }""");
        }

        public void Dispose()
        {
            Directory.Delete(_folder, recursive: true);
        }

        private void Write(string language, string json)
        {
            Directory.CreateDirectory(Path.Combine(_folder, "Translations"));
            File.WriteAllText(Path.Combine(_folder, "Translations", language + ".json"), json);
        }

        private Translator Translator()
        {
            return new Translator(_cache, _folder);
        }

        private string Text(string key)
        {
            return Translator()[key].Value;
        }

        private string Text(string key, params object[] arguments)
        {
            return Translator()[key, arguments].Value;
        }


        // A text alone.

        [Theory]
        [InlineData("en", "Saved!")]
        [InlineData("el", "Αποθηκεύτηκε!")]
        public void A_text_is_in_the_language_of_the_request(string language, string expected)
        {
            using (Culture.Use(language))
            {
                LocalizedString text = Translator()["Saved"];

                text.Name.ShouldBe("Saved");
                text.Value.ShouldBe(expected);
                text.ResourceNotFound.ShouldBeFalse();
            }
        }

        [Fact]
        public void A_text_that_is_not_there_reads_as_its_key_and_counts_as_found()
        {
            LocalizedString text = Translator()["Missing"];

            text.Value.ShouldBe("Missing");
            text.ResourceNotFound.ShouldBeFalse();
        }

        // It is not taken from English instead.
        [Fact]
        public void A_text_missing_from_one_language_reads_as_its_key()
        {
            using (Culture.Use("el"))
                Text("Pair").ShouldBe("Pair");
        }

        [Fact]
        public void A_language_with_no_file_says_so()
        {
            using (Culture.Use("hu"))
                Text("Saved").ShouldBe("__CANT_FIND_PATH__");
        }

        // The file is for exactly the name the culture has.
        [Fact]
        public void A_region_of_a_language_has_no_file()
        {
            using (Culture.Use("en-GB"))
                Text("Saved").ShouldBe("__CANT_FIND_PATH__");
        }

        [Fact]
        public void An_empty_text_is_empty()
        {
            Text("Empty").ShouldBe("");
            Text("Empty").ShouldBe("");
        }

        [Fact]
        public void A_file_with_nothing_in_it_gives_empty_texts()
        {
            Write("de", "null");

            using (Culture.Use("de"))
                Text("Saved").ShouldBe("");
        }

        [Fact]
        public void A_file_that_is_not_json_throws()
        {
            Write("de", "not json");

            using (Culture.Use("de"))
                Should.Throw<JsonReaderException>(() => Text("Saved"));
        }

        // Whatever language it was made in.
        [Fact]
        public void The_language_is_the_one_at_the_time_of_asking()
        {
            Translator translator = Translator();

            using (Culture.Use("el"))
                translator["Saved"].Value.ShouldBe("Αποθηκεύτηκε!");

            translator["Saved"].Value.ShouldBe("Saved!");
        }


        // The cache.

        [Fact]
        public void A_text_is_read_from_the_file_once()
        {
            Text("Saved").ShouldBe("Saved!");
            Write("en", """{ "Saved": "Changed" }""");

            Text("Saved").ShouldBe("Saved!");
            _cache.Lines.ShouldBe(["get locale_en_Saved", "set locale_en_Saved", "get locale_en_Saved"]);
        }

        [Fact]
        public void Each_language_has_its_own_place_in_the_cache()
        {
            Text("Saved");
            using (Culture.Use("el"))
                Text("Saved");

            _cache.Values.ShouldBe(new Dictionary<string, string> { ["locale_en_Saved"] = "Saved!", ["locale_el_Saved"] = "Αποθηκεύτηκε!" });
        }

        [Fact]
        public void A_cached_text_needs_no_file()
        {
            _cache.Values["locale_hu_Saved"] = "Mentve!";

            using (Culture.Use("hu"))
                Text("Saved").ShouldBe("Mentve!");
        }

        // So a text added to the file while the app runs is found.
        [Theory]
        [InlineData("en", "Missing")]
        [InlineData("hu", "Saved")]
        public void What_was_not_found_is_not_remembered(string language, string key)
        {
            using (Culture.Use(language))
                Text(key);

            _cache.Values.ShouldBeEmpty();
        }

        [Fact]
        public void An_empty_text_is_remembered_and_looked_up_again_all_the_same()
        {
            Text("Empty");
            Write("en", """{ "Empty": "Filled" }""");

            _cache.Values.ShouldContainKey("locale_en_Empty");
            Text("Empty").ShouldBe("Filled");
        }


        // A text with something in it.

        [Theory]
        [InlineData("Anything", "Anything is required!")]
        [InlineData(5, "5 is required!")]
        [InlineData("", " is required!")]
        public void An_argument_is_put_in_its_place(object argument, string expected)
        {
            LocalizedString text = Translator()["_0_is_required", argument];

            text.Name.ShouldBe("_0_is_required");
            text.Value.ShouldBe(expected);
            text.ResourceNotFound.ShouldBeFalse();
        }

        // The name of a field comes as its key, and is put in as the field is called.
        [Fact]
        public void An_argument_that_is_a_key_is_translated_first()
        {
            using (Culture.Use("el"))
                Text("_0_is_required", "Title").ShouldBe("Το πεδίο Τίτλος είναι υποχρεωτικό!");
        }

        [Fact]
        public void Several_arguments_go_in_their_places()
        {
            Text("Pair", "Saved", "x").ShouldBe("Saved! and x");
        }

        [Fact]
        public void An_argument_there_is_no_place_for_is_dropped()
        {
            Text("Saved", "x").ShouldBe("Saved!");
            Text("_0_is_required", "x", "y").ShouldBe("x is required!");
        }

        [Fact]
        public void A_place_there_is_no_argument_for_throws()
        {
            Should.Throw<FormatException>(() => Text("Pair", "x"));
            Should.Throw<FormatException>(() => Text("_0_is_required", Array.Empty<object>()));
        }

        // Asked for with no arguments it is not filled in at all.
        [Fact]
        public void A_text_asked_for_alone_keeps_its_place_open()
        {
            Text("_0_is_required").ShouldBe("{0} is required!");
        }

        [Fact]
        public void A_missing_argument_throws()
        {
            Should.Throw<NullReferenceException>(() => Text("_0_is_required", new object[] { null! }));
        }

        [Fact]
        public void A_text_that_is_not_there_is_filled_in_like_any_other()
        {
            Text("Nothing {0}", "x").ShouldBe("Nothing x");
        }


        // All of them.

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Every_text_of_the_language_can_be_listed(bool includeParentCultures)
        {
            using (Culture.Use("el"))
            {
                List<LocalizedString> texts = Translator().GetAllStrings(includeParentCultures).ToList();

                texts.Select(x => $"{x.Name}={x.Value}").ShouldBe(["Title=Τίτλος", "Saved=Αποθηκεύτηκε!", "_0_is_required=Το πεδίο {0} είναι υποχρεωτικό!"]);
                texts.ShouldAllBe(x => !x.ResourceNotFound);
            }
        }

        [Fact]
        public void A_language_with_no_file_has_no_texts()
        {
            using (Culture.Use("hu"))
                Translator().GetAllStrings(false).ShouldBeEmpty();
        }

        [Fact]
        public void A_file_with_nothing_in_it_has_no_texts()
        {
            Write("de", "null");

            using (Culture.Use("de"))
                Translator().GetAllStrings(false).ShouldBeEmpty();
        }


        // The factory.

        [Fact]
        public void The_factory_makes_translators_of_the_same_files_whatever_it_is_asked_for()
        {
            TranslatorFactory factory = new TranslatorFactory(_cache, _folder);

            factory.Create(typeof(TranslatorTests)).ShouldBeOfType<Translator>()["Saved"].Value.ShouldBe("Saved!");
            factory.Create("", "").ShouldBeOfType<Translator>()["Saved"].Value.ShouldBe("Saved!");
            factory.Create("Global", "Anywhere").ShouldNotBeSameAs(factory.Create("Global", "Anywhere"));
            _cache.Values.ShouldContainKey("locale_en_Saved");
        }
    }
}
