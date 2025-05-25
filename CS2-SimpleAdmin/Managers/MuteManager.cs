using CS2_SimpleAdminApi;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

internal class MuteManager(Database.Database? database)
{
    public async Task<int?> MutePlayer(PlayerInfo player, PlayerInfo? issuer, string reason, int time = 0, int type = 0)
    {
        if (database == null) return null;

        var now = Time.ActualDateTime();
        var futureTime = now.AddMinutes(time);

        var muteType = type switch
        {
            1 => "MUTE",
            2 => "SILENCE",
            _ => "GAG"
        };

        try
        {
            await using var connection = await database.GetConnectionAsync();
            const string sql = """
                               
                                               INSERT INTO `sa_mutes` 
                                               (`player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`, `server_id`) 
                                               VALUES 
                                               (@playerSteamid, @playerName, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @type, @serverid);
                                               SELECT LAST_INSERT_ID();
                               """;

            var muteId = await connection.ExecuteScalarAsync<int?>(sql, new
            {
                playerSteamid = player.SteamId.SteamId64.ToString(),
                playerName = player.Name,
                adminSteamid = issuer?.SteamId.SteamId64.ToString() ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                muteReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                type = muteType,
                serverid = CS2_SimpleAdmin.ServerId
            });

            return muteId;
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError(ex.Message);
            return null;
        }
    }

