namespace Business.Tests.ExtentionMethods
{
    public sealed class FilterByColumnContainsTests
    {
        private static readonly Row Eleni = new Row { Name = "Ελένη Παπαδοπούλου", MaybeName = "Ελένη", Parent = new Row { Name = "Γιώργος" } };
        private static readonly Row Maria = new Row { Name = "Maria Papa", MaybeName = null, Parent = new Row { Name = "José" } };

        private static List<Row> Filter(string column, string value)
        {
            return new[] { Eleni, Maria }.AsQueryable().FilterByColumnContains(column, value).ToList();
        }

        [Theory]
        [InlineData("Ελένη")]
        [InlineData("ελενη")]
        [InlineData("ΕΛΕΝΗ")]
        [InlineData("λεν")]
        [InlineData("η π")]
        [InlineData("ΠΑΠΑΔΟΠΟΥΛΟΥ")]
        [InlineData("Ελένη Παπαδοπούλου")]
        public void Part_of_a_greek_name_is_found_however_it_is_typed(string value)
        {
            Filter("Name", value).ShouldBe([Eleni]);
        }

        [Theory]
        [InlineData("maria")]
        [InlineData("MARIA")]
        [InlineData("ia p")]
        [InlineData("Mária")]
        public void Part_of_a_latin_name_is_found_however_it_is_typed(string value)
        {
            Filter("Name", value).ShouldBe([Maria]);
        }

        [Theory]
        [InlineData("Νίκος")]
        [InlineData("mariap")]
        [InlineData("Ελένη  Παπαδοπούλου")]
        [InlineData("%")]
        [InlineData("_")]
        [InlineData(" maria")]
        public void What_is_in_no_name_is_not_found(string value)
        {
            Filter("Name", value).ShouldBeEmpty();
        }

        [Fact]
        public void An_empty_text_is_in_every_name()
        {
            Filter("Name", "").ShouldBe([Eleni, Maria]);
        }

        // An empty column reads as an empty text.
        [Fact]
        public void A_column_that_may_be_empty_is_searched_as_an_empty_text()
        {
            Filter("MaybeName", "ελ").ShouldBe([Eleni]);
            Filter("MaybeName", "").ShouldBe([Eleni, Maria]);
        }

        [Theory]
        [InlineData("Parent.Name", "γιωργος")]
        [InlineData("parent.name", "ΓΙΩΡΓΟΣ")]
        [InlineData("name", "ελενη")]
        public void A_column_is_named_by_its_path_in_any_case(string column, string value)
        {
            Filter(column, value).ShouldBe([Eleni]);
        }

        [Theory]
        [InlineData("Number", "Int32")]
        [InlineData("MaybeNumber", "Nullable`1")]
        [InlineData("On", "DateTime")]
        [InlineData("Key", "Guid")]
        [InlineData("IsActive", "Boolean")]
        [InlineData("Day", "DayOfWeek")]
        public void Only_a_text_column_can_be_searched(string column, string type)
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => Filter(column, "5"));

            exception.ParamName.ShouldBe("columnPath");
            exception.Message.ShouldStartWith($"Contains filter is only supported for string properties. Property '{column}' is of type '{type}'.");
        }

        [Fact]
        public void A_missing_source_throws()
        {
            Should.Throw<ArgumentNullException>(() => ((IQueryable<Row>)null!).FilterByColumnContains("Name", "x")).ParamName.ShouldBe("source");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_missing_column_throws(string? column)
        {
            ArgumentException exception = Should.Throw<ArgumentException>(() => Filter(column!, "x"));

            exception.ParamName.ShouldBe("columnPath");
            exception.Message.ShouldStartWith("Column path cannot be null or empty.");
        }

        [Fact]
        public void A_missing_value_throws()
        {
            ArgumentNullException exception = Should.Throw<ArgumentNullException>(() => Filter("Name", null!));

            exception.ParamName.ShouldBe("value");
            exception.Message.ShouldStartWith("Value cannot be null for Contains filter.");
        }

        [Theory]
        [InlineData("Missing")]
        [InlineData("Parent.Missing")]
        [InlineData("Name.")]
        public void A_column_that_is_not_there_throws(string column)
        {
            Should.Throw<ArgumentException>(() => Filter(column, "x"));
        }
    }
}
