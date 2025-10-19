---
sidebar_position: 3
---

# Menus API

Complete reference for creating interactive admin menus.

## Menu Categories

### RegisterMenuCategory

Register a new menu category in the admin menu.

```csharp
void RegisterMenuCategory(string categoryId, string categoryName, string permission = "@css/generic")
```

**Parameters:**
- `categoryId` - Unique identifier for the category
- `categoryName` - Display name shown in menu
- `permission` - Required permission to see category (default: "@css/generic")

**Example:**
```csharp
_api.RegisterMenuCategory("mycategory", "My Custom Category", "@css/generic");
```

**Best Practice:**
Register categories in the `OnSimpleAdminReady` event handler:

```csharp
_api.OnSimpleAdminReady += () =>
{
    _api.RegisterMenuCategory("mycategory", Localizer?["category_name"] ?? "My Category");
};
```

---

## Menu Registration

### RegisterMenu (Basic)

Register a menu within a category.

```csharp
void RegisterMenu(
    string categoryId,
    string menuId,
    string menuName,
    Func<CCSPlayerController, object> menuFactory,
    string? permission = null,
    string? commandName = null
)
```

**Parameters:**
- `categoryId` - Category to add menu to
- `menuId` - Unique menu identifier
- `menuName` - Display name in menu
- `menuFactory` - Function that creates the menu
- `permission` - Required permission (optional)
- `commandName` - Command for permission override (optional, e.g., "css_god")

**Example:**
```csharp
_api.RegisterMenu(
    "mycategory",
    "mymenu",
    "My Menu",
    CreateMyMenu,
    "@css/generic"
);

private object CreateMyMenu(CCSPlayerController player)
{
    var menu = _api!.CreateMenuWithBack("My Menu", "mycategory", player);
    // Add options...
    return menu;
}
```

---

### RegisterMenu (with MenuContext) ⭐ RECOMMENDED

Register a menu with automatic context passing - **eliminates duplication!**

```csharp
void RegisterMenu(
    string categoryId,
    string menuId,
    string menuName,
    Func<CCSPlayerController, MenuContext, object> menuFactory,
    string? permission = null,
    string? commandName = null
)
```

**Parameters:**
- `categoryId` - Category to add menu to
- `menuId` - Unique menu identifier
- `menuName` - Display name in menu
- `menuFactory` - Function that receives player AND context
- `permission` - Required permission (optional)
- `commandName` - Command for permission override (optional)

**Example:**
```csharp
// ✅ NEW WAY - No duplication!
_api.RegisterMenu(
    "fun",
    "god",
    "God Mode",
    CreateGodMenu,
    "@css/cheats",
    "css_god"
);

private object CreateGodMenu(CCSPlayerController admin, MenuContext context)
{
    // context contains: CategoryId, MenuId, MenuTitle, Permission, CommandName
    return _api!.CreateMenuWithPlayers(
        context,  // ← Automatically uses "God Mode" and "fun"
        admin,
        player => player.IsValid && admin.CanTarget(player),
        (admin, target) => ToggleGod(admin, target)
    );
}

// ❌ OLD WAY - Had to repeat "God Mode" and "fun"
private object CreateGodMenuOld(CCSPlayerController admin)
{
    return _api!.CreateMenuWithPlayers(
        "God Mode",  // ← Repeated from RegisterMenu
        "fun",       // ← Repeated from RegisterMenu
        admin,
        filter,
        action
    );
}
```

**MenuContext Properties:**
```csharp
public class MenuContext
{
    public string CategoryId { get; }      // e.g., "fun"
    public string MenuId { get; }          // e.g., "god"
    public string MenuTitle { get; }       // e.g., "God Mode"
    public string? Permission { get; }     // e.g., "@css/cheats"
    public string? CommandName { get; }    // e.g., "css_god"
}
```

---

### UnregisterMenu

Remove a menu from a category.

```csharp
void UnregisterMenu(string categoryId, string menuId)
```

**Example:**
```csharp
public override void Unload(bool hotReload)
{
    _api?.UnregisterMenu("mycategory", "mymenu");
}
```

---

## Menu Creation

### CreateMenuWithBack

Create a menu with automatic back button.

