# CS2-SimpleAdmin

### Description
Manage your Counter-Strike 2 server by simple commands :) 

### Info
It's only plugin base, I don't have much time for more extensive development, so if you want to help, do it :)

### Commands
- css_ban <#userid or name> [time in minutes/0 perm] [reason] - Ban player
- css_kick <#userid or name> [reason] - Kick player
- css_slay <#userid or name> - Kill player
- css_slap <#userid or name> [damage] - Slap player

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
