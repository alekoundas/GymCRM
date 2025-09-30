using Business.Repository;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Identity;

namespace Business.Services
{
    public interface IDataService
    {
        // Create a new database context.
        ApiDbContext GetDbContext();

        // Repositories.
        IGenericRepository<User> Users { get; }
        IGenericRepository<Mail> Mails { get; }
        IGenericRepository<TrainGroup> TrainGroups { get; }
        IGenericRepository<PhoneNumber> PhoneNumbers { get; }
        IGenericRepository<TrainGroupDate> TrainGroupDates { get; }
        IGenericRepository<TrainGroupParticipant> TrainGroupParticipants { get; }
        //IGenericRepository<TrainGroupDateCancellationSubscriber> TrainGroupCancellationSubscribers { get; }

        // Identity.
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<UserRole> UserRoles { get; }
        IGenericRepository<IdentityRoleClaim<Guid>> RoleClaims { get; }
        IGenericRepository<TEntity> GetGenericRepository<TEntity>() where TEntity : class;


        void Update<TEntity>(TEntity model);
        void UpdateRange<TEntity>(List<TEntity> models);
        Task UpdateRangeAsync<TEntity>(List<TEntity> models);
        Task UpdateAsync<TEntity>(TEntity model);
    }
}
