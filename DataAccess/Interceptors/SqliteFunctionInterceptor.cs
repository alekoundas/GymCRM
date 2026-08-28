using Core.System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace DataAccess.Interceptors
{
    // A sqlite function lives on the connection, not in the file, so it has to be
    // registered again every time one is opened.
    public class SqliteFunctionInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            RegisterFunctions(connection);
            base.ConnectionOpened(connection, eventData);
        }

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            RegisterFunctions(connection);
            return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }

        private static void RegisterFunctions(DbConnection connection)
        {
            if (connection is not SqliteConnection sqliteConnection)
                return;

            sqliteConnection.CreateFunction<string?, string>(
                "normalize",
                value => TextNormalizer.Normalize(value),
                isDeterministic: true);
        }
    }
}
