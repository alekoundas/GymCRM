using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Business.Repository
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> Query { get; }
        
        bool Any(Expression<Func<TEntity, bool>>? predicate);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate);
        IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> expression);
        Task<int> CountAsync();
        Task<int> CountAsyncFiltered(List<Expression<Func<TEntity, bool>>> filters);

        // CRUD operations.
        void Add(TEntity entity);
        void AddRange(IEnumerable<TEntity> entities);
        void Update(TEntity model);
        void UpdateRange(List<TEntity> models);
        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entities);

        // First.
        Task<TEntity?> FindByIdAsync(int id);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> filter, List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null);

        // Select.
        void Select(Func<TEntity, TEntity> predicate);
        void Select(Expression<Func<TEntity, bool>> predicate);
        Task<List<TResult>> SelectAllAsync<TResult>(Expression<Func<TEntity, TResult>> selector);
        Task<List<TResult>> SelectAllAsyncFiltered<TResult>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TResult>> selector);

        // Filtered list.
        Task<List<TEntity>> GetFiltered(Expression<Func<TEntity, bool>> filter);
        Task<List<TEntity>> GetPaggingWithFilterAndSort(
            List<Expression<Func<TEntity, bool>>>? filters,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderingInfo,
            List<Func<IOrderedQueryable<TEntity>, IOrderedQueryable<TEntity>>>? thenOrderingInfos,
            List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null,
            int pageSize = 10,
            int pageIndex = 1);

        IQueryable<TEntity> GetLookup(
                    List<Expression<Func<TEntity, bool>>>? filters,
                    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderingInfo,
                    int skip = 10,
                    int take = 1);
    }
}
