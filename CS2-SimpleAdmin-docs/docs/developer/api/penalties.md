---
sidebar_position: 4
---

# Penalties API

Complete reference for issuing and managing player penalties.

## Penalty Types

```csharp
public enum PenaltyType
{
    Ban,      // Ban player from server
    Kick,     // Kick player from server
    Gag,      // Block text chat
    Mute,     // Block voice chat
    Silence,  // Block both text and voice
    Warn      // Issue warning
}
```

---

## Issue Penalties

### IssuePenalty (Online Player)

Issue a penalty to a currently connected player.

```csharp
void IssuePenalty(
    CCSPlayerController player,
    CCSPlayerController? admin,
    PenaltyType penaltyType,
    string reason,
    int duration = -1
)
```

**Parameters:**
- `player` - Target player controller
- `admin` - Admin issuing penalty (null for console)
- `penaltyType` - Type of penalty
- `reason` - Reason for penalty
- `duration` - Duration in minutes (0 = permanent, -1 = default)

**Example:**
```csharp
// Ban player for 1 day
_api!.IssuePenalty(
    player,
    admin,
    PenaltyType.Ban,
    "Cheating",
    1440  // 24 hours in minutes
);

// Permanent ban
_api.IssuePenalty(
    player,
    admin,
    PenaltyType.Ban,
    "Severe rule violation",
    0
);

// Kick player
_api.IssuePenalty(
    player,
    admin,
    PenaltyType.Kick,
    "AFK"
);

// Gag for 30 minutes
_api.IssuePenalty(
    player,
    admin,
    PenaltyType.Gag,
    "Chat spam",
    30
);
```

---

### IssuePenalty (Offline Player)

Issue a penalty to a player by SteamID (even if offline).

```csharp
void IssuePenalty(
    SteamID steamid,
    CCSPlayerController? admin,
    PenaltyType penaltyType,
    string reason,
    int duration = -1
)
```

**Parameters:**
- `steamid` - Target player's SteamID
- `admin` - Admin issuing penalty (null for console)
- `penaltyType` - Type of penalty
- `reason` - Reason for penalty
- `duration` - Duration in minutes (0 = permanent, -1 = default)

**Example:**
```csharp
// Ban offline player
var steamId = new SteamID(76561198012345678);

_api!.IssuePenalty(
    steamId,
    admin,
    PenaltyType.Ban,
    "Ban evasion",
    10080  // 7 days
);

// Mute offline player
_api.IssuePenalty(
    steamId,
    admin,
    PenaltyType.Mute,
    "Voice abuse",
    1440
);
```

**Supported SteamID Formats:**
```csharp
// SteamID64
new SteamID(76561198012345678)

// Also works with SteamID string parsing
SteamID.FromString("STEAM_1:0:12345678")
SteamID.FromString("[U:1:12345678]")
```

---

## Get Player Information

### GetPlayerInfo

Get detailed player information including penalty counts.

```csharp
PlayerInfo GetPlayerInfo(CCSPlayerController player)
```

**Returns:** `PlayerInfo` object containing:
- `PlayerName` - Player's name
- `SteamId` - Steam ID (ulong)
- `IpAddress` - Player's IP address
- `Warnings` - Warning count
- `Bans` - Ban count
- `Mutes` - Mute count
- `Gags` - Gag count

**Example:**
```csharp
var playerInfo = _api!.GetPlayerInfo(player);

Console.WriteLine($"Player: {playerInfo.PlayerName}");
Console.WriteLine($"SteamID: {playerInfo.SteamId}");
Console.WriteLine($"Warnings: {playerInfo.Warnings}");
Console.WriteLine($"Total Bans: {playerInfo.Bans}");

// Check if player has penalties
if (playerInfo.Warnings >= 3)
{
    _api.IssuePenalty(player, null, PenaltyType.Ban, "Too many warnings", 1440);
}
```

**Throws:**
- `KeyNotFoundException` - If player doesn't have a valid UserId

---

### GetPlayerMuteStatus

Get current mute/gag/silence status for a player.

```csharp
Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration, bool Passed)>> GetPlayerMuteStatus(
    CCSPlayerController player
)
```

**Returns:** Dictionary mapping penalty types to lists of active penalties

**Example:**
```csharp
var muteStatus = _api!.GetPlayerMuteStatus(player);

// Check if player is gagged
if (muteStatus.ContainsKey(PenaltyType.Gag))
{
    var gagPenalties = muteStatus[PenaltyType.Gag];

    foreach (var (endTime, duration, passed) in gagPenalties)
    {
        if (!passed)
        {
            var remaining = endTime - DateTime.UtcNow;
            Console.WriteLine($"Gagged for {remaining.TotalMinutes:F0} more minutes");
        }
    }
}

// Check if player is muted
if (muteStatus.ContainsKey(PenaltyType.Mute))
{
    Console.WriteLine("Player is currently muted");
}

// Check if player is silenced
if (muteStatus.ContainsKey(PenaltyType.Silence))
{
    Console.WriteLine("Player is silenced (gag + mute)");
}
```

