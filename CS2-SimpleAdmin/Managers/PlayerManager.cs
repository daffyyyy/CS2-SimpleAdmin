using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CS2_SimpleAdminApi;
using Dapper;
using Microsoft.Extensions.Logging;
using ZLinq;

namespace CS2_SimpleAdmin.Managers;

public class PlayerManager
{
    private readonly CS2_SimpleAdminConfig _config = CS2_SimpleAdmin.Instance.Config;

    public void LoadPlayerData(CCSPlayerController player, bool fullConnect = false)
    {
        if (player.IsBot || string.IsNullOrEmpty(player.IpAddress) || player.IpAddress.Contains("127.0.0.1"))
            return;
        
        if (!player.UserId.HasValue)
        {
            Helper.KickPlayer(player, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_INVALIDCONNECTION);
            return;
        }

        var ipAddress = player.IpAddress?.Split(":")[0];
        CS2_SimpleAdmin.PlayersInfo[player.UserId.Value] =
            new PlayerInfo(player.UserId.Value, player.Slot, new SteamID(player.SteamID), player.PlayerName, ipAddress);
        

        // if (!player.UserId.HasValue)
        // {
        //     Helper.KickPlayer(player, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_INVALIDCONNECTION);
        //     return;
        // }
        
        var userId = player.UserId.Value;
        if (!CS2_SimpleAdmin.PlayersInfo.ContainsKey(userId))
        {
            Helper.KickPlayer(userId, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_INVALIDCONNECTION);
        }
        
        var steamId64 = CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64;
        var steamId = steamId64.ToString();
        
        if (CS2_SimpleAdmin.Database == null) return;

        // Perform asynchronous database operations within a single method
        Task.Run(async () =>
        {
            var isBanned = CS2_SimpleAdmin.Instance.CacheManager != null && CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType switch
            {
                0 => CS2_SimpleAdmin.Instance.CacheManager.IsPlayerBanned(steamId, null),
                _ => CS2_SimpleAdmin.Instance.Config.OtherSettings.CheckMultiAccountsByIp
                    ? CS2_SimpleAdmin.Instance.CacheManager.IsPlayerOrAnyIpBanned(steamId64, ipAddress)
                    : CS2_SimpleAdmin.Instance.CacheManager.IsPlayerBanned(steamId, ipAddress)
            };

            if (isBanned)
            {
                // Kick the player if banned
                await Server.NextFrameAsync(() =>
                {
                    if (!player.UserId.HasValue) return;
                    Helper.KickPlayer(userId, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
                });

                return;
            }

            if (_config.OtherSettings.CheckMultiAccountsByIp && ipAddress != null)
            {
                try
                {
                    if (CS2_SimpleAdmin.Instance.CacheManager != null && CS2_SimpleAdmin.Instance.CacheManager.HasIpForPlayer(
                            CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64, ipAddress))
                    {
                        await using var connection = await CS2_SimpleAdmin.Database.GetConnectionAsync();
                        
                        const string updateQuery = """
                                                   UPDATE `sa_players_ips`
                                                   SET used_at = CURRENT_TIMESTAMP
                                                   WHERE steamid = @SteamID AND address = @IPAddress;
                                                   """;
                        await connection.ExecuteAsync(updateQuery, new
                        {
                            SteamID = CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64,
                            IPAddress = IpHelper.IpToUint(ipAddress)
                        });
                    }
                    else
                    {
                        await using var connection = await CS2_SimpleAdmin.Database.GetConnectionAsync();

                        const string selectQuery =
                            "SELECT COUNT(*) FROM `sa_players_ips` WHERE steamid = @SteamID AND address = @IPAddress;";
                        var recordExists = await connection.ExecuteScalarAsync<int>(selectQuery, new
                        {
                            SteamID = CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64,
                            IPAddress = IpHelper.IpToUint(ipAddress)
                        });

                        if (recordExists > 0)
                        {
                            const string updateQuery = """
                                                       UPDATE `sa_players_ips`
                                                       SET used_at = CURRENT_TIMESTAMP
                                                       WHERE steamid = @SteamID AND address = @IPAddress;
                                                       """;
                            await connection.ExecuteAsync(updateQuery, new
                            {
                                SteamID = CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64,
                                IPAddress = IpHelper.IpToUint(ipAddress)
                            });
                        }
                        else
                        {
                            const string insertQuery = """
                                                       INSERT INTO `sa_players_ips` (steamid, address, used_at)
                                                       VALUES (@SteamID, @IPAddress, CURRENT_TIMESTAMP);
                                                       """;
                            await connection.ExecuteAsync(insertQuery, new
                            {
                                SteamID = CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64,
                                IPAddress = IpHelper.IpToUint(ipAddress)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    CS2_SimpleAdmin._logger?.LogError(
                        $"Unable to save ip address for {CS2_SimpleAdmin.PlayersInfo[userId].Name} ({ipAddress}) {ex.Message}");   
                }

                // Get all accounts associated to the player (ip address)
                CS2_SimpleAdmin.PlayersInfo[userId].AccountsAssociated =
                    CS2_SimpleAdmin.Instance.CacheManager?.GetAccountsByIp(ipAddress).AsValueEnumerable().Select(x => (x.SteamId, x.PlayerName)).ToList() ?? [];
            }

            try                                  
            {
                // var isBanned = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0
                //     ? CS2_SimpleAdmin.Instance.CacheManager.IsPlayerBanned(
                //         CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64.ToString(), null)
                //     : CS2_SimpleAdmin.Instance.Config.OtherSettings.CheckMultiAccountsByIp
                //         ? CS2_SimpleAdmin.Instance.CacheManager.IsPlayerOrAnyIpBanned(CS2_SimpleAdmin
                //             .PlayersInfo[userId].SteamId.SteamId64)
                //         : CS2_SimpleAdmin.Instance.CacheManager.IsPlayerBanned(CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64.ToString(), ipAddress);
                
                if (fullConnect || !fullConnect) // Temp skip
                {
                    var warns = await CS2_SimpleAdmin.Instance.WarnManager.GetPlayerWarns(CS2_SimpleAdmin.PlayersInfo[userId], false);
                    var (totalMutes, totalGags, totalSilences) =
                        await CS2_SimpleAdmin.Instance.MuteManager.GetPlayerMutes(CS2_SimpleAdmin.PlayersInfo[userId]);

                    CS2_SimpleAdmin.PlayersInfo[userId].TotalBans = CS2_SimpleAdmin.Instance.CacheManager?.GetPlayerBansBySteamId(CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64.ToString()).Count ?? 0;
                    CS2_SimpleAdmin.PlayersInfo[userId].TotalMutes = totalMutes;
                    CS2_SimpleAdmin.PlayersInfo[userId].TotalGags = totalGags;
                    CS2_SimpleAdmin.PlayersInfo[userId].TotalSilences = totalSilences;
                    CS2_SimpleAdmin.PlayersInfo[userId].TotalWarns = warns.Count;

                // Check if the player is muted
                    var activeMutes = await CS2_SimpleAdmin.Instance.MuteManager.IsPlayerMuted(CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64.ToString());

                    if (activeMutes.Count > 0)
                    {
                        foreach (var mute in activeMutes)
                        {
                            string muteType = mute.type;
                            DateTime ends = mute.ends;
                            int duration = mute.duration;
                            switch (muteType)
                            {
                                // Apply mute penalty based on mute type
                                case "GAG":
                                    PlayerPenaltyManager.AddPenalty(CS2_SimpleAdmin.PlayersInfo[userId].Slot, PenaltyType.Gag, ends, duration);
                                    // if (CS2_SimpleAdmin._localizer != null)
                                    // 	mutesList[PenaltyType.Gag].Add(CS2_SimpleAdmin._localizer["sa_player_penalty_info_active_gag", ends.ToLocalTime().ToString(CultureInfo.CurrentCulture)]);
                                    break;
                                case "MUTE":
                                    PlayerPenaltyManager.AddPenalty(CS2_SimpleAdmin.PlayersInfo[userId].Slot, PenaltyType.Mute, ends, duration);
                                    await Server.NextFrameAsync(() =>
                                    {
                                        player.VoiceFlags = VoiceFlags.Muted;
                                    });
                                    // if (CS2_SimpleAdmin._localizer != null)
                                    // 	mutesList[PenaltyType.Mute].Add(CS2_SimpleAdmin._localizer["sa_player_penalty_info_active_mute", ends.ToLocalTime().ToString(CultureInfo.InvariantCulture)]);
                                    break;
                                default:
                                    PlayerPenaltyManager.AddPenalty(CS2_SimpleAdmin.PlayersInfo[userId].Slot, PenaltyType.Silence, ends, duration);
                                    await Server.NextFrameAsync(() =>
                                    {
                                        player.VoiceFlags = VoiceFlags.Muted;
                                    });
                                    // if (CS2_SimpleAdmin._localizer != null)
                                    // 	mutesList[PenaltyType.Silence].Add(CS2_SimpleAdmin._localizer["sa_player_penalty_info_active_silence", ends.ToLocalTime().ToString(CultureInfo.CurrentCulture)]);
                                    break;
                            }
                        }
                    }
                }

                if (CS2_SimpleAdmin.Instance.Config.OtherSettings.NotifyPenaltiesToAdminOnConnect && fullConnect)
                {
                    var associatedAcccountsChunks = CS2_SimpleAdmin.PlayersInfo[userId].AccountsAssociated.ChunkBy(5).ToList();
                    
                    await Server.NextFrameAsync(() =>
                    {
                        foreach (var admin in Helper.GetValidPlayers()
                                     .Where(p => (AdminManager.PlayerHasPermissions(new SteamID(p.SteamID), "@css/kick") ||
                                                  AdminManager.PlayerHasPermissions(new SteamID(p.SteamID), "@css/ban")) &&
                                                 p.Connected == PlayerConnectedState.PlayerConnected && !CS2_SimpleAdmin.AdminDisabledJoinComms.Contains(p.SteamID)))
                        {
                            if (CS2_SimpleAdmin._localizer != null && admin != player)
                            {
                                admin.SendLocalizedMessage(CS2_SimpleAdmin._localizer, "sa_admin_penalty_info",
                                    player.PlayerName,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalBans,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalGags,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalMutes,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalSilences,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalWarns
                                );

                                foreach (var chunk in associatedAcccountsChunks)
                                {
                                    admin.SendLocalizedMessage(CS2_SimpleAdmin._localizer, "sa_admin_associated_accounts",
                                        player.PlayerName,
                                        string.Join(", ", 
                                            chunk.Select(a => $"{a.PlayerName} ({a.SteamId})"))
                                    );
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                CS2_SimpleAdmin._logger?.LogError("Error processing player connection: {exception}", ex.Message);
            }
        });

        if (CS2_SimpleAdmin.RenamedPlayers.TryGetValue(player.SteamID, out var name))
        {
            player.Rename(name);
        }
    }

    public void CheckPlayersTimer()
    {
        CS2_SimpleAdmin.Instance.AddTimer(0.1f, () =>
        {
            if (CS2_SimpleAdmin.GravityPlayers.Count <= 0) return;

            foreach (var value in CS2_SimpleAdmin.GravityPlayers.Where(value => value.Key is
                         { IsValid: true, Connected: PlayerConnectedState.PlayerConnected } || value.Key.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE))
            {
                value.Key.SetGravity(value.Value);
            }
        }, TimerFlags.REPEAT);
        
        CS2_SimpleAdmin.Instance.AddTimer(61.0f, () =>
        {
#if DEBUG
            CS2_SimpleAdmin._logger?.LogCritical("[OnMapStart] Expired check");
#endif
            if (CS2_SimpleAdmin.Database == null)
                return;
            
            var tempPlayers = Helper.GetValidPlayers()
                .Select(p => new
                {
                    p.SteamID, p.IpAddress, p.UserId, p.Slot,
                })
                .ToList();
            
            _ = Task.Run(async () =>
            {
                try
                {
                    var expireTasks = new Task[]
                    {
                        CS2_SimpleAdmin.Instance.BanManager.ExpireOldBans(),
                        CS2_SimpleAdmin.Instance.MuteManager.ExpireOldMutes(),
                        CS2_SimpleAdmin.Instance.WarnManager.ExpireOldWarns(),
                        CS2_SimpleAdmin.Instance.CacheManager?.RefreshCacheAsync() ?? Task.CompletedTask,
                        CS2_SimpleAdmin.Instance.PermissionManager.DeleteOldAdmins()
                    };

                    await Task.WhenAll(expireTasks);
                }
                catch (Exception ex)
                {
                    CS2_SimpleAdmin._logger?.LogError($"Error processing players timer tasks: {ex.Message}");

                    if (ex is AggregateException aggregate)
                    {
                        foreach (var inner in aggregate.InnerExceptions)
                        {
                            CS2_SimpleAdmin._logger?.LogError($"Inner exception: {inner.Message}");
                        }
                    }
                }
                
                var bannedPlayers = tempPlayers.AsValueEnumerable()
                    .Where(player =>
                    {
                        return CS2_SimpleAdmin.Instance.CacheManager != null && CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType switch
                        {
                            0 => CS2_SimpleAdmin.Instance.CacheManager.IsPlayerBanned(player.SteamID.ToString(), null),
                            _ => 
                                CS2_SimpleAdmin.Instance.CacheManager.IsPlayerBanned(player.SteamID.ToString(), player.IpAddress?.Split(":")[0])
                        };
                    })
                    .ToList();
                
                foreach (var player in bannedPlayers)
                {
                    if (player.UserId.HasValue)
                        await Server.NextFrameAsync(() => Helper.KickPlayer((int)player.UserId, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED));
                }
                
                var onlinePlayers = tempPlayers.AsValueEnumerable().Select(player => (player.SteamID, player.UserId, player.Slot)).ToList();
                if (tempPlayers.Count == 0 || onlinePlayers.Count == 0) return;
                if (_config.OtherSettings.TimeMode == 0)
                {
                    await CS2_SimpleAdmin.Instance.MuteManager.CheckOnlineModeMutes(onlinePlayers);
                }
            });
            
            try
            {
                var players = Helper.GetValidPlayers();
                var penalizedSlots = players
                    .Where(player => PlayerPenaltyManager.IsSlotInPenalties(player.Slot))
                    .Select(player => new 
                    { 
                        Player = player, 
                        IsMuted = PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Mute, out _), 
                        IsSilenced = PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence, out _), 
                        IsGagged = PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag, out _) 
                    });

                foreach (var entry in penalizedSlots)
                {
                    // If the player is not muted or silenced, set voice flags to normal
                    if (!entry.IsMuted && !entry.IsSilenced)
                    {
                        entry.Player.VoiceFlags = VoiceFlags.Normal;
                    }
                }

                PlayerPenaltyManager.RemoveExpiredPenalties();
            }
            catch (Exception ex)
            {
                CS2_SimpleAdmin._logger?.LogError($"Unable to remove old penalties: {ex.Message}");
            }

        }, TimerFlags.REPEAT);
    }
}