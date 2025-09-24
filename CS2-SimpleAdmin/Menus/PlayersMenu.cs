using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Menu;
using Menu.Enums;
using System.Web;

namespace CS2_SimpleAdmin.Menus;

public static class PlayersMenu
{
    public static void OpenRealPlayersMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
    {
        OpenMenu(admin, menuName, onSelectAction, p => p.IsBot == false);
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

    public static void OpenMenu(
    CCSPlayerController admin,
    string menuName,
    Action<CCSPlayerController, CCSPlayerController> onSelectAction,
    Func<CCSPlayerController, bool>? enableFilter = null)
    {
        var players = Helper.GetValidPlayersWithBots();
        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, CCSPlayerController>();
        int i = 0;

        foreach (var player in players)
        {
            if (player == null) continue;

            var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
            var optionName = HttpUtility.HtmlEncode(playerName);

            if (enableFilter != null && !enableFilter(player)) continue;

            bool enabled = admin.CanTarget(player);
            if (!enabled) continue; // skip disabled options

            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(optionName)]));
            optionMap[i++] = player;
        }

        if (items.Count == 0) return;

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuName,
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;
                if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var player))
                    onSelectAction(admin, player);
            },
            true,
            freezePlayer: false,
            disableDeveloper: true);
    }
}