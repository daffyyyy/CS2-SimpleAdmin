using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus;

public static class AdminMenu
{
    public static IMenu? CreateMenu(string title)
    {
        return Helper.CreateMenu(title);
        // return CS2_SimpleAdmin.Instance.Config.UseChatMenu ? new ChatMenu(title) : new CenterHtmlMenu(title, CS2_SimpleAdmin.Instance);
    }

    public static void OpenMenu(CCSPlayerController player, IMenu menu)
    {
        menu.Open(player);
        // switch (menu)
        // {
        // 	case CenterHtmlMenu centerHtmlMenu:
        // 		MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, player, centerHtmlMenu);
        // 		break;
        // 	case ChatMenu chatMenu:
        // 		MenuManager.OpenChatMenu(player, chatMenu);
        // 		break;
        // }
    }

    public static void OpenMenu(CCSPlayerController admin)
    {
        if (admin.IsValid == false)
            return;

        var localizer = CS2_SimpleAdmin._localizer;
        if (AdminManager.PlayerHasPermissions(admin, "@css/generic") == false)
        {
            admin.PrintToChat(localizer?["sa_prefix"] ??
                              "[SimpleAdmin] " +
                              (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command")
            );
            return;
        }

        var menu = CreateMenu(localizer?["sa_title"] ?? "SimpleAdmin");
        List<ChatMenuOptionData> options =
        [
            new ChatMenuOptionData(localizer?["sa_menu_players_manage"] ?? "Players Manage", () => ManagePlayersMenu.OpenMenu(admin)),
            new ChatMenuOptionData(localizer?["sa_menu_server_manage"] ?? "Server Manage", () => ManageServerMenu.OpenMenu(admin)),
            new ChatMenuOptionData(localizer?["sa_menu_fun_commands"] ?? "Fun Commands", () => FunActionsMenu.OpenMenu(admin)),
        ];

        var customCommands = CS2_SimpleAdmin.Instance.Config.CustomServerCommands;
        if (customCommands.Count > 0)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_custom_commands"] ?? "Custom Commands", () => CustomCommandsMenu.OpenMenu(admin)));
        }

        if (AdminManager.PlayerHasPermissions(admin, "@css/root"))
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_admins_manage"] ?? "Admins Manage", () => ManageAdminsMenu.OpenMenu(admin)));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
        }

        if (menu != null) OpenMenu(admin, menu);
    }
}