```csharp
object CreateMenuWithBack(string title, string categoryId, CCSPlayerController player)
```

**Parameters:**
- `title` - Menu title
- `categoryId` - Category for back button navigation
- `player` - Player viewing the menu

**Example:**
```csharp
var menu = _api!.CreateMenuWithBack("My Menu", "mycategory", admin);
_api.AddMenuOption(menu, "Option 1", _ => DoAction1());
_api.AddMenuOption(menu, "Option 2", _ => DoAction2());
return menu;
```

---

### CreateMenuWithBack (with Context) ⭐ RECOMMENDED

Create a menu using context - **no duplication!**

```csharp
object CreateMenuWithBack(MenuContext context, CCSPlayerController player)
```

**Example:**
```csharp
private object CreateMenu(CCSPlayerController admin, MenuContext context)
{
    // ✅ Uses context.MenuTitle and context.CategoryId automatically
    var menu = _api!.CreateMenuWithBack(context, admin);

    _api.AddMenuOption(menu, "Option 1", _ => DoAction1());
    return menu;
}
```

---

### CreateMenuWithPlayers

Create a menu showing a list of players.

```csharp
object CreateMenuWithPlayers(
    string title,
    string categoryId,
    CCSPlayerController admin,
    Func<CCSPlayerController, bool> filter,
    Action<CCSPlayerController, CCSPlayerController> onSelect
)
```

**Parameters:**
- `title` - Menu title
- `categoryId` - Category for back button
- `admin` - Admin viewing menu
- `filter` - Function to filter which players to show
- `onSelect` - Action when player is selected (admin, selectedPlayer)

**Example:**
```csharp
return _api!.CreateMenuWithPlayers(
    "Select Player",
    "mycategory",
    admin,
    player => player.IsValid && admin.CanTarget(player),
    (admin, target) =>
    {
        // Do something with selected player
        DoAction(admin, target);
    }
);
```

---

### CreateMenuWithPlayers (with Context) ⭐ RECOMMENDED

```csharp
object CreateMenuWithPlayers(
    MenuContext context,
    CCSPlayerController admin,
    Func<CCSPlayerController, bool> filter,
    Action<CCSPlayerController, CCSPlayerController> onSelect
)
```

**Example:**
```csharp
private object CreatePlayerMenu(CCSPlayerController admin, MenuContext context)
{
    return _api!.CreateMenuWithPlayers(
        context,  // ← Automatically uses correct title and category!
        admin,
        player => player.PawnIsAlive && admin.CanTarget(player),
        (admin, target) => PerformAction(admin, target)
    );
}
```

---

## Menu Options

### AddMenuOption

Add a clickable option to a menu.

```csharp
void AddMenuOption(
    object menu,
    string name,
    Action<CCSPlayerController> action,
    bool disabled = false,
    string? permission = null
)
```

**Parameters:**
- `menu` - Menu to add option to
- `name` - Option display text
- `action` - Function called when selected
- `disabled` - Whether option is disabled (default: false)
- `permission` - Required permission to see option (optional)

**Example:**
```csharp
var menu = _api!.CreateMenuWithBack("Actions", "mycategory", admin);

_api.AddMenuOption(menu, "Heal Player", _ =>
{
    target.SetHp(100);
});

_api.AddMenuOption(menu, "Admin Only Option", _ =>
{
    // Admin action
}, false, "@css/root");

return menu;
```

---

### AddSubMenu

Add a submenu option that opens another menu.

```csharp
void AddSubMenu(
    object menu,
    string name,
    Func<CCSPlayerController, object> subMenuFactory,
    bool disabled = false,
    string? permission = null
)
```

**Parameters:**
- `menu` - Parent menu
- `name` - Submenu option display text
- `subMenuFactory` - Function that creates the submenu
- `disabled` - Whether option is disabled (default: false)
- `permission` - Required permission (optional)

**Example:**
```csharp
var menu = _api!.CreateMenuWithBack("Main Menu", "mycategory", admin);

_api.AddSubMenu(menu, "Player Actions", admin =>
{
    return CreatePlayerActionsMenu(admin);
});

_api.AddSubMenu(menu, "Server Settings", admin =>
{
    return CreateServerSettingsMenu(admin);
}, false, "@css/root");

return menu;
```

