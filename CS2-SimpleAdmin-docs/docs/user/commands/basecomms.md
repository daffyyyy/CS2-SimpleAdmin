---
sidebar_position: 2
---

# Communication Commands

Commands for managing player communication (voice and text chat).

## Overview

CS2-SimpleAdmin provides three types of communication restrictions:

- **Gag** - Blocks text chat only
- **Mute** - Blocks voice chat only
- **Silence** - Blocks both text and voice chat

---

## Gag Commands

### Gag Player

Prevent a player from using text chat.

```bash
css_gag <#userid or name> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_gag #123 30 "Chat spam"
css_gag PlayerName 1440 "Advertising"
css_gag @all 5 "Everyone quiet for 5 minutes"
```

### Add Gag (Offline Player)

Gag a player by SteamID even if they're offline.

```bash
css_addgag <steamid> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_addgag 76561198012345678 60 "Chat abuse"
css_addgag STEAM_1:0:12345678 1440 "Spam"
```

### Ungag Player

Remove a gag from a player.

```bash
css_ungag <steamid or name> [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_ungag PlayerName "Appeal accepted"
css_ungag 76561198012345678 "Mistake"
```

---

## Mute Commands

### Mute Player

Prevent a player from using voice chat.

```bash
css_mute <#userid or name> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_mute #123 30 "Mic spam"
css_mute PlayerName 60 "Loud music"
css_mute @t 5 "T team timeout"
```

### Add Mute (Offline Player)

Mute a player by SteamID even if they're offline.

```bash
css_addmute <steamid> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_addmute 76561198012345678 120 "Voice abuse"
css_addmute STEAM_1:0:12345678 1440 "Mic spam"
```

### Unmute Player

Remove a mute from a player.

```bash
css_unmute <steamid or name> [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_unmute PlayerName "Behavior improved"
css_unmute 76561198012345678 "Time served"
```

---

## Silence Commands

### Silence Player

Block both text and voice chat from a player.

```bash
css_silence <#userid or name> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_silence #123 60 "Complete communication ban"
css_silence PlayerName 1440 "Severe abuse"
```

### Add Silence (Offline Player)

Silence a player by SteamID even if they're offline.

```bash
css_addsilence <steamid> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_addsilence 76561198012345678 120 "Total communication ban"
css_addsilence STEAM_1:0:12345678 0 "Permanent silence"
```

### Unsilence Player

Remove a silence from a player.

```bash
css_unsilence <steamid or name> [reason]
```

**Permission:** `@css/chat`

**Examples:**
```bash
css_unsilence PlayerName "Punishment complete"
css_unsilence 76561198012345678 "Appeal granted"
```

---

## Permission Requirements

All communication commands require the `@css/chat` permission.

| Command | Action | Offline Support |
|---------|--------|----------------|
| `css_gag` | Block text chat | No |
| `css_addgag` | Block text chat | Yes |
| `css_ungag` | Remove text block | Yes |
| `css_mute` | Block voice chat | No |
| `css_addmute` | Block voice chat | Yes |
| `css_unmute` | Remove voice block | Yes |
| `css_silence` | Block both | No |
| `css_addsilence` | Block both | Yes |
| `css_unsilence` | Remove both blocks | Yes |

---

## Communication Penalty Types

### When to Use Each Type

**Gag (Text Only):**
- Chat spam
- Advertising in chat
- Offensive messages
- Spectator camera abuse messages

**Mute (Voice Only):**
- Mic spam
- Loud music/noise
- Voice abuse
- Excessive talking

**Silence (Both):**
- Severe abuse cases
- Players who switch between chat and voice to evade
- Complete communication bans

---

## Configuration Options

### UserMessage Gag Type

In `CS2-SimpleAdmin.json`:

```json
"UserMessageGagChatType": false
```

**Options:**
- `false` - Standard gag implementation (default)
- `true` - Alternative gag using UserMessage system

**Note:** Try switching this if gag commands don't work as expected.

### Notify Penalties on Connect

```json
"NotifyPenaltiesToAdminOnConnect": true
```

When enabled, admins see active communication penalties when they join:
```
[CS2-SimpleAdmin] PlayerName is gagged (30 minutes remaining)
[CS2-SimpleAdmin] PlayerName is muted (1 hour remaining)
```

---

## Checking Penalties

### View Own Penalties

Players can check their own communication penalties:

```bash
css_penalties
css_mypenalties
css_comms
```

Shows:
- Active gags, mutes, and silences
- Duration remaining
- Reason for penalty
- Admin who issued it

### Admin View of Penalties

Use the admin menu or player info command:

```bash
css_who <#userid or name>
```

Shows complete penalty history including communication restrictions.

---

## Time Durations

Common duration values:

| Duration | Minutes | Use Case |
|----------|---------|----------|
| 1 minute | 1 | Quick warning |
| 5 minutes | 5 | Minor spam |
| 15 minutes | 15 | Standard timeout |
| 30 minutes | 30 | Repeated offense |
| 1 hour | 60 | Moderate abuse |
| 6 hours | 360 | Serious abuse |
| 1 day | 1440 | Severe abuse |
| 1 week | 10080 | Extreme cases |
| Permanent | 0 | Reserved for worst cases |

---

## Player Targeting

All communication commands support advanced targeting:

- `@all` - Target all players
- `@ct` - Target all Counter-Terrorists
- `@t` - Target all Terrorists
- `@spec` - Target all spectators
- `#123` - Target by userid
- `PlayerName` - Target by name

**Examples:**
```bash
css_gag @all 1 "Quiet for one minute"
css_mute @t 5 "T team voice timeout"
css_silence @ct 10 "CT team complete silence"
```

---

## Best Practices

### Communication Management

1. **Start with warnings** - Not all chat issues need immediate gag
2. **Use appropriate durations** - Match severity to punishment
3. **Provide reasons** - Helps players understand what they did wrong
4. **Consider silence carefully** - Complete communication ban is harsh

### Gag vs Mute vs Silence

**Progressive Approach:**
1. Verbal warning
2. Gag or mute (specific to offense)
3. Longer gag/mute for repeat offense
4. Silence for continued abuse
5. Temporary ban for extreme cases

### Documentation

1. **Always provide reasons** - Required for appeals
2. **Be specific** - "Mic spam" not just "abuse"
3. **Keep records** - Use admin logs for repeat offenders

---

## Discord Integration

Communication penalties can send Discord notifications when configured:

```json
"DiscordPenaltyGagSettings": [...],
"DiscordPenaltyMuteSettings": [...],
"DiscordPenaltySilenceSettings": [...]
```

Notifications include:
- Player name and SteamID
- Penalty type and duration
- Reason provided
- Admin who issued it

---

## Troubleshooting

### Gag doesn't work

**Try:**
1. Switch `UserMessageGagChatType` in config
2. Ensure player is actually gagged (check with `css_who`)
3. Check for conflicting plugins

### Mute doesn't block voice

**Check:**
- Is sv_talk_enemy_dead configured correctly?
- Are there voice management plugins conflicting?
- Check server console for errors

### Penalties not persistent across maps

**Solution:**
- Penalties should persist automatically
- Check database connection
- Verify MultiServerMode if using multiple servers

### Player can't see their penalties

**Check:**
- Command aliases in Commands.json
- Ensure `css_penalties` is enabled
- Check player chat permissions

---

## Related Commands

- **[Ban Commands](basebans)** - For more serious offenses
- **[Player Commands](playercommands)** - Kick, team switch
- **[Base Commands](basecommands)** - Admin management
