---
sidebar_position: 5
---

# Player Commands

Commands for managing and manipulating players on your server.

:::note
Many of these commands are included in the base plugin. For extended fun commands (god mode, noclip, freeze, etc.), see the [Fun Commands Module](../../modules/funcommands).
:::

## Player Management

### Slay Player

Kill a player instantly.

```bash
css_slay <#userid or name>
```

**Permission:** `@css/slay`

**Examples:**
```bash
css_slay #123
css_slay PlayerName
css_slay @ct  # Slay all CTs
css_slay @t   # Slay all terrorists
```

**Use cases:**
- Punishment for rule breaking
- Ending rounds quickly
- Removing camping players

---

### Slap Player

Slap a player, dealing damage and pushing them.

```bash
css_slap <#userid or name> [damage]
```

**Permission:** `@css/slay`

**Parameters:**
- `damage` - HP damage to deal (default: 0)

**Examples:**
```bash
css_slap #123      # Slap with no damage
css_slap PlayerName 10  # Slap for 10 HP damage
css_slap @all 5    # Slap everyone for 5 damage
```

**Effects:**
- Player is pushed in a random direction
- Optional damage dealt
- Makes slap sound

**Use cases:**
- Funny punishment
- Getting player attention
- Moving AFK players

---

## Player Attributes

### Set Player Health

Set a player's health points.

```bash
css_hp <#userid or name> <health>
```

**Permission:** `@css/slay`

**Examples:**
```bash
css_hp #123 100     # Set to full health
css_hp PlayerName 1  # Set to 1 HP
css_hp @ct 200      # Give all CTs 200 HP
```

**Valid range:** 1 - 999+

---

### Set Player Speed

Modify a player's movement speed.

```bash
css_speed <#userid or name> <speed>
```

**Permission:** `@css/slay`

**Parameters:**
- `speed` - Speed multiplier (1.0 = normal, 2.0 = double speed, 0.5 = half speed)

**Examples:**
```bash
css_speed #123 1.5    # 150% speed
css_speed PlayerName 0.5   # 50% speed (slow motion)
css_speed @all 2.0    # Double speed for everyone
css_speed #123 1.0    # Reset to normal speed
```

**Common values:**
- `0.5` - Slow motion
- `1.0` - Normal (default)
- `1.5` - Fast
- `2.0` - Very fast
- `3.0` - Extremely fast

**Note:** Speed persists across respawns until reset.

---

### Set Player Gravity

Modify a player's gravity.

```bash
css_gravity <#userid or name> <gravity>
```

**Permission:** `@css/slay`

**Parameters:**
- `gravity` - Gravity multiplier (1.0 = normal, 0.5 = moon jump, 2.0 = heavy)

**Examples:**
```bash
css_gravity #123 0.5   # Moon jump
css_gravity PlayerName 2.0  # Heavy gravity
css_gravity @all 0.1   # Super jump for everyone
css_gravity #123 1.0   # Reset to normal
```

**Common values:**
- `0.1` - Super high jumps
- `0.5` - Moon gravity
- `1.0` - Normal (default)
- `2.0` - Heavy/fast falling

**Note:** Gravity persists across respawns until reset.

---

### Set Player Money

Set a player's money amount.

```bash
css_money <#userid or name> <amount>
```

**Permission:** `@css/slay`

**Examples:**
```bash
css_money #123 16000  # Max money
css_money PlayerName 0     # Remove all money
css_money @ct 10000   # Give all CTs $10,000
```

**Valid range:** 0 - 65535 (CS2 engine limit)

---

## Team Management

### Switch Player Team

Move a player to a different team.

```bash
css_team <#userid or name> [ct/t/spec] [-k]
```

**Permission:** `@css/kick`

**Parameters:**
- `ct` - Counter-Terrorist team
- `t` - Terrorist team
- `spec` - Spectators
- `-k` - Kill player during switch (optional)

**Examples:**
```bash
css_team #123 ct       # Move to CT
css_team PlayerName t  # Move to T
css_team @spec t       # Move all spectators to T
css_team #123 ct -k    # Move to CT and kill
```

**Configuration:**
```json
"TeamSwitchType": 1
```

Determines team switch behavior.

---

### Rename Player

Temporarily rename a player.

```bash
css_rename <#userid or name> <new name>
```

**Permission:** `@css/kick`

**Examples:**
```bash
css_rename #123 "NewName"
css_rename PlayerName "RenamedPlayer"
css_rename @all "Everyone"
```

**Notes:**
- Rename is temporary (resets on reconnect)
- For permanent rename, use `css_prename`

---

### Permanent Rename

Permanently force a player's name.

```bash
css_prename <#userid or name> <new name>
```

**Permission:** `@css/ban`

**Examples:**
```bash
css_prename #123 "EnforcedName"
css_prename PlayerName "NewIdentity"
```

**Notes:**
- Name is enforced even after reconnect
- Stored in database
- Player cannot change it
- Useful for offensive names

---

## Weapon Management

### Give Weapon

Give a weapon to a player.

```bash
css_give <#userid or name> <weapon>
```

**Permission:** `@css/cheats`

