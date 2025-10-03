using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin.Menus;

public static class AdminMenu
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

        List<ChatMenuOptionData> options =
        [
            new(localizer?["sa_menu_players_manage"] ?? "Players Manage", () => ManagePlayersMenu.OpenMenu(admin)),
            new(localizer?["sa_menu_server_manage"] ?? "Server Manage", () => ManageServerMenu.OpenMenu(admin)),
            new(localizer?["sa_menu_fun_commands"] ?? "Fun Commands", () => FunActionsMenu.OpenMenu(admin)),
        ];

        var customCommands = CS2_SimpleAdmin.Instance.Config.CustomServerCommands;
        if (customCommands.Count > 0)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_custom_commands"] ?? "Custom Commands", () => CustomCommandsMenu.OpenMenu(admin)));
        }

        if (AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/root"))
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_admins_manage"] ?? "Admins Manage", () => ManageAdminsMenu.OpenMenu(admin)));

        List<MenuItem> items = [];
        var optionMap = new Dictionary<int, ChatMenuOptionData>();
        int i = 0;

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            if (!menuOptionData.Disabled)
            {
                items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(menuName)]));
                optionMap[i++] = menuOptionData;
            }
        }

        if(i == 0) return;

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(admin, localizer?["sa_title"] ?? "SimpleAdmin", items, (buttons, menu, selected) =>
        {
            if (selected == null) return;

            if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var menuOptionData))
            {
                menuOptionData.Action.Invoke();
            }
        }, false, freezePlayer: false, disableDeveloper: true);
    }
}