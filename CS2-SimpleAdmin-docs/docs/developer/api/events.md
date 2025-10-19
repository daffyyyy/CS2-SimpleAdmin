---
sidebar_position: 5
---

# Events API

Complete reference for CS2-SimpleAdmin event system.

## Event System Overview

CS2-SimpleAdmin exposes events that allow modules to react to plugin actions and state changes.

**All events use C# event delegates and should be subscribed to in `OnAllPluginsLoaded` and unsubscribed in `Unload`.**

---

## Plugin Lifecycle Events

### OnSimpleAdminReady

Fired when CS2-SimpleAdmin is fully initialized and ready.

```csharp
event Action? OnSimpleAdminReady
```

**When Fired:**
- After plugin load
- After database initialization
- After migrations complete
- Menu system ready

**Example:**
```csharp
public override void OnAllPluginsLoaded(bool hotReload)
{
    _api = _pluginCapability.Get();
    if (_api == null) return;

    // Subscribe to ready event
    _api.OnSimpleAdminReady += OnSimpleAdminReady;

    // Also call directly for hot reload case
    OnSimpleAdminReady();
}

private void OnSimpleAdminReady()
{
    Logger.LogInformation("SimpleAdmin is ready!");

    // Register menus (requires SimpleAdmin to be ready)
    RegisterMenus();
}

public override void Unload(bool hotReload)
{
    if (_api == null) return;
    _api.OnSimpleAdminReady -= OnSimpleAdminReady;
}
```

**Best Practice:**
Always call your handler directly after subscribing to handle hot reload:
```csharp
_api.OnSimpleAdminReady += RegisterMenus;
RegisterMenus();  // ← Also call directly
```

---

## Penalty Events

### OnPlayerPenaltied

Fired when an **online player** receives a penalty.

```csharp
event Action<PlayerInfo, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltied
```

**Parameters:**
1. `PlayerInfo player` - Player who received penalty
2. `PlayerInfo? admin` - Admin who issued penalty (null if console)
3. `PenaltyType penaltyType` - Type of penalty
4. `string reason` - Penalty reason
5. `int duration` - Duration in minutes (0 = permanent)
6. `int? penaltyId` - Database penalty ID (null if not stored)
7. `int? serverId` - Server ID (null in single-server mode)

**Example:**
```csharp
_api.OnPlayerPenaltied += OnPlayerPenaltied;

private void OnPlayerPenaltied(
    PlayerInfo player,
    PlayerInfo? admin,
    PenaltyType type,
    string reason,
    int duration,
    int? penaltyId,
    int? serverId)
{
    var adminName = admin?.PlayerName ?? "Console";
    Logger.LogInformation($"{adminName} penaltied {player.PlayerName}: {type} ({duration}m) - {reason}");

    // React to specific penalty types
    switch (type)
    {
        case PenaltyType.Ban:
            // Log ban to external system
            LogBanToWebhook(player, admin, reason, duration);
            break;

        case PenaltyType.Warn:
            // Check warning count
            if (player.Warnings >= 3)
            {
                Logger.LogWarning($"{player.PlayerName} has {player.Warnings} warnings!");
            }
            break;
    }
}
```

---

### OnPlayerPenaltiedAdded

Fired when a penalty is added to an **offline player** by SteamID.

```csharp
event Action<SteamID, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltiedAdded
```

**Parameters:**
1. `SteamID steamId` - Target player's SteamID
2. `PlayerInfo? admin` - Admin who issued penalty
3. `PenaltyType penaltyType` - Type of penalty
4. `string reason` - Penalty reason
5. `int duration` - Duration in minutes
6. `int? penaltyId` - Database penalty ID
7. `int? serverId` - Server ID

**Example:**
```csharp
_api.OnPlayerPenaltiedAdded += OnPlayerPenaltiedAdded;

private void OnPlayerPenaltiedAdded(
    SteamID steamId,
    PlayerInfo? admin,
    PenaltyType type,
    string reason,
    int duration,
    int? penaltyId,
    int? serverId)
{
    var adminName = admin?.PlayerName ?? "Console";
    Logger.LogInformation($"Offline penalty: {adminName} -> SteamID {steamId}: {type} ({duration}m)");

    // Log to external database or webhook
    if (type == PenaltyType.Ban)
    {
        LogOfflineBan(steamId, admin, reason, duration);
    }
}
```

---

## Admin Activity Events

### OnAdminShowActivity

Fired when an admin action is displayed to players.

```csharp
event Action<string, string?, bool, object>? OnAdminShowActivity
```

**Parameters:**
1. `string messageKey` - Translation key for the message
2. `string? callerName` - Admin name (null if console)
3. `bool dontPublish` - If true, don't broadcast to other systems
4. `object messageArgs` - Arguments for message formatting

