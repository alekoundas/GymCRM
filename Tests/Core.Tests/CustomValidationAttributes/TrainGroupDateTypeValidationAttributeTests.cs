using System.ComponentModel.DataAnnotations;
using Core.CustomValidationAttributes;
using Core.Dtos.TrainGroupDate;
using Core.Enums;

namespace Core.Tests.CustomValidationAttributes
{
    /// <summary>
    /// A train group date has to carry the day its type runs on. Checked over every type, every one
    /// of the three days given or left out, alone and in a list, for adding and for editing.
    /// </summary>
    public sealed class TrainGroupDateTypeValidationAttributeTests
    {
        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        // The three types there are, and two numbers that are not a type at all.
        private static readonly int[] Types = [0, 1, 2, 3, -1];
        private static readonly bool[] GivenOrNot = [false, true];

        public static readonly MatrixTheoryData<int, bool, bool, bool> EveryDate = new MatrixTheoryData<int, bool, bool, bool>(Types, GivenOrNot, GivenOrNot, GivenOrNot);

        private static string? Validate(object? value)
        {
            ValidationContext context = new ValidationContext(new object());

            return new TrainGroupDateTypeValidationAttribute().GetValidationResult(value, context)?.ErrorMessage;
        }

        private static TrainGroupDateAddDto Add(int type, bool hasFixedDay, bool hasDayOfMonth, bool hasDayOfWeek)
        {
            return new TrainGroupDateAddDto
            {
                TrainGroupDateType = (TrainGroupDateTypeEnum)type,
                FixedDay = hasFixedDay ? SomeDay : null,
                RecurrenceDayOfMonth = hasDayOfMonth ? 5 : null,
                RecurrenceDayOfWeek = hasDayOfWeek ? DayOfWeek.Wednesday : null
            };
        }

        private static TrainGroupDateDto Edit(int type, bool hasFixedDay, bool hasDayOfMonth, bool hasDayOfWeek)
        {
            return new TrainGroupDateDto
            {
                TrainGroupDateType = (TrainGroupDateTypeEnum)type,
                FixedDay = hasFixedDay ? SomeDay : null,
                RecurrenceDayOfMonth = hasDayOfMonth ? 5 : null,
                RecurrenceDayOfWeek = hasDayOfWeek ? DayOfWeek.Wednesday : null
            };
        }

        // What is wrong with a date, worked out from the rule rather than from the code.
        private static string? Wrong(int type, bool hasFixedDay, bool hasDayOfMonth, bool hasDayOfWeek)
        {
            switch (type)
            {
                case 0:
                    return hasDayOfWeek ? null : "RecurrenceDayOfWeek is required when TrainGroupDateType is DAY_OF_WEEK.";
                case 1:
                    return hasDayOfMonth ? null : "RecurrenceDayOfMonth is required when TrainGroupDateType is DAY_OF_MONTH.";
                case 2:
                    return hasFixedDay ? null : "FixedDay is required when TrainGroupDateType is FIXED_DAY.";
                default:
                    return "Invalid TrainGroupDateType value.";
            }
        }

        private static string? WrongAt(int index, string? wrong)
        {
            return wrong == null ? null : $"TrainGroupDateAddDto at index {index}: {wrong}";
        }

        [Theory]
        [MemberData(nameof(EveryDate))]
        public void One_date_being_added_needs_the_day_its_type_runs_on(int type, bool hasFixedDay, bool hasDayOfMonth, bool hasDayOfWeek)
        {
            Validate(Add(type, hasFixedDay, hasDayOfMonth, hasDayOfWeek)).ShouldBe(Wrong(type, hasFixedDay, hasDayOfMonth, hasDayOfWeek));
        }

        [Theory]
        [MemberData(nameof(EveryDate))]
        public void One_date_being_edited_needs_the_day_its_type_runs_on(int type, bool hasFixedDay, bool hasDayOfMonth, bool hasDayOfWeek)
        {
            Validate(Edit(type, hasFixedDay, hasDayOfMonth, hasDayOfWeek)).ShouldBe(Wrong(type, hasFixedDay, hasDayOfMonth, hasDayOfWeek));
        }

