---
sidebar_position: 8
---

# Plugin Architecture

Deep dive into CS2-SimpleAdmin's architecture and design patterns.

## Overview

CS2-SimpleAdmin follows a **layered architecture** with clear separation of concerns and well-defined responsibilities for each component.

---

## Architecture Layers

```
┌─────────────────────────────────────────┐
│  CounterStrikeSharp Integration Layer   │ ← CS2_SimpleAdmin.cs
├─────────────────────────────────────────┤
│         Manager Layer                   │ ← /Managers/
│  • PermissionManager                    │
│  • BanManager                           │
│  • MuteManager                          │
│  • WarnManager                          │
│  • CacheManager                         │
│  • PlayerManager                        │
│  • ServerManager                        │
│  • DiscordManager                       │
├─────────────────────────────────────────┤
│         Database Layer                  │ ← /Database/
│  • IDatabaseProvider (Interface)        │
│  • MySqlDatabaseProvider                │
│  • SqliteDatabaseProvider               │
│  • Migration System                     │
├─────────────────────────────────────────┤
│         Menu System                     │ ← /Menus/
│  • MenuManager (Singleton)              │
│  • MenuBuilder (Factory)                │
│  • Specific Menu Classes                │
├─────────────────────────────────────────┤
│         Command System                  │ ← /Commands/
│  • RegisterCommands                     │
│  • Command Handlers (basebans, etc.)    │
├─────────────────────────────────────────┤
│         Public API                      │ ← /Api/
│  • ICS2_SimpleAdminApi (Interface)      │
│  • CS2_SimpleAdminApi (Implementation)  │
└─────────────────────────────────────────┘
```

---

## Core Components

### 1. CounterStrikeSharp Integration Layer

**File:** `CS2_SimpleAdmin.cs`

**Responsibilities:**
- Plugin lifecycle management (Load/Unload)
- Event registration (`player_connect`, `player_disconnect`, etc.)
- Command routing
- Low-level game operations using `MemoryFunctionVoid`
- Timer management

**Key Methods:**
```csharp
public override void Load(bool hotReload)
public override void Unload(bool hotReload)
private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
```

---

### 2. Manager Layer

Each manager encapsulates specific domain logic:

#### PermissionManager

**File:** `/Managers/PermissionManager.cs`

**Responsibilities:**
- Load admin flags and groups from database
- Maintain in-memory `AdminCache` with lazy-loading
- Time-based cache expiry
- Immunity level management

**Key Patterns:**
- Caching for performance
- Lazy loading of admin data
- Periodic refresh

#### BanManager

**File:** `/Managers/BanManager.cs`

**Responsibilities:**
- Issue bans (SteamID, IP, or hybrid)
- Remove bans (unban)
- Handle ban expiration cleanup
- Multi-server ban synchronization

**Key Operations:**
```csharp
Task BanPlayer(...)
Task AddBanBySteamId(...)
Task RemoveBan(...)
```

#### MuteManager

**File:** `/Managers/MuteManager.cs`

**Responsibilities:**
- Three mute types: GAG (text), MUTE (voice), SILENCE (both)
- Duration-based mutes
- Expiration tracking

#### WarnManager

**File:** `/Managers/WarnManager.cs`

**Responsibilities:**
- Progressive warning system
- Auto-escalation to bans based on `WarnThreshold` config
- Warning history tracking

#### CacheManager

**File:** `/Managers/CacheManager.cs`

**Purpose:** Performance optimization layer

**Features:**
- In-memory ban cache with O(1) lookups by SteamID and IP
- Player IP history tracking for multi-account detection
- Reduces database queries on player join

**Data Structures:**
```csharp
Dictionary<ulong, BanInfo> _banCacheBySteamId
Dictionary<string, List<BanInfo>> _banCacheByIp
Dictionary<ulong, List<string>> _playerIpHistory
```

#### PlayerManager

**File:** `/Managers/PlayerManager.cs`

**Responsibilities:**
- Load player data on connect
- Check bans against cache
- Update IP history
- Semaphore limiting (max 5 concurrent loads)

