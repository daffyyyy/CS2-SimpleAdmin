---
sidebar_position: 1
---

# Getting Started with Module Development

Step-by-step guide to creating your first CS2-SimpleAdmin module.

## Prerequisites

Before you begin:

- C# knowledge (intermediate level)
- .NET 8.0 SDK installed
- Visual Studio 2022 or VS Code
- Basic understanding of CounterStrikeSharp
- CS2 dedicated server for testing

---

## Step 1: Create Project

### Using .NET CLI

```bash
dotnet new classlib -n MyModule -f net8.0
cd MyModule
```

### Using Visual Studio

1. File → New → Project
2. Select "Class Library (.NET 8.0)"
3. Name: `MyModule`
4. Click Create

---

## Step 2: Add References

Edit `MyModule.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="CounterStrikeSharp.API">
      <HintPath>path/to/CounterStrikeSharp.API.dll</HintPath>
      <Private>false</Private>
    </Reference>

    <Reference Include="CS2-SimpleAdminApi">
      <HintPath>path/to/CS2-SimpleAdminApi.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

---

## Step 3: Create Main Plugin Class

Create `MyModule.cs`:

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CS2_SimpleAdminApi;

namespace MyModule;

public class MyModule : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "My Module";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Your Name";
    public override string ModuleDescription => "My awesome module";

    private ICS2_SimpleAdminApi? _api;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability =
        new("simpleadmin:api");

    public Config Config { get; set; } = new();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        // Get SimpleAdmin API
        _api = _pluginCapability.Get();
        if (_api == null)
        {
            Logger.LogError("CS2-SimpleAdmin API not found!");
            return;
        }

        Logger.LogInformation("MyModule loaded successfully!");

        // Register features
        RegisterCommands();

        // Register menus when ready
        _api.OnSimpleAdminReady += RegisterMenus;
        RegisterMenus();  // Also call for hot reload
    }

    private void RegisterCommands()
    {
        if (_api == null) return;

        foreach (var cmd in Config.MyCommands)
        {
            _api.RegisterCommand(cmd, "My command description", OnMyCommand);
        }
    }

    private void RegisterMenus()
    {
        if (_api == null) return;

        _api.RegisterMenuCategory("mymodule", "My Module", "@css/generic");
        _api.RegisterMenu("mymodule", "mymenu", "My Menu", CreateMyMenu, "@css/generic");
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/generic")]
    private void OnMyCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var targets = _api!.GetTarget(command);
        if (targets == null) return;

        foreach (var player in targets.Players.Where(p => p.IsValid && caller!.CanTarget(p)))
        {
            player.PrintToChat($"Hello from MyModule!");
        }

        _api.LogCommand(caller, command);
    }

    private object CreateMyMenu(CCSPlayerController admin, MenuContext context)
    {
        return _api!.CreateMenuWithPlayers(
            context,
            admin,
            player => player.IsValid && admin.CanTarget(player),
            (admin, target) =>
            {
                target.PrintToChat("You were selected!");
            }
        );
    }

    public void OnConfigParsed(Config config)
    {
        Config = config;
    }

    public override void Unload(bool hotReload)
    {
        if (_api == null) return;

        // Unregister commands
        foreach (var cmd in Config.MyCommands)
        {
            _api.UnRegisterCommand(cmd);
        }

        // Unregister menus
        _api.UnregisterMenu("mymodule", "mymenu");

        // Unsubscribe events
        _api.OnSimpleAdminReady -= RegisterMenus;
    }
}
```

---

## Step 4: Create Configuration

Create `Config.cs`:

```csharp
using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

public class Config : IBasePluginConfig
{
    [JsonPropertyName("Version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("MyCommands")]
    public List<string> MyCommands { get; set; } = ["css_mycommand"];

    [JsonPropertyName("EnableFeature")]
    public bool EnableFeature { get; set; } = true;
}
```

---

## Step 5: Build and Deploy

### Build

```bash
dotnet build -c Release
```

### Deploy

Copy files to server:
```
game/csgo/addons/counterstrikesharp/plugins/MyModule/
└── MyModule.dll
```

### Restart Server

```bash
# Server console
css_plugins reload
```

---

## Step 6: Test

1. Join your server
2. Open admin menu: `css_admin`
3. Look for "My Module" category
4. Test command: `css_mycommand @me`

---

## Next Steps

- **[Best Practices](best-practices)** - Write better code
- **[Examples](examples)** - More code examples
- **[API Reference](../api/overview)** - Full API documentation
- **[Fun Commands Module](https://github.com/daffyyyy/CS2-SimpleAdmin/tree/main/Modules/CS2-SimpleAdmin_FunCommands)** - Reference implementation

---

## Common Issues

### API Not Found

**Error:** `CS2-SimpleAdmin API not found!`

**Solution:**
- Ensure CS2-SimpleAdmin is installed
- Check that CS2-SimpleAdminApi.dll is in shared folder
- Verify CS2-SimpleAdmin loads before your module

### Commands Not Working

**Check:**
- Command registered in `RegisterCommands()`
- Permission is correct
- Player has required permission

### Menu Not Showing

**Check:**
- `OnSimpleAdminReady` event subscribed
- Menu registered in category
- Permission is correct
- SimpleAdmin loaded successfully

---

## Resources

- **[Module Development Guide](../../modules/development)** - Detailed guide
- **[GitHub Repository](https://github.com/daffyyyy/CS2-SimpleAdmin)** - Source code
- **[CounterStrikeSharp Docs](https://docs.cssharp.dev/)** - CSS framework
