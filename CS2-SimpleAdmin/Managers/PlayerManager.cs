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

internal class PlayerManager
{
    private readonly SemaphoreSlim _loadPlayerSemaphore = new(5);
    private readonly CS2_SimpleAdminConfig _config = CS2_SimpleAdmin.Instance.Config;

    /// <summary>
    /// Loads and initializes player data when a client connects.
    /// </summary>
    /// <param name="player">The <see cref="CCSPlayerController"/> instance representing the connecting player.</param>
    /// <param name="fullConnect">
    /// Determines whether to perform a full synchronization of player data.
    /// If true, full checks (bans, IP history, penalties, warns, mutes) will be loaded and applied.
    /// </param>
    /// <remarks>
    /// This method validates the player's identity, checks for bans, updates the IP history table,
    /// loads penalties (mutes/gags/warns), and optionally notifies admin players about the connecting player's penalties.
    /// </remarks>
    public void LoadPlayerData(CCSPlayerController player, bool fullConnect = false)
    {
        if (!player.UserId.HasValue)
        {
            Helper.KickPlayer(player, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_INVALIDCONNECTION);
            return;
        }

        var userId = player.UserId.Value;
        var slot = player.Slot;
        var steamId = player.SteamID;
        var playerName = !string.IsNullOrEmpty(player.PlayerName)
            ? player.PlayerName
            : CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
        var ipAddress = player.IpAddress?.Split(":")[0];

        if (CS2_SimpleAdmin.DatabaseProvider == null || CS2_SimpleAdmin.Instance.CacheManager == null) return;

        Task.Run(async () =>
        {
            try
            {
                await _loadPlayerSemaphore.WaitAsync();

                if (!CS2_SimpleAdmin.PlayersInfo.ContainsKey(steamId))
                {
                    var isBanned = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType switch
                    {
                        0 => CS2_SimpleAdmin.Instance.CacheManager.IsPlayerBanned(playerName, steamId, null),
                        _ => CS2_SimpleAdmin.Instance.Config.OtherSettings.CheckMultiAccountsByIp
                            ? CS2_SimpleAdmin.Instance.CacheManager.IsPlayerOrAnyIpBanned(playerName, steamId,
                                ipAddress)
                            : CS2_SimpleAdmin.Instance.CacheManager.IsPlayerBanned(playerName, steamId, ipAddress)
                    };

                    // CS2_SimpleAdmin._logger?.LogInformation($"Player {playerName} ({steamId} - {ipAddress}) is banned? {isBanned.ToString()}");

                    if (isBanned)
                    {
                        await Server.NextWorldUpdateAsync(() =>
                        {
                            // CS2_SimpleAdmin._logger?.LogInformation($"Kicking {playerName}");
                            Helper.KickPlayer(userId, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
                        });

                        return;
                    }
                }

                if (fullConnect)
                {
                    var playerInfo = new PlayerInfo(userId, slot, new SteamID(steamId), playerName, ipAddress);
                    CS2_SimpleAdmin.PlayersInfo[steamId] = playerInfo;

                    await Server.NextWorldUpdateAsync(() =>
                    {
                        if (!CS2_SimpleAdmin.CachedPlayers.Contains(player))
                            CS2_SimpleAdmin.CachedPlayers.Add(player);
                    });

                    if (_config.OtherSettings.CheckMultiAccountsByIp && ipAddress != null &&
                        CS2_SimpleAdmin.PlayersInfo[steamId] != null)
                    {
                        try
                        {
                            await using var connection = await CS2_SimpleAdmin.DatabaseProvider.CreateConnectionAsync();

                            if (CS2_SimpleAdmin.Instance.CacheManager.HasIpForPlayer(
                                    steamId, ipAddress))
                            {
                                const string updateQuery = """
                                                           UPDATE `sa_players_ips`
                                                           SET used_at = CURRENT_TIMESTAMP,
                                                               name = @playerName
                                                           WHERE steamid = @SteamID AND address = @IPAddress;
                                                           """;
                                await connection.ExecuteAsync(updateQuery, new
                                {
                                    playerName,
                                    SteamID = CS2_SimpleAdmin.PlayersInfo[steamId].SteamId.SteamId64,
                                    IPAddress = IpHelper.IpToUint(ipAddress)
                                });
                            }
                            else
                            {
                                const string selectQuery =
                                    "SELECT COUNT(*) FROM `sa_players_ips` WHERE steamid = @SteamID AND address = @IPAddress;";
                                var recordExists = await connection.ExecuteScalarAsync<int>(selectQuery, new
                                {
                                    SteamID = CS2_SimpleAdmin.PlayersInfo[steamId].SteamId.SteamId64,
                                    IPAddress = IpHelper.IpToUint(ipAddress)
                                });

                                if (recordExists > 0)
                                {
                                    const string updateQuery = """
                                                               UPDATE `sa_players_ips`
                                                               SET used_at = CURRENT_TIMESTAMP,
                                                                   name = @playerName
                                                               WHERE steamid = @SteamID AND address = @IPAddress;
                                                               """;
                                    await connection.ExecuteAsync(updateQuery, new
                                    {
                                        playerName,
                                        SteamID = CS2_SimpleAdmin.PlayersInfo[steamId].SteamId.SteamId64,
                                        IPAddress = IpHelper.IpToUint(ipAddress)
                                    });
                                }
                                else
                                {
                                    const string insertQuery = """
                                                               INSERT INTO `sa_players_ips` (steamid, name, address, used_at)
                                                               VALUES (@SteamID, @playerName, @IPAddress, CURRENT_TIMESTAMP);
                                                               """;
                                    await connection.ExecuteAsync(insertQuery, new
                                    {
                                        SteamID = CS2_SimpleAdmin.PlayersInfo[steamId].SteamId.SteamId64,
                                        playerName,
                                        IPAddress = IpHelper.IpToUint(ipAddress)
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CS2_SimpleAdmin._logger?.LogError(
                                $"Unable to save ip address for {playerInfo.Name} ({ipAddress}) {ex.Message}");
                        }

                        playerInfo.AccountsAssociated =
                            CS2_SimpleAdmin.Instance.CacheManager?.GetAccountsByIp(ipAddress).AsValueEnumerable()
                                .Select(x => (x.SteamId, x.PlayerName)).ToList() ?? [];
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

                        if (CS2_SimpleAdmin.PlayersInfo.TryGetValue(steamId, out PlayerInfo? value)) // Temp skip
                        {
                            var warns = await CS2_SimpleAdmin.Instance.WarnManager.GetPlayerWarns(value, false);
                            var (totalMutes, totalGags, totalSilences) =
                                await CS2_SimpleAdmin.Instance.MuteManager.GetPlayerMutes(value);
                            value.TotalBans = CS2_SimpleAdmin.Instance.CacheManager
                                ?.GetPlayerBansBySteamId(value.SteamId.SteamId64)
                                .Count ?? 0;
                            value.TotalMutes = totalMutes;
                            value.TotalGags = totalGags;
                            value.TotalSilences = totalSilences;
                            value.TotalWarns = warns.Count;

                            var activeMutes =
                                await CS2_SimpleAdmin.Instance.MuteManager.IsPlayerMuted(value.SteamId.SteamId64
                                    .ToString());

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
                                            PlayerPenaltyManager.AddPenalty(
                                                CS2_SimpleAdmin.PlayersInfo[steamId].Slot,
                                                PenaltyType.Gag, ends, duration);
                                            // if (CS2_SimpleAdmin._localizer != null)
                                            // 	mutesList[PenaltyType.Gag].Add(CS2_SimpleAdmin._localizer["sa_player_penalty_info_active_gag", ends.ToLocalTime().ToString(CultureInfo.CurrentCulture)]);
                                            break;
                                        case "MUTE":
                                            PlayerPenaltyManager.AddPenalty(
                                                CS2_SimpleAdmin.PlayersInfo[steamId].Slot,
                                                PenaltyType.Mute, ends, duration);
                                            await Server.NextWorldUpdateAsync(() =>
                                            {
                                                player.VoiceFlags = VoiceFlags.Muted;
                                            });
                                            // if (CS2_SimpleAdmin._localizer != null)
                                            // 	mutesList[PenaltyType.Mute].Add(CS2_SimpleAdmin._localizer["sa_player_penalty_info_active_mute", ends.ToLocalTime().ToString(CultureInfo.InvariantCulture)]);
                                            break;
                                        default:
                                            PlayerPenaltyManager.AddPenalty(
                                                CS2_SimpleAdmin.PlayersInfo[steamId].Slot,
                                                PenaltyType.Silence, ends, duration);
                                            await Server.NextWorldUpdateAsync(() =>
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
                                await Server.NextWorldUpdateAsync(() =>
                                {
                                    foreach (var admin in Helper.GetValidPlayers()
                                                 .Where(p => (AdminManager.PlayerHasPermissions(
                                                                  new SteamID(p.SteamID),
                                                                  "@css/kick") ||
                                                              AdminManager.PlayerHasPermissions(
                                                                  new SteamID(p.SteamID),
                                                                  "@css/ban")) &&
                                                             p.Connected == PlayerConnectedState.PlayerConnected &&
                                                             !CS2_SimpleAdmin.AdminDisabledJoinComms
                                                                 .Contains(p.SteamID)))
                                    {
                                        if (CS2_SimpleAdmin._localizer == null || admin == player) continue;
                                        admin.SendLocalizedMessage(CS2_SimpleAdmin._localizer,
                                            "sa_admin_penalty_info",
                                            player.PlayerName,
                                            CS2_SimpleAdmin.PlayersInfo[steamId].TotalBans,
                                            CS2_SimpleAdmin.PlayersInfo[steamId].TotalGags,
                                            CS2_SimpleAdmin.PlayersInfo[steamId].TotalMutes,
                                            CS2_SimpleAdmin.PlayersInfo[steamId].TotalSilences,
                                            CS2_SimpleAdmin.PlayersInfo[steamId].TotalWarns
                                        );

                                        if (CS2_SimpleAdmin.PlayersInfo[steamId].AccountsAssociated.Count >= 2)
                                        {
                                            var associatedAcccountsChunks =
                                                CS2_SimpleAdmin.PlayersInfo[steamId].AccountsAssociated.ChunkBy(5)
                                                    .ToList();
                                            foreach (var chunk in associatedAcccountsChunks)
                                            {
                                                admin.SendLocalizedMessage(CS2_SimpleAdmin._localizer,
                                                    "sa_admin_associated_accounts",
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
                    }
                    catch (Exception ex)
                    {
                        CS2_SimpleAdmin._logger?.LogError("Error processing player connection: {exception}",
                            ex.Message);
                    }
                }
            }
            finally
            {
                _loadPlayerSemaphore.Release();
            }
        });
        if (CS2_SimpleAdmin.RenamedPlayers.TryGetValue(player.SteamID, out var name))
        {
            player.Rename(name);
        }
    }

    /// <summary>
    /// Periodically checks the status of online players and applies timers for speed, gravity,
    /// and penalty expiration validation.
    /// </summary>
    /// <remarks>
    /// This method registers two repeating timers:
    /// <list type="bullet">
    ///   <item><description>One short-interval timer to update speed/gravity modifications applied to players.</description></item>
    ///   <item><description>
    ///   One long-interval timer (default 61 seconds) to expire bans, mutes, warns, refresh caches,
    ///   and remove outdated penalties from connected players.
    ///   </description></item>
    /// </list>
    /// Additionally, banned players still online are kicked, and admins may be updated about mute statuses based on the configured time mode.
    /// </remarks>
    public void CheckPlayersTimer()
    {
        CS2_SimpleAdmin.Instance.AddTimer(0.12f, () =>
        {
            if (CS2_SimpleAdmin.SpeedPlayers.Count > 0)
            {
                foreach (var (slot, speed) in CS2_SimpleAdmin.SpeedPlayers)
                {
                    var player = Utilities.GetPlayerFromSlot(slot);

                    if (player is null) continue;

                    if (player is { IsValid: true, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE })
                    {
                        player.SetSpeed(speed);
                    }
                }
            }

            if (CS2_SimpleAdmin.GravityPlayers.Count > 0)
            {
                foreach (var (slot, gravity) in CS2_SimpleAdmin.GravityPlayers)
                {
                    var player = Utilities.GetPlayerFromSlot(slot);

                    if (player is null) continue;

                    if (player is { IsValid: true, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE })
                    {
                        player.SetGravity(gravity);
                    }
                }
            }
        }, TimerFlags.REPEAT);

        CS2_SimpleAdmin.Instance.PlayersTimer = CS2_SimpleAdmin.Instance.AddTimer(61.0f, () =>
        {
#if DEBUG
            CS2_SimpleAdmin._logger?.LogCritical("[OnMapStart] Expired check");
#endif
            if (CS2_SimpleAdmin.DatabaseProvider == null)
                return;

            var tempPlayers = Helper.GetValidPlayers()
                .Select(p => new
                {
                    p.PlayerName,
                    p.SteamID,
                    p.IpAddress,
                    p.UserId,
                    p.Slot,
                })
                .ToList();

            var pluginInstance = CS2_SimpleAdmin.Instance;
            _ = Task.Run(async () =>
            {
                try
                {
                    var expireTasks = new[]
                    {
                        pluginInstance.BanManager.ExpireOldBans(),
                        pluginInstance.MuteManager.ExpireOldMutes(),
                        pluginInstance.WarnManager.ExpireOldWarns(),
                        pluginInstance.CacheManager?.RefreshCacheAsync() ?? Task.CompletedTask,
                        pluginInstance.PermissionManager.DeleteOldAdmins()
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

                if (pluginInstance.CacheManager == null)
                    return;

                var bannedPlayers = tempPlayers.AsValueEnumerable()
                    .Where(player =>
                    {
                        var playerName = player.PlayerName;
                        var steamId = player.SteamID;
                        var ip = player.IpAddress?.Split(':')[0];

                        return CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType switch
                        {
                            0 => pluginInstance.CacheManager.IsPlayerBanned(playerName, steamId, null),
                            _ => CS2_SimpleAdmin.Instance.Config.OtherSettings.CheckMultiAccountsByIp
                                ? pluginInstance.CacheManager.IsPlayerOrAnyIpBanned(playerName, steamId, ip)
                                : pluginInstance.CacheManager.IsPlayerBanned(playerName, steamId, ip)
                        };
                    }).ToList();

                if (bannedPlayers.Count > 0)
                {
                    foreach (var player in bannedPlayers)
                    {
                        if (!player.UserId.HasValue) continue;
                        await Server.NextWorldUpdateAsync(() => Helper.KickPlayer((int)player.UserId, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED));
                    }
                }

                if (_config.OtherSettings.TimeMode == 0)
                {
                    var onlinePlayers = tempPlayers.AsValueEnumerable().Select(player => (player.SteamID, player.UserId, player.Slot)).ToList();
                    if (tempPlayers.Count == 0 || onlinePlayers.Count == 0) return;
                    await pluginInstance.MuteManager.CheckOnlineModeMutes(onlinePlayers);
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
                        if (entry.Player.VoiceFlags.HasFlag(VoiceFlags.Muted))
                            entry.Player.VoiceFlags &= ~VoiceFlags.Muted;
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