        [Theory]
        [MemberData(nameof(EveryDate))]
        public void Each_date_in_a_list_being_added_is_checked_and_named_by_its_place(int type, bool hasFixedDay, bool hasDayOfMonth, bool hasDayOfWeek)
        {
            TrainGroupDateAddDto fine = Add(0, false, false, true);
            TrainGroupDateAddDto checkedDate = Add(type, hasFixedDay, hasDayOfMonth, hasDayOfWeek);

            Validate(new List<TrainGroupDateAddDto> { fine, checkedDate }).ShouldBe(WrongAt(1, Wrong(type, hasFixedDay, hasDayOfMonth, hasDayOfWeek)));
        }

        [Theory]
        [MemberData(nameof(EveryDate))]
        public void Each_date_in_a_list_being_edited_is_checked_and_named_by_its_place(int type, bool hasFixedDay, bool hasDayOfMonth, bool hasDayOfWeek)
        {
            TrainGroupDateDto fine = Edit(0, false, false, true);
            TrainGroupDateDto checkedDate = Edit(type, hasFixedDay, hasDayOfMonth, hasDayOfWeek);

            Validate(new List<TrainGroupDateDto> { fine, checkedDate }).ShouldBe(WrongAt(1, Wrong(type, hasFixedDay, hasDayOfMonth, hasDayOfWeek)));
        }

        [Fact]
        public void An_empty_list_is_valid()
        {
            Validate(new List<TrainGroupDateAddDto>()).ShouldBeNull();
            Validate(new List<TrainGroupDateDto>()).ShouldBeNull();
        }

        [Fact]
        public void Every_wrong_date_in_a_list_is_reported_in_one_message()
        {
            const string Expected =
                "TrainGroupDateAddDto at index 0: FixedDay is required when TrainGroupDateType is FIXED_DAY.; " +
                "TrainGroupDateAddDto at index 1: RecurrenceDayOfMonth is required when TrainGroupDateType is DAY_OF_MONTH.; " +
                "TrainGroupDateAddDto at index 2: RecurrenceDayOfWeek is required when TrainGroupDateType is DAY_OF_WEEK.; " +
                "TrainGroupDateAddDto at index 4: Invalid TrainGroupDateType value.";

            Validate(new List<TrainGroupDateAddDto> { Add(2, false, true, true), Add(1, true, false, true), Add(0, true, true, false), Add(2, true, false, false), Add(9, true, true, true) }).ShouldBe(Expected);
            Validate(new List<TrainGroupDateDto> { Edit(2, false, true, true), Edit(1, true, false, true), Edit(0, true, true, false), Edit(2, true, false, false), Edit(9, true, true, true) }).ShouldBe(Expected);
        }

        // An empty place is not counted, so whatever follows it is named one place early.
        [Fact]
        public void An_empty_place_in_a_list_is_reported_and_does_not_count()
        {
            const string Expected =
                "TrainGroupDateAddDto at index 0 is null.; " +
                "TrainGroupDateAddDto at index 0: FixedDay is required when TrainGroupDateType is FIXED_DAY.; " +
                "TrainGroupDateAddDto at index 1 is null.";

            Validate(new List<TrainGroupDateAddDto> { null!, Add(2, false, false, false), null! }).ShouldBe(Expected);
            Validate(new List<TrainGroupDateDto> { null!, Edit(2, false, false, false), null! }).ShouldBe(Expected);
        }

        [Fact]
        public void Anything_else_is_refused()
        {
            const string Refused = "Invalid type for validation. Expected TrainGroupDateAddDto or a collection of TrainGroupDateAddDto.";

            Validate(null).ShouldBe(Refused);
            Validate("text").ShouldBe(Refused);
            Validate(5).ShouldBe(Refused);
            Validate(new List<string>()).ShouldBe(Refused);
        }
    }
}
