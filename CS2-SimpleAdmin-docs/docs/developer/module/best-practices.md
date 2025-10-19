---
sidebar_position: 2
---

# Best Practices

Guidelines for writing high-quality CS2-SimpleAdmin modules.

## Code Organization

### Use Partial Classes

Split your code into logical files:

```
MyModule/
├── MyModule.cs          # Main class, initialization
├── Commands.cs          # Command handlers
├── Menus.cs            # Menu creation
├── Actions.cs          # Core logic
└── Config.cs           # Configuration
```

```csharp
// MyModule.cs
public partial class MyModule : BasePlugin, IPluginConfig<Config>
{
    // Initialization
}

// Commands.cs
public partial class MyModule
{
    private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Command logic
    }
}

// Menus.cs
public partial class MyModule
{
    private object CreateMyMenu(CCSPlayerController player, MenuContext context)
    {
        // Menu logic
    }
}
```

**Benefits:**
- ✅ Easy to navigate
- ✅ Logical separation
- ✅ Better maintainability

---

## Configuration

### Use Command Lists

Allow users to customize aliases:

```csharp
public class Config : IBasePluginConfig
{
    // ✅ Good - List allows multiple aliases
    public List<string> MyCommands { get; set; } = ["css_mycommand"];

    // ❌ Bad - Single string
    public string MyCommand { get; set; } = "css_mycommand";
}
```

**Usage:**
```csharp
foreach (var cmd in Config.MyCommands)
{
    _api!.RegisterCommand(cmd, "Description", OnMyCommand);
}
```

### Provide Sensible Defaults

```csharp
public class Config : IBasePluginConfig
{
    public int Version { get; set; } = 1;

    // Good defaults
    public bool EnableFeature { get; set; } = true;
    public int MaxValue { get; set; } = 100;
    public List<string> Commands { get; set; } = ["css_default"];
}
```

---

## API Usage

### Always Check for Null

```csharp
// ✅ Good
if (_api == null)
{
    Logger.LogError("API not available!");
    return;
}

_api.RegisterCommand(...);

// ❌ Bad
_api!.RegisterCommand(...);  // Can crash if null
```

### Use OnSimpleAdminReady Pattern

```csharp
// ✅ Good - Handles both normal load and hot reload
_api.OnSimpleAdminReady += RegisterMenus;
RegisterMenus();  // Also call directly

// ❌ Bad - Only works on normal load
_api.OnSimpleAdminReady += RegisterMenus;
```

### Always Clean Up

```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    // Unregister ALL commands
    foreach (var cmd in Config.MyCommands)
    {
        _api.UnRegisterCommand(cmd);
    }

    // Unregister ALL menus
    _api.UnregisterMenu("category", "menu");

    // Unsubscribe ALL events
    _api.OnSimpleAdminReady -= RegisterMenus;
    _api.OnPlayerPenaltied -= OnPlayerPenaltied;
}
```

---

## Player Validation

### Validate Before Acting

```csharp
// ✅ Good - Multiple checks
if (!player.IsValid)
{
    Logger.LogWarning("Player is invalid!");
    return;
}

if (!player.PawnIsAlive)
{
    caller?.PrintToChat("Target must be alive!");
    return;
}

if (admin != null && !admin.CanTarget(player))
{
    admin.PrintToChat("Cannot target this player!");
    return;
}

// Safe to proceed
DoAction(player);
```

### Check State Changes

```csharp
// ✅ Good - Validate in callback
_api.AddMenuOption(menu, "Action", _ =>
{
    // Validate again - player state may have changed
    if (!target.IsValid || !target.PawnIsAlive)
        return;

    DoAction(target);
});

// ❌ Bad - No validation in callback
_api.AddMenuOption(menu, "Action", _ =>
{
    DoAction(target);  // Might crash!
});
```

---

## Translations

### Use MenuContext for Menus

```csharp
// ✅ Good - No duplication
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

### Use Per-Player Translations

```csharp
// ✅ Good - Each player sees their language
if (Localizer != null)
{
    _api.ShowAdminActivityLocalized(
        Localizer,
        "translation_key",
        admin?.PlayerName,
        false,
        args
    );
}

// ❌ Bad - Single language for all
Server.PrintToChatAll($"{admin?.PlayerName} did something");
```

### Provide English Fallbacks

```csharp
// ✅ Good - Fallback if translation missing
_api.RegisterMenuCategory(
    "mycat",
    Localizer?["category_name"] ?? "Default Category Name",
    "@css/generic"
);

// ❌ Bad - No fallback
_api.RegisterMenuCategory(
    "mycat",
    Localizer["category_name"],  // Crashes if no translation!
    "@css/generic"
);
```

---

## Performance

### Cache Expensive Operations

```csharp
// ✅ Good - Cache on first access
private static Dictionary<int, string>? _itemCache;

private static Dictionary<int, string> GetItemCache()
{
    if (_itemCache != null) return _itemCache;

    // Build cache once
    _itemCache = new Dictionary<int, string>();
    // ... populate
    return _itemCache;
}

