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

		public async Task<MySqlConnection> GetConnection()
		{
			try
			{
				var connection = new MySqlConnection(_dbConnectionString);
				await connection.OpenAsync();
				return connection;
			}
			catch (Exception)
			{
				if (CS2_SimpleAdmin._logger != null)
					CS2_SimpleAdmin._logger.LogCritical("Unable to connect to database");
				throw;
			}
		}
	}
}