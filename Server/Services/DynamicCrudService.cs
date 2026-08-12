using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Oqtane.Modules;
using Yosh.uwa.Extensibility;
using Yosh.uwa.Models;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Services
{
    public interface IDynamicCrudService
    {
        Task<CrudDataResult> GetDataAsync(int definitionId, int moduleId, CancellationToken cancellationToken = default);
        Task<CrudOperationResult> InsertAsync(CrudDataRequest request, CancellationToken cancellationToken = default);
        Task<CrudOperationResult> UpdateAsync(CrudDataRequest request, CancellationToken cancellationToken = default);
        Task<CrudOperationResult> DeleteAsync(CrudDataRequest request, CancellationToken cancellationToken = default);
    }

    public class DynamicCrudService : IDynamicCrudService, ITransientService
    {
        private readonly ICrudDefinitionRepository _definitions;
        private readonly IDatabaseSchemaProvider _schema;
        private readonly IDbContextFactory<Context> _factory;
        private readonly IEnumerable<ICrudBusinessRule> _rules;

        public DynamicCrudService(ICrudDefinitionRepository definitions, IDatabaseSchemaProvider schema, IDbContextFactory<Context> factory, IEnumerable<ICrudBusinessRule> rules)
        {
            _definitions = definitions;
            _schema = schema;
            _factory = factory;
            _rules = rules;
        }

        public async Task<CrudDataResult> GetDataAsync(int definitionId, int moduleId, CancellationToken cancellationToken = default)
        {
            var definition = GetDefinition(definitionId, moduleId);
            var configuration = Deserialize(definition.ConfigurationJson);
            var columns = configuration.Columns.Where(column => column.IsVisible).OrderBy(column => column.SortOrder).ToList();

            if (columns.Count == 0)
            {
                columns = (await _schema.GetColumnsAsync(definition.SourceSchema, definition.SourceTable, cancellationToken))
                    .Select((column, index) => new CrudColumnDefinition
                    {
                        Schema = column.Schema,
                        Table = column.Table,
                        Column = column.Name,
                        Label = column.Name,
                        IsEditable = !column.IsIdentity && !column.IsPrimaryKey,
                        IsRequired = !column.IsNullable && !column.IsIdentity,
                        SortOrder = index
                    }).ToList();
            }

            var aliases = await ValidateAndResolveAliasesAsync(definition, configuration, columns, cancellationToken);
            var select = string.Join(", ", columns.Select((column, index) => $"{aliases[column.DataKey]}.{Quote(column.Column)} AS {Quote($"c{index}")}"));
            var sql = new StringBuilder($"SELECT TOP (100) {select} FROM {Qualified(definition.SourceSchema, definition.SourceTable)} t0");

            for (var index = 0; index < configuration.Joins.Count; index++)
            {
                var join = configuration.Joins[index];
                var joinType = string.Equals(join.JoinType, "INNER", StringComparison.OrdinalIgnoreCase) ? "INNER" : "LEFT";
                sql.Append($" {joinType} JOIN {Qualified(join.Schema, join.Table)} t{index + 1} ON t0.{Quote(join.LeftColumn)} = t{index + 1}.{Quote(join.RightColumn)}");
            }

            using var db = _factory.CreateDbContext();
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = sql.ToString();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new CrudDataResult { Columns = columns };

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < columns.Count; index++)
                {
                    row[columns[index].DataKey] = reader.IsDBNull(index)
                        ? null
                        : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture);
                }
                result.Rows.Add(row);
            }
            return result;
        }

        public Task<CrudOperationResult> InsertAsync(CrudDataRequest request, CancellationToken cancellationToken = default)
            => ExecuteAsync("Insert", request, cancellationToken);

        public Task<CrudOperationResult> UpdateAsync(CrudDataRequest request, CancellationToken cancellationToken = default)
            => ExecuteAsync("Update", request, cancellationToken);

        public Task<CrudOperationResult> DeleteAsync(CrudDataRequest request, CancellationToken cancellationToken = default)
            => ExecuteAsync("Delete", request, cancellationToken);

        private async Task<CrudOperationResult> ExecuteAsync(string operation, CrudDataRequest request, CancellationToken cancellationToken)
        {
            var definition = GetDefinition(request.DefinitionId, request.ModuleId);
            var configuration = Deserialize(definition.ConfigurationJson);
            var ruleContext = new CrudRuleContext
            {
                Definition = definition,
                Operation = operation,
                KeyValue = request.KeyValue,
                Values = request.Values
            };

            var rules = _rules.Where(rule => rule.Key == "*" || string.Equals(rule.Key, configuration.BusinessRuleKey, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var rule in rules)
            {
                var validation = await rule.ValidateAsync(ruleContext, cancellationToken);
                if (!validation.Succeeded)
                {
                    return validation;
                }
                await rule.BeforeExecuteAsync(ruleContext, cancellationToken);
            }

            var metadata = await _schema.GetColumnsAsync(definition.SourceSchema, definition.SourceTable, cancellationToken);
            var primaryKey = metadata.FirstOrDefault(column => column.Name == definition.PrimaryKeyColumn && column.IsPrimaryKey);
            if (primaryKey == null)
            {
                return CrudOperationResult.Failure("A valid primary key must be selected before data can be changed.");
            }

            using var db = _factory.CreateDbContext();
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            var editable = configuration.Columns
                .Where(column => column.IsEditable && column.Schema == definition.SourceSchema && column.Table == definition.SourceTable)
                .Select(column => metadata.FirstOrDefault(item => item.Name == column.Column))
                .Where(column => column != null && !column.IsIdentity && !column.IsPrimaryKey)
                .DistinctBy(column => column.Name)
                .ToList();

            if (operation == "Insert" || operation == "Update")
            {
                foreach (var required in editable.Where(column => !column.IsNullable))
                {
                    if (!request.Values.TryGetValue(required.Name, out var requiredValue) || string.IsNullOrWhiteSpace(requiredValue))
                    {
                        return CrudOperationResult.Failure($"{required.Name} is required.");
                    }
                }
            }

            if (operation == "Insert")
            {
                var supplied = editable.Where(column => request.Values.ContainsKey(column.Name)).ToList();
                if (supplied.Count == 0)
                {
                    return CrudOperationResult.Failure("No editable values were supplied.");
                }
                command.CommandText = $"INSERT INTO {Qualified(definition.SourceSchema, definition.SourceTable)} ({string.Join(", ", supplied.Select(column => Quote(column.Name)))}) VALUES ({string.Join(", ", supplied.Select((_, index) => $"@p{index}"))})";
                AddValueParameters(command, supplied, request.Values);
            }
            else if (operation == "Update")
            {
                var supplied = editable.Where(column => request.Values.ContainsKey(column.Name)).ToList();
                if (supplied.Count == 0)
                {
                    return CrudOperationResult.Failure("No editable values were supplied.");
                }
                command.CommandText = $"UPDATE {Qualified(definition.SourceSchema, definition.SourceTable)} SET {string.Join(", ", supplied.Select((column, index) => $"{Quote(column.Name)} = @p{index}"))} WHERE {Quote(primaryKey.Name)} = @key";
                AddValueParameters(command, supplied, request.Values);
                AddParameter(command, "@key", ConvertValue(primaryKey, request.KeyValue));
            }
            else
            {
                command.CommandText = $"DELETE FROM {Qualified(definition.SourceSchema, definition.SourceTable)} WHERE {Quote(primaryKey.Name)} = @key";
                AddParameter(command, "@key", ConvertValue(primaryKey, request.KeyValue));
            }

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                return CrudOperationResult.Failure("No matching row was changed.");
            }

            foreach (var rule in rules)
            {
                await rule.AfterExecuteAsync(ruleContext, cancellationToken);
            }
            return CrudOperationResult.Success($"{operation} completed.");
        }

        private async Task<Dictionary<string, string>> ValidateAndResolveAliasesAsync(CrudDefinition definition, CrudBuilderConfiguration configuration, List<CrudColumnDefinition> columns, CancellationToken cancellationToken)
        {
            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var baseColumns = await _schema.GetColumnsAsync(definition.SourceSchema, definition.SourceTable, cancellationToken);

            for (var index = 0; index < configuration.Joins.Count; index++)
            {
                var join = configuration.Joins[index];
                var joinedColumns = await _schema.GetColumnsAsync(join.Schema, join.Table, cancellationToken);
                if (!baseColumns.Any(column => column.Name == join.LeftColumn) || !joinedColumns.Any(column => column.Name == join.RightColumn))
                {
                    throw new InvalidOperationException($"Join {index + 1} references a column that no longer exists.");
                }
            }

            foreach (var column in columns)
            {
                if (column.Schema == definition.SourceSchema && column.Table == definition.SourceTable)
                {
                    if (!baseColumns.Any(item => item.Name == column.Column))
                    {
                        throw new InvalidOperationException($"Column {column.DataKey} no longer exists.");
                    }
                    aliases[column.DataKey] = "t0";
                    continue;
                }

                var joinIndex = configuration.Joins.FindIndex(join => join.Schema == column.Schema && join.Table == column.Table);
                if (joinIndex < 0)
                {
                    throw new InvalidOperationException($"Column {column.DataKey} is not part of the configured source or joins.");
                }
                var joinedColumns = await _schema.GetColumnsAsync(column.Schema, column.Table, cancellationToken);
                if (!joinedColumns.Any(item => item.Name == column.Column))
                {
                    throw new InvalidOperationException($"Column {column.DataKey} no longer exists.");
                }
                aliases[column.DataKey] = $"t{joinIndex + 1}";
            }
            return aliases;
        }

        private CrudDefinition GetDefinition(int definitionId, int moduleId)
        {
            var definition = _definitions.GetDefinition(definitionId, false);
            if (definition == null)
            {
                throw new InvalidOperationException("CRUD definition was not found for this module.");
            }
            var configuration = Deserialize(definition.ConfigurationJson);
            if (definition.ModuleId != moduleId && !configuration.GeneratedPages.Any(page => page.ModuleId == moduleId))
            {
                throw new InvalidOperationException("CRUD definition was not found for this module.");
            }
            return definition;
        }

        private static CrudBuilderConfiguration Deserialize(string json)
            => string.IsNullOrWhiteSpace(json)
                ? new CrudBuilderConfiguration()
                : JsonSerializer.Deserialize<CrudBuilderConfiguration>(json) ?? new CrudBuilderConfiguration();

        private static string Quote(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
        private static string Qualified(string schema, string table) => $"{Quote(schema)}.{Quote(table)}";

        private static void AddValueParameters(System.Data.Common.DbCommand command, List<DatabaseColumnInfo> columns, IDictionary<string, string> values)
        {
            for (var index = 0; index < columns.Count; index++)
            {
                AddParameter(command, $"@p{index}", ConvertValue(columns[index], values[columns[index].Name]));
            }
        }

        private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static object ConvertValue(DatabaseColumnInfo column, string value)
        {
            if (string.IsNullOrWhiteSpace(value) && column.IsNullable)
            {
                return DBNull.Value;
            }

            return column.DataType.ToLowerInvariant() switch
            {
                "int" => int.Parse(value, CultureInfo.InvariantCulture),
                "bigint" => long.Parse(value, CultureInfo.InvariantCulture),
                "smallint" => short.Parse(value, CultureInfo.InvariantCulture),
                "tinyint" => byte.Parse(value, CultureInfo.InvariantCulture),
                "decimal" or "numeric" or "money" or "smallmoney" => decimal.Parse(value, CultureInfo.InvariantCulture),
                "float" => double.Parse(value, CultureInfo.InvariantCulture),
                "real" => float.Parse(value, CultureInfo.InvariantCulture),
                "bit" => bool.Parse(value),
                "uniqueidentifier" => Guid.Parse(value),
                "date" or "datetime" or "datetime2" or "smalldatetime" => DateTime.Parse(value, CultureInfo.InvariantCulture),
                "datetimeoffset" => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture),
                "time" => TimeSpan.Parse(value, CultureInfo.InvariantCulture),
                _ => value
            };
        }
    }
}
