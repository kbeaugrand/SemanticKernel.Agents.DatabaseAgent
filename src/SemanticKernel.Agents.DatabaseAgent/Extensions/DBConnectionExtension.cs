using System.Data.Common;
using System.Text.RegularExpressions;

namespace SemanticKernel.Agents.DatabaseAgent.Extensions;

internal static class DBConnectionExtension
{
    internal static string GetProviderName(this DbConnection connection)
    {
        string typeName = connection.GetType().FullName;

        return typeName switch
        {
            string s when s.Contains("MySqlClient", StringComparison.OrdinalIgnoreCase) => "MySQL",
            string s when s.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) => "SQL Server",
            string s when s.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) => "PostgreSQL",
            string s when s.Contains("Oracle", StringComparison.OrdinalIgnoreCase) => "Oracle",
            string s when s.Contains("SQLite", StringComparison.OrdinalIgnoreCase) => "SQLite",
            string s when s.Contains("OleDb", StringComparison.OrdinalIgnoreCase) => "OLE DB",
            string s when s.Contains("Odbc", StringComparison.OrdinalIgnoreCase) => ExtractDriverFromConnectionString(connection.ConnectionString),
            _ => "Unknown Provider"
        };
    }

    private static string ExtractDriverFromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "Unknown Driver";
        }

        var match = Regex.Match(connectionString, @"(?i)Driver\s*=\s*([^;]+)");
        return match.Success ? match.Groups[1].Value : "Unknown Driver";
    }
}
