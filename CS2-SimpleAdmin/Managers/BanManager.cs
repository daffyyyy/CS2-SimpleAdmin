using CounterStrikeSharp.API;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CS2_SimpleAdminApi;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Text;
using CS2_SimpleAdmin.Database;

namespace CS2_SimpleAdmin.Managers;

internal class BanManager(IDatabaseProvider? databaseProvider)
{
    /// <summary>
    /// Bans an online player and inserts the ban record into the database.
    /// </summary>
    /// <param name="player">The player to be banned (must be currently online).</param>
    /// <param name="issuer">The admin issuing the ban. Can be null if issued from console.</param>
    /// <param name="reason">The reason for the ban.</param>
    /// <param name="time">Ban duration in minutes. If 0, the ban is permanent.</param>
    /// <returns>The newly created ban ID if successful, otherwise null.</returns>
    public async Task<int?> BanPlayer(PlayerInfo player, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (databaseProvider == null) return null;
        DateTime now = Time.ActualDateTime();
        DateTime futureTime = now.AddMinutes(time);

        await using var connection = await databaseProvider.CreateConnectionAsync();
        try
        {
            var sql = databaseProvider.GetAddBanQuery();
            var banId = await connection.ExecuteScalarAsync<int?>(sql, new
            {
                playerSteamid = player.SteamId.SteamId64,
                playerName = player.Name,
                playerIp = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 1 ? player.IpAddress : null,
                adminSteamid = issuer?.SteamId.SteamId64 ?? 0,
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                banReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                serverid = CS2_SimpleAdmin.ServerId
            });

            return banId;
        }
        catch(Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError(ex, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Adds a ban for an offline player identified by their SteamID.
    /// </summary>
    /// <param name="playerSteamId">The SteamID64 of the player to ban.</param>
    /// <param name="issuer">The admin issuing the ban. Can be null if issued from console.</param>
    /// <param name="reason">The reason for the ban.</param>
    /// <param name="time">Ban duration in minutes. If 0, the ban is permanent.</param>
    /// <returns>The ID of the newly created ban if successful, otherwise null.</returns>
    public async Task<int?> AddBanBySteamid(ulong playerSteamId, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (databaseProvider == null) return null;

        DateTime now = Time.ActualDateTime();
        DateTime futureTime = now.AddMinutes(time);

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var sql = databaseProvider.GetAddBanBySteamIdQuery();
            var banId = await connection.ExecuteScalarAsync<int?>(sql, new
            {
                playerSteamid = playerSteamId,
                adminSteamid = issuer?.SteamId.SteamId64 ?? 0,
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                banReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                serverid = CS2_SimpleAdmin.ServerId
            });
            
            return banId;
        }
        catch(Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError(ex, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Adds a ban for an offline player identified by their IP address.
    /// </summary>
    /// <param name="playerIp">The IP address of the player to ban.</param>
    /// <param name="issuer">The admin issuing the ban. Can be null if issued from console.</param>
    /// <param name="reason">The reason for the ban.</param>
    /// <param name="time">Ban duration in minutes. If 0, the ban is permanent.</param>
    public async Task AddBanByIp(string playerIp, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (databaseProvider == null) return;

        if (string.IsNullOrEmpty(playerIp)) return;

        DateTime now = Time.ActualDateTime();
        DateTime futureTime = now.AddMinutes(time);

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetAddBanByIpQuery();
            await connection.ExecuteAsync(sql, new
            {
                playerIp,
                adminSteamid = issuer?.SteamId.SteamId64 ?? 0,
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                banReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                serverid = CS2_SimpleAdmin.ServerId
            });
        }
        catch { }
    }

//     public async Task<bool> IsPlayerBanned(PlayerInfo player)
//     {
//         if (database == null) return false;
//
//         if (player.IpAddress == null)
//         {
//             return false;
//         }
//         
// #if DEBUG
//         if (CS2_SimpleAdmin._logger != null)
//             CS2_SimpleAdmin._logger.LogCritical($"IsPlayerBanned for {player.Name}");
// #endif
//
//         int banCount;
//
//         DateTime currentTime = Time.ActualDateTime();
//
//         try
//         {
//             string sql;
//             
//             if (CS2_SimpleAdmin.Instance.Config.OtherSettings.CheckMultiAccountsByIp && !CS2_SimpleAdmin.Instance.Config.OtherSettings.IgnoredIps.Contains(player.IpAddress))
//             {
//                 sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode ? """
//                                                                             SELECT COALESCE((
//                                                                                 SELECT COUNT(*)
//                                                                                 FROM sa_bans
//                                                                                 WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
//                                                                                 AND status = 'ACTIVE'
//                                                                                 AND (duration = 0 OR ends > @CurrentTime)
//                                                                             ), 0) 
//                                                                             + 
//                                                                             COALESCE((
//                                                                                 SELECT COUNT(*)
//                                                                                 FROM sa_bans
//                                                                                 JOIN sa_players_ips ON sa_bans.player_steamid = sa_players_ips.steamid
//                                                                                 WHERE sa_bans.status = 'ACTIVE'
//                                                                                 AND sa_players_ips.address = @PlayerIP
//                                                                                 AND NOT EXISTS (
//                                                                                     SELECT 1 
//                                                                                     FROM sa_bans 
//                                                                                     WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) 
//                                                                                     AND status = 'ACTIVE'
//                                                                                     AND (duration = 0 OR ends > @CurrentTime)
//                                                                                 )
//                                                                             ), 0) AS TotalBanCount;
//                                                                         """ : """
//                                                                                   SELECT COALESCE((
//                                                                                       SELECT COUNT(*)
//                                                                                       FROM sa_bans
//                                                                                       WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
//                                                                                       AND status = 'ACTIVE'
//                                                                                       AND (duration = 0 OR ends > @CurrentTime)
//                                                                                       AND server_id = @ServerId
//                                                                                   ), 0) 
//                                                                                   + 
//                                                                                   COALESCE((
//                                                                                       SELECT COUNT(*)
//                                                                                       FROM sa_bans
//                                                                                       JOIN sa_players_ips ON sa_bans.player_steamid = sa_players_ips.steamid
//                                                                                       WHERE sa_bans.status = 'ACTIVE'
//                                                                                       AND sa_players_ips.address = @PlayerIP
//                                                                                       AND NOT EXISTS (
//                                                                                           SELECT 1 
//                                                                                           FROM sa_bans 
//                                                                                           WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
//                                                                                           AND status = 'ACTIVE'
//                                                                                           AND (duration = 0 OR ends > @CurrentTime)
//                                                                                           AND server_id = @ServerId
//                                                                                       )
//                                                                                   ), 0) AS TotalBanCount;
//                                                                               """;
//             }
//             else
//             {
//                 sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode ? """
//                                                                             UPDATE sa_bans
//                                                                             SET player_ip = CASE WHEN player_ip IS NULL THEN @PlayerIP ELSE player_ip END,
//                                                                                 player_name = CASE WHEN player_name IS NULL THEN @PlayerName ELSE player_name END
//                                                                             WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
//                                                                             AND status = 'ACTIVE'
//                                                                             AND (duration = 0 OR ends > @CurrentTime);
//                                                                         
//                                                                             SELECT COUNT(*) FROM sa_bans
//                                                                             WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
//                                                                             AND status = 'ACTIVE'
//                                                                             AND (duration = 0 OR ends > @CurrentTime);
//                                                                         """ : """
//                                                                                   UPDATE sa_bans
//                                                                                   SET player_ip = CASE WHEN player_ip IS NULL THEN @PlayerIP ELSE player_ip END,
//                                                                                       player_name = CASE WHEN player_name IS NULL THEN @PlayerName ELSE player_name END
//                                                                                   WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
//                                                                                   AND status = 'ACTIVE'
//                                                                                   AND (duration = 0 OR ends > @CurrentTime) AND server_id = @ServerId;
//                                                                               
//                                                                                   SELECT COUNT(*) FROM sa_bans
//                                                                                   WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
//                                                                                   AND status = 'ACTIVE'
//                                                                                   AND (duration = 0 OR ends > @CurrentTime) AND server_id = @ServerId;
//                                                                               """;
//             }
//             
//             await using var connection = await database.GetConnectionAsync();
//
//             var parameters = new
//             {
//                 PlayerSteamID = player.SteamId.SteamId64,
//                 PlayerIP = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0 ||
//                            string.IsNullOrEmpty(player.IpAddress) ||
//                            CS2_SimpleAdmin.Instance.Config.OtherSettings.IgnoredIps.Contains(player.IpAddress)
//                     ? null
//                     : player.IpAddress,
//                 PlayerName = !string.IsNullOrEmpty(player.Name) ? player.Name : string.Empty,
//                 CurrentTime = currentTime,
//                 CS2_SimpleAdmin.ServerId
//             };
//
//             banCount = await connection.ExecuteScalarAsync<int>(sql, parameters);
//         }
//         catch (Exception ex)
//         {
//             CS2_SimpleAdmin._logger?.LogError("Unable to check ban status for {PlayerName} ({ExceptionMessage})",
//                 player.Name, ex.Message);
//             return false;
//         }
//
//         return banCount > 0;
//     }
//
//     public async Task<int> GetPlayerBans(PlayerInfo player)
//     {
//         if (database == null) return 0;
//
//         try
//         {
//             string sql;
//
//             sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
//                 ? "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)"
//                 : "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) AND server_id = @serverid";
//
//             int banCount;
//
//             await using var connection = await database.GetConnectionAsync();
//
//             if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType > 0 && !string.IsNullOrEmpty(player.IpAddress))
//             {
//                 banCount = await connection.ExecuteScalarAsync<int>(sql,
//                     new
//                     {
//                         PlayerSteamID = player.SteamId.SteamId64,
//                         PlayerIP = player.IpAddress,
//                         serverid = CS2_SimpleAdmin.ServerId
//                     });
//             }
//             else
//             {
//                 banCount = await connection.ExecuteScalarAsync<int>(sql,
//                     new
//                     {
//                         PlayerSteamID = player.SteamId.SteamId64,
//                         PlayerIP = DBNull.Value,
//                         serverid = CS2_SimpleAdmin.ServerId
//                     });
//             }
//
//             return banCount;
//         }
//         catch { }
//
//         return 0;
//     }

    /// <summary>
    /// Unbans a player based on a pattern match of SteamID or IP address.
    /// </summary>
    /// <param name="playerPattern">Pattern to match against player identifiers (e.g., partial SteamID).</param>
    /// <param name="adminSteamId">SteamID64 of the admin performing the unban.</param>
    /// <param name="reason">Optional reason for the unban. If null or empty, the unban reason is not stored.</param>
public async Task UnbanPlayer(string playerPattern, string adminSteamId, string reason)
{
    if (databaseProvider == null) return;

    if (playerPattern is not { Length: > 1 })
    {
        return;
    }
    try
    {
        await using var connection = await databaseProvider.CreateConnectionAsync();
        var sqlRetrieveBans = databaseProvider.GetUnbanRetrieveBansQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);

        var bans = await connection.QueryAsync(sqlRetrieveBans, new { pattern = playerPattern, serverid = CS2_SimpleAdmin.ServerId });
        var bansList = bans as dynamic[] ?? bans.ToArray();
        if (bansList.Length == 0)
            return;

        var sqlAdminId = databaseProvider.GetUnbanAdminIdQuery();
        var adminId = await connection.ExecuteScalarAsync<int?>(sqlAdminId, new { adminSteamId }) ?? 0;

        foreach (var ban in bansList)
        {
            int banId = ban.id;

            var sqlInsertUnban = databaseProvider.GetInsertUnbanQuery(reason != null);
            var unbanId = await connection.ExecuteScalarAsync<int>(sqlInsertUnban, new { banId, adminId, reason });

            var sqlUpdateBan = databaseProvider.GetUpdateBanStatusQuery();
            await connection.ExecuteAsync(sqlUpdateBan, new { unbanId, banId });
        }
    }
    catch { }
}

    // public async Task CheckOnlinePlayers(List<(string? IpAddress, ulong SteamID, int? UserId, int Slot)> players)
    // {
    //     if (database == null) return;
    //
    //     try
    //     {
    //         await using var connection = await database.GetConnectionAsync();
    //         bool checkIpBans = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType > 0;
    //
    //         var filteredPlayers = players.Where(p => p.UserId.HasValue).ToList();
    //
    //         var steamIds = filteredPlayers.Select(p => p.SteamID).Distinct().ToList();
    //         var ipAddresses = filteredPlayers
    //             .Where(p => !string.IsNullOrEmpty(p.IpAddress))
    //             .Select(p => p.IpAddress)
    //             .Distinct()
    //             .ToList();
    //
    //         var sql = new StringBuilder();
    //         sql.Append("SELECT `player_steamid`, `player_ip` FROM `sa_bans` WHERE `status` = 'ACTIVE' ");
    //
    //         if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
    //         {
    //             sql.Append("AND (player_steamid IN @SteamIDs ");
    //             if (checkIpBans && ipAddresses.Count != 0)
    //             {
    //                 sql.Append("OR player_ip IN @IpAddresses");
    //             }
    //             sql.Append(')');
    //         }
    //         else
    //         {
    //             sql.Append("AND server_id = @ServerId AND (player_steamid IN @SteamIDs ");
    //             if (checkIpBans && ipAddresses.Count != 0)
    //             {
    //                 sql.Append("OR player_ip IN @IpAddresses");
    //             }
    //             sql.Append(')');
    //         }
    //
    //         var bannedPlayers = await connection.QueryAsync<(ulong PlayerSteamID, string PlayerIP)>(
    //             sql.ToString(),
    //             new
    //             {
    //                 SteamIDs = steamIds,
    //                 IpAddresses = checkIpBans ? ipAddresses : [],
    //                 CS2_SimpleAdmin.ServerId
    //             });
    //
    //         var valueTuples = bannedPlayers.ToList();
    //         var bannedSteamIds = valueTuples.Select(b => b.PlayerSteamID).ToHashSet();
    //         var bannedIps = valueTuples.Select(b => b.PlayerIP).ToHashSet();
    //
    //         foreach (var player in filteredPlayers.Where(player => bannedSteamIds.Contains(player.SteamID) ||
    //                                                                (checkIpBans && bannedIps.Contains(player.IpAddress ?? ""))))
    //         {
    //             if (!player.UserId.HasValue || CS2_SimpleAdmin.PlayersInfo[player.SteamID].WaitingForKick) continue;
    //
    //             await Server.NextWorldUpdateAsync(() =>
    //             {
    //                 Helper.KickPlayer(player.UserId.Value, NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKBANADDED);
    //             });
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         CS2_SimpleAdmin._logger?.LogError($"Error checking online players: {ex.Message}");
    //     }
    // }

    /// <summary>
    /// Expires all bans that have passed their end time, including optional cleanup of old IP bans.
    /// </summary>
    public async Task ExpireOldBans()
    {
        if (databaseProvider == null) return;
        var currentTime = Time.ActualDateTime();

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var sql = databaseProvider.GetExpireBansQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);
            await connection.ExecuteAsync(sql, new { currentTime, serverid = CS2_SimpleAdmin.ServerId });
            if (CS2_SimpleAdmin.Instance.Config.OtherSettings.ExpireOldIpBans > 0)
            {
                var ipBansTime = currentTime.AddDays(-CS2_SimpleAdmin.Instance.Config.OtherSettings.ExpireOldIpBans);
                sql = databaseProvider.GetExpireIpBansQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);
                await connection.ExecuteAsync(sql, new { ipBansTime, CS2_SimpleAdmin.ServerId });
            }
        }
        catch (Exception)
        {
            CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired bans");
        }
    }
}