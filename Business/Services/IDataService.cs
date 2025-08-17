using Business.Repository;
using Core.Models;
using DataAccess;

namespace Business.Services
{
    public interface IDataService : IDisposable 
    {
        ApiDbContext Query { get; }
        GenericRepository<TEntity> GetGenericRepository<TEntity>() where TEntity : class;

        GenericRepository<Customer> Customers { get; }
        GenericRepository<ApplicationUser> Users { get; }
        GenericRepository<ContactInformation> ContactInformations { get; }



        Task<int> SaveChangesAsync();
        int SaveChanges();
    }
}
