---
sidebar_position: 1
---

# API Overview

Complete reference for the CS2-SimpleAdmin API (ICS2_SimpleAdminApi).

## Introduction

The CS2-SimpleAdmin API is exposed via the `ICS2_SimpleAdminApi` interface, accessible through CounterStrikeSharp's capability system.

---

## Getting the API

### Using Capability System

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CS2_SimpleAdminApi;

public class YourPlugin : BasePlugin
{
    private ICS2_SimpleAdminApi? _api;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability =
        new("simpleadmin:api");

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();

        if (_api == null)
        {
            Logger.LogError("CS2-SimpleAdmin API not found!");
            Unload(false);
            return;
        }

        // API is ready to use
        RegisterFeatures();
    }
}
```

### Static Capability Reference

```csharp
// Alternative approach
var capability = ICS2_SimpleAdminApi.PluginCapability;
var api = capability?.Get();
```

---

## API Categories

The API is organized into logical categories:

| Category | Description | Learn More |
|----------|-------------|------------|
| **Commands** | Register/unregister commands, parse targets | [→](commands) |
| **Menus** | Create admin menus with player selection | [→](menus) |
| **Penalties** | Issue bans, mutes, gags, warnings | [→](penalties) |
| **Events** | Subscribe to plugin events | [→](events) |
| **Utilities** | Helper functions, player info, activity messages | [→](utilities) |

---

## Quick Reference

### Command Management

```csharp
// Register command
_api.RegisterCommand(name, description, callback);

// Unregister command
_api.UnRegisterCommand(name);

// Parse player targets
var targets = _api.GetTarget(command);

// Log command
_api.LogCommand(caller, command);
```

**[Full Documentation →](commands)**

---

### Menu System

```csharp
// Register category
_api.RegisterMenuCategory(categoryId, categoryName, permission);

// Register menu
_api.RegisterMenu(categoryId, menuId, menuName, menuFactory, permission, commandName);

// Create menu with players
_api.CreateMenuWithPlayers(context, admin, filter, onSelect);

// Create menu with back button
_api.CreateMenuWithBack(context, admin);

// Add menu option
_api.AddMenuOption(menu, name, action, disabled, permission);

// Add submenu
_api.AddSubMenu(menu, name, subMenuFactory, disabled, permission);

// Open menu
_api.OpenMenu(menu, player);

// Unregister menu
_api.UnregisterMenu(categoryId, menuId);
```

**[Full Documentation →](menus)**

---

### Penalty Management

```csharp
// Issue penalty to online player
_api.IssuePenalty(player, admin, penaltyType, reason, duration);

// Issue penalty by SteamID
_api.IssuePenalty(steamId, admin, penaltyType, reason, duration);

// Get player info
var playerInfo = _api.GetPlayerInfo(player);

// Get mute status
var muteStatus = _api.GetPlayerMuteStatus(player);
```

**Penalty Types:**
- `PenaltyType.Ban` - Ban player
- `PenaltyType.Kick` - Kick player
- `PenaltyType.Gag` - Block text chat
- `PenaltyType.Mute` - Block voice chat
- `PenaltyType.Silence` - Block both
- `PenaltyType.Warn` - Issue warning

**[Full Documentation →](penalties)**

---

### Event System

```csharp
// Plugin ready event
_api.OnSimpleAdminReady += OnReady;

// Player penaltied
_api.OnPlayerPenaltied += OnPlayerPenaltied;

// Offline penalty added
_api.OnPlayerPenaltiedAdded += OnPlayerPenaltiedAdded;

// Admin activity
_api.OnAdminShowActivity += OnAdminActivity;

// Admin silent toggle
_api.OnAdminToggleSilent += OnAdminToggleSilent;
```

**[Full Documentation →](events)**

---

### Utility Functions

```csharp
// Get player info with penalties
var info = _api.GetPlayerInfo(player);

// Get database connection string
var connectionString = _api.GetConnectionString();

// Get server address
var serverAddress = _api.GetServerAddress();

// Get server ID
var serverId = _api.GetServerId();

// Get valid players
var players = _api.GetValidPlayers();

// Check if admin is silent
bool isSilent = _api.IsAdminSilent(player);

// Get all silent admins
var silentAdmins = _api.ListSilentAdminsSlots();

// Show admin activity
_api.ShowAdminActivity(messageKey, callerName, dontPublish, args);

