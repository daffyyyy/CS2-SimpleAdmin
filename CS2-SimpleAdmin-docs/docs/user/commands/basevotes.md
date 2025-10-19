---
sidebar_position: 6
---

# Vote Commands

Commands for creating polls and votes on your server.

## Create Vote

Create a custom poll for players to vote on.

```bash
css_vote <question> [option1] [option2] [option3] ...
```

**Permission:** `@css/generic`

**Parameters:**
- `question` - The question to ask players
- `option1, option2, ...` - Vote options (at least 2 required)

---

## Examples

### Simple Yes/No Vote

```bash
css_vote "Should we change map?" "Yes" "No"
```

**Player sees:**
```
Vote: Should we change map?
1. Yes
2. No
```

---

### Multiple Options

```bash
css_vote "Which map should we play next?" "de_dust2" "de_mirage" "de_inferno" "de_nuke"
```

**Player sees:**
```
Vote: Which map should we play next?
1. de_dust2
2. de_mirage
3. de_inferno
4. de_nuke
```

---

### Rule Vote

```bash
css_vote "Should we allow AWPs?" "Yes" "No" "Only one per team"
```

---

### Activity Vote

```bash
css_vote "What should we do?" "Surf" "Deathrun" "Competitive" "Fun Round"
```

---

## How Voting Works

### Player Participation

Players vote by:
1. Opening their chat
2. Typing the number of their choice
3. Or using vote menu (if available)

**Example:**
```
Player: 1  (votes for option 1)
Player: 2  (votes for option 2)
```

### Vote Duration

- Default vote time: ~30 seconds
- Vote timer shows on screen
- Results shown when vote ends

### Vote Results

After voting ends, results are displayed:
```
Vote Results:
1. Yes - 12 votes (60%)
2. No - 8 votes (40%)

Winner: Yes
```

---

## Use Cases

### Map Voting

```bash
css_vote "Next map?" "de_dust2" "de_mirage" "de_inferno"
```

### Rule Changes

```bash
css_vote "Enable friendly fire?" "Yes" "No"
css_vote "Restart round?" "Yes" "No"
```

### Player Punishment

```bash
css_vote "Ban PlayerName for cheating?" "Yes" "No"
css_vote "Kick AFK player?" "Yes" "No"
```

### Fun Rounds

```bash
css_vote "Fun round type?" "Knife only" "Deagle only" "Zeus only" "Normal"
```

### Server Settings

```bash
css_vote "Round time?" "2 minutes" "3 minutes" "5 minutes"
css_vote "Max players?" "10v10" "5v5" "7v7"
```

---

## Best Practices

### Question Clarity

**Good Questions:**
- Clear and concise
- Specific
- Easy to understand

**Examples:**
```bash
✅ css_vote "Change to de_dust2?" "Yes" "No"
❌ css_vote "Map?" "Yes" "No"  # Unclear what map

✅ css_vote "Restart this round?" "Yes" "No"
❌ css_vote "Restart?" "Yes" "No"  # Restart what?
```

### Option Limits

**Recommendations:**
- 2-5 options ideal
- Too many options confuse players
- Keep options brief

**Examples:**
```bash
✅ css_vote "Next map?" "dust2" "mirage" "inferno"
❌ css_vote "Next map?" "de_dust2" "de_mirage" "de_inferno" "de_nuke" "de_vertigo" "de_ancient" "de_anubis"
```

### Timing

**When to use votes:**
- End of round
- Between maps
- During downtime
- Not during active gameplay

**When NOT to use votes:**
- Mid-round
- During clutch situations
- Too frequently

### Vote Spam Prevention

Don't spam votes:
```bash
❌ Multiple votes in quick succession
❌ Overlapping votes
❌ Votes every round
```

Wait for current vote to finish before starting another.

---

## Vote Types

### Administrative Votes

**Map change:**
```bash
css_vote "Change map now?" "Yes" "No"
```

**Server restart:**
```bash
css_vote "Restart server?" "Yes" "No"
```