---

## Server Information

### GetConnectionString

Get the database connection string.

```csharp
string GetConnectionString()
```

**Example:**
```csharp
var connectionString = _api!.GetConnectionString();
// Use for custom database operations
```

---

### GetServerAddress

Get the server's IP address and port.

```csharp
string GetServerAddress()
```

**Example:**
```csharp
var serverAddress = _api!.GetServerAddress();
Console.WriteLine($"Server: {serverAddress}");
// Example output: "192.168.1.100:27015"
```

---

### GetServerId

Get the server's unique ID in the database.

```csharp
int? GetServerId()
```

**Returns:**
- `int` - Server ID if multi-server mode enabled
- `null` - If single-server mode

**Example:**
```csharp
var serverId = _api!.GetServerId();

if (serverId.HasValue)
{
    Console.WriteLine($"Server ID: {serverId.Value}");
}
else
{
    Console.WriteLine("Single-server mode");
}
```

---

## Complete Examples

### Ban with Validation

```csharp
private void BanPlayer(CCSPlayerController? admin, CCSPlayerController target, int duration, string reason)
{
    // Validate player
    if (!target.IsValid)
    {
        admin?.PrintToChat("Invalid player!");
        return;
    }

    // Check immunity
    if (admin != null && !admin.CanTarget(target))
    {
        admin.PrintToChat($"You cannot ban {target.PlayerName} (higher immunity)!");
        return;
    }

    // Get player info to check history
    var playerInfo = _api!.GetPlayerInfo(target);

    Logger.LogInformation(
        $"{admin?.PlayerName ?? "Console"} banning {playerInfo.PlayerName} " +
        $"(SteamID: {playerInfo.SteamId}, Previous bans: {playerInfo.Bans})"
    );

    // Issue ban
    _api.IssuePenalty(target, admin, PenaltyType.Ban, reason, duration);

    // Show activity
    if (admin == null || !_api.IsAdminSilent(admin))
    {
        var durationText = duration == 0 ? "permanently" : $"for {duration} minutes";
        Server.PrintToChatAll($"{admin?.PlayerName ?? "Console"} banned {target.PlayerName} {durationText}: {reason}");
    }
}
```

---

### Progressive Punishment System

```csharp
private void HandlePlayerOffense(CCSPlayerController? admin, CCSPlayerController target, string reason)
{
    var playerInfo = _api!.GetPlayerInfo(target);

    // Progressive punishment based on warning count
    if (playerInfo.Warnings == 0)
    {
        // First offense - warning
        _api.IssuePenalty(target, admin, PenaltyType.Warn, reason);
        target.PrintToChat("This is your first warning!");
    }
    else if (playerInfo.Warnings == 1)
    {
        // Second offense - gag for 30 minutes
        _api.IssuePenalty(target, admin, PenaltyType.Gag, $"Second offense: {reason}", 30);
        target.PrintToChat("Second warning! You are gagged for 30 minutes.");
    }
    else if (playerInfo.Warnings == 2)
    {
        // Third offense - 1 day ban
        _api.IssuePenalty(target, admin, PenaltyType.Ban, $"Third offense: {reason}", 1440);
        target.PrintToChat("Third offense! You are banned for 1 day.");
    }
    else
    {
        // More than 3 warnings - permanent ban
        _api.IssuePenalty(target, admin, PenaltyType.Ban, $"Multiple offenses: {reason}", 0);
    }
}
```

---

### Check Active Penalties Before Action

```csharp
private void AllowPlayerToChat(CCSPlayerController player)
{
    var muteStatus = _api!.GetPlayerMuteStatus(player);

    // Check if player is gagged
    if (muteStatus.ContainsKey(PenaltyType.Gag))
    {
        player.PrintToChat("You are currently gagged and cannot use chat!");
        return;
    }

    // Check if player is silenced (includes gag)
    if (muteStatus.ContainsKey(PenaltyType.Silence))
    {
        player.PrintToChat("You are silenced and cannot communicate!");
        return;
    }

    // Player can chat
    ProcessChatMessage(player);
}
```

---

### Offline Player Ban

```csharp
private void BanOfflinePlayer(CCSPlayerController? admin, string steamIdString, int duration, string reason)
{
    // Parse SteamID
    if (!ulong.TryParse(steamIdString, out ulong steamId64))
    {
        admin?.PrintToChat("Invalid SteamID format!");
        return;
    }

    var steamId = new SteamID(steamId64);

    // Issue offline ban
    _api!.IssuePenalty(steamId, admin, PenaltyType.Ban, reason, duration);

    Logger.LogInformation(
        $"{admin?.PlayerName ?? "Console"} banned offline player " +
        $"(SteamID: {steamId64}) for {duration} minutes: {reason}"
    );

    admin?.PrintToChat($"Offline ban issued to SteamID {steamId64}");
}
```