**Example:**
```csharp
_api.OnAdminShowActivity += OnAdminShowActivity;

private void OnAdminShowActivity(
    string messageKey,
    string? callerName,
    bool dontPublish,
    object messageArgs)
{
    if (dontPublish) return;

    Logger.LogInformation($"Admin activity: {messageKey} by {callerName ?? "Console"}");

    // Log to Discord, database, etc.
    LogAdminAction(messageKey, callerName, messageArgs);
}
```

---

### OnAdminToggleSilent

Fired when an admin toggles silent mode.

```csharp
event Action<int, bool>? OnAdminToggleSilent
```

**Parameters:**
1. `int slot` - Player slot of admin
2. `bool status` - New silent status (true = silent, false = normal)

**Example:**
```csharp
_api.OnAdminToggleSilent += OnAdminToggleSilent;

private void OnAdminToggleSilent(int slot, bool status)
{
    var player = Utilities.GetPlayerFromSlot(slot);
    if (player == null) return;

    var statusText = status ? "enabled" : "disabled";
    Logger.LogInformation($"{player.PlayerName} {statusText} silent mode");

    // Update UI or external systems
    UpdateAdminStatus(player, status);
}
```

---

## Complete Examples

### Ban Logging System

```csharp
public class BanLogger
{
    private ICS2_SimpleAdminApi? _api;

    public void Initialize(ICS2_SimpleAdminApi api)
    {
        _api = api;

        // Subscribe to both ban events
        _api.OnPlayerPenaltied += OnPlayerPenaltied;
        _api.OnPlayerPenaltiedAdded += OnPlayerPenaltiedAdded;
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

        // Log to file
        File.AppendAllText("bans.log",
            $"[{DateTime.Now}] {player.PlayerName} ({player.SteamId}) " +
            $"banned by {admin?.PlayerName ?? "Console"} " +
            $"for {duration} minutes: {reason}\n");

        // Send to Discord webhook
        SendDiscordNotification(
            $"🔨 **Ban Issued**\n" +
            $"Player: {player.PlayerName}\n" +
            $"Admin: {admin?.PlayerName ?? "Console"}\n" +
            $"Duration: {FormatDuration(duration)}\n" +
            $"Reason: {reason}"
        );
    }

    private void OnPlayerPenaltiedAdded(
        SteamID steamId,
        PlayerInfo? admin,
        PenaltyType type,
        string reason,
        int duration,
        int? penaltyId,
        int? serverId)
    {
        if (type != PenaltyType.Ban) return;

        File.AppendAllText("bans.log",
            $"[{DateTime.Now}] Offline ban: SteamID {steamId} " +
            $"by {admin?.PlayerName ?? "Console"} " +
            $"for {duration} minutes: {reason}\n");
    }

    private string FormatDuration(int minutes)
    {
        if (minutes == 0) return "Permanent";
        if (minutes < 60) return $"{minutes} minutes";
        if (minutes < 1440) return $"{minutes / 60} hours";
        return $"{minutes / 1440} days";
    }
}
```

---

### Warning Escalation System

```csharp
public class WarningEscalation
{
    private ICS2_SimpleAdminApi? _api;

    public void Initialize(ICS2_SimpleAdminApi api)
    {
        _api = api;
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
        // Only handle warnings
        if (type != PenaltyType.Warn) return;

        Logger.LogInformation($"{player.PlayerName} now has {player.Warnings} warnings");

        // Auto-escalate based on warning count
        if (player.Warnings >= 3)
        {
            // 3 warnings = 1 hour ban
            _api.IssuePenalty(
                GetPlayerController(player.SteamId),
                null,
                PenaltyType.Ban,
                "Automatic: 3 warnings",
                60
            );
        }
        else if (player.Warnings >= 5)
        {
            // 5 warnings = 1 day ban
            _api.IssuePenalty(
                GetPlayerController(player.SteamId),
                null,
                PenaltyType.Ban,
                "Automatic: 5 warnings",
                1440
            );
        }
    }
}
```

---

### Admin Activity Monitor

```csharp
public class AdminMonitor
{
    private readonly Dictionary<string, int> _adminActions = new();

    public void Initialize(ICS2_SimpleAdminApi api)
    {
        api.OnAdminShowActivity += OnAdminShowActivity;
    }

    private void OnAdminShowActivity(
        string messageKey,
        string? callerName,
        bool dontPublish,
        object messageArgs)
    {
        if (callerName == null) return;  // Ignore console actions

        // Track admin actions
        if (!_adminActions.ContainsKey(callerName))
        {
            _adminActions[callerName] = 0;
        }

        _adminActions[callerName]++;

        // Log every 10th action
        if (_adminActions[callerName] % 10 == 0)
        {
            Logger.LogInformation(
                $"{callerName} has performed {_adminActions[callerName]} admin actions"
            );
        }

        // Alert if admin is very active
        if (_adminActions[callerName] > 100)
        {
            Logger.LogWarning($"{callerName} has performed many actions ({_adminActions[callerName]})");
        }
    }
}
```

---

## Best Practices

### 1. Always Unsubscribe