**Rule enforcement:**
```bash
css_vote "Kick PlayerName?" "Yes" "No"
```

### Gameplay Votes

**Weapon restrictions:**
```bash
css_vote "Disable AWP?" "Yes" "No"
```

**Team scramble:**
```bash
css_vote "Scramble teams?" "Yes" "No"
```

**Round rules:**
```bash
css_vote "Knife round first?" "Yes" "No"
```

### Event Votes

**Tournament:**
```bash
css_vote "Start tournament?" "Yes, start now" "Wait 5 minutes" "No, cancel"
```

**Custom game mode:**
```bash
css_vote "Game mode?" "Hide and Seek" "Gungame" "Surf" "Normal"
```

---

## Limitations

### Technical Limits

- Maximum ~10 options (depends on menu system)
- One vote at a time
- Requires active players to participate

### Permission Required

Only admins with `@css/generic` permission can start votes.

To grant permission:
```bash
css_addadmin STEAMID "Name" "@css/generic" 50 0
```

---

## Vote Results Handling

### Manual Enforcement

Votes don't automatically execute actions. Admins must:

1. **See the results**
2. **Manually execute the winning option**

**Example:**
```bash
# Start vote
css_vote "Change to de_dust2?" "Yes" "No"

# If "Yes" wins, manually change map
css_map de_dust2
```

### Why Manual?

- Prevents abuse
- Allows admin oversight
- Gives control over execution

---

## Advanced Usage

### Combining with Commands

Use votes to decide, then execute:

```bash
# Vote on map
css_vote "Next map?" "dust2" "mirage" "inferno"
# If dust2 wins:
css_map de_dust2

# Vote on player kick
css_vote "Kick PlayerName?" "Yes" "No"
# If Yes wins:
css_kick PlayerName "Voted to be kicked"
```

### Sequential Votes

Run multiple votes for complex decisions:

```bash
# First vote: Mode
css_vote "Game mode?" "Competitive" "Casual"

# If Competitive wins, second vote:
css_vote "Round time?" "2 min" "3 min" "5 min"
```

---

## Configuration

Check if vote commands are enabled in:
```
addons/counterstrikesharp/configs/plugins/CS2-SimpleAdmin/Commands.json
```

```json
{
  "Commands": {
    "css_vote": {
      "Aliases": [
        "css_vote"
      ]
    }
  }
}
```

To disable votes, remove all aliases:
```json
{
  "Commands": {
    "css_vote": {
      "Aliases": []
    }
  }
}
```

---

## Troubleshooting

### Vote doesn't start

**Check:**
- Do you have `@css/generic` permission?
- Is command enabled in Commands.json?
- Are there at least 2 options?

### Players can't vote

**Check:**
- Vote menu is showing
- Players know how to vote (type number in chat)
- Vote hasn't already ended

### Vote results not showing

**Check:**
- Wait for vote to complete
- Check server console
- Ensure voting system is working

---

## Permission Requirements

| Command | Permission | Description |
|---------|------------|-------------|
| `css_vote` | `@css/generic` | Create votes/polls |

---

## Tips

### Effective Polling

1. **Ask clear questions** - No ambiguity
2. **Limit options** - 2-4 is ideal
3. **Time it right** - Between rounds
4. **Follow through** - Execute winning option
5. **Don't overuse** - Votes lose impact if spammed

### Community Engagement

Use votes to:
- Involve community in decisions
- Gauge player preferences
- Create democratic server atmosphere
- Get feedback on changes

### Example Scenarios

**New map test:**
```bash
css_vote "Try new map cs_office?" "Yes" "No"
```

**Event planning:**
```bash
css_vote "Tournament this weekend?" "Saturday" "Sunday" "No thanks"
```

**Rule feedback:**
```bash
css_vote "Keep no-AWP rule?" "Yes" "No" "Only limit to 2"
```

---

## Related Commands

- **[Base Commands](basecommands)** - Server management
- **[Chat Commands](basechat)** - Announcements
- **[Player Commands](playercommands)** - Player actions
