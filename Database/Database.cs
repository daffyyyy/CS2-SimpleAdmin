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
			return connection;
		}
		catch (Exception ex)
		{
			if (CS2_SimpleAdmin._logger != null)
				CS2_SimpleAdmin._logger.LogCritical($"Unable to connect to database: {ex.Message}");
			throw;
		}
	}

	public async Task<MySqlConnection> GetConnectionAsync()
	{
		try
		{
			var connection = new MySqlConnection(dbConnectionString);
			await connection.OpenAsync();
			return connection;
		}
		catch (Exception ex)
		{
			CS2_SimpleAdmin._logger?.LogCritical($"Unable to connect to database: {ex.Message}");
			throw;
		}
	}

	public void DatabaseMigration()
	{
		Migration migrator = new(this);
		migrator.ExecuteMigrations();
	}

	public bool CheckDatabaseConnection()
	{
		using var connection = GetConnection();

		try
		{
			return connection.Ping();
		}
		catch
		{
			return false;
		}
	}
}
