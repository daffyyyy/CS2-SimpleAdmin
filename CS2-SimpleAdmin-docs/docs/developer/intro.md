---
sidebar_position: 1
---

# Developer Introduction

Welcome to the CS2-SimpleAdmin developer documentation!

## Overview

This section contains technical documentation for developers who want to:

- Create modules using the CS2-SimpleAdmin API
- Contribute to the core plugin
- Integrate with CS2-SimpleAdmin from other plugins
- Understand the plugin architecture

---

## API Documentation

The CS2-SimpleAdmin API provides a rich set of features for module developers:

### Core Features

- **[Commands](api/commands)** - Register and manage commands
- **[Menus](api/menus)** - Create admin menus with player selection
- **[Penalties](api/penalties)** - Issue bans, mutes, gags, warnings
- **[Events](api/events)** - Subscribe to plugin events
- **[Utilities](api/utilities)** - Helper functions and player management

---

## Quick Links

### For Module Developers

- **[Module Development Guide](module/getting-started)** - Start creating modules
- **[Best Practices](module/best-practices)** - Write better code
- **[Examples](module/examples)** - Code examples and patterns

### For Core Contributors

- **[Architecture](architecture)** - Plugin structure and design
- **[GitHub Repository](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Source code

---

## Getting Started

### Prerequisites

- C# knowledge (intermediate level)
- .NET 8.0 SDK
- CounterStrikeSharp understanding
- CS2 dedicated server for testing

### Development Environment

**Recommended:**
- Visual Studio 2022 (Community or higher)
- VS Code with C# extension
- Git for version control

---

## CS2-SimpleAdminApi Interface

The main API interface provides all functionality:

```csharp
using CounterStrikeSharp.API.Core.Capabilities;
using CS2_SimpleAdminApi;

// Get the API
private ICS2_SimpleAdminApi? _api;
private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability =
    new("simpleadmin:api");

public override void OnAllPluginsLoaded(bool hotReload)
{
    _api = _pluginCapability.Get();

    if (_api == null)
    {
        Logger.LogError("CS2-SimpleAdmin API not found!");
        return;
    }

    // Use the API
    _api.RegisterCommand("css_mycommand", "Description", OnMyCommand);
}
```

---

## API Categories

### Command Management

Register custom commands that integrate with CS2-SimpleAdmin:

```csharp
_api.RegisterCommand("css_mycommand", "Description", callback);
_api.UnRegisterCommand("css_mycommand");
_api.GetTarget(command);  // Parse player targets
```

**[Learn more →](api/commands)**

---

### Menu System

Create interactive menus with automatic back button handling:

```csharp
// Register category
_api.RegisterMenuCategory("mycategory", "My Category", "@css/generic");

// Register menu
_api.RegisterMenu("mycategory", "mymenu", "My Menu", CreateMenu, "@css/generic");

// Create menu with players
_api.CreateMenuWithPlayers(context, admin, filter, onSelect);
```

**[Learn more →](api/menus)**

---

### Penalty System

Issue and manage player penalties:

```csharp
// Ban player
_api.IssuePenalty(player, admin, PenaltyType.Ban, "Reason", 1440);

// Offline ban
_api.IssuePenalty(steamId, admin, PenaltyType.Ban, "Reason", 0);

// Check penalties
var status = _api.GetPlayerMuteStatus(player);
```

**[Learn more →](api/penalties)**

---

### Event System

React to plugin events:

```csharp
_api.OnSimpleAdminReady += () => { /* Plugin ready */ };
_api.OnPlayerPenaltied += (player, admin, type, reason, duration, id, serverId) =>
{
    // Player received penalty
};
```

**[Learn more →](api/events)**

---

### Utility Functions

Helper functions for common tasks:

```csharp
// Get player info
var playerInfo = _api.GetPlayerInfo(player);

// Get valid players
var players = _api.GetValidPlayers();

// Check admin status
if (_api.IsAdminSilent(admin)) { /* ... */ }

// Show admin activity
_api.ShowAdminActivity("message_key", callerName, false, args);
```

**[Learn more →](api/utilities)**

---

## Code Examples

### Simple Command

```csharp
[CommandHelper(1, "<#userid or name>")]
[RequiresPermissions("@css/generic")]
private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
{
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    foreach (var target in targets.Players.Where(p => p.IsValid && caller!.CanTarget(p)))
    {
        // Do something with target
    }

    _api.LogCommand(caller, command);
}
```

### Simple Menu

```csharp
private object CreateMyMenu(CCSPlayerController admin, MenuContext context)
{
    return _api!.CreateMenuWithPlayers(
        context,
        admin,
        player => player.IsValid && admin.CanTarget(player),
        (admin, target) => DoAction(admin, target)
    );
}
```

---

## Best Practices

### Error Handling

```csharp
if (_api == null)
{
    Logger.LogError("API not available!");
    return;
}

if (!player.IsValid || !player.PawnIsAlive)
{
    return;
}
```

### Resource Cleanup

```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    // Unregister commands
    _api.UnRegisterCommand("css_mycommand");

    // Unregister menus
    _api.UnregisterMenu("mycategory", "mymenu");

    // Unsubscribe events
    _api.OnSimpleAdminReady -= OnReady;
}
```

### Translations

```csharp
// Use per-player language support
_api.ShowAdminActivityLocalized(
    Localizer,
    "translation_key",
    caller?.PlayerName,
    false,
    args
);
```

---

## Reference Implementation

The **Fun Commands Module** serves as a complete reference implementation demonstrating all API features:

- Command registration from config
- Menu creation with context
- Per-player translations
- Proper cleanup
- Code organization

**[View Source Code](https://github.com/daffyyyy/CS2-SimpleAdmin/tree/main/Modules/CS2-SimpleAdmin_FunCommands)**

---

## Architecture Overview

CS2-SimpleAdmin follows a layered architecture:

**Layers:**
1. **CounterStrikeSharp Integration** - Game event handling
2. **Manager Layer** - Business logic (Bans, Mutes, Permissions)
3. **Database Layer** - MySQL/SQLite with migrations
4. **Menu System** - MenuManager with factory pattern
5. **Command System** - Dynamic registration
6. **Public API** - ICS2_SimpleAdminApi interface

**[Learn more →](architecture)**

---

## Contributing

### Ways to Contribute

1. **Report Bugs** - [GitHub Issues](https://github.com/daffyyyy/CS2-SimpleAdmin/issues)
2. **Suggest Features** - [GitHub Discussions](https://github.com/daffyyyy/CS2-SimpleAdmin/discussions)
3. **Submit Pull Requests** - Code contributions
4. **Create Modules** - Extend functionality
5. **Improve Documentation** - Help others learn

### Development Workflow

1. Fork the repository
2. Create feature branch
3. Make changes
4. Test thoroughly
5. Submit pull request

---

## Resources

### Documentation

- **[API Reference](api/overview)** - Complete API documentation
- **[Module Development](module/getting-started)** - Create modules
- **[Architecture](architecture)** - Plugin design

### External Resources

- **[CounterStrikeSharp Docs](https://docs.cssharp.dev/)** - CSS framework
- **[CS2 Docs](https://developer.valvesoftware.com/wiki/Counter-Strike_2)** - Game documentation

### Community

- **[GitHub](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Source code
- **[Issues](https://github.com/daffyyyy/CS2-SimpleAdmin/issues)** - Bug reports
- **[Discussions](https://github.com/daffyyyy/CS2-SimpleAdmin/discussions)** - Questions and ideas

---

## Support

### Getting Help

1. **Check Documentation** - Most questions answered here
2. **Search Issues** - Someone may have had same problem
3. **Ask in Discussions** - Community help
4. **Create Issue** - For bugs or feature requests

### Reporting Bugs

Include:
- CS2-SimpleAdmin version
- CounterStrikeSharp version
- Error messages
- Steps to reproduce
- Expected vs actual behavior

---

## Next Steps

### For New Developers

1. **[Read API Overview](api/overview)** - Understand available features
2. **[Study Examples](module/examples)** - Learn from code
3. **[Create First Module](module/getting-started)** - Get hands-on

### For Advanced Developers

1. **[Read Architecture](architecture)** - Deep dive into structure
2. **[Review Source Code](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Understand implementation
3. **[Contribute](https://github.com/daffyyyy/CS2-SimpleAdmin/pulls)** - Help improve the plugin