```csharp
public override void OnAllPluginsLoaded(bool hotReload)
{
    _api = _pluginCapability.Get();
    if (_api == null) return;

    // Subscribe
    _api.OnPlayerPenaltied += OnPlayerPenaltied;
    _api.OnAdminShowActivity += OnAdminShowActivity;
}

public override void Unload(bool hotReload)
{
    if (_api == null) return;

    // ALWAYS unsubscribe
    _api.OnPlayerPenaltied -= OnPlayerPenaltied;
    _api.OnAdminShowActivity -= OnAdminShowActivity;
}
```

### 2. Handle Null Admins

```csharp
private void OnPlayerPenaltied(
    PlayerInfo player,
    PlayerInfo? admin,  // ← Can be null!
    PenaltyType type,
    string reason,
    int duration,
    int? penaltyId,
    int? serverId)
{
    var adminName = admin?.PlayerName ?? "Console";
    // Use adminName safely
}
```

### 3. Use Events for Integration

```csharp
// ✅ Good - React to penalties
_api.OnPlayerPenaltied += (player, admin, type, reason, duration, id, sid) =>
{
    if (type == PenaltyType.Ban)
    {
        NotifyExternalSystem(player, reason);
    }
};

// ❌ Bad - Wrapping penalty methods
// Don't wrap IssuePenalty, use events instead
```

### 4. Check Event Parameters

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
    // Check nullable parameters
    if (penaltyId.HasValue)
    {
        Logger.LogInformation($"Penalty ID: {penaltyId.Value}");
    }

    if (serverId.HasValue)
    {
        Logger.LogInformation($"Server ID: {serverId.Value}");
    }
}
```

### 5. OnSimpleAdminReady Pattern

```csharp
// ✅ Good - Handles both normal load and hot reload
_api.OnSimpleAdminReady += RegisterMenus;
RegisterMenus();

// ❌ Bad - Only works on normal load
_api.OnSimpleAdminReady += RegisterMenus;
```

---

## Common Patterns

### Event-Based Statistics

```csharp
public class ServerStatistics
{
    private int _totalBans;
    private int _totalMutes;
    private int _totalWarnings;

    public void Initialize(ICS2_SimpleAdminApi api)
    {
        api.OnPlayerPenaltied += (player, admin, type, reason, duration, id, sid) =>
        {
            switch (type)
            {
                case PenaltyType.Ban:
                    _totalBans++;
                    break;
                case PenaltyType.Mute:
                case PenaltyType.Gag:
                case PenaltyType.Silence:
                    _totalMutes++;
                    break;
                case PenaltyType.Warn:
                    _totalWarnings++;
                    break;
            }
        };
    }

    public void PrintStatistics()
    {
        Logger.LogInformation($"Server Statistics:");
        Logger.LogInformation($"Total Bans: {_totalBans}");
        Logger.LogInformation($"Total Mutes: {_totalMutes}");
        Logger.LogInformation($"Total Warnings: {_totalWarnings}");
    }
}
```

### Conditional Event Handling

```csharp
_api.OnPlayerPenaltied += (player, admin, type, reason, duration, id, sid) =>
{
    // Only handle bans
    if (type != PenaltyType.Ban) return;

    // Only handle permanent bans
    if (duration != 0) return;

    // Only handle admin-issued bans
    if (admin == null) return;

    // Process permanent admin bans
    NotifyImportantBan(player, admin, reason);
};
```

---

## Performance Considerations

### Async Operations in Events

```csharp
// ⚠️ Be careful with async in event handlers
_api.OnPlayerPenaltied += async (player, admin, type, reason, duration, id, sid) =>
{
    // Don't block the game thread
    await Task.Run(() =>
    {
        // Long-running operation
        LogToExternalDatabase(player, type, reason);
    });
};
```

### Efficient Event Handlers

```csharp
// ✅ Good - Quick processing
_api.OnPlayerPenaltied += (player, admin, type, reason, duration, id, sid) =>
{
    // Quick logging
    Logger.LogInformation($"Ban: {player.PlayerName}");
};

// ❌ Bad - Heavy processing
_api.OnPlayerPenaltied += (player, admin, type, reason, duration, id, sid) =>
{
    // Don't do expensive operations synchronously
    SendEmailNotification(player);  // ← This blocks the game thread!
};
```

---

## Troubleshooting

### Event Not Firing

**Check:**
1. Did you subscribe to the event?
2. Is `_api` not null?
3. Are you testing the right scenario?
4. Check server console for errors

### Memory Leaks

**Always unsubscribe:**
```csharp
public override void Unload(bool hotReload)
{
    if (_api == null) return;

    // Unsubscribe ALL events
    _api.OnSimpleAdminReady -= OnReady;
    _api.OnPlayerPenaltied -= OnPlayerPenaltied;
    // ... etc
}
```

---

## Related APIs

- **[Penalties API](penalties)** - Issue penalties that trigger events
- **[Commands API](commands)** - Commands that may trigger admin activity
- **[Utilities API](utilities)** - Helper functions for event handlers