// ❌ Bad - Rebuild every time
private Dictionary<int, string> GetItems()
{
    var items = new Dictionary<int, string>();
    // ... expensive operation
    return items;
}
```

### Efficient LINQ Queries

```csharp
// ✅ Good - Single query
var players = _api.GetValidPlayers()
    .Where(p => p.IsValid && !p.IsBot && p.PawnIsAlive)
    .ToList();

// ❌ Bad - Multiple iterations
var players = _api.GetValidPlayers();
players = players.Where(p => p.IsValid).ToList();
players = players.Where(p => !p.IsBot).ToList();
players = players.Where(p => p.PawnIsAlive).ToList();
```

---

## Error Handling

### Log Errors

```csharp
// ✅ Good - Detailed logging
try
{
    DoAction();
}
catch (Exception ex)
{
    Logger.LogError($"Failed to perform action: {ex.Message}");
    Logger.LogError($"Stack trace: {ex.StackTrace}");
}

// ❌ Bad - Silent failure
try
{
    DoAction();
}
catch
{
    // Ignore
}
```

### Graceful Degradation

```csharp
// ✅ Good - Continue with reduced functionality
_api = _pluginCapability.Get();
if (_api == null)
{
    Logger.LogError("SimpleAdmin API not found - limited functionality!");
    // Module still loads, just without SimpleAdmin integration
    return;
}

// ❌ Bad - Crash the entire module
_api = _pluginCapability.Get() ?? throw new Exception("No API!");
```

---

## Security

### Validate Admin Permissions

```csharp
// ✅ Good - Check permissions
[RequiresPermissions("@css/ban")]
private void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Already validated by attribute
}

// ❌ Bad - No permission check
private void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Anyone can use this!
}
```

### Check Immunity

```csharp
// ✅ Good - Check immunity
if (admin != null && !admin.CanTarget(target))
{
    admin.PrintToChat($"Cannot target {target.PlayerName}!");
    return;
}

// ❌ Bad - Ignore immunity
DoAction(target);  // Can target higher immunity!
```

### Sanitize Input

```csharp
// ✅ Good - Validate and sanitize
private void OnSetValueCommand(CCSPlayerController? caller, CommandInfo command)
{
    if (!int.TryParse(command.GetArg(1), out int value))
    {
        caller?.PrintToChat("Invalid number!");
        return;
    }

    if (value < 0 || value > 1000)
    {
        caller?.PrintToChat("Value must be between 0 and 1000!");
        return;
    }

    SetValue(value);
}

// ❌ Bad - No validation
private void OnSetValueCommand(CCSPlayerController? caller, CommandInfo command)
{
    var value = int.Parse(command.GetArg(1));  // Can crash!
    SetValue(value);  // No range check!
}
```

---

## Documentation

### Comment Complex Logic

```csharp
// ✅ Good - Explain why, not what
// We need to check immunity twice because player state can change
// between menu creation and action execution
if (!admin.CanTarget(player))
{
    return;
}

// ❌ Bad - States the obvious
// Check if admin can target player
if (!admin.CanTarget(player))
{
    return;
}
```

### XML Documentation

```csharp
/// <summary>
/// Toggles god mode for the specified player.
/// </summary>
/// <param name="admin">Admin performing the action (null for console)</param>
/// <param name="target">Player to toggle god mode for</param>
/// <returns>True if god mode is now enabled, false otherwise</returns>
public bool ToggleGodMode(CCSPlayerController? admin, CCSPlayerController target)
{
    // Implementation
}
```

---

## Testing

### Test Edge Cases

```csharp
// Test with:
// - Invalid players
// - Disconnected players
// - Players who changed teams
// - Null admins (console)
// - Silent admins
// - Players with higher immunity
```

### Test Hot Reload

```bash
# Server console
css_plugins reload YourModule
```

Make sure everything works after reload!

---

## Common Mistakes

### ❌ Forgetting to Unsubscribe

```csharp
public override void Unload(bool hotReload)
{
    // Missing unsubscribe = memory leak!
    // _api.OnSimpleAdminReady -= RegisterMenus;  ← FORGOT THIS
}
```

### ❌ Not Checking API Availability

```csharp
// Crashes if SimpleAdmin not loaded!
_api.RegisterCommand(...);  // ← No null check
```

### ❌ Hardcoding Strings

```csharp
// Bad - not translatable
player.PrintToChat("You have been banned!");

// Good - uses translations
var message = Localizer?["ban_message"] ?? "You have been banned!";
player.PrintToChat(message);
```

### ❌ Blocking Game Thread

```csharp
// Bad - blocks game thread
Thread.Sleep(5000);

// Good - use CounterStrikeSharp timers
AddTimer(5.0f, () => DoAction());
```

---

## Reference Implementation

Study the **Fun Commands Module** for best practices:

**[View Source](https://github.com/daffyyyy/CS2-SimpleAdmin/tree/main/Modules/CS2-SimpleAdmin_FunCommands)**

Shows:
- ✅ Proper code organization
- ✅ Configuration best practices
- ✅ Menu creation with context
- ✅ Per-player translations
- ✅ Proper cleanup
- ✅ Error handling

---

## Next Steps

- **[Examples](examples)** - More code examples
- **[API Reference](../api/overview)** - Full API documentation
- **[GitHub](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Browse source code
