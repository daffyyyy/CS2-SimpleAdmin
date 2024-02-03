using MySqlConnector;

namespace CS2_SimpleAdmin;
public class Database
{
	private readonly string _dbConnectionString;

	public Database(string dbConnectionString)
	{
		_dbConnectionString = dbConnectionString;
	}

	public MySqlConnection GetConnection()
	{
		var connection = new MySqlConnection(_dbConnectionString);
		connection.Open();
		return connection;
	}
}

public static class DatabaseExtension
{
	public static string WithConnectionPooling(this string connectionString)
	{
		return $"{connectionString};Pooling=true;MinimumPoolSize=1;MaximumPoolSize=15";
	}
}