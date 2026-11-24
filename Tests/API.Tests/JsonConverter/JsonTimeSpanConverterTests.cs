using System.Text.Json;
using API.JsonConverter;
using Tests.Shared;

namespace API.Tests.JsonConverter
{
    /// <summary>
    /// A length of time. The app does not use it: nothing it sends is one.
    /// </summary>
    public sealed class JsonTimeSpanConverterTests
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { Converters = { new JsonTimeSpanConverter() } };

        // Hours, minutes and seconds: the days, the sign and anything under a second are lost.
        [Theory]
        [InlineData("00:00:00", "00:00:00")]
        [InlineData("10:20:30", "10:20:30")]
        [InlineData("23:59:59", "23:59:59")]
        [InlineData("10:20:30.9990000", "10:20:30")]
        [InlineData("1.02:03:04", "02:03:04")]
        [InlineData("-10:20:30", "10:20:30")]
        public void A_length_goes_out_as_hours_minutes_and_seconds(string length, string expected)
        {
            JsonSerializer.Serialize(TimeSpan.Parse(length), Options).ShouldBe($"\"{expected}\"");
        }

        [Theory]
        [InlineData("10:20:30", "10:20:30")]
        [InlineData("10:20", "10:20:00")]
        [InlineData("1.02:03:04", "1.02:03:04")]
        [InlineData("-10:20:30", "-10:20:30")]
        [InlineData("10:20:30.5", "10:20:30.5")]
        // One number alone is days, and so is the first of three when it is no hour.
        [InlineData("5", "5.00:00:00")]
        [InlineData("25:00:00", "25.00:00:00")]
        public void A_length_comes_in_whole(string text, string expected)
        {
            JsonSerializer.Deserialize<TimeSpan>($"\"{text}\"", Options).ShouldBe(TimeSpan.Parse(expected));
        }

        [Fact]
        public void In_every_language_it_is_the_same()
        {
            foreach (string language in JsonDateTimeConverterTests.Languages)
            {
                using Culture _ = Culture.Use(language);

                JsonSerializer.Serialize(new TimeSpan(10, 20, 30), Options).ShouldBe("\"10:20:30\"", language);
                JsonSerializer.Deserialize<TimeSpan>("\"10:20:30\"", Options).ShouldBe(new TimeSpan(10, 20, 30), language);
            }
        }

        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"x\"")]
        public void Something_that_is_no_length_is_refused(string json)
        {
            Should.Throw<FormatException>(() => JsonSerializer.Deserialize<TimeSpan>(json, Options));
        }

        [Fact]
        public void One_with_too_many_minutes_is_refused()
        {
            Should.Throw<OverflowException>(() => JsonSerializer.Deserialize<TimeSpan>("\"10:60\"", Options));
        }

        [Theory]
        [InlineData("5")]
        [InlineData("{}")]
        public void Something_that_is_no_text_is_refused(string json)
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<TimeSpan>(json, Options));
        }

        [Fact]
        public void There_is_no_word_for_a_null()
        {
            Should.Throw<ArgumentNullException>(() => JsonSerializer.Deserialize<TimeSpan>("null", Options));
            JsonSerializer.Deserialize<TimeSpan?>("null", Options).ShouldBeNull();
        }
    }
}
