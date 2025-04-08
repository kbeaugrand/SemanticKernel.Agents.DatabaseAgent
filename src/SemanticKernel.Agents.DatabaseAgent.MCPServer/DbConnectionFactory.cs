using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer;

public class DbConnectionFactory
{
    public static DbConnection CreateDbConnection(string connectionString, string providerType)
    {
        switch (providerType.ToLower())
        {
            case "sqlserver":
                return new SqlConnection(connectionString);

            case "sqlite":
                return new SqliteConnection(connectionString);

            case "mysql":
                return new MySqlConnection(connectionString);

            case "postgresql":
                return new NpgsqlConnection(connectionString);

            case "oracle":
                return new OracleConnection(connectionString);

            case "access":
                return new OleDbConnection(connectionString);

            case "odbc": 
                return new OdbcConnection(connectionString);

            default:
                throw new ArgumentException($"Unsupported provider type: {providerType}");
        }
    }
}