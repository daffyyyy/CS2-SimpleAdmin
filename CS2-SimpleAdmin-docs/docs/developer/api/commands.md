---
sidebar_position: 2
---

# Commands API

Complete reference for command registration and management.

## Command Registration

### RegisterCommand

Register a new command that integrates with CS2-SimpleAdmin.

```csharp
void RegisterCommand(string name, string? description, CommandInfo.CommandCallback callback)
```

**Parameters:**
- `name` - Command name (e.g., "css_mycommand")
- `description` - Command description (optional)
- `callback` - Method to call when command is executed

**Example:**
```csharp
_api.RegisterCommand("css_mycommand", "My custom command", OnMyCommand);

private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Command logic here
}
```

**Throws:**
- `ArgumentException` - If command name is null or empty
- `ArgumentNullException` - If callback is null

---

### UnRegisterCommand

Unregister a previously registered command.

```csharp
void UnRegisterCommand(string commandName)
```

**Parameters:**
- `commandName` - Name of command to unregister

**Example:**
```csharp
_api.UnRegisterCommand("css_mycommand");
```

**Best Practice:**
Always unregister commands in your plugin's `Unload()` method:

```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    _api.UnRegisterCommand("css_mycommand");
}
```

---

## Target Parsing

### GetTarget

Parse player targets from command arguments.

```csharp
TargetResult? GetTarget(CommandInfo command)
```

**Parameters:**
- `command` - Command info containing arguments

**Returns:**
- `TargetResult` - Contains matched players
- `null` - If no targets found or error

**Example:**
```csharp
[CommandHelper(1, "<#userid or name>")]
private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
{
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    foreach (var player in targets.Players)
    {
        // Do something with player
    }
}
```

**Supported Target Syntax:**
- `@all` - All players
- `@ct` - All Counter-Terrorists
- `@t` - All Terrorists
- `@spec` - All spectators
- `@alive` - All alive players
- `@dead` - All dead players
- `@bot` - All bots
- `@human` - All human players
- `@me` - Command caller
- `#123` - Player by user ID
- `PlayerName` - Player by name (partial match)

---

## Command Logging

### LogCommand (with CommandInfo)

Log a command execution with full command info.

```csharp
void LogCommand(CCSPlayerController? caller, CommandInfo command)
```

**Parameters:**
- `caller` - Player who executed command (null for console)
- `command` - Command info object

**Example:**
```csharp
private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Execute command logic

    // Log the command
    _api!.LogCommand(caller, command);
}
```

---

### LogCommand (with string)

Log a command execution with custom command string.

```csharp
void LogCommand(CCSPlayerController? caller, string command)
```

**Parameters:**
- `caller` - Player who executed command (null for console)
- `command` - Command string to log

**Example:**
```csharp
private void DoAction(CCSPlayerController? caller, CCSPlayerController target)
{
    // Perform action

    // Log with custom string
    _api!.LogCommand(caller, $"css_mycommand {target.PlayerName}");
}
```

---

## Complete Example

### Basic Command

```csharp
private void RegisterCommands()
{
    _api!.RegisterCommand("css_hello", "Say hello to a player", OnHelloCommand);
}

[CommandHelper(1, "<#userid or name>")]
[RequiresPermissions("@css/generic")]
private void OnHelloCommand(CCSPlayerController? caller, CommandInfo command)
{
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    foreach (var player in targets.Players.Where(p => p.IsValid))
    {
        player.PrintToChat($"Hello {player.PlayerName}!");
    }

    _api.LogCommand(caller, command);
}

public override void Unload(bool hotReload)
{
    _api?.UnRegisterCommand("css_hello");
}
```

---

### Command with Permission Check

```csharp
[CommandHelper(1, "<#userid or name>")]
[RequiresPermissions("@css/ban")]
private void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Get targets
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    // Filter for players caller can target
    var validPlayers = targets.Players
        .Where(p => p.IsValid && !p.IsBot && caller!.CanTarget(p))
        .ToList();

    foreach (var player in validPlayers)
    {
        // Issue ban
        _api.IssuePenalty(player, caller, PenaltyType.Ban, "Banned via command", 1440);
    }

    _api.LogCommand(caller, command);
}
```

---

### Command with Arguments

```csharp
[CommandHelper(2, "<#userid or name> <value>")]
[RequiresPermissions("@css/slay")]
private void OnSetHpCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Parse HP value
    if (!int.TryParse(command.GetArg(2), out int hp))
    {
        caller?.PrintToChat("Invalid HP value!");
        return;
    }

    // Get targets
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    foreach (var player in targets.Players.Where(p => p.IsValid && p.PawnIsAlive))
    {
        player.PlayerPawn?.Value?.SetHealth(hp);
    }

    _api.LogCommand(caller, $"css_sethp {hp}");
}
```

