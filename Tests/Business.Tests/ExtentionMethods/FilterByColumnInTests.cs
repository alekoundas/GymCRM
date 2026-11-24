using System.Collections;

namespace Business.Tests.ExtentionMethods
{
    public sealed class FilterByColumnInTests
    {
        private static readonly Guid SomeKey = new Guid("11111111-2222-3333-4444-555555555555");
        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        private static readonly Row One = new Row
        {
            Number = 1,
            MaybeNumber = 1,
            Big = 1,
            Name = "one",
            MaybeName = "one",
            IsActive = true,
            MaybeActive = true,
            On = SomeDay,
            MaybeOn = SomeDay,
            Key = SomeKey,
            MaybeKey = SomeKey,
            Parent = new Row { Number = 1 }
        };

        private static readonly Row Two = new Row
        {
            Number = 2,
            MaybeNumber = null,
            Big = 2,
            Name = "two",
            MaybeName = null,
            IsActive = false,
            MaybeActive = null,
            On = SomeDay.AddDays(1),
            MaybeOn = null,
            Key = Guid.Empty,
            MaybeKey = null,
            Parent = new Row { Number = 2 }
        };

        private static List<Row> Filter(string column, IEnumerable values)
        {
            return new[] { One, Two }.AsQueryable().FilterByColumnIn(column, values).ToList();
        }

        public static TheoryData<string, object> Matches
        {
            get
            {
                return new TheoryData<string, object>
                {
                    { "Number", "1" },
                    { "Number", 1 },
                    { "Number", 1L },
                    { "MaybeNumber", "1" },
                    { "MaybeNumber", 1 },
                    { "Big", "1" },
                    { "Name", "one" },
                    { "MaybeName", "one" },
                    { "IsActive", "true" },
                    { "IsActive", true },
                    { "MaybeActive", "True" },
                    { "On", "2031-03-05" },
                    { "On", SomeDay },
                    { "MaybeOn", "2031-03-05" },
                    { "Key", "11111111-2222-3333-4444-555555555555" },
                    { "Key", "{11111111-2222-3333-4444-555555555555}" },
                    { "Key", "11111111222233334444555555555555" },
                    { "Key", SomeKey },
                    { "MaybeKey", "11111111-2222-3333-4444-555555555555" },
                    { "MaybeKey", SomeKey },
                    { "Parent.Number", "1" },
                    { "parent.number", "1" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(Matches))]
        public void A_row_is_kept_when_its_value_is_the_one(string column, object value)
        {
            Filter(column, new[] { value }).ShouldBe([One]);
        }

        [Theory]
        [MemberData(nameof(Matches))]
        public void The_same_value_twice_is_the_same_value(string column, object value)
        {
            Filter(column, new[] { value, value }).ShouldBe([One]);
        }

        [Fact]
        public void A_row_is_kept_when_its_value_is_any_of_several()
        {
            Filter("Number", new[] { "1", "2" }).ShouldBe([One, Two]);
            Filter("Number", new object[] { "2", 1, 3L }).ShouldBe([One, Two]);
            Filter("Name", new[] { "two", "three" }).ShouldBe([Two]);
            Filter("Key", new[] { Guid.Empty.ToString(), SomeKey.ToString() }).ShouldBe([One, Two]);
        }

        [Fact]
        public void A_value_no_row_has_keeps_none()
        {
            Filter("Number", new[] { "3" }).ShouldBeEmpty();
            Filter("Name", new[] { "ONE" }).ShouldBeEmpty();
            Filter("Key", new[] { Guid.NewGuid().ToString() }).ShouldBeEmpty();
        }

        [Fact]
        public void No_values_keep_no_rows()
        {
            Filter("Number", Array.Empty<string>()).ShouldBeEmpty();
            Filter("Number", new ArrayList()).ShouldBeEmpty();
        }

        // Nothing is looked up for it, so even a column that is not there passes.
        [Fact]
        public void No_values_keep_no_rows_whatever_the_column()
        {
            Filter("Missing", Array.Empty<string>()).ShouldBeEmpty();
        }


        // Null among the values.

        [Theory]
        [InlineData("MaybeNumber", "1")]
        [InlineData("MaybeActive", "true")]
        [InlineData("MaybeOn", "2031-03-05")]
        [InlineData("MaybeKey", "11111111-2222-3333-4444-555555555555")]
        public void Null_keeps_the_rows_where_a_value_that_may_be_empty_is(string column, string value)
        {
            Filter(column, new string?[] { null }).ShouldBe([Two]);
            Filter(column, new string?[] { null, value }).ShouldBe([One, Two]);
        }

        // A text column is one that cannot hold null as far as this is concerned.
        [Theory]
        [InlineData("Number", "Int32")]
        [InlineData("On", "DateTime")]
        [InlineData("Key", "Guid")]
        [InlineData("IsActive", "Boolean")]
        [InlineData("Name", "String")]
        [InlineData("MaybeName", "String")]
        public void Null_against_any_other_column_throws(string column, string type)
        {
            Should.Throw<ArgumentException>(() => Filter(column, new string?[] { null })).Message
                .ShouldBe($"Null value provided for non-nullable property '{column}' of type '{type}'.");
        }


        // What cannot be turned into the column's type.

        [Theory]
        [InlineData("Number", "five", "Int32", typeof(FormatException))]
        [InlineData("Number", "", "Int32", typeof(FormatException))]
        [InlineData("Number", "99999999999", "Int32", typeof(OverflowException))]
        [InlineData("MaybeNumber", "five", "Int32", typeof(FormatException))]
        [InlineData("MaybeNumber", "", "Int32", typeof(FormatException))]
        [InlineData("IsActive", "yes", "Boolean", typeof(FormatException))]
        [InlineData("On", "someday", "DateTime", typeof(FormatException))]
        [InlineData("Day", "Friday", "DayOfWeek", typeof(InvalidCastException))]
        public void A_value_that_is_not_one_throws_and_says_which(string column, string value, string type, Type reason)
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => Filter(column, new[] { value }));

            exception.Message.ShouldBe($"Cannot convert value '{value}' to type '{type}' for property '{column}'.");
            exception.InnerException.ShouldBeOfType(reason);
        }

