---
sidebar_position: 1
---

# Modules Introduction

Extend CS2-SimpleAdmin functionality with powerful modules.

## What are Modules?

Modules are extensions that add new features to CS2-SimpleAdmin. They use the CS2-SimpleAdmin API to integrate seamlessly with the core plugin.

## Official Modules

### Fun Commands Module

Adds entertainment and player manipulation commands like god mode, noclip, freeze, and more.

**[Learn more →](funcommands)**

---

## Benefits of Modules

### 🔌 Easy Integration
- Built on CS2-SimpleAdmin API
- Automatic menu registration
- Command system integration

### 🎨 Feature Separation
- Keep core plugin lightweight
- Add only features you need
- Easy to enable/disable

### 🔧 Customizable
- Configure each module independently
- Disable unwanted commands
- Customize permissions

### 📦 Simple Installation
- Drop module files in folder
- Restart server
- Module auto-loads

---

## Installing Modules

### Standard Installation

1. **Download the module** from releases or build from source

2. **Extract to plugins folder:**
   ```
   game/csgo/addons/counterstrikesharp/plugins/ModuleName/
   ```

3. **Restart server** or reload plugins:
   ```
   css_plugins reload
   ```

4. **Configure** (if needed):
   ```
   addons/counterstrikesharp/configs/plugins/ModuleName/
   ```

---

## Module Structure

Typical module structure:

```
plugins/
└── CS2-SimpleAdmin_ModuleName/
    ├── CS2-SimpleAdmin_ModuleName.dll
    ├── CS2-SimpleAdmin_ModuleName.json  (config)
    └── lang/                             (translations)
        ├── en.json
        ├── pl.json
        └── ...
```

---

## Module Configuration

Each module has its own configuration file:

```
addons/counterstrikesharp/configs/plugins/ModuleName/ModuleName.json
```

### Common Configuration Pattern

```json
{
  "Version": 1,
  "CommandName": ["css_command", "css_alias"],
  "OtherSettings": {
    "EnableFeature": true
  }
}
```

**Key Features:**
- Command lists allow multiple aliases
- Empty command list = feature disabled
- Module-specific settings

---

## Available Modules

### Core Modules

| Module | Description | Status |
|--------|-------------|--------|
| **[Fun Commands](funcommands)** | God mode, noclip, freeze, speed, gravity | ✅ Official |

### Community Modules

Check the [GitHub repository](https://github.com/daffyyyy/CS2-SimpleAdmin) for community-contributed modules.

---

## Developing Modules

Want to create your own module?

**[See Module Development Guide →](development)**

**[See Developer Documentation →](../developer/intro)**

---

## Module vs Core Plugin

### When to use Core Plugin:
- Essential admin functions
- Punishment system
- Permission management
- Database operations

### When to use Modules:
- Optional features
- Server-specific functionality
- Experimental features
- Custom integrations

---

## Module Dependencies

### Required for All Modules:
- CS2-SimpleAdmin (core plugin)
- CS2-SimpleAdminApi.dll

### Module-Specific:
Check each module's documentation for specific requirements.

---

## Troubleshooting Modules

### Module doesn't load

**Check:**
1. Is CS2-SimpleAdmin loaded?
2. Is CS2-SimpleAdminApi.dll in shared folder?
3. Check server console for errors
4. Verify module files are complete

### Module commands not working

**Check:**
1. Is command enabled in module config?
2. Do you have required permissions?
3. Check Commands.json for conflicts
4. Verify module loaded successfully

### Module conflicts

**Check:**
- Multiple modules providing same command
- Check server console for warnings
- Disable conflicting module

---

## Best Practices

### Module Management

1. **Use only needed modules** - Don't overload server
2. **Keep modules updated** - Check for updates regularly
3. **Test before production** - Test modules on dev server first
4. **Review permissions** - Understand what each module can do

### Performance

1. **Monitor resource usage** - Some modules may impact performance
2. **Configure wisely** - Disable unused features
3. **Check logs** - Monitor for errors

---

## Module Updates

### Updating Modules

1. **Backup current version**
2. **Download new version**
3. **Replace files** in plugins folder
4. **Check configuration** - New config options may exist
5. **Restart server**

### Breaking Changes

Some updates may have breaking changes:
- Check module changelog
- Review new configuration options
- Test thoroughly

---

## Community Contributions

### Sharing Modules

Created a module? Share it with the community!

1. **Publish on GitHub**
2. **Document thoroughly**
3. **Provide examples**
4. **Include README**

### Using Community Modules

1. **Review code** - Ensure it's safe
2. **Check compatibility** - Verify CS2-SimpleAdmin version
3. **Test thoroughly** - Don't trust blindly
4. **Report issues** - Help improve modules

---

## Next Steps

- **[Explore Fun Commands Module](funcommands)** - Add entertainment features
- **[Learn Module Development](development)** - Create your own modules
- **[Read API Documentation](../developer/intro)** - Understand the API

---

## Need Help?

- **Issues** - [GitHub Issues](https://github.com/daffyyyy/CS2-SimpleAdmin/issues)
- **Discussions** - [GitHub Discussions](https://github.com/daffyyyy/CS2-SimpleAdmin/discussions)
- **Examples** - Check official modules for reference