**Key Pattern:**
```csharp
private readonly SemaphoreSlim _semaphore = new(5, 5);

public async Task LoadPlayerData(CCSPlayerController player)
{
    await _semaphore.WaitAsync();
    try
    {
        // Load player data
    }
    finally
    {
        _semaphore.Release();
    }
}
```

#### ServerManager

**File:** `/Managers/ServerManager.cs`

**Responsibilities:**
- Load/register server metadata (IP, port, hostname, RCON)
- Multi-server mode support
- Server ID management

#### DiscordManager

**File:** `/Managers/DiscordManager.cs`

**Responsibilities:**
- Send webhook notifications for admin actions
- Configurable webhooks per penalty type
- Embed formatting with placeholders

---

### 3. Database Layer

**Files:** `/Database/`

**Provider Pattern** for database abstraction:

```csharp
public interface IDatabaseProvider
{
    Task ExecuteAsync(string query, object? parameters = null);
    Task<T> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null);
    Task<List<T>> QueryAsync<T>(string query, object? parameters = null);

    // Query generation methods
    string GetBanQuery(bool multiServer);
    string GetMuteQuery(bool multiServer);
    // ... more query methods
}
```

**Implementations:**
- `MySqlDatabaseProvider` - MySQL-specific SQL syntax
- `SqliteDatabaseProvider` - SQLite-specific SQL syntax

**Benefits:**
- Single codebase supports both MySQL and SQLite
- Easy to add new database providers
- Query methods accept `multiServer` boolean for scoping

**Migration System:**

**File:** `Database/Migration.cs`

- File-based migrations in `/Database/Migrations/{mysql,sqlite}/`
- Numbered files: `001_CreateTables.sql`, `002_AddColumn.sql`
- Tracking table: `sa_migrations`
- Auto-applies on plugin load
- Safe for multi-server environments

---

### 4. Menu System

**Files:** `/Menus/`

**MenuManager (Singleton Pattern):**

```csharp
public class MenuManager
{
    public static MenuManager Instance { get; private set; }

    private readonly Dictionary<string, MenuCategory> _categories = new();
    private readonly Dictionary<string, Dictionary<string, MenuInfo>> _menus = new();

    public void RegisterCategory(string id, string name, string permission);
    public void RegisterMenu(string categoryId, string menuId, string name, ...);
    public MenuBuilder CreateCategoryMenuPublic(MenuCategory category, CCSPlayerController player);
}
```

**MenuBuilder (Factory Pattern):**

```csharp
public class MenuBuilder
{
    public MenuBuilder(string title);
    public MenuBuilder AddOption(string name, Action<CCSPlayerController> action, ...);
    public MenuBuilder AddSubMenu(string name, Func<CCSPlayerController, MenuBuilder> factory, ...);
    public MenuBuilder WithBackAction(Action<CCSPlayerController> backAction);
    public void OpenMenu(CCSPlayerController player);
}
```

**Specific Menu Classes:**
- `AdminMenu` - Main admin menu with categories
- `ManagePlayersMenu` - Player management menus
- `ManageServerMenu` - Server settings
- `DurationMenu` - Duration selection
- `ReasonMenu` - Reason selection

**Benefits:**
- Centralized menu management
- Permission-aware rendering
- Automatic back button handling
- Reusable menu components

---

### 5. Command System

**Files:** `/Commands/`

**Central Registration:**

**File:** `RegisterCommands.cs`

```csharp
public static class RegisterCommands
{
    public static Dictionary<string, List<CommandDefinition>> _commandDefinitions = new();

    public static void RegisterCommands(CS2_SimpleAdmin plugin)
    {
        // Load Commands.json
        // Map commands to handler methods
        // Register with CounterStrikeSharp
    }
}
```

**Command Handlers:**

Organized by category:
- `basebans.cs` - Ban, unban, warn commands
- `basecomms.cs` - Gag, mute, silence commands
- `basecommands.cs` - Admin management, server commands
- `basechat.cs` - Chat commands (asay, csay, etc.)
- `playercommands.cs` - Player manipulation (slay, hp, etc.)
- `funcommands.cs` - Fun commands (god, noclip, etc.)
- `basevotes.cs` - Voting system

