---
sidebar_position: 3
---

# Code Examples

Practical examples for common module development scenarios.

## Complete Mini Module

A fully working minimal module:

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CS2_SimpleAdminApi;

namespace HelloModule;

public class HelloModule : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Hello Module";
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
        foreach (var cmd in Config.HelloCommands)
        {
            _api.RegisterCommand(cmd, "Say hello to a player", OnHelloCommand);
        }
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/generic")]
    private void OnHelloCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var targets = _api!.GetTarget(command);
        if (targets == null) return;

        foreach (var player in targets.Players.Where(p => p.IsValid && caller!.CanTarget(p)))
        {
            player.PrintToChat($"Hello {player.PlayerName}!");
            caller?.PrintToChat($"Said hello to {player.PlayerName}");
        }

        _api.LogCommand(caller, command);
    }

    public void OnConfigParsed(Config config) => Config = config;

    public override void Unload(bool hotReload)
    {
        if (_api == null) return;
        foreach (var cmd in Config.HelloCommands)
        {
            _api.UnRegisterCommand(cmd);
        }
    }
}

public class Config : IBasePluginConfig
{
    public int Version { get; set; } = 1;
    public List<string> HelloCommands { get; set; } = ["css_hello"];
}
```

---

## Command Examples

### Simple Target Command

```csharp
[CommandHelper(1, "<#userid or name>")]
[RequiresPermissions("@css/slay")]
private void OnSlayCommand(CCSPlayerController? caller, CommandInfo command)
{
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    foreach (var player in targets.Players.Where(p => p.IsValid && caller!.CanTarget(p)))
    {
        if (player.PawnIsAlive)
        {
            player.PlayerPawn?.Value?.CommitSuicide(false, true);
            caller?.PrintToChat($"Slayed {player.PlayerName}");
        }
    }

    _api.LogCommand(caller, command);
}
```

### Command with Value Parameter

```csharp
[CommandHelper(2, "<#userid or name> <value>")]
[RequiresPermissions("@css/slay")]
private void OnSetHpCommand(CCSPlayerController? caller, CommandInfo command)
{
    // Parse HP value
    if (!int.TryParse(command.GetArg(2), out int hp) || hp < 1 || hp > 999)
    {
        caller?.PrintToChat("Invalid HP! Use 1-999");
        return;
    }

    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    foreach (var player in targets.Players.Where(p => p.IsValid && p.PawnIsAlive && caller!.CanTarget(p)))
    {
        player.PlayerPawn?.Value?.SetHealth(hp);
        caller?.PrintToChat($"Set {player.PlayerName} HP to {hp}");
    }

    _api.LogCommand(caller, $"css_sethp {hp}");
}
```

### Command with Penalty

```csharp
[CommandHelper(1, "<#userid or name> [duration] [reason]")]
[RequiresPermissions("@css/ban")]
private void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
{
    var targets = _api!.GetTarget(command);
    if (targets == null) return;

    // Parse duration (default: 60 minutes)
    int duration = 60;
    if (command.ArgCount > 2)
    {
        int.TryParse(command.GetArg(2), out duration);
    }

    // Get reason (default: "Banned")
    string reason = command.ArgCount > 3
        ? string.Join(" ", command.ArgString.Split(' ').Skip(2))
        : "Banned";

    foreach (var player in targets.Players.Where(p => p.IsValid && caller!.CanTarget(p)))
    {
        _api.IssuePenalty(player, caller, PenaltyType.Ban, reason, duration);
        caller?.PrintToChat($"Banned {player.PlayerName} for {duration} minutes");
    }

    _api.LogCommand(caller, command);
}
```

---

## Menu Examples

### Simple Player Selection Menu

```csharp
private void RegisterMenus()
{
    _api!.RegisterMenuCategory("actions", "Player Actions", "@css/generic");

    _api.RegisterMenu(
        "actions",
        "kick",
        "Kick Player",
        CreateKickMenu,
        "@css/kick"
    );
}

private object CreateKickMenu(CCSPlayerController admin, MenuContext context)
{
    return _api!.CreateMenuWithPlayers(
        context,
        admin,
        player => player.IsValid && admin.CanTarget(player),
        (admin, target) =>
        {
            Server.ExecuteCommand($"css_kick #{target.UserId} Kicked via menu");
            admin.PrintToChat($"Kicked {target.PlayerName}");
        }
    );
}
```

### Nested Menu (Player → Action)

```csharp
private object CreatePlayerActionsMenu(CCSPlayerController admin, MenuContext context)
{
    var menu = _api!.CreateMenuWithBack(context, admin);

    foreach (var player in _api.GetValidPlayers().Where(p => admin.CanTarget(p)))
    {
        _api.AddSubMenu(menu, player.PlayerName, admin =>
        {
            return CreateActionSelectMenu(admin, player);
        });
    }

    return menu;
}

private object CreateActionSelectMenu(CCSPlayerController admin, CCSPlayerController target)
{
    var menu = _api!.CreateMenuWithBack($"Actions: {target.PlayerName}", "actions", admin);

    _api.AddMenuOption(menu, "Slay", _ =>
    {
        if (target.IsValid && target.PawnIsAlive)
        {
            target.PlayerPawn?.Value?.CommitSuicide(false, true);
            admin.PrintToChat($"Slayed {target.PlayerName}");
        }
    });

    _api.AddMenuOption(menu, "Kick", _ =>
    {
        if (target.IsValid)
        {
            Server.ExecuteCommand($"css_kick #{target.UserId}");
        }
    });

    _api.AddMenuOption(menu, "Ban", _ =>
    {
        if (target.IsValid)
        {
            _api.IssuePenalty(target, admin, PenaltyType.Ban, "Banned via menu", 1440);
        }
    });

