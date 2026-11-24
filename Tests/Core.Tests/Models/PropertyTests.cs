using System.Collections;
using System.Reflection;
using Core.Dtos;
using Core.Dtos.AutoComplete;
using Core.Dtos.DataTable;
using Core.Dtos.Lookup;
using Core.Dtos.TrainGroupDate;
using Core.Dtos.TrainGroupΑttendance;
using Core.Dtos.UserRole;
using Core.Dtos.WorkoutPlanRule;
using Core.Enums;
using Core.Models;

namespace Core.Tests.Models
{
    /// <summary>
    /// Every class the database and the client are fed with: what a new one starts as, and that each
    /// property keeps what it is given and touches no other.
    /// </summary>
    public sealed class PropertyTests
    {
        // A reference the database or the json always fills, so the class leaves it empty.
        private static readonly string[] EmptyUntilFilled =
        [
            "LookupDto.Filter",
            "TimeSlotResponseDto.Trainer",
            "TrainGroupCancellationSubscriberDto.TrainGroupDate",
            "TrainGroupΑttendanceDto.User",
            "Exercise.WorkoutPlan",
            "ExerciseHistory.Exercise",
            "Mail.User",
            "PhoneNumber.User",
            "QuestionnaireAnswer.QuestionnaireQuestion",
            "QuestionnaireAnswer.User",
            "QuestionnaireQuestion.Questionnaire",
            "Subscription.User",
            "TrainGroup.Trainer",
            "TrainGroupDate.TrainGroup",
            "TrainGroupParticipant.TrainGroupDate",
            "TrainGroupParticipant.TrainGroup",
            "TrainGroupParticipant.User",
            "TrainGroupParticipantUnavailableDate.TrainGroupParticipant",
            "TrainGroupUnavailableDate.TrainGroup",
            "TrainGroupΑttendance.User",
            "UserRole.User",
            "UserRole.Role",
            "WorkoutPlan.User",
            "WorkoutPlanRecording.User",
            "WorkoutPlanRuleWeek.WorkoutPlanRule",
        ];

        public static TheoryData<Type> Classes
        {
            get
            {
                IEnumerable<Type> classes = typeof(User).Assembly.GetTypes()
                    .Where(x => x.IsClass && x.IsPublic && !x.IsAbstract)
                    .Where(x => x.Namespace!.StartsWith("Core.Dtos", StringComparison.Ordinal) || x.Namespace == "Core.Models")
                    .Where(x => x != typeof(ApiResponse<>))
                    .Select(x => x.IsGenericType ? x.MakeGenericType(typeof(LookupOptionDto)) : x)
                    .OrderBy(x => x.Name, StringComparer.Ordinal);

                return new TheoryData<Type>(classes);
            }
        }

        private static IEnumerable<PropertyInfo> PropertiesOf(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead && x.CanWrite);
        }

        private static object New(Type type)
        {
            return Activator.CreateInstance(type)!;
        }

        // Two values of a type that are never equal to each other, nor to a default.
        private static object Sample(Type type, int number)
        {
            Type inner = Nullable.GetUnderlyingType(type) ?? type;

            if (inner == typeof(string))
                return "text " + number;
            if (inner == typeof(int))
                return 40 + number;
            if (inner == typeof(bool))
                return number == 1;
            if (inner == typeof(DateTime))
                return new DateTime(2031, 3, number);
            if (inner == typeof(DateTimeOffset))
                return new DateTimeOffset(2031, 3, number, 0, 0, 0, TimeSpan.Zero);
            if (inner == typeof(Guid))
                return new Guid(number, 2, 3, [4, 5, 6, 7, 8, 9, 10, 11]);
            if (inner == typeof(byte[]))
                return new byte[] { (byte)number };
            if (inner.IsEnum)
                return Enum.ToObject(inner, number - 1);

            if (inner.IsGenericType && inner.IsAssignableTo(typeof(IEnumerable)))
                return New(typeof(List<>).MakeGenericType(inner.GetGenericArguments()[0]));

            return New(inner);
        }

        [Theory]
        [MemberData(nameof(Classes))]
        public void Every_property_keeps_what_it_is_given(Type type)
        {
            foreach (PropertyInfo property in PropertiesOf(type))
                foreach (int number in new[] { 1, 2 })
                {
                    object item = New(type);
                    object value = Sample(property.PropertyType, number);

                    property.SetValue(item, value);

                    property.GetValue(item).ShouldBe(value, $"{type.Name}.{property.Name}");
                }
        }