**Two-Tier Pattern:**

```csharp
// Entry command - parses arguments
[CommandHelper(2, "<#userid> <duration> [reason]")]
[RequiresPermissions("@css/ban")]
public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
{
    var targets = GetTarget(command);
    int duration = ParseDuration(command.GetArg(2));
    string reason = ParseReason(command);

    foreach (var target in targets)
    {
        Ban(caller, target, duration, reason);  // Core method
    }
}

// Core method - database writes, events
private void Ban(CCSPlayerController? admin, CCSPlayerController target, int duration, string reason)
{
    // Write to database
    BanManager.BanPlayer(target, admin, duration, reason);

    // Update cache
    CacheManager.AddBan(target);

    // Trigger events
    ApiInstance.OnPlayerPenaltiedEvent(target, admin, PenaltyType.Ban, reason, duration);

    // Kick player
    Server.ExecuteCommand($"kick {target.UserId}");

    // Send Discord notification
    DiscordManager.SendBanNotification(target, admin, duration, reason);

    // Broadcast action
    ShowAdminActivity("ban_message", admin?.PlayerName, target.PlayerName, duration, reason);
}
```

**Benefits:**
- Separation of parsing and execution
- Reusable core methods
- Consistent event triggering
- Easy to test

---

### 6. Public API

**Files:** `/Api/`

**Interface:** `ICS2_SimpleAdminApi.cs` (in CS2-SimpleAdminApi project)

**Implementation:** `CS2_SimpleAdminApi.cs`

**Capability System:**

```csharp
// In API interface
public static readonly PluginCapability<ICS2_SimpleAdminApi> PluginCapability = new("simpleadmin:api");

// In module
private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability = new("simpleadmin:api");

public override void OnAllPluginsLoaded(bool hotReload)
{
    _api = _pluginCapability.Get();
}
```

**Event Publishing:**

```csharp
// API exposes events
public event Action<PlayerInfo, PlayerInfo?, PenaltyType, ...>? OnPlayerPenaltied;

// Core plugin triggers events
ApiInstance.OnPlayerPenaltiedEvent(player, admin, type, reason, duration, id);

// Modules subscribe
_api.OnPlayerPenaltied += (player, admin, type, reason, duration, id, sid) =>
{
    // React to penalty
};
```

---

## Data Flow Patterns

### Player Join Flow

```
1. player_connect event
   ↓
2. PlayerManager.LoadPlayerData()
   ↓
3. Semaphore.WaitAsync() ← Max 5 concurrent
   ↓
4. CacheManager.CheckBan(steamId, ip)
   ↓
5a. BANNED → Kick player immediately
5b. CLEAN → Continue
   ↓
6. Load active penalties from DB
   ↓
7. Store in PlayersInfo dictionary
   ↓
8. Update player IP history
```

### Ban Command Flow

```
1. OnBanCommand() ← Parse arguments
   ↓
2. Ban() ← Core method
   ↓
3. BanManager.BanPlayer() ← Write to DB
   ↓
4. CacheManager.AddBan() ← Update cache
   ↓
5. ApiInstance.OnPlayerPenaltiedEvent() ← Trigger event
   ↓
6. Server.ExecuteCommand("kick") ← Kick player
   ↓
7. DiscordManager.SendNotification() ← Discord webhook
   ↓
8. ShowAdminActivity() ← Broadcast action
```

### Admin Permission Check Flow

```
1. Plugin Load
   ↓
2. PermissionManager.LoadAdmins()
   ↓
3. Build AdminCache ← SteamID → Flags/Immunity
   ↓
4. Command Execution
   ↓
5. RequiresPermissions attribute check
   ↓
6. AdminManager.PlayerHasPermissions() ← Check cache
   ↓
7a. HAS PERMISSION → Execute
7b. NO PERMISSION → Deny
```

---

## Design Patterns Used

### Singleton Pattern

```csharp
public class MenuManager
{
    public static MenuManager Instance { get; private set; }

    public static void Initialize(CS2_SimpleAdmin plugin)
    {
        Instance = new MenuManager(plugin);
    }
}
```

**Used for:**
- MenuManager - Single menu registry
- Cache management - Single source of truth

