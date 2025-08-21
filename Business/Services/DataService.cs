using Business.Repository;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Business.Services
{
    public class DataService : IDataService
    {
        private IDbContextFactory<ApiDbContext> _dbContextFactory;


        public IGenericRepository<Customer> Customers { get; }
        public IGenericRepository<User> Users { get; }
        public IGenericRepository<ContactInformation> ContactInformations { get; }
        public IGenericRepository<IdentityRole<Guid>> Roles { get; }
        public IGenericRepository<IdentityUserRole<Guid>> UserRoles { get; }
        public IGenericRepository<IdentityRoleClaim<Guid>> RoleClaims { get; }


        public DataService(
            IDbContextFactory<ApiDbContext> dbContextFactory,
            IGenericRepository<Customer> customerRepository,
            IGenericRepository<User> userRepository,
            IGenericRepository<ContactInformation> contactInformationRepository,
            IGenericRepository<IdentityRole<Guid>> roleRepository,
            IGenericRepository<IdentityRoleClaim<Guid>> roleClaimRepository,
            IGenericRepository<IdentityUserRole<Guid>> userRoleRepository)
        {
            _dbContextFactory = dbContextFactory;
            Customers = customerRepository;
            Users = userRepository;
            ContactInformations = contactInformationRepository;
            Roles = roleRepository;
            RoleClaims = roleClaimRepository;
            UserRoles = userRoleRepository;
        }

        public IGenericRepository<TEntity> GetGenericRepository<TEntity>() where TEntity : class
        {
            if (typeof(TEntity) == typeof(Customer))
                return (IGenericRepository<TEntity>)Customers;
            if (typeof(TEntity) == typeof(User))
                return (IGenericRepository<TEntity>)Users;
            if (typeof(TEntity) == typeof(ContactInformation))
                return (IGenericRepository<TEntity>)ContactInformations;
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
