# Module Development Guide - CS2-SimpleAdmin

> **🎓 New to module development?** Start with the [Fun Commands Module](./CS2-SimpleAdmin_FunCommands/) - it's a fully documented reference implementation showing all best practices!

This guide explains how to create modules for CS2-SimpleAdmin with custom commands, menus, and translations.

## 📖 Table of Contents

1. [Quick Start](#quick-start)
2. [Learning Resources](#learning-resources)
3. [API Methods Reference](#api-methods)
4. [Menu Patterns](#menu-patterns)
5. [Best Practices](#best-practices)
6. [Common Patterns](#common-patterns)
7. [Troubleshooting](#troubleshooting)

## 🚀 Quick Start

### Step 1: Study the Example Module

The **[CS2-SimpleAdmin_FunCommands](./CS2-SimpleAdmin_FunCommands/)** module is your best learning resource. It demonstrates:

✅ Command registration from config
✅ Dynamic menu creation
✅ Per-player translation support
✅ Proper cleanup on unload
✅ Code organization with partial classes
✅ All menu patterns you'll need

**Start here:** Read [`CS2-SimpleAdmin_FunCommands/README.md`](./CS2-SimpleAdmin_FunCommands/README.md)

### Step 2: Create Your Module Structure

```
YourModule/
├── YourModule.csproj                 # Project file
├── YourModule.cs                     # Main plugin class
├── Commands.cs                       # Command handlers (optional)
├── Menus.cs                          # Menu creation (optional)
├── Config.cs                         # Configuration
└── lang/                             # Translations
    ├── en.json
    ├── pl.json
    └── ru.json
```

### Step 3: Minimal Working Example

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CS2_SimpleAdminApi;

public class MyModule : BasePlugin
{
    private ICS2_SimpleAdminApi? _api;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability = new("simpleadmin:api");

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api == null)
        {
            Logger.LogError("CS2-SimpleAdmin API not found");
            return;
        }

        // Register menus after API is ready
        _api.OnSimpleAdminReady += RegisterMenus;
        RegisterMenus(); // Fallback for hot reload
    }

    private void RegisterMenus()
    {
        if (_api == null) return;

        // 1. Register a new category
        _api.RegisterMenuCategory("mymodule", "My Module", "@css/generic");

        // 2. Register menu items in the category
        _api.RegisterMenu("mymodule", "action1", "Action 1", CreateAction1Menu, "@css/generic");
        _api.RegisterMenu("mymodule", "action2", "Action 2", CreateAction2Menu, "@css/kick");
    }

    private object CreateAction1Menu(CCSPlayerController admin)
    {
        // Create a menu with automatic back button
        var menu = _api!.CreateMenuWithBack("Action 1 Menu", "mymodule", admin);

        // Add menu options
        _api.AddMenuOption(menu, "Option 1", player =>
        {
            player.PrintToChat("You selected Option 1");
        });

        _api.AddMenuOption(menu, "Option 2", player =>
        {
            player.PrintToChat("You selected Option 2");
        });

        return menu;
    }

    private object CreateAction2Menu(CCSPlayerController admin)
    {
        // Use the built-in player selection menu
        return _api!.CreateMenuWithPlayers("Select Player", "mymodule", admin,
            player => player.IsValid && admin.CanTarget(player),
            (adminPlayer, targetPlayer) =>
            {
                adminPlayer.PrintToChat($"You selected {targetPlayer.PlayerName}");
            });
    }

    public override void Unload(bool hotReload)
    {
        if (_api == null) return;

        // Clean up registered menus
        _api.UnregisterMenu("mymodule", "action1");
        _api.UnregisterMenu("mymodule", "action2");
        _api.OnSimpleAdminReady -= RegisterMenus;
    }
}
```

## 📚 Learning Resources

### For Beginners

1. **Start Here:** [`CS2-SimpleAdmin_FunCommands/README.md`](./CS2-SimpleAdmin_FunCommands/README.md)
   - Explains every file and pattern
   - Shows code organization
   - Demonstrates all menu types

2. **Read the Code:** Study these files in order:
   - `Config.cs` - Simple configuration
   - `CS2-SimpleAdmin_FunCommands.cs` - Plugin initialization
   - `Commands.cs` - Command registration
   - `Menus.cs` - Menu creation patterns

3. **Translations:** [`TRANSLATION_EXAMPLE.md`](./TRANSLATION_EXAMPLE.md)
   - How to use module translations
   - Per-player language support
   - Best practices

### Key Concepts

| Concept | What It Does | Example File |
|---------|-------------|--------------|
| **API Capability** | Get access to SimpleAdmin API | `CS2-SimpleAdmin_FunCommands.cs:37` |
| **Command Registration** | Register console commands | `Commands.cs:15-34` |
| **Menu Registration** | Add menus to admin menu | `CS2-SimpleAdmin_FunCommands.cs:130-141` |
| **Translations** | Per-player language support | `Actions.cs:20-31` |
| **Cleanup** | Unregister on plugin unload | `CS2-SimpleAdmin_FunCommands.cs:63-97` |

## 🎯 Menu Patterns

The FunCommands module demonstrates **3 essential menu patterns** you'll use in every module:

### Pattern 1: Simple Player Selection
**When to use:** Select a player and immediately execute an action

```csharp
// Example: God Mode menu
private object CreateGodModeMenu(CCSPlayerController admin)
{
    return _api.CreateMenuWithPlayers(
        "God Mode",                         // Title
        "yourmodule",                       // Category ID
        admin,                              // Admin
        player => player.IsValid && admin.CanTarget(player),  // Filter
        (adminPlayer, target) =>            // Action
        {
            // Execute action immediately
            ToggleGodMode(target);
        });
}
```

**See:** `CS2-SimpleAdmin_FunCommands/Menus.cs:21-29`

### Pattern 2: Nested Menu (Player → Value)
**When to use:** Select a player, then select a value/option for that player

```csharp
// Example: Set HP menu (player selection)
private object CreateSetHpMenu(CCSPlayerController admin)
{
    var menu = _api.CreateMenuWithBack("Set HP", "yourmodule", admin);
    var players = _api.GetValidPlayers().Where(p => admin.CanTarget(p));

    foreach (var player in players)
    {
        // AddSubMenu automatically adds back button to submenu
        _api.AddSubMenu(menu, player.PlayerName,
            p => CreateHpValueMenu(admin, player));
    }

    return menu;
}

// Example: Set HP menu (value selection)
private object CreateHpValueMenu(CCSPlayerController admin, CCSPlayerController target)
{
    var menu = _api.CreateMenuWithBack($"HP for {target.PlayerName}", "yourmodule", admin);
    var values = new[] { 50, 100, 200, 500 };

    foreach (var hp in values)
    {
        _api.AddMenuOption(menu, $"{hp} HP", _ =>
        {
            if (target.IsValid)  // Always validate!
            {
                target.PlayerPawn.Value.Health = hp;
            }
        });
    }

    return menu;
}
```

**See:** `CS2-SimpleAdmin_FunCommands/Menus.cs:134-173`

### Pattern 3: Nested Menu with Complex Data
**When to use:** Need to display more complex options (like weapons with icons, items with descriptions)

```csharp
// Example: Give Weapon menu
private object CreateGiveWeaponMenu(CCSPlayerController admin)
{
    var menu = _api.CreateMenuWithBack("Give Weapon", "yourmodule", admin);
    var players = _api.GetValidPlayers().Where(p => admin.CanTarget(p));

    foreach (var player in players)
    {
        _api.AddSubMenu(menu, player.PlayerName,
            p => CreateWeaponSelectionMenu(admin, player));
    }

    return menu;
}

private object CreateWeaponSelectionMenu(CCSPlayerController admin, CCSPlayerController target)
{
    var menu = _api.CreateMenuWithBack($"Weapons for {target.PlayerName}", "yourmodule", admin);

    // Use cached data for performance
    foreach (var weapon in GetWeaponsCache())
    {
        _api.AddMenuOption(menu, weapon.Value.ToString(), _ =>
        {
            if (target.IsValid)
            {
                target.GiveNamedItem(weapon.Value);
            }
        });
    }

    return menu;
}
```

**See:** `CS2-SimpleAdmin_FunCommands/Menus.cs:67-109`

## 📋 API Methods Reference

### 1. Category Management

#### `RegisterMenuCategory(string categoryId, string categoryName, string permission = "@css/generic")`

Registers a new menu category that appears in the main admin menu.

**Parameters:**
- `categoryId` - Unique identifier for the category (e.g., "fun", "vip", "economy")
- `categoryName` - Display name shown in menu (e.g., "Fun Commands")
- `permission` - Required permission to see the category (default: "@css/generic")

**Example:**
```csharp
_api.RegisterMenuCategory("vip", "VIP Features", "@css/vip");
```

### 2. Menu Registration

#### `RegisterMenu(string categoryId, string menuId, string menuName, Func<CCSPlayerController, object> menuFactory, string? permission = null)`

Registers a menu item within a category.

**Parameters:**
- `categoryId` - The category to add this menu to
- `menuId` - Unique identifier for the menu
- `menuName` - Display name in the category menu
- `menuFactory` - Function that creates the menu when selected (receives admin player)
- `permission` - Optional permission required to see this menu item

**Example:**
```csharp
_api.RegisterMenu("fun", "godmode", "God Mode", CreateGodModeMenu, "@css/cheats");
```

#### `UnregisterMenu(string categoryId, string menuId)`

Removes a menu item from a category.

**Example:**
```csharp
_api.UnregisterMenu("fun", "godmode");
```

### 3. Menu Creation

#### `CreateMenuWithBack(string title, string categoryId, CCSPlayerController player)`

Creates a menu with an automatic "Back" button that returns to the category menu.

**Parameters:**
- `title` - Menu title
- `categoryId` - Category this menu belongs to (for back navigation)
- `player` - The admin player viewing the menu

**Returns:** `object` (MenuBuilder instance)

**Example:**
```csharp
var menu = _api.CreateMenuWithBack("Weapon Selection", "fun", admin);
```

#### `CreateMenuWithPlayers(string title, string categoryId, CCSPlayerController admin, Func<CCSPlayerController, bool> filter, Action<CCSPlayerController, CCSPlayerController> onSelect)`

Creates a menu with a list of players, filtered and with automatic back button.

**Parameters:**
- `title` - Menu title
- `categoryId` - Category for back navigation
- `admin` - The admin player viewing the menu
- `filter` - Function to filter which players appear in the menu
- `onSelect` - Action to execute when a player is selected (receives admin and target)

**Returns:** `object` (MenuBuilder instance)

**Example:**
```csharp
return _api.CreateMenuWithPlayers("Select Player to Kick", "admin", admin,
    player => player.IsValid && admin.CanTarget(player),
    (adminPlayer, targetPlayer) =>
    {
        // Kick the selected player
        Server.ExecuteCommand($"css_kick {targetPlayer.UserId}");
    });
```

### 4. Menu Manipulation

#### `AddMenuOption(object menu, string name, Action<CCSPlayerController> action, bool disabled = false, string? permission = null)`

Adds a clickable option to a menu.

**Parameters:**
- `menu` - The menu object (from CreateMenuWithBack)
- `name` - Display name of the option
- `action` - Action to execute when clicked (receives the player who clicked)
- `disabled` - Whether the option is disabled (grayed out)
- `permission` - Optional permission required to see this option

**Example:**
```csharp
_api.AddMenuOption(menu, "Give AK-47", player =>
{
    player.GiveNamedItem("weapon_ak47");
}, permission: "@css/cheats");
```

#### `AddSubMenu(object menu, string name, Func<CCSPlayerController, object> subMenuFactory, bool disabled = false, string? permission = null)`

Adds a submenu option that opens another menu. **Automatically adds a back button to the submenu.**

**Parameters:**
- `menu` - The parent menu
- `name` - Display name of the submenu option
- `subMenuFactory` - Function that creates the submenu (receives the player)
- `disabled` - Whether the option is disabled
- `permission` - Optional permission required

**Example:**
```csharp
_api.AddSubMenu(menu, "Weapon Category", player =>
{
    var weaponMenu = _api.CreateMenuWithBack("Weapons", "fun", player);
    _api.AddMenuOption(weaponMenu, "AK-47", p => p.GiveNamedItem("weapon_ak47"));
    _api.AddMenuOption(weaponMenu, "AWP", p => p.GiveNamedItem("weapon_awp"));
    return weaponMenu;
});
```

#### `OpenMenu(object menu, CCSPlayerController player)`

Opens a menu for a specific player.

**Example:**
```csharp
var menu = _api.CreateMenuWithBack("Custom Menu", "fun", player);
_api.AddMenuOption(menu, "Test", p => p.PrintToChat("Test!"));
_api.OpenMenu(menu, player);
```

## Advanced Examples

### Nested Menus with Player Selection

```csharp
private object CreateGiveWeaponMenu(CCSPlayerController admin)
{
    var menu = _api.CreateMenuWithBack("Give Weapon", "fun", admin);
    var players = _api.GetValidPlayers()
        .Where(p => p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(p));

    foreach (var player in players)
    {
        // Add submenu for each player - automatic back button will be added
        _api.AddSubMenu(menu, player.PlayerName, p => CreateWeaponSelectionMenu(admin, player));
    }

    return menu;
}

private object CreateWeaponSelectionMenu(CCSPlayerController admin, CCSPlayerController target)
{
    var weaponMenu = _api.CreateMenuWithBack($"Weapons for {target.PlayerName}", "fun", admin);

    var weapons = new[] { "weapon_ak47", "weapon_m4a1", "weapon_awp", "weapon_deagle" };
    foreach (var weapon in weapons)
    {
        _api.AddMenuOption(weaponMenu, weapon, _ =>
        {
            if (target.IsValid)
            {
                target.GiveNamedItem(weapon);
                admin.PrintToChat($"Gave {weapon} to {target.PlayerName}");
            }
        });
    }

    return weaponMenu;
}
```

### Dynamic Menu with Value Selection

```csharp
private object CreateSetHpMenu(CCSPlayerController admin)
{
    var menu = _api.CreateMenuWithBack("Set HP", "admin", admin);
    var players = _api.GetValidPlayers().Where(p => admin.CanTarget(p));

    foreach (var player in players)
    {
        _api.AddSubMenu(menu, player.PlayerName, p => CreateHpValueMenu(admin, player));
    }

    return menu;
}

private object CreateHpValueMenu(CCSPlayerController admin, CCSPlayerController target)
{
    var hpMenu = _api.CreateMenuWithBack($"HP for {target.PlayerName}", "admin", admin);
    var hpValues = new[] { 1, 50, 100, 200, 500, 1000 };

    foreach (var hp in hpValues)
    {
        _api.AddMenuOption(hpMenu, $"{hp} HP", _ =>
        {
            if (target.IsValid && target.PlayerPawn?.Value != null)
            {
                target.PlayerPawn.Value.Health = hp;
                admin.PrintToChat($"Set {target.PlayerName} HP to {hp}");
            }
        });
    }

    return hpMenu;
}
```

### Permission-Based Menu Options

```csharp
private object CreateAdminToolsMenu(CCSPlayerController admin)
{
    var menu = _api.CreateMenuWithBack("Admin Tools", "admin", admin);

    // Only root admins can see this
    _api.AddMenuOption(menu, "Dangerous Action", player =>
    {
        player.PrintToChat("Executing dangerous action...");
    }, permission: "@css/root");

    // All admins can see this
    _api.AddMenuOption(menu, "Safe Action", player =>
    {
        player.PrintToChat("Executing safe action...");
    }, permission: "@css/generic");

    return menu;
}
```

## Best Practices

1. **Always check for API availability**
   ```csharp
   if (_api == null) return;
   ```

2. **Validate player state before actions**
   ```csharp
   if (target.IsValid && target.PlayerPawn?.Value != null)
   {
       // Safe to perform action
   }
   ```

3. **Use descriptive category and menu IDs**
   - Good: `"economy"`, `"vip_features"`, `"fun_commands"`
   - Bad: `"cat1"`, `"menu"`, `"test"`

4. **Clean up on unload**
   ```csharp
   public override void Unload(bool hotReload)
   {
       _api?.UnregisterMenu("mymodule", "mymenu");
       _api.OnSimpleAdminReady -= RegisterMenus;
   }
   ```

5. **Use appropriate permissions**
   - `@css/generic` - All admins
   - `@css/ban` - Admins who can ban
   - `@css/kick` - Admins who can kick
   - `@css/root` - Root admins only
   - Custom permissions from your module

6. **Handle hot reload**
   ```csharp
   _api.OnSimpleAdminReady += RegisterMenus;
   RegisterMenus(); // Fallback for hot reload case
   ```

## Automatic Back Button

The menu system **automatically adds a "Back" button** to all submenus created with:
- `CreateMenuWithBack()` - Returns to the category menu
- `AddSubMenu()` - Returns to the parent menu

You don't need to manually add back buttons - the system handles navigation automatically!

## Getting Valid Players

Use the API helper method to get valid, connected players:

```csharp
var players = _api.GetValidPlayers();

// With filtering
var alivePlayers = _api.GetValidPlayers()
    .Where(p => p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE);

var targetablePlayers = _api.GetValidPlayers()
    .Where(p => admin.CanTarget(p));
```

## Complete Module Example

See the `CS2-SimpleAdmin_FunCommands` module in the `Modules/` directory for a complete, production-ready example of:
- Category registration
- Multiple menu types
- Nested menus with automatic back buttons
- Player selection menus
- Permission-based access
- Proper cleanup on unload

## Troubleshooting

**Q: My category doesn't appear in the admin menu**
- Ensure you're calling `RegisterMenuCategory()` after the API is ready
- Check that the player has the required permission
- Verify the category has at least one menu registered with `RegisterMenu()`

**Q: Back button doesn't work**
- Make sure you're using `CreateMenuWithBack()` instead of creating menus manually
- The `categoryId` parameter must match the category you registered
- Use `AddSubMenu()` for nested menus - it handles back navigation automatically

**Q: Menu appears but is empty**
- Check that you're adding options with `AddMenuOption()` or `AddSubMenu()`
- Verify permission checks aren't filtering out all options
- Ensure player validation in filters isn't too restrictive

**Q: API is null in OnAllPluginsLoaded**
- Wait for the `OnSimpleAdminReady` event instead of immediate registration
- Make sure CS2-SimpleAdmin is loaded before your module
