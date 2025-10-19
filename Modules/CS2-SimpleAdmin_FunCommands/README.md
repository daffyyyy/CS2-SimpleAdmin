# CS2-SimpleAdmin Fun Commands Module

This module serves as a **reference implementation** for creating CS2-SimpleAdmin modules. It demonstrates best practices for menu creation, command registration, translation support, and API usage.

## 📚 What This Module Teaches

This module is designed to be educational and shows you how to:

1. ✅ **Register commands dynamically** from configuration
2. ✅ **Create menu categories** and menu items
3. ✅ **Use per-player translations** with `ShowAdminActivityLocalized`
4. ✅ **Handle player targeting** and validation
5. ✅ **Implement proper cleanup** on module unload
6. ✅ **Structure code** using partial classes for organization
7. ✅ **Cache data** for performance (weapons cache)
8. ✅ **Use configuration** to enable/disable features

## 🎯 Features

This module provides fun admin commands:

- **God Mode** (`css_god`) - Toggle god mode for players
- **No Clip** (`css_noclip`) - Enable no-clip mode
- **Freeze/Unfreeze** (`css_freeze`, `css_unfreeze`) - Freeze players in place
- **Respawn** (`css_respawn`) - Respawn dead players
- **Give Weapon** (`css_give`) - Give weapons to players
- **Strip Weapons** (`css_strip`) - Remove all player weapons
- **Set HP** (`css_hp`) - Set player health
- **Set Speed** (`css_speed`) - Modify player movement speed
- **Set Gravity** (`css_gravity`) - Change player gravity
- **Set Money** (`css_money`) - Set player money

## 📁 File Structure

```
CS2-SimpleAdmin_FunCommands/
├── CS2-SimpleAdmin_FunCommands.cs    # Main plugin file - initialization, registration
├── Commands.cs                        # Command handlers
├── Actions.cs                         # Action methods (God, NoClip, Freeze, etc.)
├── Menus.cs                          # Menu creation using SimpleAdmin API
├── Config.cs                         # Configuration with command lists
└── lang/                             # Translation files (13 languages)
    ├── en.json
    ├── pl.json
    ├── ru.json
    └── ... (10 more languages)
```

## 🔍 Code Organization Explained

### 1. Main Plugin File (`CS2-SimpleAdmin_FunCommands.cs`)

**Key Concepts Demonstrated:**

```csharp
public partial class CS2_SimpleAdmin_FunCommands : BasePlugin, IPluginConfig<Config>
{
    // ✅ BEST PRACTICE: Use capability system to get API
    private ICS2_SimpleAdminApi? _sharedApi;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability = new("simpleadmin:api");

    // ✅ BEST PRACTICE: Cache expensive data
    private static Dictionary<int, CsItem>? _weaponsCache;

    // ✅ BEST PRACTICE: Track menu registration state
    private bool _menusRegistered = false;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        // Get the API
        _sharedApi = _pluginCapability.Get();

        // Register commands
        RegisterFunCommands();

        // ✅ BEST PRACTICE: Wait for SimpleAdmin to be ready before registering menus
        _sharedApi.OnSimpleAdminReady += RegisterFunMenus;
        RegisterFunMenus(); // Fallback for hot reload
    }
}
```

**Why partial classes?**
- Separates concerns (commands, actions, menus)
- Makes code easier to navigate
- Each file has a specific purpose

### 2. Configuration (`Config.cs`)

**Key Concept:** Command lists for flexibility

```csharp
public class Config : IBasePluginConfig
{
    // ✅ BEST PRACTICE: Allow multiple command aliases
    public List<string> NoclipCommands { get; set; } = ["css_noclip"];
    public List<string> GodCommands { get; set; } = ["css_god"];
    // ... more command lists
}
```

**Benefits:**
- Users can disable features by emptying the list
- Users can add command aliases (e.g., `["css_god", "css_godmode"]`)
- Menus only register if commands exist

### 3. Commands (`Commands.cs`)

**Key Concepts Demonstrated:**

```csharp
[CommandHelper(1, "<#userid or name>")]
[RequiresPermissions("@css/cheats")]
private void OnGodCommand(CCSPlayerController? caller, CommandInfo command)
{
    // ✅ BEST PRACTICE: Use API to get targets (handles target syntax)
    var targets = _sharedApi!.GetTarget(command);
    if (targets == null) return;

    // ✅ BEST PRACTICE: Filter for alive players
    var playersToTarget = targets.Players.Where(player =>
        player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

    // ✅ BEST PRACTICE: Check targeting permissions
    playersToTarget.ForEach(player =>
    {
        if (caller!.CanTarget(player))
        {
            God(caller, player);
        }
    });

    // ✅ BEST PRACTICE: Always log commands
    _sharedApi.LogCommand(caller, command);
}
```

