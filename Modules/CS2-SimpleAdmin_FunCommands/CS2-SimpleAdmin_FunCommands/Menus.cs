using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CS2_SimpleAdmin_FunCommands;

/// <summary>
/// Menu creation methods for Fun Commands module.
/// This file demonstrates different menu patterns using SimpleAdmin API.
///
/// PERMISSION OVERRIDE SUPPORT:
/// ============================
/// When registering menus in RegisterFunMenus(), you can pass a command name (e.g., "css_god")
/// as the last parameter. This enables automatic permission override checking via CounterStrikeSharp's
/// admin system.
///
/// How it works:
/// 1. Server admin overrides a command's permissions (e.g., css_god requires @css/vip instead of @css/cheats)
/// 2. SimpleAdmin's menu system automatically checks for overrides when displaying menus
/// 3. If override exists, it uses the overridden permission; otherwise, uses the default permission
///
/// Example from RegisterFunMenus():
///     _sharedApi.RegisterMenu("fun", "god",
///         Localizer?["fun_menu_god"] ?? "God Mode",
///         CreateGodModeMenu,
///         "@css/cheats",   // Default permission
///         "css_god");      // Command name for override checking
///
/// This means developers don't need to manually check permissions in their menu factory methods!
///
/// MENU CONTEXT API (NEW!):
/// ========================
/// Menu factory methods now receive a MenuContext parameter that contains:
/// - CategoryId: The category this menu belongs to (e.g., "fun")
/// - MenuId: The unique identifier for this menu (e.g., "god")
/// - MenuTitle: The display title from registration (e.g., "God Mode")
/// - Permission: The default permission (e.g., "@css/cheats")
/// - CommandName: The command name for override checking (e.g., "css_god")
///
/// This eliminates duplication when creating menus - you no longer need to repeat
/// the title and category in both RegisterMenu() and CreateMenuWithPlayers()!
///
/// Before (old API):
///     private object CreateGodModeMenu(CCSPlayerController admin)
///     {
///         return _sharedApi.CreateMenuWithPlayers(
///             "God Mode",  // ← Duplicated from RegisterMenu
///             "fun",       // ← Duplicated from RegisterMenu
///             admin, filter, action);
///     }
///
/// After (new API with MenuContext):
///     private object CreateGodModeMenu(CCSPlayerController admin, MenuContext context)
///     {
///         return _sharedApi.CreateMenuWithPlayers(
///             context,     // ← Contains both title and category automatically!
///             admin, filter, action);
///     }
/// </summary>
public partial class CS2_SimpleAdmin_FunCommands
{
    // =================================
    // SIMPLE PLAYER SELECTION MENUS
    // =================================
    // Pattern: Direct player selection with immediate action
    // Use CreateMenuWithPlayers when you just need to select a player and execute an action

