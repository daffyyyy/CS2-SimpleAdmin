---
sidebar_position: 1
---

# Ban Commands

Commands for managing player bans.

## Ban Player

Ban a player currently on the server.

```bash
css_ban <#userid or name> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/ban`

**Examples:**
```bash
css_ban @all 60 "Timeout for everyone"
css_ban #123 1440 "Hacking - 1 day ban"
css_ban PlayerName 0 "Permanent ban for cheating"
css_ban @ct 30 "CT team timeout"
```

**Notes:**
- Time in minutes (0 = permanent)
- Supports player targeting (@all, @ct, @t, #userid, name)
- Reason is optional but recommended

---

## Add Ban (Offline Player)

Ban a player by SteamID even if they're not online.

```bash
css_addban <steamid> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/ban`

**Examples:**
```bash
css_addban STEAM_1:0:12345678 1440 "Ban evasion"
css_addban 76561198012345678 10080 "Hacking - 7 day ban"
css_addban STEAM_1:1:87654321 0 "Permanent ban"
```

**Supported SteamID formats:**
- SteamID64: `76561198012345678`
- SteamID: `STEAM_1:0:12345678`
- SteamID3: `[U:1:12345678]`

---

## Ban IP Address

Ban an IP address.

```bash
css_banip <ip> [time in minutes/0 perm] [reason]
```

**Permission:** `@css/ban`

**Examples:**
```bash
css_banip 192.168.1.100 1440 "Ban evasion attempt"
css_banip 10.0.0.5 0 "Persistent troublemaker"
```

**Notes:**
- Useful for preventing ban evasion
- Can be combined with SteamID bans
- Check config for `BanType` setting (SteamID, IP, or Both)

---

## Unban Player

Remove a ban from a player.

```bash
css_unban <steamid or name or ip> [reason]
```

**Permission:** `@css/unban`

**Examples:**
```bash
css_unban 76561198012345678 "Appeal accepted"
css_unban STEAM_1:0:12345678 "Ban lifted"
css_unban 192.168.1.100 "Wrong person banned"
css_unban PlayerName "Mistake"
```

**Notes:**
- Works with SteamID, IP, or player name
- Unban reason is logged
- Can unban offline players

---

## Warn Player

Issue a warning to a player.

```bash
css_warn <#userid or name> [reason]
```

**Permission:** `@css/kick`

**Examples:**
```bash
css_warn #123 "Mic spam"
css_warn PlayerName "Language"
css_warn @all "Final warning"
```

**Notes:**
- Warnings can accumulate
- Auto-escalation to bans based on `WarnThreshold` config
- Example: 3 warnings = 1 hour ban, 4 warnings = 2 hour ban

**Warning Threshold Configuration:**
```json
"WarnThreshold": {
  "3": "css_addban STEAMID64 60 \"3 warnings\"",
  "4": "css_ban #USERID 120 \"4 warnings\""
}
```

---

## Unwarn Player

Remove a warning from a player.

```bash
css_unwarn <steamid or name>
```

**Permission:** `@css/kick`

**Examples:**
```bash
css_unwarn 76561198012345678
css_unwarn PlayerName
```

**Notes:**
- Removes the most recent warning
- Helps manage warning thresholds
- Can be used for offline players

---

## Permission Requirements

| Command | Required Permission | Description |
|---------|-------------------|-------------|
| `css_ban` | `@css/ban` | Ban online players |
| `css_addban` | `@css/ban` | Ban offline players by SteamID |
| `css_banip` | `@css/ban` | Ban IP addresses |
| `css_unban` | `@css/unban` | Remove bans |
| `css_warn` | `@css/kick` | Issue warnings |
| `css_unwarn` | `@css/kick` | Remove warnings |

## Ban Types

Configure ban behavior in `CS2-SimpleAdmin.json`:

```json
"BanType": 1
```

**Options:**
- `1` - SteamID only (default)
- `2` - IP only
- `3` - Both SteamID and IP

## Time Durations

Common time values:

| Duration | Minutes | Description |
|----------|---------|-------------|
| 1 minute | 1 | Very short timeout |
| 5 minutes | 5 | Short timeout |
| 15 minutes | 15 | Medium timeout |
| 1 hour | 60 | Standard timeout |
| 1 day | 1440 | Daily ban |
| 1 week | 10080 | Weekly ban |
| 2 weeks | 20160 | Bi-weekly ban |
| 1 month | 43200 | Monthly ban |
| Permanent | 0 | Never expires |

## Player Targeting

All ban commands support advanced targeting:

- `@all` - Target all players
- `@ct` - Target all Counter-Terrorists
- `@t` - Target all Terrorists
- `@spec` - Target all spectators
- `#123` - Target by userid
- `PlayerName` - Target by name (partial match)

## Best Practices

### Banning

1. **Always provide a reason** - Helps with appeals and record keeping
2. **Use appropriate durations** - Don't permaban for minor offenses
3. **Check ban history** - Use `css_who` to see if player has priors
4. **Consider warnings first** - Give players a chance to improve

### Warning System

1. **Be consistent** - Use warnings for minor offenses
2. **Configure thresholds** - Set up auto-escalation in config
3. **Communicate clearly** - Let players know why they're warned
4. **Review regularly** - Check warning history with `css_warns`

### Multi-Account Detection

When `CheckMultiAccountsByIp` is enabled:
- Plugin detects multiple accounts from same IP
- Sends Discord notifications if configured
- Helps identify ban evasion

## Troubleshooting

### Ban doesn't work

**Check:**
- Do you have `@css/ban` permission?
- Is the SteamID format correct?
- Check server console for errors

### Player rejoins after ban

**Check:**
- Is `MultiServerMode` enabled if using multiple servers?
- Is the database shared across servers?
- Check ban type configuration (SteamID vs IP)

### Warning threshold not working

**Check:**
- Is `WarnThreshold` configured correctly?
- Are the command formats correct in config?
- Check server console for execution errors

## Related Commands

- **[Communication Commands](basecomms)** - Mute, gag, silence
- **[Player Commands](playercommands)** - Kick, slay, etc.
- **[Base Commands](basecommands)** - Admin management
