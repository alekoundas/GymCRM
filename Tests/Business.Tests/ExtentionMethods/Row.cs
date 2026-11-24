namespace Business.Tests.ExtentionMethods
{
    /// <summary>
    /// One column of every type a grid could be sorted or filtered by.
    /// </summary>
    internal sealed class Row
    {
        public int Number { get; set; }

        public int? MaybeNumber { get; set; }

        public long Big { get; set; }

        public double Fraction { get; set; }

        public string Name { get; set; } = "";

        public string? MaybeName { get; set; }

        public bool IsActive { get; set; }

        public bool? MaybeActive { get; set; }

        public DateTime On { get; set; }

        public DateTime? MaybeOn { get; set; }

        public Guid Key { get; set; }

        public Guid? MaybeKey { get; set; }

        public DayOfWeek Day { get; set; }

        public Row? Parent { get; set; }

        // A field, which the helpers read just the same.
        public int Field;
    }
}
