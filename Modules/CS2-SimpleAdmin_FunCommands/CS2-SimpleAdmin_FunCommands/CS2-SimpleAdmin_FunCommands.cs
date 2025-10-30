using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CS2_SimpleAdmin_FunCommands;

/// <summary>
/// CS2-SimpleAdmin Fun Commands Module
///
/// This module serves as a REFERENCE IMPLEMENTATION for creating CS2-SimpleAdmin modules.
/// Study this code to learn best practices for:
/// - Command registration from configuration
/// - Menu creation with SimpleAdmin API
/// - Per-player translation support
/// - Proper cleanup on module unload
/// - Code organization using partial classes
///
/// File Structure:
/// - CS2-SimpleAdmin_FunCommands.cs (this file) - Plugin initialization and registration
/// - Commands.cs - Command handlers
/// - Actions.cs - Action methods (God, NoClip, Freeze, etc.)
/// - Menus.cs - Menu creation
/// - Config.cs - Configuration with command lists
/// - lang/ - Translation files (13 languages)
///
/// See README.md for detailed explanation of all patterns demonstrated here.
/// </summary>
public partial class CS2_SimpleAdmin_FunCommands : BasePlugin, IPluginConfig<Config>
{
    public Config Config { get; set; }

    /// <summary>
    /// BEST PRACTICE: Cache expensive operations
    /// Weapons enum values don't change, so we cache them on first access
    /// </summary>
    private static Dictionary<int, CsItem>? _weaponsCache;
    private static Dictionary<int, CsItem> GetWeaponsCache()
    {
        if (_weaponsCache != null) return _weaponsCache;

        var weaponsArray = Enum.GetValues(typeof(CsItem));
        _weaponsCache = new Dictionary<int, CsItem>();

        foreach (CsItem item in weaponsArray)
        {
            if (item == CsItem.Tablet) continue;  // Skip tablet (invalid weapon)
            _weaponsCache[(int)item] = item;
        }

        return _weaponsCache;
    }

    /// <summary>
    /// Track players with god mode enabled
    /// HashSet for O(1) lookup performance
    /// </summary>
    private static readonly HashSet<int> GodPlayers = [];

    /// <summary>
    /// Track players with modified speed
    /// Dictionary for storing speed values per player
    /// </summary>
    private static readonly Dictionary<CCSPlayerController, float> SpeedPlayers = [];

    /// <summary>
    /// Track players with modified gravity
    /// Dictionary for storing gravity values per player
    /// </summary>
    private static readonly Dictionary<CCSPlayerController, float> GravityPlayers = [];

    /// <summary>
    /// BEST PRACTICE: Use capability system to get SimpleAdmin API
    /// This ensures your module works even if SimpleAdmin loads after your module
    /// </summary>
    private ICS2_SimpleAdminApi? _sharedApi;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability = new("simpleadmin:api");

    /// <summary>
    /// BEST PRACTICE: Track menu registration state to prevent duplicate registrations
    /// </summary>
    private bool _menusRegistered = false;

    // Plugin metadata
    public override string ModuleName => "CS2-SimpleAdmin Fun Commands";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Your Name";
    public override string ModuleDescription => "Fun commands extension for CS2-SimpleAdmin";

    /// <summary>
    /// BEST PRACTICE: Initialize plugin after all plugins are loaded
    /// This ensures SimpleAdmin API is available
    /// </summary>
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        // STEP 1: Get SimpleAdmin API using capability system
        _sharedApi = _pluginCapability.Get();
        if (_sharedApi == null)
        {
            Logger.LogError("CS2-SimpleAdmin API not found - make sure CS2-SimpleAdmin is loaded!");
            Unload(false);
            return;
        }

        // STEP 2: Register commands (can be done immediately)
        RegisterFunCommands();

        // STEP 3: Register menus (wait for SimpleAdmin to be ready)
        // BEST PRACTICE: Use event + fallback to handle both normal load and hot reload
        _sharedApi.OnSimpleAdminReady += RegisterFunMenus;
        RegisterFunMenus(); // Fallback for hot reload case

