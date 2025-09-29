using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin.Menus;

public static class CustomCommandsMenu
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

        var customCommands = CS2_SimpleAdmin.Instance.Config.CustomServerCommands
            .Where(c => !string.IsNullOrEmpty(c.DisplayName) && !string.IsNullOrEmpty(c.Command))
            .Where(c => AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), c.Flag))
            .ToList();

        if (customCommands.Count == 0) return;

        var items = new List<MenuItem>();
        for (int i = 0; i < customCommands.Count; i++)
        {
            var cmd = customCommands[i];
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(cmd.DisplayName)]));
        }

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            localizer?["sa_menu_custom_commands"] ?? "Custom Commands",
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;
                if (buttons == MenuButtons.Select && menu.Option >= 0 && menu.Option < customCommands.Count)
                {
                    var cmd = customCommands[menu.Option];
                    Helper.TryLogCommandOnDiscord(admin, cmd.Command);

                    if (cmd.ExecuteOnClient)
                        admin.ExecuteClientCommandFromServer(cmd.Command);
                    else
                        Server.ExecuteCommand(cmd.Command);
                }
            },
            true,
            freezePlayer: false,
            disableDeveloper: true);
    }
}