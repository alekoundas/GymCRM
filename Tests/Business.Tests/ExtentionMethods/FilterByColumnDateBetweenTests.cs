using Tests.Shared;

namespace Business.Tests.ExtentionMethods
{
    public sealed class FilterByColumnDateBetweenTests
    {
        private static readonly DateTime From = new DateTime(2031, 3, 5);
        private static readonly DateTime To = new DateTime(2031, 3, 7);

        // Either side of both edges, by the smallest step there is.
        public static readonly TheoryData<DateTime, bool> Times = new TheoryData<DateTime, bool>
        {
            { From.AddTicks(-1), false },
            { From, true },
            { From.AddTicks(1), true },
            { From.AddDays(1), true },
            { To.AddTicks(-1), true },
            { To, true },
            { To.AddTicks(1), false },
        };

        internal static List<Row> Filter(Row[] rows, string column, object start, object end)
        {
            return rows.AsQueryable().FilterByColumnDateBetween(column, start, end).ToList();
        }

        [Theory]
        [MemberData(nameof(Times))]
        public void Both_edges_are_inside(DateTime on, bool isInside)
        {
            Row row = new Row { On = on, MaybeOn = on, Parent = new Row { On = on } };
            Row[] expected = isInside ? [row] : [];

            Filter([row], "On", From, To).ShouldBe(expected);
            Filter([row], "MaybeOn", From, To).ShouldBe(expected);
            Filter([row], "Parent.On", From, To).ShouldBe(expected);
            Filter([row], "on", From, To).ShouldBe(expected);
        }

        [Theory]
        [MemberData(nameof(Times))]
        public void The_edges_may_be_text(DateTime on, bool isInside)
        {
            Row row = new Row { On = on };
            Row[] expected = isInside ? [row] : [];

            Filter([row], "On", "2031-03-05", "2031-03-07").ShouldBe(expected);
            Filter([row], "On", "2031-03-05T00:00:00", "2031-03-07T00:00:00.000").ShouldBe(expected);
            Filter([row], "On", "2031-03-05T00:00:00.000Z", "2031-03-07T00:00:00.000Z").ShouldBe(expected);
            Filter([row], "On", From, "2031-03-07").ShouldBe(expected);
            Filter([row], "On", "2031-03-05", To).ShouldBe(expected);
        }

        [Fact]
        public void An_empty_date_is_never_inside()
        {
            Filter([new Row { MaybeOn = null }], "MaybeOn", DateTime.MinValue, DateTime.MaxValue).ShouldBeEmpty();
        }

        [Fact]
        public void One_day_to_itself_is_that_moment_alone()
        {
            Row row = new Row { On = From };

            Filter([row, new Row { On = From.AddTicks(1) }], "On", From, From).ShouldBe([row]);
        }

        [Fact]
        public void The_end_before_the_start_throws()
        {
            Should.Throw<ArgumentException>(() => Filter([], "On", To, From)).Message.ShouldBe("Start date must be less than or equal to end date.");
            Should.Throw<ArgumentException>(() => Filter([], "On", From.AddTicks(1), From)).Message.ShouldBe("Start date must be less than or equal to end date.");
        }

        [Theory]
        [InlineData("someday", "2031-03-07")]
        [InlineData("2031-03-05", "someday")]
        [InlineData("", "2031-03-07")]
        [InlineData("2031-03-05", "")]
        [InlineData("2031-13-05", "2031-03-07")]
        [InlineData("null", "2031-03-07")]
        public void Text_that_is_not_a_date_throws_and_says_which(string start, string end)
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => Filter([], "On", start, end));

            exception.Message.ShouldBe($"Cannot convert startValue '{start}' or endValue '{end}' to DateTime.");
            exception.InnerException.ShouldBeOfType<FormatException>();
        }

        [Fact]
        public void Something_that_is_not_a_date_at_all_throws()
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => Filter([], "On", 5, To));

            exception.Message.ShouldStartWith("Cannot convert startValue '5' or endValue '");
            exception.InnerException.ShouldBeOfType<InvalidCastException>();
        }

        [Theory]
        [InlineData("Number", "Int32")]
        [InlineData("MaybeNumber", "Nullable`1")]
        [InlineData("Name", "String")]
        [InlineData("Key", "Guid")]
        [InlineData("IsActive", "Boolean")]
        public void Only_a_date_column_can_be_compared(string column, string type)
        {
            Should.Throw<ArgumentException>(() => Filter([], column, From, To)).Message
                .ShouldBe($"Between filter is only supported for DateTime properties. Property '{column}' is of type '{type}'.");
        }

        [Fact]
        public void A_missing_source_throws()
        {
            Should.Throw<ArgumentNullException>(() => ((IQueryable<Row>)null!).FilterByColumnDateBetween("On", From, To)).ParamName.ShouldBe("source");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_missing_column_throws(string? column)
        {
            Should.Throw<ArgumentException>(() => Filter([], column!, From, To)).ParamName.ShouldBe("columnPath");
        }

        [Fact]
        public void A_missing_edge_throws()
        {
            ArgumentNullException start = Should.Throw<ArgumentNullException>(() => Filter([], "On", null!, To));
            start.ParamName.ShouldBe("startValue");
            start.Message.ShouldStartWith("Start value cannot be null.");

            ArgumentNullException end = Should.Throw<ArgumentNullException>(() => Filter([], "On", From, null!));
            end.ParamName.ShouldBe("endValue");
            end.Message.ShouldStartWith("End value cannot be null.");
        }

        [Theory]
        [InlineData("Missing")]
        [InlineData("Parent.Missing")]
        public void A_column_that_is_not_there_throws(string column)
        {
            Should.Throw<ArgumentException>(() => Filter([], column, From, To));
        }
    }

    /// <summary>
    /// The page sends its edges with a Z. Read back they become the machine's own time, so on one
    /// that is not in UTC the same filter keeps different rows than it does on the server.
    /// </summary>
    [Collection(ProcessState.Name)]
    public sealed class FilterByColumnDateBetweenZoneTests
    {
        [Fact]
        public void An_edge_in_utc_is_moved_to_the_machines_own_time()
        {
            Row midnight = new Row { On = new DateTime(2031, 3, 5) };

            FilterByColumnDateBetweenTests.Filter([midnight], "On", "2031-03-05T00:00:00.000Z", "2031-03-05T23:59:59.999Z").ShouldBe([midnight]);

            using (LocalTimeZone.Use(LocalTimeZone.Athens))
                FilterByColumnDateBetweenTests.Filter([midnight], "On", "2031-03-05T00:00:00.000Z", "2031-03-05T23:59:59.999Z").ShouldBeEmpty();
        }

        [Fact]
        public void An_edge_that_names_no_zone_is_not_moved()
        {
            Row midnight = new Row { On = new DateTime(2031, 3, 5) };

            using (LocalTimeZone.Use(LocalTimeZone.Athens))
                FilterByColumnDateBetweenTests.Filter([midnight], "On", "2031-03-05T00:00:00", "2031-03-05T23:59:59").ShouldBe([midnight]);
        }
    }
}