### 4. Actions (`Actions.cs`)

**Key Concepts Demonstrated:**

```csharp
private void God(CCSPlayerController? caller, CCSPlayerController player)
{
    // Perform the action
    if (!GodPlayers.Add(player.Slot))
    {
        GodPlayers.Remove(player.Slot);
    }

    // ✅ BEST PRACTICE: Use per-player language support
    var activityArgs = new object[] { "CALLER", player.PlayerName };
    if (caller == null || !_sharedApi!.IsAdminSilent(caller))
    {
        if (Localizer != null)
        {
            // Each player sees message in their configured language!
            _sharedApi!.ShowAdminActivityLocalized(Localizer, "fun_admin_god_message", callerName, false, activityArgs);
        }
    }

    // ✅ BEST PRACTICE: Log the action
    _sharedApi!.LogCommand(caller, $"css_god {player.PlayerName}");
}
```

### 5. Menus (`Menus.cs`)

**Key Concepts Demonstrated:**

#### Simple Player Selection Menu (NEW API with MenuContext!)

```csharp
// 🆕 NEW: Factory receives MenuContext - no more duplication!
private object CreateGodModeMenu(CCSPlayerController admin, MenuContext context)
{
    // ✅ BEST PRACTICE: Use context instead of repeating title and category
    return _sharedApi!.CreateMenuWithPlayers(
        context,  // ← Contains "God Mode" title and "fun" category automatically!
        admin,
        player => player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(player),
        God);     // Direct method reference
}
```

