---
sidebar_position: 2
---

# Installation

This guide will help you install CS2-SimpleAdmin on your Counter-Strike 2 server.

## Prerequisites

Before installing CS2-SimpleAdmin, ensure you have the following dependencies installed:

### Required Dependencies

1. **[CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/)** (v1.0.340+)
   - The core framework for CS2 server plugins

2. **[AnyBaseLibCS2](https://github.com/NickFox007/AnyBaseLibCS2)**
   - Required by PlayerSettings

3. **[PlayerSettings](https://github.com/NickFox007/PlayerSettingsCS2)**
   - Required by MenuManager

4. **[MenuManagerCS2](https://github.com/NickFox007/MenuManagerCS2)**
   - Provides the menu system

### Database Requirements

You'll need either:
- **MySQL** server (recommended for production)
- **SQLite** (built-in, good for testing)

## Installation Steps

### 1. Download the Plugin

Download the latest release from the [GitHub Releases page](https://github.com/daffyyyy/CS2-SimpleAdmin/releases).

You can either:
- Download the pre-built release ZIP file
- Clone the repository and build from source

### 2. Extract Files

Extract the downloaded files to your server's CounterStrikeSharp directory:

```
game/csgo/addons/counterstrikesharp/plugins/CS2-SimpleAdmin/
```

Your directory structure should look like this:

```
csgo/
└── addons/
    └── counterstrikesharp/
        ├── plugins/
        │   └── CS2-SimpleAdmin/
        │       ├── CS2-SimpleAdmin.dll
        │       ├── lang/
        │       └── ... (other files)
        └── shared/
            └── CS2-SimpleAdminApi/
                └── CS2-SimpleAdminApi.dll
```

### 3. First Launch

Start your server. On the first launch, CS2-SimpleAdmin will:

1. Create a configuration file at:
   ```
   addons/counterstrikesharp/configs/plugins/CS2-SimpleAdmin/CS2-SimpleAdmin.json
   ```

2. Create a database (if using SQLite):
   ```
   addons/counterstrikesharp/plugins/CS2-SimpleAdmin/cs2-simpleadmin.sqlite
   ```

3. Apply database migrations automatically

### 4. Configure the Plugin

Edit the generated configuration file to match your server setup.

See the [Configuration Guide](configuration) for detailed information.

### 5. Restart Your Server

After editing the configuration, restart your server or reload the plugin:

```bash
css_plugins reload CS2-SimpleAdmin
```

## Building from Source

If you want to build CS2-SimpleAdmin from source:

### Prerequisites

- .NET 8.0 SDK
- Git

### Build Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/daffyyyy/CS2-SimpleAdmin.git
   cd CS2-SimpleAdmin
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore CS2-SimpleAdmin.sln
   ```

3. **Build the solution:**
   ```bash
   dotnet build CS2-SimpleAdmin.sln -c Release
   ```

4. **Build output location:**
   ```
   CS2-SimpleAdmin/bin/Release/net8.0/
   CS2-SimpleAdminApi/bin/Release/net8.0/
   ```

5. **Copy to server:**
   - Copy `CS2-SimpleAdmin.dll` and its dependencies to `plugins/CS2-SimpleAdmin/`
   - Copy `CS2-SimpleAdminApi.dll` to `shared/CS2-SimpleAdminApi/`

## Verification

To verify the installation was successful:

1. **Check server console** for the plugin load message:
   ```
   [CS2-SimpleAdmin] Plugin loaded successfully
   ```

2. **Run an admin command** in-game:
   ```
   css_admin
   ```

3. **Check the logs** at:
   ```
   addons/counterstrikesharp/logs/CS2-SimpleAdmin*.txt
   ```

## Troubleshooting

### Plugin doesn't load

**Solution:** Ensure all required dependencies are installed:
- CounterStrikeSharp (latest version)
- AnyBaseLibCS2
- PlayerSettings
- MenuManagerCS2

### Database connection errors

**Solution:**
- For MySQL: Verify database credentials in the config file
- For SQLite: Ensure the plugin has write permissions in its directory

### Commands not working

**Solution:**
- Check that you have admin permissions configured
- Verify the commands are enabled in `Commands.json`
- Check server console for error messages

## Next Steps

- **[Configure your plugin](configuration)** - Set up database, permissions, and features
- **[Learn the commands](commands/basebans)** - Browse available admin commands
- **[Add admins](#)** - Set up your admin team

## Need Help?

If you encounter issues:

1. Check the [GitHub Issues](https://github.com/daffyyyy/CS2-SimpleAdmin/issues) for similar problems
2. Review server logs for error messages
3. Ask for help on [GitHub Discussions](https://github.com/daffyyyy/CS2-SimpleAdmin/discussions)
