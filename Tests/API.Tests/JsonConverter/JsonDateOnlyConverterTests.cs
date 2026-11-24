using System.Text.Json;
using API.JsonConverter;
using Tests.Shared;

namespace API.Tests.JsonConverter
{
    /// <summary>
    /// A date with no time. The app does not use it: nothing it sends is one.
    /// </summary>
    public sealed class JsonDateOnlyConverterTests
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { Converters = { new JsonDateOnlyConverter() } };

        [Theory]
        [InlineData(2031, 3, 5, "2031-03-05")]
        [InlineData(1, 1, 1, "0001-01-01")]
        [InlineData(9999, 12, 31, "9999-12-31")]
        public void A_date_goes_out_and_comes_in_the_same(int year, int month, int day, string text)
        {
            DateOnly date = new DateOnly(year, month, day);

            JsonSerializer.Serialize(date, Options).ShouldBe($"\"{text}\"");
            JsonSerializer.Deserialize<DateOnly>($"\"{text}\"", Options).ShouldBe(date);
        }

        [Fact]
        public void In_every_language_it_is_the_same()
        {
            foreach (string language in JsonDateTimeConverterTests.Languages)
            {
                using Culture _ = Culture.Use(language);

                JsonSerializer.Serialize(new DateOnly(2031, 3, 5), Options).ShouldBe("\"2031-03-05\"", language);
                JsonSerializer.Deserialize<DateOnly>("\"2031-03-05\"", Options).ShouldBe(new DateOnly(2031, 3, 5), language);
            }
        }

        [Theory]
        [InlineData("en", 3, 5)]
        [InlineData("el", 5, 3)]
        public void A_date_written_for_people_depends_on_the_language(string language, int month, int day)
        {
            using Culture _ = Culture.Use(language);

            JsonSerializer.Deserialize<DateOnly>("\"03/05/2031\"", Options).ShouldBe(new DateOnly(2031, month, day));
        }

        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"x\"")]
        [InlineData("\"2031-02-30\"")]
        // One with a time is none.
        [InlineData("\"2031-03-05T10:00:00Z\"")]
        public void Something_that_is_no_date_is_refused(string json)
        {
            Should.Throw<FormatException>(() => JsonSerializer.Deserialize<DateOnly>(json, Options));
        }

        [Theory]
        [InlineData("5")]
        [InlineData("{}")]
        public void Something_that_is_no_text_is_refused(string json)
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<DateOnly>(json, Options));
        }

        [Fact]
        public void There_is_no_word_for_a_null()
        {
            Should.Throw<ArgumentNullException>(() => JsonSerializer.Deserialize<DateOnly>("null", Options));
            JsonSerializer.Deserialize<DateOnly?>("null", Options).ShouldBeNull();
        }
    }
}
