using Business.Repository;
using Business.Services;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// The real data service on a test's database, put together the way the container does it.
    /// </summary>
    internal static class DataServices
    {
        public static DataService On(TestDatabase database, IDbContextFactory<ApiDbContext>? contexts = null)
        {
            return new DataService(
                contexts ?? database.Factory,
                new GenericRepository<Mail>(database.Factory),
                new GenericRepository<Exercise>(database.Factory),
                new GenericRepository<TrainGroup>(database.Factory),
                new GenericRepository<UserStatus>(database.Factory),
                new GenericRepository<TrainGroupCategory>(database.Factory),
                new GenericRepository<WorkoutPlan>(database.Factory),
                new GenericRepository<WorkoutPlanRule>(database.Factory),
                new GenericRepository<WorkoutPlanRuleWeek>(database.Factory),
                new GenericRepository<WorkoutPlanRecording>(database.Factory),
                new GenericRepository<Subscription>(database.Factory),
                new GenericRepository<PhoneNumber>(database.Factory),
                new GenericRepository<TrainGroupDate>(database.Factory),
                new GenericRepository<ExerciseHistory>(database.Factory),
                new GenericRepository<TrainGroupΑttendance>(database.Factory),
                new GenericRepository<TrainGroupParticipant>(database.Factory),
                new GenericRepository<TrainGroupUnavailableDate>(database.Factory),
                new GenericRepository<TrainGroupParticipantUnavailableDate>(database.Factory),
                new GenericRepository<User>(database.Factory),
                new GenericRepository<Role>(database.Factory),
                new GenericRepository<UserRole>(database.Factory),
                new GenericRepository<IdentityRoleClaim<Guid>>(database.Factory),
                new GenericRepository<GoogleRefreshToken>(database.Factory));
        }
    }
}
