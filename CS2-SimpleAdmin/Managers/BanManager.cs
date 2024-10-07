using CounterStrikeSharp.API;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CS2_SimpleAdminApi;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Text;

namespace CS2_SimpleAdmin.Managers;

internal class BanManager(Database.Database? database)
{
    public async Task BanPlayer(PlayerInfo player, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (database == null) return;
        
        DateTime now = Time.ActualDateTime();
        DateTime futureTime = now.AddMinutes(time);

        await using MySqlConnection connection = await database.GetConnectionAsync();
        try
        {
            const string sql =
                "INSERT INTO `sa_bans` (`player_steamid`, `player_name`, `player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
                               "VALUES (@playerSteamid, @playerName, @playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

            await connection.ExecuteAsync(sql, new
            {
                playerSteamid = player.SteamId.SteamId64.ToString(),
                playerName = player.Name,
                playerIp = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 1 ? player.IpAddress : null,
                adminSteamid = issuer?.SteamId.SteamId64.ToString() ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
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

    public async Task AddBanBySteamid(string playerSteamId, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (database == null) return;

        if (string.IsNullOrEmpty(playerSteamId)) return;

        DateTime now = Time.ActualDateTime();
        DateTime futureTime = now.AddMinutes(time);

        try
        {
            await using MySqlConnection connection = await database.GetConnectionAsync();

            var sql = "INSERT INTO `sa_bans` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
                "VALUES (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

            await connection.ExecuteAsync(sql, new
            {
                playerSteamid = playerSteamId,
                adminSteamid = issuer?.SteamId.SteamId64.ToString() ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
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

    public async Task AddBanByIp(string playerIp, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (database == null) return;

        if (string.IsNullOrEmpty(playerIp)) return;

        DateTime now = Time.ActualDateTime();
        DateTime futureTime = now.AddMinutes(time);

        try
        {
            await using MySqlConnection connection = await database.GetConnectionAsync();

            var sql = "INSERT INTO `sa_bans` (`player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
                "VALUES (@playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

            await connection.ExecuteAsync(sql, new
            {
                playerIp,
                adminSteamid = issuer?.SteamId.SteamId64.ToString() ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
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

    public async Task<bool> IsPlayerBanned(PlayerInfo player)
    {
        if (database == null) return false;

        if (player.IpAddress == null)
        {
            return false;
        }
        
#if DEBUG
        if (CS2_SimpleAdmin._logger != null)
            CS2_SimpleAdmin._logger.LogCritical($"IsPlayerBanned for {player.Name}");
#endif

        int banCount;

        DateTime currentTime = Time.ActualDateTime();

        try
        {
            var sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode ? """
                                                                            SELECT COALESCE((
                                                                                SELECT COUNT(*)
                                                                                FROM sa_bans
                                                                                WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
                                                                                AND status = 'ACTIVE'
                                                                                AND (duration = 0 OR ends > @CurrentTime)
                                                                            ), 0) 
                                                                            + 
                                                                            COALESCE((
                                                                                SELECT COUNT(*)
                                                                                FROM sa_bans
                                                                                JOIN sa_players_ips ON sa_bans.player_steamid = sa_players_ips.steamid
                                                                                WHERE sa_bans.status = 'ACTIVE'
                                                                                AND sa_players_ips.address = @PlayerIP
                                                                                AND NOT EXISTS (
                                                                                    SELECT 1 
                                                                                    FROM sa_bans 
                                                                                    WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) 
                                                                                    AND status = 'ACTIVE'
                                                                                    AND (duration = 0 OR ends > @CurrentTime)
                                                                                )
                                                                            ), 0) AS TotalBanCount;
                                                                        """ : """
                                                                                  SELECT COALESCE((
                                                                                      SELECT COUNT(*)
                                                                                      FROM sa_bans
                                                                                      WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
                                                                                      AND status = 'ACTIVE'
                                                                                      AND (duration = 0 OR ends > @CurrentTime)
                                                                                      AND server_id = @ServerId
                                                                                  ), 0) 
                                                                                  + 
                                                                                  COALESCE((
                                                                                      SELECT COUNT(*)
                                                                                      FROM sa_bans
                                                                                      JOIN sa_players_ips ON sa_bans.player_steamid = sa_players_ips.steamid
                                                                                      WHERE sa_bans.status = 'ACTIVE'
                                                                                      AND sa_players_ips.address = @PlayerIP
                                                                                      AND NOT EXISTS (
                                                                                          SELECT 1 
                                                                                          FROM sa_bans 
                                                                                          WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
                                                                                          AND status = 'ACTIVE'
                                                                                          AND (duration = 0 OR ends > @CurrentTime)
                                                                                          AND server_id = @ServerId
                                                                                      )
                                                                                  ), 0) AS TotalBanCount;
                                                                              """;

            await using var connection = await database.GetConnectionAsync();

            var parameters = new
            {
                PlayerSteamID = player.SteamId.SteamId64.ToString(),
                PlayerIP = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0 ||
                           string.IsNullOrEmpty(player.IpAddress)
                    ? null
                    : player.IpAddress,
                PlayerName = !string.IsNullOrEmpty(player.Name) ? player.Name : string.Empty,
                CurrentTime = currentTime,
                CS2_SimpleAdmin.ServerId
            };

            banCount = await connection.ExecuteScalarAsync<int>(sql, parameters);
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError("Unable to check ban status for {PlayerName} ({ExceptionMessage})",
                player.Name, ex.Message);
            return false;
        }

        return banCount > 0;
    }

    public async Task<int> GetPlayerBans(PlayerInfo player)
    {
        if (database == null) return 0;

        try
        {
            string sql;

            sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)"
                : "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) AND server_id = @serverid";

            int banCount;

            await using var connection = await database.GetConnectionAsync();

            if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType > 0 && !string.IsNullOrEmpty(player.IpAddress))
            {
                banCount = await connection.ExecuteScalarAsync<int>(sql,
                    new
                    {
                        PlayerSteamID = player.SteamId.SteamId64.ToString(),
                        PlayerIP = player.IpAddress,
                        serverid = CS2_SimpleAdmin.ServerId
                    });
            }
            else
            {
                banCount = await connection.ExecuteScalarAsync<int>(sql,
                    new
                    {
                        PlayerSteamID = player.SteamId.SteamId64.ToString(),
                        PlayerIP = DBNull.Value,
                        serverid = CS2_SimpleAdmin.ServerId
                    });
            }

            return banCount;
        }
        catch { }

        return 0;
    }

    public async Task UnbanPlayer(string playerPattern, string adminSteamId, string reason)
    {
        if (database == null) return;

        if (playerPattern is not { Length: > 1 })
        {
            return;
        }
        try
        {
            await using var connection = await database.GetConnectionAsync();

            var sqlRetrieveBans = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? "SELECT id FROM sa_bans WHERE (player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern) AND status = 'ACTIVE'"
                : "SELECT id FROM sa_bans WHERE (player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern) AND status = 'ACTIVE' AND server_id = @serverid";

            var bans = await connection.QueryAsync(sqlRetrieveBans, new { pattern = playerPattern, serverid = CS2_SimpleAdmin.ServerId });

            var bansList = bans as dynamic[] ?? bans.ToArray();
            if (bansList.Length == 0)
                return;

            const string sqlAdmin = "SELECT id FROM sa_admins WHERE player_steamid = @adminSteamId";
            var sqlInsertUnban = "INSERT INTO sa_unbans (ban_id, admin_id, reason) VALUES (@banId, @adminId, @reason); SELECT LAST_INSERT_ID();";

            var sqlAdminId = await connection.ExecuteScalarAsync<int?>(sqlAdmin, new { adminSteamId });
            var adminId = sqlAdminId ?? 0;

            foreach (var ban in bansList)
            {
                int banId = ban.id;
                int? unbanId;

                if (reason != null)
                {
                    unbanId = await connection.ExecuteScalarAsync<int>(sqlInsertUnban, new { banId, adminId, reason });
                }
                else
                {
                    sqlInsertUnban = "INSERT INTO sa_unbans (ban_id, admin_id) VALUES (@banId, @adminId); SELECT LAST_INSERT_ID();";
                    unbanId = await connection.ExecuteScalarAsync<int>(sqlInsertUnban, new { banId, adminId });
                }

                const string sqlUpdateBan = "UPDATE sa_bans SET status = 'UNBANNED', unban_id = @unbanId WHERE id = @banId";
                await connection.ExecuteAsync(sqlUpdateBan, new { unbanId, banId });
            }

        }
        catch { }
    }

    public async Task CheckOnlinePlayers(List<(string? IpAddress, ulong SteamID, int? UserId, int Slot)> players)
    {
        if (database == null) return;

        try
        {
            await using var connection = await database.GetConnectionAsync();
            bool checkIpBans = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType > 0;

            var filteredPlayers = players.Where(p => p.UserId.HasValue).ToList();

            var steamIds = filteredPlayers.Select(p => p.SteamID).Distinct().ToList();
            var ipAddresses = filteredPlayers
                .Where(p => !string.IsNullOrEmpty(p.IpAddress))
                .Select(p => p.IpAddress)
                .Distinct()
                .ToList();

            var sql = new StringBuilder();
            sql.Append("SELECT `player_steamid`, `player_ip` FROM `sa_bans` WHERE `status` = 'ACTIVE' ");

            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                sql.Append("AND (player_steamid IN @SteamIDs ");
                if (checkIpBans && ipAddresses.Count != 0)
                {
                    sql.Append("OR player_ip IN @IpAddresses");
                }
                sql.Append(')');
            }
            else
            {
                sql.Append("AND server_id = @ServerId AND (player_steamid IN @SteamIDs ");
                if (checkIpBans && ipAddresses.Count != 0)
                {
                    sql.Append("OR player_ip IN @IpAddresses");
                }
                sql.Append(')');
            }

            var bannedPlayers = await connection.QueryAsync<(ulong PlayerSteamID, string PlayerIP)>(
                sql.ToString(),
                new
                {
                    SteamIDs = steamIds,
                    IpAddresses = checkIpBans ? ipAddresses : [],
                    ServerId = CS2_SimpleAdmin.ServerId
                });

            var valueTuples = bannedPlayers.ToList();
            var bannedSteamIds = valueTuples.Select(b => b.PlayerSteamID).ToHashSet();
            var bannedIps = valueTuples.Select(b => b.PlayerIP).ToHashSet();

            foreach (var player in filteredPlayers.Where(player => bannedSteamIds.Contains(player.SteamID) ||
                                                                   (checkIpBans && bannedIps.Contains(player.IpAddress ?? ""))))
            {
                if (!player.UserId.HasValue) continue;

                await Server.NextFrameAsync(() =>
                {
                    Helper.KickPlayer(player.UserId.Value, NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKBANADDED);
                });
            }
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError($"Error checking online players: {ex.Message}");
        }
    }

    public async Task ExpireOldBans()
    {
        if (database == null) return;
        var currentTime = Time.ActualDateTime();

        try
        {
            await using var connection = await database.GetConnectionAsync();
            /*
			string sql = "";
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			sql = "UPDATE sa_bans SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.UtcNow });
			*/

            string sql;

            sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode ? """
                                                                    
                                                                    				UPDATE sa_bans
                                                                    				SET
                                                                    					status = 'EXPIRED'
                                                                    				WHERE
                                                                    					status = 'ACTIVE'
                                                                    					AND
                                                                    					`duration` > 0
                                                                    					AND
                                                                    					ends <= @currentTime
                                                                    """ : """
                                                                          
                                                                          				UPDATE sa_bans
                                                                          				SET
                                                                          					status = 'EXPIRED'
                                                                          				WHERE
                                                                          					status = 'ACTIVE'
                                                                          					AND
                                                                          					`duration` > 0
                                                                          					AND
                                                                          					ends <= @currentTime
                                                                          					AND server_id = @serverid
                                                                          """;

            await connection.ExecuteAsync(sql, new { currentTime, serverid = CS2_SimpleAdmin.ServerId });

            if (CS2_SimpleAdmin.Instance.Config.OtherSettings.ExpireOldIpBans > 0)
            {
                var ipBansTime = currentTime.AddDays(-CS2_SimpleAdmin.Instance.Config.OtherSettings.ExpireOldIpBans);
                sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode ? """
                                                                        
                                                                        				UPDATE sa_bans
                                                                        				SET
                                                                        					player_ip = NULL
                                                                        				WHERE
                                                                        					status = 'ACTIVE'
                                                                        					AND
                                                                        					ends <= @ipBansTime
                                                                        """ : """
                                                                              
                                                                              				UPDATE sa_bans
                                                                              				SET
                                                                              					player_ip = NULL
                                                                              				WHERE
                                                                              					status = 'ACTIVE'
                                                                              					AND
                                                                              					ends <= @ipBansTime
                                                                              					AND server_id = @serverid
                                                                              """;

                await connection.ExecuteAsync(sql, new { ipBansTime, CS2_SimpleAdmin.ServerId });
            }
        }
        catch (Exception)
        {
            CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired bans");
        }
    }
}