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
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using MenuManager;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    [CommandHelper(usage: "[#userid or name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPenaltiesCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.IsValid == false || !caller.UserId.HasValue || Database == null)
            return;
        
        var userId = caller.UserId.Value;
        
        if (!string.IsNullOrEmpty(command.GetArg(1)) && AdminManager.PlayerHasPermissions(caller, "@css/kick"))
        {
            var targets = GetTarget(command);
            
            if (targets == null)
                return;
            
            var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();
            playersToTarget.ForEach(player =>
            {
                if (!player.UserId.HasValue) return;
                if (!caller!.CanTarget(player)) return;

                userId = player.UserId.Value;
            });
        }

        Task.Run(async () =>
        {
            try
            {
                var warns = await WarnManager.GetPlayerWarns(PlayersInfo[userId], false);

                // Check if the player is muted
                var activeMutes = await MuteManager.IsPlayerMuted(PlayersInfo[userId].SteamId.SteamId64.ToString());

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

                await Server.NextFrameAsync(() =>
                {
                    caller.SendLocalizedMessage(_localizer, "sa_player_penalty_info",
                    [
                        PlayersInfo[userId].Name,
                        PlayersInfo[userId].TotalBans,
                        PlayersInfo[userId].TotalGags,
                        PlayersInfo[userId].TotalMutes,
                        PlayersInfo[userId].TotalSilences,
                        PlayersInfo[userId].TotalWarns,
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

    [RequiresPermissions("@css/generic")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAdminCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.IsValid == false)
            return;

        AdminMenu.OpenMenu(caller);
    }

    [RequiresPermissions("@css/generic")]
    public void OnAdminHelpCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var lines = File.ReadAllLines(ModuleDirectory + "/admin_help.txt");

        foreach (var line in lines)
        {
            command.ReplyToCommand(string.IsNullOrWhiteSpace(line) ? " " : line.ReplaceColorTags());
        }
    }

    [CommandHelper(minArgs: 4, usage: "<steamid> <name> <flags/groups> <immunity> <duration>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnAddAdminCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;


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
        int.TryParse(command.GetArg(5), out var time);

        AddAdmin(caller, steamid, name, flags, immunity, time, globalAdmin, command);
    }

    public static void AddAdmin(CCSPlayerController? caller, string steamid, string name, string flags, int immunity, int time = 0, bool globalAdmin = false, CommandInfo? command = null)
    {
        if (Database == null) return;
        
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

    [CommandHelper(minArgs: 1, usage: "<steamid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnDelAdminCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
        {
            command.ReplyToCommand($"Invalid SteamID64.");
            return;
        }

        var globalDelete = command.GetArg(2).ToLower().Equals("-g");

        RemoveAdmin(caller, steamId.SteamId64.ToString(), globalDelete, command);
    }

    public void RemoveAdmin(CCSPlayerController? caller, string steamid, bool globalDelete = false, CommandInfo? command = null)
    {
        if (Database == null) return;
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

    [CommandHelper(minArgs: 3, usage: "<group_name> <flags> <immunity>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnAddGroup(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

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

    private static void AddGroup(CCSPlayerController? caller, string name, string flags, int immunity, bool globalGroup, CommandInfo? command = null)
    {
        if (Database == null) return;

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

    [CommandHelper(minArgs: 1, usage: "<group_name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnDelGroupCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        if (!command.GetArg(1).StartsWith($"#"))
        {
            command.ReplyToCommand($"Group name must start with #.");
            return;
        }

        var groupName = command.GetArg(1);

        RemoveGroup(caller, groupName, command);
    }

    private void RemoveGroup(CCSPlayerController? caller, string name, CommandInfo? command = null)
    {
        if (Database == null) return;
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

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnRelAdminCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        ReloadAdmins(caller);

        command.ReplyToCommand("Reloaded sql admins and groups");
    }

    public void ReloadAdmins(CCSPlayerController? caller)
    {
        if (Database == null) return;

        for (var index = 0; index < PermissionManager.AdminCache.Keys.ToList().Count; index++)
        {
            var steamId = PermissionManager.AdminCache.Keys.ToList()[index];
            if (!PermissionManager.AdminCache.TryRemove(steamId, out _)) continue;

            AdminManager.ClearPlayerPermissions(steamId);
            AdminManager.RemovePlayerAdminData(steamId);
        }

        Task.Run(async () =>
        {
            await PermissionManager.CrateGroupsJsonFile();
            await PermissionManager.CreateAdminsJsonFile();

            var adminsFile = await File.ReadAllTextAsync(Instance.ModuleDirectory + "/data/admins.json");
            var groupsFile = await File.ReadAllTextAsync(Instance.ModuleDirectory + "/data/groups.json");

            await Server.NextFrameAsync(() =>
            {
                if (!string.IsNullOrEmpty(adminsFile))
                    AddTimer(0.5f, () => AdminManager.LoadAdminData(ModuleDirectory + "/data/admins.json"));
                if (!string.IsNullOrEmpty(groupsFile))
                    AddTimer(1.0f, () => AdminManager.LoadAdminGroups(ModuleDirectory + "/data/groups.json"));
                if (!string.IsNullOrEmpty(adminsFile))
                    AddTimer(1.5f, () => AdminManager.LoadAdminData(ModuleDirectory + "/data/admins.json"));
            });
        });

        //_ = _adminManager.GiveAllGroupsFlags();
        //_ = _adminManager.GiveAllFlags();
    }

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
            caller.ChangeTeam(CsTeam.Spectator);
        }
        else
        {
            Server.ExecuteCommand("sv_disable_teamselect_menu 1");

            if (caller.PlayerPawn.Value != null && caller.PawnIsAlive)
                caller.PlayerPawn.Value.CommitSuicide(true, false);

            AddTimer(1.0f, () => { Server.NextFrame(() => caller.ChangeTeam(CsTeam.Spectator)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(1.4f, () => { Server.NextFrame(() => caller.ChangeTeam(CsTeam.None)); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            caller.PrintToChat($"You are hidden now!");
            AddTimer(2.0f, () => { Server.NextFrame(() => Server.ExecuteCommand("sv_disable_teamselect_menu 0")); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }
    }

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

    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/generic")]
    public void OnWhoCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null) return;

        var targets = GetTarget(command);
        if (targets == null) return;

        Helper.LogCommand(caller, command);

        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (!player.UserId.HasValue) return;
            if (!caller!.CanTarget(player)) return;

            var playerInfo = PlayersInfo[player.UserId.Value];

            Task.Run(async () =>
            {
                await Server.NextFrameAsync(() =>
                {
                    Action<string> printMethod = caller == null ? Server.PrintToConsole : caller.PrintToConsole;

                    printMethod($"--------- INFO ABOUT \"{playerInfo.Name}\" ---------");

                    printMethod($"• Clan: \"{player.Clan}\" Name: \"{playerInfo.Name}\"");
                    printMethod($"• UserID: \"{playerInfo.UserId}\"");
                    printMethod($"• SteamID64: \"{playerInfo.SteamId.SteamId64}\"");
                    if (player.Connected == PlayerConnectedState.PlayerConnected)
                    {
                        printMethod($"• SteamID2: \"{playerInfo.SteamId.SteamId2}\"");
                        printMethod($"• Community link: \"{playerInfo.SteamId.ToCommunityUrl()}\"");
                    }
                    if (playerInfo.IpAddress != null && AdminManager.PlayerHasPermissions(caller, "@css/showip"))
                        printMethod($"• IP Address: \"{playerInfo.IpAddress}\"");
                    printMethod($"• Ping: \"{player.Ping}\"");
                    if (player.Connected == PlayerConnectedState.PlayerConnected)
                    {
                        printMethod($"• Total Bans: \"{playerInfo.TotalBans}\"");
                        printMethod($"• Total Gags: \"{playerInfo.TotalGags}\"");
                        printMethod($"• Total Mutes: \"{playerInfo.TotalMutes}\"");
                        printMethod($"• Total Silences: \"{playerInfo.TotalSilences}\"");
                        printMethod($"• Total Warns: \"{playerInfo.TotalWarns}\"");
                    }

                    printMethod($"--------- END INFO ABOUT \"{player.PlayerName}\" ---------");
                });
            });
        });
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/kick")]
    public void OnDisconnectedCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (_localizer == null || caller == null) return;

        var disconnectedMenu = Helper.CreateMenu(_localizer["sa_menu_disconnected_title"]);

        DisconnectedPlayers.ForEach(player =>
        {
            disconnectedMenu?.AddMenuOption(player.Name, (_, _) =>
            {
                var disconnectedMenuAction = Helper.CreateMenu(_localizer["sa_menu_disconnected_action_title"]);
                disconnectedMenuAction?.AddMenuOption(_localizer["sa_ban"], (_, _) =>
                {
                    DurationMenu.OpenMenu(caller, _localizer["sa_ban"], player, (_, _, duration) =>
                        ReasonMenu.OpenMenu(caller, PenaltyType.Ban, _localizer["sa_reason"], player, (_, _, reason) =>
                        {
                            caller.ExecuteClientCommandFromServer($"css_addban {player.SteamId.SteamId64} {duration} \"{reason}\"");
                        }));
                });
                disconnectedMenuAction?.AddMenuOption(_localizer["sa_mute"], (_, _) =>
                {
                    DurationMenu.OpenMenu(caller, _localizer["sa_mute"], player, (_, _, duration) =>
                        ReasonMenu.OpenMenu(caller, PenaltyType.Mute, _localizer["sa_reason"], player, (_, _, reason) =>
                        {
                            caller.ExecuteClientCommandFromServer($"css_addmute {player.SteamId.SteamId64} {duration} \"{reason}\"");
                        }));
                });
                disconnectedMenuAction?.AddMenuOption(_localizer["sa_gag"], (_, _) =>
                {
                    DurationMenu.OpenMenu(caller, _localizer["sa_gag"], player, (_, _, duration) =>
                        ReasonMenu.OpenMenu(caller, PenaltyType.Mute, _localizer["sa_reason"], player, (_, _, reason) =>
                        {
                            caller.ExecuteClientCommandFromServer($"css_addgag {player.SteamId.SteamId64} {duration} \"{reason}\"");
                        }));
                });
                disconnectedMenuAction?.AddMenuOption(_localizer["sa_silence"], (_, _) =>
                {
                    DurationMenu.OpenMenu(caller, _localizer["sa_silence"], player, (_, _, duration) =>
                        ReasonMenu.OpenMenu(caller, PenaltyType.Mute, _localizer["sa_reason"], player, (_, _, reason) =>
                        {
                            caller.ExecuteClientCommandFromServer($"css_addsilence {player.SteamId.SteamId64} {duration} \"{reason}\"");
                        }));
                });

                disconnectedMenuAction?.Open(caller);
            });
        });

        disconnectedMenu?.Open(caller);
    }

    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/kick")]
    public void OnWarnsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (Database == null || _localizer == null || caller == null) return;

        var targets = GetTarget(command);
        if (targets == null) return;

        Helper.LogCommand(caller, command);

        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsBot: false }).ToList();

        if (playersToTarget.Count > 1)
            return;

        playersToTarget.ForEach(player =>
        {
            if (!player.UserId.HasValue) return;
            if (!caller!.CanTarget(player)) return;

            var userId = player.UserId.Value;

            IMenu? warnsMenu = Helper.CreateMenu(_localizer["sa_admin_warns_menu_title", player.PlayerName]);

            Task.Run(async () =>
            {
                var warnsList = await WarnManager.GetPlayerWarns(PlayersInfo[userId], false);
                var sortedWarns = warnsList
                    .OrderBy(warn => (string)warn.status == "ACTIVE" ? 0 : 1)
                    .ThenByDescending(warn => (int)warn.id)
                    .ToList();

                sortedWarns.ForEach(w =>
                {
                    warnsMenu?.AddMenuOption($"[{((string)w.status == "ACTIVE" ? $"{ChatColors.LightRed}X" : $"{ChatColors.Lime}✔️")}{ChatColors.Default}] {(string)w.reason}",
                        (controller, option) =>
                        {
                            _ = WarnManager.UnwarnPlayer(PlayersInfo[userId], (int)w.id);
                            player.PrintToChat(_localizer["sa_admin_warns_unwarn", player.PlayerName, (string)w.reason]);
                        });
                });

                await Server.NextFrameAsync(() =>
                {
                    warnsMenu?.Open(caller);
                });
            });
        });
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/generic")]
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
                caller.PrintToConsole($"--------- PLAYER LIST ---------");
                playersToTarget.ForEach(player =>
                {
                    caller.PrintToConsole(
                        $"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{(AdminManager.PlayerHasPermissions(caller, "@css/showip") ? player.IpAddress?.Split(":")[0] : "Unknown")}\" SteamID64: \"{player.SteamID}\")");
                });
                caller.PrintToConsole($"--------- END PLAYER LIST ---------");
            }
            else
            {
                Server.PrintToConsole($"--------- PLAYER LIST ---------");
                playersToTarget.ForEach(player =>
                {
                    Server.PrintToConsole($"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{player.IpAddress?.Split(":")[0]}\" SteamID64: \"{player.SteamID}\")");
                });
                Server.PrintToConsole($"--------- END PLAYER LIST ---------");
            }
        }
        else
        {
            var playersJson = JsonConvert.SerializeObject(playersToTarget.Select((CCSPlayerController player) =>
            {
                var matchStats = player.ActionTrackingServices?.MatchStats;

                return new
                {
                    player.UserId,
                    Name = player.PlayerName,
                    SteamId = player.SteamID.ToString(),
                    IpAddress = AdminManager.PlayerHasPermissions(caller, "@css/showip") ? player.IpAddress?.Split(":")[0] ?? "Unknown" : "Unknown",
                    player.Ping,
                    IsAdmin = AdminManager.PlayerHasPermissions(player, "@css/ban") || AdminManager.PlayerHasPermissions(player, "@css/generic"),
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

    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        var reason = _localizer?["sa_unknown"] ?? "Unknown";

        var targets = GetTarget(command);

        if (targets == null) return;
        var playersToTarget = targets.Players
            .Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        if (playersToTarget.Count > 1 && Config.OtherSettings.DisableDangerousCommands || playersToTarget.Count == 0)
        {
            return;
        }

        if (command.ArgCount >= 2 && command.GetArg(2).Length > 0)
            reason = command.GetArg(2);

        playersToTarget.ForEach(player =>
        {
            if (!player.IsValid)
                return;

            if (caller!.CanTarget(player))
            {
                Kick(caller, player, reason, callerName, command);
            }
        });
    }

    public void Kick(CCSPlayerController? caller, CCSPlayerController player, string? reason = "Unknown", string? callerName = null, CommandInfo? command = null)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;
        if (!player.UserId.HasValue) return;
        
        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";
        reason ??= _localizer?["sa_unknown"] ?? "Unknown";
        
        var playerInfo = PlayersInfo[player.UserId.Value];
        var adminInfo = caller != null && caller.UserId.HasValue ? PlayersInfo[caller.UserId.Value] : null;

        // Freeze player pawn if alive
        if (player.PawnIsAlive)
        {
            player.Pawn.Value?.Freeze();
        }

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
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }

        // Schedule the kick for the player
        if (player.UserId.HasValue)
        {
            AddTimer(Config.OtherSettings.KickTime, () =>
            {
                if (player.IsValid)
                {
                    Helper.KickPlayer(player.UserId.Value);
                }
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Log the command and send Discord notification
        if (command == null)
            Helper.LogCommand(caller, $"css_kick {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {reason}");
        else
            Helper.LogCommand(caller, command);
        
        SimpleAdminApi?.OnPlayerPenaltiedEvent(playerInfo, adminInfo, PenaltyType.Kick, reason);
    }

    [RequiresPermissions("@css/changemap")]
    [CommandHelper(minArgs: 1, usage: "<mapname>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var map = command.GetCommandString.Split(" ")[1];
        ChangeMap(caller, map, command);
    }

    public void ChangeMap(CCSPlayerController? caller, string map, CommandInfo? command = null)
    {
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";
        map = map.ToLower();

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
                var msg = $"Map {map} not found.";
                if (command != null)
                    command.ReplyToCommand(msg);
                else if (caller != null && caller.IsValid)
                    caller.PrintToChat(msg);
                else
                    Server.PrintToConsole(msg);
                return;
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
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }

        Helper.LogCommand(caller, command?.GetCommandString ?? $"css_map {map}");
    }

    [CommandHelper(1, "<name or id>")]
    [RequiresPermissions("@css/changemap")]
    public void OnWorkshopMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var map = command.GetArg(1);
        ChangeWorkshopMap(caller, map, command);
    }

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
            Helper.ShowAdminActivity(activityMessageKey, callerName, ["CALLER", map]);
        }

        // Add timer to execute the map change command after a delay
        AddTimer(3.0f, () =>
        {
            Server.ExecuteCommand(issuedCommand);
        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

        // Log the command for the change
        Helper.LogCommand(caller, command?.GetCommandString ?? $"css_wsmap {map}");
    }

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

        if (cvar.Name.Equals("sv_cheats") && !AdminManager.PlayerHasPermissions(caller, "@css/cheats"))
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

    [RequiresPermissions("@css/generic")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRestartCommand(CCSPlayerController? caller, CommandInfo command)
    {
        RestartGame(caller);
    }

    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPluginManagerCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (MenuApi == null || caller == null)
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

        var pluginsMenu = Helper.CreateMenu(Localizer["sa_menu_pluginsmanager_title"]);
        
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
            var allowedMenuTypes = new[] { "chat", "console" };

            if (!allowedMenuTypes.Contains(Config.MenuConfigs.MenuType) && MenuApi.GetMenuType(caller) >= MenuType.CenterMenu)
                status = state?.ToUpper() != "UNLOADED" ? "<font color='lime'>ON</font>" : "<font color='red'>OFF</font>";
            var nestedType = nestedPlugin.GetType();
            var moduleName = nestedType.GetProperty("ModuleName")?.GetValue(nestedPlugin)?.ToString() ?? "Unknown";
            var moduleVersion = nestedType.GetProperty("ModuleVersion")?.GetValue(nestedPlugin)?.ToString();
            // var moduleAuthor = nestedType.GetProperty("ModuleAuthor")?.GetValue(nestedPlugin)?.ToString();
            // var moduleDescription = nestedType.GetProperty("ModuleDescription")?.GetValue(nestedPlugin)?.ToString();

            pluginsMenu?.AddMenuOption($"({status}) [{moduleName} {moduleVersion}]", (_, _) =>
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

                AddTimer(0.1f, () => OnPluginManagerCommand(caller, commandInfo));
            });
                
            // Console.WriteLine($"[#{pluginId}:{state?.ToUpper()}]: \"{moduleName ?? "Unknown"}\" ({moduleVersion ?? "Unknown"}) by {moduleAuthor}");
        }
        
        pluginsMenu?.Open(caller);
    }

    public static void RestartGame(CCSPlayerController? admin)
    {
        Helper.LogCommand(admin, "css_restartgame");

        // TODO: Localize
        var name = admin == null ? _localizer?["sa_console"] ?? "Console" : admin.PlayerName;
        Server.PrintToChatAll($"[SA] {name}: Restarting game...");
        Server.ExecuteCommand("mp_restartgame 2");
    }
}