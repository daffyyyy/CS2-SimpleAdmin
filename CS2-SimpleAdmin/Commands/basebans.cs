using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Menus;
using CS2_SimpleAdminApi;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    /// <summary>
    /// Handles the 'ban' command, allowing admins to ban one or more valid connected players.
    /// </summary>
    /// <param name="caller">The player issuing the ban command, or null for console.</param>
    /// <param name="command">The command information including arguments.</param>
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        if (command.ArgCount < 2)
            return;
        
        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, Connected: PlayerConnectedState.PlayerConnected, IsHLTV: false }).ToList();

        if (playersToTarget.Count > 1 && Config.OtherSettings.DisableDangerousCommands || playersToTarget.Count == 0)
        {
            return;
        }
        
        var reason = command.ArgCount >= 3
            ? string.Join(" ", Enumerable.Range(3, command.ArgCount - 3).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";

        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;
        var time = Helper.ParsePenaltyTime(command.GetArg(2));

        playersToTarget.ForEach(player =>
        {
            if (!caller.CanTarget(player)) return;
            
            if (time < 0 && caller != null && caller.IsValid && Config.OtherSettings.ShowBanMenuIfNoTime)
            {
                DurationMenu.OpenMenu(caller, $"{_localizer?["sa_ban"] ?? "Ban"}: {player.PlayerName}", player,
                    ManagePlayersMenu.BanMenu);
                return;
            }

            Ban(caller, player, time, reason, callerName, BanManager, command);
        });
    }

    /// <summary>
    /// Core logic to ban a specific player, scheduling database updates, notifications, and kicks.
    /// </summary>
    /// <param name="caller">The player issuing the ban, or null for console.</param>
    /// <param name="player">The player to be banned.</param>
    /// <param name="time">Ban duration in minutes; 0 means permanent.</param>
    /// <param name="reason">Reason for the ban.</param>
    /// <param name="callerName">Optional caller name string. If null, defaults to player name or console.</param>
    /// <param name="banManager">Optional BanManager to handle ban persistence.</param>
    /// <param name="command">Optional command info object for logging.</param>
    /// <param name="silent">If true, suppresses command logging.</param>
    internal void Ban(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, BanManager? banManager = null, CommandInfo? command = null, bool silent = false)
    {
        if (DatabaseProvider == null || !player.IsValid || !player.UserId.HasValue) return;
        if (!caller.CanTarget(player)) return;
        if (!CheckValidBan(caller, time)) return;

        // Set default caller name if not provided
        callerName = !string.IsNullOrEmpty(caller?.PlayerName) 
            ? caller.PlayerName 
            : (_localizer?["sa_console"] ?? "Console");
        
        // Get player and admin information
        var playerInfo = PlayersInfo[player.SteamID];
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.SteamID] : null;

        // Asynchronously handle banning logic
        Task.Run(async () =>
        {
            int? penaltyId = await BanManager.BanPlayer(playerInfo, adminInfo, reason, time);
            await Server.NextWorldUpdateAsync(() =>
            {
                SimpleAdminApi?.OnPlayerPenaltiedEvent(playerInfo, adminInfo, PenaltyType.Ban, reason, time, penaltyId);
            });
        });

        // Determine message keys and arguments based on ban time
        var (messageKey, activityMessageKey, centerArgs, adminActivityArgs) = time == 0
            ? ("sa_player_ban_message_perm", "sa_admin_ban_message_perm",
                [reason, "CALLER"],
                ["CALLER", player.PlayerName, reason])
            : ("sa_player_ban_message_time", "sa_admin_ban_message_time",
                new object[] { reason, time, "CALLER" },
                new object[] { "CALLER", player.PlayerName, reason, time });

        // Display center message to the player
        Helper.DisplayCenterMessage(player, messageKey, callerName, centerArgs);

        // Display admin activity message if necessary
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Schedule a kick timer
        if (player.UserId.HasValue)
        {
            Helper.KickPlayer(player.UserId.Value, NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKBANADDED, Config.OtherSettings.KickTime);
        }

        // Execute ban command if necessary
        if (UnlockedCommands)
        {
            Server.ExecuteCommand($"banid 1 {new SteamID(player.SteamID).SteamId3}");
        }

        if (!silent)
        {
            if (command == null)
                Helper.LogCommand(caller, $"css_ban {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {time} {reason}");
            else
                Helper.LogCommand(caller, command);
        }
        
        Helper.SendDiscordPenaltyMessage(caller, player, reason, time, PenaltyType.Ban, _localizer);
    }

    /// <summary>
    /// Adds a ban for a player by their SteamID, including offline bans.
    /// </summary>
    /// <param name="caller">The player issuing the ban command.</param>
    /// <param name="steamid">SteamID of the player to ban.</param>
    /// <param name="time">Ban duration in minutes (0 means permanent).</param>
    /// <param name="reason">Reason for banning.</param>
    /// <param name="banManager">Optional ban manager for database operations.</param>
    internal void AddBan(CCSPlayerController? caller, SteamID steamid, int time, string reason, BanManager? banManager = null)
    {
        // Set default caller name if not provided
        var callerName = !string.IsNullOrEmpty(caller?.PlayerName) 
            ? caller.PlayerName 
            : (_localizer?["sa_console"] ?? "Console");
        
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.SteamID] : null;
        var player = Helper.GetPlayerFromSteamid64(steamid.SteamId64);
        if (player != null && player.IsValid)
        {
            if (!caller.CanTarget(player))
                return;
            Ban(caller, player, time, reason, callerName, silent: true);
            //command.ReplyToCommand($"Banned player {player.PlayerName}.");
        }
        else
        {
            if (!caller.CanTarget(steamid))
                return;
            // Asynchronous ban operation if player is not online or not found
            Task.Run(async () =>
            {
                int? penaltyId = await BanManager.AddBanBySteamid(steamid.SteamId64, adminInfo, reason, time);
                await Server.NextWorldUpdateAsync(() =>
                {
                    SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamid, adminInfo, PenaltyType.Ban, reason, time,
                        penaltyId);
                });
            });
            
            Helper.SendDiscordPenaltyMessage(caller, steamid.SteamId64.ToString(), reason, time, PenaltyType.Ban, _localizer);
        }
    }

    /// <summary>
    /// Handles banning a player by specifying their SteamID via command.
    /// </summary>
    /// <param name="caller">The player issuing the command, or null if console.</param>
    /// <param name="command">Command information including arguments (SteamID, time, reason).</param>
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAddBanCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;
        var callerName = caller?.PlayerName ?? _localizer?["sa_console"] ?? "Console";
        if (command.ArgCount < 2 || string.IsNullOrEmpty(command.GetArg(1))) return;
        if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
        {
            command.ReplyToCommand("Invalid SteamID64.");
            return;
        }

        var steamid = steamId.SteamId64;
        var reason = command.ArgCount >= 3
            ? string.Join(" ", Enumerable.Range(3, command.ArgCount - 3).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";
        
        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;

        var time = Math.Max(0, Helper.ParsePenaltyTime(command.GetArg(2)));
        if (!CheckValidBan(caller, time)) return;

        var adminInfo = caller != null && caller.UserId.HasValue
            ? PlayersInfo[caller.SteamID]
            : null;

        var player = Helper.GetPlayerFromSteamid64(steamid);
        if (player != null && player.IsValid)
        {
            if (!caller.CanTarget(player))
                return;

            Ban(caller, player, time, reason, callerName, silent: true);
            //command.ReplyToCommand($"Banned player {player.PlayerName}.");
        }
        else
        {
            if (!caller.CanTarget(new SteamID(steamId.SteamId64)))
                return;
            
            // Asynchronous ban operation if player is not online or not found
            Task.Run(async () =>
            {
                int? penaltyId = await BanManager.AddBanBySteamid(steamid, adminInfo, reason, time);
                await Server.NextWorldUpdateAsync(() =>
                {
                    SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamId, adminInfo, PenaltyType.Ban, reason, time,
                        penaltyId);
                });
            });
            
            Helper.SendDiscordPenaltyMessage(caller, steamid.ToString(), reason, time, PenaltyType.Ban, _localizer);

            command.ReplyToCommand($"Player with steamid {steamid} is not online. Ban has been added offline.");
        }

        Helper.LogCommand(caller, command);

        if (UnlockedCommands)
            Server.ExecuteCommand($"banid 1 {steamId.SteamId3}");
    }

    /// <summary>
    /// Handles banning a player by their IP address, supporting offline banning if player is not online.
    /// </summary>
    /// <param name="caller">The player issuing the ban command.</param>
    /// <param name="command">The command containing the IP, time, and reason arguments.</param>
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 1, usage: "<ip> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBanIpCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;
        var callerName = caller?.PlayerName ?? _localizer?["sa_console"] ?? "Console";
        if (command.ArgCount < 2 || string.IsNullOrEmpty(command.GetArg(1))) return;
        var ipAddress = command.GetArg(1);

        if (!Helper.IsValidIp(ipAddress))
        {
            command.ReplyToCommand($"Invalid IP address.");
            return;
        }

        var reason = command.ArgCount >= 3
            ? string.Join(" ", Enumerable.Range(3, command.ArgCount - 3).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";
        
        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;

        var time = Math.Max(0, Helper.ParsePenaltyTime(command.GetArg(2)));

        if (!CheckValidBan(caller, time)) return;

        var adminInfo = caller != null && caller.UserId.HasValue
            ? PlayersInfo[caller.SteamID]
            : null;

        var players = Helper.GetPlayerFromIp(ipAddress);
        if (players.Count >= 1)
        {
            foreach (var player in players)
            {
                if (player == null || !player.IsValid) continue;
                if (!caller.CanTarget(player))
                    return;

                Ban(caller, player, time, reason, callerName, silent: true);
            }
        }
        else
        {
            // Asynchronous ban operation if player is not online or not found
            Task.Run(async () =>
            {
                await BanManager.AddBanByIp(ipAddress, adminInfo, reason, time);
            });

            command.ReplyToCommand($"Player with ip {ipAddress} is not online. Ban has been added offline.");
        }

        Helper.LogCommand(caller, command);
    }

    /// <summary>
    /// Checks whether the ban duration is valid based on the caller's permissions and configured limits.
    /// </summary>
    /// <param name="caller">The player issuing the ban command.</param>
    /// <param name="duration">Requested ban duration in minutes.</param>
    /// <returns>True if ban duration is valid; otherwise, false.</returns>
    private bool CheckValidBan(CCSPlayerController? caller, int duration)
    {
        if (caller == null) return true;

        var canPermBan = AdminManager.PlayerHasPermissions(new SteamID(caller.SteamID), "@css/permban");

        if (duration <= 0 && canPermBan == false)
        {
            caller.PrintToChat($"{_localizer!["sa_prefix"]} {_localizer["sa_ban_perm_restricted"]}");
            return false;
        }

        if (duration <= Config.OtherSettings.MaxBanDuration || canPermBan) return true;

        caller.PrintToChat($"{_localizer!["sa_prefix"]} {_localizer["sa_ban_max_duration_exceeded", Config.OtherSettings.MaxBanDuration]}");
        return false;
    }

    /// <summary>
    /// Handles unbanning players by pattern (steamid, name, or IP).
    /// </summary>
    /// <param name="caller">The player issuing the unban command.</param>
    /// <param name="command">Command containing target pattern and optional reason.</param>
    [RequiresPermissions("@css/unban")]
    [CommandHelper(minArgs: 1, usage: "<steamid or name or ip> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnUnbanCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;
        var callerSteamId = caller?.SteamID.ToString() ?? _localizer?["sa_console"] ?? "Console";
        if (command.GetArg(1).Length <= 1)
        {
            command.ReplyToCommand($"Too short pattern to search.");
            return;
        }

        var pattern = command.GetArg(1);
        var reason = command.ArgCount >= 2
            ? string.Join(" ", Enumerable.Range(2, command.ArgCount - 2).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";
        
        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;
        Task.Run(async () => await BanManager.UnbanPlayer(pattern, callerSteamId, reason));
        Helper.LogCommand(caller, command);
        command.ReplyToCommand($"Unbanned player with pattern {pattern}.");
    }

    /// <summary>
    /// Handles warning players, supporting multiple targets and warning durations.
    /// </summary>
    /// <param name="caller">The player issuing the warn command.</param>
    /// <param name="command">The command containing target, time, and reason parameters.</param>
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnWarnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null)
            return;
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        if (command.ArgCount < 2)
            return;
        
        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV).ToList();

        if (playersToTarget.Count > 1 && Config.OtherSettings.DisableDangerousCommands || playersToTarget.Count == 0)
        {
            return;
        }

        var time = Math.Max(0, Helper.ParsePenaltyTime(command.GetArg(2)));
        var reason = command.ArgCount >= 3
            ? string.Join(" ", Enumerable.Range(3, command.ArgCount - 3).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";
        
        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                Warn(caller, player, time, reason, callerName, command);
            }
        });
    }

    /// <summary>
    /// Issues a warning penalty to a specific player with optional duration and reason.
    /// </summary>
    /// <param name="caller">The player issuing the warning.</param>
    /// <param name="player">The player to warn.</param>
    /// <param name="time">Duration of the warning in minutes.</param>
    /// <param name="reason">Reason for the warning.</param>
    /// <param name="callerName">Optional display name of the caller.</param>
    /// <param name="command">Optional command info for logging.</param>
    internal void Warn(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, CommandInfo? command = null)
    {
        if (DatabaseProvider == null || !player.IsValid || !player.UserId.HasValue) return;
        if (!caller.CanTarget(player)) return;
        if (!CheckValidBan(caller, time)) return;

        // Set default caller name if not provided
        callerName = !string.IsNullOrEmpty(caller?.PlayerName) 
            ? caller.PlayerName 
            : (_localizer?["sa_console"] ?? "Console");

        // Freeze player pawn if alive
        if (player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE)
        {
            player.PlayerPawn?.Value?.Freeze();
            AddTimer(5.0f, () => player.PlayerPawn?.Value?.Unfreeze(), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Get player and admin information
        var playerInfo = PlayersInfo[player.SteamID];
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.SteamID] : null;

        // Asynchronously handle warning logic
        Task.Run(async () =>
        {
            int? penaltyId = await WarnManager.WarnPlayer(playerInfo, adminInfo, reason, time);
            await Server.NextWorldUpdateAsync(() =>
            {
                SimpleAdminApi?.OnPlayerPenaltiedEvent(playerInfo, adminInfo, PenaltyType.Warn, reason, time,
                    penaltyId);
            });

            // Check for warn thresholds and execute punish command if applicable
            var totalWarns = await WarnManager.GetPlayerWarnsCount(player.SteamID);
            if (Config.WarnThreshold.Count > 0)
            {
                string? punishCommand = null;
                var lastKey = Config.WarnThreshold.Keys.Max();

                if (totalWarns >= lastKey)
                    punishCommand = Config.WarnThreshold[lastKey];
                else if (Config.WarnThreshold.TryGetValue(totalWarns, out var value))
                    punishCommand = value;

                if (!string.IsNullOrEmpty(punishCommand))
                {
                    await Server.NextWorldUpdateAsync(() =>
                    {
                        Server.ExecuteCommand(punishCommand.Replace("USERID", playerInfo.UserId.ToString()).Replace("STEAMID64", playerInfo.SteamId?.ToString()));
                    });
                }
            }
        });

        // Determine message keys and arguments based on warning time
        var (messageKey, activityMessageKey, centerArgs, adminActivityArgs) = time == 0
            ? ("sa_player_warn_message_perm", "sa_admin_warn_message_perm",
                new object[] { reason, "CALLER" },
                new object[] { "CALLER", player.PlayerName, reason })
            : ("sa_player_warn_message_time", "sa_admin_warn_message_time",
                [reason, time, "CALLER"],
                ["CALLER", player.PlayerName, reason, time]);

        // Display center message to the playser
        Helper.DisplayCenterMessage(player, messageKey, callerName, centerArgs);

        // Display admin activity message if necessary
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Log the warning command
        if (command == null)
            Helper.LogCommand(caller, $"css_warn {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {time} {reason}");
        else
            Helper.LogCommand(caller, command);

        // Send Discord notification for the warning
        Helper.SendDiscordPenaltyMessage(caller, player, reason, time, PenaltyType.Warn, _localizer);
    }
    
    /// <summary>
    /// Adds a warning to a player by their SteamID, including support for offline players.
    /// </summary>
    /// <param name="caller">The player issuing the warn command.</param>
    /// <param name="steamid">SteamID of the player to warn.</param>
    /// <param name="time">Warning duration in minutes.</param>
    /// <param name="reason">Reason for the warning.</param>
    /// <param name="warnManager">Optional warn manager instance.</param>
    internal void AddWarn(CCSPlayerController? caller, SteamID steamid, int time, string reason, WarnManager? warnManager = null)
    {
        // Set default caller name if not provided
        var callerName = !string.IsNullOrEmpty(caller?.PlayerName) 
            ? caller.PlayerName 
            : (_localizer?["sa_console"] ?? "Console");
        
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.SteamID] : null;

        var player = Helper.GetPlayerFromSteamid64(steamid.SteamId64);

        if (player != null && player.IsValid)
        {
            if (!caller.CanTarget(player))
                return;

            Warn(caller, player, time, reason, callerName);
            //command.ReplyToCommand($"Banned player {player.PlayerName}.");
        }
        else
        {
            if (!caller.CanTarget(steamid))
                return;
            
            // Asynchronous ban operation if player is not online or not found
            Task.Run(async () =>
            {
                int? penaltyId = await WarnManager.AddWarnBySteamid(steamid.SteamId64, adminInfo, reason, time);
                await Server.NextWorldUpdateAsync(() =>
                {
                    SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamid, adminInfo, PenaltyType.Warn, reason, time,
                        penaltyId);
                });

                // Check for warn thresholds and execute punish command if applicable
                var totalWarns = await WarnManager.GetPlayerWarnsCount(steamid.SteamId64);
                if (Config.WarnThreshold.Count > 0)
                {
                    string? punishCommand = null;
                    var lastKey = Config.WarnThreshold.Keys.Max();

                    if (totalWarns >= lastKey)
                        punishCommand = Config.WarnThreshold[lastKey];
                    else if (Config.WarnThreshold.TryGetValue(totalWarns, out var value))
                        punishCommand = value;

                    if (!string.IsNullOrEmpty(punishCommand))
                    {
                        await Server.NextWorldUpdateAsync(() =>
                        {
                            Server.ExecuteCommand(punishCommand.Replace("STEAMID64", steamid.SteamId64.ToString()));
                        });
                    }
                }
            });
            
            Helper.SendDiscordPenaltyMessage(caller, steamid.SteamId64.ToString(), reason, time, PenaltyType.Warn, _localizer);
        }
    }
    
    /// <summary>
    /// Handles removing a warning (unwarn) by a pattern string.
    /// </summary>
    /// <param name="caller">The player issuing the unwarn command.</param>
    /// <param name="command">The command containing target pattern.</param>
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, usage: "<steamid or name or ip>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnUnwarnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;

        if (command.GetArg(1).Length <= 1)
        {
            command.ReplyToCommand($"Too short pattern to search.");
            return;
        }

        var pattern = command.GetArg(1);
        Task.Run(async () => await WarnManager.UnwarnPlayer(pattern));
        Helper.LogCommand(caller, command);
        command.ReplyToCommand($"Unwarned player with pattern {pattern}.");
    }
}