        // STEP 4: Start timer to maintain speed and gravity modifications
        StartSpeedGravityTimer();
    }

    public override void Unload(bool hotReload)
    {
        if (_sharedApi == null) return;

        // Unregister commands
        if (Config.NoclipCommands.Count > 0)
        {
            foreach (var command in Config.NoclipCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.GodCommands.Count > 0)
        {
            foreach (var command in Config.GodCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.FreezeCommands.Count > 0)
        {
            foreach (var command in Config.FreezeCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.UnfreezeCommands.Count > 0)
        {
            foreach (var command in Config.UnfreezeCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.RespawnCommands.Count > 0)
        {
            foreach (var command in Config.RespawnCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.GiveCommands.Count > 0)
        {
            foreach (var command in Config.GiveCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.StripCommands.Count > 0)
        {
            foreach (var command in Config.StripCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.HpCommands.Count > 0)
        {
            foreach (var command in Config.HpCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.SpeedCommands.Count > 0)
        {
            foreach (var command in Config.SpeedCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.GravityCommands.Count > 0)
        {
            foreach (var command in Config.GravityCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.MoneyCommands.Count > 0)
        {
            foreach (var command in Config.MoneyCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        if (Config.ResizeCommands.Count > 0)
        {
            foreach (var command in Config.ResizeCommands)
            {
                _sharedApi.UnRegisterCommand(command);
            }
        }

        // Unregister menus
        if (Config.NoclipCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "noclip");

        if (Config.GodCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "god");

        if (Config.RespawnCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "respawn");

        if (Config.GiveCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "give");

        if (Config.StripCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "strip");

        if (Config.FreezeCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "freeze");

        if (Config.HpCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "hp");

        if (Config.SpeedCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "speed");

        if (Config.GravityCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "gravity");

        if (Config.MoneyCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "money");

        if (Config.ResizeCommands.Count > 0)
            _sharedApi.UnregisterMenu("fun", "resize");

        _sharedApi.OnSimpleAdminReady -= RegisterFunMenus;
    }

    private void RegisterFunCommands()
    {
        if (_sharedApi == null) return;

        if (Config.NoclipCommands.Count > 0)
        {
            foreach (var command in Config.NoclipCommands)
            {
                _sharedApi.RegisterCommand(command, "Enable noclip", OnNoclipCommand);
            }
        }

        if (Config.GodCommands.Count > 0)
        {
            foreach (var command in Config.GodCommands)
            {
                _sharedApi.RegisterCommand(command, "Enable god mode", OnGodCommand);
            }
        }

        if (Config.FreezeCommands.Count > 0)
        {
            foreach (var command in Config.FreezeCommands)
            {
                _sharedApi.RegisterCommand(command, "Freeze player", OnFreezeCommand);
            }
        }

        if (Config.UnfreezeCommands.Count > 0)
        {
            foreach (var command in Config.UnfreezeCommands)
            {
                _sharedApi.RegisterCommand(command, "Unfreeze player", OnUnfreezeCommand);
            }
        }

        if (Config.RespawnCommands.Count > 0)
        {
            foreach (var command in Config.RespawnCommands)
            {
                _sharedApi.RegisterCommand(command, "Respawn player", OnRespawnCommand);
            }
        }

        if (Config.GiveCommands.Count > 0)
        {
            foreach (var command in Config.GiveCommands)
            {
                _sharedApi.RegisterCommand(command, "Give weapon", OnGiveWeaponCommand);
            }
        }

        if (Config.StripCommands.Count > 0)
        {
            foreach (var command in Config.StripCommands)
            {
                _sharedApi.RegisterCommand(command, "Strip weapons", OnStripWeaponsCommand);
            }
        }

        if (Config.HpCommands.Count > 0)
        {
            foreach (var command in Config.HpCommands)
            {
                _sharedApi.RegisterCommand(command, "Set HP", OnSetHpCommand);
            }
        }

        if (Config.SpeedCommands.Count > 0)
        {
            foreach (var command in Config.SpeedCommands)
            {
                _sharedApi.RegisterCommand(command, "Set speed", OnSetSpeedCommand);
            }
        }

        if (Config.GravityCommands.Count > 0)
        {
            foreach (var command in Config.GravityCommands)
            {
                _sharedApi.RegisterCommand(command, "Set gravity", OnSetGravityCommand);
            }
        }

        if (Config.MoneyCommands.Count > 0)
        {
            foreach (var command in Config.MoneyCommands)
            {
                _sharedApi.RegisterCommand(command, "Set money", OnSetMoneyCommand);
            }
        }

        if (Config.ResizeCommands.Count > 0)
        {
            foreach (var command in Config.ResizeCommands)
            {
                _sharedApi.RegisterCommand(command, "Resize player", OnSetResizeCommand);
            }
        }
    }

    private void RegisterFunMenus()
    {
        if (_sharedApi == null || _menusRegistered) return;

        try
        {
            // 🆕 NEW: Per-player localization support for modules!
            // - This module has its own lang/ folder with translations
            // - We pass translation KEYS and the module's Localizer to the API
            // - SimpleAdmin will translate menu names per-player based on their css_lang
            // - Each player sees menus in their own language!
            _sharedApi.RegisterMenuCategory("fun", "fun_category_name", "@css/generic", Localizer!);

            // Register menus with command names for permission override support
            // Server admins can override default permissions via CounterStrikeSharp admin system
            // Example: If "css_god" is overridden to "@css/vip", only VIPs will see the God Mode menu
            //
            // 🆕 NEW: All menus use translation keys and module's Localizer for per-player localization!

            if (Config.GodCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "god",
                    "fun_menu_god",
                    CreateGodModeMenu, "@css/cheats", "css_god", Localizer!);

            if (Config.NoclipCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "noclip",
                    "fun_menu_noclip",
                    CreateNoClipMenu, "@css/cheats", "css_noclip", Localizer!);

            if (Config.RespawnCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "respawn",
                    "fun_menu_respawn",
                    CreateRespawnMenu, "@css/cheats", "css_respawn", Localizer!);

            if (Config.GiveCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "give",
                    "fun_menu_give",
                    CreateGiveWeaponMenu, "@css/cheats", "css_give", Localizer!);

            if (Config.StripCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "strip",
                    "fun_menu_strip",
                    CreateStripWeaponsMenu, "@css/slay", "css_strip", Localizer!);

            if (Config.FreezeCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "freeze",
                    "fun_menu_freeze",
                    CreateFreezeMenu, "@css/slay", "css_freeze", Localizer!);

            if (Config.HpCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "hp",
                    "fun_menu_hp",
                    CreateSetHpMenu, "@css/slay", "css_hp", Localizer!);

            if (Config.SpeedCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "speed",
                    "fun_menu_speed",
                    CreateSetSpeedMenu, "@css/slay", "css_speed", Localizer!);

            if (Config.GravityCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "gravity",
                    "fun_menu_gravity",
                    CreateSetGravityMenu, "@css/slay", "css_gravity", Localizer!);

            if (Config.MoneyCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "money",
                    "fun_menu_money",
                    CreateSetMoneyMenu, "@css/slay", "css_money", Localizer!);

            if (Config.ResizeCommands.Count > 0)
                _sharedApi.RegisterMenu("fun", "resize",
                    "fun_menu_resize",
                    CreateSetResizeMenu, "@css/slay", "css_resize", Localizer!);

            _menusRegistered = true;
            Logger.LogInformation("Fun menus registered successfully!");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to register Fun menus: {ex.Message}");
        }
    }

    public void OnConfigParsed(Config config)
    {
        Config = config;
    }

    /// <summary>
    /// Starts a repeating timer to maintain speed and gravity modifications for players.
    /// This ensures that speed/gravity changes persist even after respawns or round changes.
    /// </summary>
    private void StartSpeedGravityTimer()
    {
        AddTimer(0.12f, () =>
        {
            // Early exit if no players have modified speed or gravity
            var hasSpeedPlayers = SpeedPlayers.Count > 0;
            var hasGravityPlayers = GravityPlayers.Count > 0;

            if (!hasSpeedPlayers && !hasGravityPlayers)
                return;

            if (hasSpeedPlayers)
            {
                // Iterate through players with modified speed
                foreach (var kvp in SpeedPlayers)
                {
                    var player = kvp.Key;
                    // Early validation check - avoid accessing PlayerPawn if player is invalid
                    if (player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected)
                    {
                        var pawn = player.PlayerPawn?.Value;
                        if (pawn != null && pawn.LifeState == (int)LifeState_t.LIFE_ALIVE)
                        {
                            player.SetSpeed(kvp.Value);
                        }
                    }
                }
            }

            if (hasGravityPlayers)
            {
                // Iterate through players with modified gravity
                foreach (var kvp in GravityPlayers)
                {
                    var player = kvp.Key;
                    if (player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected)
                    {
                        var pawn = player.PlayerPawn?.Value;
                        if (pawn != null && pawn.LifeState == (int)LifeState_t.LIFE_ALIVE)
                        {
                            player.SetGravity(kvp.Value);
                        }
                    }
                }
            }
        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }

}