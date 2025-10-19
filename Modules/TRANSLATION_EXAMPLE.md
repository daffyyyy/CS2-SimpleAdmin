# Module Translation Guide

> **🎓 New to translations?** This guide shows you how to add multi-language support to your module!

## Why Use Module Translations?

When you use SimpleAdmin API's translation system, **each player automatically sees messages in their preferred language**!

**Example:**
- 🇬🇧 **Player with English:** "Admin gave PlayerName a weapon!"
- 🇵🇱 **Player with Polish:** "Admin dał PlayerName broń!"
- 🇷🇺 **Player with Russian:** "Admin дал PlayerName оружие!"

**All from ONE line of code!**

## Quick Start

### Step 1: Create Your Translation Files

Create a `lang` folder in your module with translation files for each language:

```
YourModule/
└── lang/
    ├── en.json
    ├── pl.json
    └── ru.json
```

**Example: `lang/en.json`**
```json
{
  "yourmod_admin_action": "{lightred}{0}{default} performed action on {lightred}{1}{default}!"
}
```

**Example: `lang/pl.json`**
```json
{
  "yourmod_admin_action": "{lightred}{0}{default} wykonał akcję na {lightred}{1}{default}!"
}
```

### Step 2: Use in Your Code

**✅ RECOMMENDED METHOD:** `ShowAdminActivityLocalized`

```csharp
// Show activity with per-player language support
var args = new object[] { "CALLER", targetPlayer.PlayerName };
if (admin == null || !_api.IsAdminSilent(admin))
{
    if (Localizer != null)
    {
        // Each player sees this in their language!
        _api.ShowAdminActivityLocalized(
            Localizer,                         // Your module's localizer
            "yourmod_admin_action",            // Translation key
            admin.PlayerName,                  // Caller name
            false,                             // dontPublish
            args);                             // Message arguments
    }
}
```

That's it! SimpleAdmin handles the rest.

## Complete Example

```csharp
using CounterStrikeSharp.API.Core;
using CS2_SimpleAdminApi;

public partial class MyModule : BasePlugin
{
    private ICS2_SimpleAdminApi? _api;

    private void GiveWeaponToPlayer(CCSPlayerController admin, CCSPlayerController target, string weaponName)
    {
        // Give the weapon
        target.GiveNamedItem(weaponName);

        var callerName = admin.PlayerName;

        // Show activity using module's localizer - each player sees it in their language!
        if (admin == null || !_api!.IsAdminSilent(admin))
        {
            var args = new object[] { "CALLER", target.PlayerName, weaponName };
            if (Localizer != null)
            {
                _api!.ShowAdminActivityLocalized(Localizer, "yourmod_admin_give_message", callerName, false, args);
            }
        }

        // Log the command
        _api!.LogCommand(admin, $"css_give {target.PlayerName} {weaponName}");
    }
}
```

## 🔑 Important: The "CALLER" Placeholder

**Always use `"CALLER"` as the first argument** in your translation messages!

The API automatically replaces `"CALLER"` based on the server's `ShowActivityType` configuration:

| ShowActivityType | What Players See |
|-----------------|-----------------|
| `1` | Non-admins see "Admin", admins see real name |
| `2+` | Everyone sees real admin name |

**Example:**
```json
{
  "yourmod_message": "{0} did something to {1}"
                     ↑ This will be replaced with "Admin" or admin's name
}
```

```csharp
var args = new object[] { "CALLER", targetPlayer.PlayerName };
//                         ↑ API replaces this automatically
```

## 💡 Pro Tips

### Tip 1: Use a Helper Method

Create a reusable helper to reduce code duplication:

