namespace Business.Tests.ExtentionMethods
{
    /// <summary>
    /// Equal and not equal, for a value that mostly arrives as text. What each type of column can be
    /// compared with, and what makes it throw - a grid that sends the latter gets a failed request.
    /// </summary>
    public sealed class FilterByColumnEqualityTests
    {
        private static readonly Guid SomeKey = new Guid("11111111-2222-3333-4444-555555555555");
        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        private static readonly Row Match = new Row
        {
            Number = 5,
            MaybeNumber = 5,
            Big = 5,
            Fraction = 5.5,
            Name = "Maria",
            MaybeName = "Maria",
            IsActive = true,
            MaybeActive = true,
            On = SomeDay,
            MaybeOn = SomeDay,
            Key = SomeKey,
            MaybeKey = SomeKey,
            Day = DayOfWeek.Friday,
            Field = 5,
            Parent = new Row { Number = 5, Name = "Maria" }
        };

        private static readonly Row Other = new Row
        {
            Number = 6,
            MaybeNumber = null,
            Big = 6,
            Fraction = 6.5,
            Name = "maria",
            MaybeName = null,
            IsActive = false,
            MaybeActive = null,
            On = SomeDay.AddSeconds(1),
            MaybeOn = null,
            Key = Guid.Empty,
            MaybeKey = null,
            Day = DayOfWeek.Monday,
            Field = 6,
            Parent = new Row { Number = 6, Name = "other" }
        };

        // A value each column finds the first row by.
        public static TheoryData<string, object> Matches
        {
            get
            {
                return new TheoryData<string, object>
                {
                    { "Number", "5" },
                    { "Number", 5 },
                    { "Number", 5L },
                    { "Number", 5.0 },
                    { "Number", " 5 " },
                    { "MaybeNumber", "5" },
                    { "MaybeNumber", 5 },
                    { "MaybeNumber", 5L },
                    { "Big", "5" },
                    { "Big", 5 },
                    { "Fraction", "5.5" },
                    { "Fraction", 5.5 },
                    { "Name", "Maria" },
                    { "MaybeName", "Maria" },
                    { "IsActive", "true" },
                    { "IsActive", "True" },
                    { "IsActive", true },
                    { "IsActive", 1 },
                    { "MaybeActive", "true" },
                    { "MaybeActive", true },
                    { "On", "2031-03-05" },
                    { "On", "2031-03-05 00:00:00" },
                    { "On", SomeDay },
                    { "MaybeOn", "2031-03-05" },
                    { "MaybeOn", SomeDay },
                    { "Key", SomeKey },
                    { "MaybeKey", SomeKey },
                    { "Day", DayOfWeek.Friday },
                    { "Field", "5" },
                    { "Parent.Number", "5" },
                    { "Parent.Name", "Maria" },
                    { "number", "5" },
                    { "parent.name", "Maria" },
                };
            }
        }

        private static List<Row> Filter(string column, object? value, bool isEqual)
        {
            return new[] { Match, Other }.AsQueryable().FilterByColumnEquality(column, value, isEqual).ToList();
        }

        [Theory]
        [MemberData(nameof(Matches))]
        public void Equal_keeps_the_rows_with_the_value(string column, object value)
        {
            Filter(column, value, true).ShouldBe([Match]);
        }

        [Theory]
        [MemberData(nameof(Matches))]
        public void Not_equal_keeps_every_other_row(string column, object value)
        {
            Filter(column, value, false).ShouldBe([Other]);
        }

        [Theory]
        [InlineData("Number", "7")]
        [InlineData("Name", "MARIA")]
        [InlineData("Name", "")]
        [InlineData("MaybeNumber", "7")]
        [InlineData("On", "2031-03-06")]
        public void A_value_no_row_has_finds_nothing_and_leaves_out_nothing(string column, string value)
        {
            Filter(column, value, true).ShouldBeEmpty();
            Filter(column, value, false).ShouldBe([Match, Other]);
        }


        // Null.

