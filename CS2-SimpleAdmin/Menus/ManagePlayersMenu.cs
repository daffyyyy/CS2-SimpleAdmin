using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_SimpleAdminApi;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin.Menus;

public static class ManagePlayersMenu
{
    public static void OpenMenu(CCSPlayerController admin)
    {
        if (!admin.IsValid) return;

        var localizer = CS2_SimpleAdmin._localizer;
        if (!AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/generic"))
        {
            admin.PrintToChat(localizer?["sa_prefix"] ?? "[SimpleAdmin] " +
                            (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command"));
            return;
        }

        var menuTitle = localizer?["sa_menu_players_manage"] ?? "Manage Players";
        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        void AddOption(string name, Action action, bool disabled = false)
        {
            if (disabled) return; // skip disabled options
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(name)]));
            optionMap[i++] = action;
        }

        // Permissions
        var hasSlay = AdminManager.CommandIsOverriden("css_slay")
            ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_slay"))
            : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/slay");

        var hasKick = AdminManager.CommandIsOverriden("css_kick")
            ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_kick"))
            : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/kick");

        var hasBan = AdminManager.CommandIsOverriden("css_ban")
            ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_ban"))
            : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/ban");

        var hasChat = AdminManager.CommandIsOverriden("css_gag")
            ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_gag"))
            : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/chat");

        // Add menu options
        if (hasSlay)
        {
            AddOption(localizer?["sa_slap"] ?? "Slap", () => PlayersMenu.OpenMenu(admin, localizer?["sa_slap"] ?? "Slap", SlapMenu));
            AddOption(localizer?["sa_slay"] ?? "Slay", () => PlayersMenu.OpenMenu(admin, localizer?["sa_slay"] ?? "Slay", Slay));
        }

        if (hasKick)
        {
            AddOption(localizer?["sa_kick"] ?? "Kick", () => PlayersMenu.OpenMenu(admin, localizer?["sa_kick"] ?? "Kick", KickMenu));
        }

        if (AdminManager.CommandIsOverriden("css_warn")
            ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_warn"))
            : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/kick"))
        {
            AddOption(localizer?["sa_warn"] ?? "Warn",
                () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_warn"] ?? "Warn",
                    (adminController, player) => DurationMenu.OpenMenu(adminController, $"{localizer?["sa_warn"] ?? "Warn"}: {player.PlayerName}", player, WarnMenu)));
        }

        if (hasBan)
        {
            AddOption(localizer?["sa_ban"] ?? "Ban",
                () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_ban"] ?? "Ban",
                    (adminController, player) => DurationMenu.OpenMenu(adminController, $"{localizer?["sa_ban"] ?? "Ban"}: {player.PlayerName}", player, BanMenu)));
        }

