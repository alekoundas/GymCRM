using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Core.CustomValidationAttributes;
using Core.Dtos;
using Core.Dtos.Chart;
using Core.Dtos.Exercise;
using Core.Dtos.ExerciseHistory;
using Core.Dtos.Lookup;
using Core.Dtos.Mail;
using Core.Dtos.PhoneNumber;
using Core.Dtos.Subscription;
using Core.Dtos.TrainGroup;
using Core.Dtos.TrainGroupCategory;
using Core.Dtos.TrainGroupDate;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
using Core.Dtos.TrainGroupUnavailableDate;
using Core.Dtos.TrainGroupΑttendance;
using Core.Dtos.User;
using Core.Dtos.UserStatus;
using Core.Dtos.WorkoutPlan;
using Core.Dtos.WorkoutPlanRecording;
using Core.Dtos.WorkoutPlanRule;
using Core.Models;
using Core.Translations;

namespace Core.Tests.Dtos
{
    /// <summary>
    /// What the api refuses before any of its own code runs: every rule on every property the client
    /// sends, at its edges. The last test fails if a rule exists that is not in one of the tables.
    /// </summary>
    public sealed class DtoRuleTests
    {
        private const string Required = TranslationKeys._0_is_required;


        // Text that has to be there.

        public static readonly TheoryData<Type, string, string> RequiredText = new TheoryData<Type, string, string>
        {
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.Name), Required },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.Sets), Required },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.Reps), Required },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.Weight), Required },
            { typeof(ExerciseDto), nameof(ExerciseDto.Name), Required },
            { typeof(ExerciseDto), nameof(ExerciseDto.Sets), Required },
            { typeof(ExerciseDto), nameof(ExerciseDto.Reps), Required },
            { typeof(ExerciseDto), nameof(ExerciseDto.Weight), Required },
            { typeof(ExerciseHistoryDto), nameof(ExerciseHistoryDto.Name), Required },
            { typeof(ExerciseHistoryDto), nameof(ExerciseHistoryDto.Sets), Required },
            { typeof(ExerciseHistoryDto), nameof(ExerciseHistoryDto.Reps), Required },
            { typeof(ExerciseHistoryDto), nameof(ExerciseHistoryDto.Weight), Required },
            { typeof(MailAddDto), nameof(MailAddDto.Subject), Required },
            { typeof(MailAddDto), nameof(MailAddDto.Body), Required },
            { typeof(MailAddDto), nameof(MailAddDto.UserId), Required },
            { typeof(MailDto), nameof(MailDto.Subject), Required },
            { typeof(MailDto), nameof(MailDto.Body), Required },
            { typeof(MailDto), nameof(MailDto.UserId), Required },
            { typeof(MailSendDto), nameof(MailSendDto.Subject), Required },
            { typeof(MailSendDto), nameof(MailSendDto.Body), Required },
            { typeof(PhoneNumberAddDto), nameof(PhoneNumberAddDto.Number), Required },
            { typeof(PhoneNumberAddDto), nameof(PhoneNumberAddDto.UserId), Required },
            { typeof(PhoneNumberDto), nameof(PhoneNumberDto.Number), Required },
            { typeof(PhoneNumberDto), nameof(PhoneNumberDto.UserId), Required },
            { typeof(SubscriptionAddDto), nameof(SubscriptionAddDto.UserId), Required },
            { typeof(TrainGroupAddDto), nameof(TrainGroupAddDto.Title), Required },
            { typeof(TrainGroupAddDto), nameof(TrainGroupAddDto.TrainerId), Required },
            { typeof(TrainGroupDto), nameof(TrainGroupDto.Title), Required },
            { typeof(TrainGroupDto), nameof(TrainGroupDto.TrainerId), Required },
            { typeof(TrainGroupCancellationSubscriberDto), nameof(TrainGroupCancellationSubscriberDto.Id), "Id is required" },
            { typeof(TrainGroupCategoryDto), nameof(TrainGroupCategoryDto.Name), Required },
            { typeof(TrainGroupCategoryAddDto), nameof(TrainGroupCategoryAddDto.Name), Required },
            { typeof(TimeSlotRequestDto), nameof(TimeSlotRequestDto.UserId), Required },
            { typeof(TrainGroupParticipantAddDto), nameof(TrainGroupParticipantAddDto.UserId), Required },
            { typeof(TrainGroupParticipantDto), nameof(TrainGroupParticipantDto.UserId), Required },
            { typeof(TrainGroupΑttendanceAddDto), nameof(TrainGroupΑttendanceAddDto.UserId), Required },
            { typeof(TrainGroupΑttendanceDto), nameof(TrainGroupΑttendanceDto.UserId), Required },
            { typeof(UserDto), nameof(UserDto.UserName), Required },
            { typeof(UserDto), nameof(UserDto.Email), Required },
            { typeof(UserDto), nameof(UserDto.FirstName), Required },
            { typeof(UserDto), nameof(UserDto.LastName), Required },
            { typeof(UserPasswordChangeDto), nameof(UserPasswordChangeDto.UserId), Required },
            { typeof(UserPasswordChangeDto), nameof(UserPasswordChangeDto.OldPassword), Required },
            { typeof(UserPasswordChangeDto), nameof(UserPasswordChangeDto.NewPassword), Required },
            { typeof(UserPasswordForgotDto), nameof(UserPasswordForgotDto.Email), Required },
            { typeof(UserPasswordResetDto), nameof(UserPasswordResetDto.Email), Required },
            { typeof(UserPasswordResetDto), nameof(UserPasswordResetDto.Token), Required },
            { typeof(UserPasswordResetDto), nameof(UserPasswordResetDto.NewPassword), Required },
            { typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.UserName), Required },
            { typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.Email), Required },
            { typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.FirstName), Required },
            { typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.LastName), Required },
            { typeof(UserStatusAddDto), nameof(UserStatusAddDto.Name), Required },
            { typeof(UserStatusAddDto), nameof(UserStatusAddDto.Color), Required },
            { typeof(UserStatusDto), nameof(UserStatusDto.Name), Required },
            { typeof(UserStatusDto), nameof(UserStatusDto.Color), Required },
            { typeof(WorkoutPlanAddDto), nameof(WorkoutPlanAddDto.Title), Required },
            { typeof(WorkoutPlanAddDto), nameof(WorkoutPlanAddDto.UserId), Required },
            { typeof(WorkoutPlanDto), nameof(WorkoutPlanDto.Title), Required },
            { typeof(WorkoutPlanDto), nameof(WorkoutPlanDto.UserId), Required },
            { typeof(WorkoutPlanRuleAddDto), nameof(WorkoutPlanRuleAddDto.Name), Required },
            { typeof(WorkoutPlanRuleDto), nameof(WorkoutPlanRuleDto.Name), Required },
        };

        [Theory]
        [MemberData(nameof(RequiredText))]
        public void A_required_text_has_to_be_more_than_spaces(Type dto, string property, string error)
        {
            foreach (string? missing in new[] { null, "", " ", "\t\r\n" })
                ValidDto.Errors(ValidDto.With(dto, property, missing)).ShouldBe([error]);
        }


        // A thing that has to be there.

        public static readonly TheoryData<Type, string, string> RequiredThing = new TheoryData<Type, string, string>
        {
            { typeof(ChartDataDto), nameof(ChartDataDto.DailyEmails), "The DailyEmails field is required." },
            { typeof(ChartDataDto), nameof(ChartDataDto.UserGrowth), "The UserGrowth field is required." },
            { typeof(LookupDto), nameof(LookupDto.Filter), Required },
            { typeof(MailSendDto), nameof(MailSendDto.UserIds), Required },
        };

        [Theory]
        [MemberData(nameof(RequiredThing))]
        public void A_required_thing_has_to_be_there(Type dto, string property, string error)
        {
            ValidDto.Errors(ValidDto.With(dto, property, null)).ShouldBe([error]);
        }

        // Nobody to send to is a list like any other: the endpoint answers that one itself.
        [Fact]
        public void A_required_list_may_be_empty()
        {
            ValidDto.Errors(ValidDto.With(typeof(MailSendDto), nameof(MailSendDto.UserIds), new List<string>())).ShouldBeEmpty();
        }


        // Numbers and dates marked required. They are never missing once the json is read, so the
        // mark refuses nothing: nought and the first day of year one pass.

        public static readonly TheoryData<Type, string> RequiredValue = new TheoryData<Type, string>
        {
            { typeof(ChartDataDto), nameof(ChartDataDto.AvailableEmails) },
            { typeof(DailyEmailCountDto), nameof(DailyEmailCountDto.Date) },
            { typeof(DailyEmailCountDto), nameof(DailyEmailCountDto.Count) },
            { typeof(UserGrowthDto), nameof(UserGrowthDto.Date) },
            { typeof(UserGrowthDto), nameof(UserGrowthDto.Cumulative) },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.GroupNumber) },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.GroupExerciseOrderNumber) },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.WorkoutPlanId) },
            { typeof(ExerciseDto), nameof(ExerciseDto.GroupNumber) },
            { typeof(ExerciseDto), nameof(ExerciseDto.GroupExerciseOrderNumber) },
            { typeof(ExerciseDto), nameof(ExerciseDto.WorkoutPlanId) },
            { typeof(ExerciseHistoryDto), nameof(ExerciseHistoryDto.GroupNumber) },
            { typeof(ExerciseHistoryDto), nameof(ExerciseHistoryDto.GroupExerciseOrderNumber) },
            { typeof(ExerciseHistoryDto), nameof(ExerciseHistoryDto.ExerciseId) },
            { typeof(SubscriptionAddDto), nameof(SubscriptionAddDto.Amount) },
            { typeof(SubscriptionDecideDto), nameof(SubscriptionDecideDto.Id) },
            { typeof(SubscriptionRemoveDto), nameof(SubscriptionRemoveDto.Id) },
            { typeof(SubscriptionRequestDto), nameof(SubscriptionRequestDto.RequestedAmount) },
            { typeof(TrainGroupAddDto), nameof(TrainGroupAddDto.Duration) },
            { typeof(TrainGroupAddDto), nameof(TrainGroupAddDto.StartOn) },
            { typeof(TrainGroupAddDto), nameof(TrainGroupAddDto.MaxParticipants) },
            { typeof(TrainGroupDto), nameof(TrainGroupDto.Id) },
            { typeof(TrainGroupDto), nameof(TrainGroupDto.Duration) },
            { typeof(TrainGroupDto), nameof(TrainGroupDto.StartOn) },
            { typeof(TrainGroupDto), nameof(TrainGroupDto.MaxParticipants) },
            { typeof(TrainGroupCancellationSubscriberDto), nameof(TrainGroupCancellationSubscriberDto.SelectedDate) },
            { typeof(TrainGroupCancellationSubscriberDto), nameof(TrainGroupCancellationSubscriberDto.TrainGroupDateId) },
            { typeof(TrainGroupCancellationSubscriberDto), nameof(TrainGroupCancellationSubscriberDto.CancellationSubscriberId) },
            { typeof(TimeSlotRequestDto), nameof(TimeSlotRequestDto.SelectedDate) },
            { typeof(TrainGroupDateAddDto), nameof(TrainGroupDateAddDto.TrainGroupDateType) },
            { typeof(TrainGroupDateDto), nameof(TrainGroupDateDto.Id) },
            { typeof(TrainGroupDateDto), nameof(TrainGroupDateDto.TrainGroupDateType) },
            { typeof(TrainGroupDateDto), nameof(TrainGroupDateDto.TrainGroupId) },
            { typeof(TrainGroupParticipantAddDto), nameof(TrainGroupParticipantAddDto.TrainGroupDateId) },
            { typeof(TrainGroupParticipantAddDto), nameof(TrainGroupParticipantAddDto.TrainGroupId) },
            { typeof(TrainGroupParticipantBookDto), nameof(TrainGroupParticipantBookDto.TrainGroupId) },
            { typeof(TrainGroupParticipantBookDto), nameof(TrainGroupParticipantBookDto.SelectedDate) },
            { typeof(TrainGroupParticipantDto), nameof(TrainGroupParticipantDto.TrainGroupDateId) },
            { typeof(TrainGroupParticipantDto), nameof(TrainGroupParticipantDto.TrainGroupId) },
            { typeof(TrainGroupParticipantUnavailableDateAddDto), nameof(TrainGroupParticipantUnavailableDateAddDto.UnavailableDate) },
            { typeof(TrainGroupParticipantUnavailableDateAddDto), nameof(TrainGroupParticipantUnavailableDateAddDto.TrainGroupParticipantId) },
            { typeof(TrainGroupParticipantUnavailableDateDto), nameof(TrainGroupParticipantUnavailableDateDto.UnavailableDate) },
            { typeof(TrainGroupParticipantUnavailableDateDto), nameof(TrainGroupParticipantUnavailableDateDto.TrainGroupParticipantId) },
            { typeof(TrainGroupUnavailableDateAddDto), nameof(TrainGroupUnavailableDateAddDto.UnavailableDate) },
            { typeof(TrainGroupUnavailableDateAddDto), nameof(TrainGroupUnavailableDateAddDto.TrainGroupId) },
            { typeof(TrainGroupUnavailableDateDto), nameof(TrainGroupUnavailableDateDto.UnavailableDate) },
            { typeof(TrainGroupUnavailableDateDto), nameof(TrainGroupUnavailableDateDto.TrainGroupId) },
            { typeof(TrainGroupΑttendanceAddDto), nameof(TrainGroupΑttendanceAddDto.AttendanceDate) },
            { typeof(TrainGroupΑttendanceDto), nameof(TrainGroupΑttendanceDto.AttendanceDate) },
            { typeof(WorkoutPlanStartRequestDto), nameof(WorkoutPlanStartRequestDto.WorkoutPlanId) },
            { typeof(WorkoutPlanStartRequestDto), nameof(WorkoutPlanStartRequestDto.WeekNumber) },
        };

        [Theory]
        [MemberData(nameof(RequiredValue))]
        public void A_required_number_or_date_may_be_its_default(Type dto, string property)
        {
            Type type = dto.GetProperty(property)!.PropertyType;

            // A date type of nought is a weekday, which the valid one already is.
            ValidDto.Errors(ValidDto.With(dto, property, Activator.CreateInstance(type))).ShouldBeEmpty();
        }


        // How long a text may be.

        private const string TooLong500 = TranslationKeys._0_cannot_exceed_500_characters;
        private const string MailSubject = TranslationKeys.Subject_length_must_be_between_5_and_200_character_long;

        public static readonly TheoryData<Type, string, int, string> LongestText = new TheoryData<Type, string, int, string>
        {
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.Description), 500, TooLong500 },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.Sets), 500, TooLong500 },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.Reps), 500, TooLong500 },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.Weight), 500, TooLong500 },
            { typeof(ExerciseAddDto), nameof(ExerciseAddDto.VideoUrl), 500, "The field VideoUrl must be a string with a maximum length of 500." },
            { typeof(ExerciseDto), nameof(ExerciseDto.Description), 500, TooLong500 },
            { typeof(ExerciseDto), nameof(ExerciseDto.Sets), 500, TooLong500 },
            { typeof(ExerciseDto), nameof(ExerciseDto.Reps), 500, TooLong500 },
            { typeof(ExerciseDto), nameof(ExerciseDto.Weight), 500, TooLong500 },
            { typeof(ExerciseDto), nameof(ExerciseDto.VideoUrl), 500, TooLong500 },
            { typeof(MailAddDto), nameof(MailAddDto.Subject), 200, MailSubject },
            { typeof(MailDto), nameof(MailDto.Subject), 200, MailSubject },
            { typeof(MailSendDto), nameof(MailSendDto.Subject), 200, MailSubject },
            { typeof(TrainGroupAddDto), nameof(TrainGroupAddDto.Title), 100, TranslationKeys.Title_cannot_exceed_100_characters },
            { typeof(TrainGroupAddDto), nameof(TrainGroupAddDto.Description), 500, TranslationKeys.Description_cannot_exceed_500_characters },
            { typeof(TrainGroupDto), nameof(TrainGroupDto.Title), 100, TranslationKeys.Title_cannot_exceed_100_characters },
            { typeof(TrainGroupDto), nameof(TrainGroupDto.Description), 500, TranslationKeys.Description_cannot_exceed_500_characters },
            { typeof(TrainGroupCategoryDto), nameof(TrainGroupCategoryDto.Name), 100, TranslationKeys._0_cannot_exceed_100_characters },
            { typeof(TrainGroupCategoryAddDto), nameof(TrainGroupCategoryAddDto.Name), 100, TranslationKeys._0_cannot_exceed_100_characters },
            { typeof(UserDto), nameof(UserDto.UserName), 100, TranslationKeys.UserName_cannot_exceed_100_characters },
            { typeof(UserDto), nameof(UserDto.FirstName), 50, TranslationKeys.FirstName_cannot_exceed_50_characters },
            { typeof(UserDto), nameof(UserDto.LastName), 50, TranslationKeys.LastName_cannot_exceed_50_characters },
            // The message speaks of 200, the limit is 100.
            { typeof(UserDto), nameof(UserDto.Address), 100, TranslationKeys.Address_cannot_exceed_200_characters },
            { typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.UserName), 100, TranslationKeys.UserName_cannot_exceed_100_characters },
            { typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.FirstName), 50, TranslationKeys.FirstName_cannot_exceed_50_characters },
            { typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.LastName), 50, TranslationKeys.LastName_cannot_exceed_50_characters },
            { typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.Address), 100, TranslationKeys.Address_cannot_exceed_200_characters },
        };

        [Theory]
        [MemberData(nameof(LongestText))]
        public void A_text_may_be_as_long_as_its_limit_and_no_longer(Type dto, string property, int longest, string error)
        {
            ValidDto.Errors(ValidDto.With(dto, property, new string('x', longest))).ShouldBeEmpty();
            ValidDto.Errors(ValidDto.With(dto, property, new string('x', longest + 1))).ShouldBe([error]);
        }

        [Theory]
        [InlineData(typeof(UserDto), nameof(UserDto.Email))]
        [InlineData(typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.Email))]
        public void An_email_may_be_a_hundred_characters_and_no_longer(Type dto, string property)
        {
            const string Ending = "@gym.test";

            ValidDto.Errors(ValidDto.With(dto, property, new string('x', 100 - Ending.Length) + Ending)).ShouldBeEmpty();
            ValidDto.Errors(ValidDto.With(dto, property, new string('x', 101 - Ending.Length) + Ending)).ShouldBe([TranslationKeys.Email_cannot_exceed_100_characters]);
        }

        // Twenty characters is the limit, fifteen digits is all the format has room for.
        [Theory]
        [InlineData(typeof(PhoneNumberAddDto))]
        [InlineData(typeof(PhoneNumberDto))]
        public void A_phone_number_too_long_is_refused_for_its_length_and_its_format(Type dto)
        {
            string longest = new string('9', 20);

            ValidDto.Errors(ValidDto.With(dto, "Number", longest)).ShouldBe([TranslationKeys.Invalid_phone_number_format]);
            ValidDto.Errors(ValidDto.With(dto, "Number", longest + "9")).ShouldBe([TranslationKeys.Phone_number_cannot_exceed_20_characters, TranslationKeys.Invalid_phone_number_format], ignoreOrder: true);
        }


        // How short.

        public static readonly TheoryData<Type, string, int, string> ShortestText = new TheoryData<Type, string, int, string>
        {
            { typeof(MailAddDto), nameof(MailAddDto.Subject), 5, MailSubject },
            { typeof(MailDto), nameof(MailDto.Subject), 5, MailSubject },
            { typeof(MailSendDto), nameof(MailSendDto.Subject), 5, MailSubject },
            { typeof(UserPasswordChangeDto), nameof(UserPasswordChangeDto.NewPassword), 6, TranslationKeys.New_password_must_be_at_least_6_characters },
            { typeof(UserPasswordResetDto), nameof(UserPasswordResetDto.NewPassword), 6, TranslationKeys.New_password_must_be_at_least_6_characters },
        };

        [Theory]
        [MemberData(nameof(ShortestText))]
        public void A_text_may_be_as_short_as_its_limit_and_no_shorter(Type dto, string property, int shortest, string error)
        {
            ValidDto.Errors(ValidDto.With(dto, property, new string('x', shortest))).ShouldBeEmpty();
            ValidDto.Errors(ValidDto.With(dto, property, new string('x', shortest - 1))).ShouldBe([error]);
        }


        // How big a number may be.

        public static readonly TheoryData<Type, string, int, int, string> Ranges = new TheoryData<Type, string, int, int, string>
        {
            { typeof(ChartDataDto), nameof(ChartDataDto.AvailableEmails), 0, 500, "The field AvailableEmails must be between 0 and 500." },
            { typeof(DailyEmailCountDto), nameof(DailyEmailCountDto.Count), 0, int.MaxValue, "The field Count must be between 0 and 2147483647." },
            { typeof(UserGrowthDto), nameof(UserGrowthDto.Cumulative), 0, int.MaxValue, "The field Cumulative must be between 0 and 2147483647." },
            { typeof(WorkoutPlanRuleWeekAddDto), nameof(WorkoutPlanRuleWeekAddDto.WeekNumber), 1, 15, Required },
            { typeof(WorkoutPlanRuleWeekAddDto), nameof(WorkoutPlanRuleWeekAddDto.MaxRecordings), 1, 5, Required },
            { typeof(WorkoutPlanRuleWeekDto), nameof(WorkoutPlanRuleWeekDto.WeekNumber), 1, 15, Required },
            { typeof(WorkoutPlanRuleWeekDto), nameof(WorkoutPlanRuleWeekDto.MaxRecordings), 1, 5, Required },
        };

        [Theory]
        [MemberData(nameof(Ranges))]
        public void A_number_may_be_anything_from_its_least_to_its_most(Type dto, string property, int least, int most, string error)
        {
            ValidDto.Errors(ValidDto.With(dto, property, least)).ShouldBeEmpty();
            ValidDto.Errors(ValidDto.With(dto, property, most)).ShouldBeEmpty();
            ValidDto.Errors(ValidDto.With(dto, property, least - 1)).ShouldBe([error]);

            if (most < int.MaxValue)
                ValidDto.Errors(ValidDto.With(dto, property, most + 1)).ShouldBe([error]);
        }


        // What a text has to look like.

        private static readonly string[] Emails = ["maria@gym.test", "a@b", "maria papa@gym.test", "ΜΑΡΙΑ@γυμ.gr"];
        private static readonly string[] NotEmails = ["maria", "@gym.test", "maria@", "maria@gym@test", "@", "maria.gym.test"];

        [Theory]
        [InlineData(typeof(UserDto))]
        [InlineData(typeof(UserRegisterRequestDto))]
        public void An_email_has_one_at_sign_with_something_on_each_side(Type dto)
        {
            foreach (string email in Emails)
                ValidDto.Errors(ValidDto.With(dto, "Email", email)).ShouldBeEmpty(email);

            foreach (string email in NotEmails)
                ValidDto.Errors(ValidDto.With(dto, "Email", email)).ShouldBe([TranslationKeys.Invalid_email_address], email);
        }

        private static readonly string[] Numbers = ["10", "2101234567", "+302101234567", "6971234567", "123456789012345", "+123456789012345"];
        private static readonly string[] NotNumbers = ["1", "0", "0210123456", "+0210123456", "+", "++30210", "210 1234567", "210-1234567", "(210)1234567", "2101234567a", "1234567890123456", "+1"];

        [Theory]
        [InlineData(typeof(PhoneNumberAddDto))]
        [InlineData(typeof(PhoneNumberDto))]
        public void A_phone_number_is_two_to_fifteen_digits_not_starting_with_nought(Type dto)
        {
            foreach (string number in Numbers)
                ValidDto.Errors(ValidDto.With(dto, "Number", number)).ShouldBeEmpty(number);

            foreach (string number in NotNumbers)
                ValidDto.Errors(ValidDto.With(dto, "Number", number)).ShouldBe([TranslationKeys.Invalid_phone_number_format], number);
        }

        private static readonly string?[] Colors = [null, "", "#000000", "#ffffff", "#FFFFFF", "#aB09cD"];
        private static readonly string[] NotColors = ["#fff", "000000", "#00000", "#0000000", "#00000g", "red", " #000000", "#000000 ", " ", "rgb(0,0,0)"];

        [Theory]
        [InlineData(typeof(TrainGroupCategoryDto))]
        [InlineData(typeof(TrainGroupCategoryAddDto))]
        public void A_colour_is_a_hash_and_six_hex_digits_or_nothing(Type dto)
        {
            foreach (string? color in Colors)
                ValidDto.Errors(ValidDto.With(dto, "Color", color)).ShouldBeEmpty(color);

            foreach (string color in NotColors)
                ValidDto.Errors(ValidDto.With(dto, "Color", color)).ShouldBe([TranslationKeys.Invalid_data_provided], color);
        }


        // Lists that need something in them. The message is the same for both whatever the property
        // says, because the attribute writes its own.

        [Fact]
        public void A_new_train_group_needs_a_date()
        {
            ValidDto.Errors(ValidDto.With(typeof(TrainGroupAddDto), nameof(TrainGroupAddDto.TrainGroupDates), new List<TrainGroupDateAddDto>()))
                .ShouldBe(["At least 1 Train Group Date is required!"]);
        }

        [Fact]
        public void A_new_member_needs_a_phone_number()
        {
            ValidDto.Errors(ValidDto.With(typeof(UserRegisterRequestDto), nameof(UserRegisterRequestDto.PhoneNumbers), new List<PhoneNumberAddDto>()))
                .ShouldBe(["At least 1 Train Group Date is required!"]);
        }


        // A date against its type: every combination is in TrainGroupDateTypeValidationAttributeTests.

        [Theory]
        [InlineData(typeof(TrainGroupDateAddDto))]
        [InlineData(typeof(TrainGroupDateDto))]
        public void A_train_group_date_is_checked_against_its_type(Type dto)
        {
            ValidDto.Errors(ValidDto.With(dto, "RecurrenceDayOfWeek", null)).ShouldBe(["RecurrenceDayOfWeek is required when TrainGroupDateType is DAY_OF_WEEK."]);
        }


        // Nothing missed.

        [Fact]
        public void A_valid_one_of_each_is_valid()
        {
            foreach (Type type in ValidDto.Types)
                ValidDto.Errors(ValidDto.Of(type)).ShouldBeEmpty(type.Name);
        }

        [Fact]
        public void Every_rule_there_is_has_a_test_here()
        {
            List<string> tested = new List<string>();

            tested.AddRange(RequiredText.Select(x => $"{x.Data.Item1.Name}.{x.Data.Item2} Required"));
            tested.AddRange(RequiredThing.Select(x => $"{x.Data.Item1.Name}.{x.Data.Item2} Required"));
            tested.AddRange(RequiredValue.Select(x => $"{x.Data.Item1.Name}.{x.Data.Item2} Required"));
            tested.AddRange(LongestText.Select(x => $"{x.Data.Item1.Name}.{x.Data.Item2} StringLength"));
            tested.AddRange(ShortestText.Where(x => x.Data.Item2 == "NewPassword").Select(x => $"{x.Data.Item1.Name}.{x.Data.Item2} MinLength"));
            tested.AddRange(Ranges.Select(x => $"{x.Data.Item1.Name}.{x.Data.Item2} Range"));
            tested.AddRange(new[] { "UserDto.Email", "UserRegisterRequestDto.Email" }.SelectMany(x => new[] { $"{x} StringLength", $"{x} EmailAddress" }));
            tested.AddRange(new[] { "PhoneNumberAddDto.Number", "PhoneNumberDto.Number" }.SelectMany(x => new[] { $"{x} StringLength", $"{x} RegularExpression" }));
            tested.AddRange(["TrainGroupCategoryDto.Color RegularExpression", "TrainGroupCategoryAddDto.Color RegularExpression"]);
            tested.AddRange(["TrainGroupAddDto.TrainGroupDates MinListCount", "UserRegisterRequestDto.PhoneNumbers MinListCount"]);
            tested.AddRange(["TrainGroupDateAddDto TrainGroupDateTypeValidation", "TrainGroupDateDto TrainGroupDateTypeValidation"]);

            RulesInTheCode().Order().ShouldBe(tested.Order());
        }

        private static IEnumerable<string> RulesInTheCode()
        {
            foreach (Type type in typeof(UserDto).Assembly.GetTypes())
            {
                foreach (ValidationAttribute attribute in type.GetCustomAttributes<ValidationAttribute>())
                    yield return $"{type.Name} {NameOf(attribute)}";

                foreach (PropertyInfo property in type.GetProperties())
                    foreach (ValidationAttribute attribute in property.GetCustomAttributes<ValidationAttribute>())
                        yield return $"{type.Name}.{property.Name} {NameOf(attribute)}";
            }
        }

        private static string NameOf(ValidationAttribute attribute)
        {
            return attribute.GetType().Name.Replace("Attribute", string.Empty);
        }
    }
}
