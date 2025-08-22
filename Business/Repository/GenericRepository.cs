using Core.Enums;
using Core.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Linq;
namespace Business.Repository
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        //protected ApiDbContext _context;
        protected IQueryable<TEntity>? _query;

        public GenericRepository(IDbContextFactory<ApiDbContext> contextFactory)
        {
            //_context = contextFactory.CreateDbContext();
            _contextFactory = contextFactory;
        }

        //public IQueryable<TEntity> Query => _context.Set<TEntity>();




        public GenericRepository<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationProperty)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            _query = _query.Include(navigationProperty);
            return this;
        }

        //TODO: Find a way to combine Select methods.
        public GenericRepository<User> Select<TResult>(Expression<Func<TEntity, User>> selector)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var newQuery = _query.Select(selector);
            return new GenericRepository<User>(_contextFactory)
            {
                //_context = _context,
                _query = newQuery
            };
        }
        public GenericRepository<ContactInformation> Select<TResult>(Expression<Func<TEntity, ContactInformation>> selector)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var newQuery = _query.Select(selector);
            return new GenericRepository<ContactInformation>(_contextFactory)
            {
                //_context = _context,
                _query = newQuery
            };
        }
        public GenericRepository<Customer> Select<TResult>(Expression<Func<TEntity, Customer>> selector)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var newQuery = _query.Select(selector);
            return new GenericRepository<Customer>(_contextFactory)
            {
                //_context = _context,
                _query = newQuery
            };
        }


        //TODO: Find a way to combine Select Many methods.
        public GenericRepository<User> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<User>>> selector)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var newQuery = _query.SelectMany(selector);
            return new GenericRepository<User>(_contextFactory)
            {
                //_context = _context,
                _query = newQuery
            };
        }
        public GenericRepository<ContactInformation> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<ContactInformation>>> selector)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var newQuery = _query.SelectMany(selector);
            return new GenericRepository<ContactInformation>(_contextFactory)
            {
                //_context = _context,
                _query = newQuery
            };
        }
        public GenericRepository<Customer> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<Customer>>> selector)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var newQuery = _query.SelectMany(selector);
            return new GenericRepository<Customer>(_contextFactory)
            {
                //_context = _context,
                _query = newQuery
            };
        }







        public GenericRepository<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            _query = _query.OrderBy(keySelector);

            return this;
        }

        public GenericRepository<TEntity> OrderBy(string propertyName, OrderDirectionEnum orderDirection)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            if (orderDirection == OrderDirectionEnum.ASCENDING)
                _query = _query.OrderByColumn(propertyName);
            else
                _query = _query.OrderByColumnDescending(propertyName);

            return this;
        }

        public GenericRepository<TEntity> ThenBy(string propertyName, OrderDirectionEnum orderDirection)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            if (orderDirection == OrderDirectionEnum.ASCENDING)
                _query = (_query as IOrderedQueryable<TEntity>).ThenByColumn(propertyName);
            else
                _query = (_query as IOrderedQueryable<TEntity>).ThenByColumnDescending(propertyName);

            return this;
        }

        public GenericRepository<TEntity> Contains(string propertyName, string value)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            if (string.IsNullOrEmpty(propertyName) || value == null)
                return this;

            _query = _query.Where($"{propertyName}.Contains(@0)", value);

            return this;
        }


        public GenericRepository<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            _query = _query.Where(predicate);
            return this;
        }

        public GenericRepository<TEntity> AddPagging(int skip = 10, int take = 1)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            if (skip >= 0 && take >= 0)
                _query = _query.Skip(skip).Take(take);

            return this;
        }




        public bool Any(Expression<Func<TEntity, bool>> predicate)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var result = _query.Any(predicate);
            Dispose();
            return result;
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var result = await _query.AnyAsync(predicate);
            Dispose();
            return result;
        }






        public int Count()
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var result = _query.Count();
            Dispose();
            return result;
        }
        public async Task<int> CountAsync()
        {
            if (_query == null)
                _query = _contextFactory.CreateDbContext().Set<TEntity>();

            var result = await _query.CountAsync();
            Dispose();
            return result;
        }





        public int Add(TEntity entity)
        {
            ApiDbContext context = _contextFactory.CreateDbContext();
            context.Set<TEntity>().Add(entity);
            int result = context.SaveChanges();
            context.Dispose();
            Dispose();

            return result;
        }

        public async Task<int> AddAsync(TEntity entity)
        {
            ApiDbContext context = _contextFactory.CreateDbContext();
            await context.AddAsync(entity);
            int result = await context.SaveChangesAsync();
            context.Dispose();
            Dispose();

            return result;
        }


        public int AddRange(IEnumerable<TEntity> entities)
        {
            ApiDbContext context = _contextFactory.CreateDbContext();
            context.Set<TEntity>().AddRange(entities);
            int result = context.SaveChanges();
            context.Dispose();
            Dispose();

            return result;
        }

        public async Task<int> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            ApiDbContext context = _contextFactory.CreateDbContext();
            await context.Set<TEntity>().AddRangeAsync(entities);
            int result = await context.SaveChangesAsync();
            context.Dispose();
            Dispose();

            return result;
        }

        public int Remove(TEntity entity)
        {
            ApiDbContext context = _contextFactory.CreateDbContext();
            context.Set<TEntity>().Remove(entity);
            int result = context.SaveChanges();
            context.Dispose();
            Dispose();

            return result;
        }
        public async Task<int> RemoveAsync(TEntity entity)
        {
            ApiDbContext context = _contextFactory.CreateDbContext();
            context.Set<TEntity>().Remove(entity);
            int result = await context.SaveChangesAsync();
            context.Dispose();
            Dispose();

            return result;
        }

        public int RemoveRange(IEnumerable<TEntity> entities)
        {
            ApiDbContext context = _contextFactory.CreateDbContext();
            context.Set<TEntity>().RemoveRange(entities);
            int result = context.SaveChanges();
            context.Dispose();
            Dispose();

            return result;
        }

        public async Task<int> RemoveRangeAsync(IEnumerable<TEntity> entities)
        {
            ApiDbContext context = _contextFactory.CreateDbContext();
            context.Set<TEntity>().RemoveRange(entities);
            int result = await context.SaveChangesAsync();
            context.Dispose();
            Dispose();

            return result;
        }


        public TEntity? Find(int id)
        {
            TEntity? result = _contextFactory.CreateDbContext().Set<TEntity>().Find(id);

            Dispose();
            return result;
        }
        public async Task<TEntity?> FindAsync(int id)
        {
            TEntity? result = await _contextFactory.CreateDbContext().Set<TEntity>().FindAsync(id);

            Dispose();
            return result;
        }



        public async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> filter)
        {
            TEntity? result = null;

            if (_query == null)
                result = await _contextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().FirstAsync(filter);
            else
                result = await _query.AsNoTracking().FirstAsync(filter);

            Dispose();
            return result;
        }


        public TEntity? FirstOrDefault(Expression<Func<TEntity, bool>>? filter = null)
        {
            TEntity? result = null;
            if (filter != null)
            {
                if (_query == null)
                    result = _contextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().FirstOrDefault(filter);
                else
                    result = _query.AsNoTracking().FirstOrDefault(filter);
            }
            else
            {
                if (_query == null)
                    result = _contextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().FirstOrDefault();
                else
                    result = _query.AsNoTracking().FirstOrDefault();
            }

            Dispose();
            return result;
        }

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? filter = null)
        {
            TEntity? result = null;
            if (filter != null)
            {
                if (_query == null)
                    result = await _contextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(filter);
                else
                    result = await _query.AsNoTracking().FirstOrDefaultAsync(filter);
            }
            else
            {
                if (_query == null)
                    result = await _contextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().FirstOrDefaultAsync();
                else
                    result = await _query.AsNoTracking().FirstOrDefaultAsync();
            }
            Dispose();
            return result;
        }


        //public async Task<List<TEntity>> GetFiltered(Expression<Func<TEntity, bool>> filter)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    return await context.Set<TEntity>().Where(filter).ToListAsync();
        //}

        //public IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> expression)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    return context.Set<TEntity>().Where(expression).ToList();
        //}


        public List<TEntity> ToList()
        {
            List<TEntity>? result = null;

            if (_query == null)
                result = _contextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().ToList();
            else
                result = _query.AsNoTracking().ToList();


            Dispose();
            return result;
        }

        public async Task<List<TEntity>> ToListAsync()
        {
            List<TEntity>? result = null;

            if (_query == null)
                result = await _contextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().ToListAsync();
            else
                result = await _query.AsNoTracking().ToListAsync();


            Dispose();
            return result;
        }


        public void Dispose()
        {
            //_context = null;
            _query = null;
        }
    }
}
