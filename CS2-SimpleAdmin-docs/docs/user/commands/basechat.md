---
sidebar_position: 4
---

# Chat Commands

Admin chat and messaging commands.

## Admin Chat

### Admin Say (Private)

Send a message to all online admins only.

```bash
css_asay <message>
```

**Permission:** `@css/chat`

**Features:**
- Only admins see the message
- Useful for admin coordination
- Colored differently from regular chat

**Examples:**
```bash
css_asay "Player is suspicious, keep an eye on them"
css_asay "I'm going AFK for 5 minutes"
css_asay "Need help with a situation"
```

**Message format:**
```
[ADMIN] YourName: message
```

---

## Public Announcements

### CSS Say (Colored)

Send a colored message to all players.

```bash
css_cssay <message>
```

**Permission:** `@css/chat`

**Features:**
- Colorful formatted message
- Visible to all players
- Stands out from regular chat

**Examples:**
```bash
css_cssay "Server will restart in 5 minutes!"
css_cssay "Welcome to our server!"
css_cssay "Rules: No cheating, be respectful"
```

---

### Say (With Prefix)

Send a message to all players with admin prefix.

```bash
css_say <message>
```

**Permission:** `@css/chat`

**Features:**
- Message shows with "(ADMIN)" prefix
- Visible to all players
- Authority message format

**Examples:**
```bash
css_say "Please be respectful in chat"
css_say "Cheating will result in permanent ban"
css_say "Type !rules for server rules"
```

**Message format:**
```
(ADMIN) YourName: message
```

---

### Private Say (Whisper)

Send a private message to a specific player.

```bash
css_psay <#userid or name> <message>
```

**Permission:** `@css/chat`

**Features:**
- Only the target player sees the message
- Useful for private warnings or help
- Doesn't clutter public chat

**Examples:**
```bash
css_psay #123 "Please stop mic spamming"
css_psay PlayerName "You need to join a team"
css_psay @all "This is a private message to everyone"
```

**Target receives:**
```
(ADMIN to you) AdminName: message
```

---

### Center Say

Display a message in the center of all players' screens.

```bash
css_csay <message>
```

**Permission:** `@css/chat`

**Features:**
- Large text in center of screen
- Impossible to miss
- Useful for important announcements

**Examples:**
```bash
css_csay "ROUND STARTS IN 10 SECONDS"
css_csay "FREEZE! Don't move!"
css_csay "Server restarting NOW"
```

**Display:**
- Shows in center of screen
- Large, bold text
- Auto-fades after a few seconds

---

### HUD Say

Display a message on players' HUD (screen overlay).

```bash
css_hsay <message>
```

**Permission:** `@css/chat`

**Features:**
- Message appears on screen overlay
- Less intrusive than center say
- Stays visible longer

**Examples:**
```bash
css_hsay "Tournament starting soon!"
css_hsay "New map voting available"
css_hsay "Visit our website: example.com"
```

---

## Usage Examples

### Announcements

```bash
# Server restart warning
css_cssay "Server will restart in 10 minutes! Save your progress!"

# Center screen countdown
css_csay "5"
# Wait...
css_csay "4"
css_csay "3"
css_csay "2"
css_csay "1"
css_csay "GO!"
```

### Player Communication

```bash
# Private warning
css_psay PlayerName "This is your first warning for chat spam"

# Public announcement
css_say "Everyone please be quiet for the next round"

# Admin coordination
css_asay "I'm spectating the suspicious player in T spawn"
```

### Event Management

```bash
# Tournament announcement
css_cssay "⚠ TOURNAMENT STARTING IN 5 MINUTES ⚠"
css_hsay "Teams, please get ready!"
css_csay "TOURNAMENT BEGINS NOW!"
```

---

## Color Codes

Many chat commands support color codes:

```
{default} - Default chat color
{white} - White
{darkred} - Dark red
{green} - Green
{lightyellow} - Light yellow
{lightblue} - Light blue
{olive} - Olive
{lime} - Lime
{red} - Red
{purple} - Purple
{grey} - Grey
{yellow} - Yellow
{gold} - Gold
{silver} - Silver
{blue} - Blue
{darkblue} - Dark blue
{bluegrey} - Blue grey
{magenta} - Magenta
{lightred} - Light red
{orange} - Orange
```

