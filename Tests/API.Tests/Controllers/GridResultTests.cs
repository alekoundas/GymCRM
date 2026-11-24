using System.Collections;
using System.Reflection;
using API.Controllers;
using API.Tests.Host;
using Core.Models;

namespace API.Tests.Controllers
{
    /// <summary>
    /// What a grid adds to its rows goes by their place: the first row is of the first entity. There is
    /// always one of each for every one, and the code does not rely on it.
    /// </summary>
    public sealed class GridResultTests : ApiTest
    {
        private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;

        public static readonly TheoryData<Type> Grids =
        [
            typeof(ExerciseHistoriesController),
            typeof(ExercisesController),
            typeof(TrainGroupParticipantsController),
            typeof(TrainGroupsController),
            typeof(WorkoutPlanRecordingsController),
            typeof(WorkoutPlanRulesController),
            typeof(WorkoutPlansController)
        ];

        public GridResultTests(ApiFixture api) : base(api)
        {
        }

        [Theory]
        [MemberData(nameof(Grids))]
        public async Task They_are_gone_through_as_far_as_both_go(Type type)
        {
            MethodInfo update = type.GetMethod("DataTableResultUpdate", Hidden)!;
            Type entityType = update.GetParameters()[0].ParameterType.GetGenericArguments()[0];
            Type rowType = update.GetParameters()[1].ParameterType.GetGenericArguments()[0];

            foreach ((int entities, int rows) in new[] { (0, 0), (0, 2), (2, 0), (1, 2), (2, 1), (2, 2) })
            {
                object controller = NewController(type);

                // For a day, so that whether they came is looked at too.
                type.GetField("_selectedDate", Hidden)?.SetValue(controller, new DateTime(2031, 3, 5));

                await Should.NotThrowAsync(() => (Task)update.Invoke(controller, [ListOf(entityType, entities), ListOf(rowType, rows)])!, $"{entities} {rows}");
            }
        }

        private object NewController(Type type)
        {
            MethodInfo make = typeof(Direct).GetMethod(nameof(Direct.Controller))!.MakeGenericMethod(type);

            return make.Invoke(null, [Services, type.Name, Array.Empty<string>(), null, Array.Empty<object>()])!;
        }

        private static IList ListOf(Type type, int count)
        {
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type))!;

            for (int i = 0; i < count; i++)
                list.Add(New(type));

            return list;
        }

        // With what the grid always reads along with it.
        private static object New(Type type)
        {
            if (type == typeof(Exercise))
                return new Exercise { WorkoutPlan = new WorkoutPlan() };
            if (type == typeof(ExerciseHistory))
                return new ExerciseHistory { Exercise = new Exercise { WorkoutPlan = new WorkoutPlan() } };
            if (type == typeof(TrainGroup))
                return new TrainGroup { Trainer = new User() };

            return Activator.CreateInstance(type)!;
        }
    }
}