        [Theory]
        [InlineData("Key")]
        [InlineData("MaybeKey")]
        public void Text_that_is_not_an_id_throws_and_says_which(string column)
        {
            Should.Throw<ArgumentException>(() => Filter(column, new[] { "five" })).Message
                .ShouldBe($"Value 'five' is not a valid Guid for property '{column}'.");
        }

        [Fact]
        public void Something_that_is_not_an_id_at_all_throws()
        {
            Should.Throw<ArgumentException>(() => Filter("Key", new object[] { 5 })).Message
                .ShouldBe("Cannot convert value '5' to type 'Guid' for property 'Key'.");
        }


        // What the caller has to give.

        [Fact]
        public void A_missing_source_throws()
        {
            Should.Throw<ArgumentNullException>(() => ((IQueryable<Row>)null!).FilterByColumnIn("Number", new[] { 1 })).ParamName.ShouldBe("source");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_missing_column_throws(string? column)
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => Filter(column!, new[] { 1 }));

            exception.ParamName.ShouldBe("columnPath");
            exception.Message.ShouldStartWith("Column path cannot be null or empty.");
        }

        [Fact]
        public void Missing_values_throw()
        {
            Should.Throw<ArgumentNullException>(() => Filter("Number", null!)).ParamName.ShouldBe("values");
        }

        [Theory]
        [InlineData("Missing")]
        [InlineData("Parent.Missing")]
        public void A_column_that_is_not_there_throws(string column)
        {
            Should.Throw<ArgumentException>(() => Filter(column, new[] { 1 }));
        }
    }
}
