using System.Data.Common;

namespace SemanticKernel.Agents.DatabaseAgent.Extensions;

internal static class DBConnectionExtension
{    
    internal static string GetProviderName(this DbConnection connection)
    {
        string typeName = connection.GetType().FullName;

        return typeName switch
        {
            string s when s.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) => "SQL Server",
            string s when s.Contains("MySqlClient", StringComparison.OrdinalIgnoreCase) => "MySQL",
            string s when s.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) => "PostgreSQL",
            string s when s.Contains("Oracle", StringComparison.OrdinalIgnoreCase) => "Oracle",
            string s when s.Contains("SQLite", StringComparison.OrdinalIgnoreCase) => "SQLite",
            _ => "Unknown Provider"
        };
    }
}
