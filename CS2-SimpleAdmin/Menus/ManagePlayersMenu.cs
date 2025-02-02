using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_SimpleAdminApi;

namespace CS2_SimpleAdmin.Menus;

public static class ManagePlayersMenu
{
    public static void OpenMenu(CCSPlayerController admin)
    {
        if (admin.IsValid == false)
            return;

        var localizer = CS2_SimpleAdmin._localizer;
        if (AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/generic") == false)
        {
            admin.PrintToChat(localizer?["sa_prefix"] ??
                              "[SimpleAdmin] " +
                              (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command")
            );
            return;
        }

        var menu = AdminMenu.CreateMenu(localizer?["sa_menu_players_manage"] ?? "Manage Players");
        List<ChatMenuOptionData> options = [];

        // permissions
        var hasSlay = AdminManager.CommandIsOverriden("css_slay") ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_slay")) : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/slay");
        var hasKick = AdminManager.CommandIsOverriden("css_kick") ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_kick")) : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/kick");
        var hasBan = AdminManager.CommandIsOverriden("css_ban") ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_ban")) : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/ban");
        var hasChat = AdminManager.CommandIsOverriden("css_gag") ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_gag")) : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/chat");

        // TODO: Localize options
        // options added in order

        if (hasSlay)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_slap"] ?? "Slap", () => PlayersMenu.OpenMenu(admin, localizer?["sa_slap"] ?? "Slap", SlapMenu)));
            options.Add(new ChatMenuOptionData(localizer?["sa_slay"] ?? "Slay", () => PlayersMenu.OpenMenu(admin, localizer?["sa_slay"] ?? "Slay", Slay)));
        }

        if (hasKick)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_kick"] ?? "Kick", () => PlayersMenu.OpenMenu(admin, localizer?["sa_kick"] ?? "Kick", KickMenu)));
        }

        if (AdminManager.CommandIsOverriden("css_warn")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_warn"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/kick"))
            options.Add(new ChatMenuOptionData(localizer?["sa_warn"] ?? "Warn", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_warn"] ?? "Warn", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_warn"] ?? "Warn"}: {player.PlayerName}", player, WarnMenu))));

        if (hasBan)
            options.Add(new ChatMenuOptionData(localizer?["sa_ban"] ?? "Ban", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_ban"] ?? "Ban", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_ban"] ?? "Ban"}: {player.PlayerName}", player, BanMenu))));

        if (hasChat)
        {
            if (AdminManager.CommandIsOverriden("css_gag")
                    ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_gag"))
                    : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/chat"))
                options.Add(new ChatMenuOptionData(localizer?["sa_gag"] ?? "Gag", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_gag"] ?? "Gag", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_gag"] ?? "Gag"}: {player.PlayerName}", player, GagMenu))));
            if (AdminManager.CommandIsOverriden("css_mute")
                    ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_mute"))
                    : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/chat"))
                options.Add(new ChatMenuOptionData(localizer?["sa_mute"] ?? "Mute", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_mute"] ?? "Mute", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_mute"] ?? "Mute"}: {player.PlayerName}", player, MuteMenu))));
            if (AdminManager.CommandIsOverriden("css_silence")
                    ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_silence"))
                    : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/chat"))
                options.Add(new ChatMenuOptionData(localizer?["sa_silence"] ?? "Silence", () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_silence"] ?? "Silence", (admin, player) => DurationMenu.OpenMenu(admin, $"{localizer?["sa_silence"] ?? "Silence"}: {player.PlayerName}", player, SilenceMenu))));
        }

        if (AdminManager.CommandIsOverriden("css_team")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_team"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/kick"))
            options.Add(new ChatMenuOptionData(localizer?["sa_team_force"] ?? "Force Team", () => PlayersMenu.OpenMenu(admin, localizer?["sa_team_force"] ?? "Force Team", ForceTeamMenu)));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void SlapMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_slap"] ?? "Slap"}: {player.PlayerName}");
        List<ChatMenuOptionData> options =
        [
			// options added in order
			new ChatMenuOptionData("0 hp", () => ApplySlapAndKeepMenu(admin, player, 0)),
            new ChatMenuOptionData("1 hp", () => ApplySlapAndKeepMenu(admin, player, 1)),
            new ChatMenuOptionData("5 hp", () => ApplySlapAndKeepMenu(admin, player, 5)),
            new ChatMenuOptionData("10 hp", () => ApplySlapAndKeepMenu(admin, player, 10)),
            new ChatMenuOptionData("50 hp", () => ApplySlapAndKeepMenu(admin, player, 50)),
            new ChatMenuOptionData("100 hp", () => ApplySlapAndKeepMenu(admin, player, 100)),
        ];

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
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
        ReasonMenu.OpenMenu(admin, PenaltyType.Ban,
            $"{CS2_SimpleAdmin._localizer?["sa_ban"] ?? "Ban"}: {player.PlayerName}", player, (_, _, reason) =>
            {
                if (player is { IsValid: true })
                    Ban(admin, player, duration, reason);
                
                CS2_SimpleAdmin.MenuApi?.CloseMenu(admin);
            });

        // var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_ban"] ?? "Ban"}: {player?.PlayerName}");
        //
        // foreach (var option in CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons)
        // {
        // 	menu?.AddMenuOption(option, (_, _) =>
        // 	{
        // 		if (player is { IsValid: true })
        // 			Ban(admin, player, duration, option);
        // 	});
        // }
        //
        // if (menu != null) AdminMenu.OpenMenu(admin, menu);
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
                    Warn(admin, player, duration, reason);
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
        // TODO: Localize
        var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_team_force"] ?? "Force Team"} {player.PlayerName}");
        List<ChatMenuOptionData> options =
        [
            new ChatMenuOptionData(CS2_SimpleAdmin._localizer?["sa_team_ct"] ?? "CT", () => ForceTeam(admin, player, "ct", CsTeam.CounterTerrorist)),
            new ChatMenuOptionData(CS2_SimpleAdmin._localizer?["sa_team_t"] ?? "T", () => ForceTeam(admin, player, "t", CsTeam.Terrorist)),
            new ChatMenuOptionData(CS2_SimpleAdmin._localizer?["sa_team_swap"] ?? "Swap", () => ForceTeam(admin, player, "swap", CsTeam.Spectator)),
            new ChatMenuOptionData(CS2_SimpleAdmin._localizer?["sa_team_spec"] ?? "Spec", () => ForceTeam(admin, player, "spec", CsTeam.Spectator)),
        ];

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void ForceTeam(CCSPlayerController admin, CCSPlayerController player, string teamName, CsTeam teamNum)
    {
        if (player is not { IsValid: true }) return;

        CS2_SimpleAdmin.ChangeTeam(admin, player, teamName, teamNum, true);
    }
}