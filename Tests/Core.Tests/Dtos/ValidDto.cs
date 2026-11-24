using System.ComponentModel.DataAnnotations;
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
using Core.Enums;
using Core.Models;

namespace Core.Tests.Dtos
{
    /// <summary>
    /// One of each thing the client sends, filled in so that nothing about it is refused. A rule is
    /// tested by changing one property of it.
    /// </summary>
    internal static class ValidDto
    {
        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        private static readonly Dictionary<Type, Func<object>> All = new Dictionary<Type, Func<object>>
        {
            [typeof(ChartDataDto)] = () => new ChartDataDto { AvailableEmails = 10 },
            [typeof(DailyEmailCountDto)] = () => new DailyEmailCountDto { Date = SomeDay, Count = 1 },
            [typeof(UserGrowthDto)] = () => new UserGrowthDto { Date = SomeDay, Cumulative = 1 },
            [typeof(ExerciseAddDto)] = () => new ExerciseAddDto { Name = "Squat", Sets = "3", Reps = "10", Weight = "50", GroupNumber = 1, GroupExerciseOrderNumber = 1, WorkoutPlanId = 1 },
            [typeof(ExerciseDto)] = () => new ExerciseDto { Name = "Squat", Sets = "3", Reps = "10", Weight = "50", GroupNumber = 1, GroupExerciseOrderNumber = 1, WorkoutPlanId = 1 },
            [typeof(ExerciseHistoryDto)] = () => new ExerciseHistoryDto { Name = "Squat", Sets = "3", Reps = "10", Weight = "50", GroupNumber = 1, GroupExerciseOrderNumber = 1, ExerciseId = 1 },
            [typeof(LookupDto)] = () => new LookupDto { Filter = new LookupFilterDto() },
            [typeof(MailAddDto)] = () => new MailAddDto { Subject = "Hello", Body = "Body", UserId = "1" },
            [typeof(MailDto)] = () => new MailDto { Subject = "Hello", Body = "Body", UserId = "1" },
            [typeof(MailSendDto)] = () => new MailSendDto { Subject = "Hello", Body = "Body", UserIds = ["1"] },
            [typeof(PhoneNumberAddDto)] = () => new PhoneNumberAddDto { Number = "2101234567", UserId = "1" },
            [typeof(PhoneNumberDto)] = () => new PhoneNumberDto { Number = "2101234567", UserId = "1" },
            [typeof(SubscriptionAddDto)] = () => new SubscriptionAddDto { UserId = "1", Amount = 10 },
            [typeof(SubscriptionDecideDto)] = () => new SubscriptionDecideDto { Id = 1 },
            [typeof(SubscriptionRemoveDto)] = () => new SubscriptionRemoveDto { Id = 1 },
            [typeof(SubscriptionRequestDto)] = () => new SubscriptionRequestDto { RequestedAmount = 10 },
            [typeof(TrainGroupAddDto)] = () => new TrainGroupAddDto { Title = "Pilates", Duration = SomeDay, StartOn = SomeDay, MaxParticipants = 5, TrainerId = "1", TrainGroupDates = [Date()] },
            [typeof(TrainGroupDto)] = () => new TrainGroupDto { Id = 1, Title = "Pilates", Duration = SomeDay, StartOn = SomeDay, MaxParticipants = 5, TrainerId = "1" },
            [typeof(TrainGroupCancellationSubscriberDto)] = () => new TrainGroupCancellationSubscriberDto { Id = "1", SelectedDate = SomeDay, TrainGroupDateId = 1, CancellationSubscriberId = Guid.NewGuid() },
            [typeof(TrainGroupCategoryDto)] = () => new TrainGroupCategoryDto { Id = 1, Name = "Reformer", Color = "#aabbcc" },
            [typeof(TrainGroupCategoryAddDto)] = () => new TrainGroupCategoryAddDto { Name = "Reformer", Color = "#aabbcc" },
            [typeof(TimeSlotRequestDto)] = () => new TimeSlotRequestDto { SelectedDate = SomeDay, UserId = "1" },
            [typeof(TrainGroupDateAddDto)] = () => Date(),
            [typeof(TrainGroupDateDto)] = () => new TrainGroupDateDto { Id = 1, TrainGroupId = 1, TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK, RecurrenceDayOfWeek = DayOfWeek.Monday },
            [typeof(TrainGroupParticipantAddDto)] = () => new TrainGroupParticipantAddDto { TrainGroupDateId = 1, TrainGroupId = 1, UserId = "1" },
            [typeof(TrainGroupParticipantBookDto)] = () => new TrainGroupParticipantBookDto { TrainGroupId = 1, SelectedDate = SomeDay },
            [typeof(TrainGroupParticipantDto)] = () => new TrainGroupParticipantDto { TrainGroupDateId = 1, TrainGroupId = 1, UserId = "1" },
            [typeof(TrainGroupParticipantUnavailableDateAddDto)] = () => new TrainGroupParticipantUnavailableDateAddDto { UnavailableDate = SomeDay, TrainGroupParticipantId = 1 },
            [typeof(TrainGroupParticipantUnavailableDateDto)] = () => new TrainGroupParticipantUnavailableDateDto { UnavailableDate = SomeDay, TrainGroupParticipantId = 1 },
            [typeof(TrainGroupUnavailableDateAddDto)] = () => new TrainGroupUnavailableDateAddDto { UnavailableDate = SomeDay, TrainGroupId = 1 },
            [typeof(TrainGroupUnavailableDateDto)] = () => new TrainGroupUnavailableDateDto { UnavailableDate = SomeDay, TrainGroupId = 1 },
            [typeof(TrainGroupΑttendanceAddDto)] = () => new TrainGroupΑttendanceAddDto { AttendanceDate = SomeDay, UserId = "1" },
            [typeof(TrainGroupΑttendanceDto)] = () => new TrainGroupΑttendanceDto { AttendanceDate = SomeDay, UserId = "1" },
            [typeof(UserDto)] = () => new UserDto { UserName = "maria", Email = "maria@gym.test", FirstName = "Maria", LastName = "Papa" },
            [typeof(UserPasswordChangeDto)] = () => new UserPasswordChangeDto { UserId = "1", OldPassword = "old", NewPassword = "newnew" },
            [typeof(UserPasswordForgotDto)] = () => new UserPasswordForgotDto { Email = "maria@gym.test" },
            [typeof(UserPasswordResetDto)] = () => new UserPasswordResetDto { Email = "maria@gym.test", Token = "token", NewPassword = "newnew" },
            [typeof(UserRegisterRequestDto)] = () => new UserRegisterRequestDto { UserName = "maria", Email = "maria@gym.test", FirstName = "Maria", LastName = "Papa", PhoneNumbers = [new PhoneNumberAddDto()] },
            [typeof(UserStatusAddDto)] = () => new UserStatusAddDto { Name = "Active", Color = "#aabbcc" },
            [typeof(UserStatusDto)] = () => new UserStatusDto { Name = "Active", Color = "#aabbcc" },
            [typeof(WorkoutPlanAddDto)] = () => new WorkoutPlanAddDto { Title = "Legs", UserId = "1" },
            [typeof(WorkoutPlanDto)] = () => new WorkoutPlanDto { Title = "Legs", UserId = "1" },
            [typeof(WorkoutPlanStartRequestDto)] = () => new WorkoutPlanStartRequestDto { WorkoutPlanId = 1, WeekNumber = 1 },
            [typeof(WorkoutPlanRuleAddDto)] = () => new WorkoutPlanRuleAddDto { Name = "Four weeks" },
            [typeof(WorkoutPlanRuleDto)] = () => new WorkoutPlanRuleDto { Name = "Four weeks" },
            [typeof(WorkoutPlanRuleWeekAddDto)] = () => new WorkoutPlanRuleWeekAddDto { WeekNumber = 1 },
            [typeof(WorkoutPlanRuleWeekDto)] = () => new WorkoutPlanRuleWeekDto { WeekNumber = 1 },
        };

        public static IEnumerable<Type> Types
        {
            get { return All.Keys; }
        }

        private static TrainGroupDateAddDto Date()
        {
            return new TrainGroupDateAddDto { TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK, RecurrenceDayOfWeek = DayOfWeek.Monday };
        }

        public static object Of(Type type)
        {
            return All[type]();
        }

        /// <summary>
        /// A valid one with a single property changed.
        /// </summary>
        public static object With(Type type, string property, object? value)
        {
            object dto = Of(type);
            type.GetProperty(property)!.SetValue(dto, value);

            return dto;
        }

        /// <summary>
        /// Every message the validation has for it, as the message is written on the attribute.
        /// </summary>
        public static List<string> Errors(object dto)
        {
            List<ValidationResult> results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);

            return results.Select(x => x.ErrorMessage ?? string.Empty).ToList();
        }
    }
}
