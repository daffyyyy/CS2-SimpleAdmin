using CS2_SimpleAdmin.Database;
using CS2_SimpleAdminApi;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

internal class MuteManager(IDatabaseProvider? databaseProvider)
{
    /// <summary>
    /// Adds a mute entry for a specified player with detailed information.
    /// </summary>
    /// <param name="player">Player to be muted.</param>
    /// <param name="issuer">Admin issuing the mute; null if issued from console.</param>
    /// <param name="reason">Reason for muting the player.</param>
    /// <param name="time">Duration of the mute in minutes. Zero means permanent mute.</param>
    /// <param name="type">Mute type: 0 = GAG, 1 = MUTE, 2 = SILENCE.</param>
    /// <returns>Mute ID if successfully added, otherwise null.</returns>
    public async Task<int?> MutePlayer(PlayerInfo player, PlayerInfo? issuer, string reason, int time = 0, int type = 0)
    {
        if (databaseProvider == null) return null;

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
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var sql = databaseProvider.GetAddMuteQuery(true);

            var muteId = await connection.ExecuteScalarAsync<int?>(sql, new
            {
                playerSteamid = player.SteamId.SteamId64,
                playerName = player.Name,
                adminSteamid = issuer?.SteamId.SteamId64 ?? 0,
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

    /// <summary>
    /// Adds a mute entry for a offline player identified by their SteamID.
    /// </summary>
    /// <param name="playerSteamId">SteamID64 of the player to mute.</param>
    /// <param name="issuer">Admin issuing the mute; can be null if from console.</param>
    /// <param name="reason">Reason for the mute.</param>
    /// <param name="time">Mute duration in minutes; 0 for permanent.</param>
    /// <param name="type">Mute type: 0 = GAG, 1 = MUTE, 2 = SILENCE.</param>
    /// <returns>Mute ID if successful, otherwise null.</returns>
    public async Task<int?> AddMuteBySteamid(ulong playerSteamId, PlayerInfo? issuer, string reason, int time = 0, int type = 0)
    {
        if (databaseProvider == null) return null;

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
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var sql = databaseProvider.GetAddMuteQuery(false);
            
            var muteId = await connection.ExecuteScalarAsync<int?>(sql, new
            {
                playerSteamid = playerSteamId,
                adminSteamid = issuer?.SteamId.SteamId64 ?? 0,
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

    /// <summary>
    /// Checks if a player with the given SteamID currently has any active mutes.
    /// </summary>
    /// <param name="steamId">SteamID64 of the player to check.</param>
    /// <returns>List of active mute records; empty list if none or on error.</returns>
    public async Task<List<dynamic>> IsPlayerMuted(string steamId)
    {
        if (databaseProvider == null) return [];

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
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var currentTime = Time.ActualDateTime();
            
            var sql = databaseProvider.GetIsMutedQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode, CS2_SimpleAdmin.Instance.Config.OtherSettings.TimeMode);
            
            var parameters = new { PlayerSteamID = steamId, CurrentTime = currentTime, serverid = CS2_SimpleAdmin.ServerId };
            var activeMutes = (await connection.QueryAsync(sql, parameters)).ToList();
            return activeMutes;
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    /// Retrieves counts of total mutes, gags, and silences for a given player.
    /// </summary>
    /// <param name="playerInfo">Information about the player.</param>
    /// <returns>
    /// Tuple containing total mutes, total gags, and total silences respectively.
    /// Returns zeros if no data or on error.
    /// </returns>
    public async Task<(int TotalMutes, int TotalGags, int TotalSilences)> GetPlayerMutes(PlayerInfo playerInfo)
    {
        if (databaseProvider == null) return (0,0,0);

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetRetrieveMutesQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);
            var result = await connection.QuerySingleAsync<(int TotalMutes, int TotalGags, int TotalSilences)>(sql, new
            {
                PlayerSteamID = playerInfo.SteamId.SteamId64,
                CS2_SimpleAdmin.ServerId
            });

            return result;
        }
        catch (Exception)
        {
            return (0, 0, 0);
        }
    }
    
    /// <summary>
    /// Processes a batch of online players to update their mute status and remove expired penalties.
    /// </summary>
    /// <param name="players">List of tuples containing player SteamID, optional UserID, and slot index.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task CheckOnlineModeMutes(List<(ulong SteamID, int? UserId, int Slot)> players)
    {
        if (databaseProvider == null) return;

        try
        {
            const int batchSize = 20;
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetUpdateMutePassedQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);

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

            sql = databaseProvider.GetCheckExpiredMutesQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);

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

    /// <summary>
    /// Removes active mutes for players matching the specified pattern.
    /// </summary>
    /// <param name="playerPattern">Pattern to match player names or identifiers.</param>
    /// <param name="adminSteamId">SteamID64 of the admin performing the unmute.</param>
    /// <param name="reason">Reason for unmuting the player(s).</param>
    /// <param name="type">Mute type to remove: 0 = GAG, 1 = MUTE, 2 = SILENCE.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task UnmutePlayer(string playerPattern, string adminSteamId, string reason, int type = 0)
    {
        if (databaseProvider == null) return;

        if (playerPattern.Length <= 1)
        {
            return;
        }

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var muteType = type switch
            {
                1 => "MUTE",
                2 => "SILENCE",
                _ => "GAG"
            };

            var sqlRetrieveMutes =
                databaseProvider.GetRetrieveMutesQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode);
            var mutes = await connection.QueryAsync(sqlRetrieveMutes, new { pattern = playerPattern, muteType, serverid = CS2_SimpleAdmin.ServerId });

            var mutesList = mutes as dynamic[] ?? mutes.ToArray();
            if (mutesList.Length == 0)
                return;

            var sqlAdmin = databaseProvider.GetUnmuteAdminIdQuery();
            var sqlInsertUnmute = databaseProvider.GetInsertUnmuteQuery(string.IsNullOrEmpty(reason));

            var sqlAdminId = await connection.ExecuteScalarAsync<int?>(sqlAdmin, new { adminSteamId });
            var adminId = sqlAdminId ?? 0;

            foreach (var mute in mutesList)
            {
                int muteId = mute.id;

                int? unmuteId =
                    await connection.ExecuteScalarAsync<int>(sqlInsertUnmute, new { muteId, adminId, reason });

                var sqlUpdateMute = databaseProvider.GetUpdateMuteStatusQuery();
                await connection.ExecuteAsync(sqlUpdateMute, new { unmuteId, muteId });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    /// <summary>
    /// Expires all old mutes that have passed their duration according to current time.
    /// </summary>
    /// <returns>Task representing the asynchronous expiration operation.</returns>
    public async Task ExpireOldMutes()
    {
        if (databaseProvider == null) return;

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();
            var sql = databaseProvider.GetExpireMutesQuery(CS2_SimpleAdmin.Instance.Config.MultiServerMode, CS2_SimpleAdmin.Instance.Config.OtherSettings.TimeMode);
            await connection.ExecuteAsync(sql, new { CurrentTime = Time.ActualDateTime(), serverid = CS2_SimpleAdmin.ServerId });
        }
        catch (Exception)
        {
            CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired mutes");
        }
    }
}