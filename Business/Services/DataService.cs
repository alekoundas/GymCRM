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
        public IGenericRepository<User> Users { get; }
        public IGenericRepository<Mail> Mails { get; }
        public IGenericRepository<TrainGroup> TrainGroups { get; }
        public IGenericRepository<PhoneNumber> PhoneNumbers { get; }


        public IGenericRepository<TrainGroupDate> TrainGroupDates { get; }
        public IGenericRepository<TrainGroupParticipant> TrainGroupParticipants { get; }
        public IGenericRepository<TrainGroupDateCancellationSubscriber> TrainGroupCancellationSubscribers { get; }
        public IGenericRepository<TrainGroupParticipantUnavailableDate> TrainGroupParticipantUnavailableDates { get; }

        // Identity.
        public IGenericRepository<IdentityRole<Guid>> Roles { get; }
        public IGenericRepository<IdentityUserRole<Guid>> UserRoles { get; }
        public IGenericRepository<IdentityRoleClaim<Guid>> RoleClaims { get; }



        public DataService(
            IDbContextFactory<ApiDbContext> dbContextFactory,
            IGenericRepository<User> userRepository,
            IGenericRepository<Mail> mailRepository,
            IGenericRepository<TrainGroup> trainGroupRepository,
            IGenericRepository<PhoneNumber> phoneNumberRepository,
            IGenericRepository<IdentityRole<Guid>> roleRepository,
            IGenericRepository<TrainGroupDate> trainGroupDateRepository,
            IGenericRepository<IdentityUserRole<Guid>> userRoleRepository,
            IGenericRepository<IdentityRoleClaim<Guid>> roleClaimRepository,
            IGenericRepository<TrainGroupParticipant> trainGroupParticipantRepository,
            IGenericRepository<TrainGroupDateCancellationSubscriber> trainGroupCancellationSubscriberRepository,
            IGenericRepository<TrainGroupParticipantUnavailableDate> trainGroupParticipantUnavailableDateRepository)
        {
            _dbContextFactory = dbContextFactory;

            // Repositories.
            Users = userRepository;
            TrainGroups = trainGroupRepository;
            PhoneNumbers = phoneNumberRepository;
            Mails = mailRepository;

            // TrainGroup.
            TrainGroupDates = trainGroupDateRepository;
            TrainGroupParticipants = trainGroupParticipantRepository;
            TrainGroupCancellationSubscribers = trainGroupCancellationSubscriberRepository;
            TrainGroupParticipantUnavailableDates = trainGroupParticipantUnavailableDateRepository;


            // Identity.
            Roles = roleRepository;
            RoleClaims = roleClaimRepository;
            UserRoles = userRoleRepository;
        }

        public IGenericRepository<TEntity> GetGenericRepository<TEntity>() where TEntity : class
        {
            if (typeof(TEntity) == typeof(User))
                return (IGenericRepository<TEntity>)Users;
            if (typeof(TEntity) == typeof(TrainGroup))
                return (IGenericRepository<TEntity>)TrainGroups;
            if (typeof(TEntity) == typeof(PhoneNumber))
                return (IGenericRepository<TEntity>)PhoneNumbers;
            if (typeof(TEntity) == typeof(Mail))
                return (IGenericRepository<TEntity>)Mails;
            if (typeof(TEntity) == typeof(TrainGroupDate))
                return (IGenericRepository<TEntity>)TrainGroupDates;
            if (typeof(TEntity) == typeof(TrainGroupParticipant))
                return (IGenericRepository<TEntity>)TrainGroupParticipants;
            if (typeof(TEntity) == typeof(TrainGroupDateCancellationSubscriber))
                return (IGenericRepository<TEntity>)TrainGroupCancellationSubscribers;
            if (typeof(TEntity) == typeof(TrainGroupParticipantUnavailableDate))
                return (IGenericRepository<TEntity>)TrainGroupParticipantUnavailableDates;
            if (typeof(TEntity) == typeof(IdentityRole<Guid>))
                return (IGenericRepository<TEntity>)Roles;
            if (typeof(TEntity) == typeof(IdentityUserRole<Guid>))
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