### Factory Pattern

```csharp
public class MenuBuilder
{
    public static MenuBuilder Create(string title) => new MenuBuilder(title);

    public MenuBuilder AddOption(...) { /* ... */ return this; }
    public MenuBuilder AddSubMenu(...) { /* ... */ return this; }
}
```

**Used for:**
- Menu creation
- Database provider creation

### Strategy Pattern

```csharp
public interface IDatabaseProvider
{
    Task<List<BanInfo>> GetBans(bool multiServer);
}

public class MySqlDatabaseProvider : IDatabaseProvider { /* ... */ }
public class SqliteDatabaseProvider : IDatabaseProvider { /* ... */ }
```

**Used for:**
- Database abstraction
- Query generation per DB type

### Observer Pattern

```csharp
// Publisher
public event Action<PlayerInfo, ...>? OnPlayerPenaltied;

// Trigger
OnPlayerPenaltied?.Invoke(player, admin, type, reason, duration, id, sid);

// Subscribers
_api.OnPlayerPenaltied += (player, admin, type, reason, duration, id, sid) =>
{
    // React
};
```

**Used for:**
- Event system
- Module communication

---

## Concurrency & Thread Safety

### Async/Await Patterns

All database operations use `async`/`await`:

```csharp
public async Task BanPlayer(...)
{
    await _database.ExecuteAsync(query, parameters);
}
```

### Semaphore for Rate Limiting

```csharp
private readonly SemaphoreSlim _semaphore = new(5, 5);

public async Task LoadPlayerData(CCSPlayerController player)
{
    await _semaphore.WaitAsync();
    try
    {
        // Load data
    }
    finally
    {
        _semaphore.Release();
    }
}
```

### Thread-Safe Collections

```csharp
private readonly ConcurrentDictionary<ulong, PlayerInfo> PlayersInfo = new();
```

---

## Memory Management

### In-Memory Caches

**AdminCache:**
```csharp
Dictionary<ulong, (List<string> Flags, int Immunity, DateTime Expiry)> AdminCache
```

**BanCache:**
```csharp
Dictionary<ulong, BanInfo> _banCacheBySteamId
Dictionary<string, List<BanInfo>> _banCacheByIp
```

**Benefits:**
- Reduces database load
- O(1) lookups
- TTL-based expiry

### Cleanup

```csharp
// On player disconnect
PlayersInfo.TryRemove(player.SteamID, out _);

// Periodic cache cleanup
AddTimer(3600f, CleanupExpiredCache, TimerFlags.REPEAT);
```

---

## Configuration System

### Multi-Level Configuration

1. **Main Config:** `CS2-SimpleAdmin.json`
2. **Commands Config:** `Commands.json`
3. **Module Configs:** Per-module JSON files

### Hot Reload Support

```csharp
public void OnConfigParsed(Config config)
{
    Config = config;
    // Reconfigure without restart
}
```

---

## Performance Optimizations

1. **Caching** - Minimize database queries
2. **Lazy Loading** - Load admin data on-demand
3. **Semaphore** - Limit concurrent operations
4. **Connection Pooling** - Reuse DB connections
5. **Indexed Queries** - Fast database lookups
6. **Memory Cleanup** - Remove disconnected player data

---

## Future Extensibility

### Plugin Capabilities

New modules can extend functionality:

```csharp
// New capability
var customCapability = new PluginCapability<ICustomFeature>("custom:feature");
Capabilities.RegisterPluginCapability(customCapability, () => _customFeature);

// Other plugins can use it
var feature = _customCapability.Get();
```

### Event-Driven Architecture

New events can be added without breaking changes:

```csharp
public event Action<NewEventArgs>? OnNewEvent;
```

---

## Testing Considerations

### Unit Testing

- Managers can be tested independently
- Mock `IDatabaseProvider` for testing
- Test command handlers with mock players

### Integration Testing

- Test on actual CS2 server
- Multi-server scenarios
- Database migration testing

---

## Related Documentation

- **[API Overview](api/overview)** - Public API details
- **[Module Development](module/getting-started)** - Create modules
- **[GitHub Source](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Browse code
