namespace Business.Tests.ExtentionMethods
{
    /// <summary>
    /// Sorting by a column named in text, which is how a grid asks for it.
    /// </summary>
    public sealed class OrderByColumnTests
    {
        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        private static readonly Row[] Rows =
        [
            new Row { Number = 2, Name = "b", MaybeNumber = 5, On = SomeDay, Field = 30, Parent = new Row { Number = 9, Name = "x" } },
            new Row { Number = 1, Name = "c", MaybeNumber = null, On = SomeDay.AddDays(2), Field = 10, Parent = new Row { Number = 7, Name = "y" } },
            new Row { Number = 3, Name = "a", MaybeNumber = 1, On = SomeDay.AddDays(1), Field = 20, Parent = new Row { Number = 8, Name = "x" } },
        ];

        public static readonly TheoryData<string, string> Columns = new TheoryData<string, string>
        {
            { "Number", "123" },
            { "Name", "321" },
            { "MaybeNumber", "132" },
            { "On", "231" },
            { "Field", "132" },
            { "Parent.Number", "132" },
            // The first letter's case does not matter, nor any other.
            { "number", "123" },
            { "NUMBER", "123" },
            { "parent.number", "132" },
        };

        private static string NumbersOf(IEnumerable<Row> rows)
        {
            return string.Concat(rows.Select(x => x.Number));
        }

        private static string Reversed(string text)
        {
            return string.Concat(text.Reverse());
        }

        [Theory]
        [MemberData(nameof(Columns))]
        public void Ascending(string column, string expected)
        {
            NumbersOf(Rows.AsQueryable().OrderByColumn(column)).ShouldBe(expected);
        }

        [Theory]
        [MemberData(nameof(Columns))]
        public void Descending(string column, string expected)
        {
            NumbersOf(Rows.AsQueryable().OrderByColumnDescending(column)).ShouldBe(Reversed(expected));
        }

        // Two rows share Parent.Name, so the second column decides between them.
        [Theory]
        [MemberData(nameof(Columns))]
        public void Then_ascending_decides_only_where_the_first_is_equal(string column, string expected)
        {
            string sameParent = string.Concat(expected.Where(x => x != '1'));

            NumbersOf(Rows.AsQueryable().OrderByColumn("Parent.Name").ThenByColumn(column)).ShouldBe(sameParent + "1");
        }

        [Theory]
        [MemberData(nameof(Columns))]
        public void Then_descending_decides_only_where_the_first_is_equal(string column, string expected)
        {
            string sameParent = string.Concat(expected.Where(x => x != '1'));

            NumbersOf(Rows.AsQueryable().OrderByColumn("Parent.Name").ThenByColumnDescending(column)).ShouldBe(Reversed(sameParent) + "1");
        }

        [Theory]
        [InlineData("Missing")]
        [InlineData("Parent.Missing")]
        [InlineData("")]
        [InlineData("Number.")]
        [InlineData(" Number")]
        public void A_column_that_is_not_there_is_refused(string column)
        {
            IQueryable<Row> rows = Rows.AsQueryable();

            Should.Throw<ArgumentException>(() => rows.OrderByColumn(column));
            Should.Throw<ArgumentException>(() => rows.OrderByColumnDescending(column));
            Should.Throw<ArgumentException>(() => rows.OrderByColumn("Number").ThenByColumn(column));
            Should.Throw<ArgumentException>(() => rows.OrderByColumn("Number").ThenByColumnDescending(column));
        }

        [Fact]
        public void No_rows_sort_to_no_rows()
        {
            Array.Empty<Row>().AsQueryable().OrderByColumn("Number").ShouldBeEmpty();
        }
    }
}
