using CounterStrikeSharp.API.Core;
using CS2_SimpleAdmin.Models;

namespace CS2_SimpleAdmin.Menus;

public static class DurationMenu
{
    public static void OpenMenu(CCSPlayerController admin, string menuName, CCSPlayerController player, Action<CCSPlayerController, CCSPlayerController, int> onSelectAction)
    {
        var menu = AdminMenu.CreateMenu(menuName);
        if (menu == null)
            return;

        var durations = CS2_SimpleAdmin.Instance.Config.MenuConfigs.Durations;

        // Capture admin and player to avoid closure issues
        var capturedAdmin = admin;
        var capturedPlayer = player;
        var capturedAction = onSelectAction;

        foreach (var durationItem in durations)
        {
            var duration = durationItem.Duration; // Capture in local variable
            var name = durationItem.Name;

            menu.AddMenuOption(name, (controller, option) =>
            {
                capturedAction(capturedAdmin, capturedPlayer, duration);
            });
        }

        AdminMenu.OpenMenu(admin, menu);
    }

    public static void OpenMenu(CCSPlayerController admin, string menuName, DisconnectedPlayer player, Action<CCSPlayerController, DisconnectedPlayer, int> onSelectAction)
    {
        var menu = AdminMenu.CreateMenu(menuName);
        foreach (var durationItem in CS2_SimpleAdmin.Instance.Config.MenuConfigs.Durations)
        {
            menu?.AddMenuOption(durationItem.Name, (_, _) => { onSelectAction(admin, player, durationItem.Duration); });
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

}