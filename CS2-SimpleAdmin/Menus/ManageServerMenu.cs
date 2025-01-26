using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdmin.Menus;

public static class ManageServerMenu
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

        var menu = AdminMenu.CreateMenu(localizer?["sa_menu_server_manage"] ?? "Server Manage");
        List<ChatMenuOptionData> options = [];


        // permissions
        var hasMap = AdminManager.CommandIsOverriden("css_map") ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_map")) : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/changemap");
        var hasPlugins = AdminManager.CommandIsOverriden("css_pluginsmanager") ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_pluginsmanager")) : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/root");

        //bool hasMap = AdminManager.PlayerHasPermissions(admin, "@css/changemap");

        // options added in order

        if (hasPlugins)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_pluginsmanager_title"] ?? "Manage Plugins", () => admin.ExecuteClientCommandFromServer("css_pluginsmanager")));
        }

        if (hasMap)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_changemap"] ?? "Change Map", () => ChangeMapMenu(admin)));
        }

        options.Add(new ChatMenuOptionData(localizer?["sa_restart_game"] ?? "Restart Game", () => CS2_SimpleAdmin.RestartGame(admin)));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void ChangeMapMenu(CCSPlayerController admin)
    {
        var menu = AdminMenu.CreateMenu(CS2_SimpleAdmin._localizer?["sa_changemap"] ?? "Change Map");
        List<ChatMenuOptionData> options = [];

        var maps = CS2_SimpleAdmin.Instance.Config.DefaultMaps;
        options.AddRange(maps.Select(map => new ChatMenuOptionData(map, () => ExecuteChangeMap(admin, map, false))));

        var wsMaps = CS2_SimpleAdmin.Instance.Config.WorkshopMaps;
        options.AddRange(wsMaps.Select(map => new ChatMenuOptionData($"{map.Key} (WS)", () => ExecuteChangeMap(admin, map.Value?.ToString() ?? map.Key, true))));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void ExecuteChangeMap(CCSPlayerController admin, string mapName, bool workshop)
    {
        if (workshop)
            CS2_SimpleAdmin.Instance.ChangeWorkshopMap(admin, mapName);
        else
            CS2_SimpleAdmin.Instance.ChangeMap(admin, mapName);
    }
}