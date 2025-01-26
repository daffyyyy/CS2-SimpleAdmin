using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdmin.Menus;

public static class CustomCommandsMenu
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

        var menu = AdminMenu.CreateMenu(localizer?["sa_menu_custom_commands"] ?? "Custom Commands");
        List<ChatMenuOptionData> options = [];

        var customCommands = CS2_SimpleAdmin.Instance.Config.CustomServerCommands;
        options.AddRange(from customCommand in customCommands
                         where !string.IsNullOrEmpty(customCommand.DisplayName) && !string.IsNullOrEmpty(customCommand.Command)
                         let hasRights = AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), customCommand.Flag)
                         where hasRights
                         select new ChatMenuOptionData(customCommand.DisplayName, () =>
                         {
                             Helper.TryLogCommandOnDiscord(admin, customCommand.Command);

                             if (customCommand.ExecuteOnClient)
                                 admin.ExecuteClientCommandFromServer(customCommand.Command);
                             else
                                 Server.ExecuteCommand(customCommand.Command);
                         }));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddMenuOption(menuName, (_, _) => { menuOptionData.Action(); }, menuOptionData.Disabled);
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }
}