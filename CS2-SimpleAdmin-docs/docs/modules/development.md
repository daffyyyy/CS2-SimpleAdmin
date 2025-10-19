---
sidebar_position: 3
---

# Module Development

Learn how to create your own CS2-SimpleAdmin modules.

## Introduction

Creating modules for CS2-SimpleAdmin allows you to extend the plugin's functionality while keeping your code separate and maintainable.

:::tip Reference Implementation
The **[Fun Commands Module](https://github.com/daffyyyy/CS2-SimpleAdmin/tree/main/Modules/CS2-SimpleAdmin_FunCommands)** serves as a complete reference implementation. Study its code to learn best practices!
:::

---

## Prerequisites

### Knowledge Required

- C# programming (intermediate level)
- .NET 8.0
- CounterStrikeSharp basics
- Understanding of CS2-SimpleAdmin structure

### Tools Needed

- Visual Studio 2022 or VS Code
- .NET 8.0 SDK
- CS2 server for testing

---

## Quick Start

### 1. Create Project

```bash
dotnet new classlib -n YourModuleName -f net8.0
cd YourModuleName
```

### 2. Add References

Edit your `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- CounterStrikeSharp -->
    <Reference Include="CounterStrikeSharp.API">
      <HintPath>path/to/CounterStrikeSharp.API.dll</HintPath>
      <Private>false</Private>
    </Reference>

    <!-- CS2-SimpleAdmin API -->
    <Reference Include="CS2-SimpleAdminApi">
      <HintPath>path/to/CS2-SimpleAdminApi.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

### 3. Create Main Plugin Class

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CS2_SimpleAdminApi;

namespace YourModuleName;

public class YourModule : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Your Module Name";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Your Name";
    public override string ModuleDescription => "Description";

    private ICS2_SimpleAdminApi? _api;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability = new("simpleadmin:api");

    public Config Config { get; set; } = new();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        // Get SimpleAdmin API
        _api = _pluginCapability.Get();
        if (_api == null)
        {
            Logger.LogError("CS2-SimpleAdmin API not found!");
            return;
        }

        // Register your commands and menus
        RegisterCommands();
        _api.OnSimpleAdminReady += RegisterMenus;
        RegisterMenus(); // Fallback for hot reload
    }

    public void OnConfigParsed(Config config)
    {
        Config = config;
    }

    private void RegisterCommands()
    {
        // Register commands here
    }

    private void RegisterMenus()
    {
        // Register menus here
    }
}
```

---

## Module Structure

### Recommended File Organization

```
YourModuleName/
├── YourModule.cs          # Main plugin class
├── Config.cs              # Configuration
├── Commands.cs            # Command handlers (partial class)
├── Menus.cs              # Menu creation (partial class)
├── Actions.cs            # Core logic (partial class)
├── lang/                 # Translations
│   ├── en.json
│   ├── pl.json
│   └── ...
└── YourModuleName.csproj
```

### Using Partial Classes

Split your code for better organization:

```csharp
// YourModule.cs
public partial class YourModule : BasePlugin, IPluginConfig<Config>
{
    // Plugin initialization
}

// Commands.cs
public partial class YourModule
{
    private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Command logic
    }
}

// Menus.cs
public partial class YourModule
{
    private object CreateMyMenu(CCSPlayerController player, MenuContext context)
    {
        // Menu creation
    }
}
```

---

## Configuration

### Create Config Class

```csharp
using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

public class Config : IBasePluginConfig
{
    [JsonPropertyName("Version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("MyCommands")]
    public List<string> MyCommands { get; set; } = ["css_mycommand"];

    [JsonPropertyName("EnableFeature")]
    public bool EnableFeature { get; set; } = true;

    [JsonPropertyName("MaxValue")]
    public int MaxValue { get; set; } = 100;
}
```

### Config Best Practices

1. **Use command lists** - Allow users to add aliases or disable features
2. **Provide defaults** - Sensible default values
3. **Version your config** - Track config changes
4. **Document settings** - Clear property names

---

## Registering Commands

### Basic Command Registration

```csharp
private void RegisterCommands()
{
    if (_api == null) return;

    foreach (var cmd in Config.MyCommands)
    {
        _api.RegisterCommand(cmd, "Command description", OnMyCommand);
    }
}

[CommandHelper(1, "<#userid or name>")]
[RequiresPermissions("@css/generic")]
private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Get target players
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    // Filter for valid players
    var players = targets.Players
        .Where(p => p.IsValid && !p.IsBot)
        .ToList();

    // Process each player
    foreach (var player in players)
    {
        if (caller!.CanTarget(player))
        {
            DoSomething(caller, player);
        }
    }

    // Log the command
    _api.LogCommand(caller, command);
}
```

### Command Cleanup

Always unregister commands when unloading:

```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    foreach (var cmd in Config.MyCommands)
    {
        _api.UnRegisterCommand(cmd);
    }
}
```

---

## Creating Menus

### Register Menu Category

```csharp
private void RegisterMenus()
{
    if (_api == null || _menusRegistered) return;

    // Register category
    _api.RegisterMenuCategory(
        "mycategory",
        Localizer?["category_name"] ?? "My Category",
        "@css/generic"
    );

    // Register menu
    _api.RegisterMenu(
        "mycategory",
        "mymenu",
        Localizer?["menu_name"] ?? "My Menu",
        CreateMyMenu,
        "@css/generic",
        "css_mycommand"  // For permission override
    );

    _menusRegistered = true;
}
```

### Menu with Player Selection (NEW API)

```csharp
private object CreateMyMenu(CCSPlayerController admin, MenuContext context)
{
    // Context contains: CategoryId, MenuId, MenuTitle, Permission, CommandName
    // No need to repeat "mycategory" and "My Menu" here!

    return _api!.CreateMenuWithPlayers(
        context,  // ← Automatically uses menu title and category
        admin,
        player => player.IsValid && admin.CanTarget(player),
        (admin, target) => DoSomethingToPlayer(admin, target)
    );
}
```

### Menu with Custom Options

```csharp
private object CreateValueSelectionMenu(CCSPlayerController admin, MenuContext context)
{
    var menu = _api!.CreateMenuWithBack(context, admin);

    var values = new[] { 10, 25, 50, 100, 200 };

    foreach (var value in values)
    {
        _api.AddMenuOption(menu, $"{value} points", player =>
        {
            GivePoints(player, value);
        });
    }

    return menu;
}
```

### Nested Menus

```csharp
private object CreatePlayerSelectionMenu(CCSPlayerController admin, MenuContext context)
{
    var menu = _api!.CreateMenuWithBack(context, admin);

    var players = _api.GetValidPlayers()
        .Where(p => admin.CanTarget(p));

    foreach (var player in players)
    {
        _api.AddSubMenu(menu, player.PlayerName, admin =>
        {
            return CreateValueMenu(admin, player);
        });
    }

    return menu;
}

private object CreateValueMenu(CCSPlayerController admin, CCSPlayerController target)
{
    var menu = _api!.CreateMenuWithBack($"Select value for {target.PlayerName}", "mycategory", admin);

    // Add options...

    return menu;
}
```

---

## Translations

### Create Translation Files

Create `lang/en.json`:

```json
{
  "command_success": "{green}Success! {default}Action performed on {lightred}{0}",
  "command_failed": "{red}Failed! {default}Could not perform action",
  "menu_title": "My Custom Menu"
}
```

### Use Translations in Code

```csharp
// In commands
private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Using module's own localizer for per-player language
    if (Localizer != null)
    {
        _api!.ShowAdminActivityLocalized(
            Localizer,
            "command_success",
            caller?.PlayerName,
            false,
            target.PlayerName
        );
    }
}
```

### Multiple Language Support

Create files for each language:
- `lang/en.json` - English
- `lang/pl.json` - Polish
- `lang/ru.json` - Russian
- `lang/de.json` - German
- etc.

---

## Working with API

### Issue Penalties

```csharp
// Ban online player
_api!.IssuePenalty(
    player,
    admin,
    PenaltyType.Ban,
    "Cheating",
    1440  // 1 day in minutes
);

// Ban offline player by SteamID
_api!.IssuePenalty(
    new SteamID(76561198012345678),
    admin,
    PenaltyType.Ban,
    "Ban evasion",
    0  // Permanent
);

// Other penalty types
_api!.IssuePenalty(player, admin, PenaltyType.Gag, "Chat spam", 30);
_api!.IssuePenalty(player, admin, PenaltyType.Mute, "Mic spam", 60);
_api!.IssuePenalty(player, admin, PenaltyType.Silence, "Total abuse", 120);
_api!.IssuePenalty(player, admin, PenaltyType.Warn, "Rule break");
```

### Get Player Information

```csharp
// Get player info with penalty data
var playerInfo = _api!.GetPlayerInfo(player);

Console.WriteLine($"Player: {playerInfo.PlayerName}");
Console.WriteLine($"SteamID: {playerInfo.SteamId}");
Console.WriteLine($"Warnings: {playerInfo.Warnings}");

// Get player mute status
var muteStatus = _api!.GetPlayerMuteStatus(player);

if (muteStatus.ContainsKey(PenaltyType.Gag))
{
    Console.WriteLine("Player is gagged");
}
```

### Check Admin Status

```csharp
// Check if admin is in silent mode
if (_api!.IsAdminSilent(admin))
{
    // Don't broadcast this action
}

// Get all silent admins
var silentAdmins = _api!.ListSilentAdminsSlots();
```

---

## Events

### Subscribe to Events

```csharp
public override void OnAllPluginsLoaded(bool hotReload)
{
    _api = _pluginCapability.Get();

    // Subscribe to events
    _api.OnSimpleAdminReady += OnSimpleAdminReady;
    _api.OnPlayerPenaltied += OnPlayerPenaltied;
    _api.OnPlayerPenaltiedAdded += OnPlayerPenaltiedAdded;
    _api.OnAdminShowActivity += OnAdminShowActivity;
}

private void OnSimpleAdminReady()
{
    Logger.LogInformation("SimpleAdmin is ready!");
    RegisterMenus();
}

private void OnPlayerPenaltied(PlayerInfo player, PlayerInfo? admin,
    PenaltyType type, string reason, int duration, int? penaltyId, int? serverId)
{
    Logger.LogInformation($"{player.PlayerName} received {type} for {reason}");
}

private void OnPlayerPenaltiedAdded(SteamID steamId, PlayerInfo? admin,
    PenaltyType type, string reason, int duration, int? penaltyId, int? serverId)
{
    Logger.LogInformation($"Offline ban added to {steamId}");
}

private void OnAdminShowActivity(string messageKey, string? callerName,
    bool dontPublish, object messageArgs)
{
    // React to admin activity
}
```

### Unsubscribe on Unload

```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    _api.OnSimpleAdminReady -= OnSimpleAdminReady;
    _api.OnPlayerPenaltied -= OnPlayerPenaltied;
    // ... unsubscribe all events
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

### 2. Validate Player State

```csharp
if (!player.IsValid || !player.PawnIsAlive)
{
    return;
}
```

### 3. Check Target Permissions

```csharp
if (!caller.CanTarget(target))
{
    // caller can't target this player (immunity)
    return;
}
```

### 4. Log Commands

```csharp
_api.LogCommand(caller, command);
// or
_api.LogCommand(caller, $"css_mycommand {player.PlayerName}");
```

### 5. Use Per-Player Translations

```csharp
// Each player sees message in their language!
_api.ShowAdminActivityLocalized(
    Localizer,
    "translation_key",
    callerName,
    false,
    args
);
```

### 6. Clean Up Resources

```csharp
public override void Unload(bool hotReload)
{
    // Unregister commands
    // Unregister menus
    // Unsubscribe events
    // Dispose resources
}
```

---

## Common Patterns

### Player Targeting Helper

```csharp
private List<CCSPlayerController> GetTargets(CommandInfo command, CCSPlayerController? caller)
{
    var targets = _api!.GetTarget(command);
    if (targets == null) return new List<CCSPlayerController>();

    return targets.Players
        .Where(p => p.IsValid && !p.IsBot && caller!.CanTarget(p))
        .ToList();
}
```

### Menu Context Pattern (NEW!)

```csharp
// ✅ NEW: Use context to avoid duplication
private object CreateMenu(CCSPlayerController player, MenuContext context)
{
    // context.MenuTitle, context.CategoryId already set!
    return _api!.CreateMenuWithPlayers(context, player, filter, action);
}

// ❌ OLD: Had to repeat title and category
private object CreateMenu(CCSPlayerController player)
{
    return _api!.CreateMenuWithPlayers("My Menu", "mycategory", player, filter, action);
}
```

### Action with Activity Message

```csharp
private void DoAction(CCSPlayerController? caller, CCSPlayerController target)
{
    // Perform action
    // ...

    // Show activity
    if (caller == null || !_api!.IsAdminSilent(caller))
    {
        _api!.ShowAdminActivityLocalized(
            Localizer,
            "action_message",
            caller?.PlayerName,
            false,
            target.PlayerName
        );
    }

    // Log action
    _api!.LogCommand(caller, $"css_action {target.PlayerName}");
}
```

---

## Testing Your Module

### 1. Build

```bash
dotnet build -c Release
```

### 2. Copy to Server

```
game/csgo/addons/counterstrikesharp/plugins/YourModuleName/
```

### 3. Test

- Start server
- Check console for load messages
- Test commands
- Test menus
- Check translations

### 4. Debug

Enable detailed logging:
```csharp
Logger.LogInformation("Debug: ...");
Logger.LogWarning("Warning: ...");
Logger.LogError("Error: ...");
```

---

## Example: Complete Mini-Module

Here's a complete working example:

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CS2_SimpleAdminApi;

namespace ExampleModule;

public class ExampleModule : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Example Module";
    public override string ModuleVersion => "1.0.0";

    private ICS2_SimpleAdminApi? _api;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability = new("simpleadmin:api");

    public Config Config { get; set; } = new();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api == null)
        {
            Logger.LogError("CS2-SimpleAdmin API not found!");
            return;
        }

        // Register command
        if (Config.ExampleCommands.Count > 0)
        {
            foreach (var cmd in Config.ExampleCommands)
            {
                _api.RegisterCommand(cmd, "Example command", OnExampleCommand);
            }
        }
    }

    public void OnConfigParsed(Config config)
    {
        Config = config;
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/generic")]
    private void OnExampleCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var targets = _api!.GetTarget(command);
        if (targets == null) return;

        foreach (var target in targets.Players.Where(p => p.IsValid && caller!.CanTarget(p)))
        {
            // Do something to target
            caller?.PrintToChat($"Performed action on {target.PlayerName}");
        }

        _api.LogCommand(caller, command);
    }

    public override void Unload(bool hotReload)
    {
        if (_api == null) return;

        foreach (var cmd in Config.ExampleCommands)
        {
            _api.UnRegisterCommand(cmd);
        }
    }
}

public class Config : IBasePluginConfig
{
    public int Version { get; set; } = 1;
    public List<string> ExampleCommands { get; set; } = ["css_example"];
}
```

---

## Next Steps

- **[Study Fun Commands Module](https://github.com/daffyyyy/CS2-SimpleAdmin/tree/main/Modules/CS2-SimpleAdmin_FunCommands)** - Complete reference
- **[Read API Documentation](../developer/api/overview)** - Full API reference
- **[Check Examples](../developer/module/examples)** - More code examples

---

## Resources

- **[CS2-SimpleAdmin GitHub](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Source code
- **[CounterStrikeSharp Docs](https://docs.cssharp.dev/)** - CSS documentation
- **[Module Development Guide](../developer/module/getting-started)** - Detailed guide

---

## Need Help?

- **Issues:** [GitHub Issues](https://github.com/daffyyyy/CS2-SimpleAdmin/issues)
- **Discussions:** [GitHub Discussions](https://github.com/daffyyyy/CS2-SimpleAdmin/discussions)
- **Examples:** Study official modules for reference
