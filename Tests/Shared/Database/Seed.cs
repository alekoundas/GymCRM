using Core.Enums;
using Core.Models;

namespace Tests.Shared
{
    /// <summary>
    /// Rows for a test, written one by one: each call adds a row that could really be there and
    /// hands it back with its id. Anything a test is about is set in <c>change</c>.
    /// </summary>
    public sealed class Seed
    {
        private readonly TestDatabase _database;
        private int _count;

        public Seed(TestDatabase database)
        {
            _database = database;
        }


        // People.

        public User User(Action<User>? change = null)
        {
            int number = ++_count;

            User user = new User
            {
                Id = Guid.NewGuid(),
                UserName = $"user{number}",
                Email = $"user{number}@gym.test",
                FirstName = $"First{number}",
                LastName = $"Last{number}",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            change?.Invoke(user);

            user.NormalizedUserName = user.UserName?.ToUpperInvariant();
            user.NormalizedEmail = user.Email.ToUpperInvariant();

            return _database.Add(user);
        }

        public UserStatus UserStatus(Action<UserStatus>? change = null)
        {
            UserStatus status = new UserStatus { Name = $"Status {++_count}", Color = "#112233" };
            change?.Invoke(status);

            return _database.Add(status);
        }

        public Role Role(string name)
        {
            return _database.Add(new Role { Id = Guid.NewGuid(), Name = name, NormalizedName = name.ToUpperInvariant(), ConcurrencyStamp = Guid.NewGuid().ToString() });
        }

        public UserRole UserRole(User user, Role role)
        {
            return _database.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        }

        public PhoneNumber PhoneNumber(User user, string number = "2101234567", bool isPrimary = false)
        {
            return _database.Add(new PhoneNumber { UserId = user.Id, Number = number, IsPrimary = isPrimary });
        }

        public Mail Mail(User user, Action<Mail>? change = null)
        {
            Mail mail = new Mail { UserId = user.Id, Subject = $"Subject {++_count}", Body = "Body" };
            change?.Invoke(mail);

            return _database.Add(mail);
        }

        public Subscription Subscription(User user, int amount, SubscriptionStatusEnum status = SubscriptionStatusEnum.APPROVED, Action<Subscription>? change = null)
        {
            Subscription subscription = new Subscription { UserId = user.Id, Amount = amount, Status = status };
            change?.Invoke(subscription);

            return _database.Add(subscription);
        }


        // Train groups.

        public TrainGroupCategory Category(string name, string color = "")
        {
            return _database.Add(new TrainGroupCategory { Name = name, Color = color });
        }

        public TrainGroup Group(User trainer, Action<TrainGroup>? change = null)
        {
            TrainGroup group = new TrainGroup
            {
                Title = $"Group {++_count}",
                Description = "Description",
                TrainerId = trainer.Id,
                MaxParticipants = 10,
                StartOn = new DateTime(2025, 1, 1, 18, 0, 0),
                Duration = new DateTime(2000, 1, 1, 1, 0, 0)
            };
            change?.Invoke(group);

            return _database.Add(group);
        }

        public TrainGroupDate Weekly(TrainGroup group, DayOfWeek day)
        {
            return _database.Add(new TrainGroupDate { TrainGroupId = group.Id, TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK, RecurrenceDayOfWeek = day });
        }

        public TrainGroupDate Monthly(TrainGroup group, int day)
        {
            return _database.Add(new TrainGroupDate { TrainGroupId = group.Id, TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_MONTH, RecurrenceDayOfMonth = day });
        }

        public TrainGroupDate Fixed(TrainGroup group, DateTime? day)
        {
            return _database.Add(new TrainGroupDate { TrainGroupId = group.Id, TrainGroupDateType = TrainGroupDateTypeEnum.FIXED_DAY, FixedDay = day });
        }

        public TrainGroupParticipant OneOff(TrainGroupDate date, User user, DateTime day, Action<TrainGroupParticipant>? change = null)
        {
            return Participant(date, user, x =>
            {
                x.SelectedDate = day;
                change?.Invoke(x);
            });
        }

        public TrainGroupParticipant Recurring(TrainGroupDate date, User user, DateTime? from, DateTime? end = null, Action<TrainGroupParticipant>? change = null)
        {
            return Participant(date, user, x =>
            {
                x.RecurringStartOnDate = from;
                x.RecurringEndOnDate = end;
                change?.Invoke(x);
            });
        }

        public TrainGroupParticipant Participant(TrainGroupDate date, User user, Action<TrainGroupParticipant>? change = null)
        {
            TrainGroupParticipant participant = new TrainGroupParticipant { TrainGroupId = date.TrainGroupId, TrainGroupDateId = date.Id, UserId = user.Id };
            change?.Invoke(participant);

            return _database.Add(participant);
        }

        public TrainGroupParticipantUnavailableDate Skip(TrainGroupParticipant participant, DateTime day)
        {
            return _database.Add(new TrainGroupParticipantUnavailableDate { TrainGroupParticipantId = participant.Id, UnavailableDate = day });
        }

        public TrainGroupUnavailableDate Cancelled(TrainGroup group, DateTime day)
        {
            return _database.Add(new TrainGroupUnavailableDate { TrainGroupId = group.Id, UnavailableDate = day });
        }

        public TrainGroupΑttendance Attendance(User user, DateTime day, TrainGroup? group = null, Action<TrainGroupΑttendance>? change = null)
        {
            TrainGroupΑttendance attendance = new TrainGroupΑttendance
            {
                UserId = user.Id,
                AttendanceDate = day,
                TrainGroupId = group?.Id,
                TrainGroupTitle = group?.Title ?? "By hand",
                TrainerId = group?.TrainerId
            };
            change?.Invoke(attendance);

            return _database.Add(attendance);
        }


        // Workout plans.

        public WorkoutPlanRule Rule(string name, params int[] maxRecordings)
        {
            WorkoutPlanRule rule = new WorkoutPlanRule { Name = name };

            for (int i = 0; i < maxRecordings.Length; i++)
                rule.Weeks.Add(new WorkoutPlanRuleWeek { WeekNumber = i + 1, MaxRecordings = maxRecordings[i], Message = $"Message {i + 1}" });

            return _database.Add(rule);
        }

        public WorkoutPlan Plan(User user, WorkoutPlanRule? rule = null, int? currentWeek = null, Action<WorkoutPlan>? change = null)
        {
            WorkoutPlan plan = new WorkoutPlan { Title = $"Plan {++_count}", UserId = user.Id, WorkoutPlanRuleId = rule?.Id, CurrentWeek = currentWeek };
            change?.Invoke(plan);

            return _database.Add(plan);
        }

        public WorkoutPlanRecording Recording(WorkoutPlan plan, DateTime startedOn, int weekNumber = 1, DateTime? completedOn = null, Action<WorkoutPlanRecording>? change = null)
        {
            WorkoutPlanRecording recording = new WorkoutPlanRecording
            {
                WorkoutPlanId = plan.Id,
                WorkoutPlanTitle = plan.Title,
                UserId = plan.UserId,
                StartedOn = startedOn,
                CompletedOn = completedOn,
                DurationSeconds = completedOn == null ? null : (int)(completedOn.Value - startedOn).TotalSeconds,
                WeekNumber = weekNumber
            };
            change?.Invoke(recording);

            return _database.Add(recording);
        }

        public Exercise Exercise(WorkoutPlan plan, Action<Exercise>? change = null)
        {
            Exercise exercise = new Exercise { WorkoutPlanId = plan.Id, Name = $"Exercise {++_count}", Sets = "3", Reps = "10", Weight = "50", GroupNumber = 1, GroupExerciseOrderNumber = 1 };
            change?.Invoke(exercise);

            return _database.Add(exercise);
        }

        public ExerciseHistory History(Exercise exercise, Action<ExerciseHistory>? change = null)
        {
            ExerciseHistory history = new ExerciseHistory { ExerciseId = exercise.Id, Name = exercise.Name, Sets = exercise.Sets, Reps = exercise.Reps, Weight = exercise.Weight };
            change?.Invoke(history);

            return _database.Add(history);
        }


        // The questionnaire.

        public Questionnaire Questionnaire(params string[] titles)
        {
            Questionnaire questionnaire = new Questionnaire { Name = "Health" };

            for (int i = 0; i < titles.Length; i++)
                questionnaire.Questions.Add(new QuestionnaireQuestion { OrderNumber = i + 1, Title = titles[i] });

            return _database.Add(questionnaire);
        }

        public QuestionnaireAnswer Answer(QuestionnaireQuestion question, User user, string answer)
        {
            return _database.Add(new QuestionnaireAnswer { QuestionnaireQuestionId = question.Id, UserId = user.Id, Answer = answer });
        }
    }
}