---

### Multi-Account Detection

```csharp
[GameEventHandler]
public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
{
    var player = @event.Userid;
    if (player == null || !player.IsValid) return HookResult.Continue;

    var playerInfo = _api!.GetPlayerInfo(player);

    // Check if player has multiple accounts
    if (playerInfo.Bans > 0)
    {
        // Notify admins
        var admins = Utilities.GetPlayers()
            .Where(p => AdminManager.PlayerHasPermissions(p, "@css/ban"));

        foreach (var admin in admins)
        {
            admin.PrintToChat(
                $"⚠ {player.PlayerName} has {playerInfo.Bans} previous ban(s)!"
            );
        }
    }

    return HookResult.Continue;
}
```

---

## Best Practices

### 1. Always Validate Players

```csharp
if (!target.IsValid || !target.PawnIsAlive)
{
    return;
}

// Check immunity
if (admin != null && !admin.CanTarget(target))
{
    admin.PrintToChat("Cannot target this player!");
    return;
}
```

### 2. Provide Clear Reasons

```csharp
// ✅ Good - Specific reason
_api.IssuePenalty(player, admin, PenaltyType.Ban, "Aimbot detected in Round 12", 10080);

// ❌ Bad - Vague reason
_api.IssuePenalty(player, admin, PenaltyType.Ban, "cheating", 10080);
```

### 3. Log Penalty Actions

```csharp
_api.IssuePenalty(player, admin, PenaltyType.Ban, reason, duration);

Logger.LogInformation(
    $"Penalty issued: {admin?.PlayerName ?? "Console"} -> {player.PlayerName} " +
    $"| Type: {PenaltyType.Ban} | Duration: {duration}m | Reason: {reason}"
);
```

### 4. Handle Kick Separately

```csharp
// Kick doesn't need duration
_api.IssuePenalty(player, admin, PenaltyType.Kick, reason);

// NOT:
_api.IssuePenalty(player, admin, PenaltyType.Kick, reason, 0);
```

### 5. Check Active Penalties

```csharp
// Before issuing new penalty, check existing ones
var muteStatus = _api.GetPlayerMuteStatus(player);

if (muteStatus.ContainsKey(PenaltyType.Gag))
{
    admin?.PrintToChat($"{player.PlayerName} is already gagged!");
    return;
}
```

---

## Common Patterns

### Duration Helpers

```csharp
public static class PenaltyDurations
{
    public const int OneHour = 60;
    public const int OneDay = 1440;
    public const int OneWeek = 10080;
    public const int TwoWeeks = 20160;
    public const int OneMonth = 43200;
    public const int Permanent = 0;
}

// Usage
_api.IssuePenalty(player, admin, PenaltyType.Ban, reason, PenaltyDurations.OneWeek);
```

### Penalty History Display

```csharp
private void ShowPlayerHistory(CCSPlayerController admin, CCSPlayerController target)
{
    var info = _api!.GetPlayerInfo(target);

    admin.PrintToChat($"=== {info.PlayerName} History ===");
    admin.PrintToChat($"Warnings: {info.Warnings}");
    admin.PrintToChat($"Bans: {info.Bans}");
    admin.PrintToChat($"Mutes: {info.Mutes}");
    admin.PrintToChat($"Gags: {info.Gags}");

    var muteStatus = _api.GetPlayerMuteStatus(target);

    if (muteStatus.ContainsKey(PenaltyType.Gag))
        admin.PrintToChat("Currently: GAGGED");
    if (muteStatus.ContainsKey(PenaltyType.Mute))
        admin.PrintToChat("Currently: MUTED");
    if (muteStatus.ContainsKey(PenaltyType.Silence))
        admin.PrintToChat("Currently: SILENCED");
}
```

---

## Error Handling

### Handle Invalid Players

```csharp
try
{
    var playerInfo = _api!.GetPlayerInfo(player);
    // Use playerInfo...
}
catch (KeyNotFoundException)
{
    Logger.LogError($"Player info not found for {player?.PlayerName}");
    return;
}
```

### Validate SteamID

```csharp
private bool TryParseSteamId(string input, out SteamID steamId)
{
    steamId = default;

    if (ulong.TryParse(input, out ulong steamId64))
    {
        steamId = new SteamID(steamId64);
        return true;
    }

    return false;
}
```

---

## Related APIs

- **[Commands API](commands)** - Issue penalties from commands
- **[Menus API](menus)** - Issue penalties from menus
- **[Events API](events)** - React to penalty events
- **[Utilities API](utilities)** - Helper functions
