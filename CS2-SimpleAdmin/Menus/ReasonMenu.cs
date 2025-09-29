using CounterStrikeSharp.API.Core;
using CS2_SimpleAdmin.Models;
using CS2_SimpleAdminApi;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin.Menus;

public static class ReasonMenu
{
    public static void OpenMenu(
    CCSPlayerController admin,
    PenaltyType penaltyType,
    string menuName,
    CCSPlayerController player,
    Action<CCSPlayerController, CCSPlayerController, string> onSelectAction)
    {
        var reasons = penaltyType switch
        {
            PenaltyType.Ban => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons,
            PenaltyType.Kick => CS2_SimpleAdmin.Instance.Config.MenuConfigs.KickReasons,
            PenaltyType.Mute => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
            PenaltyType.Warn => CS2_SimpleAdmin.Instance.Config.MenuConfigs.WarnReasons,
            PenaltyType.Gag => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
            PenaltyType.Silence => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
            _ => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons
        };

        var items = new List<MenuItem>();
        for (int i = 0; i < reasons.Count; i++)
        {
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(reasons[i])]));
        }

        if (items.Count == 0) return;

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuName,
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;
                if (buttons == MenuButtons.Select && menu.Option >= 0 && menu.Option < reasons.Count)
                {
                    onSelectAction(admin, player, reasons[menu.Option]);
                }
            },
            true,
            freezePlayer: false,
            disableDeveloper: true);
    }

    public static void OpenMenu(
        CCSPlayerController admin,
        PenaltyType penaltyType,
        string menuName,
        DisconnectedPlayer player,
        Action<CCSPlayerController, DisconnectedPlayer, string> onSelectAction)
    {
        var reasons = penaltyType switch
        {
            PenaltyType.Ban => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons,
            PenaltyType.Kick => CS2_SimpleAdmin.Instance.Config.MenuConfigs.KickReasons,
            PenaltyType.Mute => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
            PenaltyType.Warn => CS2_SimpleAdmin.Instance.Config.MenuConfigs.WarnReasons,
            _ => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons
        };

        var items = new List<MenuItem>();
        for (int i = 0; i < reasons.Count; i++)
        {
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(reasons[i])]));
        }

        if (items.Count == 0) return;

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuName,
            items,
            (buttons, menu, selected) =>
            {
                if (selected == null) return;
                if (buttons == MenuButtons.Select && menu.Option >= 0 && menu.Option < reasons.Count)
                {
                    onSelectAction(admin, player, reasons[menu.Option]);
                }
            },
            true,
            freezePlayer: false,
            disableDeveloper: true);
    }
}