**Example:**
```bash
css_say "{red}WARNING: {default}No camping in spawn!"
```

---

## Message Targeting

Some commands support player targeting for private messages:

### Supported Targets

- `@all` - All players (private message to each)
- `@ct` - All Counter-Terrorists
- `@t` - All Terrorists
- `@spec` - All spectators
- `#123` - Specific userid
- `PlayerName` - Player by name

**Examples:**
```bash
# Private message to all CTs
css_psay @ct "Defend bombsite A this round"

# Private message to all terrorists
css_psay @t "Rush B with smoke and flash"

# Message to spectators
css_psay @spec "Type !join to play"
```

---

## Best Practices

### When to Use Each Command

**css_asay** (Admin Say):
- Admin coordination
- Discussing player behavior
- Planning admin actions
- Private admin discussions

**css_cssay** (Colored Say):
- Important server announcements
- Event notifications
- Eye-catching messages
- Server information

**css_say** (Say):
- General admin announcements
- Rule reminders
- Warnings to all players
- Admin presence

**css_psay** (Private Say):
- Private warnings
- Individual help
- Direct player communication
- Discretion needed

**css_csay** (Center Say):
- Emergency announcements
- Cannot-miss messages
- Round starts/events
- Countdowns

**css_hsay** (HUD Say):
- Persistent information
- Less urgent announcements
- Server info
- Website/Discord links

---

## Communication Guidelines

### Professional Communication

1. **Be clear and concise** - Don't spam long messages
2. **Use appropriate command** - Don't center-spam trivial messages
3. **Check for typos** - You represent the server
4. **Avoid excessive colors** - Can be hard to read

### Spam Prevention

1. **Don't overuse center say** - Very intrusive for players
2. **Space out announcements** - Don't flood chat
3. **Use HUD say for persistent info** - Less annoying
4. **Coordinate with other admins** - Avoid duplicate messages

### Effective Messaging

**Good Examples:**
```bash
css_cssay "🎯 New map voting system available! Type !mapvote"
css_psay PlayerName "Hey! Please enable your mic or use team chat"
css_asay "Checking player demos for possible aimbot"
```

**Poor Examples:**
```bash
css_csay "hi"  # Don't use center say for trivial messages
css_cssay "a" "b" "c" "d"  # Don't spam center messages
css_say "asdfasdfasdf"  # Unprofessional
```

---

## Silent Mode

Admins can use silent mode to hide their activity:

```bash
css_hide  # Toggle silent mode
```

When in silent mode:
- Chat messages still work
- Admin name might be hidden (depends on config)
- Useful for undercover moderation

---

## Configuration

### Show Activity Type

Controls how admin actions are displayed:

```json
"ShowActivityType": 2
```

**Options:**
- `0` - Hide all admin activity
- `1` - Anonymous ("An admin says...")
- `2` - Show name ("AdminName says...")

---

## Chat Restrictions

### Respecting Gags

Remember that:
- `css_asay` works even if admin is gagged (admin chat)
- Other commands respect communication penalties
- Can't use chat commands while silenced

### Permission Requirements

All chat commands require `@css/chat` permission:

```bash
css_addadmin STEAMID "Name" "@css/chat" 50 0
```

Or add to a group with chat permission:
```bash
css_addgroup "#moderators" "@css/chat,@css/kick" 50
```

---

## Troubleshooting

### Messages not showing

**Check:**
- Do you have `@css/chat` permission?
- Are you silenced/gagged?
- Check console for errors
- Verify player is connected (for css_psay)

### Colors not working

**Check:**
- Use correct color code syntax: `{red}`
- Some commands may not support colors
- Different chat systems handle colors differently

### Players can't see center/HUD messages

**Check:**
- CS2 client-side chat settings
- Conflicting HUD plugins
- Server console for errors

---

## Related Commands

- **[Communication Commands](basecomms)** - Gag, mute, silence players
- **[Base Commands](basecommands)** - Admin management
- **[Ban Commands](basebans)** - Player punishment
