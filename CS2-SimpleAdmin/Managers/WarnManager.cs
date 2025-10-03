using CS2_SimpleAdmin.Database;
using CS2_SimpleAdminApi;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

internal class WarnManager(IDatabaseProvider? databaseProvider)
{
    /// <summary>
    /// Adds a warning to a player with an optional issuer and reason.
    /// </summary>
    /// <param name="player">The player who is being warned.</param>
    /// <param name="issuer">The player issuing the warning; null indicates console or system.</param>
    /// <param name="reason">The reason for the warning.</param>
    /// <param name="time">Optional duration of the warning in minutes (0 means permanent).</param>
    /// <returns>The identifier of the inserted warning, or null if the operation failed.</returns>
    public async Task<int?> WarnPlayer(PlayerInfo player, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (databaseProvider == null) return null;

        var now = Time.ActualDateTime();
        var futureTime = now.AddMinutes(time);

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var sql = databaseProvider.GetAddWarnQuery(true);

            var warnId = await connection.ExecuteScalarAsync<int?>(sql, new
            {
                playerSteamid = player.SteamId.SteamId64,
                playerName = player.Name,
                adminSteamid = issuer?.SteamId.SteamId64 ?? 0,
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                muteReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                serverid = CS2_SimpleAdmin.ServerId
            });

            return warnId;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Adds a warning to a player identified by SteamID with optional issuer and reason.
    /// </summary>
    /// <param name="playerSteamId">The SteamID64 of the player being warned.</param>
    /// <param name="issuer">The player issuing the warning; null indicates console or system.</param>
    /// <param name="reason">The reason for the warning.</param>
    /// <param name="time">Optional duration of the warning in minutes (0 means permanent).</param>
    /// <returns>The identifier of the inserted warning, or null if the operation failed.</returns>
    public async Task<int?> AddWarnBySteamid(ulong playerSteamId, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (databaseProvider == null) return null;

        var now = Time.ActualDateTime();
        var futureTime = now.AddMinutes(time);

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var sql = databaseProvider.GetAddWarnQuery(false);

            var warnId = await connection.ExecuteScalarAsync<int?>(sql, new
            {
                playerSteamid = playerSteamId,
                adminSteamid = issuer?.SteamId.SteamId64 ?? 0,
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                muteReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                serverid = CS2_SimpleAdmin.ServerId
            });

            return warnId;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves a list of warnings for a specific player.
    /// </summary>
    /// <param name="player">The player whose warnings to retrieve.</param>
    /// <param name="active">If true, returns only active (non-expired) warnings; otherwise returns all warnings.</param>
    /// <returns>A list of dynamic objects representing warnings, or an empty list if none found or on failure.</returns>
    public async Task<List<dynamic>> GetPlayerWarns(PlayerInfo player, bool active = true)
    {
        if (databaseProvider == null) return [];

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetPlayerWarnsQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode, active);
            var parameters = new { PlayerSteamID = player.SteamId.SteamId64, serverid = CS2_SimpleAdmin.ServerId };
            var warns = await connection.QueryAsync<dynamic>(sql, parameters);

            return warns.ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    /// Retrieves the count of warnings for a player specified by SteamID.
    /// </summary>
    /// <param name="steamId">The SteamID64 of the player.</param>
    /// <param name="active">If true, counts only active (non-expired) warnings; otherwise counts all warnings.</param>
    /// <returns>The count of warnings as an integer, or 0 if none found or on failure.</returns>
    public async Task<int> GetPlayerWarnsCount(ulong steamId, bool active = true)
    {
        if (databaseProvider == null) return 0;

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetPlayerWarnsCountQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode, active);
            var warnsCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = steamId, serverid = CS2_SimpleAdmin.ServerId });
            return warnsCount;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Removes a specific warning by its identifier from a player's record.
    /// </summary>
    /// <param name="player">The player whose warning will be removed.</param>
    /// <param name="warnId">The identifier of the warning to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UnwarnPlayer(PlayerInfo player, int warnId)
    {
        if (databaseProvider == null) return;

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetUnwarnByIdQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);
            await connection.ExecuteAsync(sql, new { steamid = player.SteamId.SteamId64, warnId, serverid = CS2_SimpleAdmin.ServerId });
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogCritical($"Unable to remove warn + {ex}");
        }
    }
    
    /// <summary>
    /// Removes the most recent warning matching a player pattern (usually SteamID string).
    /// </summary>
    /// <param name="playerPattern">The pattern identifying the player whose last warning should be removed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UnwarnPlayer(string playerPattern)
    {
        if (databaseProvider == null) return;

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetUnwarnLastQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);
            await connection.ExecuteAsync(sql, new { steamid = playerPattern, serverid = CS2_SimpleAdmin.ServerId });
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogCritical("Unable to remove last warn {exception}", ex.Message);
        }
    }

    /// <summary>
    /// Expires old warnings based on the current time, removing or marking them as inactive.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExpireOldWarns()
    {
        if (databaseProvider == null) return;

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetExpireWarnsQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);
            await connection.ExecuteAsync(sql, new { CurrentTime = Time.ActualDateTime(), serverid = CS2_SimpleAdmin.ServerId });
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogCritical($"Unable to remove expired warns + {ex}");
        }
    }
}