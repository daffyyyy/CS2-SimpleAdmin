using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using System.Web;

namespace CS2_SimpleAdmin.Menus;

public static class PlayersMenu
{
    public static void OpenRealPlayersMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
    {
        OpenMenu(admin, menuName, onSelectAction, p => !p.IsBot);
    }

    public static void OpenAdminPlayersMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController?, bool>? enableFilter = null)
    {
        OpenMenu(admin, menuName, onSelectAction, p => AdminManager.GetPlayerAdminData(p)?.Flags.Count > 0);
    }

    public static void OpenAliveMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
    {
        OpenMenu(admin, menuName, onSelectAction, p => p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE);
    }

    public static void OpenDeadMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController?, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
    {
        OpenMenu(admin, menuName, onSelectAction, p => p.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE);
    }

    public static void OpenMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
    {
        var menu = AdminMenu.CreateMenu(menuName);

        var players = Helper.GetValidPlayersWithBots();

        foreach (var player in players)
        {
            var playerName = player != null && player.PlayerName.Length > 26 ? player.PlayerName[..26] : player?.PlayerName;

            var optionName = HttpUtility.HtmlEncode(playerName);
            if (player != null && enableFilter != null && !enableFilter(player))
                continue;

            var enabled = admin.CanTarget(player);
            var capturedPlayer = player; // Capture in local variable to avoid closure issues

            if (optionName != null)
                menu?.AddMenuOption(optionName, (controller, option) =>
                    {
                        if (capturedPlayer != null) onSelectAction.Invoke(admin, capturedPlayer);
                    },
                    !enabled);
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }
}