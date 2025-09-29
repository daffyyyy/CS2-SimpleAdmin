using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin.Menus;

public static class ManageServerMenu
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

        string menuTitle = localizer?["sa_menu_server_manage"] ?? "Server Manage";
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
        var hasMap = AdminManager.CommandIsOverriden("css_map")
            ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_map"))
            : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/changemap");

        var hasPlugins = AdminManager.CommandIsOverriden("css_pluginsmanager")
            ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_pluginsmanager"))
            : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/root");

        // Menu options
        if (hasPlugins)
            AddOption(localizer?["sa_menu_pluginsmanager_title"] ?? "Manage Plugins", () => admin.ExecuteClientCommandFromServer("css_pluginsmanager"));

        if (hasMap)
            AddOption(localizer?["sa_changemap"] ?? "Change Map", () => ChangeMapMenu(admin));

        AddOption(localizer?["sa_restart_game"] ?? "Restart Game", () => CS2_SimpleAdmin.RestartGame(admin));

        if (items.Count == 0) return; // nothing to show

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

    private static void ChangeMapMenu(CCSPlayerController admin)
    {
        string menuTitle = CS2_SimpleAdmin._localizer?["sa_changemap"] ?? "Change Map";
        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        void AddOption(string name, Action action, bool disabled = false)
        {
            if (disabled) return;
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(name)]));
            optionMap[i++] = action;
        }

        var maps = CS2_SimpleAdmin.Instance.Config.DefaultMaps;
        foreach (var map in maps)
            AddOption(map, () => ExecuteChangeMap(admin, map, false));

        var wsMaps = CS2_SimpleAdmin.Instance.Config.WorkshopMaps;
        foreach (var map in wsMaps)
            AddOption($"{map.Key} (WS)", () => ExecuteChangeMap(admin, map.Value?.ToString() ?? map.Key, true));

        if (items.Count == 0) return;

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

    private static void ExecuteChangeMap(CCSPlayerController admin, string mapName, bool workshop)
    {
        if (workshop)
            CS2_SimpleAdmin.Instance.ChangeWorkshopMap(admin, mapName);
        else
            CS2_SimpleAdmin.Instance.ChangeMap(admin, mapName);
    }
}