// Show admin activity with custom translation
_api.ShowAdminActivityTranslated(translatedMessage, callerName, dontPublish);

// Show admin activity with module localizer (recommended)
_api.ShowAdminActivityLocalized(moduleLocalizer, messageKey, callerName, dontPublish, args);
```

**[Full Documentation →](utilities)**

---

## Common Patterns

### Basic Module Structure

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CS2_SimpleAdminApi;

namespace MyModule;

public class MyModule : BasePlugin
{
    private ICS2_SimpleAdminApi? _api;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability =
        new("simpleadmin:api");

    public override string ModuleName => "My Module";
    public override string ModuleVersion => "1.0.0";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api == null) return;

        // Register features
        RegisterCommands();

        // Wait for SimpleAdmin ready
        _api.OnSimpleAdminReady += RegisterMenus;
        RegisterMenus();  // Fallback for hot reload
    }

    public override void Unload(bool hotReload)
    {
        if (_api == null) return;

        // Cleanup
        _api.UnRegisterCommand("css_mycommand");
        _api.UnregisterMenu("category", "menu");
        _api.OnSimpleAdminReady -= RegisterMenus;
    }
}
```

---

### Command with Target Selection

```csharp
[CommandHelper(1, "<#userid or name>")]
[RequiresPermissions("@css/generic")]
private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Parse targets
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    // Filter valid players
    var players = targets.Players
        .Where(p => p.IsValid && !p.IsBot && caller!.CanTarget(p))
        .ToList();

    // Process each player
    foreach (var player in players)
    {
        DoSomethingToPlayer(caller, player);
    }

    // Log command
    _api.LogCommand(caller, command);
}
```

---

### Menu with Player Selection

```csharp
private object CreateMyMenu(CCSPlayerController admin, MenuContext context)
{
    // Context contains categoryId, menuId, menuName, permission, commandName
    return _api!.CreateMenuWithPlayers(
        context,  // Automatic title and category
        admin,
        player => player.IsValid && admin.CanTarget(player),
        (admin, target) =>
        {
            // Action when player selected
            PerformAction(admin, target);
        }
    );
}
```

---

### Nested Menu

```csharp
private object CreatePlayerMenu(CCSPlayerController admin, MenuContext context)
{
    var menu = _api!.CreateMenuWithBack(context, admin);

    var players = _api.GetValidPlayers()
        .Where(p => admin.CanTarget(p));

    foreach (var player in players)
    {
        _api.AddSubMenu(menu, player.PlayerName, admin =>
        {
            return CreateActionMenu(admin, player);
        });
    }

    return menu;
}

private object CreateActionMenu(CCSPlayerController admin, CCSPlayerController target)
{
    var menu = _api!.CreateMenuWithBack($"Actions for {target.PlayerName}", "category", admin);

    _api.AddMenuOption(menu, "Action 1", _ => DoAction1(admin, target));
    _api.AddMenuOption(menu, "Action 2", _ => DoAction2(admin, target));

    return menu;
}
```

---

### Issue Penalty

```csharp
private void BanPlayer(CCSPlayerController? admin, CCSPlayerController target, int duration, string reason)
{
    // Issue ban
    _api!.IssuePenalty(
        target,
        admin,
        PenaltyType.Ban,
        reason,
        duration  // minutes, 0 = permanent
    );

    // Show activity
    if (admin == null || !_api.IsAdminSilent(admin))
    {
        _api.ShowAdminActivityLocalized(
            Localizer,
            "ban_message",
            admin?.PlayerName,
            false,
            target.PlayerName,
            duration
        );
    }
}
```

---

### Event Subscription

```csharp
public override void OnAllPluginsLoaded(bool hotReload)
{
    _api = _pluginCapability.Get();
    if (_api == null) return;

    // Subscribe to events
    _api.OnPlayerPenaltied += OnPlayerPenaltied;
}

private void OnPlayerPenaltied(
    PlayerInfo player,
    PlayerInfo? admin,
    PenaltyType type,
    string reason,
    int duration,
    int? penaltyId,
    int? serverId)
{
    Logger.LogInformation($"{player.PlayerName} received {type}: {reason} ({duration} min)");

    // React to penalty
    if (type == PenaltyType.Ban)
    {
        // Handle ban
    }
}

public override void Unload(bool hotReload)
{
    if (_api == null) return;
    _api.OnPlayerPenaltied -= OnPlayerPenaltied;
}
```

