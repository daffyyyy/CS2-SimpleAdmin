---
sidebar_position: 2
---

# Fun Commands Module

Add entertaining and powerful player manipulation commands to your server.

## Overview

The Fun Commands module extends CS2-SimpleAdmin with commands for god mode, noclip, freeze, respawn, weapon management, and player attribute modification.

**Module Name:** `CS2-SimpleAdmin_FunCommands`

---

## Features

- ⭐ God Mode - Make players invincible
- 👻 No Clip - Allow players to fly through walls
- 🧊 Freeze/Unfreeze - Freeze players in place
- 🔄 Respawn - Bring dead players back
- 🔫 Give Weapons - Provide any weapon to players
- 🗑️ Strip Weapons - Remove all weapons
- ❤️ Set HP - Modify player health
- ⚡ Set Speed - Change movement speed
- 🌙 Set Gravity - Modify gravity
- 💰 Set Money - Adjust player money
- 📏 Resize Player - Change player model size

---

## Installation

### Prerequisites

- CS2-SimpleAdmin installed and working
- CS2-SimpleAdminApi.dll in shared folder

### Install Steps

1. **Download** the module from releases

2. **Extract** to your server:
   ```
   game/csgo/addons/counterstrikesharp/plugins/CS2-SimpleAdmin_FunCommands/
   ```

3. **Restart** your server or reload plugins:
   ```
   css_plugins reload
   ```

4. **Verify** the module loaded:
   - Check server console for load message
   - Try `css_admin` and look for "Fun Commands" menu

---

## Commands

### God Mode

Toggle god mode (invincibility) for a player.

```bash
css_god <#userid or name>
css_godmode <#userid or name>
```

**Permission:** `@css/cheats`

**Examples:**
```bash
css_god #123
css_god PlayerName
css_god @all  # Toggle god mode for everyone
```

**Effects:**
- Player takes no damage
- Toggles on/off with each use

---

### No Clip

Enable noclip mode (fly through walls).

```bash
css_noclip <#userid or name>
```

**Permission:** `@css/cheats`

**Examples:**
```bash
css_noclip #123
css_noclip PlayerName
```

**Effects:**
- Player can fly
- Can pass through walls
- Gravity disabled
- Toggles on/off with each use

---

### Freeze

Freeze a player in place.

```bash
css_freeze <#userid or name> [duration]
```

**Permission:** `@css/slay`

**Parameters:**
- `duration` - Freeze duration in seconds (optional, default: permanent until unfreeze)

**Examples:**
```bash
css_freeze #123        # Freeze permanently
css_freeze PlayerName 30  # Freeze for 30 seconds
css_freeze @t 10       # Freeze all terrorists for 10 seconds
```

**Effects:**
- Player cannot move
- Player cannot shoot
- Auto-unfreezes after duration (if specified)

---

### Unfreeze

Unfreeze a frozen player.

```bash
css_unfreeze <#userid or name>
```

**Permission:** `@css/slay`

**Examples:**
```bash
css_unfreeze #123
css_unfreeze PlayerName
css_unfreeze @all  # Unfreeze everyone
```

---

### Respawn

Respawn a dead player at last death position.

```bash
css_respawn <#userid or name>
```

**Permission:** `@css/cheats`

**Examples:**
```bash
css_respawn #123
css_respawn PlayerName
css_respawn @dead  # Respawn all dead players
```

**Effects:**
- Player spawns at death point
- Gets default weapons
- Joins their team

---

### Give Weapon

Give a weapon to a player.

```bash
css_give <#userid or name> <weapon>
```

**Permission:** `@css/cheats`

**Weapon names:**

**Rifles:**
- `weapon_ak47` or `ak47`
- `weapon_m4a1` or `m4a1`
- `weapon_m4a1_silencer` or `m4a1_silencer`
- `weapon_awp` or `awp`
- `weapon_aug` or `aug`
- `weapon_sg556` or `sg556`
- `weapon_ssg08` or `ssg08` (Scout)
- `weapon_g3sg1` or `g3sg1`
- `weapon_scar20` or `scar20`

**SMGs:**
- `weapon_mp5sd` or `mp5sd`
- `weapon_mp7` or `mp7`
- `weapon_mp9` or `mp9`
- `weapon_mac10` or `mac10`
- `weapon_p90` or `p90`
- `weapon_ump45` or `ump45`
- `weapon_bizon` or `bizon`

**Heavy:**
- `weapon_nova` or `nova`
- `weapon_xm1014` or `xm1014`
- `weapon_mag7` or `mag7`
- `weapon_sawedoff` or `sawedoff`
- `weapon_m249` or `m249`
- `weapon_negev` or `negev`

