---
sidebar_position: 3
---

# Base Commands

Core admin commands for server management and admin system.

## Player Information

### Show Penalties

View your own active penalties.

```bash
css_penalties
css_mypenalties
css_comms
```

**Permission:** None (all players)

**Shows:**
- Active bans
- Active communication restrictions (gag, mute, silence)
- Warning count
- Duration remaining

---

### Hide Penalty Notifications

Hide penalty notifications when you connect to the server.

```bash
css_hidecomms
```

**Permission:** `@css/kick`

**Notes:**
- Toggle on/off
- Admins won't see penalty notifications on join
- Useful for admin privacy

---

## Admin Menu

### Open Admin Menu

Opens the main admin menu interface.

```bash
css_admin
```

**Permission:** `@css/generic`

**Features:**
- Player management
- Server management
- Ban/kick/mute players via menu
- Map changing
- Custom server commands

---

### Admin Help

Print the admin help file.

```bash
css_adminhelp
```

**Permission:** `@css/generic`

**Shows:**
- Available commands for your permission level
- Command syntax
- Permission requirements

---

## Admin Management

### Add Admin

Add a new admin to the database.

```bash
css_addadmin <steamid> <name> <flags/groups> <immunity> <duration>
```

**Permission:** `@css/root`

**Parameters:**
- `steamid` - Player's SteamID (any format)
- `name` - Admin name (for identification)
- `flags/groups` - Permission flags or group name
- `immunity` - Immunity level (0-100, higher = more protection)
- `duration` - Duration in minutes (0 = permanent)

**Examples:**
```bash
# Add permanent admin with root access
css_addadmin 76561198012345678 "AdminName" "@css/root" 99 0

# Add moderator for 30 days
css_addadmin STEAM_1:0:12345678 "ModName" "@css/kick,@css/ban" 50 43200

# Add admin using group
css_addadmin 76561198012345678 "AdminName" "#moderators" 60 0

# Add admin to all servers (-g flag)
css_addadmin 76561198012345678 "AdminName" "@css/root" 99 0 -g
```

**Flags:**
- `-g` - Add to all servers (global admin)

---

### Delete Admin

Remove an admin from the database.

```bash
css_deladmin <steamid>
```

**Permission:** `@css/root`

**Examples:**
```bash
# Remove admin from current server
css_deladmin 76561198012345678

# Remove admin from all servers (-g flag)
css_deladmin 76561198012345678 -g
```

---

### Add Admin Group

Create a new admin group.

```bash
css_addgroup <group_name> <flags> <immunity>
```

**Permission:** `@css/root`

**Parameters:**
- `group_name` - Name of the group (e.g., "#moderators")
- `flags` - Permission flags for the group
- `immunity` - Default immunity level for group members

**Examples:**
```bash
# Create moderator group
css_addgroup "#moderators" "@css/kick,@css/ban,@css/chat" 50

# Create VIP group
css_addgroup "#vip" "@css/vip" 10

# Create global group (-g flag)
css_addgroup "#owner" "@css/root" 99 -g
```

**Flags:**
- `-g` - Create group on all servers

---

### Delete Admin Group

Remove an admin group from the database.

```bash
css_delgroup <group_name>
```

**Permission:** `@css/root`

**Examples:**
```bash
# Delete group from current server
css_delgroup "#moderators"

# Delete group from all servers (-g flag)
css_delgroup "#moderators" -g
```

---

### Reload Admins

Reload admin permissions from the database.

```bash
css_reloadadmins
```

**Permission:** `@css/root`

**When to use:**
- After adding/removing admins via database
- After modifying admin permissions
- After group changes
- Troubleshooting permission issues

**Note:** Admins are automatically reloaded periodically and on map change (if configured).

---

## Player Information

### Hide in Scoreboard

Toggle admin stealth mode (hide from scoreboard).

```bash
css_hide
css_stealth
```

**Permission:** `@css/kick`

**Features:**
- Hides you from the scoreboard
- Makes admin actions anonymous
- Useful for undercover moderation

---

### Who is This Player

Show detailed information about a player.

```bash
css_who <#userid or name>
```

**Permission:** `@css/generic`

**Shows:**
- Player name and SteamID
- IP address (if you have `@css/showip` permission)
- Connection time
- Active penalties
- Warning count
- Ban history

**Examples:**
```bash
css_who #123
css_who PlayerName
css_who @me
```

---

### Show Disconnected Players

Show recently disconnected players.

```bash
css_disconnected
css_last
css_last10  # Show last 10 (config value)
```

**Permission:** `@css/kick`

**Shows:**
- Player name
- SteamID
- Disconnect time
- Disconnect reason

**Configuration:**
```json
"DisconnectedPlayersHistoryCount": 10
```

---

### Show Warns for Player

Open warn list for a specific player.

```bash
css_warns <#userid or name>
```

**Permission:** `@css/kick`

**Shows:**
- All warnings for the player
- Warning reasons
- Admins who issued warnings
- Warning timestamps
- Total warning count

**Examples:**
```bash
css_warns #123
css_warns PlayerName
```

---

### Show Online Players

Show information about all online players.

```bash
css_players
```

**Permission:** `@css/generic`

**Shows:**
- List of all connected players
- UserIDs
- Names
- Teams
- Connection status

---

## Server Management

### Kick Player

Kick a player from the server.

```bash
css_kick <#userid or name> [reason]
```

**Permission:** `@css/kick`

**Examples:**
```bash
css_kick #123 "AFK"
css_kick PlayerName "Rule violation"
css_kick @spec "Cleaning spectators"
```

