---
sidebar_position: 6
---

# Utilities API

Helper functions and utility methods for module development.

## Player Management

### GetValidPlayers

Get a list of all valid, connected players.

```csharp
List<CCSPlayerController> GetValidPlayers()
```

**Returns:** List of valid player controllers

**Example:**
```csharp
var players = _api!.GetValidPlayers();

foreach (var player in players)
{
    Console.WriteLine($"Player: {player.PlayerName}");
}

// Filter for specific criteria
var alivePlayers = _api.GetValidPlayers()
    .Where(p => p.PawnIsAlive)
    .ToList();

var ctPlayers = _api.GetValidPlayers()
    .Where(p => p.Team == CsTeam.CounterTerrorist)
    .ToList();
```

**Note:** This method filters out invalid and bot players automatically.

---

## Admin Status

### IsAdminSilent

Check if an admin is in silent mode.

```csharp
bool IsAdminSilent(CCSPlayerController player)
```

**Parameters:**
- `player` - Player to check

**Returns:** `true` if player is in silent mode, `false` otherwise

**Example:**
```csharp
private void PerformAdminAction(CCSPlayerController admin, CCSPlayerController target)
{
    // Do the action
    DoAction(target);

    // Only show activity if not silent
    if (!_api!.IsAdminSilent(admin))
    {
        Server.PrintToChatAll($"{admin.PlayerName} performed action on {target.PlayerName}");
    }
}
```

---

### ListSilentAdminsSlots

Get a list of player slots for all admins currently in silent mode.

```csharp
HashSet<int> ListSilentAdminsSlots()
```

**Returns:** HashSet of player slots

**Example:**
```csharp
var silentAdmins = _api!.ListSilentAdminsSlots();

Console.WriteLine($"Silent admins: {silentAdmins.Count}");

foreach (var slot in silentAdmins)
{
    var player = Utilities.GetPlayerFromSlot(slot);
    if (player != null)
    {
        Console.WriteLine($"- {player.PlayerName} (slot {slot})");
    }
}
```

---

## Activity Messages

### ShowAdminActivity

Show an admin activity message to all players.

```csharp
void ShowAdminActivity(
    string messageKey,
    string? callerName = null,
    bool dontPublish = false,
    params object[] messageArgs
)
```

**Parameters:**
- `messageKey` - Translation key from SimpleAdmin's lang files
- `callerName` - Admin name (null for console)
- `dontPublish` - If true, don't trigger OnAdminShowActivity event
- `messageArgs` - Arguments for message formatting

**Example:**
```csharp
// Using SimpleAdmin's built-in translations
_api!.ShowAdminActivity(
    "sa_admin_player_kick_message",  // Translation key
    admin?.PlayerName,
    false,
    player.PlayerName,
    reason
);
```

**Limitations:**
- Only works with SimpleAdmin's own translation keys
- For module-specific messages, use `ShowAdminActivityLocalized`

---

### ShowAdminActivityTranslated

Show a pre-translated admin activity message.

```csharp
void ShowAdminActivityTranslated(
    string translatedMessage,
    string? callerName = null,
    bool dontPublish = false
)
```

**Parameters:**
- `translatedMessage` - Already translated message
- `callerName` - Admin name
- `dontPublish` - If true, don't trigger event

**Example:**
```csharp
// Use when you've already translated the message
var message = Localizer?["my_action_message", player.PlayerName] ?? $"Action on {player.PlayerName}";

_api!.ShowAdminActivityTranslated(
    message,
    admin?.PlayerName,
    false
);
```

**Use Case:**
- When you need custom message formatting
- When translation is already done

---

### ShowAdminActivityLocalized ⭐ RECOMMENDED

Show admin activity with per-player language support using module's localizer.

```csharp
void ShowAdminActivityLocalized(
    object moduleLocalizer,
    string messageKey,
    string? callerName = null,
    bool dontPublish = false,
    params object[] messageArgs
)
```

**Parameters:**
- `moduleLocalizer` - Your module's `IStringLocalizer` instance
- `messageKey` - Translation key from your module's lang files
- `callerName` - Admin name
- `dontPublish` - If true, don't trigger event
- `messageArgs` - Message arguments

