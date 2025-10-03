using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CS2_SimpleAdmin.Database;

public class Database(string dbConnectionString)
{
    public MySqlConnection GetConnection()
    {
        try
        {
            var connection = new MySqlConnection(dbConnectionString);
            connection.Open();
            
            // using var cmd = connection.CreateCommand();
            // cmd.CommandText = "SET NAMES 'utf8mb4' COLLATE 'utf8mb4_general_ci';";
            // cmd.ExecuteNonQueryAsync();
            
            return connection;
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogCritical($"Unable to connect to database: {ex.Message}");
            throw;
        }
    }

    public async Task<MySqlConnection> GetConnectionAsync()
    {
        try
        {
            var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // await using var cmd = connection.CreateCommand();
            // cmd.CommandText = "SET NAMES 'utf8mb4' COLLATE 'utf8mb4_general_ci';";
            // await cmd.ExecuteNonQueryAsync();
            
            return connection;
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogCritical($"Unable to connect to database: {ex.Message}");
            throw;
        }
    }

    // public async Task DatabaseMigration()
    // {
    //     Migration migrator = new(this);
    //     await migrator.ExecuteMigrationsAsync();
    // }

    public bool CheckDatabaseConnection(out string? exception)
    {
        using var connection = GetConnection();
        exception = null;
        
        try
        {
            return connection.Ping();
        }
        catch (Exception ex)
        {
            exception = ex.Message;
            return false;
        }
    }
}