using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Oqtane.Modules;
using Yosh.uwa.Models;

namespace Yosh.uwa.Repository
{
    public interface IDatabaseSchemaProvider
    {
        Task<List<DatabaseTableInfo>> GetTablesAsync(CancellationToken cancellationToken = default);
        Task<List<DatabaseColumnInfo>> GetColumnsAsync(string schema, string table, CancellationToken cancellationToken = default);
    }

    public class SqlServerSchemaProvider : IDatabaseSchemaProvider, ITransientService
    {
        private readonly IDbContextFactory<Context> _factory;

        public SqlServerSchemaProvider(IDbContextFactory<Context> factory)
        {
            _factory = factory;
        }

        public async Task<List<DatabaseTableInfo>> GetTablesAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT s.name, t.name
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;";

            using var db = _factory.CreateDbContext();
            return await QueryAsync(db.Database.GetDbConnection(), sql, reader => new DatabaseTableInfo
            {
                Schema = reader.GetString(0),
                Name = reader.GetString(1)
            }, null, cancellationToken);
        }

        public async Task<List<DatabaseColumnInfo>> GetColumnsAsync(string schema, string table, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT s.name, t.name, c.name, ty.name, c.is_nullable, c.is_identity, c.column_id,
       CAST(CASE WHEN pk.column_id IS NULL THEN 0 ELSE 1 END AS bit)
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN (
    SELECT ic.object_id, ic.column_id
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.is_primary_key = 1
) pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
WHERE s.name = @schema AND t.name = @table
ORDER BY c.column_id;";

            using var db = _factory.CreateDbContext();
            var parameters = new Dictionary<string, object> { ["@schema"] = schema, ["@table"] = table };
            return await QueryAsync(db.Database.GetDbConnection(), sql, reader => new DatabaseColumnInfo
            {
                Schema = reader.GetString(0),
                Table = reader.GetString(1),
                Name = reader.GetString(2),
                DataType = reader.GetString(3),
                IsNullable = reader.GetBoolean(4),
                IsIdentity = reader.GetBoolean(5),
                Ordinal = reader.GetInt32(6),
                IsPrimaryKey = reader.GetBoolean(7)
            }, parameters, cancellationToken);
        }

        private static async Task<List<T>> QueryAsync<T>(DbConnection connection, string sql, System.Func<DbDataReader, T> map, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var results = new List<T>();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            if (parameters != null)
            {
                foreach (var item in parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = item.Key;
                    parameter.Value = item.Value;
                    command.Parameters.Add(parameter);
                }
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(map(reader));
            }
            return results;
        }
    }
}
