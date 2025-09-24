using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin.Menus;

public static class ManageAdminsMenu
{
    public static void OpenMenu(CCSPlayerController admin)
    {
        if (!admin.IsValid)
            return;

        var localizer = CS2_SimpleAdmin._localizer;

        if (!AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/root"))
        {
            admin.PrintToChat(localizer?["sa_prefix"] ??
                            "[SimpleAdmin] " +
                            (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command"));
            return;
        }

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        void AddOption(string name, Action action, bool disabled = false)
        {
            if (disabled) return; // skip disabled options
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(name)]));
            optionMap[i++] = action;
        }

        AddOption(localizer?["sa_admin_add"] ?? "Add Admin",
            () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_admin_add"] ?? "Add Admin", AddAdminMenu));

        AddOption(localizer?["sa_admin_remove"] ?? "Remove Admin",
            () => PlayersMenu.OpenAdminPlayersMenu(admin, localizer?["sa_admin_remove"] ?? "Remove Admin",
                RemoveAdmin, player => player != admin && admin.CanTarget(player)));

        AddOption(localizer?["sa_admin_reload"] ?? "Reload Admins", () => ReloadAdmins(admin));

        if (i == 0) return; // nothing to show

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            localizer?["sa_menu_admins_manage"] ?? "Admins Manage",
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;
                if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                    action.Invoke();
            },
            true, freezePlayer: false, disableDeveloper: true);
    }

    private static void AddAdminMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_admin_add"] ?? "Add Admin"}: {player.PlayerName}";

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var adminFlag in CS2_SimpleAdmin.Instance.Config.MenuConfigs.AdminFlags)
        {
            bool disabled = AdminManager.PlayerHasPermissions(player, adminFlag.Flag);
            if (disabled) continue; // skip disabled flags

            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(adminFlag.Name)]));
            int index = i++;
            optionMap[index] = () => AddAdmin(admin, player, adminFlag.Flag);
        }

        if (i == 0) return;

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


    private static void AddAdmin(CCSPlayerController admin, CCSPlayerController player, string flag)
    {
        // TODO: Change default immunity?
        CS2_SimpleAdmin.AddAdmin(admin, player.SteamID.ToString(), player.PlayerName, flag, 10);
    }

    private static void RemoveAdmin(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.Instance.RemoveAdmin(admin, player.SteamID.ToString());
    }

    private static void ReloadAdmins(CCSPlayerController admin)
    {
        CS2_SimpleAdmin.Instance.ReloadAdmins(admin);
    }
}