---

## Opening Menus

### OpenMenu

Display a menu to a player.

```csharp
void OpenMenu(object menu, CCSPlayerController player)
```

**Example:**
```csharp
var menu = CreateMyMenu(player);
_api!.OpenMenu(menu, player);
```

**Note:** Usually menus open automatically when selected, but this can be used for direct opening.

---

## Complete Examples

### Simple Player Selection Menu

```csharp
private void RegisterMenus()
{
    _api!.RegisterMenuCategory("actions", "Player Actions", "@css/generic");

    _api.RegisterMenu(
        "actions",
        "slay",
        "Slay Player",
        CreateSlayMenu,
        "@css/slay"
    );
}

private object CreateSlayMenu(CCSPlayerController admin, MenuContext context)
{
    return _api!.CreateMenuWithPlayers(
        context,
        admin,
        player => player.PawnIsAlive && admin.CanTarget(player),
        (admin, target) =>
        {
            target.PlayerPawn?.Value?.CommitSuicide(false, true);
            admin.PrintToChat($"Slayed {target.PlayerName}");
        }
    );
}
```

---

### Nested Menu with Value Selection

```csharp
private object CreateSetHpMenu(CCSPlayerController admin, MenuContext context)
{
    var menu = _api!.CreateMenuWithBack(context, admin);

    var players = _api.GetValidPlayers()
        .Where(p => p.PawnIsAlive && admin.CanTarget(p));

    foreach (var player in players)
    {
        _api.AddSubMenu(menu, player.PlayerName, admin =>
        {
            return CreateHpValueMenu(admin, player);
        });
    }

    return menu;
}

private object CreateHpValueMenu(CCSPlayerController admin, CCSPlayerController target)
{
    var menu = _api!.CreateMenuWithBack($"Set HP: {target.PlayerName}", "mycategory", admin);

    var hpValues = new[] { 1, 10, 50, 100, 200, 500 };

    foreach (var hp in hpValues)
    {
        _api.AddMenuOption(menu, $"{hp} HP", _ =>
        {
            if (target.IsValid && target.PawnIsAlive)
            {
                target.PlayerPawn?.Value?.SetHealth(hp);
                admin.PrintToChat($"Set {target.PlayerName} HP to {hp}");
            }
        });
    }

    return menu;
}
```

---

### Menu with Permissions

```csharp
private object CreateAdminMenu(CCSPlayerController admin, MenuContext context)
{
    var menu = _api!.CreateMenuWithBack(context, admin);

    // Everyone with menu access sees this
    _api.AddMenuOption(menu, "Basic Action", _ => DoBasicAction());

    // Only root admins see this
    _api.AddMenuOption(menu, "Dangerous Action", _ =>
    {
        DoDangerousAction();
    }, false, "@css/root");

    // Submenu with permission
    _api.AddSubMenu(menu, "Advanced Options", admin =>
    {
        return CreateAdvancedMenu(admin);
    }, false, "@css/root");

    return menu;
}
```

---

### Dynamic Menu with Current State

```csharp
private object CreateToggleMenu(CCSPlayerController admin, MenuContext context)
{
    var menu = _api!.CreateMenuWithBack(context, admin);

    var players = _api.GetValidPlayers()
        .Where(p => admin.CanTarget(p));

    foreach (var player in players)
    {
        // Show current state in option name
        bool hasGod = GodPlayers.Contains(player.Slot);
        string status = hasGod ? "✓ ON" : "✗ OFF";

        _api.AddMenuOption(menu, $"{player.PlayerName} ({status})", _ =>
        {
            if (hasGod)
                GodPlayers.Remove(player.Slot);
            else
                GodPlayers.Add(player.Slot);

            // Recreate menu to show updated state
            var newMenu = CreateToggleMenu(admin, context);
            _api.OpenMenu(newMenu, admin);
        });
    }

    return menu;
}
```

---

## Best Practices

### 1. Use MenuContext

