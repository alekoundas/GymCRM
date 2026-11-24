using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Core.CustomValidationAttributes;
using Core.Translations;
using Microsoft.Extensions.Localization;

namespace Core.Tests.CustomValidationAttributes
{
    public sealed class MinListCountAttributeTests
    {
        private const string Untranslated = "At least 1 Train Group Date is required!";

        private static readonly int[] Minimums = [-1, 0, 1, 2, 3];
        private static readonly int[] Counts = [0, 1, 2, 3, 4];

        public static readonly MatrixTheoryData<int, int> EveryMinimumAndCount = new MatrixTheoryData<int, int>(Minimums, Counts);

        private static string? Validate(object? value, int minimum, IServiceProvider? services = null)
        {
            ValidationContext context = new ValidationContext(new object(), services, null) { MemberName = "TrainGroupDates" };

            ValidationResult? result = new MinListCountAttribute(minimum).GetValidationResult(value, context);

            return result?.ErrorMessage;
        }

        [Theory]
        [MemberData(nameof(EveryMinimumAndCount))]
        public void A_list_is_valid_from_the_minimum_up(int minimum, int count)
        {
            List<string> list = Enumerable.Repeat("item", count).ToList();

            string? expected = count >= minimum ? null : Untranslated;

            Validate(list, minimum).ShouldBe(expected);
        }

        // Anything that is an IList counts, whatever it holds.
        [Theory]
        [MemberData(nameof(EveryMinimumAndCount))]
        public void An_array_a_collection_and_an_untyped_list_are_counted_the_same(int minimum, int count)
        {
            string?[] array = new string?[count];
            string? expected = count >= minimum ? null : Untranslated;

            Validate(array, minimum).ShouldBe(expected);
            Validate(new Collection<string?>(array), minimum).ShouldBe(expected);
            Validate(new ArrayList(array), minimum).ShouldBe(expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Anything_that_is_not_a_list_is_refused_whatever_the_minimum(int minimum)
        {
            const string Refused = "Invalid list type for this validation.";

            Validate(null, minimum).ShouldBe(Refused);
            Validate("text", minimum).ShouldBe(Refused);
            Validate(5, minimum).ShouldBe(Refused);
            Validate(new HashSet<int> { 1, 2 }, minimum).ShouldBe(Refused);
            Validate(Enumerable.Range(0, 3).Where(x => x > 0), minimum).ShouldBe(Refused);
        }


        // The message.

        [Fact]
        public void The_message_is_translated_when_there_is_a_translator()
        {
            Validate(new List<string>(), 1, new Services(new FakeLocalizer())).ShouldBe("translated: " + TranslationKeys.At_least_1_Train_Group_Date_is_required);
        }

        [Fact]
        public void The_message_is_the_english_one_when_there_is_no_translator()
        {
            Validate(new List<string>(), 1, new Services(null)).ShouldBe(Untranslated);
        }

        [Fact]
        public void The_message_is_the_english_one_when_the_translator_is_something_else()
        {
            Validate(new List<string>(), 1, new Services("not a translator")).ShouldBe(Untranslated);
        }

        [Fact]
        public void A_list_long_enough_never_asks_for_the_translator()
        {
            Validate(new List<string> { "item" }, 1, new ThrowingServices()).ShouldBeNull();
        }

        private sealed class Services : IServiceProvider
        {
            private readonly object? _localizer;

            public Services(object? localizer)
            {
                _localizer = localizer;
            }

            public object? GetService(Type serviceType)
            {
                return serviceType == typeof(IStringLocalizer) ? _localizer : null;
            }
        }

        private sealed class ThrowingServices : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                throw new InvalidOperationException("Nothing should have been asked for.");
            }
        }

        private sealed class FakeLocalizer : IStringLocalizer
        {
            public LocalizedString this[string name]
            {
                get { return new LocalizedString(name, "translated: " + name); }
            }

            public LocalizedString this[string name, params object[] arguments]
            {
                get { throw new InvalidOperationException("The message has no arguments."); }
            }

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            {
                throw new InvalidOperationException("Not asked for.");
            }
        }
    }
}