        [Theory]
        [InlineData("MaybeNumber")]
        [InlineData("MaybeName")]
        [InlineData("MaybeActive")]
        [InlineData("MaybeOn")]
        [InlineData("MaybeKey")]
        public void Null_finds_the_rows_where_a_column_that_may_be_empty_is(string column)
        {
            Filter(column, null, true).ShouldBe([Other]);
            Filter(column, null, false).ShouldBe([Match]);
        }

        // A text column may always be compared with null, whether or not it can hold it.
        [Fact]
        public void Null_finds_no_row_where_a_text_is_never_empty()
        {
            Filter("Name", null, true).ShouldBeEmpty();
            Filter("Name", null, false).ShouldBe([Match, Other]);
        }

        [Theory]
        [InlineData("Number")]
        [InlineData("Big")]
        [InlineData("IsActive")]
        [InlineData("On")]
        [InlineData("Key")]
        [InlineData("Day")]
        public void Null_against_a_column_that_is_never_empty_throws(string column)
        {
            Should.Throw<InvalidCastException>(() => Filter(column, null, true));
            Should.Throw<InvalidCastException>(() => Filter(column, null, false));
        }

        // Only a whole number takes an empty text to mean nothing.
        [Fact]
        public void An_empty_text_is_null_for_a_number_that_may_be_empty()
        {
            Filter("MaybeNumber", "", true).ShouldBe([Other]);
            Filter("MaybeNumber", "", false).ShouldBe([Match]);
        }

        [Theory]
        [InlineData("Number")]
        [InlineData("IsActive")]
        [InlineData("MaybeActive")]
        [InlineData("On")]
        [InlineData("MaybeOn")]
        public void An_empty_text_against_anything_else_throws(string column)
        {
            Should.Throw<FormatException>(() => Filter(column, "", true));
        }


        // What cannot be turned into the column's type.

        [Theory]
        [InlineData("Number", "five")]
        [InlineData("MaybeNumber", "five")]
        [InlineData("Number", "5.5")]
        [InlineData("Big", "five")]
        [InlineData("IsActive", "yes")]
        [InlineData("MaybeActive", "yes")]
        [InlineData("On", "someday")]
        [InlineData("MaybeOn", "someday")]
        public void Text_that_is_not_one_throws(string column, string value)
        {
            Should.Throw<FormatException>(() => Filter(column, value, true));
            Should.Throw<FormatException>(() => Filter(column, value, false));
        }

        [Theory]
        [InlineData("Number", "99999999999")]
        [InlineData("MaybeNumber", "99999999999")]
        [InlineData("Number", 99999999999L)]
        public void A_number_too_big_throws(string column, object value)
        {
            Should.Throw<OverflowException>(() => Filter(column, value, true));
        }

        // An id or an enum sent as text cannot be compared at all. The controllers that need it
        // convert it themselves.
        [Theory]
        [InlineData("Key", "11111111-2222-3333-4444-555555555555")]
        [InlineData("MaybeKey", "11111111-2222-3333-4444-555555555555")]
        [InlineData("MaybeKey", "")]
        [InlineData("Day", "Friday")]
        [InlineData("Day", 5)]
        public void An_id_or_an_enum_in_text_throws(string column, object value)
        {
            Should.Throw<InvalidCastException>(() => Filter(column, value, true));
            Should.Throw<InvalidCastException>(() => Filter(column, value, false));
        }


        // What the caller has to give.

        [Fact]
        public void A_missing_source_throws()
        {
            Should.Throw<ArgumentNullException>(() => ((IQueryable<Row>)null!).FilterByColumnEquality("Number", 5, true)).ParamName.ShouldBe("source");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_missing_column_throws(string? column)
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => Filter(column!, 5, true));

            exception.ParamName.ShouldBe("columnPath");
            exception.Message.ShouldStartWith("Column path cannot be null or empty.");
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("Missing")]
        [InlineData("Parent.Missing")]
        [InlineData("Number.Value")]
        public void A_column_that_is_not_there_throws(string column)
        {
            Should.Throw<ArgumentException>(() => Filter(column, 5, true));
        }
    }
}
