# CS2-SimpleAdmin

### Description
Manage your Counter-Strike 2 server by simple commands :) 

### Info
It's only plugin base, I don't have much time for more extensive development, so if you want to help, do it :)

### Commands
- css_admin - Display all admin commands // @css/generic
- css_ban <#userid or name> [time in minutes/0 perm] [reason] - Ban player // @css/ban
- css_addban <steamid> [time in minutes/0 perm] [reason] - Ban player via steamid64 // @css/ban
- css_unban <steamid or name> - Unban player // @css/unban
- css_kick <#userid or name> [reason] - Kick player / @css/kick
- css_slay <#userid or name> - Kill player // @css/slay
- css_slap <#userid or name> [damage] - Slap player // @css/slay
- css_map <mapname> - Change map // @css/changemap
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


### Requirments
[CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/) **tested on v90**

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
