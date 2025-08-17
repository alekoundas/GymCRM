using Business.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using DataAccess;

namespace Business.Repository
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly ApiDbContext Context;
        protected readonly DbSet<TEntity> _set;

        public GenericRepository(ApiDbContext context)
        {
            Context = context;
            _set = Context.Set<TEntity>();
        }

        public IQueryable<TEntity> Query => Context.Set<TEntity>();


        public bool Any(Expression<Func<TEntity, bool>>? predicate)
        {
            if (predicate != null)
                return _set.Any(predicate);

            return _set.Any();
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate)
        {
               if (predicate != null)
                return await _set.AnyAsync(predicate);

            return await _set.AnyAsync();
        }

        public IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> expression)
        {
            return _set.Where(expression);
        }

        public async Task<int> CountAsync()
        {
            return await _set.CountAsync();
        }

        public async Task<int> CountAsyncFiltered(List<Expression<Func<TEntity, bool>>> filters)
        {
            var qry = (IQueryable<TEntity>)_set;

            if (filters != null)
                foreach (var filter in filters)
                    qry = qry.Where(filter);

            return await _set.CountAsync();
        }

        // CRUD operations.

        public void Add(TEntity entity)
        {
            _set.Add(entity);
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            _set.AddRange(entities);
        }

        public void Update(TEntity model)
        {
            if (model != null)
                Context.Entry(model).State = EntityState.Modified;
        }

        public void UpdateRange(List<TEntity> models)
        {
            foreach (var model in models)
                if (model != null)
                    Context.Entry(model).State = EntityState.Modified;
        }

        public void Remove(TEntity entity)
        {
            _set.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            _set.RemoveRange(entities);
        }



        // First.
        public async Task<TEntity?> FindByIdAsync(int id)
        {
            return await _set.FindAsync(id);
        }

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _set.FirstOrDefaultAsync(predicate);
        }

        public async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> filter,
            List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null)
        {
            var qry = (IQueryable<TEntity>)_set;

            if (includes != null)
                foreach (var include in includes)
                    qry = include(qry);

            return await qry.FirstOrDefaultAsync(filter);
        }



        // Select. 
        public void Select(Func<TEntity, TEntity> predicate)
        {
            _set.Select(predicate);
        }

        public void Select(Expression<Func<TEntity, bool>> predicate)
        {
            _set.Select(predicate);
        }

        public async Task<List<TResult>> SelectAllAsync<TResult>(Expression<Func<TEntity, TResult>> selector)
        {
            return await _set.Select(selector).ToListAsync();
        }

        public async Task<List<TResult>> SelectAllAsyncFiltered<TResult>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TResult>> selector)
        {
            return await _set.Where(predicate).Select(selector).ToListAsync();
        }



        // Filtered list.
        public async Task<List<TEntity>> GetPaggingWithFilterAndSort(
            List<Expression<Func<TEntity, bool>>>? filters,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderingInfo,
            List<Func<IOrderedQueryable<TEntity>, IOrderedQueryable<TEntity>>>? thenOrderingInfos,
            List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null,
            int pageSize = 10,
            int pageIndex = 1)
        {
            var qry = (IQueryable<TEntity>)_set;

            if (includes != null)
                foreach (var include in includes)
                    qry = include(qry);

            if (filters != null)
                foreach (var filter in filters)
                    qry = qry.Where(filter);

            if (orderingInfo != null)
                qry = orderingInfo(qry);

            if (thenOrderingInfos != null)
                foreach (var thenOrderingInfo in thenOrderingInfos)
                    qry = thenOrderingInfo((IOrderedQueryable<TEntity>)qry);

            if (pageSize != -1 && pageSize != 0)
                qry = qry.Skip((pageIndex - 1) * pageSize).Take(pageSize);

            return await qry.ToListAsync();
        }

        public IQueryable<TEntity> GetLookup(
            List<Expression<Func<TEntity, bool>>>? filters,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderingInfo,
            int skip = 10,
            int take = 1)
        {
            var qry = (IQueryable<TEntity>)_set;


            if (filters != null)
                foreach (var filter in filters)
                    qry = qry.Where(filter);

            if (orderingInfo != null)
                qry = orderingInfo(qry);

            if (skip >= 0 && take >= 0)
                qry = qry.Skip(skip).Take(take);

            return qry;
        }

        public async Task<List<TEntity>> GetFiltered(Expression<Func<TEntity, bool>> filter)
        {
            return await _set.Where(filter).ToListAsync();
        }

        // Save.
        public async Task<int> SaveChangesAsync()
        {
            return await Context.SaveChangesAsync();
        }
        public int SaveChanges()
        {
            return Context.SaveChanges();
        }


        //// Dispose.
        //private bool disposed = false;

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!this.disposed)
        //        if (disposing)
        //            Context.Dispose();

        //    this.disposed = true;
        //}

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

    }
}