**Pistols:**
- `weapon_deagle` or `deagle`
- `weapon_elite` or `elite` (Dual Berettas)
- `weapon_fiveseven` or `fiveseven`
- `weapon_glock` or `glock`
- `weapon_hkp2000` or `hkp2000`
- `weapon_p250` or `p250`
- `weapon_usp_silencer` or `usp_silencer`
- `weapon_tec9` or `tec9`
- `weapon_cz75a` or `cz75a`
- `weapon_revolver` or `revolver`

**Grenades:**
- `weapon_flashbang` or `flashbang`
- `weapon_hegrenade` or `hegrenade`
- `weapon_smokegrenade` or `smokegrenade`
- `weapon_molotov` or `molotov`
- `weapon_incgrenade` or `incgrenade`
- `weapon_decoy` or `decoy`

**Equipment:**
- `weapon_knife` or `knife`
- `weapon_taser` or `taser`
- `item_defuser` or `defuser`
- `item_kevlar` or `kevlar`
- `item_assaultsuit` or `assaultsuit`

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

---

### Set HP

Set a player's health.

```bash
css_hp <#userid or name> <health>
```

**Permission:** `@css/slay`

**Parameters:**
- `health` - Health amount (1-999+)

**Examples:**
```bash
css_hp #123 100    # Full health
css_hp PlayerName 200  # 200 HP
css_hp @all 1      # 1 HP everyone
```

**Common values:**
- `1` - 1 HP (one-shot mode)
- `100` - Normal health
- `200` - Double health
- `500` - Tank mode

---

### Set Speed

Modify a player's movement speed.

```bash
css_speed <#userid or name> <speed>
```

**Permission:** `@css/slay`

**Parameters:**
- `speed` - Speed multiplier (0.1 - 10.0)
  - `1.0` = Normal speed
  - `2.0` = Double speed
  - `0.5` = Half speed

**Examples:**
```bash
css_speed #123 1.5    # 50% faster
css_speed PlayerName 0.5   # Slow motion
css_speed @all 2.0    # Everyone fast
css_speed #123 1.0    # Reset to normal
```

**Common values:**
- `0.5` - Slow motion mode
- `1.0` - Normal (reset)
- `1.5` - Fast mode
- `2.0` - Super fast
- `3.0` - Extremely fast

---

### Set Gravity

Modify a player's gravity.

```bash
css_gravity <#userid or name> <gravity>
```

**Permission:** `@css/slay`

**Parameters:**
- `gravity` - Gravity multiplier (0.1 - 10.0)
  - `1.0` = Normal gravity
  - `0.5` = Moon jump
  - `2.0` = Heavy

**Examples:**
```bash
css_gravity #123 0.5   # Moon jump
css_gravity PlayerName 0.1  # Super jump
css_gravity @all 2.0   # Heavy gravity
css_gravity #123 1.0   # Reset to normal
```

**Common values:**
- `0.1` - Super high jumps
- `0.5` - Moon gravity
- `1.0` - Normal (reset)
- `2.0` - Heavy/fast falling

---

### Set Money

Set a player's money amount.

```bash
css_money <#userid or name> <amount>
```

**Permission:** `@css/slay`

**Parameters:**
- `amount` - Money amount (0-65535)

**Examples:**
```bash
css_money #123 16000  # Max money
css_money PlayerName 0     # Remove all money
css_money @ct 10000   # Give all CTs $10,000
```

---

### Resize Player

Change a player's model size.

```bash
css_resize <#userid or name> <scale>
```

**Permission:** `@css/slay`

**Parameters:**
- `scale` - Size scale (0.1 - 10.0)
  - `1.0` = Normal size
  - `0.5` = Half size
  - `2.0` = Double size

**Examples:**
```bash
css_resize #123 0.5   # Tiny player
css_resize PlayerName 2.0  # Giant player
css_resize #123 1.0   # Reset to normal
```

**Common values:**
- `0.5` - Tiny mode
- `1.0` - Normal (reset)
- `1.5` - Big
- `2.0` - Giant

---

## Configuration

Configuration file location:
```
addons/counterstrikesharp/configs/plugins/CS2-SimpleAdmin_FunCommands/CS2-SimpleAdmin_FunCommands.json
```

### Default Configuration

```json
{
  "Version": 1,
  "GodCommands": ["css_god", "css_godmode"],
  "NoclipCommands": ["css_noclip"],
  "FreezeCommands": ["css_freeze"],
  "UnfreezeCommands": ["css_unfreeze"],
  "RespawnCommands": ["css_respawn"],
  "GiveCommands": ["css_give"],
  "StripCommands": ["css_strip"],
  "HpCommands": ["css_hp"],
  "SpeedCommands": ["css_speed"],
  "GravityCommands": ["css_gravity"],
  "MoneyCommands": ["css_money"],
  "ResizeCommands": ["css_resize"]
}
```

### Customizing Commands

**Add aliases:**
```json
"GodCommands": ["css_god", "css_godmode", "css_immortal"]
```