        if (hasChat)
        {
            if (AdminManager.CommandIsOverriden("css_gag")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_gag"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/chat"))
            {
                AddOption(localizer?["sa_gag"] ?? "Gag",
                    () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_gag"] ?? "Gag",
                        (adminController, player) => DurationMenu.OpenMenu(adminController, $"{localizer?["sa_gag"] ?? "Gag"}: {player.PlayerName}", player, GagMenu)));
            }

            if (AdminManager.CommandIsOverriden("css_mute")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_mute"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/chat"))
            {
                AddOption(localizer?["sa_mute"] ?? "Mute",
                    () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_mute"] ?? "Mute",
                        (adminController, player) => DurationMenu.OpenMenu(adminController, $"{localizer?["sa_mute"] ?? "Mute"}: {player.PlayerName}", player, MuteMenu)));
            }

            if (AdminManager.CommandIsOverriden("css_silence")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_silence"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/chat"))
            {
                AddOption(localizer?["sa_silence"] ?? "Silence",
                    () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_silence"] ?? "Silence",
                        (adminController, player) => DurationMenu.OpenMenu(adminController, $"{localizer?["sa_silence"] ?? "Silence"}: {player.PlayerName}", player, SilenceMenu)));
            }
        }

        if (AdminManager.CommandIsOverriden("css_team")
            ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_team"))
            : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/kick"))
        {
            AddOption(localizer?["sa_team_force"] ?? "Force Team", () => PlayersMenu.OpenMenu(admin, localizer?["sa_team_force"] ?? "Force Team", ForceTeamMenu));
        }

        if (i == 0) return; // nothing to show

        // Show scrollable menu
        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuTitle,
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;
                if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                    action.Invoke();
            },
            true, freezePlayer: false, disableDeveloper: true);
    }

    private static void SlapMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_slap"] ?? "Slap"}: {player.PlayerName}";

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        void AddOption(string name, Action action, bool disabled = false)
        {
            if (disabled) return; // skip disabled options
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(name)]));
            optionMap[i++] = action;
        }

        // Add slap options
        AddOption("0 hp", () => ApplySlapAndKeepMenu(admin, player, 0));
        AddOption("1 hp", () => ApplySlapAndKeepMenu(admin, player, 1));
        AddOption("5 hp", () => ApplySlapAndKeepMenu(admin, player, 5));
        AddOption("10 hp", () => ApplySlapAndKeepMenu(admin, player, 10));
        AddOption("50 hp", () => ApplySlapAndKeepMenu(admin, player, 50));
        AddOption("100 hp", () => ApplySlapAndKeepMenu(admin, player, 100));

        if (i == 0) return; // nothing to show

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuTitle,
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;
                if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                    action.Invoke();
            },
            true, freezePlayer: false, disableDeveloper: true);
    }

    private static void ApplySlapAndKeepMenu(CCSPlayerController admin, CCSPlayerController player, int damage)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.Slap(admin, player, damage);
        SlapMenu(admin, player);
    }

    private static void Slay(CCSPlayerController admin, CCSPlayerController player)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.Slay(admin, player);
    }

    private static void KickMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        ReasonMenu.OpenMenu(admin, PenaltyType.Kick,
            $"{CS2_SimpleAdmin._localizer?["sa_kick"] ?? "Kick"}: {player.PlayerName}", player, (_, _, reason) =>
        {
            if (player is { IsValid: true })
                Kick(admin, player, reason);
        });

        // var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_kick"] ?? "Kick"}: {player?.PlayerName}");
        //
        // foreach (var option in CS2_SimpleAdmin.Instance.Config.MenuConfigs.KickReasons)
        // {
        // 	menu?.AddMenuOption(option, (_, _) =>
        // 	{
        // 		if (player is { IsValid: true })
        // 			Kick(admin, player, option);
        // 	});
        // }
        //
        // if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void Kick(CCSPlayerController admin, CCSPlayerController player, string? reason)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.Instance.Kick(admin, player, reason, admin.PlayerName);
    }

    internal static void BanMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
    {
        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_ban"] ?? "Ban"}: {player.PlayerName}";

        // Open the reason menu as a scrollable menu
        ReasonMenu.OpenMenu(
            admin,
            PenaltyType.Ban,
            menuTitle,
            player,
            (_, _, reason) =>
            {
                if (player is { IsValid: true })
                    Ban(admin, player, duration, reason);

                // Close menu after selection
                CS2_SimpleAdmin.Menu.ClearMenus(admin);
            });
    }


    private static void Ban(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.Instance.Ban(admin, player, duration, reason, admin.PlayerName);
    }

    private static void WarnMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
    {
        ReasonMenu.OpenMenu(admin, PenaltyType.Warn,
            $"{CS2_SimpleAdmin._localizer?["sa_warn"] ?? "Warn"}: {player.PlayerName}", player, (_, _, reason) =>
            {
                if (player is { IsValid: true })
                {
                    Warn(admin, player, duration, reason);
                }
            });

        // var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_warn"] ?? "Warn"}: {player?.PlayerName}");
        //
        // foreach (var option in CS2_SimpleAdmin.Instance.Config.MenuConfigs.WarnReasons)
        // {
        // 	menu?.AddMenuOption(option, (_, _) =>
        // 	{
        // 		if (player is { IsValid: true })
        // 			Warn(admin, player, duration, option);
        // 	});
        // }
        //
        // if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void Warn(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.Instance.Warn(admin, player, duration, reason, admin.PlayerName);
    }

    internal static void GagMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
    {
        ReasonMenu.OpenMenu(admin, PenaltyType.Gag,
            $"{CS2_SimpleAdmin._localizer?["sa_gag"] ?? "Gag"}: {player.PlayerName}", player, (_, _, reason) =>
            {
                if (player is { IsValid: true })
                    Gag(admin, player, duration, reason);
            });

        // var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_gag"] ?? "Gag"}: {player?.PlayerName}");
        //
        // foreach (var option in CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons)
        // {
        // 	menu?.AddMenuOption(option, (_, _) =>
        // 	{
        // 		if (player is { IsValid: true })
        // 			Gag(admin, player, duration, option);
        // 	});
        // }
        //
        // if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void Gag(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.Instance.Gag(admin, player, duration, reason);
    }

    internal static void MuteMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
    {
        ReasonMenu.OpenMenu(admin, PenaltyType.Mute,
            $"{CS2_SimpleAdmin._localizer?["sa_mute"] ?? "mute"}: {player.PlayerName}", player, (_, _, reason) =>
            {
                if (player is { IsValid: true })
                    Mute(admin, player, duration, reason);
            });

        // // TODO: Localize and make options in config?
        // var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_mute"] ?? "Mute"}: {player?.PlayerName}");
        //
        // foreach (var option in CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons)
        // {
        // 	menu?.AddMenuOption(option, (_, _) =>
        // 	{
        // 		if (player is { IsValid: true })
        // 			Mute(admin, player, duration, option);
        // 	});
        // }
        //
        // if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void Mute(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.Instance.Mute(admin, player, duration, reason);
    }

    internal static void SilenceMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
    {
        ReasonMenu.OpenMenu(admin, PenaltyType.Silence,
            $"{CS2_SimpleAdmin._localizer?["sa_silence"] ?? "Silence"}: {player.PlayerName}", player, (_, _, reason) =>
            {
                if (player is { IsValid: true })
                    Silence(admin, player, duration, reason);
            });

        // // TODO: Localize and make options in config?
        // var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_silence"] ?? "Silence"}: {player?.PlayerName}");
        //
        // foreach (var option in CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons)
        // {
        // 	menu?.AddMenuOption(option, (_, _) =>
        // 	{
        // 		if (player is { IsValid: true })
        // 			Silence(admin, player, duration, option);
        // 	});
        // }
        //
        // if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void Silence(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.Instance.Silence(admin, player, duration, reason);
    }

    private static void ForceTeamMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_team_force"] ?? "Force Team"} {player.PlayerName}";

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        void AddOption(string name, Action action, bool disabled = false)
        {
            if (disabled) return; // skip disabled options
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(name)]));
            optionMap[i++] = action;
        }

        AddOption(CS2_SimpleAdmin._localizer?["sa_team_ct"] ?? "CT",
            () => ForceTeam(admin, player, "ct", CsTeam.CounterTerrorist));

        AddOption(CS2_SimpleAdmin._localizer?["sa_team_t"] ?? "T",
            () => ForceTeam(admin, player, "t", CsTeam.Terrorist));

        AddOption(CS2_SimpleAdmin._localizer?["sa_team_swap"] ?? "Swap",
            () => ForceTeam(admin, player, "swap", CsTeam.Spectator));

        AddOption(CS2_SimpleAdmin._localizer?["sa_team_spec"] ?? "Spec",
            () => ForceTeam(admin, player, "spec", CsTeam.Spectator));

        if (i == 0) return; // nothing to show

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuTitle,
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;
                if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                    action.Invoke();
            },
            true, freezePlayer: false, disableDeveloper: true);
    }


    private static void ForceTeam(CCSPlayerController admin, CCSPlayerController player, string teamName, CsTeam teamNum)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.ChangeTeam(admin, player, teamName, teamNum, true);
    }
}