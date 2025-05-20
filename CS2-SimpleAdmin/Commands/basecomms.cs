using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Menus;
using CS2_SimpleAdminApi;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGagCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        
        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

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
            if (!caller!.CanTarget(player)) return;
            if (time < 0 && caller != null && caller.IsValid && Config.OtherSettings.ShowBanMenuIfNoTime)
            {
                DurationMenu.OpenMenu(caller, $"{_localizer?["sa_gag"] ?? "Gag"}: {player.PlayerName}", player,
                    ManagePlayersMenu.GagMenu);
                return;
            }
            
            Gag(caller, player, time, reason, callerName, command);
        });
    }

    internal void Gag(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, CommandInfo? command = null, bool silent = false)
    {
        if (Database == null || !player.IsValid || !player.UserId.HasValue) return;
        if (!caller.CanTarget(player)) return;
        if (!CheckValidMute(caller, time)) return;

        // Set default caller name if not provided
        callerName ??= caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Get player and admin information
        var playerInfo = PlayersInfo[player.UserId.Value];
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        // Asynchronously handle gag logic
        Task.Run(async () =>
        {
            int? penaltyId = await MuteManager.MutePlayer(playerInfo, adminInfo, reason, time);
            SimpleAdminApi?.OnPlayerPenaltiedEvent(playerInfo, adminInfo, PenaltyType.Gag, reason, time, penaltyId);
        });

        // Add penalty to the player's penalty manager
        PlayerPenaltyManager.AddPenalty(player.Slot, PenaltyType.Gag, Time.ActualDateTime().AddMinutes(time), time);

        // Determine message keys and arguments based on gag time (permanent or timed)
        var (messageKey, activityMessageKey, playerArgs, adminActivityArgs) = time == 0
            ? ("sa_player_gag_message_perm", "sa_admin_gag_message_perm",
                [reason, "CALLER"],
                ["CALLER", player.PlayerName, reason])
            : ("sa_player_gag_message_time", "sa_admin_gag_message_time",
                new object[] { reason, time, "CALLER" },
                new object[] { "CALLER", player.PlayerName, reason, time });

        // Display center message to the gagged player
        Helper.DisplayCenterMessage(player, messageKey, callerName, playerArgs);

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Increment the player's total gags count
        PlayersInfo[player.UserId.Value].TotalGags++;

        // Log the gag command and send Discord notification
        if (!silent)
        {
            if (command == null)
                Helper.LogCommand(caller, $"css_gag {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {time} {reason}");
            else
                Helper.LogCommand(caller, command);
        }

        Helper.SendDiscordPenaltyMessage(caller, player, reason, time, PenaltyType.Gag, _localizer);
    }
    
    internal void AddGag(CCSPlayerController? caller, SteamID steamid, int time, string reason, MuteManager? muteManager = null)
    {
        // Set default caller name if not provided
        var callerName = !string.IsNullOrEmpty(caller?.PlayerName) 
            ? caller.PlayerName 
            : (_localizer?["sa_console"] ?? "Console");
        
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        var player = Helper.GetPlayerFromSteamid64(steamid.SteamId64.ToString());

        if (player != null && player.IsValid)
        {
            if (!caller.CanTarget(player))
                return;
            
            Gag(caller, player, time, reason, callerName, silent: true);
        }
        else
        {
            if (!caller.CanTarget(steamid))
                return;
            
            // Asynchronous ban operation if player is not online or not found
            Task.Run(async () =>
            {
                int? penaltyId = await MuteManager.AddMuteBySteamid(steamid.SteamId64.ToString(), adminInfo, reason, time, 3);
                SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamid, adminInfo, PenaltyType.Gag, reason, time, penaltyId);
            });
            
            Helper.SendDiscordPenaltyMessage(caller, steamid.SteamId64.ToString(), reason, time, PenaltyType.Gag, _localizer);
        }
    }

    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAddGagCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        // Set caller name
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Validate command arguments
        if (command.ArgCount < 2 || string.IsNullOrEmpty(command.GetArg(1))) return;

        // Validate and extract SteamID
        if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
        {
            command.ReplyToCommand("Invalid SteamID64.");
            return;
        }

        var steamid = steamId.SteamId64.ToString();
        var reason = command.ArgCount >= 3
            ? string.Join(" ", Enumerable.Range(3, command.ArgCount - 3).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";
        
        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;

        var time = Math.Max(0, Helper.ParsePenaltyTime(command.GetArg(2)));
        if (!CheckValidMute(caller, time)) return;

        // Get player and admin info
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        // Attempt to match player based on SteamID
        var player = Helper.GetPlayerFromSteamid64(steamid);

        if (player != null && player.IsValid)
        {
            // Check if caller can target the player
            if (!caller.CanTarget(player)) return;

            // Perform the gag for an online player
            Gag(caller, player, time, reason, callerName, silent: true);
        }
        else
        {
            if (!caller.CanTarget(new SteamID(steamId.SteamId64)))
                return;

            // Asynchronous gag operation for offline players
            Task.Run(async () =>
            {
                int? penaltyId = await MuteManager.AddMuteBySteamid(steamid, adminInfo, reason, time);
                SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamId, adminInfo, PenaltyType.Gag, reason, time, penaltyId);
            });

            Helper.SendDiscordPenaltyMessage(caller, steamid, reason, time, PenaltyType.Gag, _localizer);

            command.ReplyToCommand($"Player with steamid {steamid} is not online. Gag has been added offline.");
        }

        // Log the gag command and respond to the command
        Helper.LogCommand(caller, command);
    }

    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<steamid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnUngagCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        var callerSteamId = caller?.SteamID.ToString() ?? _localizer?["sa_console"] ?? "Console";
        var pattern = command.GetArg(1);
        var reason = command.ArgCount >= 2
            ? string.Join(" ", Enumerable.Range(2, command.ArgCount - 2).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";

        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;
        
        if (pattern.Length <= 1)
        {
            command.ReplyToCommand($"Too short pattern to search.");
            return;
        }

        Helper.LogCommand(caller, command);

        // Check if pattern is a valid SteamID64
        if (Helper.ValidateSteamId(pattern, out var steamId) && steamId != null)
        {
            var player = Helper.GetPlayerFromSteamid64(steamId.SteamId64.ToString());

            if (player != null && player.IsValid)
            {
                PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Gag);

                Task.Run(async () =>
                {
                    await MuteManager.UnmutePlayer(player.SteamID.ToString(), callerSteamId, reason);
                });

                command.ReplyToCommand($"Ungaged player {player.PlayerName}.");
                return;
            }
        }

        // If not a valid SteamID64, check by player name
        var nameMatches = Helper.GetPlayerFromName(pattern);
        var namePlayer = nameMatches.Count == 1 ? nameMatches.FirstOrDefault() : null;

        if (namePlayer != null && namePlayer.IsValid)
        {
            PlayerPenaltyManager.RemovePenaltiesByType(namePlayer.Slot, PenaltyType.Gag);

            if (namePlayer.UserId.HasValue && PlayersInfo[namePlayer.UserId.Value].TotalGags > 0)
                PlayersInfo[namePlayer.UserId.Value].TotalGags--;

            Task.Run(async () =>
            {
                await MuteManager.UnmutePlayer(namePlayer.SteamID.ToString(), callerSteamId, reason);
            });

            command.ReplyToCommand($"Ungaged player {namePlayer.PlayerName}.");
        }
        else
        {
            Task.Run(async () =>
            {
                await MuteManager.UnmutePlayer(pattern, callerSteamId, reason);
            });

            command.ReplyToCommand($"Ungaged offline player with pattern {pattern}.");
        }
    }

    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnMuteCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

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
            if (!caller!.CanTarget(player)) return;
            if (time < 0 && caller != null && caller.IsValid && Config.OtherSettings.ShowBanMenuIfNoTime)
            {
                DurationMenu.OpenMenu(caller, $"{_localizer?["sa_mute"] ?? "Mute"}: {player.PlayerName}", player,
                    ManagePlayersMenu.MuteMenu);
                return;
            }

            Mute(caller, player, time, reason, callerName, command);
        });
    }

    internal void Mute(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, CommandInfo? command = null, bool silent = false)
    {
        if (Database == null || !player.IsValid || !player.UserId.HasValue) return;
        if (!caller.CanTarget(player)) return;
        if (!CheckValidMute(caller, time)) return;

        // Set default caller name if not provided
        callerName ??= caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Get player and admin information
        var playerInfo = PlayersInfo[player.UserId.Value];
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        // Set player's voice flags to muted
        player.VoiceFlags = VoiceFlags.Muted;

        // Asynchronously handle mute logic
        Task.Run(async () =>
        {
            int? penaltyId = await MuteManager.MutePlayer(playerInfo, adminInfo, reason, time, 1);
            SimpleAdminApi?.OnPlayerPenaltiedEvent(playerInfo, adminInfo, PenaltyType.Mute, reason, time, penaltyId);
        });

        // Add penalty to the player's penalty manager
        PlayerPenaltyManager.AddPenalty(player.Slot, PenaltyType.Mute, Time.ActualDateTime().AddMinutes(time), time);

        // Determine message keys and arguments based on mute time (permanent or timed)
        var (messageKey, activityMessageKey, playerArgs, adminActivityArgs) = time == 0
            ? ("sa_player_mute_message_perm", "sa_admin_mute_message_perm",
                [reason, "CALLER"],
                ["CALLER", player.PlayerName, reason])
            : ("sa_player_mute_message_time", "sa_admin_mute_message_time",
                new object[] { reason, time, "CALLER" },
                new object[] { "CALLER", player.PlayerName, reason, time });

        // Display center message to the muted player
        Helper.DisplayCenterMessage(player, messageKey, callerName, playerArgs);

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Increment the player's total mutes count
        PlayersInfo[player.UserId.Value].TotalMutes++;

        // Log the mute command and send Discord notification
        if (!silent)
        {
            if (command == null)
                Helper.LogCommand(caller, $"css_mute {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {time} {reason}");
            else
                Helper.LogCommand(caller, command);
        }

        Helper.SendDiscordPenaltyMessage(caller, player, reason, time, PenaltyType.Mute, _localizer);
    }

    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAddMuteCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        // Set caller name
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Validate command arguments
        if (command.ArgCount < 2 || string.IsNullOrEmpty(command.GetArg(1))) return;

        // Validate and extract SteamID
        if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
        {
            command.ReplyToCommand("Invalid SteamID64.");
            return;
        }

        var steamid = steamId.SteamId64.ToString();
        var reason = command.ArgCount >= 3
            ? string.Join(" ", Enumerable.Range(3, command.ArgCount - 3).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";
        
        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;

        var time = Math.Max(0, Helper.ParsePenaltyTime(command.GetArg(2)));
        if (!CheckValidMute(caller, time)) return;

        // Get player and admin info
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        // Attempt to match player based on SteamID
        var player = Helper.GetPlayerFromSteamid64(steamid);

        if (player != null && player.IsValid)
        {
            // Check if caller can target the player
            if (!caller.CanTarget(player)) return;

            // Perform the mute for an online player
            Mute(caller, player, time, reason, callerName, silent: true);
        }
        else
        {
            if (!caller.CanTarget(new SteamID(steamId.SteamId64)))
                return;

            // Asynchronous mute operation for offline players
            Task.Run(async () =>
            {
                int? penaltyId = await MuteManager.AddMuteBySteamid(steamid, adminInfo, reason, time, 1);
                SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamId, adminInfo, PenaltyType.Mute, reason, time, penaltyId);
            });

            Helper.SendDiscordPenaltyMessage(caller, steamid, reason, time, PenaltyType.Mute, _localizer);
            
            command.ReplyToCommand($"Player with steamid {steamid} is not online. Mute has been added offline.");
        }

        // Log the mute command and respond to the command
        Helper.LogCommand(caller, command);
    }
    
    internal void AddMute(CCSPlayerController? caller, SteamID steamid, int time, string reason, MuteManager? muteManager = null)
    {
        // Set default caller name if not provided
        var callerName = !string.IsNullOrEmpty(caller?.PlayerName) 
            ? caller.PlayerName 
            : (_localizer?["sa_console"] ?? "Console");
        
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        var player = Helper.GetPlayerFromSteamid64(steamid.SteamId64.ToString());

        if (player != null && player.IsValid)
        {
            if (!caller.CanTarget(player))
                return;
            
            Mute(caller, player, time, reason, callerName, silent: true);
        }
        else
        {
            if (!caller.CanTarget(steamid))
                return;
            
            // Asynchronous ban operation if player is not online or not found
            Task.Run(async () =>
            {
                int? penaltyId = await MuteManager.AddMuteBySteamid(steamid.SteamId64.ToString(), adminInfo, reason, time, 1);
                SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamid, adminInfo, PenaltyType.Mute, reason, time, penaltyId);
            });
            
            Helper.SendDiscordPenaltyMessage(caller, steamid.SteamId64.ToString(), reason, time, PenaltyType.Mute, _localizer);
        }
    }

    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<steamid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnUnmuteCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        var callerSteamId = caller?.SteamID.ToString() ?? _localizer?["sa_console"] ?? "Console";
        var pattern = command.GetArg(1);
        var reason = command.ArgCount >= 2
            ? string.Join(" ", Enumerable.Range(2, command.ArgCount - 2).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";

        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;
        
        if (pattern.Length <= 1)
        {
            command.ReplyToCommand("Too short pattern to search.");
            return;
        }

        Helper.LogCommand(caller, command);

        // Check if pattern is a valid SteamID64
        if (Helper.ValidateSteamId(pattern, out var steamId) && steamId != null)
        {
            var player = Helper.GetPlayerFromSteamid64(steamId.SteamId64.ToString());

            if (player != null && player.IsValid)
            {
                PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Mute);
                player.VoiceFlags = VoiceFlags.Normal;

                Task.Run(async () =>
                {
                    await MuteManager.UnmutePlayer(player.SteamID.ToString(), callerSteamId, reason, 1);
                });

                command.ReplyToCommand($"Unmuted player {player.PlayerName}.");
                return;
            }
        }

        // If not a valid SteamID64, check by player name
        var nameMatches = Helper.GetPlayerFromName(pattern);
        var namePlayer = nameMatches.Count == 1 ? nameMatches.FirstOrDefault() : null;

        if (namePlayer != null && namePlayer.IsValid)
        {
            PlayerPenaltyManager.RemovePenaltiesByType(namePlayer.Slot, PenaltyType.Mute);
            namePlayer.VoiceFlags = VoiceFlags.Normal;

            if (namePlayer.UserId.HasValue && PlayersInfo[namePlayer.UserId.Value].TotalMutes > 0)
                PlayersInfo[namePlayer.UserId.Value].TotalMutes--;

            Task.Run(async () =>
            {
                await MuteManager.UnmutePlayer(namePlayer.SteamID.ToString(), callerSteamId, reason, 1);
            });

            command.ReplyToCommand($"Unmuted player {namePlayer.PlayerName}.");
        }
        else
        {
            Task.Run(async () =>
            {
                await MuteManager.UnmutePlayer(pattern, callerSteamId, reason, 1);
            });

            command.ReplyToCommand($"Unmuted offline player with pattern {pattern}.");
        }
    }

    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSilenceCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        
        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

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
            if (!caller!.CanTarget(player)) return;
            if (time < 0 && caller != null && caller.IsValid && Config.OtherSettings.ShowBanMenuIfNoTime)
            {
                DurationMenu.OpenMenu(caller, $"{_localizer?["sa_silence"] ?? "Silence"}: {player.PlayerName}", player,
                    ManagePlayersMenu.SilenceMenu);
                return;
            }
                
            Silence(caller, player, time, reason, callerName, command);
        });
    }

    internal void Silence(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, CommandInfo? command = null, bool silent = false)
    {
        if (Database == null || !player.IsValid || !player.UserId.HasValue) return;
        if (!caller.CanTarget(player)) return;
        if (!CheckValidMute(caller, time)) return;

        // Set default caller name if not provided
        callerName ??= caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Get player and admin information
        var playerInfo = PlayersInfo[player.UserId.Value];
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        // Asynchronously handle silence logic
        Task.Run(async () =>
        {
            int? penaltyId = await MuteManager.MutePlayer(playerInfo, adminInfo, reason, time, 2); // Assuming 2 is the type for silence
            SimpleAdminApi?.OnPlayerPenaltiedEvent(playerInfo, adminInfo, PenaltyType.Silence, reason, time, penaltyId);
        });

        // Add penalty to the player's penalty manager
        PlayerPenaltyManager.AddPenalty(player.Slot, PenaltyType.Silence, Time.ActualDateTime().AddMinutes(time), time);
        player.VoiceFlags = VoiceFlags.Muted;

        // Determine message keys and arguments based on silence time (permanent or timed)
        var (messageKey, activityMessageKey, playerArgs, adminActivityArgs) = time == 0
            ? ("sa_player_silence_message_perm", "sa_admin_silence_message_perm",
                [reason, "CALLER"],
                ["CALLER", player.PlayerName, reason])
            : ("sa_player_silence_message_time", "sa_admin_silence_message_time",
                new object[] { reason, time, "CALLER" },
                new object[] { "CALLER", player.PlayerName, reason, time });

        // Display center message to the silenced player
        Helper.DisplayCenterMessage(player, messageKey, callerName, playerArgs);

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Increment the player's total silences count
        PlayersInfo[player.UserId.Value].TotalSilences++;

        // Log the silence command and send Discord notification
        if (!silent)
        {
            if (command == null)
                Helper.LogCommand(caller, $"css_silence {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {time} {reason}");
            else
                Helper.LogCommand(caller, command);
        }

        Helper.SendDiscordPenaltyMessage(caller, player, reason, time, PenaltyType.Silence, _localizer);
    }

    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAddSilenceCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        // Set caller name
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Validate command arguments
        if (command.ArgCount < 2 || string.IsNullOrEmpty(command.GetArg(1))) return;

        // Validate and extract SteamID
        if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
        {
            command.ReplyToCommand("Invalid SteamID64.");
            return;
        }

        var steamid = steamId.SteamId64.ToString();
        var reason = command.ArgCount >= 3
            ? string.Join(" ", Enumerable.Range(3, command.ArgCount - 3).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";
        
        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;

        var time = Math.Max(0, Helper.ParsePenaltyTime(command.GetArg(2)));
        if (!CheckValidMute(caller, time)) return;

        // Get player and admin info
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        // Attempt to match player based on SteamID
        var player = Helper.GetPlayerFromSteamid64(steamid);

        if (player != null && player.IsValid)
        {
            // Check if caller can target the player
            if (!caller.CanTarget(player)) return;

            // Perform the silence for an online player
            Silence(caller, player, time, reason, callerName, silent: true);
        }
        else
        {
            if (!caller.CanTarget(new SteamID(steamId.SteamId64)))
                return;

            // Asynchronous silence operation for offline players
            Task.Run(async () =>
            {
                int? penaltyId = await MuteManager.AddMuteBySteamid(steamid, adminInfo, reason, time, 2);
                SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamId, adminInfo, PenaltyType.Silence, reason, time, penaltyId);
            });

            Helper.SendDiscordPenaltyMessage(caller, steamid, reason, time, PenaltyType.Silence, _localizer);

            command.ReplyToCommand($"Player with steamid {steamid} is not online. Silence has been added offline.");
        }

        // Log the silence command and respond to the command
        Helper.LogCommand(caller, command);
    }
    
    internal void AddSilence(CCSPlayerController? caller, SteamID steamid, int time, string reason, MuteManager? muteManager = null)
    {
        // Set default caller name if not provided
        var callerName = !string.IsNullOrEmpty(caller?.PlayerName) 
            ? caller.PlayerName 
            : (_localizer?["sa_console"] ?? "Console");
        
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        var player = Helper.GetPlayerFromSteamid64(steamid.SteamId64.ToString());

        if (player != null && player.IsValid)
        {
            if (!caller.CanTarget(player))
                return;
            
            Silence(caller, player, time, reason, callerName, silent: true);
        }
        else
        {
            if (!caller.CanTarget(steamid))
                return;
            
            // Asynchronous ban operation if player is not online or not found
            Task.Run(async () =>
            {
                int? penaltyId = await MuteManager.AddMuteBySteamid(steamid.SteamId64.ToString(), adminInfo, reason, time, 2);
                SimpleAdminApi?.OnPlayerPenaltiedAddedEvent(steamid, adminInfo, PenaltyType.Silence, reason, time, penaltyId);
            });
            
            Helper.SendDiscordPenaltyMessage(caller, steamid.SteamId64.ToString(), reason, time, PenaltyType.Silence, _localizer);
        }
    }

    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "<steamid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnUnsilenceCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        var callerSteamId = caller?.SteamID.ToString() ?? _localizer?["sa_console"] ?? "Console";
        var pattern = command.GetArg(1);
        var reason = command.ArgCount >= 2
            ? string.Join(" ", Enumerable.Range(2, command.ArgCount - 2).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";

        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;
        
        if (pattern.Length <= 1)
        {
            command.ReplyToCommand("Too short pattern to search.");
            return;
        }
        
        Helper.LogCommand(caller, command);

        // Check if pattern is a valid SteamID64
        if (Helper.ValidateSteamId(pattern, out var steamId) && steamId != null)
        {
            var player = Helper.GetPlayerFromSteamid64(steamId.SteamId64.ToString());

            if (player != null && player.IsValid)
            {
                PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Silence);

                // Reset voice flags to normal
                player.VoiceFlags = VoiceFlags.Normal;

                Task.Run(async () =>
                {
                    await MuteManager.UnmutePlayer(player.SteamID.ToString(), callerSteamId, reason, 2); // Unmute by type 2 (silence)
                });

                command.ReplyToCommand($"Unsilenced player {player.PlayerName}.");
                return;
            }
        }

        // If not a valid SteamID64, check by player name
        var nameMatches = Helper.GetPlayerFromName(pattern);
        var namePlayer = nameMatches.Count == 1 ? nameMatches.FirstOrDefault() : null;

        if (namePlayer != null && namePlayer.IsValid)
        {
            PlayerPenaltyManager.RemovePenaltiesByType(namePlayer.Slot, PenaltyType.Silence);

            // Reset voice flags to normal
            namePlayer.VoiceFlags = VoiceFlags.Normal;

            if (namePlayer.UserId.HasValue && PlayersInfo[namePlayer.UserId.Value].TotalSilences > 0)
                PlayersInfo[namePlayer.UserId.Value].TotalSilences--;

            Task.Run(async () =>
            {
                await MuteManager.UnmutePlayer(namePlayer.SteamID.ToString(), callerSteamId, reason, 2); // Unmute by type 2 (silence)
            });

            command.ReplyToCommand($"Unsilenced player {namePlayer.PlayerName}.");
        }
        else
        {
            Task.Run(async () =>
            {
                await MuteManager.UnmutePlayer(pattern, callerSteamId, reason, 2); // Unmute by type 2 (silence)
            });

            command.ReplyToCommand($"Unsilenced offline player with pattern {pattern}.");
        }
    }
    
    private bool CheckValidMute(CCSPlayerController? caller, int duration)
    {
        if (caller == null) return true;

        var canPermMute = AdminManager.PlayerHasPermissions(new SteamID(caller.SteamID), "@css/permmute");

        if (duration <= 0 && canPermMute == false)
        {
            caller.PrintToChat($"{_localizer!["sa_prefix"]} {_localizer["sa_ban_perm_restricted"]}");
            return false;
        }

        if (duration <= Config.OtherSettings.MaxMuteDuration || canPermMute) return true;

        caller.PrintToChat($"{_localizer!["sa_prefix"]} {_localizer["sa_ban_max_duration_exceeded", Config.OtherSettings.MaxMuteDuration]}");
        return false;
    }
}