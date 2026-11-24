using DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Tests.Shared
{
    /// <summary>
    /// A real SQLite database in memory, one per test, built by the real migrations.
    /// The database lives as long as a connection to it is open, so one is held.
    /// NO DISK FILE
    ///
    /// The migrations run once per process, into a template every database is then a copy of:
    /// twenty-two of them for every test would be most of the time a run takes.
    /// </summary>
    public sealed class TestDatabase : IDisposable
    {
        private static readonly Lazy<SqliteConnection> Template = new Lazy<SqliteConnection>(BuildTemplate);

        private readonly SqliteConnection _connection;

        public TestDatabase(bool isMigrated = true)
        {
            ConnectionString = NewConnectionString();
            Configuration = ConfigurationFor(ConnectionString);
            Factory = new ApiDbContextFactory(Configuration);

            _connection = new SqliteConnection(ConnectionString);
            _connection.Open();

            if (isMigrated)
                Reset();
        }

        public string ConnectionString { get; }

        public IConfiguration Configuration { get; }

        public IDbContextFactory<ApiDbContext> Factory { get; }

        public ApiDbContext Open()
        {
            return Factory.CreateDbContext();
        }

        /// <summary>
        /// Empty again, with every table in place.
        /// </summary>
        public void Reset()
        {
            lock (Template)
                Template.Value.BackupDatabase(_connection);
        }

        /// <summary>
        /// Writes the rows and hands back the first, with its id.
        /// </summary>
        public TEntity Add<TEntity>(TEntity entity, params object[] others) where TEntity : class
        {
            using ApiDbContext db = Open();

            db.Add(entity);
            db.AddRange(others);
            db.SaveChanges();

            return entity;
        }

        public TResult Read<TResult>(Func<ApiDbContext, TResult> read)
        {
            using ApiDbContext db = Open();

            return read(db);
        }

        public void Change(Action<ApiDbContext> change)
        {
            using ApiDbContext db = Open();

            change(db);
            db.SaveChanges();
        }

        /// <summary>
        /// One value by hand-written sql, past EF and everything it does to a value.
        /// </summary>
        public object? Scalar(string sql)
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.CommandText = sql;

            object? value = command.ExecuteScalar();

            return value is DBNull ? null : value;
        }

        public void Execute(string sql)
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        // Unpooled: a pooled connection stays open after it is closed, and an in-memory database
        // lives as long as any connection to it does.
        private static string NewConnectionString()
        {
            return $"Data Source=gymcrm-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False";
        }

        private static IConfiguration ConfigurationFor(string connectionString)
        {
            Dictionary<string, string?> settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            };

            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        private static SqliteConnection BuildTemplate()
        {
            string connectionString = NewConnectionString();

            SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            using ApiDbContext db = new ApiDbContextFactory(ConfigurationFor(connectionString)).CreateDbContext();
            db.Database.Migrate();

            return connection;
        }
    }
}