    return menu;
}
```

### Menu with Value Selection

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
    var menu = _api!.CreateMenuWithBack($"Set HP: {target.PlayerName}", "actions", admin);

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

## Event Examples

### React to Bans

```csharp
public override void OnAllPluginsLoaded(bool hotReload)
{
    _api = _pluginCapability.Get();
    if (_api == null) return;

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
    if (type != PenaltyType.Ban) return;

    var adminName = admin?.PlayerName ?? "Console";
    Logger.LogInformation($"Ban: {adminName} -> {player.PlayerName} ({duration}m): {reason}");

    // Log to file
    File.AppendAllText("bans.log",
        $"[{DateTime.Now}] {player.PlayerName} banned by {adminName} for {duration}m: {reason}\n");
}
```

### Warning Escalation

```csharp
private void OnPlayerPenaltied(
    PlayerInfo player,
    PlayerInfo? admin,
    PenaltyType type,
    string reason,
    int duration,
    int? penaltyId,
    int? serverId)
{
    if (type != PenaltyType.Warn) return;

    Logger.LogInformation($"{player.PlayerName} has {player.Warnings} warnings");

    // Auto-ban at 3 warnings
    if (player.Warnings >= 3)
    {
        var controller = Utilities.GetPlayers()
            .FirstOrDefault(p => p.SteamID == player.SteamId);

        if (controller != null)
        {
            _api!.IssuePenalty(
                controller,
                null,
                PenaltyType.Ban,
                "Automatic: 3 warnings",
                1440  // 1 day
            );
        }
    }
}
```

---

## Translation Examples

### Module with Translations

**lang/en.json:**
```json
{
  "category_name": "My Module",
  "menu_name": "My Action",
  "action_message": "{lightred}{0}{default} performed action on {lightred}{1}{default}!",
  "error_invalid_player": "{red}Error:{default} Invalid player!",
  "success": "{green}Success!{default} Action completed."
}
```

**Code:**
```csharp
private void PerformAction(CCSPlayerController? admin, CCSPlayerController target)
{
    // Perform action
    DoSomething(target);

    // Show activity with translation
    if (admin == null || !_api!.IsAdminSilent(admin))
    {
        if (Localizer != null)
        {
            _api!.ShowAdminActivityLocalized(
                Localizer,
                "action_message",
                admin?.PlayerName,
                false,
                admin?.PlayerName ?? "Console",
                target.PlayerName
            );
        }
    }

    // Send success message
    admin?.PrintToChat(Localizer?["success"] ?? "Success!");
}
```

---

## Utility Examples

### Get Players by Team

```csharp
private List<CCSPlayerController> GetTeamPlayers(CsTeam team)
{
    return _api!.GetValidPlayers()
        .Where(p => p.Team == team)
        .ToList();
}

// Usage
var ctPlayers = GetTeamPlayers(CsTeam.CounterTerrorist);
var tPlayers = GetTeamPlayers(CsTeam.Terrorist);
```

### Get Alive Players

```csharp
private List<CCSPlayerController> GetAlivePlayers()
{
    return _api!.GetValidPlayers()
        .Where(p => p.PawnIsAlive)
        .ToList();
}
```

### Notify Admins

```csharp
private void NotifyAdmins(string message, string permission = "@css/generic")
{
    var admins = _api!.GetValidPlayers()
        .Where(p => AdminManager.PlayerHasPermissions(p, permission));

    foreach (var admin in admins)
    {
        admin.PrintToChat(message);
    }
}

// Usage
NotifyAdmins("⚠ Important admin message", "@css/root");
```

---

## Timer Examples

### Delayed Action

```csharp
private void DelayedAction(CCSPlayerController player, float delay)
{
    AddTimer(delay, () =>
    {
        if (player.IsValid && player.PawnIsAlive)
        {
            DoAction(player);
        }
    });
}
```

### Repeating Timer

```csharp
private void StartRepeatingAction()
{
    AddTimer(1.0f, () =>
    {
        foreach (var player in _api!.GetValidPlayers())
        {
            if (player.PawnIsAlive)
            {
                UpdatePlayer(player);
            }
        }
    }, TimerFlags.REPEAT);
}
```

---

## Configuration Examples

### Multiple Feature Toggles

```csharp
public class Config : IBasePluginConfig
{
    public int Version { get; set; } = 1;

    [JsonPropertyName("EnableFeature1")]
    public bool EnableFeature1 { get; set; } = true;

    [JsonPropertyName("EnableFeature2")]
    public bool EnableFeature2 { get; set; } = false;

    [JsonPropertyName("Feature1Commands")]
    public List<string> Feature1Commands { get; set; } = ["css_feature1"];

    [JsonPropertyName("Feature2Commands")]
    public List<string> Feature2Commands { get; set; } = ["css_feature2"];

    [JsonPropertyName("MaxValue")]
    public int MaxValue { get; set; } = 100;
}

// Usage
private void RegisterCommands()
{
    if (Config.EnableFeature1)
    {
        foreach (var cmd in Config.Feature1Commands)
        {
            _api!.RegisterCommand(cmd, "Feature 1", OnFeature1Command);
        }
    }

    if (Config.EnableFeature2)
    {
        foreach (var cmd in Config.Feature2Commands)
        {
            _api!.RegisterCommand(cmd, "Feature 2", OnFeature2Command);
        }
    }
}
```

---

## Next Steps

- **[Best Practices](best-practices)** - Write better code
- **[Getting Started](getting-started)** - Create your first module
- **[API Reference](../api/overview)** - Full API documentation
- **[Fun Commands Source](https://github.com/daffyyyy/CS2-SimpleAdmin/tree/main/Modules/CS2-SimpleAdmin_FunCommands)** - Complete reference implementation
