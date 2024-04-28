using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CS2_SimpleAdmin;

public class Database(string dbConnectionString)
{
	private readonly string _dbConnectionString = dbConnectionString;

	public MySqlConnection GetConnection()
	{
		try
		{
			var connection = new MySqlConnection(_dbConnectionString);
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
			var connection = new MySqlConnection(_dbConnectionString);
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
