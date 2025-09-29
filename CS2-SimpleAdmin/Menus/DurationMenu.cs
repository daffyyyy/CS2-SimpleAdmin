using CounterStrikeSharp.API.Core;
using CS2_SimpleAdmin.Models;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin.Menus;

public static class DurationMenu
{
    public static void OpenMenu(CCSPlayerController admin, string menuName, CCSPlayerController player, Action<CCSPlayerController, CCSPlayerController, int> onSelectAction)
    {
        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var durationItem in CS2_SimpleAdmin.Instance.Config.MenuConfigs.Durations)
        {
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(durationItem.Name)]));

            optionMap[i++] = () =>
            {
                onSelectAction(admin, player, durationItem.Duration);
            };
        }

        if (i == 0) return;

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuName,
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;

                if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                {
                    action.Invoke();
                }
            },
            true, freezePlayer: false, disableDeveloper: true);
    }

    public static void OpenMenu(CCSPlayerController admin, string menuName, DisconnectedPlayer player, Action<CCSPlayerController, DisconnectedPlayer, int> onSelectAction)
    {
        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var durationItem in CS2_SimpleAdmin.Instance.Config.MenuConfigs.Durations)
        {
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(durationItem.Name)]));

            optionMap[i++] = () =>
            {
                onSelectAction(admin, player, durationItem.Duration);
            };
        }

        if (i == 0) return;

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuName,
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;

                if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                {
                    action.Invoke();
                }
            },
            true, freezePlayer: false, disableDeveloper: true);
    }
}