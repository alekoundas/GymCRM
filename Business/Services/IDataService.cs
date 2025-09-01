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
        IGenericRepository<TrainGroup> TrainGroups { get; }
        IGenericRepository<TrainGroupDate> TrainGroupDates { get; }
        IGenericRepository<ContactInformation> ContactInformations { get; }
        IGenericRepository<TrainGroupParticipant> TrainGroupParticipants { get; }
        IGenericRepository<TrainGroupDateCancellationSubscriber> TrainGroupCancellationSubscribers { get; }

        // Identity.
        IGenericRepository<IdentityRole<Guid>> Roles { get; }
        IGenericRepository<IdentityUserRole<Guid>> UserRoles { get; }
        IGenericRepository<IdentityRoleClaim<Guid>> RoleClaims { get; }
        IGenericRepository<TEntity> GetGenericRepository<TEntity>() where TEntity : class;


        void Update<TEntity>(TEntity model);
        void UpdateRange<TEntity>(List<TEntity> models);
        Task UpdateRangeAsync<TEntity>(List<TEntity> models);
        Task UpdateAsync<TEntity>(TEntity model);
    }
}
