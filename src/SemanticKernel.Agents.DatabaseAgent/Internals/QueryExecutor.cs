using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using System.Data.Common;
using System.Text;

namespace SemanticKernel.Agents.DatabaseAgent.Internals;

internal static class QueryExecutor
{
    internal static async Task<DataTable> ExecuteSQLAsync(
                                        DbConnection connection,
                                        string sqlQuery,
                                        ILoggerFactory? loggerFactory,
                                        CancellationToken cancellationToken)
    {
        var log = loggerFactory?.CreateLogger(nameof(QueryExecutor)) ?? NullLogger.Instance;

        log.LogInformation("SQL Query: {Query}", sqlQuery);

        using var command = connection.CreateCommand();

        command.CommandText = sqlQuery;
        command.Parameters.Clear();

        var result = new StringBuilder();

        try
        {
            await connection.OpenAsync(cancellationToken)
                                    .ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken)
                                        .ConfigureAwait(false);

            var dataTable = new DataTable();

            return await SafeLoadToDataTableAsync(reader, cancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error executing SQL Query: {Query}", sqlQuery);
            throw;
        }
        finally
        {
            connection.Close();
        }
    }

    private static async Task<DataTable> SafeLoadToDataTableAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var dataTable = new DataTable();

        // Build columns using schema info
        var schemaTable = await Task.Run(() => reader.GetSchemaTable(), cancellationToken);
        if (schemaTable != null)
        {
            foreach (DataRow row in schemaTable.Rows)
            {
                var columnName = row["ColumnName"]?.ToString() ?? "Column";
                if (!dataTable.Columns.Contains(columnName))
                {
                    // Always use object to prevent type mismatch
                    dataTable.Columns.Add(new DataColumn(columnName, typeof(object)));
                }
            }
        }

        // Read data rows
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }
}
