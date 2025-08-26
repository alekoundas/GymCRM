using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Business.Repository
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        GenericRepository<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationProperty);

        GenericRepository<User> Select<TResult>(Expression<Func<TEntity, User>> selector);
        GenericRepository<ContactInformation> Select<TResult>(Expression<Func<TEntity, ContactInformation>> selector);
        GenericRepository<Customer> Select<TResult>(Expression<Func<TEntity, Customer>> selector);

        GenericRepository<User> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<User>>> selector);
        GenericRepository<ContactInformation> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<ContactInformation>>> selector);
        GenericRepository<Customer> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<Customer>>> selector);


        GenericRepository<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);
        GenericRepository<TEntity> OrderBy(string propertyName, OrderDirectionEnum orderDirection);
        GenericRepository<TEntity> ThenBy(string propertyName, OrderDirectionEnum orderDirection);

      
        GenericRepository<TEntity> Contains(string propertyName, string value);
        GenericRepository<TEntity> FilterByColumn(string columnPath, object value);
        GenericRepository<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
        GenericRepository<TEntity> AddPagging(int skip = 10, int take = 1);



        bool Any(Expression<Func<TEntity, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

        int Count();
        Task<int> CountAsync();



        int Add(TEntity entity);
        Task<int> AddAsync(TEntity entity);
        int AddRange(IEnumerable<TEntity> entities);
        Task<int> AddRangeAsync(IEnumerable<TEntity> entities);


        int Remove(TEntity entity);
        Task<int> RemoveAsync(TEntity entity);

        int RemoveRange(IEnumerable<TEntity> entities);
        Task<int> RemoveRangeAsync(IEnumerable<TEntity> entities);


        TEntity? Find(int id);
        Task<TEntity?> FindAsync(int id);

        Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> filter);
        TEntity? FirstOrDefault(Expression<Func<TEntity, bool>>? predicate);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate);

        List<TEntity> ToList();
        Task<List<TEntity>> ToListAsync();
    }
}
