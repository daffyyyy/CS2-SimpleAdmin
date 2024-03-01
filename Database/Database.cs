using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CS2_SimpleAdmin
{
	public class Database
	{
		private readonly string _dbConnectionString;

		public Database(string dbConnectionString)
		{
			_dbConnectionString = dbConnectionString;
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
				if (CS2_SimpleAdmin._logger != null)
					CS2_SimpleAdmin._logger.LogCritical($"Unable to connect to database: {ex.Message}");
				throw;
			}
		}
	}
}