**Why MenuContext is better:**
- ❌ **Before:** You had to type `"God Mode"` and `"fun"` twice (in `RegisterMenu` and `CreateMenuWithPlayers`)
- ✅ **After:** Context contains these values automatically - no duplication!
- ✅ Less error-prone (can't accidentally use wrong category)
- ✅ Easier to refactor (change title in one place)

#### Nested Menu with Value Selection (NEW API!)

```csharp
// 🆕 NEW: Uses MenuContext to eliminate duplication
private object CreateSetHpMenu(CCSPlayerController admin, MenuContext context)
{
    // ✅ BEST PRACTICE: Use context instead of manual title/category
    var menu = _sharedApi!.CreateMenuWithBack(context, admin);

    var players = _sharedApi.GetValidPlayers().Where(p =>
        p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(p));

    foreach (var player in players)
    {
        // ✅ BEST PRACTICE: AddSubMenu automatically adds back button to submenu
        _sharedApi.AddSubMenu(menu, playerName, p => CreateHpSelectionMenu(admin, player));
    }

    return menu;
}

private object CreateHpSelectionMenu(CCSPlayerController admin, CCSPlayerController target)
{
    // Note: Submenus don't receive context - they create their own titles dynamically
    var hpMenu = _sharedApi!.CreateMenuWithBack($"Set HP: {target.PlayerName}", "fun", admin);
    var hpValues = new[] { 1, 10, 25, 50, 100, 200, 500, 999 };

    foreach (var hp in hpValues)
    {
        // ✅ BEST PRACTICE: AddMenuOption for simple actions
        _sharedApi.AddMenuOption(hpMenu, $"{hp} HP", _ =>
        {
            // ✅ BEST PRACTICE: Always validate before executing
            if (target.IsValid)
            {
                target.SetHp(hp);
                LogAndShowActivity(admin, target, "fun_admin_hp_message", "css_hp", hp.ToString());
            }
        });
    }

    return hpMenu;
}
```

**Comparison: Old vs New API**

```csharp
// ❌ OLD API - lots of duplication
_sharedApi.RegisterMenu("fun", "god", "God Mode", CreateGodModeMenu, "@css/cheats", "css_god");

private object CreateGodModeMenu(CCSPlayerController admin)
{
    return _sharedApi.CreateMenuWithPlayers(
        "God Mode",  // ← Repeated from RegisterMenu
        "fun",       // ← Repeated from RegisterMenu
        admin, filter, action);
}

// ✅ NEW API - no duplication!
_sharedApi.RegisterMenu("fun", "god", "God Mode", CreateGodModeMenu, "@css/cheats", "css_god");

private object CreateGodModeMenu(CCSPlayerController admin, MenuContext context)
{
    return _sharedApi.CreateMenuWithPlayers(
        context,     // ← Contains title and category automatically!
        admin, filter, action);
}
```

### 6. Translations

**Key Concept:** Module-specific translations

```json
// lang/en.json
{
    "fun_admin_god_message": "{lightred}{0}{default} changed god mode for {lightred}{1}{default}!",
    "fun_admin_hp_message": "{lightred}{0}{default} changed {lightred}{1}{default} hp amount!"
}
```

**Why module translations?**
- Your module is independent from SimpleAdmin
- You can update translations without affecting main plugin
- Each player sees messages in their language automatically

## 🛠️ How to Use This as a Template

### Step 1: Copy the Module
```bash
cp -r CS2-SimpleAdmin_FunCommands YourModuleName
```

### Step 2: Rename Files
- Rename `.csproj` file
- Rename all `.cs` files to match your module name
- Update namespace in all files

### Step 3: Update References
- Change `namespace CS2_SimpleAdmin_FunCommands` to `namespace YourModuleName`
- Update plugin metadata (name, version, author, description)

### Step 4: Modify Config
```csharp
public class Config : IBasePluginConfig
{
    public int Version { get; set; } = 1;

    // Add your own command lists
    public List<string> YourCommands { get; set; } = ["css_yourcommand"];
}
```

### Step 5: Add Your Commands
Look at `Commands.cs` for examples of command handlers

### Step 6: Add Your Menus
Look at `Menus.cs` for examples of menu creation

### Step 7: Add Translations
Create language files in `lang/{language}.json` (e.g., `lang/en.json`, `lang/pl.json`)

## 📖 Learning Path

If you're new to module development, study files in this order:

1. **Config.cs** - Understand configuration structure
2. **CS2-SimpleAdmin_FunCommands.cs** - See initialization and API acquisition
3. **Commands.cs** - Learn command registration and handling
4. **Actions.cs** - Understand action methods and translations
5. **Menus.cs** - Study menu creation patterns

## 🎓 Best Practices Demonstrated

### ✅ Command Registration
```csharp
// Dynamic registration based on config
if (Config.GodCommands.Count > 0)
{
    foreach (var command in Config.GodCommands)
    {
        _sharedApi.RegisterCommand(command, "Enable god mode", OnGodCommand);
    }
}
```

### ✅ Target Validation
```csharp
// Always check if player can be targeted
if (!caller.CanTarget(player)) return;

// Always validate player state
if (!player.IsValid) return;
```

### ✅ Translation Usage
```csharp
// Use module's localizer for per-player language support
if (Localizer != null)
{
    _sharedApi.ShowAdminActivityLocalized(Localizer, "translation_key", callerName, false, args);
}
```

### ✅ Cleanup on Unload
```csharp
public override void Unload(bool hotReload)
{
    // Unregister all commands
    if (Config.GodCommands.Count > 0)
    {
        foreach (var command in Config.GodCommands)
        {
            _sharedApi.UnRegisterCommand(command);
        }
    }

    // Unregister all menus
    if (Config.GodCommands.Count > 0)
        _sharedApi.UnregisterMenu("fun", "god");

    // Remove event handlers
    _sharedApi.OnSimpleAdminReady -= RegisterFunMenus;
}
```

### ✅ Data Caching
```csharp
// Cache expensive operations
private static Dictionary<int, CsItem> GetWeaponsCache()
{
    if (_weaponsCache != null) return _weaponsCache;

    var weaponsArray = Enum.GetValues(typeof(CsItem));
    _weaponsCache = new Dictionary<int, CsItem>();
    // ... populate cache
    return _weaponsCache;
}
```

## 🔗 Related Documentation

- **[MODULE_DEVELOPMENT.md](../MODULE_DEVELOPMENT.md)** - Complete API reference
- **[TRANSLATION_EXAMPLE.md](../TRANSLATION_EXAMPLE.md)** - Translation usage guide
- **[CS2-SimpleAdminApi](../../CS2-SimpleAdminApi/)** - API interface definitions

## 💡 Tips

1. **Start Simple** - Begin with one command and one menu, then expand
2. **Test Thoroughly** - Test with multiple players, different permissions, and languages
3. **Handle Errors** - Always validate player state before actions
4. **Log Everything** - Use `_sharedApi.LogCommand()` for all admin actions
5. **Use Partial Classes** - Split your code into logical files
6. **Comment Your Code** - Especially if others will use it as reference

## 🐛 Common Mistakes to Avoid

- ❌ **Don't** forget to check `IsValid` before accessing player properties
- ❌ **Don't** skip permission checks (`CanTarget`, `RequiresPermissions`)
- ❌ **Don't** forget to unregister commands/menus in `Unload()`
- ❌ **Don't** use SimpleAdmin's translations - create your own
- ❌ **Don't** hardcode command names - use config lists instead

## 🤝 Contributing

If you improve this module or find better patterns, please contribute back so others can learn!

## 📄 License

This module is provided as-is for educational purposes. Feel free to use it as a template for your own modules.
