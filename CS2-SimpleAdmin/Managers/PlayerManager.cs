using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CS2_SimpleAdminApi;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

public class PlayerManager
{
    private readonly CS2_SimpleAdminConfig _config = CS2_SimpleAdmin.Instance.Config;

    public void LoadPlayerData(CCSPlayerController player)
    {
        if (player.IsBot || string.IsNullOrEmpty(player.IpAddress) || player.IpAddress.Contains("127.0.0.1")
                                                   || !player.UserId.HasValue)
            return;

        var ipAddress = player.IpAddress?.Split(":")[0];

        CS2_SimpleAdmin.PlayersInfo[player.UserId.Value] =
            new PlayerInfo(player.UserId.Value, player.Slot, new SteamID(player.SteamID), player.PlayerName, ipAddress);

        var userId = player.UserId.Value;

        // Check if the player's IP or SteamID is in the bannedPlayers list
        if (_config.OtherSettings.BanType > 0 && CS2_SimpleAdmin.BannedPlayers.Contains(ipAddress) ||
            CS2_SimpleAdmin.BannedPlayers.Contains(player.SteamID.ToString()))
        {
            // Kick the player if banned
            if (player.UserId.HasValue)
                Helper.KickPlayer(player.UserId.Value, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
            return;
        }

        if (CS2_SimpleAdmin.Database == null) return;

        // Perform asynchronous database operations within a single method
        Task.Run(async () =>
        {
            try
            {
                await using var connection = await CS2_SimpleAdmin.Database.GetConnectionAsync();
                const string selectQuery = "SELECT COUNT(*) FROM `sa_players_ips` WHERE steamid = @SteamID AND address = @IPAddress;";
                var recordExists = await connection.ExecuteScalarAsync<int>(selectQuery, new
                {
                    SteamID = CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64,
                    IPAddress = ipAddress
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
                        IPAddress = ipAddress
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
                        IPAddress = ipAddress
                    });
                }
            }
            catch (Exception ex)
            {
                CS2_SimpleAdmin._logger?.LogError(
                    $"Unable to save ip address for {CS2_SimpleAdmin.PlayersInfo[userId].Name} ({ipAddress}) {ex.Message}");   
            }

            try
            {
                // Check if the player is banned
                var isBanned = await CS2_SimpleAdmin.Instance.BanManager.IsPlayerBanned(CS2_SimpleAdmin.PlayersInfo[userId]);
                
                if (isBanned)
                {
                    // Add player's IP and SteamID to bannedPlayers list if not already present
                    if (_config.OtherSettings.BanType > 0 && ipAddress != null &&
                        !CS2_SimpleAdmin.BannedPlayers.Contains(ipAddress))
                    {
                        CS2_SimpleAdmin.BannedPlayers.Add(ipAddress);
                    }

                    if (!CS2_SimpleAdmin.BannedPlayers.Contains(CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64.ToString()))
                    {
                        CS2_SimpleAdmin.BannedPlayers.Add(CS2_SimpleAdmin.PlayersInfo[userId].SteamId.SteamId64.ToString());
                    }

                    // Kick the player if banned
                    await Server.NextFrameAsync(() =>
                    {
                        var victim = Utilities.GetPlayerFromUserid(userId);

                        if (victim?.UserId == null) return;

                        if (CS2_SimpleAdmin.UnlockedCommands)
                            Server.ExecuteCommand($"banid 1 {userId}");

                        Helper.KickPlayer(userId, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
                    });

                    return;
                }

                var warns = await CS2_SimpleAdmin.Instance.WarnManager.GetPlayerWarns(CS2_SimpleAdmin.PlayersInfo[userId], false);
                var (totalMutes, totalGags, totalSilences) =
                    await CS2_SimpleAdmin.Instance.MuteManager.GetPlayerMutes(CS2_SimpleAdmin.PlayersInfo[userId]);

                CS2_SimpleAdmin.PlayersInfo[userId].TotalBans =
                    await CS2_SimpleAdmin.Instance.BanManager.GetPlayerBans(CS2_SimpleAdmin.PlayersInfo[userId]);
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

                if (CS2_SimpleAdmin.Instance.Config.OtherSettings.NotifyPenaltiesToAdminOnConnect)
                {
                    await Server.NextFrameAsync(() =>
                    {
                        foreach (var admin in Helper.GetValidPlayers()
                                     .Where(p => (AdminManager.PlayerHasPermissions(p, "@css/kick") ||
                                                  AdminManager.PlayerHasPermissions(p, "@css/ban")) &&
                                                 p.Connected == PlayerConnectedState.PlayerConnected && !CS2_SimpleAdmin.AdminDisabledJoinComms.Contains(p.SteamID)))
                        {
                            if (CS2_SimpleAdmin._localizer != null && admin != player)
                                admin.SendLocalizedMessage(CS2_SimpleAdmin._localizer, "sa_admin_penalty_info",
                                    player.PlayerName,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalBans,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalGags,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalMutes,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalSilences,
                                    CS2_SimpleAdmin.PlayersInfo[userId].TotalWarns
                                );
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                CS2_SimpleAdmin._logger?.LogError($"Error processing player connection: {ex}");
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
            
            foreach (var value in CS2_SimpleAdmin.GravityPlayers)
            {
                if (value.Key is not
                    { IsValid: true, Connected: PlayerConnectedState.PlayerConnected, PawnIsAlive: true })
                    continue;

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
            
            var players = Helper.GetValidPlayers();
            var onlinePlayers = new List<(string? IpAddress, ulong SteamID, int? UserId, int Slot)>();
            // var onlinePlayers = players
            //     .Where(player => player.IpAddress != null)
            //     .Select(player => (player.IpAddress, player.SteamID, player.UserId, player.Slot))
            //     .ToList();

            foreach (var player in players)
            {
                if (player.IpAddress != null)
                    onlinePlayers.Add((player.IpAddress, player.SteamID, player.UserId, player.Slot));
            }
            
            try
            {
                var expireTasks = new[]
                {
                    CS2_SimpleAdmin.Instance.BanManager.ExpireOldBans(),
                    CS2_SimpleAdmin.Instance.MuteManager.ExpireOldMutes(),
                    CS2_SimpleAdmin.Instance.WarnManager.ExpireOldWarns(),
                    CS2_SimpleAdmin.Instance.PermissionManager.DeleteOldAdmins()
                };

                Task.WhenAll(expireTasks).ContinueWith(t =>
                {
                    if (t is not { IsFaulted: true, Exception: not null }) return;
                    
                    foreach (var ex in t.Exception.InnerExceptions)
                    {
                        CS2_SimpleAdmin._logger?.LogError($"Error expiring penalties: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                CS2_SimpleAdmin._logger?.LogError($"Unexpected error: {ex.Message}");
            }
            
            CS2_SimpleAdmin.BannedPlayers.Clear();

            if (onlinePlayers.Count > 0)
            {
                try
                {
                    Task.Run(async () =>
                    {
                        await CS2_SimpleAdmin.Instance.BanManager.CheckOnlinePlayers(onlinePlayers);

                        if (_config.OtherSettings.TimeMode == 0)
                        {
                            await CS2_SimpleAdmin.Instance.MuteManager.CheckOnlineModeMutes(onlinePlayers);
                        }
                    }).ContinueWith(t =>
                    {
                        if (t is not { IsFaulted: true, Exception: not null }) return;
                        
                        foreach (var ex in t.Exception.InnerExceptions)
                        {
                            CS2_SimpleAdmin._logger?.LogError($"Error checking online players: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    CS2_SimpleAdmin._logger?.LogError($"Unexpected error: {ex.Message}");
                }
            }

            if (onlinePlayers.Count <= 0) return;
            
            {
                try
                {
                    var penalizedSlots = players
                        .Where(player => PlayerPenaltyManager.IsSlotInPenalties(player.Slot))
                        .Select(player => new 
                        { 
                            Player = player, 
                            IsMuted = PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Mute), 
                            IsSilenced = PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence), 
                            IsGagged = PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) 
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
            }

        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }
}