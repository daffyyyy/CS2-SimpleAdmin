---
sidebar_position: 1
---

# Introduction

Welcome to **CS2-SimpleAdmin** - a comprehensive administration plugin for Counter-Strike 2 servers built with C# (.NET 8.0) for CounterStrikeSharp.

## What is CS2-SimpleAdmin?

CS2-SimpleAdmin is a powerful server administration tool that provides comprehensive features for managing your Counter-Strike 2 server. It offers:

- **Player Management** - Ban, kick, mute, gag, and warn players
- **Admin System** - Flexible permission system with flags and groups
- **Multi-Server Support** - Manage multiple servers with a shared database
- **Discord Integration** - Send notifications to Discord webhooks
- **Menu System** - Easy-to-use admin menus
- **Extensive Commands** - Over 50 admin commands
- **Module Support** - Extend functionality with custom modules

## Key Features

### 🛡️ Comprehensive Penalty System
- **Bans** - Ban players by SteamID or IP address
- **Mutes** - Mute voice communication
- **Gags** - Gag text chat
- **Silence** - Block both voice and text
- **Warnings** - Progressive warning system with auto-escalation

### 👥 Flexible Admin System
- Permission-based access control using flags
- Admin groups for easy management
- Immunity levels to prevent abuse
- Server-specific or global admin assignments

### 🗄️ Database Support
- **MySQL** - For production environments
- **SQLite** - For quick setup and testing
- Automatic migration system
- Multi-server mode with shared data

### 🎮 User-Friendly Interface
- Interactive admin menus
- In-game admin panel
- Player selection menus
- Duration and reason selection

### 🔧 Extensibility
- Public API for module development
- Event system for custom integrations
- Command registration system
- Menu builder API

## Requirements

Before installing CS2-SimpleAdmin, make sure you have:

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/) (v1.0.340+)
- [AnyBaseLibCS2](https://github.com/NickFox007/AnyBaseLibCS2)
- [PlayerSettings](https://github.com/NickFox007/PlayerSettingsCS2)
- [MenuManagerCS2](https://github.com/NickFox007/MenuManagerCS2)
- MySQL database (or use SQLite for testing)

## Quick Links

- **[Installation Guide](installation)** - Get started with CS2-SimpleAdmin
- **[Configuration](configuration)** - Learn how to configure the plugin
- **[Commands](commands/basebans)** - Browse all available commands
- **[GitHub Repository](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Source code and releases

## Community & Support

Need help or want to contribute?

- **Issues** - Report bugs on [GitHub Issues](https://github.com/daffyyyy/CS2-SimpleAdmin/issues)
- **Discussions** - Ask questions on [GitHub Discussions](https://github.com/daffyyyy/CS2-SimpleAdmin/discussions)
- **Pull Requests** - Contribute improvements

## License

CS2-SimpleAdmin is open-source software. Check the [GitHub repository](https://github.com/daffyyyy/CS2-SimpleAdmin) for license details.