        [Theory]
        [MemberData(nameof(Classes))]
        public void Setting_one_property_changes_no_other(Type type)
        {
            List<PropertyInfo> properties = PropertiesOf(type).ToList();

            foreach (PropertyInfo changed in properties)
            {
                object item = New(type);
                Dictionary<string, object?> before = properties.ToDictionary(x => x.Name, x => x.GetValue(item));

                changed.SetValue(item, Sample(changed.PropertyType, 2));

                foreach (PropertyInfo other in properties.Where(x => x != changed))
                    other.GetValue(item).ShouldBe(before[other.Name], $"{type.Name}.{other.Name} after {changed.Name}");
            }
        }

        // A text or a list nobody filled in is empty rather than missing: the client reads both
        // without asking.
        [Theory]
        [MemberData(nameof(Classes))]
        public void What_may_not_be_null_never_is(Type type)
        {
            NullabilityInfoContext nullability = new NullabilityInfoContext();
            object item = New(type);

            List<string> missing = PropertiesOf(type)
                .Where(x => x.DeclaringType!.Assembly == typeof(User).Assembly)
                .Where(x => !x.PropertyType.IsValueType && nullability.Create(x).ReadState == NullabilityState.NotNull)
                .Where(x => x.GetValue(item) == null)
                .Select(x => $"{type.Name}.{x.Name}")
                .ToList();

            missing.ShouldBe(EmptyUntilFilled.Where(x => x.Split('.')[0] == type.Name), ignoreOrder: true);
        }

        [Theory]
        [MemberData(nameof(Classes))]
        public void Texts_and_lists_start_empty(Type type)
        {
            object item = New(type);

            foreach (PropertyInfo property in PropertiesOf(type).Where(x => x.DeclaringType!.Assembly == typeof(User).Assembly))
            {
                object? value = property.GetValue(item);

                if (value is string text)
                    text.ShouldBe(string.Empty, $"{type.Name}.{property.Name}");
                else if (value is IEnumerable list)
                    list.Cast<object>().ShouldBeEmpty($"{type.Name}.{property.Name}");
            }
        }


        // What a new one starts as, where it is not simply empty.

        [Fact]
        public void A_new_row_is_stamped_with_the_time_it_was_made()
        {
            DateTime before = DateTime.UtcNow;

            List<DateTime> stamps =
            [
                new BaseModel().CreatedOn,
                new Mail().CreatedOn,
                new User().CreatedOn,
                new Role().CreatedOn,
                new QuestionnaireAnswer().UpdatedOn
            ];

            DateTime after = DateTime.UtcNow;

            foreach (DateTime stamp in stamps)
            {
                stamp.ShouldBeInRange(before, after);
                stamp.Kind.ShouldBe(DateTimeKind.Utc);
            }
        }

        [Fact]
        public void A_new_mail_is_still_to_be_sent()
        {
            new Mail().Status.ShouldBe(MailStatusEnum.PENDING);
            new Mail().SentOn.ShouldBeNull();
        }

        [Fact]
        public void A_new_subscription_is_still_to_be_decided()
        {
            new Subscription().Status.ShouldBe(SubscriptionStatusEnum.PENDING);
            new Subscription().DecidedOn.ShouldBeNull();
        }

        [Fact]
        public void A_new_plan_is_active_and_has_no_rule()
        {
            new WorkoutPlan().IsInactive.ShouldBeFalse();
            new WorkoutPlan().WorkoutPlanRuleId.ShouldBeNull();
            new WorkoutPlan().CurrentWeek.ShouldBeNull();
        }

        [Fact]
        public void A_new_week_of_a_rule_allows_one_recording_unless_it_says_otherwise()
        {
            new WorkoutPlanRuleWeekAddDto().MaxRecordings.ShouldBe(1);
            new WorkoutPlanRuleWeekDto().MaxRecordings.ShouldBe(1);
        }

        [Fact]
        public void A_grid_filter_that_names_no_kind_is_a_contains()
        {
            new DataTableFilterDto().FilterType.ShouldBe(DataTableFiltersEnum.contains);
        }

        [Fact]
        public void A_user_role_always_has_a_role_to_read()
        {
            new UserRoleDto().Role.ShouldNotBeNull();
        }

        [Fact]
        public void A_new_user_has_an_email_to_read_even_before_one_is_given()
        {
            new User().Email.ShouldBe("");
        }
    }
}