**Configuration:**
```json
"KickTime": 5
```
Delay in seconds before kicking (allows player to see the reason).

---

### Change Map

Change to a different map.

```bash
css_map <mapname>
css_changemap <mapname>
```

**Permission:** `@css/changemap`

**Examples:**
```bash
css_map de_dust2
css_changemap de_mirage
```

**Configuration:**
```json
"DefaultMaps": [
  "de_dust2",
  "de_mirage",
  "de_inferno"
]
```

Maps in this list appear in the map change menu.

---

### Change Workshop Map

Change to a workshop map by ID or name.

```bash
css_wsmap <name or id>
css_changewsmap <name or id>
css_workshop <name or id>
```

**Permission:** `@css/changemap`

**Examples:**
```bash
css_wsmap 123456789
css_wsmap aim_map
```

**Configuration:**
```json
"WorkshopMaps": {
  "aim_map": "123456789",
  "surf_map": "987654321"
}
```

Maps configured here can be changed by name instead of ID.

---

### Change CVar

Change a server console variable.

```bash
css_cvar <cvar> <value>
```

**Permission:** `@css/cvar`

**Examples:**
```bash
css_cvar sv_cheats 1
css_cvar mp_roundtime 5
css_cvar mp_maxmoney 16000
```

**Warning:** This is a powerful command. Only grant to trusted admins.

---

### Execute RCON Command

Execute any command as the server.

```bash
css_rcon <command>
```

**Permission:** `@css/rcon`

**Examples:**
```bash
css_rcon status
css_rcon changelevel de_dust2
css_rcon sv_cheats 1
```

**Warning:** Extremely powerful command. Only grant to server owners.

**Configuration:**
```json
"DisableDangerousCommands": true
```

When enabled, prevents execution of dangerous commands via css_rcon.

---

### Restart Game

Restart the current game/round.

```bash
css_rr
css_rg
css_restart
css_restartgame
```

**Permission:** `@css/generic`

**Notes:**
- Restarts the current round
- Score is reset
- Players remain connected

---

## Permission Flags

Common permission flags used in CS2-SimpleAdmin:

| Flag | Description | Common Use |
|------|-------------|------------|
| `@css/generic` | Generic admin access | Basic admin menu, info commands |
| `@css/chat` | Chat management | Gag, mute, silence |
| `@css/kick` | Kick players | Kick, warnings, player info |
| `@css/ban` | Ban players | Ban, banip, addban |
| `@css/unban` | Unban players | Remove bans |
| `@css/permban` | Permanent bans | Issue permanent bans |
| `@css/changemap` | Change maps | Map changing |
| `@css/cvar` | Change cvars | Server variable modification |
| `@css/rcon` | Execute rcon | Full server control |
| `@css/root` | Root access | All permissions, admin management |
| `@css/slay` | Slay/respawn | Player manipulation |
| `@css/cheats` | Cheat commands | God mode, noclip, give weapons |
| `@css/showip` | View IPs | See player IP addresses |

---

## Immunity System

Immunity prevents lower-level admins from targeting higher-level admins.

**How it works:**
- Each admin has an immunity value (0-100)
- Higher immunity = more protection
- Admins can only target players with lower immunity

**Example:**
- Admin A has immunity 50
- Admin B has immunity 30
- Admin A can ban Admin B
- Admin B cannot ban Admin A

**Best Practice:**
- Owner: 99
- Senior admins: 80-90
- Moderators: 50-70
- Trial mods: 20-40
- Regular players: 0

---

## Configuration Options

### Reload Admins on Map Change

```json
"ReloadAdminsEveryMapChange": false
```

**Options:**
- `true` - Reload admin permissions every map change
- `false` - Only reload when explicitly requested (better performance)

### Show Activity Type

```json
"ShowActivityType": 2
```

**Options:**
- `0` - Hide all admin activity
- `1` - Show activity anonymously ("An admin banned PlayerName")
- `2` - Show admin name ("AdminName banned PlayerName")

---

## Best Practices

### Admin Management

1. **Use groups** - Easier to manage than individual permissions
2. **Set appropriate immunity** - Prevent abuse
3. **Time-limited admin** - For trial moderators
4. **Document changes** - Keep track of who has what permissions

### Permission Assignment

**Recommended hierarchy:**
```
Root (@css/root, immunity 99):
  - Server owners only

Senior Admin (@css/ban,@css/kick,@css/chat,@css/changemap, immunity 80):
  - Trusted long-term admins

Moderator (@css/kick,@css/chat, immunity 50):
  - Regular moderators

Trial Mod (@css/kick, immunity 20):
  - New moderators on probation
```

### Security

1. **Limit @css/rcon** - Only to server owner
2. **Limit @css/cvar** - Only to senior admins
3. **Monitor admin actions** - Review logs regularly
4. **Use time-limited admin** - For temporary staff

---

## Troubleshooting

### Admin permissions not working

**Check:**
1. Is admin correctly added with `css_addadmin`?
2. Run `css_reloadadmins`
3. Check database connection
4. Verify SteamID format

### Can't target another admin

**Check:**
- Your immunity level vs target's immunity
- You need equal or higher immunity to target

### Commands not available

**Check:**
- Your permission flags
- Commands.json for disabled commands
- Server console for errors

---

## Related Commands

- **[Ban Commands](basebans)** - Player punishment
- **[Communication Commands](basecomms)** - Chat/voice management
- **[Player Commands](playercommands)** - Player manipulation
