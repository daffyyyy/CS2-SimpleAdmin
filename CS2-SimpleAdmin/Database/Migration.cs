using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Database;

public class Migration(string migrationsPath)
{
    /// <summary>
    /// Executes all migration scripts found in the configured migrations path that have not been applied yet.
    /// Creates a migration tracking table if it does not exist.
    /// Applies migration scripts in filename order and logs successes or failures.
    /// </summary>
    public async Task ExecuteMigrationsAsync()
    {
        if (CS2_SimpleAdmin.DatabaseProvider == null) return;
        var files = Directory.GetFiles(migrationsPath, "*.sql").OrderBy(f => f).ToList();
        if (files.Count == 0) return;

        await using var connection = await CS2_SimpleAdmin.DatabaseProvider.CreateConnectionAsync();
        await using (var cmd = connection.CreateCommand())
        {
            if (migrationsPath.Contains("sqlite", StringComparison.CurrentCultureIgnoreCase))
            {
                cmd.CommandText = """
                                                  CREATE TABLE IF NOT EXISTS sa_migrations (
                                                      id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                      version TEXT NOT NULL
                                                  );
                                              
                                  """;
            }
            else
            {
                cmd.CommandText = """
                                      CREATE TABLE IF NOT EXISTS sa_migrations (
                                          id INT PRIMARY KEY AUTO_INCREMENT,
                                          version VARCHAR(128) NOT NULL
                                      );
                                  """;
            }

            await cmd.ExecuteNonQueryAsync();
        }

        var lastAppliedVersion = await GetLastAppliedVersionAsync(connection);

        foreach (var file in files)
        {
            var version = Path.GetFileNameWithoutExtension(file);
            if (string.Compare(version, lastAppliedVersion, StringComparison.OrdinalIgnoreCase) <= 0)
                continue;

            try
            {
                var sqlScript = await File.ReadAllTextAsync(file);

                await using (var cmdMigration = connection.CreateCommand())
                {
                    cmdMigration.CommandText = sqlScript;
                    await cmdMigration.ExecuteNonQueryAsync();
                }

                await UpdateLastAppliedVersionAsync(connection, version);

                CS2_SimpleAdmin._logger?.LogInformation($"Migration \"{version}\" successfully applied.");
            }
            catch (Exception ex)
            {
                CS2_SimpleAdmin._logger?.LogError(ex, $"Error applying migration \"{version}\".");
                break;
            }
        }
    }

    /// <summary>
    /// Retrieves the version string of the last applied migration from the database.
    /// </summary>
    /// <param name="connection">The open database connection.</param>
    /// <returns>The version string of the last applied migration, or empty string if none.</returns>
    private static async Task<string> GetLastAppliedVersionAsync(DbConnection connection)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT version FROM sa_migrations ORDER BY id DESC LIMIT 1;";
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Inserts a record tracking the successful application of a migration version.
    /// </summary>
    /// <param name="connection">The open database connection.</param>
    /// <param name="version">The version string of the migration applied.</param>
    private static async Task UpdateLastAppliedVersionAsync(DbConnection connection, string version)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO sa_migrations (version) VALUES (@Version);";

        var param = cmd.CreateParameter();
        param.ParameterName = "@Version";
        param.Value = version;
        cmd.Parameters.Add(param);

        await cmd.ExecuteNonQueryAsync();
    }
}
