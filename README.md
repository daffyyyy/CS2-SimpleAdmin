# CS2-SimpleAdmin

### Description
Manage your Counter-Strike 2 server by simple commands :) 

### Info
~~It's only plugin base~~, I don't have much time for more extensive development, so if you want to help, do it :)
**The plugin will be developed as much as possible**, so it is no longer just a base for other plugins

### Commands
```js
- css_addadmin <steamid> <name> <flags/groups> <immunity> [time in minutes] - Add admin by steamid // @css/root
- css_deladmin <steamid> - Delete admin by steamid // @css/root
- css_reladmin - Reload sql admins // @css/root
- css_admin - Display all admin commands // @css/generic
- css_who <#userid or name>  - Display informations about player // @css/generic
- css_players - Display player list // @css/generic
- css_ban <#userid or name> [time in minutes/0 perm] [reason] - Ban player // @css/ban
- css_addban <steamid> [time in minutes/0 perm] [reason] - Ban player via steamid64 // @css/ban
- css_banip <ip> [time in minutes/0 perm] [reason] - Ban player via IP address // @css/ban
- css_unban <steamid or name or ip> - Unban player // @css/unban
- css_kick <#userid or name> [reason] - Kick player / @css/kick
- css_gag <#userid or name> [time in minutes/0 perm] [reason] - Gag player // @css/chat
- css_addgag <steamid> [time in minutes/0 perm] [reason] - Gag player via steamid64 // @css/chat
- css_ungag <steamid or name> - Ungag player // @css/chat
- css_mute <#userid or name> [time in minutes/0 perm] [reason] - Mute player // @css/chat
- css_addmute <steamid> [time in minutes/0 perm] [reason] - Mute player via steamid64 // @css/chat
- css_unmute <steamid or name> - Unmute player // @css/chat
- css_give <#userid or name> <weapon> - Give weapon to player // @css/cheats
- css_strip <#userid or name> - Takes all of the player weapons // @css/slay
- css_hp <#userid or name> [health] - Set player health // @css/slay
- css_speed <#userid or name> [speed] - Set player speed // @css/slay
- css_god <#userid or name> - Toggle godmode for player // @css/cheats
- css_slay <#userid or name> - Kill player // @css/slay
- css_slap <#userid or name> [damage] - Slap player // @css/slay
- css_team <#userid or name> [<ct/tt/spec/swap>] [-k] - Change player team (swap - swap player team, -k - kill player) // @css/kick
- css_vote <"Question?"> ["Answer1"] ["Answer2"] ... - Create vote // @css/generic
- css_map <mapname> - Change map // @css/changemap
- css_wsmap <name or id> - Change workshop map // @css/changemap
- css_asay <message> - Say message to all admins // @css/chat
- css_say <message> - Say message as admin in chat // @css/chat
- css_psay <#userid or name> <message> - Sends private message to player // @css/chat
- css_csay <message> - Say message as admin in center // @css/chat
- css_hsay <message> - Say message as admin in hud // @css/chat
- css_noclip <#userid or name> - Toggle noclip for player // @css/cheats
- css_freeze <#userid or name> [duration] - Freeze player // @css/slay
- css_unfreeze <#userid or name> - Unfreeze player // @css/slay
- css_respawn <#userid or name> - Respawn player // @css/cheats
- css_cvar <cvar> <value> - Change cvar value // @css/cvar
- css_rcon <command> - Run command as server // @css/rcon
- css_give <#userid or name> <WeaponName> - Gives a weapon to a Player // @css/give

- team_chat @Message - Say message to all admins // @css/chat
```

### Requirments
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/) **tested on v144**
- MySQL **tested on MySQL (MariaDB) Server version: 10.11.4-MariaDB-1~deb12u1 Debian 12**


### Configuration
After first launch, u need to configure plugin in  addons/counterstrikesharp/configs/plugins/CS2-SimpleAdmin/CS2-SimpleAdmin.json

### Colors
```
        public static char Default = '\x01';
        public static char White = '\x01';
        public static char Darkred = '\x02';
        public static char Green = '\x04';
        public static char LightYellow = '\x03';
        public static char LightBlue = '\x03';
        public static char Olive = '\x05';
        public static char Lime = '\x06';
        public static char Red = '\x07';
        public static char Purple = '\x03';
        public static char Grey = '\x08';
        public static char Yellow = '\x09';
        public static char Gold = '\x10';
        public static char Silver = '\x0A';
        public static char Blue = '\x0B';
        public static char DarkBlue = '\x0C';
        public static char BlueGrey = '\x0D';
        public static char Magenta = '\x0E';
        public static char LightRed = '\x0F';
```
Use color name for e.g. {LightRed}

Credits for https://github.com/Hackmastr/css-basic-admin/
