using System.Text.Json;
using API.JsonConverter;
using Tests.Shared;

namespace API.Tests.JsonConverter
{
    /// <summary>
    /// Every date the app sends or is sent. They all go out as UTC, worked out with the machine's own
    /// zone, and in the language of the request - which is more than a date should depend on.
    /// </summary>
    [Collection(ProcessState.Name)]
    public sealed class JsonDateTimeConverterTests
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { Converters = { new JsonDateTimeConverter() } };

        // The languages the app is in.
        public static readonly string[] Languages = ["ar", "cs", "da", "de", "el", "en", "es", "fi", "fr", "it", "ja", "ko", "nl", "no", "pl", "pt", "ru", "sq", "sv", "tr", "zh"];

        private static string Write(DateTime value)
        {
            return JsonSerializer.Serialize(value, Options).Trim('"');
        }

        private static DateTime Read(string text)
        {
            return JsonSerializer.Deserialize<DateTime>($"\"{text}\"", Options);
        }

        private static TimeZoneInfo Zone(string name)
        {
            return name switch
            {
                "Athens" => LocalTimeZone.Athens,
                "New York" => LocalTimeZone.NewYork,
                _ => LocalTimeZone.Utc
            };
        }


        // ================================================================
        // Going out
        // ================================================================

        // A value from the database is of no kind at all, and is taken for the machine's own time: on
        // a server in UTC it stays what it is.
        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        public void On_the_server_a_date_goes_out_as_it_is(DateTimeKind kind)
        {
            Write(new DateTime(2031, 3, 5, 18, 30, 15, 250, kind)).ShouldBe("2031-03-05T18:30:15.250Z");
        }

        // Anywhere else it moves.
        [Theory]
        [InlineData("Athens", DateTimeKind.Unspecified, "2031-03-05T16:30:15.250Z")]
        [InlineData("Athens", DateTimeKind.Local, "2031-03-05T16:30:15.250Z")]
        [InlineData("Athens", DateTimeKind.Utc, "2031-03-05T18:30:15.250Z")]
        [InlineData("New York", DateTimeKind.Unspecified, "2031-03-05T23:30:15.250Z")]
        [InlineData("New York", DateTimeKind.Local, "2031-03-05T23:30:15.250Z")]
        [InlineData("New York", DateTimeKind.Utc, "2031-03-05T18:30:15.250Z")]
        public void On_another_machine_it_moves_by_its_zone(string zone, DateTimeKind kind, string expected)
        {
            using IDisposable _ = LocalTimeZone.Use(Zone(zone));

            Write(new DateTime(2031, 3, 5, 18, 30, 15, 250, kind)).ShouldBe(expected);
        }

        // Midnight is a date to the pages. In Athens it is the day before, and by another hour in summer.
        [Theory]
        [InlineData(1, "2030-12-31T22:00:00.000Z")]
        [InlineData(7, "2031-06-30T21:00:00.000Z")]
        public void In_Athens_a_day_becomes_the_day_before(int month, string expected)
        {
            using IDisposable _ = LocalTimeZone.Use(LocalTimeZone.Athens);

            Write(new DateTime(2031, month, 1)).ShouldBe(expected);
        }

        // To the thousandth, cut and not rounded.
        [Theory]
        [InlineData(0, "2031-03-05T18:30:15.000Z")]
        [InlineData(9999, "2031-03-05T18:30:15.000Z")]
        [InlineData(10000, "2031-03-05T18:30:15.001Z")]
        [InlineData(9999999, "2031-03-05T18:30:15.999Z")]
        public void It_goes_out_to_the_thousandth(int ticks, string expected)
        {
            Write(new DateTime(2031, 3, 5, 18, 30, 15, DateTimeKind.Utc).AddTicks(ticks)).ShouldBe(expected);
        }

        [Fact]
        public void The_first_and_the_last_moment_there_are()
        {
            Write(DateTime.MinValue).ShouldBe("0001-01-01T00:00:00.000Z");
            Write(DateTime.MaxValue).ShouldBe("9999-12-31T23:59:59.999Z");

            // They cannot move, and stay.
            using (LocalTimeZone.Use(LocalTimeZone.Athens))
                Write(DateTime.MinValue).ShouldBe("0001-01-01T00:00:00.000Z");
            using (LocalTimeZone.Use(LocalTimeZone.NewYork))
                Write(DateTime.MaxValue).ShouldBe("9999-12-31T23:59:59.999Z");
        }

        // It is written with the signs of the language. In Danish and in Finnish the hours are parted
        // by full stops, and no page can read that as a date.
        [Fact]
        public void In_two_languages_it_cannot_be_read()
        {
            DateTime moment = new DateTime(2031, 3, 5, 18, 30, 15, 250, DateTimeKind.Utc);
            List<string> unreadable = new List<string>();

            foreach (string language in Languages)
            {
                using Culture _ = Culture.Use(language);

                string written = Write(moment);
                if (written == "2031-03-05T18:30:15.250Z")
                    continue;

                written.ShouldBe("2031-03-05T18.30.15.250Z", language);
                unreadable.Add(language);
            }

            unreadable.ShouldBe(["da", "fi"]);
        }


