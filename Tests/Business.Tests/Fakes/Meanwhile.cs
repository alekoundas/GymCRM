using System.Data.Common;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tests.Shared;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// A test's database where something else happens just before the first update is written:
    /// what another request does between a read and the write that follows it.
    /// </summary>
    internal sealed class Meanwhile : DbCommandInterceptor, IDbContextFactory<ApiDbContext>
    {
        private readonly TestDatabase _database;
        private Func<Task>? _happens;

        public Meanwhile(TestDatabase database, Func<Task> happens)
        {
            _database = database;
            _happens = happens;
        }

        public bool HasHappened
        {
            get { return _happens == null; }
        }

        public ApiDbContext CreateDbContext()
        {
            DbContextOptions<ApiDbContext> options = new DbContextOptionsBuilder<ApiDbContext>().AddInterceptors(this).Options;

            return new ApiDbContext(options, _database.Configuration);
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (_happens != null && command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal))
            {
                Func<Task> happens = _happens;
                _happens = null;

                await happens();
            }

            return result;
        }
    }
}
