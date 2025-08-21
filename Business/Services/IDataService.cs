using Business.Repository;
using Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Business.Services
{
    public interface IDataService
    {

        IGenericRepository<Customer> Customers { get; }
        IGenericRepository<User> Users { get; }
        IGenericRepository<ContactInformation> ContactInformations { get; }
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