        // ================================================================
        // Coming in
        // ================================================================

        [Theory]
        // What the pages send, and what they might.
        [InlineData("2031-03-05T18:30:15.250Z", "2031-03-05 18:30:15.250", DateTimeKind.Utc)]
        [InlineData("2031-03-05T18:30:15Z", "2031-03-05 18:30:15.000", DateTimeKind.Utc)]
        [InlineData("2031-03-05T18:30:15", "2031-03-05 18:30:15.000", DateTimeKind.Unspecified)]
        [InlineData("2031-03-05T18:30", "2031-03-05 18:30:00.000", DateTimeKind.Unspecified)]
        [InlineData("2031-03-05", "2031-03-05 00:00:00.000", DateTimeKind.Unspecified)]
        [InlineData("2031-03-05T18:30:15.1234567Z", "2031-03-05 18:30:15.123", DateTimeKind.Utc)]
        [InlineData("  2031-03-05  ", "2031-03-05 00:00:00.000", DateTimeKind.Unspecified)]
        [InlineData("03/05/2031", "2031-03-05 00:00:00.000", DateTimeKind.Unspecified)]
        [InlineData("5 March 2031", "2031-03-05 00:00:00.000", DateTimeKind.Unspecified)]
        public void A_date_comes_in_as_it_is_written(string text, string expected, DateTimeKind kind)
        {
            DateTime read = Read(text);

            read.ToString("yyyy-MM-dd HH:mm:ss.fff").ShouldBe(expected);
            read.Kind.ShouldBe(kind);
        }

        // One with an offset becomes the machine's own time.
        [Theory]
        [InlineData("UTC", "2031-03-05 16:30")]
        [InlineData("Athens", "2031-03-05 18:30")]
        [InlineData("New York", "2031-03-05 11:30")]
        public void An_offset_is_turned_into_the_machines_time(string zone, string expected)
        {
            using IDisposable _ = LocalTimeZone.Use(Zone(zone));

            DateTime read = Read("2031-03-05T18:30:00+02:00");

            read.ToString("yyyy-MM-dd HH:mm").ShouldBe(expected);
            read.Kind.ShouldBe(DateTimeKind.Local);
        }

        // Anywhere, and in any language.
        [Fact]
        public void What_the_pages_send_is_read_the_same_everywhere()
        {
            foreach (string language in Languages)
                foreach (TimeZoneInfo zone in new[] { LocalTimeZone.Utc, LocalTimeZone.Athens, LocalTimeZone.NewYork })
                {
                    using Culture culture = Culture.Use(language);
                    using IDisposable local = LocalTimeZone.Use(zone);

                    Read("2031-03-05T18:30:15.250Z").ShouldBe(new DateTime(2031, 3, 5, 18, 30, 15, 250, DateTimeKind.Utc), $"{language} {zone.Id}");
                }
        }

        // The day comes first or the month does.
        [Theory]
        [InlineData("en", 3, 5)]
        [InlineData("el", 5, 3)]
        [InlineData("de", 5, 3)]
        public void A_date_written_for_people_depends_on_the_language(string language, int month, int day)
        {
            using Culture _ = Culture.Use(language);

            Read("03/05/2031").ShouldBe(new DateTime(2031, month, day));
        }

        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"x\"")]
        [InlineData("\"2031-02-30\"")]
        [InlineData("\"2031-13-01\"")]
        public void Something_that_is_no_date_is_refused(string json)
        {
            Should.Throw<FormatException>(() => JsonSerializer.Deserialize<DateTime>(json, Options));
        }

        [Theory]
        [InlineData("5")]
        [InlineData("true")]
        [InlineData("{}")]
        [InlineData("[]")]
        public void Something_that_is_no_text_is_refused(string json)
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, Options));
        }

        // Where there must be a date the converter has no word for a null: it throws something nobody
        // up the line knows to be a bad request. Where there need not be, none is none.
        [Fact]
        public void There_is_no_word_for_a_null()
        {
            Should.Throw<ArgumentNullException>(() => JsonSerializer.Deserialize<DateTime>("null", Options));
            JsonSerializer.Deserialize<DateTime?>("null", Options).ShouldBeNull();
            JsonSerializer.Serialize<DateTime?>(null, Options).ShouldBe("null");
            JsonSerializer.Deserialize<DateTime?>("\"2031-03-05\"", Options).ShouldBe(new DateTime(2031, 3, 5));
        }

        // Out and in again it is the same moment.
        [Fact]
        public void A_date_survives_the_round_trip()
        {
            foreach (TimeZoneInfo zone in new[] { LocalTimeZone.Utc, LocalTimeZone.Athens, LocalTimeZone.NewYork })
            {
                using IDisposable _ = LocalTimeZone.Use(zone);

                DateTime moment = new DateTime(2031, 7, 5, 18, 30, 15, 250, DateTimeKind.Utc);
                Read(Write(moment)).ShouldBe(moment);
                Read(Write(moment.ToLocalTime())).ShouldBe(moment);
            }
        }
    }
}
