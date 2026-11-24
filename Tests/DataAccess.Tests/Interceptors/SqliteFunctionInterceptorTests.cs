using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using DataAccess.Interceptors;
using Microsoft.Data.Sqlite;

namespace DataAccess.Tests.Interceptors
{
    /// <summary>
    /// A sqlite function lives on the connection, so it has to be there on every one that is opened,
    /// whichever way it is opened.
    /// </summary>
    public sealed class SqliteFunctionInterceptorTests
    {
        private static SqliteConnection Connection()
        {
            SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            return connection;
        }

        private static object? Normalize(SqliteConnection connection, string value)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT normalize({value})";

            return command.ExecuteScalar();
        }

        [Fact]
        public void A_connection_has_no_such_function_of_its_own()
        {
            using SqliteConnection connection = Connection();

            Should.Throw<SqliteException>(() => Normalize(connection, "'x'")).Message.ShouldContain("no such function: normalize");
        }

        [Fact]
        public void An_opened_connection_gets_the_function()
        {
            using SqliteConnection connection = Connection();

            new SqliteFunctionInterceptor().ConnectionOpened(connection, null!);

            Normalize(connection, "'Ελένη'").ShouldBe("ελενη");
            Normalize(connection, "NULL").ShouldBe("");
            Normalize(connection, "''").ShouldBe("");
        }

        [Fact]
        public async Task A_connection_opened_asynchronously_gets_the_function()
        {
            using SqliteConnection connection = Connection();

            await new SqliteFunctionInterceptor().ConnectionOpenedAsync(connection, null!, TestContext.Current.CancellationToken);

            Normalize(connection, "'ΝΙΚΟΣ'").ShouldBe("νικοσ");
        }

        // Deterministic, so sqlite may use it where it has to give the same answer every time.
        [Fact]
        public void The_function_may_be_used_in_an_index()
        {
            using SqliteConnection connection = Connection();
            new SqliteFunctionInterceptor().ConnectionOpened(connection, null!);

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE names (name TEXT); CREATE INDEX ix ON names (normalize(name));";

            Should.NotThrow(() => command.ExecuteNonQuery());
        }

        [Fact]
        public void Opening_the_same_connection_again_changes_nothing()
        {
            using SqliteConnection connection = Connection();
            SqliteFunctionInterceptor interceptor = new SqliteFunctionInterceptor();

            interceptor.ConnectionOpened(connection, null!);
            interceptor.ConnectionOpened(connection, null!);

            Normalize(connection, "'Maria'").ShouldBe("maria");
        }

        [Fact]
        public async Task A_connection_to_something_else_is_left_alone()
        {
            using OtherConnection other = new OtherConnection();

            Should.NotThrow(() => new SqliteFunctionInterceptor().ConnectionOpened(other, null!));
            await Should.NotThrowAsync(() => new SqliteFunctionInterceptor().ConnectionOpenedAsync(other, null!, TestContext.Current.CancellationToken));
        }

        // Throws on anything it is asked for.
        private sealed class OtherConnection : DbConnection
        {
            [AllowNull]
            public override string ConnectionString
            {
                get { throw new InvalidOperationException(); }
                set { throw new InvalidOperationException(); }
            }

            public override string Database
            {
                get { throw new InvalidOperationException(); }
            }

            public override string DataSource
            {
                get { throw new InvalidOperationException(); }
            }

            public override string ServerVersion
            {
                get { throw new InvalidOperationException(); }
            }

            public override ConnectionState State
            {
                get { return ConnectionState.Open; }
            }

            public override void ChangeDatabase(string databaseName)
            {
                throw new InvalidOperationException();
            }

            public override void Close()
            {
            }

            public override void Open()
            {
                throw new InvalidOperationException();
            }

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                throw new InvalidOperationException();
            }

            protected override DbCommand CreateDbCommand()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