---

## Best Practices

### 1. Always Check for Null

```csharp
if (_api == null)
{
    Logger.LogError("API not available!");
    return;
}
```

### 2. Use OnSimpleAdminReady Event

```csharp
_api.OnSimpleAdminReady += () =>
{
    // Register menus only when SimpleAdmin is ready
    RegisterMenus();
};

// Also call directly for hot reload case
RegisterMenus();
```

### 3. Clean Up on Unload

```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    // Unregister all commands
    _api.UnRegisterCommand("css_mycommand");

    // Unregister all menus
    _api.UnregisterMenu("category", "menu");

    // Unsubscribe all events
    _api.OnSimpleAdminReady -= OnReady;
}
```

### 4. Validate Player State

```csharp
if (!player.IsValid || !player.PawnIsAlive)
{
    return;
}

if (!caller.CanTarget(player))
{
    return;  // Immunity check
}
```

### 5. Use Per-Player Translations

```csharp
// Each player sees message in their configured language
_api.ShowAdminActivityLocalized(
    Localizer,  // Your module's localizer
    "translation_key",
    caller?.PlayerName,
    false,
    args
);
```

### 6. Log All Admin Actions

```csharp
_api.LogCommand(caller, command);
// or
_api.LogCommand(caller, $"css_mycommand {player.PlayerName}");
```

---

## API Versioning

The API uses semantic versioning:
- **Major** - Breaking changes
- **Minor** - New features, backwards compatible
- **Patch** - Bug fixes

**Current Version:** Check [GitHub Releases](https://github.com/daffyyyy/CS2-SimpleAdmin/releases)

---

## Thread Safety

The API is designed for single-threaded use within the CounterStrikeSharp game thread.

**Do NOT:**
- Call API methods from background threads
- Use async/await with API calls without proper synchronization

**Do:**
- Call API methods from event handlers
- Call API methods from commands
- Call API methods from timers

---

## Error Handling

The API uses exceptions for critical errors:

```csharp
try
{
    _api.RegisterCommand("css_cmd", "Desc", callback);
}
catch (ArgumentException ex)
{
    Logger.LogError($"Failed to register command: {ex.Message}");
}
```

**Common exceptions:**
- `ArgumentException` - Invalid arguments
- `InvalidOperationException` - Invalid state
- `KeyNotFoundException` - Player not found

---

## Performance Considerations

### Efficient Player Filtering

```csharp
// ✅ Good - single LINQ query
var players = _api.GetValidPlayers()
    .Where(p => p.IsValid && admin.CanTarget(p))
    .ToList();

// ❌ Bad - multiple iterations
var players = _api.GetValidPlayers();
players = players.Where(p => p.IsValid).ToList();
players = players.Where(p => admin.CanTarget(p)).ToList();
```

### Cache Expensive Operations

```csharp
// Cache menu creation if used multiple times
private object? _cachedMenu;

private object GetMenu(CCSPlayerController player)
{
    if (_cachedMenu == null)
    {
        _cachedMenu = CreateMenu(player);
    }
    return _cachedMenu;
}
```

---

## Debugging

### Enable Detailed Logging

```csharp
Logger.LogInformation("Debug: API loaded");
Logger.LogWarning("Warning: Player not found");
Logger.LogError("Error: Failed to execute command");
```

### Check API Availability

```csharp
public override void OnAllPluginsLoaded(bool hotReload)
{
    _api = _pluginCapability.Get();

    if (_api == null)
    {
        Logger.LogError("❌ CS2-SimpleAdmin API not found!");
        Logger.LogError("Make sure CS2-SimpleAdmin is installed and loaded.");
        return;
    }

    Logger.LogInformation("✅ CS2-SimpleAdmin API loaded successfully");
}
```

---

## Next Steps

- **[Commands API](commands)** - Command registration and targeting
- **[Menus API](menus)** - Menu system details
- **[Penalties API](penalties)** - Penalty management
- **[Events API](events)** - Event subscription
- **[Utilities API](utilities)** - Helper functions

---

## Resources

- **[GitHub Repository](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Source code
- **[Fun Commands Module](https://github.com/daffyyyy/CS2-SimpleAdmin/tree/main/Modules/CS2-SimpleAdmin_FunCommands)** - Reference implementation
- **[Module Development Guide](../module/getting-started)** - Create modules
