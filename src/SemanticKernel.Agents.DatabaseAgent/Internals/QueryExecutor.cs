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
            dataTable.Load(reader);

            return dataTable;
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
}