**Disable feature:**
```json
"GodCommands": []
```

**Rename command:**
```json
"NoclipCommands": ["css_fly"]
```

---

## Admin Menu Integration

The module automatically adds a "Fun Commands" category to the admin menu with these options:

- God Mode
- No Clip
- Freeze
- Respawn
- Give Weapon
- Strip Weapons
- Set HP
- Set Speed
- Set Gravity
- Set Money
- Resize Player

**Access menu:**
```bash
css_admin  # Navigate to "Fun Commands"
```

---

## Permission System

### Permission Override

Admins can override command permissions using CounterStrikeSharp's admin system.

**Example:**
If you want VIPs to use god mode:

1. **In admin config**, add permission override for `css_god`:
   ```json
   {
     "css_god": ["@css/vip"]
   }
   ```

2. **VIPs will now see God Mode** in the menu

---

## Permissions Required

| Command | Default Permission | Description |
|---------|------------------|-------------|
| `css_god` | `@css/cheats` | God mode |
| `css_noclip` | `@css/cheats` | No clip |
| `css_freeze` | `@css/slay` | Freeze players |
| `css_unfreeze` | `@css/slay` | Unfreeze players |
| `css_respawn` | `@css/cheats` | Respawn players |
| `css_give` | `@css/cheats` | Give weapons |
| `css_strip` | `@css/slay` | Strip weapons |
| `css_hp` | `@css/slay` | Set health |
| `css_speed` | `@css/slay` | Set speed |
| `css_gravity` | `@css/slay` | Set gravity |
| `css_money` | `@css/slay` | Set money |
| `css_resize` | `@css/slay` | Resize player |

---

## Use Cases

### Fun Rounds

```bash
# Low gravity, high speed round
css_gravity @all 0.3
css_speed @all 1.5

# One-shot mode
css_hp @all 1
css_give @all deagle

# Tiny players
css_resize @all 0.5
```

### Admin Events

```bash
# Hide and seek (seekers)
css_speed @ct 1.5
css_hp @ct 200

# Hide and seek (hiders)
css_resize @t 0.5
css_speed @t 0.8
```

### Testing & Debug

```bash
# Test map navigation
css_noclip @me
css_god @me

# Test weapon balance
css_give @me awp
css_hp @me 100
```

---

## Best Practices

### Competitive Balance

1. **Don't use during serious matches** - Breaks game balance
2. **Announce fun rounds** - Let players know it's for fun
3. **Reset after use** - Return to normal settings
4. **Save for appropriate times** - End of night, special events

### Reset Commands

Always reset modifications after fun rounds:

```bash
css_speed @all 1.0
css_gravity @all 1.0
css_resize @all 1.0
```

### Permission Management

1. **Limit @css/cheats** - Only trusted admins
2. **@css/slay is safer** - For HP/speed/gravity
3. **Monitor usage** - Check logs for abuse

---

## Troubleshooting

### Speed/Gravity not persisting

**Solution:**
- These are maintained by a repeating timer
- If they reset, reapply them
- Check server console for timer errors

### God mode not working

**Check:**
- Is player alive?
- Check console for errors
- Try toggling off and on

### Can't give weapons

**Check:**
- Correct weapon name
- Player is alive
- Player has inventory space

### Noclip doesn't work

**Check:**
- Player must be alive
- sv_cheats doesn't need to be enabled
- Check console for errors

---

## Module Development

This module serves as a **reference implementation** for creating CS2-SimpleAdmin modules.

**Key concepts demonstrated:**
- Command registration from configuration
- Menu creation with SimpleAdmin API
- Per-player translation support
- Proper cleanup on module unload
- Code organization using partial classes

**[View source code](https://github.com/daffyyyy/CS2-SimpleAdmin/tree/main/Modules/CS2-SimpleAdmin_FunCommands)** for implementation details.

---

## Translations

The module includes translations for 13 languages:

- English (en)
- Polish (pl)
- Russian (ru)
- Portuguese (pt)
- And 9 more...

Translation files location:
```
plugins/CS2-SimpleAdmin_FunCommands/lang/
```

---

## Related Documentation

- **[Player Commands](../user/commands/playercommands)** - Core player commands
- **[Module Development](development)** - Create your own modules
- **[API Reference](../developer/api/overview)** - CS2-SimpleAdmin API

---

## Version History

**v1.0.0** - Initial release
- God mode
- Noclip
- Freeze/Unfreeze
- Respawn
- Give/Strip weapons
- HP/Speed/Gravity/Money
- Resize player
- Admin menu integration
- 13 language support

---

## Support

**Issues:** [GitHub Issues](https://github.com/daffyyyy/CS2-SimpleAdmin/issues)

**Questions:** [GitHub Discussions](https://github.com/daffyyyy/CS2-SimpleAdmin/discussions)
