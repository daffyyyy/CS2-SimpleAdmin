using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CS2_SimpleAdmin;

public class Migration(Database database)
{
	private readonly Database _database = database;

	public void ExecuteMigrations()
	{
		var migrationsDirectory = CS2_SimpleAdmin.Instance.ModuleDirectory + "/Database/Migrations";

		var files = Directory.GetFiles(migrationsDirectory, "*.sql")
							 .OrderBy(f => f);

		using var connection = _database.GetConnection();

		// Create sa_migrations table if not exists
		using var cmd = new MySqlCommand("""
		                                 
		                                             CREATE TABLE IF NOT EXISTS `sa_migrations` (
		                                                 `id` INT PRIMARY KEY AUTO_INCREMENT,
		                                                 `version` VARCHAR(255) NOT NULL
		                                             );
		                                 """, connection);

		cmd.ExecuteNonQuery();

		// Get the last applied migration version
		var lastAppliedVersion = GetLastAppliedVersion(connection);

		foreach (var file in files)
		{
			var version = Path.GetFileNameWithoutExtension(file);

			// Check if the migration has already been applied
			if (string.Compare(version, lastAppliedVersion, StringComparison.OrdinalIgnoreCase) <= 0) continue;
			var sqlScript = File.ReadAllText(file);

			using var cmdMigration = new MySqlCommand(sqlScript, connection);
			cmdMigration.ExecuteNonQuery();

			// Update the last applied migration version
			UpdateLastAppliedVersion(connection, version);

			CS2_SimpleAdmin._logger?.LogInformation($"Migration \"{version}\" successfully applied.");
		}
	}

	private static string GetLastAppliedVersion(MySqlConnection connection)
	{
		using var cmd = new MySqlCommand("SELECT `version` FROM `sa_migrations` ORDER BY `id` DESC LIMIT 1;", connection);
		var result = cmd.ExecuteScalar();
		return result?.ToString() ?? string.Empty;
	}

	private static void UpdateLastAppliedVersion(MySqlConnection connection, string version)
	{
		using var cmd = new MySqlCommand("INSERT INTO `sa_migrations` (`version`) VALUES (@Version);", connection);
		cmd.Parameters.AddWithValue("@Version", version);
		cmd.ExecuteNonQuery();
	}
}