```csharp
/// <summary>
/// Helper method to show activity and log command
/// Copy this to your module!
/// </summary>
private void LogAndShowActivity(
    CCSPlayerController? caller,
    CCSPlayerController target,
    string translationKey,
    string baseCommand,
    params string[] extraArgs)
{
    var callerName = caller?.PlayerName ?? "Console";

    // Build args: "CALLER" + target name + any extra args
    var args = new List<object> { "CALLER", target.PlayerName };
    args.AddRange(extraArgs);

    // Show activity with per-player language support
    if (caller == null || !_api.IsAdminSilent(caller))
    {
        if (Localizer != null)
        {
            _api.ShowAdminActivityLocalized(
                Localizer,
                translationKey,
                callerName,
                false,
                args.ToArray());
        }
    }

    // Build and log command
    var logCommand = $"{baseCommand} {target.PlayerName}";
    if (extraArgs.Length > 0)
    {
        logCommand += $" {string.Join(" ", extraArgs)}";
    }
    _api.LogCommand(caller, logCommand);
}
```

**Usage:**
```csharp
// Simple action
LogAndShowActivity(admin, target, "yourmod_kick_message", "css_kick");

// Action with parameters
LogAndShowActivity(admin, target, "yourmod_hp_message", "css_hp", "100");
```

### Tip 2: Translation Key Naming Convention

Use a consistent prefix for your module:

```json
{
  "yourmod_admin_action1": "...",
  "yourmod_admin_action2": "...",
  "yourmod_error_notarget": "..."
}
```

This prevents conflicts with other modules and makes it clear which module owns the translation.

### Tip 3: Color Formatting

Use CounterStrikeSharp color tags in your translations:

```json
{
  "yourmod_message": "{lightred}{0}{default} gave {green}{1}{default} a {yellow}{2}{default}!"
}
```

**Available colors:**
- `{default}`, `{white}`, `{darkred}`, `{green}`, `{lightyellow}`
- `{lightblue}`, `{olive}`, `{lime}`, `{red}`, `{purple}`
- `{grey}`, `{yellow}`, `{gold}`, `{silver}`, `{blue}`
- `{darkblue}`, `{bluegrey}`, `{magenta}`, `{lightred}`, `{orange}`

## 📖 Real Example: Fun Commands Module

The **[CS2-SimpleAdmin_FunCommands](./CS2-SimpleAdmin_FunCommands/)** module is a perfect reference:

**Translation files:** `Modules/CS2-SimpleAdmin_FunCommands/lang/`
- Has 13 languages (en, pl, ru, de, fr, es, etc.)
- Shows proper key naming (`fun_admin_*`)
- Demonstrates color usage

**Code examples:** `Modules/CS2-SimpleAdmin_FunCommands/CS2-SimpleAdmin_FunCommands/Actions.cs`
- Lines 20-31: God mode with translations
- Lines 48-59: NoClip with translations
- Lines 76-86: Freeze with translations

**Helper method:** `Modules/CS2-SimpleAdmin_FunCommands/CS2-SimpleAdmin_FunCommands/Commands.cs:274-306`

## ❌ Common Mistakes

### Mistake 1: Forgetting "CALLER"
```csharp
// ❌ WRONG
var args = new object[] { admin.PlayerName, target.PlayerName };

// ✅ CORRECT
var args = new object[] { "CALLER", target.PlayerName };
```

### Mistake 2: Using SimpleAdmin's Translations
```csharp
// ❌ WRONG - Uses SimpleAdmin's keys
_api.ShowAdminActivity("sa_admin_kick", ...)

// ✅ CORRECT - Uses YOUR module's keys
_api.ShowAdminActivityLocalized(Localizer, "yourmod_kick", ...)
```

### Mistake 3: Not Checking Localizer
```csharp
// ❌ WRONG - Will crash if Localizer is null
_api.ShowAdminActivityLocalized(Localizer, "key", ...)

// ✅ CORRECT - Check first
if (Localizer != null)
{
    _api.ShowAdminActivityLocalized(Localizer, "key", ...)
}
```

## 🔗 See Also

- **[MODULE_DEVELOPMENT.md](./MODULE_DEVELOPMENT.md)** - Complete module development guide
- **[CS2-SimpleAdmin_FunCommands/README.md](./CS2-SimpleAdmin_FunCommands/README.md)** - Reference implementation
- **[CounterStrikeSharp Localization](https://docs.cssharp.dev/guides/localization/)** - Official CSS localization docs

---

**Need help?** Study the FunCommands module - it demonstrates all these patterns correctly!
