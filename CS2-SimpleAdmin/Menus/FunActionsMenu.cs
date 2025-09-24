using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin.Menus;

public static class FunActionsMenu
{
    private static Dictionary<int, CsItem>? _weaponsCache;

    private static Dictionary<int, CsItem> GetWeaponsCache
    {
        get
        {
            if (_weaponsCache != null) return _weaponsCache;

            var weaponsArray = Enum.GetValues(typeof(CsItem));

            // avoid duplicates in the menu
            _weaponsCache = new Dictionary<int, CsItem>();
            foreach (CsItem item in weaponsArray)
            {
                if (item == CsItem.Tablet)
                    continue;

                _weaponsCache[(int)item] = item;
            }

            return _weaponsCache;
        }
    }

    public static void OpenMenu(CCSPlayerController admin)
    {
        if (!admin.IsValid)
            return;

        var localizer = CS2_SimpleAdmin._localizer;

        if (!AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/generic"))
        {
            admin.PrintToChat(localizer?["sa_prefix"] ??
                            "[SimpleAdmin] " +
                            (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command"));
            return;
        }

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        void TryAddOption(string command, string defaultPermission, string localizerKey, Action action)
        {
            string[] permission = AdminManager.CommandIsOverriden(command)
                ? AdminManager.GetPermissionOverrides(command)
                : [defaultPermission];

            if (!AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), permission))
                return;

            string menuName = localizer?[localizerKey] ?? localizerKey;
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(menuName)]));
            optionMap[i++] = action;
        }

        // Add options
        TryAddOption("css_god", "@css/cheats", "sa_godmode",
            () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_godmode"] ?? "God Mode", GodMode));
        TryAddOption("css_noclip", "@css/cheats", "sa_noclip",
            () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_noclip"] ?? "No Clip", NoClip));
        TryAddOption("css_respawn", "@css/cheats", "sa_respawn",
            () => PlayersMenu.OpenDeadMenu(admin, localizer?["sa_respawn"] ?? "Respawn", Respawn));
        TryAddOption("css_give", "@css/cheats", "sa_give_weapon",
            () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_give_weapon"] ?? "Give Weapon", GiveWeaponMenu));
        TryAddOption("css_strip", "@css/slay", "sa_strip_weapons",
            () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_strip_weapons"] ?? "Strip Weapons", StripWeapons));
        TryAddOption("css_freeze", "@css/slay", "sa_freeze",
            () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_freeze"] ?? "Freeze", Freeze));
        TryAddOption("css_hp", "@css/slay", "sa_set_hp",
            () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_hp"] ?? "Set Hp", SetHpMenu));
        TryAddOption("css_speed", "@css/slay", "sa_set_speed",
            () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_speed"] ?? "Set Speed", SetSpeedMenu));
        TryAddOption("css_gravity", "@css/slay", "sa_set_gravity",
            () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_gravity"] ?? "Set Gravity", SetGravityMenu));
        TryAddOption("css_money", "@css/slay", "sa_set_money",
            () => PlayersMenu.OpenMenu(admin, localizer?["sa_set_money"] ?? "Set Money", SetMoneyMenu));

        if (i == 0) return; // nothing to show

        // Show menu using new ShowScrollableMenu method
        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            localizer?["sa_menu_fun_commands"] ?? "Fun Commands",
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

    private static void GodMode(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.God(admin, player);
    }

    private static void NoClip(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.NoClip(admin, player);
    }

    private static void Respawn(CCSPlayerController? admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.Respawn(admin, player);
    }

    private static void GiveWeaponMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_give_weapon"] ?? "Give Weapon"}: {player.PlayerName}";

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var weapon in GetWeaponsCache)
        {
            string weaponName = weapon.Value.ToString();
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(weaponName)]));

            optionMap[i++] = () =>
            {
                GiveWeapon(admin, player, weapon.Value);
            };
        }

        if (i == 0) return; // nothing to show

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuTitle,
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

    private static void GiveWeapon(CCSPlayerController admin, CCSPlayerController player, CsItem weaponValue)
    {
        CS2_SimpleAdmin.GiveWeapon(admin, player, weaponValue);
    }

    private static void StripWeapons(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.StripWeapons(admin, player);
    }

    private static void Freeze(CCSPlayerController admin, CCSPlayerController player)
    {
        if (!(player.PlayerPawn.Value?.IsValid ?? false))
            return;

        if (player.PlayerPawn.Value.MoveType != MoveType_t.MOVETYPE_INVALID)
            CS2_SimpleAdmin.Freeze(admin, player, -1);
        else
            CS2_SimpleAdmin.Unfreeze(admin, player);
    }

    private static void SetHpMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var hpArray = new[]
        {
            new Tuple<string, int>("1", 1),
            new Tuple<string, int>("10", 10),
            new Tuple<string, int>("25", 25),
            new Tuple<string, int>("50", 50),
            new Tuple<string, int>("100", 100),
            new Tuple<string, int>("200", 200),
            new Tuple<string, int>("500", 500),
            new Tuple<string, int>("999", 999)
        };

        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_set_hp"] ?? "Set Hp"}: {player.PlayerName}";

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var (optionName, value) in hpArray)
        {
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(optionName)]));

            optionMap[i++] = () =>
            {
                SetHp(admin, player, value);
            };
        }

        if (i == 0) return; // nothing to show

        CS2_SimpleAdmin.Menu?.ShowScrollableMenu(
            admin,
            menuTitle,
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

    private static void SetHp(CCSPlayerController admin, CCSPlayerController player, int hp)
    {
        CS2_SimpleAdmin.SetHp(admin, player, hp);
    }

    private static void SetSpeedMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var speedArray = new[]
        {
            new Tuple<string, float>("0.1", .1f),
            new Tuple<string, float>("0.25", .25f),
            new Tuple<string, float>("0.5", .5f),
            new Tuple<string, float>("0.75", .75f),
            new Tuple<string, float>("1", 1),
            new Tuple<string, float>("2", 2),
            new Tuple<string, float>("3", 3),
            new Tuple<string, float>("4", 4)
        };

        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_set_speed"] ?? "Set Speed"}: {player.PlayerName}";

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var (optionName, value) in speedArray)
        {
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(optionName)]));
            optionMap[i++] = () => SetSpeed(admin, player, value);
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

    private static void SetSpeed(CCSPlayerController admin, CCSPlayerController player, float speed)
    {
        CS2_SimpleAdmin.SetSpeed(admin, player, speed);
    }

    private static void SetGravityMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var gravityArray = new[]
        {
            new Tuple<string, float>("0.1", .1f),
            new Tuple<string, float>("0.25", .25f),
            new Tuple<string, float>("0.5", .5f),
            new Tuple<string, float>("0.75", .75f),
            new Tuple<string, float>("1", 1),
            new Tuple<string, float>("2", 2)
        };

        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_set_gravity"] ?? "Set Gravity"}: {player.PlayerName}";

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var (optionName, value) in gravityArray)
        {
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(optionName)]));
            optionMap[i++] = () => SetGravity(admin, player, value);
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

    private static void SetGravity(CCSPlayerController admin, CCSPlayerController player, float gravity)
    {
        CS2_SimpleAdmin.SetGravity(admin, player, gravity);
    }

    private static void SetMoneyMenu(CCSPlayerController admin, CCSPlayerController player)
    {
        var moneyArray = new[]
        {
            new Tuple<string, int>("$0", 0),
            new Tuple<string, int>("$1000", 1000),
            new Tuple<string, int>("$2500", 2500),
            new Tuple<string, int>("$5000", 5000),
            new Tuple<string, int>("$10000", 10000),
            new Tuple<string, int>("$16000", 16000)
        };

        var menuTitle = $"{CS2_SimpleAdmin._localizer?["sa_set_money"] ?? "Set Money"}: {player.PlayerName}";

        List<MenuItem> items = new();
        var optionMap = new Dictionary<int, Action>();
        int i = 0;

        foreach (var (optionName, value) in moneyArray)
        {
            items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(optionName)]));
            optionMap[i++] = () => SetMoney(admin, player, value);
        }

        if (i == 0) return; // nothing to show

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

    private static void SetMoney(CCSPlayerController admin, CCSPlayerController player, int money)
    {
        CS2_SimpleAdmin.SetMoney(admin, player, money);
    }
}