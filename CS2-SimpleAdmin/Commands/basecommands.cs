using System.Collections;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Menus;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API.ValveConstants.Protobuf;

using Menu;
using Menu.Enums;
using System.Diagnostics.CodeAnalysis;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    /// <summary>
    /// Handles the command that shows active penalties and warns for the caller or specified player.
    /// Queries warnings and mute status, formats them locally, and sends the result to caller's chat.
    /// </summary>
    /// <param name="caller">The player issuing this command.</param>
    /// <param name="command">Command input parameters.</param>
    [CommandHelper(usage: "[#userid or name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPenaltiesCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.IsValid == false || !caller.UserId.HasValue || DatabaseProvider == null)
            return;

        var userId = caller.UserId.Value;
        var steamId = caller.SteamID;


        if (!string.IsNullOrEmpty(command.GetArg(1)) && AdminManager.PlayerHasPermissions(new SteamID(caller.SteamID), "@css/kick"))
        {
            var targets = GetTarget(command);

            if (targets == null)
                return;

            var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();
            playersToTarget.ForEach(player =>
            {
                if (!player.UserId.HasValue) return;
                if (!caller.CanTarget(player)) return;

                userId = player.UserId.Value;
            });
        }

        Task.Run(async () =>
        {
            try
            {
                var warns = await WarnManager.GetPlayerWarns(PlayersInfo[steamId], false);

                // Check if the player is muted
                var activeMutes = await MuteManager.IsPlayerMuted(PlayersInfo[steamId].SteamId.SteamId64.ToString());

                Dictionary<PenaltyType, List<string>> mutesList = new()
                {
                    { PenaltyType.Gag, [] },
                    { PenaltyType.Mute, [] },
                    { PenaltyType.Silence, [] }
                };

                List<string> warnsList = [];

                bool found = false;
                foreach (var warn in warns.TakeWhile(warn => (string)warn.status == "ACTIVE"))
                {
                    DateTime ends = warn.ends;
                    if (_localizer == null) continue;
                    using (new WithTemporaryCulture(caller.GetLanguage()))
                        warnsList.Add(_localizer["sa_player_penalty_info_active_warn", ends.ToLocalTime().ToString(CultureInfo.CurrentCulture), (string)warn.reason]);
                    found = true;
                }

                if (!found)
                {
                    if (_localizer != null)
                        warnsList.Add(_localizer["sa_player_penalty_info_no_active_warn"]);
                }

                if (activeMutes.Count > 0)
                {
                    foreach (var mute in activeMutes)
                    {
                        string muteType = mute.type;
                        DateTime ends = mute.ends;
                        using (new WithTemporaryCulture(caller.GetLanguage()))
                        {
                            switch (muteType)
                            {
                                // Apply mute penalty based on mute type
                                case "GAG":
                                    if (_localizer != null)
                                        mutesList[PenaltyType.Gag].Add(_localizer["sa_player_penalty_info_active_gag", ends.ToLocalTime().ToString(CultureInfo.CurrentCulture)]);
                                    break;
                                case "MUTE":
                                    if (_localizer != null)
                                        mutesList[PenaltyType.Mute].Add(_localizer["sa_player_penalty_info_active_mute", ends.ToLocalTime().ToString(CultureInfo.CurrentCulture)]);
                                    break;
                                default:
                                    if (_localizer != null)
                                        mutesList[PenaltyType.Silence].Add(_localizer["sa_player_penalty_info_active_silence", ends.ToLocalTime().ToString(CultureInfo.CurrentCulture)]);
                                    break;
                            }
                        }
                    }
                }

                if (_localizer != null)
                {
                    if (mutesList[PenaltyType.Gag].Count == 0)
                        mutesList[PenaltyType.Gag].Add(_localizer["sa_player_penalty_info_no_active_gag"]);
                    if (mutesList[PenaltyType.Mute].Count == 0)
                        mutesList[PenaltyType.Mute].Add(_localizer["sa_player_penalty_info_no_active_mute"]);
                    if (mutesList[PenaltyType.Silence].Count == 0)
                        mutesList[PenaltyType.Silence].Add(_localizer["sa_player_penalty_info_no_active_silence"]);
                }

                await Server.NextWorldUpdateAsync(() =>
                {
                    caller.SendLocalizedMessage(_localizer, "sa_player_penalty_info",
                    [
                        PlayersInfo[steamId].Name,
                        PlayersInfo[steamId].TotalBans,
                        PlayersInfo[steamId].TotalGags,
                        PlayersInfo[steamId].TotalMutes,
                        PlayersInfo[steamId].TotalSilences,
                        PlayersInfo[steamId].TotalWarns,
                        string.Join("\n", mutesList.SelectMany(kvp => kvp.Value)),
                        string.Join("\n", warnsList)
                    ]);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error processing player information: {ex}");
            }
        });
    }

    /// <summary>
    /// Toggles the admin voice listening mode or mutes/unmutes all players' voice.
    /// Sends confirmation messages accordingly.
    /// </summary>
    /// <param name="caller">The player issuing this command.</param>
    /// <param name="command">Command input parameters.</param>
    [RequiresPermissions("@css/chat")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAdminVoiceCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.IsValid == false)
            return;

        if (command.ArgCount > 1)
        {
            if (command.GetArg(2).ToLower().Equals("muteAll"))
            {
                caller.SendLocalizedMessage(_localizer, "sa_admin_voice_mute_all");
                foreach (var player in Helper.GetValidPlayers().Where(p => p != caller && !AdminManager.PlayerHasPermissions(new SteamID(p.SteamID), "@css/chat")))
                {
                    player.VoiceFlags = VoiceFlags.Muted;
                }
            }

            if (command.GetArg(2).ToLower().Equals("unmuteAll"))
            {
                caller.SendLocalizedMessage(_localizer, "sa_admin_voice_unmute_all");
                foreach (var player in Helper.GetValidPlayers().Where(p => p != caller))
                {
                    if (PlayerPenaltyManager.GetPlayerPenalties(player.Slot, [PenaltyType.Silence, PenaltyType.Mute]).Count == 0)
                        player.VoiceFlags = VoiceFlags.Normal;
                }
            }

            return;
        }

        var enabled = caller.VoiceFlags.HasFlag(VoiceFlags.ListenAll);
        var messageKey = enabled
            ? "sa_admin_voice_unlisten_all"
            : "sa_admin_voice_listen_all";

        caller.SendLocalizedMessage(_localizer, messageKey);
        caller.VoiceFlags ^= VoiceFlags.ListenAll;

        caller.VoiceFlags = caller.VoiceFlags == VoiceFlags.All ? VoiceFlags.Normal : VoiceFlags.All;
    }

    /// <summary>
    /// Opens the admin menu for the caller.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">Command input parameters.</param>

    [RequiresPermissions("@css/generic")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAdminCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.IsValid == false)
            return;

        AdminMenu.OpenMenu(caller);
    }

    /// <summary>
    /// Displays admin help text read from a file.
    /// Outputs lines one at a time as replies to the command.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">Command input parameters.</param>
    [RequiresPermissions("@css/generic")]
    public void OnAdminHelpCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var lines = File.ReadAllLines(ModuleDirectory + "/admin_help.txt");

        foreach (var line in lines)
        {
            command.ReplyToCommand(string.IsNullOrWhiteSpace(line) ? " " : line.ReplaceColorTags());
        }
    }

    /// <summary>
    /// Handles adding a new admin with specified SteamID, name, flags, immunity, and duration.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">Command input parameters.</param>
    [CommandHelper(minArgs: 4, usage: "<steamid> <name> <flags/groups> <immunity> <duration>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnAddAdminCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;

        if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
        {
            command.ReplyToCommand($"Invalid SteamID64.");
            return;
        }

        var steamid = steamId.SteamId64.ToString();

        if (command.GetArg(2).Length <= 0)
        {
            command.ReplyToCommand($"Invalid player name.");
            return;
        }
        if (!command.GetArg(3).Contains('@') && !command.GetArg(3).Contains('#'))
        {
            command.ReplyToCommand($"Invalid flag or group.");
            return;
        }

        var name = command.GetArg(2);
        var flags = command.GetArg(3);
        var globalAdmin = command.GetArg(4).ToLower().Equals("-g") || command.GetArg(5).ToLower().Equals("-g") ||
                          command.GetArg(6).ToLower().Equals("-g");
        int.TryParse(command.GetArg(4), out var immunity);
        var time = Math.Max(0, Helper.ParsePenaltyTime(command.GetArg(5)));

        AddAdmin(caller, steamid, name, flags, immunity, time, globalAdmin, command);
    }

    /// <summary>
    /// Adds admin permissions and groups for a player.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="steamid">SteamID as string identifying the player.</param>
    /// <param name="name">Player's name.</param>
    /// <param name="flags">Comma-separated admin flags/groups.</param>
    /// <param name="immunity">Admin immunity level.</param>
    /// <param name="time">Duration of permission (default 0 = permanent).</param>
    /// <param name="globalAdmin">Whether admin is global.</param>
    /// <param name="command">Optional command info for confirmation messages.</param>
    public static void AddAdmin(CCSPlayerController? caller, string steamid, string name, string flags, int immunity, int time = 0, bool globalAdmin = false, CommandInfo? command = null)
    {
        if (DatabaseProvider == null) return;

        var flagsList = flags.Split(',').Select(flag => flag.Trim()).ToList();
        _ = Instance.PermissionManager.AddAdminBySteamId(steamid, name, flagsList, immunity, time, globalAdmin);

        Helper.LogCommand(caller, $"css_addadmin {steamid} {name} {flags} {immunity} {time}");

        var msg = $"Added '{flags}' flags to '{name}' ({steamid})";
        if (command != null)
            command.ReplyToCommand(msg);
        else if (caller != null && caller.IsValid)
            caller.PrintToChat(msg);
        else
            Server.PrintToConsole(msg);
    }

    /// <summary>
    /// Handles removing an admin's flags and groups by SteamID.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">Command input parameters.</param>
    [CommandHelper(minArgs: 1, usage: "<steamid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnDelAdminCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;

        if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
        {
            command.ReplyToCommand($"Invalid SteamID64.");
            return;
        }

        var globalDelete = command.GetArg(2).ToLower().Equals("-g");

        RemoveAdmin(caller, steamId.SteamId64.ToString(), globalDelete, command);
    }

    /// <summary>
    /// Removes admin permissions and groups for a player.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="steamid">SteamID as string identifying the player.</param>
    /// <param name="globalDelete">Whether to delete globally.</param>
    /// <param name="command">Optional command info.</param>
    public void RemoveAdmin(CCSPlayerController? caller, string steamid, bool globalDelete = false, CommandInfo? command = null)
    {
        if (DatabaseProvider == null) return;
        _ = PermissionManager.DeleteAdminBySteamId(steamid, globalDelete);

        AddTimer(2, () =>
        {
            if (string.IsNullOrEmpty(steamid) || !SteamID.TryParse(steamid, out var steamId) ||
                steamId == null) return;
            if (PermissionManager.AdminCache.ContainsKey(steamId))
            {
                PermissionManager.AdminCache.TryRemove(steamId, out _);
            }

            AdminManager.ClearPlayerPermissions(steamId);
            AdminManager.RemovePlayerAdminData(steamId);
        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

        Helper.LogCommand(caller, $"css_deladmin {steamid}");

        var msg = $"Removed flags from '{steamid}'";
        if (command != null)
            command.ReplyToCommand(msg);
        else if (caller != null && caller.IsValid)
            caller.PrintToChat(msg);
        else
            Server.PrintToConsole(msg);
    }

    /// <summary>
    /// Adds a new admin group with specified flags and immunity settings.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">Command input parameters.</param>
    [CommandHelper(minArgs: 3, usage: "<group_name> <flags> <immunity>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnAddGroup(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;

        if (!command.GetArg(1).StartsWith("#"))
        {
            command.ReplyToCommand($"Group name must start with #.");
            return;
        }

        if (!command.GetArg(2).StartsWith($"@") && !command.GetArg(2).StartsWith($"#"))
        {
            command.ReplyToCommand($"Invalid flag or group.");
            return;
        }

        var groupName = command.GetArg(1);
        var flags = command.GetArg(2);
        int.TryParse(command.GetArg(3), out var immunity);
        var globalGroup = command.GetArg(4).ToLower().Equals("-g");

        AddGroup(caller, groupName, flags, immunity, globalGroup, command);
    }

    /// <summary>
    /// Adds a new admin group with specified flags and immunity level.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="name">Group name (prefix with #).</param>
    /// <param name="flags">Comma-separated flags/groups string.</param>
    /// <param name="immunity">Immunity level.</param>
    /// <param name="globalGroup">Whether group is global.</param>
    /// <param name="command">Optional command info.</param>
    private static void AddGroup(CCSPlayerController? caller, string name, string flags, int immunity, bool globalGroup, CommandInfo? command = null)
    {
        if (DatabaseProvider == null) return;

        var flagsList = flags.Split(',').Select(flag => flag.Trim()).ToList();
        _ = Instance.PermissionManager.AddGroup(name, flagsList, immunity, globalGroup);

        Helper.LogCommand(caller, $"css_addgroup {name} {flags} {immunity}");

        var msg = $"Created group '{name}' with flags '{flags}'";
        if (command != null)
            command.ReplyToCommand(msg);
        else if (caller != null && caller.IsValid)
            caller.PrintToChat(msg);
        else
            Server.PrintToConsole(msg);
    }

    /// <summary>
    /// Handles removing a group by name.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">Command input parameters.</param>
    [CommandHelper(minArgs: 1, usage: "<group_name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnDelGroupCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;

        if (!command.GetArg(1).StartsWith($"#"))
        {
            command.ReplyToCommand($"Group name must start with #.");
            return;
        }

        var groupName = command.GetArg(1);

        RemoveGroup(caller, groupName, command);
    }

    /// <summary>
    /// Removes a group.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="name">The group name to remove.</param>
    /// <param name="command">Optional command info for confirmation.</param>
    private void RemoveGroup(CCSPlayerController? caller, string name, CommandInfo? command = null)
    {
        if (DatabaseProvider == null) return;
        _ = PermissionManager.DeleteGroup(name);

        AddTimer(2, () =>
        {
            ReloadAdmins(caller);
        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

        Helper.LogCommand(caller, $"css_delgroup {name}");

        var msg = $"Removed group '{name}'";
        if (command != null)
            command.ReplyToCommand(msg);
        else if (caller != null && caller.IsValid)
            caller.PrintToChat(msg);
        else
            Server.PrintToConsole(msg);
    }

    /// <summary>
    /// Reloads admin and group data from database and json files.
    /// </summary>
    /// <param name="caller">The player issuing the reload command.</param>
    /// <param name="command">Command input parameters.</param>
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnRelAdminCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;
        ReloadAdmins(caller);
        command.ReplyToCommand("Reloaded sql admins and groups");
    }

    /// <summary>
    /// Reloads bans cache.
    /// </summary>
    /// <param name="caller">The player issuing the reload command.</param>
    /// <param name="command">Command input parameters.</param>

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnRelBans(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;

        _ = Instance.CacheManager?.ForceReInitializeCacheAsync();
        command.ReplyToCommand("Reloaded bans");
    }

    /// <summary>
    /// Reloads admin data asynchronously and updates admin caches.
    /// </summary>
    /// <param name="caller">The player issuing the reload command.</param>
    public void ReloadAdmins(CCSPlayerController? caller)
    {
        if (DatabaseProvider == null) return;

        Task.Run(async () =>
        {
            await PermissionManager.CreateGroupsJsonFile();
            await PermissionManager.CreateAdminsJsonFile();

            var adminsFile = await File.ReadAllTextAsync(Instance.ModuleDirectory + "/data/admins.json");
            var groupsFile = await File.ReadAllTextAsync(Instance.ModuleDirectory + "/data/groups.json");

            await Server.NextWorldUpdateAsync(() =>
            {
                AddTimer(1, () =>
                {
                    if (!string.IsNullOrEmpty(adminsFile))
                        AddTimer(2.0f, () => AdminManager.LoadAdminData(ModuleDirectory + "/data/admins.json"));
                    if (!string.IsNullOrEmpty(groupsFile))
                        AddTimer(3.0f, () => AdminManager.LoadAdminGroups(ModuleDirectory + "/data/groups.json"));
                    if (!string.IsNullOrEmpty(adminsFile))
                        AddTimer(4.0f, () => AdminManager.LoadAdminData(ModuleDirectory + "/data/admins.json"));

                    _logger?.LogInformation("Loaded admins!");
                });
            });
        });
    }

    /// <summary>
    /// Toggles player visibility on the server, hiding or revealing them.
    /// </summary>
    /// <param name="caller">The player issuing the hide command.</param>
    /// <param name="command">Command input parameters.</param>
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/kick")]
    public void OnHideCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null) return;

        Helper.LogCommand(caller, command);

        if (!SilentPlayers.Add(caller.Slot))
        {
            SilentPlayers.Remove(caller.Slot);
            caller.PrintToChat($"You aren't hidden now!");
            if (caller.TeamNum <= 1)
                caller.ChangeTeam(CsTeam.Spectator);
            SimpleAdminApi?.OnAdminToggleSilentEvent(caller.Slot, false);
            caller.ChangeTeam(CsTeam.Spectator);
            Server.ExecuteCommand($"mm_removeexcludeslot {caller.Slot}");
        }
        else
        {
            Server.ExecuteCommand("sv_disable_teamselect_menu 1");
            Server.ExecuteCommand($"mm_excludeslot {caller.Slot}");

            if (caller.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE)
                caller.PlayerPawn.Value?.CommitSuicide(true, false);

            AddTimer(1.0f, () => { Server.NextWorldUpdateAsync(() => caller.ChangeTeam(CsTeam.Spectator)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(1.4f, () => { Server.NextWorldUpdateAsync(() => caller.ChangeTeam(CsTeam.None)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            caller.PrintToChat($"You are hidden now!");
            if (caller.TeamNum > 1)
                AddTimer(0.15f, () => { Server.NextWorldUpdate(() => caller.ChangeTeam(CsTeam.Spectator)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(0.26f, () => { Server.NextWorldUpdate(() => caller.ChangeTeam(CsTeam.None)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(0.50f, () => { Server.NextWorldUpdate(() => Server.ExecuteCommand("sv_disable_teamselect_menu 0")); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            SimpleAdminApi?.OnAdminToggleSilentEvent(caller.Slot, true);
            AddTimer(2.0f, () => { Server.NextWorldUpdateAsync(() => Server.ExecuteCommand("sv_disable_teamselect_menu 0")); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }
    }

    /// <summary>
    /// Toggles penalty notification visibility to admins.
    /// </summary>
    /// <param name="caller">The player toggling notification visibility.</param>
    /// <param name="command">Command input parameters.</param>
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/kick")]
    public void OnHideCommsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
            return;

        if (!AdminDisabledJoinComms.Add(caller.SteamID))
        {
            AdminDisabledJoinComms.Remove(caller.SteamID);
            command.ReplyToCommand("From now on, you'll see penalty notifications");
        }
        else
        {
            AdminDisabledJoinComms.Add(caller.SteamID);
            command.ReplyToCommand($"You don't see penalty notifications now");
        }
    }

    /// <summary>
    /// Displays detailed information about target players, including admin groups, permissions, and penalties.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">Command input parameters including targets.</param>
    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/generic")]
    public void OnWhoCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null) return;

        var targets = GetTarget(command);
        if (targets == null) return;

        Helper.LogCommand(caller, command);

        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (!player.UserId.HasValue) return;
            if (!caller!.CanTarget(player)) return;

            var playerInfo = PlayersInfo[player.SteamID];

            Task.Run(async () =>
            {
                await Server.NextWorldUpdateAsync(() =>
                {
                    Action<string> printMethod = caller == null ? Server.PrintToConsole : caller.PrintToConsole;

                    var adminData = AdminManager.GetPlayerAdminData(new SteamID(player.SteamID));

                    printMethod($"--------- INFO ABOUT \"{playerInfo.Name}\" ---------");
                    printMethod($"• Clan: \"{player.Clan}\" Name: \"{playerInfo.Name}\"");
                    printMethod($"• UserID: \"{playerInfo.UserId}\"");
                    printMethod($"• SteamID64: \"{playerInfo.SteamId.SteamId64}\"");
                    if (adminData != null)
                    {
                        var flags = string.Join(",", adminData._flags);
                        var groups = string.Join(",", adminData.Groups);

                        printMethod($"• Groups/Flags: \"{groups}{flags}\"");
                    }
                    printMethod($"• SteamID2: \"{playerInfo.SteamId.SteamId2}\"");
                    printMethod($"• Community link: \"{playerInfo.SteamId.ToCommunityUrl()}\"");
                    if (playerInfo.IpAddress != null && AdminManager.PlayerHasPermissions(new SteamID(caller!.SteamID), "@css/showip"))
                        printMethod($"• IP Address: \"{playerInfo.IpAddress}\"");
                    printMethod($"• Ping: \"{player.Ping}\"");
                    printMethod($"• Total Bans: \"{playerInfo.TotalBans}\"");
                    printMethod($"• Total Gags: \"{playerInfo.TotalGags}\"");
                    printMethod($"• Total Mutes: \"{playerInfo.TotalMutes}\"");
                    printMethod($"• Total Silences: \"{playerInfo.TotalSilences}\"");
                    printMethod($"• Total Warns: \"{playerInfo.TotalWarns}\"");

                    var chunkedAccounts = playerInfo.AccountsAssociated.ChunkBy(3).ToList();
                    foreach (var chunk in chunkedAccounts)
                        printMethod($"• Associated Accounts: \"{string.Join(", ", chunk.Select(a => $"{a.PlayerName} ({a.SteamId})"))}\"");

                    printMethod($"--------- END INFO ABOUT \"{player.PlayerName}\" ---------");
                });
            });
        });
    }

    /// <summary>
    /// Displays a menu with disconnected players, allowing the caller to apply penalties like ban, mute, gag, or silence.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">The command containing parameters.</param>
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/kick")]
    public void OnDisconnectedCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (_localizer == null || caller == null) return;

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var player in DisconnectedPlayers)
        {
            var menuName = player.Name;
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(menuName)]));

            optionMap[i++] = () =>
            {
                // Build submenu for actions (ban, mute, gag, silence)
                List<MenuItem> subItems = new();
                var subOptionMap = new Dictionary<int, Action>();
                int j = 0;

                void AddSubMenuOption(string title, string command, string permMsgKey, string timeMsgKey, string adminPermMsgKey, string adminTimeMsgKey)
                {
                    subItems.Add(new MenuItem(MenuItemType.Button, [new MenuValue(title)]));
                    subOptionMap[j++] = () =>
                    {
                        DurationMenu.OpenMenu(caller, title, player, (_, _, duration) =>
                            ReasonMenu.OpenMenu(caller, PenaltyType.Ban, _localizer?["sa_reason"] ?? "reason", player, (_, _, reason) =>
                            {
                                caller.ExecuteClientCommandFromServer($"{command} {player.SteamId.SteamId64} {duration} \"{reason}\"");

                                // Message handling
                                var (_, activityMessageKey, _, adminActivityArgs) = duration == 0
                                    ? (permMsgKey, adminPermMsgKey,
                                        new object[] { reason, "CALLER" },
                                        new object[] { "CALLER", player.Name, reason })
                                    : (timeMsgKey, adminTimeMsgKey,
                                        new object[] { reason, duration, "CALLER" },
                                        new object[] { "CALLER", player.Name, reason, duration });

                                if (!SilentPlayers.Contains(caller.Slot))
                                {
                                    Helper.ShowAdminActivity(activityMessageKey, caller.PlayerName, false, adminActivityArgs);
                                }
                            }));
                    };
                }

                // Add each option
                AddSubMenuOption(_localizer["sa_ban"], "css_addban",
                    "sa_player_ban_message_perm", "sa_player_ban_message_time",
                    "sa_admin_ban_message_perm", "sa_admin_ban_message_time");

                AddSubMenuOption(_localizer["sa_mute"], "css_addmute",
                    "sa_player_mute_message_perm", "sa_player_mute_message_time",
                    "sa_admin_mute_message_perm", "sa_admin_mute_message_time");

                AddSubMenuOption(_localizer["sa_gag"], "css_addgag",
                    "sa_player_gag_message_perm", "sa_player_gag_message_time",
                    "sa_admin_gag_message_perm", "sa_admin_gag_message_time");

                AddSubMenuOption(_localizer["sa_silence"], "css_addsilence",
                    "sa_player_silence_message_perm", "sa_player_silence_message_time",
                    "sa_admin_silence_message_perm", "sa_admin_silence_message_time");

                // Show submenu
                Menu?.ShowScrollableMenu(caller,
                    _localizer["sa_menu_disconnected_action_title"],
                    subItems,
                    (buttons, menu, selected) =>
                    {
                        if (selected == null) return;

                        if (buttons == MenuButtons.Select && subOptionMap.TryGetValue(menu.Option, out var action))
                        {
                            action.Invoke();
                        }
                    },
                    false, freezePlayer: false, disableDeveloper: true);
            };
        }

        // Main disconnected menu
        if (i > 0)
        {
            Menu?.ShowScrollableMenu(caller,
                _localizer["sa_menu_disconnected_title"],
                items,
                (buttons, menu, selected) =>
                {
                    if (selected == null) return;

                    if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                    {
                        action.Invoke();
                    }
                },
                false, freezePlayer: false, disableDeveloper: true);
        }
    }

    /// <summary>
    /// Displays the warning menu for a player specified by a command argument,
    /// showing active and past warns with options to remove them.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">The command containing target player identifier.</param>
    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/kick")]
    public void OnWarnsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (DatabaseProvider == null || _localizer == null || caller == null) return;

        var targets = GetTarget(command);
        if (targets == null) return;

        Helper.LogCommand(caller, command);

        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsBot: false }).ToList();
        if (playersToTarget.Count > 1)
            return;

        playersToTarget.ForEach(player =>
        {
            if (!caller.CanTarget(player)) return;

            var steamId = player.SteamID;

            Task.Run(async () =>
            {
                var warnsList = await WarnManager.GetPlayerWarns(PlayersInfo[steamId], false);
                var sortedWarns = warnsList
                    .OrderBy(warn => (string)warn.status == "ACTIVE" ? 0 : 1)
                    .ThenByDescending(warn => (int)warn.id)
                    .ToList();

                List<MenuItem> items = new();
                var optionMap = new Dictionary<int, Action>();
                int i = 0;

                foreach (var w in sortedWarns)
                {
                    string statusSymbol = (string)w.status == "ACTIVE"
                        ? $"{ChatColors.LightRed}X{ChatColors.Default}"
                        : $"{ChatColors.Lime}✔️{ChatColors.Default}";

                    string label = $"[{statusSymbol}] {(string)w.reason}";

                    items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(label)]));

                    optionMap[i++] = () =>
                    {
                        _ = WarnManager.UnwarnPlayer(PlayersInfo[steamId], (int)w.id);
                        player.PrintToChat(_localizer["sa_admin_warns_unwarn", player.PlayerName, (string)w.reason]);
                    };
                }

                if (i == 0)
                {
                    player.PrintToChat(_localizer["sa_admin_warns_no_warns", player.PlayerName]);
                    return;
                }

                await Server.NextFrameAsync(() =>
                {
                    Menu?.ShowScrollableMenu(
                        caller,
                        _localizer["sa_admin_warns_menu_title", player.PlayerName],
                        items,
                        (buttons, menu, selected) =>
                        {
                            if (selected == null) return;

                            if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                            {
                                action.Invoke();
                            }
                        },
                        false, freezePlayer: false, disableDeveloper: true);
                });
            });

        });
    }

    /// <summary>
    /// Lists players currently connected to the server with options to output JSON or filter duplicate IPs.
    /// </summary>
    /// <param name="caller">The player issuing the command or null for console.</param>
    /// <param name="command">The command containing output options.</param>
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/generic")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public void OnPlayersCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var isJson = command.GetArg(1).ToLower().Equals("-json");
        var isDuplicate = command.GetArg(1).ToLower().Equals("-duplicate") || command.GetArg(2).ToLower().Equals("-duplicate");

        var playersToTarget = isDuplicate
            ? Helper.GetValidPlayers().GroupBy(player => player.IpAddress?.Split(":")[0] ?? "Unknown")
                .Where(group => group.Count() > 1)
                .SelectMany(group => group)
                .ToList()
            : Helper.GetValidPlayers();

        if (!isJson)
        {
            if (caller != null)
            {
                caller.PrintToConsole("--------- PLAYER LIST ---------");
                foreach (var player in playersToTarget)
                {
                    caller.PrintToConsole(
                        $"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{(AdminManager.PlayerHasPermissions(new SteamID(caller.SteamID), "@css/showip") ? player.IpAddress?.Split(":")[0] : "Unknown")}\" SteamID64: \"{player.SteamID}\")");
                }
                ;
                caller.PrintToConsole("--------- END PLAYER LIST ---------");
            }
            else
            {
                Server.PrintToConsole("--------- PLAYER LIST ---------");
                foreach (var player in playersToTarget)
                {
                    Server.PrintToConsole($"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{player.IpAddress?.Split(":")[0]}\" SteamID64: \"{player.SteamID}\")");
                }
                ;
                Server.PrintToConsole("--------- END PLAYER LIST ---------");
            }
        }
        else
        {
            var playersJson = JsonSerializer.Serialize(playersToTarget.Select(player =>
            {
                var matchStats = player.ActionTrackingServices?.MatchStats;

                return new
                {
                    player.UserId,
                    Name = player.PlayerName,
                    SteamId = player.SteamID.ToString(),
                    IpAddress = AdminManager.PlayerHasPermissions(new SteamID(caller!.SteamID), "@css/showip") ? player.IpAddress?.Split(":")[0] ?? "Unknown" : "Unknown",
                    player.Ping,
                    IsAdmin = AdminManager.PlayerHasPermissions(new SteamID(player.SteamID), "@css/ban") || AdminManager.PlayerHasPermissions(new SteamID(player.SteamID), "@css/generic"),
                    Stats = new
                    {
                        player.Score,
                        Kills = matchStats?.Kills ?? 0,
                        Deaths = matchStats?.Deaths ?? 0,
                        player.MVPs
                    }
                };
            }));

            if (caller != null)
                caller.PrintToConsole(playersJson);
            else
                Server.PrintToConsole(playersJson);
        }
    }

    /// <summary>
    /// Issues a kick to one or multiple players specified in the command arguments.
    /// </summary>
    /// <param name="caller">The player issuing the kick command.</param>
    /// <param name="command">The command with target player(s) and optional reason.</param>
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        var targets = GetTarget(command);

        if (targets == null) return;
        var playersToTarget = targets.Players
            .Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        if (playersToTarget.Count > 1 && Config.OtherSettings.DisableDangerousCommands || playersToTarget.Count == 0)
        {
            return;
        }

        var reason = command.ArgCount >= 2
            ? string.Join(" ", Enumerable.Range(2, command.ArgCount - 2).Select(command.GetArg)).Trim()
            : _localizer?["sa_unknown"] ?? "Unknown";

        reason = string.IsNullOrWhiteSpace(reason) ? _localizer?["sa_unknown"] ?? "Unknown" : reason;

        playersToTarget.ForEach(player =>
        {
            if (!player.IsValid)
                return;

            if (caller!.CanTarget(player))
            {
                Kick(caller, player, reason, callerName, command);
            }
        });

        Helper.LogCommand(caller, command);
    }


    /// <summary>
    /// Kicks a specified player immediately with reason, notifying the server and logging the action.
    /// </summary>
    /// <param name="caller">The player issuing the kick.</param>
    /// <param name="player">The player to be kicked.</param>
    /// <param name="reason">The reason for the kick.</param>
    /// <param name="callerName">Optional name of the kick issuer for notifications.</param>
    /// <param name="command">Optional command for logging.</param>
    public void Kick(CCSPlayerController? caller, CCSPlayerController player, string? reason = "Unknown", string? callerName = null, CommandInfo? command = null)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;
        if (!player.UserId.HasValue) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";
        reason ??= _localizer?["sa_unknown"] ?? "Unknown";


        var playerInfo = PlayersInfo[player.SteamID];
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.SteamID] : null;

        // Determine message keys and arguments for the kick notification
        var (messageKey, activityMessageKey, centerArgs, adminActivityArgs) =
            ("sa_player_kick_message", "sa_admin_kick_message",
                new object[] { reason, "CALLER" },
                new object[] { "CALLER", player.PlayerName, reason });

        // Display center message to the kicked player
        Helper.DisplayCenterMessage(player, messageKey, callerName, centerArgs);

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Schedule the kick for the player
        if (player.UserId.HasValue)
        {
            Helper.KickPlayer(player.UserId.Value, NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED, Config.OtherSettings.KickTime);
        }

        // Log the command and send Discord notification
        if (command == null)
            Helper.LogCommand(caller, $"css_kick {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {reason}");

        SimpleAdminApi?.OnPlayerPenaltiedEvent(playerInfo, adminInfo, PenaltyType.Kick, reason, -1, null);
    }

    /// <summary>
    /// Changes the current map to the specified map name or workshop map ID.
    /// </summary>
    /// <param name="caller">The player issuing the map change.</param>
    /// <param name="command">The command containing the map name or ID.</param>
    [RequiresPermissions("@css/changemap")]
    [CommandHelper(minArgs: 1, usage: "<mapname>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var map = command.GetCommandString.Split(" ")[1];
        ChangeMap(caller, map, command);
    }

    /// <summary>
    /// Changes to a specified map, validating it or handling workshop maps, and notifying the server and admins.
    /// </summary>
    /// <param name="caller">The player issuing the change.</param>
    /// <param name="map">The map name or identifier.</param>
    /// <param name="command">Optional command object for logging and replies.</param>
    public void ChangeMap(CCSPlayerController? caller, string map, CommandInfo? command = null)
    {
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";
        map = map.ToLower();

        var wsMaps = Config.WorkshopMaps;

        if (map.StartsWith("ws:"))
        {
            var issuedCommand = long.TryParse(map.Replace("ws:", ""), out var mapId)
                ? $"host_workshop_map {mapId}"
                : $"ds_workshop_changelevel {map.Replace("ws:", "")}";

            AddTimer(3.0f, () =>
            {
                Server.ExecuteCommand(issuedCommand);
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }
        else
        {
            if (!Server.IsMapValid(map))
            {
                if (wsMaps.ContainsKey(map))
                {
                    map = wsMaps.Where(m => m.Key == map).FirstOrDefault().Key;
                }
                else
                {
                    var msg = $"Map {map} not found.";
                    if (command != null)
                        command.ReplyToCommand(msg);
                    else if (caller != null && caller.IsValid)
                        caller.PrintToChat(msg);
                    else
                        Server.PrintToConsole(msg);
                    return;
                }
            }

            AddTimer(3.0f, () =>
            {
                Server.ExecuteCommand($"changelevel {map}");
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }

        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_changemap_message",
                new object[] { "CALLER", map });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        Helper.LogCommand(caller, command?.GetCommandString ?? $"css_map {map}");
    }

    /// <summary>
    /// Changes the current map to a workshop map specified by name or ID.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">The command containing the workshop map identifier.</param>
    [CommandHelper(1, "<name or id>")]
    [RequiresPermissions("@css/changemap")]
    public void OnWorkshopMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var map = command.GetArg(1);
        ChangeWorkshopMap(caller, map, command);
    }

    /// <summary>
    /// Changes to a specified workshop map by name or ID and notifies admins.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="map">The workshop map identifier.</param>
    /// <param name="command">Optional command for logging.</param>
    public void ChangeWorkshopMap(CCSPlayerController? caller, string map, CommandInfo? command = null)
    {
        map = map.ToLower();
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Determine the workshop command
        var issuedCommand = long.TryParse(map, out var mapId)
            ? $"host_workshop_map {mapId}"
            : $"ds_workshop_changelevel {map}";

        // Define the admin activity message and arguments
        var activityMessageKey = "sa_admin_changemap_message";

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, ["CALLER", map]);
        }

        // Add timer to execute the map change command after a delay
        AddTimer(3.0f, () =>
        {
            Server.ExecuteCommand(issuedCommand);
        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

        // Log the command for the change
        Helper.LogCommand(caller, command?.GetCommandString ?? $"css_wsmap {map}");
    }

    /// <summary>
    /// Allows changing a console variable's value.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">The command with cvar name and value.</param>
    [CommandHelper(2, "<cvar> <value>")]
    [RequiresPermissions("@css/cvar")]
    public void OnCvarCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var cvar = ConVar.Find(command.GetArg(1));
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        if (cvar == null)
        {
            command.ReplyToCommand($"Cvar \"{command.GetArg(1)}\" not found.");
            return;
        }

        if (cvar.Name.Equals("sv_cheats") && !AdminManager.PlayerHasPermissions(new SteamID(caller!.SteamID), "@css/cheats"))
        {
            command.ReplyToCommand($"You don't have permissions to change \"{command.GetArg(1)}\".");
            return;
        }

        Helper.LogCommand(caller, command);
        var value = command.GetArg(2);
        Server.ExecuteCommand($"{cvar.Name} {value}");
        command.ReplyToCommand($"{callerName} changed cvar {cvar.Name} to {value}.");
        Logger.LogInformation($"{callerName} changed cvar {cvar.Name} to {value}.");
    }

    /// <summary>
    /// Executes an RCON command on the server.
    /// </summary>
    /// <param name="caller">The player issuing the command.</param>
    /// <param name="command">The command string to execute via RCON.</param>
    [CommandHelper(1, "<command>")]
    [RequiresPermissions("@css/rcon")]
    public void OnRconCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        Helper.LogCommand(caller, command);
        Server.ExecuteCommand(command.ArgString);
        command.ReplyToCommand($"{callerName} executed command {command.ArgString}.");
        Logger.LogInformation($"{callerName} executed command ({command.ArgString}).");
    }

    /// <summary>
    /// Restarts the game.
    /// </summary>
    /// <param name="caller">The player or console initiating the restart.</param>
    /// <param name="command">The restart command info.</param>
    [RequiresPermissions("@css/generic")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRestartCommand(CCSPlayerController? caller, CommandInfo command)
    {
        RestartGame(caller);
    }

    /// <summary>
    /// Opens plugin manager menu for the caller with options to load or unload plugins.
    /// </summary>
    /// <param name="caller">The player opening the plugin manager.</param>
    /// <param name="commandInfo">The command parameters.</param>
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPluginManagerCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null)
            return;

        var pluginManager = Helper.GetPluginManager();
        if (pluginManager == null)
        {
            Logger.LogError("Unable to access PluginManager.");
            return;
        }

        var getLoadedPluginsMethod = pluginManager.GetType().GetMethod("GetLoadedPlugins", BindingFlags.Public | BindingFlags.Instance);
        if (getLoadedPluginsMethod?.Invoke(pluginManager, null) is not IEnumerable plugins)
        {
            Logger.LogError("Unable to retrieve plugins.");
            return;
        }

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var plugin in plugins)
        {
            var pluginType = plugin.GetType();

            // Accessing each property with the Type of the plugin
            var pluginId = pluginType.GetProperty("PluginId")?.GetValue(plugin);
            var state = pluginType.GetProperty("State")?.GetValue(plugin)?.ToString();
            var path = pluginType.GetProperty("FilePath")?.GetValue(plugin)?.ToString();
            path = Path.GetFileName(Path.GetDirectoryName(path));

            // Access nested properties within "Plugin" (like ModuleName, ModuleVersion, etc.)
            var nestedPlugin = pluginType.GetProperty("Plugin")?.GetValue(plugin);
            if (nestedPlugin == null) continue;

            var status = state?.ToUpper() != "UNLOADED" ? "ON" : "OFF";

            status = state?.ToUpper() != "UNLOADED"
                    ? "<font color='lime'>ON</font>"
                    : "<font color='red'>OFF</font>";

            var nestedType = nestedPlugin.GetType();
            var moduleName = nestedType.GetProperty("ModuleName")?.GetValue(nestedPlugin)?.ToString() ?? "Unknown";
            var moduleVersion = nestedType.GetProperty("ModuleVersion")?.GetValue(nestedPlugin)?.ToString();

            string label = $"({status}) [{moduleName} {moduleVersion}]";
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(label)]));

            optionMap[i++] = () =>
            {
                if (state?.ToUpper() != "UNLOADED")
                {
                    caller.SendLocalizedMessage(Localizer, "sa_menu_pluginsmanager_unloaded", moduleName);
                    Server.ExecuteCommand($"css_plugins unload {pluginId}");
                }
                else
                {
                    caller.SendLocalizedMessage(Localizer, "sa_menu_pluginsmanager_loaded", moduleName);
                    Server.ExecuteCommand($"css_plugins load {path}");
                }

                // refresh menu after a short delay
                AddTimer(0.1f, () => OnPluginManagerCommand(caller, commandInfo));
            };
        }

        // Only show if there are plugins
        if (i > 0)
        {
            Menu?.ShowScrollableMenu(
                caller,
                Localizer["sa_menu_pluginsmanager_title"],
                items,
                (buttons, menu, selected) =>
                {
                    if (selected == null) return;

                    if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                    {
                        action.Invoke();
                    }
                },
                true, freezePlayer: false, disableDeveloper: true);
        }

    }

    /// <summary>
    /// Restarts the game process by issuing the restart game command to the server and logging the action.
    /// </summary>
    /// <param name="admin">The admin or console requesting the restart.</param>
    public static void RestartGame(CCSPlayerController? admin)
    {
        Helper.LogCommand(admin, "css_restartgame");

        // TODO: Localize
        var name = admin == null ? _localizer?["sa_console"] ?? "Console" : admin.PlayerName;
        Server.PrintToChatAll($"[SA] {name}: Restarting game...");
        Server.ExecuteCommand("mp_restartgame 2");
    }
}