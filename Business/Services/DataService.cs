using Business.Repository;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services
{
    public class DataService : IDataService
    {
        private IDbContextFactory<ApiDbContext> _dbContextFactory;


        // Repositories.
        public IGenericRepository<Mail> Mails { get; }
        public IGenericRepository<Exercise> Exercises { get; }
        public IGenericRepository<TrainGroup> TrainGroups { get; }
        public IGenericRepository<UserStatus> UserStatuses { get; }
        public IGenericRepository<WorkoutPlan> WorkoutPlans { get; }
        public IGenericRepository<PhoneNumber> PhoneNumbers { get; }
        public IGenericRepository<TrainGroupDate> TrainGroupDates { get; }
        public IGenericRepository<TrainGroupParticipant> TrainGroupParticipants { get; }
        public IGenericRepository<TrainGroupUnavailableDate> TrainGroupUnavailableDates { get; }
        public IGenericRepository<TrainGroupParticipantUnavailableDate> TrainGroupParticipantUnavailableDates { get; }

        // Identity.
        public IGenericRepository<User> Users { get; }
        public IGenericRepository<Role> Roles { get; }
        public IGenericRepository<UserRole> UserRoles { get; }
        public IGenericRepository<IdentityRoleClaim<Guid>> RoleClaims { get; }



        public DataService(
            IDbContextFactory<ApiDbContext> dbContextFactory,
            IGenericRepository<Mail> mailRepository,
            IGenericRepository<Exercise> exerciseRepository,
            IGenericRepository<TrainGroup> trainGroupRepository,
            IGenericRepository<UserStatus> userStatusesRepository,
            IGenericRepository<WorkoutPlan> workoutPlanRepository,
            IGenericRepository<PhoneNumber> phoneNumberRepository,
            IGenericRepository<TrainGroupDate> trainGroupDateRepository,
            IGenericRepository<TrainGroupParticipant> trainGroupParticipantRepository,
            IGenericRepository<TrainGroupUnavailableDate> trainGroupUnavailableDateRepository,
            IGenericRepository<TrainGroupParticipantUnavailableDate> trainGroupParticipantUnavailableDateRepository,
            IGenericRepository<User> userRepository,
            IGenericRepository<Role> roleRepository,
            IGenericRepository<UserRole> userRoleRepository,
            IGenericRepository<IdentityRoleClaim<Guid>> roleClaimRepository)
        {
            _dbContextFactory = dbContextFactory;

            // Repositories.
            Mails = mailRepository;
            Exercises = exerciseRepository;
            TrainGroups = trainGroupRepository;
            PhoneNumbers = phoneNumberRepository;
            WorkoutPlans = workoutPlanRepository;
            UserStatuses = userStatusesRepository;  

            // TrainGroup.
            TrainGroupDates = trainGroupDateRepository;
            TrainGroupParticipants = trainGroupParticipantRepository;
            TrainGroupParticipantUnavailableDates = trainGroupParticipantUnavailableDateRepository;
            TrainGroupUnavailableDates = trainGroupUnavailableDateRepository;

            // Identity.
            Users = userRepository;
            Roles = roleRepository;
            RoleClaims = roleClaimRepository;
            UserRoles = userRoleRepository;
        }

        public IGenericRepository<TEntity> GetGenericRepository<TEntity>() where TEntity : class
        {
            if (typeof(TEntity) == typeof(Mail))
                return (IGenericRepository<TEntity>)Mails;
            if (typeof(TEntity) == typeof(Exercise))
                return (IGenericRepository<TEntity>)Exercises;
            if (typeof(TEntity) == typeof(TrainGroup))
                return (IGenericRepository<TEntity>)TrainGroups;
            if (typeof(TEntity) == typeof(UserStatus))
                return (IGenericRepository<TEntity>)UserStatuses;
            if (typeof(TEntity) == typeof(WorkoutPlan))
                return (IGenericRepository<TEntity>)WorkoutPlans;
            if (typeof(TEntity) == typeof(PhoneNumber))
                return (IGenericRepository<TEntity>)PhoneNumbers;
            if (typeof(TEntity) == typeof(TrainGroupDate))
                return (IGenericRepository<TEntity>)TrainGroupDates;
            if (typeof(TEntity) == typeof(TrainGroupParticipant))
                return (IGenericRepository<TEntity>)TrainGroupParticipants;
            if (typeof(TEntity) == typeof(TrainGroupUnavailableDate))
                return (IGenericRepository<TEntity>)TrainGroupUnavailableDates;
            if (typeof(TEntity) == typeof(TrainGroupParticipantUnavailableDate))
                return (IGenericRepository<TEntity>)TrainGroupParticipantUnavailableDates;
            if (typeof(TEntity) == typeof(User))
                return (IGenericRepository<TEntity>)Users;
            if (typeof(TEntity) == typeof(Role))
                return (IGenericRepository<TEntity>)Roles;
            if (typeof(TEntity) == typeof(UserRole))
                return (IGenericRepository<TEntity>)UserRoles;
            if (typeof(TEntity) == typeof(IdentityRoleClaim<Guid>))
                return (IGenericRepository<TEntity>)RoleClaims;

            throw new InvalidOperationException($"No repository found for type {typeof(TEntity).Name}");
        }


        //public GenericRepository<TEntity> GetGenericRepository<TEntity>() where TEntity : class
        //{
        //    return new GenericRepository<TEntity>(_dbContextFactory);
        //}
        public ApiDbContext GetDbContext()
        {
            return _dbContextFactory.CreateDbContext();
        }

        public void Update<TEntity>(TEntity model)
        {
            if (model == null)
                return;

            ApiDbContext _dbContext = _dbContextFactory.CreateDbContext();

            _dbContext.Entry(model).State = EntityState.Modified;
            _dbContext.SaveChanges();
            _dbContext.Dispose();
        }

        public async Task UpdateAsync<TEntity>(TEntity model)
        {
            if (model == null)
                return;

            ApiDbContext _dbContext = _dbContextFactory.CreateDbContext();

            _dbContext.Entry(model).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            await _dbContext.DisposeAsync();
        }


        public void UpdateRange<TEntity>(List<TEntity> models)
        {
            ApiDbContext _dbContext = _dbContextFactory.CreateDbContext();

            foreach (var model in models)
                if (model != null)
                    _dbContext.Entry(model).State = EntityState.Modified;

            _dbContext.SaveChanges();
            _dbContext.Dispose();
        }

        public async Task UpdateRangeAsync<TEntity>(List<TEntity> models)
        {
            ApiDbContext _dbContext = _dbContextFactory.CreateDbContext();

            foreach (var model in models)
                if (model != null)
                    _dbContext.Entry(model).State = EntityState.Modified;

            await _dbContext.SaveChangesAsync();
            await _dbContext.DisposeAsync();
        }
    }
}