**Weapon names:**
- Rifles: `ak47`, `m4a1`, `m4a1_silencer`, `aug`, `sg556`, `awp`
- SMGs: `mp5sd`, `mp7`, `mp9`, `p90`, `ump45`
- Heavy: `nova`, `xm1014`, `mag7`, `m249`, `negev`
- Pistols: `deagle`, `elite`, `fiveseven`, `glock`, `hkp2000`, `p250`, `tec9`, `usp_silencer`
- Grenades: `flashbang`, `hegrenade`, `smokegrenade`, `molotov`, `incgrenade`, `decoy`
- Equipment: `kevlar`, `assaultsuit`, `defuser`, `knife`

**Examples:**
```bash
css_give #123 awp
css_give PlayerName ak47
css_give @ct m4a1
css_give @all deagle
```

---

### Strip Weapons

Remove all weapons from a player.

```bash
css_strip <#userid or name>
```

**Permission:** `@css/slay`

**Examples:**
```bash
css_strip #123
css_strip PlayerName
css_strip @t  # Disarm all terrorists
```

**Effects:**
- Removes all weapons
- Leaves player with knife only
- Removes grenades and equipment

---

## Teleportation

### Teleport to Player

Teleport yourself to another player.

```bash
css_tp <#userid or name>
css_tpto <#userid or name>
css_goto <#userid or name>
```

**Permission:** `@css/kick`

**Examples:**
```bash
css_tp #123
css_goto PlayerName
```

**Use cases:**
- Checking player behavior
- Admin help
- Spectating suspicious players

---

### Teleport Player to You

Bring a player to your location.

```bash
css_bring <#userid or name>
css_tphere <#userid or name>
```

**Permission:** `@css/kick`

**Examples:**
```bash
css_bring #123
css_tphere PlayerName
css_bring @all  # Bring everyone to you
```

**Use cases:**
- Moving stuck players
- Gathering players
- Admin events

---

### Respawn Player

Respawn a dead player.

```bash
css_respawn <#userid or name>
```

**Permission:** `@css/cheats`

**Examples:**
```bash
css_respawn #123
css_respawn PlayerName
css_respawn @ct  # Respawn all dead CTs
```

**Notes:**
- Player spawns at spawn point
- Equipped with default weapons
- Can break competitive balance

---

## Player Targeting

All player commands support advanced targeting:

### Target Syntax

- `@all` - All players
- `@ct` - All Counter-Terrorists
- `@t` - All Terrorists
- `@spec` - All spectators
- `@alive` - All alive players
- `@dead` - All dead players
- `@bot` - All bots
- `@human` - All human players
- `@me` - Yourself
- `#123` - Specific user ID
- `PlayerName` - By name (partial match supported)

### Multiple Targets

```bash
css_slay @ct  # Kills all CTs
css_hp @all 200  # Give everyone 200 HP
css_speed @t 2.0  # Make all Ts fast
```

---

## Permission Requirements

| Command | Required Permission | Description |
|---------|-------------------|-------------|
| `css_slay` | `@css/slay` | Kill players |
| `css_slap` | `@css/slay` | Slap players |
| `css_hp` | `@css/slay` | Set health |
| `css_speed` | `@css/slay` | Modify speed |
| `css_gravity` | `@css/slay` | Modify gravity |
| `css_money` | `@css/slay` | Set money |
| `css_team` | `@css/kick` | Change team |
| `css_rename` | `@css/kick` | Temporary rename |
| `css_prename` | `@css/ban` | Permanent rename |
| `css_give` | `@css/cheats` | Give weapons |
| `css_strip` | `@css/slay` | Remove weapons |
| `css_tp` | `@css/kick` | Teleport to player |
| `css_bring` | `@css/kick` | Bring player |
| `css_respawn` | `@css/cheats` | Respawn players |

---

## Best Practices

### Punishment Commands

**Slay:**
- Use for rule violations
- Better than kick for minor issues
- Allows player to stay and learn

**Slap:**
- Lighter punishment
- Good for warnings
- Can be funny/entertaining

### Gameplay Modification

**HP/Speed/Gravity:**
- Use for events/fun rounds
- Don't abuse during competitive play
- Reset to normal after use

**Respawn:**
- Very disruptive to gameplay
- Use sparingly
- Good for fixing bugs/mistakes

### Team Management

**Team switching:**
- Balance teams fairly
- Don't abuse for winning
- Use `-k` flag for competitive integrity

---

## Configuration

### Team Switch Behavior

```json
"TeamSwitchType": 1
```

Controls how team switching works.

---

## Troubleshooting

### Speed/Gravity not persisting

**Solution:** These are maintained by a timer. If they reset:
- Check server console for errors
- Ensure plugin is loaded correctly
- Try reapplying the modification

### Can't teleport

**Check:**
- Target player is connected
- You have correct permissions
- Both players are valid

### Give weapon not working

**Check:**
- Weapon name is correct
- Player is alive
- Player has inventory space

---

## Related Commands

- **[Fun Commands Module](../../modules/funcommands)** - Extended fun commands (freeze, god mode, noclip)
- **[Ban Commands](basebans)** - Punishment commands
- **[Base Commands](basecommands)** - Server management