    /// <summary>
    /// Creates a simple player selection menu for god mode.
    /// PATTERN: CreateMenuWithPlayers with method reference
    /// IMPROVED: Uses MenuContext to eliminate duplication of title and category
    /// </summary>
    private object CreateGodModeMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        return _sharedApi!.CreateMenuWithPlayers(
            context,                       // ← Context contains title & category automatically!
            admin,                         // Admin opening the menu
            player => player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(player),  // Filter: only alive, targetable players
            God);                          // Action to execute (method reference)
    }

    private object CreateNoClipMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        return _sharedApi!.CreateMenuWithPlayers(
            context,
            admin,
            player => player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(player),
            NoClip);
    }

    /// <summary>
    /// Creates a player selection menu for respawn command.
    /// PATTERN: CreateMenuWithPlayers with method reference
    /// IMPROVED: Uses MenuContext to eliminate duplication
    /// </summary>
    private object CreateRespawnMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        return _sharedApi!.CreateMenuWithPlayers(
            context,
            admin,
            admin.CanTarget,        // Filter: only targetable players (no LifeState check - can respawn dead players)
            Respawn);               // Use the Respawn method which includes death position teleport
    }

    // =================================
    // NESTED MENUS - PLAYER → VALUE SELECTION
    // =================================
    // Pattern: First select player, then select a value/option for that player
    // Use CreateMenuWithBack + AddSubMenu for multi-level menus

    /// <summary>
    /// Creates a nested menu: Player selection → Weapon selection.
    /// PATTERN: CreateMenuWithBack + foreach + AddSubMenu
    /// IMPROVED: Uses MenuContext - no more duplication of title/category!
    /// </summary>
    private object CreateGiveWeaponMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        var menu = _sharedApi!.CreateMenuWithBack(
            context,              // ← Context contains title & category!
            admin);
        var players = _sharedApi.GetValidPlayers().Where(p =>
            p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(p));

        foreach (var player in players)
        {
            var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;

            // AddSubMenu automatically adds a "Back" button to the submenu
            // The lambda receives 'p' but we use captured 'player' variable (closure)
            _sharedApi.AddSubMenu(menu, playerName, p => CreateWeaponSelectionMenu(admin, player));
        }

        return menu;
    }

    /// <summary>
    /// Creates weapon selection submenu for a specific player.
    /// PATTERN: CreateMenuWithBack + foreach + AddMenuOption
    /// </summary>
    private object CreateWeaponSelectionMenu(CCSPlayerController admin, CCSPlayerController target)
    {
        var weaponMenu = _sharedApi!.CreateMenuWithBack(
            Localizer?["fun_menu_give_player", target.PlayerName] ?? $"Give Weapon: {target.PlayerName}",
            "fun",
            admin);

        // Loop through cached weapons (performance optimization)
        foreach (var weapon in GetWeaponsCache())
        {
            // AddMenuOption for each selectable option
            // IMPORTANT: Always validate target.IsValid before executing action
            _sharedApi.AddMenuOption(weaponMenu, weapon.Value.ToString(), _ =>
            {
                if (target.IsValid)  // Player might disconnect before selection
                {
                    target.GiveNamedItem(weapon.Value);
                    LogAndShowActivity(admin, target, "fun_admin_give_message", $"css_give", weapon.Value.ToString());
                }
            });
        }

        return weaponMenu;
    }

    private object CreateStripWeaponsMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        return _sharedApi!.CreateMenuWithPlayers(
            context,
            admin,
            player => player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(player),
            (adminPlayer, targetPlayer) =>
            {
                targetPlayer.RemoveWeapons();
                LogAndShowActivity(adminPlayer, targetPlayer, "fun_admin_strip_message", "css_strip");
            });
    }

    private object CreateFreezeMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        return _sharedApi!.CreateMenuWithPlayers(
            context,
            admin,
            player => player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(player),
            (adminPlayer, targetPlayer) => { Freeze(adminPlayer, targetPlayer, -1); });
    }

    /// <summary>
    /// Creates a nested menu for setting player HP with predefined values.
    /// PATTERN: Same as Give Weapon (player selection → value selection)
    /// IMPROVED: Uses MenuContext - cleaner and less error-prone!
    /// </summary>
    private object CreateSetHpMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        var menu = _sharedApi!.CreateMenuWithBack(
            context,
            admin);
        var players = _sharedApi.GetValidPlayers().Where(p =>
            p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(p));

        foreach (var player in players)
        {
            var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
            _sharedApi.AddSubMenu(menu, playerName, p => CreateHpSelectionMenu(admin, player));
        }

        return menu;
    }

    /// <summary>
    /// Creates HP value selection submenu.
    /// TIP: Use arrays for predefined values - easy to modify and maintain
    /// </summary>
    private object CreateHpSelectionMenu(CCSPlayerController admin, CCSPlayerController target)
    {
        var hpSelectionMenu = _sharedApi!.CreateMenuWithBack(
            Localizer?["fun_menu_hp_player", target.PlayerName] ?? $"Set HP: {target.PlayerName}",
            "fun",
            admin);

        // Predefined HP values - easy to customize
        var hpValues = new[] { 1, 10, 25, 50, 100, 200, 500, 999 };

        foreach (var hp in hpValues)
        {
            _sharedApi.AddMenuOption(hpSelectionMenu,
                Localizer?["fun_menu_hp_value", hp] ?? $"{hp} HP",
                _ =>
            {
                if (target.IsValid)
                {
                    target.SetHp(hp);
                    LogAndShowActivity(admin, target, "fun_admin_hp_message", "css_hp", hp.ToString());
                }
            });
        }

        return hpSelectionMenu;
    }

    private object CreateSetSpeedMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        var menu = _sharedApi!.CreateMenuWithBack(
            context,
            admin);
        var players = _sharedApi.GetValidPlayers().Where(p =>
            p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(p));

        foreach (var player in players)
        {
            var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
            _sharedApi.AddSubMenu(menu, playerName, p => CreateSpeedSelectionMenu(admin, player));
        }

        return menu;
    }

    /// <summary>
    /// Creates speed value selection submenu.
    /// TIP: Use tuples (value, display) when you need different internal value vs display text
    /// Example: (0.5f, "0.5") - float value for code, string for display
    /// </summary>
    private object CreateSpeedSelectionMenu(CCSPlayerController admin, CCSPlayerController target)
    {
        var speedSelectionMenu = _sharedApi!.CreateMenuWithBack(
            Localizer?["fun_menu_speed_player", target.PlayerName] ?? $"Set Speed: {target.PlayerName}",
            "fun",
            admin);

        // Tuple pattern: (actualValue, displayText)
        // Useful when display text differs from actual value
        var speedValues = new[]
        {
            (0.1f, "0.1"), (0.25f, "0.25"), (0.5f, "0.5"), (0.75f, "0.75"), (1f, "1"), (2f, "2"), (3f, "3"), (4f, "4")
        };

        foreach (var (speed, display) in speedValues)
        {
            _sharedApi.AddMenuOption(speedSelectionMenu,
                Localizer?["fun_menu_speed_value", display] ?? $"Speed {display}",
                _ =>
            {
                if (target.IsValid)
                {
                    target.SetSpeed(speed);

                    // Track speed modification for timer
                    if (speed == 1f)
                        SpeedPlayers.Remove(target);
                    else
                        SpeedPlayers[target] = speed;

                    LogAndShowActivity(admin, target, "fun_admin_speed_message", "css_speed", speed.ToString(CultureInfo.InvariantCulture));
                }
            });
        }

        return speedSelectionMenu;
    }

    private object CreateSetGravityMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        var menu = _sharedApi!.CreateMenuWithBack(
            context,
            admin);
        var players = _sharedApi.GetValidPlayers().Where(p =>
            p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(p));

        foreach (var player in players)
        {
            var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
            _sharedApi.AddSubMenu(menu, playerName, p => CreateGravitySelectionMenu(admin, player));
        }

        return menu;
    }

    private object CreateGravitySelectionMenu(CCSPlayerController admin, CCSPlayerController target)
    {
        var gravitySelectionMenu = _sharedApi!.CreateMenuWithBack(
            Localizer?["fun_menu_gravity_player", target.PlayerName] ?? $"Set Gravity: {target.PlayerName}",
            "fun",
            admin);
        var gravityValues = new[]
            { (0.1f, "0.1"), (0.25f, "0.25"), (0.5f, "0.5"), (0.75f, "0.75"), (1f, "1"), (2f, "2") };

        foreach (var (gravity, display) in gravityValues)
        {
            _sharedApi.AddMenuOption(gravitySelectionMenu,
                Localizer?["fun_menu_gravity_value", display] ?? $"Gravity {display}",
                _ =>
            {
                if (target.IsValid)
                {
                    target.SetGravity(Convert.ToSingle(gravity, CultureInfo.InvariantCulture));

                    // Track gravity modification for timer
                    if (gravity == 1f)
                        GravityPlayers.Remove(target);
                    else
                        GravityPlayers[target] = gravity;

                    LogAndShowActivity(admin, target, "fun_admin_gravity_message", "css_gravity", gravity.ToString(CultureInfo.InvariantCulture));
                }
            });
        }

        return gravitySelectionMenu;
    }

    private object CreateSetMoneyMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        var menu = _sharedApi!.CreateMenuWithBack(
            context,
            admin);
        var players = _sharedApi.GetValidPlayers().Where(p => admin.CanTarget(p));

        foreach (var player in players)
        {
            var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
            _sharedApi.AddSubMenu(menu, playerName, p => CreateMoneySelectionMenu(admin, player));
        }

        return menu;
    }

    private object CreateMoneySelectionMenu(CCSPlayerController admin, CCSPlayerController target)
    {
        var moneySelectionMenu = _sharedApi!.CreateMenuWithBack(
            Localizer?["fun_menu_money_player", target.PlayerName] ?? $"Set Money: {target.PlayerName}",
            "fun",
            admin);
        var moneyValues = new[] { 0, 1000, 2500, 5000, 10000, 16000 };

        foreach (var money in moneyValues)
        {
            _sharedApi.AddMenuOption(moneySelectionMenu,
                Localizer?["fun_menu_money_value", money] ?? $"${money}",
                _ =>
            {
                if (target.IsValid)
                {
                    target.SetMoney(money);
                    LogAndShowActivity(admin, target, "fun_admin_money_message", "css_money", money.ToString());
                }
            });
        }

        return moneySelectionMenu;
    }

    private object CreateSetResizeMenu(CCSPlayerController admin, CS2_SimpleAdminApi.MenuContext context)
    {
        var menu = _sharedApi!.CreateMenuWithBack(
            context,
            admin);
        var players = _sharedApi.GetValidPlayers().Where(p =>
            p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(p));

        foreach (var player in players)
        {
            var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
            _sharedApi.AddSubMenu(menu, playerName, p => CreateResizeSelectionMenu(admin, player));
        }

        return menu;
    }

    private object CreateResizeSelectionMenu(CCSPlayerController admin, CCSPlayerController target)
    {
        var resizeSelectionMenu = _sharedApi!.CreateMenuWithBack(
            Localizer?["fun_menu_resize_player", target.PlayerName] ?? $"Resize: {target.PlayerName}",
            "fun",
            admin);
        var resizeValues = new[]
            { (0.5f, "0.5"), (0.75f, "0.75"), (1f, "1"), (1.25f, "1.25"), (1.5f, "1.5"), (2f, "2"), (3f, "3") };

        foreach (var (resize, display) in resizeValues)
        {
            _sharedApi.AddMenuOption(resizeSelectionMenu,
                Localizer?["fun_menu_resize_value", display] ?? $"Size {display}",
                _ =>
            {
                if (target.IsValid)
                {
                    Resize(admin, target, resize);
                }
            });
        }

        return resizeSelectionMenu;
    }
}