    public async Task<int?> AddMuteBySteamid(string playerSteamId, PlayerInfo? issuer, string reason, int time = 0, int type = 0)
    {
        if (database == null) return null;
        if (string.IsNullOrEmpty(playerSteamId)) return null;

        var now = Time.ActualDateTime();
        var futureTime = now.AddMinutes(time);

        var muteType = type switch
        {
            1 => "MUTE",
            2 => "SILENCE",
            _ => "GAG"
        };

        try
        {
            await using var connection = await database.GetConnectionAsync();
            const string sql = """
                               
                                               INSERT INTO `sa_mutes` 
                                               (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`, `server_id`) 
                                               VALUES 
                                               (@playerSteamid, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @type, @serverid);
                                               SELECT LAST_INSERT_ID();
                               """;

            var muteId = await connection.ExecuteScalarAsync<int?>(sql, new
            {
                playerSteamid = playerSteamId,
                adminSteamid = issuer?.SteamId.SteamId64.ToString() ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                muteReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                type = muteType,
                serverid = CS2_SimpleAdmin.ServerId
            });

            return muteId;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<dynamic>> IsPlayerMuted(string steamId)
    {
        if (database == null) return [];

        if (string.IsNullOrEmpty(steamId))
        {
            return [];
        }

#if DEBUG
        if (CS2_SimpleAdmin._logger != null)
            CS2_SimpleAdmin._logger.LogCritical($"IsPlayerMuted for {steamId}");
#endif

        try
        {
            await using var connection = await database.GetConnectionAsync();
            var currentTime = Time.ActualDateTime();
            var sql = "";

            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                sql = CS2_SimpleAdmin.Instance.Config.OtherSettings.TimeMode == 1
                    ? "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)"
                    : "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR duration > COALESCE(passed, 0))";
            }
            else
            {
                sql = CS2_SimpleAdmin.Instance.Config.OtherSettings.TimeMode == 1
                    ? "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime) AND server_id = @serverid"
                    : "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR duration > COALESCE(passed, 0)) AND server_id = @serverid";

            }

            var parameters = new { PlayerSteamID = steamId, CurrentTime = currentTime, serverid = CS2_SimpleAdmin.ServerId };
            var activeMutes = (await connection.QueryAsync(sql, parameters)).ToList();
            return activeMutes;
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<(int TotalMutes, int TotalGags, int TotalSilences)> GetPlayerMutes(PlayerInfo playerInfo)
    {
        if (database == null) return (0,0,0);

        try
        {
            await using var connection = await database.GetConnectionAsync();

            var sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? """
                  SELECT
                      COUNT(CASE WHEN type = 'MUTE' THEN 1 END) AS TotalMutes,
                      COUNT(CASE WHEN type = 'GAG' THEN 1 END) AS TotalGags,
                      COUNT(CASE WHEN type = 'SILENCE' THEN 1 END) AS TotalSilences
                  FROM sa_mutes
                  WHERE player_steamid = @PlayerSteamID;
                  """
                : """
                  SELECT
                      COUNT(CASE WHEN type = 'MUTE' THEN 1 END) AS TotalMutes,
                      COUNT(CASE WHEN type = 'GAG' THEN 1 END) AS TotalGags,
                      COUNT(CASE WHEN type = 'SILENCE' THEN 1 END) AS TotalSilences
                  FROM sa_mutes
                  WHERE player_steamid = @PlayerSteamID AND server_id = @ServerId;
                  """;

            var result = await connection.QuerySingleAsync<(int TotalMutes, int TotalGags, int TotalSilences)>(sql, new
            {
                PlayerSteamID = playerInfo.SteamId.SteamId64.ToString(),
                CS2_SimpleAdmin.ServerId
            });

            return result;
        }
        catch (Exception)
        {
            return (0, 0, 0);
        }
    }
    
    public async Task CheckOnlineModeMutes(List<(ulong SteamID, int? UserId, int Slot)> players)
    {
        if (database == null) return;

        try
        {
            const int batchSize = 20;
            await using var connection = await database.GetConnectionAsync();

            var sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? "UPDATE `sa_mutes` SET passed = COALESCE(passed, 0) + 1 WHERE (player_steamid = @PlayerSteamID) AND duration > 0 AND status = 'ACTIVE'"
                : "UPDATE `sa_mutes` SET passed = COALESCE(passed, 0) + 1 WHERE (player_steamid = @PlayerSteamID) AND duration > 0 AND status = 'ACTIVE' AND server_id = @serverid";

            for (var i = 0; i < players.Count; i += batchSize)
            {
                var batch = players.Skip(i).Take(batchSize);
                var parametersList = new List<object>();

                foreach (var (steamId, _, _) in batch)
                {
                    parametersList.Add(new { PlayerSteamID = steamId, serverid = CS2_SimpleAdmin.ServerId });
                }

                await connection.ExecuteAsync(sql, parametersList);
            }

            sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? "SELECT * FROM `sa_mutes` WHERE player_steamid = @PlayerSteamID AND passed >= duration AND duration > 0 AND status = 'ACTIVE'"
                : "SELECT * FROM `sa_mutes` WHERE player_steamid = @PlayerSteamID AND passed >= duration AND duration > 0 AND status = 'ACTIVE' AND server_id = @serverid";


            foreach (var (steamId, _, slot) in players)
            {
                var muteRecords = await connection.QueryAsync(sql, new { PlayerSteamID = steamId, serverid = CS2_SimpleAdmin.ServerId });

                foreach (var muteRecord in muteRecords)
                {
                    DateTime endDateTime = muteRecord.ends;
                    PlayerPenaltyManager.RemovePenaltiesByDateTime(slot, endDateTime);
                }

            }
        }
        catch { }
    }

    public async Task UnmutePlayer(string playerPattern, string adminSteamId, string reason, int type = 0)
    {
        if (database == null) return;

        if (playerPattern.Length <= 1)
        {
            return;
        }

        try
        {
            await using var connection = await database.GetConnectionAsync();

            var muteType = type switch
            {
                1 => "MUTE",
                2 => "SILENCE",
                _ => "GAG"
            };

            string sqlRetrieveMutes;

            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                sqlRetrieveMutes = "SELECT id FROM sa_mutes WHERE (player_steamid = @pattern OR player_name = @pattern) AND " +
                    "type = @muteType AND status = 'ACTIVE'";
            }
            else
            {
                sqlRetrieveMutes = "SELECT id FROM sa_mutes WHERE (player_steamid = @pattern OR player_name = @pattern) AND " +
                    "type = @muteType AND status = 'ACTIVE' AND server_id = @serverid";
            }

            var mutes = await connection.QueryAsync(sqlRetrieveMutes, new { pattern = playerPattern, muteType, serverid = CS2_SimpleAdmin.ServerId });

            var mutesList = mutes as dynamic[] ?? mutes.ToArray();
            if (mutesList.Length == 0)
                return;

            const string sqlAdmin = "SELECT id FROM sa_admins WHERE player_steamid = @adminSteamId";
            var sqlInsertUnmute = "INSERT INTO sa_unmutes (mute_id, admin_id, reason) VALUES (@muteId, @adminId, @reason); SELECT LAST_INSERT_ID();";

            var sqlAdminId = await connection.ExecuteScalarAsync<int?>(sqlAdmin, new { adminSteamId });
            var adminId = sqlAdminId ?? 0;

            foreach (var mute in mutesList)
            {
                int muteId = mute.id;
                int? unmuteId;

                // Insert into sa_unmutes
                if (reason != null)
                {
                    unmuteId = await connection.ExecuteScalarAsync<int>(sqlInsertUnmute, new { muteId, adminId, reason });
                }
                else
                {
                    sqlInsertUnmute = "INSERT INTO sa_unmutes (muteId, admin_id) VALUES (@muteId, @adminId); SELECT LAST_INSERT_ID();";
                    unmuteId = await connection.ExecuteScalarAsync<int>(sqlInsertUnmute, new { muteId, adminId });
                }

                // Update sa_mutes to set unmute_id
                const string sqlUpdateMute = "UPDATE sa_mutes SET status = 'UNMUTED', unmute_id = @unmuteId WHERE id = @muteId";
                await connection.ExecuteAsync(sqlUpdateMute, new { unmuteId, muteId });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task ExpireOldMutes()
    {
        if (database == null) return;

        try
        {
            await using var connection = await database.GetConnectionAsync();
            string sql;

            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                sql = CS2_SimpleAdmin.Instance.Config.OtherSettings.TimeMode == 1
                    ? "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime"
                    : "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND `passed` >= `duration`";
            }
            else
            {
                sql = CS2_SimpleAdmin.Instance.Config.OtherSettings.TimeMode == 1
                    ? "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime AND server_id = @serverid"
                    : "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND `passed` >= `duration` AND server_id = @serverid";
            }

            await connection.ExecuteAsync(sql, new { CurrentTime = Time.ActualDateTime(), serverid = CS2_SimpleAdmin.ServerId });
        }
        catch (Exception)
        {
            CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired mutes");
        }
    }
}