---

## Best Practices

### 1. Always Validate Targets

```csharp
var targets = _api!.GetTarget(command);
if (targets == null) return;  // No targets found

// Filter for valid players
var validPlayers = targets.Players
    .Where(p => p.IsValid && !p.IsBot)
    .ToList();

if (validPlayers.Count == 0)
{
    caller?.PrintToChat("No valid targets found!");
    return;
}
```

### 2. Check Immunity

```csharp
foreach (var player in targets.Players)
{
    // Check if caller can target this player (immunity check)
    if (!caller!.CanTarget(player))
    {
        caller.PrintToChat($"You cannot target {player.PlayerName}!");
        continue;
    }

    // Safe to target player
    DoAction(player);
}
```

### 3. Always Log Commands

```csharp
// Log every admin command execution
_api.LogCommand(caller, command);
```

### 4. Use CommandHelper Attribute

```csharp
// Specify minimum args and usage
[CommandHelper(minArgs: 1, usage: "<#userid or name>")]
[RequiresPermissions("@css/generic")]
private void OnCommand(CCSPlayerController? caller, CommandInfo command)
{
    // CounterStrikeSharp validates args automatically
}
```

### 5. Cleanup on Unload

```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    // Unregister ALL commands
    _api.UnRegisterCommand("css_command1");
    _api.UnRegisterCommand("css_command2");
}
```

---

## Common Patterns

### Multiple Aliases

```csharp
// Register same command with multiple aliases
var aliases = new[] { "css_mycommand", "css_mycmd", "css_mc" };

foreach (var alias in aliases)
{
    _api.RegisterCommand(alias, "My command", OnMyCommand);
}

// Unregister all
public override void Unload(bool hotReload)
{
    foreach (var alias in aliases)
    {
        _api?.UnRegisterCommand(alias);
    }
}
```

### Command from Config

```csharp
// In Config.cs
public List<string> MyCommands { get; set; } = ["css_mycommand"];

// In Plugin
private void RegisterCommands()
{
    foreach (var cmd in Config.MyCommands)
    {
        _api!.RegisterCommand(cmd, "Description", OnMyCommand);
    }
}

// Allows users to add aliases or disable by clearing list
```

### Target Filtering

```csharp
// Get only alive players
var alivePlayers = targets.Players
    .Where(p => p.IsValid && p.PawnIsAlive)
    .ToList();

// Get only enemy team
var enemies = targets.Players
    .Where(p => p.IsValid && p.Team != caller!.Team)
    .ToList();

// Get targetable players
var targetable = targets.Players
    .Where(p => p.IsValid && caller!.CanTarget(p))
    .ToList();
```

---

## Error Handling

### Command Registration Errors

```csharp
try
{
    _api.RegisterCommand("css_mycommand", "Description", OnMyCommand);
}
catch (ArgumentException ex)
{
    Logger.LogError($"Failed to register command: {ex.Message}");
}
```

### Target Parsing Errors

```csharp
var targets = _api!.GetTarget(command);

if (targets == null)
{
    // Target parsing failed
    // Error message already sent to caller by SimpleAdmin
    return;
}

if (targets.Players.Count == 0)
{
    caller?.PrintToChat("No players matched your target!");
    return;
}
```

---

## Performance Tips

### Cache Command Lists

```csharp
// Don't create new list every time
private readonly List<string> _commandAliases = new() { "css_cmd1", "css_cmd2" };

private void RegisterCommands()
{
    foreach (var cmd in _commandAliases)
    {
        _api!.RegisterCommand(cmd, "Description", OnCommand);
    }
}
```

### Efficient Target Filtering

```csharp
// ✅ Good - single LINQ query
var players = targets.Players
    .Where(p => p.IsValid && !p.IsBot && p.PawnIsAlive && caller!.CanTarget(p))
    .ToList();

// ❌ Bad - multiple iterations
var players = targets.Players.Where(p => p.IsValid).ToList();
players = players.Where(p => !p.IsBot).ToList();
players = players.Where(p => p.PawnIsAlive).ToList();
```

---

## Related APIs

- **[Menus API](menus)** - Create interactive menus
- **[Penalties API](penalties)** - Issue penalties from commands
- **[Utilities API](utilities)** - Helper functions for commands