**Example:**
```csharp
// Each player sees message in their configured language!
if (Localizer != null)
{
    _api!.ShowAdminActivityLocalized(
        Localizer,  // Your module's localizer
        "fun_admin_god_message",  // From your lang/en.json
        admin?.PlayerName,
        false,
        player.PlayerName
    );
}
```

**lang/en.json:**
```json
{
  "fun_admin_god_message": "{lightred}{0}{default} changed god mode for {lightred}{1}{default}!"
}
```

**Why This is Best:**
- ✅ Each player sees message in their own language
- ✅ Uses your module's translations
- ✅ Supports color codes
- ✅ Per-player localization

---

## Complete Examples

### Action with Activity Message

```csharp
private void ToggleGodMode(CCSPlayerController? admin, CCSPlayerController target)
{
    // Perform action
    if (GodPlayers.Contains(target.Slot))
    {
        GodPlayers.Remove(target.Slot);
    }
    else
    {
        GodPlayers.Add(target.Slot);
    }

    // Show activity (respecting silent mode)
    if (admin == null || !_api!.IsAdminSilent(admin))
    {
        if (Localizer != null)
        {
            _api!.ShowAdminActivityLocalized(
                Localizer,
                "fun_admin_god_message",
                admin?.PlayerName,
                false,
                target.PlayerName
            );
        }
    }

    // Log action
    _api!.LogCommand(admin, $"css_god {target.PlayerName}");
}
```

---

### Broadcast to Non-Silent Admins

```csharp
private void NotifyAdmins(string message)
{
    var silentAdmins = _api!.ListSilentAdminsSlots();

    var players = _api.GetValidPlayers();

    foreach (var player in players)
    {
        // Check if player is admin
        if (!AdminManager.PlayerHasPermissions(player, "@css/generic"))
            continue;

        // Skip if admin is in silent mode
        if (silentAdmins.Contains(player.Slot))
            continue;

        player.PrintToChat(message);
    }
}
```

---

### Filter Players by Criteria

```csharp
private List<CCSPlayerController> GetTargetablePlayers(CCSPlayerController admin)
{
    return _api!.GetValidPlayers()
        .Where(p =>
            p.IsValid &&
            !p.IsBot &&
            p.PawnIsAlive &&
            admin.CanTarget(p))
        .ToList();
}

private List<CCSPlayerController> GetAliveEnemies(CCSPlayerController player)
{
    return _api!.GetValidPlayers()
        .Where(p =>
            p.Team != player.Team &&
            p.PawnIsAlive)
        .ToList();
}

private List<CCSPlayerController> GetAdmins()
{
    return _api!.GetValidPlayers()
        .Where(p => AdminManager.PlayerHasPermissions(p, "@css/generic"))
        .ToList();
}
```

---

## Best Practices

### 1. Use ShowAdminActivityLocalized

```csharp
// ✅ Good - Per-player language
_api.ShowAdminActivityLocalized(
    Localizer,
    "my_message_key",
    admin?.PlayerName,
    false,
    args
);

// ❌ Bad - Single language for all
Server.PrintToChatAll($"{admin?.PlayerName} did something");
```

### 2. Respect Silent Mode

```csharp
// ✅ Good - Check silent mode
if (admin == null || !_api.IsAdminSilent(admin))
{
    ShowActivity();
}

// ❌ Bad - Always show activity
ShowActivity();  // Ignores silent mode!
```

### 3. Validate Players from GetValidPlayers

```csharp
var players = _api.GetValidPlayers();

foreach (var player in players)
{
    // Still good to check, especially for async operations
    if (!player.IsValid) continue;

    DoSomething(player);
}
```

### 4. Cache Silent Admin List if Checking Multiple Times

```csharp
// ✅ Good - Cache for multiple checks
var silentAdmins = _api.ListSilentAdminsSlots();

foreach (var admin in admins)
{
    if (silentAdmins.Contains(admin.Slot)) continue;
    NotifyAdmin(admin);
}

// ❌ Bad - Query for each admin
foreach (var admin in admins)
{
    if (_api.IsAdminSilent(admin)) continue;  // ← Repeated calls
    NotifyAdmin(admin);
}
```

---

## Common Patterns

### Silent Mode Wrapper