```csharp
// ✅ Good - Uses context
_api.RegisterMenu("cat", "id", "Title", CreateMenu, "@css/generic");

private object CreateMenu(CCSPlayerController admin, MenuContext context)
{
    return _api.CreateMenuWithPlayers(context, admin, filter, action);
}

// ❌ Bad - Duplicates title and category
_api.RegisterMenu("cat", "id", "Title", CreateMenuOld, "@css/generic");

private object CreateMenuOld(CCSPlayerController admin)
{
    return _api.CreateMenuWithPlayers("Title", "cat", admin, filter, action);
}
```

### 2. Register in OnSimpleAdminReady

```csharp
_api.OnSimpleAdminReady += RegisterMenus;
RegisterMenus();  // Also call directly for hot reload

private void RegisterMenus()
{
    if (_menusRegistered) return;

    _api!.RegisterMenuCategory("category", "Category Name");
    _api.RegisterMenu("category", "menu", "Menu Name", CreateMenu);

    _menusRegistered = true;
}
```

### 3. Always Unregister

```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    _api.UnregisterMenu("category", "menu");
    _api.OnSimpleAdminReady -= RegisterMenus;
}
```

### 4. Validate Player State

```csharp
private object CreateMenu(CCSPlayerController admin, MenuContext context)
{
    return _api!.CreateMenuWithPlayers(
        context,
        admin,
        player => player.IsValid &&          // Player exists
                  !player.IsBot &&            // Not a bot
                  player.PawnIsAlive &&       // Alive
                  admin.CanTarget(player),    // Can be targeted
        (admin, target) =>
        {
            // Extra validation before action
            if (!target.IsValid || !target.PawnIsAlive)
                return;

            DoAction(admin, target);
        }
    );
}
```

### 5. Use Translations for Menu Names

```csharp
_api.RegisterMenuCategory(
    "mycategory",
    Localizer?["category_name"] ?? "Default Name",
    "@css/generic"
);

_api.RegisterMenu(
    "mycategory",
    "mymenu",
    Localizer?["menu_name"] ?? "Default Menu",
    CreateMenu
);
```

---

## Permission Override

The `commandName` parameter allows server admins to override menu permissions via CounterStrikeSharp's admin system.

**Example:**
```csharp
_api.RegisterMenu(
    "fun",
    "god",
    "God Mode",
    CreateGodMenu,
    "@css/cheats",    // Default permission
    "css_god"         // Command name for override
);
```

**Admin config can override:**
```json
{
  "css_god": ["@css/vip"]
}
```

Now VIPs will see the God Mode menu instead of requiring @css/cheats!

---

## Common Patterns

### Player List with Actions

```csharp
private object CreatePlayerListMenu(CCSPlayerController admin, MenuContext context)
{
    var menu = _api!.CreateMenuWithBack(context, admin);

    foreach (var player in _api.GetValidPlayers())
    {
        if (!admin.CanTarget(player)) continue;

        _api.AddSubMenu(menu, player.PlayerName, admin =>
        {
            var actionMenu = _api.CreateMenuWithBack($"Actions: {player.PlayerName}", context.CategoryId, admin);

            _api.AddMenuOption(actionMenu, "Slay", _ => player.CommitSuicide());
            _api.AddMenuOption(actionMenu, "Kick", _ => KickPlayer(player));
            _api.AddMenuOption(actionMenu, "Ban", _ => BanPlayer(admin, player));

            return actionMenu;
        });
    }

    return menu;
}
```

### Category-Based Organization

```csharp
private void RegisterAllMenus()
{
    // Player management category
    _api!.RegisterMenuCategory("players", "Player Management", "@css/generic");
    _api.RegisterMenu("players", "kick", "Kick Player", CreateKickMenu, "@css/kick");
    _api.RegisterMenu("players", "ban", "Ban Player", CreateBanMenu, "@css/ban");

    // Server management category
    _api.RegisterMenuCategory("server", "Server Management", "@css/generic");
    _api.RegisterMenu("server", "map", "Change Map", CreateMapMenu, "@css/changemap");
    _api.RegisterMenu("server", "settings", "Settings", CreateSettingsMenu, "@css/root");
}
```

---

## Related APIs

- **[Commands API](commands)** - Command integration
- **[Penalties API](penalties)** - Issue penalties from menus
- **[Utilities API](utilities)** - Helper functions for menus