```csharp
private void ShowActivityIfNotSilent(
    CCSPlayerController? admin,
    string messageKey,
    params object[] args)
{
    if (admin != null && _api!.IsAdminSilent(admin))
        return;

    if (Localizer != null)
    {
        _api!.ShowAdminActivityLocalized(
            Localizer,
            messageKey,
            admin?.PlayerName,
            false,
            args
        );
    }
}

// Usage
ShowActivityIfNotSilent(admin, "my_action", player.PlayerName);
```

---

### Get Online Admins

```csharp
private List<CCSPlayerController> GetOnlineAdmins(string permission = "@css/generic")
{
    return _api!.GetValidPlayers()
        .Where(p => AdminManager.PlayerHasPermissions(p, permission))
        .ToList();
}

// Usage
var admins = GetOnlineAdmins("@css/root");
foreach (var admin in admins)
{
    admin.PrintToChat("Important admin message");
}
```

---

### Notify All Players Except Silent Admins

```csharp
private void BroadcastMessage(string message, bool excludeSilentAdmins = true)
{
    var silentAdmins = excludeSilentAdmins
        ? _api!.ListSilentAdminsSlots()
        : new HashSet<int>();

    foreach (var player in _api.GetValidPlayers())
    {
        if (silentAdmins.Contains(player.Slot))
            continue;

        player.PrintToChat(message);
    }
}
```

---

## Activity Message Formatting

### Color Codes in Messages

All activity messages support color codes:

```json
{
  "my_message": "{lightred}Admin{default} banned {lightred}{0}{default} for {yellow}{1}{default}"
}
```

**Available Colors:**
- `{default}` - Default color
- `{white}` - White
- `{darkred}` - Dark red
- `{green}` - Green
- `{lightyellow}` - Light yellow
- `{lightblue}` - Light blue
- `{olive}` - Olive
- `{lime}` - Lime
- `{red}` - Red
- `{purple}` - Purple
- `{grey}` - Grey
- `{yellow}` - Yellow
- `{gold}` - Gold
- `{silver}` - Silver
- `{blue}` - Blue
- `{darkblue}` - Dark blue
- `{bluegrey}` - Blue grey
- `{magenta}` - Magenta
- `{lightred}` - Light red
- `{orange}` - Orange

---

### Message Arguments

```csharp
// lang/en.json
{
  "ban_message": "{lightred}{0}{default} banned {lightred}{1}{default} for {yellow}{2}{default} minutes: {red}{3}"
}

// Code
_api.ShowAdminActivityLocalized(
    Localizer,
    "ban_message",
    admin?.PlayerName,
    false,
    admin?.PlayerName,  // {0}
    target.PlayerName,   // {1}
    duration,            // {2}
    reason               // {3}
);
```

---

## Performance Tips

### Minimize GetValidPlayers Calls

```csharp
// ✅ Good - Call once, filter multiple times
var allPlayers = _api.GetValidPlayers();
var alivePlayers = allPlayers.Where(p => p.PawnIsAlive).ToList();
var deadPlayers = allPlayers.Where(p => !p.PawnIsAlive).ToList();

// ❌ Bad - Multiple calls
var alivePlayers = _api.GetValidPlayers().Where(p => p.PawnIsAlive).ToList();
var deadPlayers = _api.GetValidPlayers().Where(p => !p.PawnIsAlive).ToList();
```

---

### Efficient Filtering

```csharp
// ✅ Good - Single LINQ query
var targets = _api.GetValidPlayers()
    .Where(p => p.Team == CsTeam.Terrorist &&
                p.PawnIsAlive &&
                admin.CanTarget(p))
    .ToList();

// ❌ Bad - Multiple iterations
var players = _api.GetValidPlayers();
players = players.Where(p => p.Team == CsTeam.Terrorist).ToList();
players = players.Where(p => p.PawnIsAlive).ToList();
players = players.Where(p => admin.CanTarget(p)).ToList();
```

---

## Troubleshooting

### Activity Messages Not Showing

**Check:**
1. Is `Localizer` not null?
2. Does translation key exist in lang files?
3. Is message correctly formatted?
4. Check `dontPublish` parameter

### Silent Mode Not Working

**Check:**
1. Is player actually in silent mode? (`css_hide` command)
2. Are you checking before showing activity?
3. Check slot vs player controller mismatch

---

## Related APIs

- **[Commands API](commands)** - Log commands
- **[Menus API](menus)** - Get players for menus
- **[Events API](events)** - Admin activity events
- **[Penalties API](penalties)** - Get player info
