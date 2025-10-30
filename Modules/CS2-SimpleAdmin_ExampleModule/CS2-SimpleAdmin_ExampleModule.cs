using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin_ExampleModule;

/// <summary>
/// COMPLETE EXAMPLE MODULE FOR CS2-SIMPLEADMIN
///
/// This module demonstrates:
/// 1. ✅ Getting CS2-SimpleAdmin API via capability system
/// 2. ✅ Using API methods (GetServerId, GetConnectionString, IssuePenalty)
/// 3. ✅ Listening to events (OnPlayerPenaltied, OnPlayerPenaltiedAdded)
/// 4. ✅ Registering console commands
/// 5. ✅ Creating menu categories and menu items
/// 6. ✅ Using NEW MenuContext API to eliminate code duplication
/// 7. ✅ Proper cleanup on module unload
///
/// Study this file to learn how to create your own CS2-SimpleAdmin modules!
/// </summary>
public class CS2_SimpleAdmin_ExampleModule: BasePlugin
{
    // ========================================
    // PLUGIN METADATA
    // ========================================
    public override string ModuleName => "[CS2-SimpleAdmin] Example Module";
    public override string ModuleVersion => "v1.1.0";
    public override string ModuleAuthor => "daffyy & Example Contributors";

    // ========================================
    // PRIVATE FIELDS
    // ========================================

    /// <summary>
    /// Server ID from SimpleAdmin (null for single-server mode)
    /// Useful for multi-server setups to identify which server this is
    /// </summary>
    private int? _serverId;

    /// <summary>
    /// Database connection string from SimpleAdmin
    /// Use this if your module needs direct database access
    /// </summary>
    private string _dbConnectionString = string.Empty;

    /// <summary>
    /// Reference to CS2-SimpleAdmin API
    /// Use this to call API methods and register menus
    /// </summary>
    private static ICS2_SimpleAdminApi? _sharedApi;

    /// <summary>
    /// Capability for getting the SimpleAdmin API
    /// This is the recommended way to get access to another plugin's API
    /// </summary>
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability = new("simpleadmin:api");

    /// <summary>
    /// Flag to prevent duplicate menu registration
    /// Important for hot reload scenarios
    /// </summary>
    private bool _menusRegistered = false;

    // ========================================
    // PLUGIN LIFECYCLE
    // ========================================

    /// <summary>
    /// Called when all plugins are loaded (including hot reload)
    /// BEST PRACTICE: Use this instead of Load() to ensure all dependencies are available
    /// </summary>
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        // STEP 1: Get the SimpleAdmin API using capability system
        try
        {
            _sharedApi = _pluginCapability.Get();
        }
        catch (Exception)
        {
            Logger.LogError("CS2-SimpleAdmin API not found - make sure CS2-SimpleAdmin is loaded!");
            Unload(false);
            return;
        }

        // STEP 2: Get server information from SimpleAdmin
        _serverId = _sharedApi.GetServerId();
        _dbConnectionString = _sharedApi.GetConnectionString();
        Logger.LogInformation($"{ModuleName} started with serverId {_serverId}");

        // STEP 3: Subscribe to SimpleAdmin events
        // These events fire when penalties (ban, kick, mute, etc.) are issued
        _sharedApi.OnPlayerPenaltied += OnPlayerPenaltied;        // When penalty is issued to ONLINE player
        _sharedApi.OnPlayerPenaltiedAdded += OnPlayerPenaltiedAdded;  // When penalty is issued to OFFLINE player

        // STEP 4: Register menus
        // BEST PRACTICE: Wait for SimpleAdmin to be ready before registering menus
        // This handles both normal load and hot reload scenarios
        _sharedApi.OnSimpleAdminReady += RegisterExampleMenus;
        RegisterExampleMenus();  // Fallback for hot reload case
    }

    /// <summary>
    /// Called when the plugin is being unloaded
    /// BEST PRACTICE: Always clean up your registrations to prevent memory leaks
    /// </summary>
    public override void Unload(bool hotReload)
    {
        if (_sharedApi == null) return;

        // Unsubscribe from events
        _sharedApi.OnPlayerPenaltied -= OnPlayerPenaltied;
        _sharedApi.OnPlayerPenaltiedAdded -= OnPlayerPenaltiedAdded;
        _sharedApi.OnSimpleAdminReady -= RegisterExampleMenus;

        // Unregister menus
        _sharedApi.UnregisterMenu("example", "simple_action");
        _sharedApi.UnregisterMenu("example", "player_selection");
        _sharedApi.UnregisterMenu("example", "nested_menu");
        _sharedApi.UnregisterMenu("example", "test_command");

        Logger.LogInformation($"{ModuleName} unloaded successfully");
    }

    // ========================================
    // MENU REGISTRATION
    // ========================================

    /// <summary>
    /// Registers all example menus in the admin menu
    /// BEST PRACTICE: Use this pattern to prevent duplicate registrations
    /// </summary>
    private void RegisterExampleMenus()
    {
        if (_sharedApi == null || _menusRegistered) return;

        try
        {
            // STEP 1: Register a menu category
            // This creates a new section in the main admin menu
            // Permission: @css/generic means all admins can see it
            //
            // ⚠️ LOCALIZATION OPTIONS:
            //
            // OPTION A - No translations (hard-coded text):
            _sharedApi.RegisterMenuCategory(
                "example",              // Category ID (unique identifier)
                "Example Features",     // Display name (hard-coded, same for all players)
                "@css/generic"          // Required permission
            );
            //
            // OPTION B - With per-player translations (🆕 NEW!):
            // If your module has lang/ folder with translations, use this pattern:
            //   _sharedApi.RegisterMenuCategory(
            //       "example",                      // Category ID
            //       "example_category_name",        // Translation key
            //       "@css/generic",                 // Permission
            //       Localizer!                      // Module's localizer
            //   );
            // This will translate the category name per-player based on their css_lang setting!

            // STEP 2: Register individual menu items in the category
            // 🆕 NEW: These use MenuContext API - factory receives (admin, context) parameters
            //
            // ⚠️ LOCALIZATION OPTIONS:
            //
            // OPTION A - No translations (hard-coded text):

            // Example 1: Simple menu with options
            _sharedApi.RegisterMenu(
                "example",              // Category ID
                "simple_action",        // Menu ID (unique within category)
                "Simple Actions",       // Display name (hard-coded)
                CreateSimpleActionMenu, // Factory method
                "@css/generic"          // Required permission
            );

            // Example 2: Player selection menu
            _sharedApi.RegisterMenu(
                "example",
                "player_selection",
                "Select Player",        // Display name
                CreatePlayerSelectionMenu,
                "@css/kick"             // Requires kick permission
            );

            // Example 3: Nested menu (Player → Value)
            _sharedApi.RegisterMenu(
                "example",
                "nested_menu",
                "Give Credits",         // Display name
                CreateGiveCreditsMenu,
                "@css/generic"
            );

            // Example 4: Menu with permission override support
            _sharedApi.RegisterMenu(
                "example",
                "test_command",
                "Test Command",         // Display name
                CreateTestCommandMenu,
                "@css/root",            // Default permission
                "css_test"              // Command name for override checking
            );

            // OPTION B - With per-player translations (🆕 NEW!):
            // If your module has lang/ folder, use this pattern:
            //   _sharedApi.RegisterMenu(
            //       "example",                      // Category ID
            //       "menu_id",                      // Menu ID
            //       "menu_translation_key",         // Translation key (NOT translated text!)
            //       CreateYourMenu,                 // Factory method
            //       "@css/generic",                 // Permission
            //       "css_command",                  // Command name (optional)
            //       Localizer!                      // Module's localizer
            //   );
            // This will translate the menu name per-player based on their css_lang!
            // See FunCommands module for real example.

            _menusRegistered = true;
            Logger.LogInformation("Example menus registered successfully!");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to register example menus: {ex.Message}");
        }
    }

    // ========================================
    // MENU FACTORY METHODS
    // ========================================

    /// <summary>
    /// PATTERN 1: Simple menu with static options
    /// 🆕 NEW: Uses MenuContext to eliminate duplication!
    /// </summary>
    private object CreateSimpleActionMenu(CCSPlayerController admin, MenuContext context)
    {
        // Create menu with automatic back button
        // 🆕 NEW: Use context instead of repeating title and category!
        var menu = _sharedApi!.CreateMenuWithBack(context, admin);

        // Add menu options
        _sharedApi.AddMenuOption(menu, "Print Server Info", player =>
        {
            player.PrintToChat($"Server ID: {_serverId}");
            player.PrintToChat($"Server IP: {_sharedApi?.GetServerAddress()}");
        });

        _sharedApi.AddMenuOption(menu, "Get My Stats", player =>
        {
            try
            {
                var playerInfo = _sharedApi?.GetPlayerInfo(player);
                player.PrintToChat($"Total Bans: {playerInfo?.TotalBans ?? 0}");
                player.PrintToChat($"Total Kicks: {playerInfo?.TotalKicks ?? 0}");
                player.PrintToChat($"Total Warns: {playerInfo?.TotalWarns ?? 0}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting player info: {ex.Message}");
                player.PrintToChat("Error retrieving your stats");
            }
        });

        _sharedApi.AddMenuOption(menu, "Check Silent Mode", player =>
        {
            var isSilent = _sharedApi?.IsAdminSilent(player) ?? false;
            player.PrintToChat($"Silent mode: {(isSilent ? "ON" : "OFF")}");
        });

        return menu;
    }

    /// <summary>
    /// PATTERN 2: Player selection menu with immediate action
    /// 🆕 NEW: Uses MenuContext API - cleaner and less error-prone!
    /// </summary>
    private object CreatePlayerSelectionMenu(CCSPlayerController admin, MenuContext context)
    {
        // 🆕 NEW: CreateMenuWithPlayers now uses context instead of title/category
        return _sharedApi!.CreateMenuWithPlayers(
            context,    // ← Contains title and category automatically!
            admin,
            // Filter: Only show valid players that admin can target
            player => player.IsValid && admin.CanTarget(player),
            // Action: What happens when a player is selected
            (adminPlayer, targetPlayer) =>
            {
                adminPlayer.PrintToChat($"You selected: {targetPlayer.PlayerName}");

                // Example: Show player info
                try
                {
                    var playerInfo = _sharedApi?.GetPlayerInfo(targetPlayer);
                    adminPlayer.PrintToChat($"{targetPlayer.PlayerName} - Bans: {playerInfo?.TotalBans}, Warns: {playerInfo?.TotalWarns}");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Could not get info for {targetPlayer.PlayerName}: {ex.Message}");
                }
            }
        );
    }

    /// <summary>
    /// PATTERN 3: Nested menu (Player → Value selection)
    /// 🆕 NEW: First level menu uses MenuContext
    /// </summary>
    private object CreateGiveCreditsMenu(CCSPlayerController admin, MenuContext context)
    {
        // Create menu with back button
        // 🆕 NEW: Uses context - no more repeating title/category!
        var menu = _sharedApi!.CreateMenuWithBack(context, admin);

        // Get all valid, targetable players
        var players = _sharedApi.GetValidPlayers().Where(p =>
            p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && admin.CanTarget(p));

        foreach (var player in players)
        {
            var playerName = player.PlayerName.Length > 26
                ? player.PlayerName[..26]
                : player.PlayerName;

            // AddSubMenu automatically adds a "Back" button to the submenu
            _sharedApi.AddSubMenu(menu, playerName, p => CreateCreditAmountMenu(admin, player));
        }

        return menu;
    }

    /// <summary>
    /// Submenu for selecting credit amount
    /// Note: Submenus create dynamic titles, so they don't receive MenuContext
    /// </summary>
    private object CreateCreditAmountMenu(CCSPlayerController admin, CCSPlayerController target)
    {
        // Dynamic title includes target's name
        var menu = _sharedApi!.CreateMenuWithBack(
            $"Credits for {target.PlayerName}",
            "example",  // Category for back navigation
            admin
        );

        // Predefined credit amounts
        var creditAmounts = new[] { 100, 500, 1000, 5000, 10000 };

        foreach (var amount in creditAmounts)
        {
            _sharedApi.AddMenuOption(menu, $"{amount} Credits", _ =>
            {
                // BEST PRACTICE: Always validate player is still valid before action
                if (target.IsValid)
                {
                    Server.PrintToChatAll($"{admin.PlayerName} gave {amount} credits to {target.PlayerName}");
                    Logger.LogInformation($"Admin {admin.PlayerName} gave {amount} credits to {target.PlayerName}");
                }
                else
                {
                    admin.PrintToChat("Player is no longer available");
                }
            });
        }

        return menu;
    }

    /// <summary>
    /// Example menu with permission override support
    /// </summary>
    private object CreateTestCommandMenu(CCSPlayerController admin, MenuContext context)
    {
        var menu = _sharedApi!.CreateMenuWithBack(context, admin);

        // You can access context properties if needed
        _sharedApi.AddMenuOption(menu, "Show Context Info", player =>
        {
            player.PrintToChat($"Category: {context.CategoryId}");
            player.PrintToChat($"Menu ID: {context.MenuId}");
            player.PrintToChat($"Title: {context.MenuTitle}");
            player.PrintToChat($"Permission: {context.Permission}");
            player.PrintToChat($"Command: {context.CommandName}");
        });

        _sharedApi.AddMenuOption(menu, "Test Action", player =>
        {
            player.PrintToChat("Test action executed!");
        });

        return menu;
    }

    // ========================================
    // CONSOLE COMMANDS
    // ========================================

    /// <summary>
    /// Example command: Kick yourself
    /// Demonstrates using IssuePenalty API for online players
    /// </summary>
    [ConsoleCommand("css_kickme")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void KickMeCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null) return;

        // Issue a kick penalty to the caller
        // Parameters: player, admin (null = console), penaltyType, reason
        _sharedApi?.IssuePenalty(caller, null, PenaltyType.Kick, "You kicked yourself!");
    }

    /// <summary>
    /// Example command: Get server address
    /// Demonstrates using GetServerAddress API
    /// </summary>
    [ConsoleCommand("css_serveraddress")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void ServerAddressCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        commandInfo.ReplyToCommand($"Server IP: {_sharedApi?.GetServerAddress()}");
    }

    /// <summary>
    /// Example command: Get player statistics
    /// Demonstrates using GetPlayerInfo API
    /// </summary>
    [ConsoleCommand("css_getmyinfo")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void GetMyInfoCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null) return;

        try
        {
            var playerInfo = _sharedApi?.GetPlayerInfo(caller);
            commandInfo.ReplyToCommand($"Your Statistics:");
            commandInfo.ReplyToCommand($"  Total Bans: {playerInfo?.TotalBans ?? 0}");
            commandInfo.ReplyToCommand($"  Total Kicks: {playerInfo?.TotalKicks ?? 0}");
            commandInfo.ReplyToCommand($"  Total Gags: {playerInfo?.TotalGags ?? 0}");
            commandInfo.ReplyToCommand($"  Total Mutes: {playerInfo?.TotalMutes ?? 0}");
            commandInfo.ReplyToCommand($"  Total Warns: {playerInfo?.TotalWarns ?? 0}");
            commandInfo.ReplyToCommand($"  SteamID: {playerInfo?.SteamId}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in GetMyInfoCommand: {ex.Message}");
            commandInfo.ReplyToCommand("Error retrieving your information");
        }
    }

    /// <summary>
    /// Example command: Add ban to offline player
    /// Demonstrates using IssuePenalty API with SteamID for offline players
    /// SERVER ONLY - dangerous command!
    /// </summary>
    [ConsoleCommand("css_testaddban")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void OnAddBanCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        // Issue a ban to an offline player by SteamID
        // Parameters: steamID, admin (null = console), penaltyType, reason, duration (minutes)
        _sharedApi?.IssuePenalty(
            new SteamID(76561197960287930),  // Target SteamID
            null,                             // Admin (null = console)
            PenaltyType.Ban,                  // Penalty type
            "Test ban from example module",   // Reason
            10                                // Duration (10 minutes)
        );

        Logger.LogInformation("Test ban issued via API");
    }

    // ========================================
    // EVENT HANDLERS
    // ========================================

    /// <summary>
    /// Called when a penalty is issued to an ONLINE player
    /// Use this to react to bans/kicks/mutes happening in real-time
    /// </summary>
    private void OnPlayerPenaltied(
        PlayerInfo player,      // The player who received the penalty
        PlayerInfo? admin,      // The admin who issued it (null = console)
        PenaltyType penaltyType,// Type of penalty (Ban, Kick, Mute, etc.)
        string reason,          // Reason for the penalty
        int duration,           // Duration in minutes (-1 = permanent)
        int? penaltyId,         // Database ID of the penalty
        int? serverId           // Server ID where it was issued
    )
    {
        // Example: Announce bans to all players
        if (penaltyType == PenaltyType.Ban)
        {
            var adminName = admin?.Name ?? "Console";
            var durationText = (duration == -1 || duration == 0) ? "permanently" : $"for {duration} minutes";
            Server.PrintToChatAll($"{player.Name} was banned {durationText} by {adminName}");
        }

        // Log all penalties
        var adminNameLog = admin?.Name ?? "Console";
        switch (penaltyType)
        {
            case PenaltyType.Ban:
                Logger.LogInformation($"Ban issued to {player.Name} by {adminNameLog} (ID: {penaltyId}, Duration: {duration}m, Reason: {reason})");
                break;
            case PenaltyType.Kick:
                Logger.LogInformation($"Kick issued to {player.Name} by {adminNameLog} (Reason: {reason})");
                break;
            case PenaltyType.Gag:
                Logger.LogInformation($"Gag issued to {player.Name} by {adminNameLog} (ID: {penaltyId}, Duration: {duration}m)");
                break;
            case PenaltyType.Mute:
                Logger.LogInformation($"Mute issued to {player.Name} by {adminNameLog} (ID: {penaltyId}, Duration: {duration}m)");
                break;
            case PenaltyType.Silence:
                Logger.LogInformation($"Silence issued to {player.Name} by {adminNameLog} (ID: {penaltyId}, Duration: {duration}m)");
                break;
            case PenaltyType.Warn:
                Logger.LogInformation($"Warning issued to {player.Name} by {adminNameLog} (ID: {penaltyId}, Reason: {reason})");
                break;
        }
    }

    /// <summary>
    /// Called when a penalty is issued to an OFFLINE player
    /// Use this to react to bans/mutes added via SteamID (player not on server)
    /// </summary>
    private void OnPlayerPenaltiedAdded(
        SteamID steamId,        // SteamID of the penalized player
        PlayerInfo? admin,      // The admin who issued it (null = console)
        PenaltyType penaltyType,// Type of penalty
        string reason,          // Reason for the penalty
        int duration,           // Duration in minutes (-1 = permanent)
        int? penaltyId,         // Database ID of the penalty
        int? serverId           // Server ID where it was issued
    )
    {
        // Log offline penalty additions
        var adminName = admin?.Name ?? "Console";

        switch (penaltyType)
        {
            case PenaltyType.Ban:
                Logger.LogInformation($"Ban added for offline player {steamId} by {adminName} (ID: {penaltyId}, Duration: {duration}m, Reason: {reason})");
                break;
            case PenaltyType.Gag:
                Logger.LogInformation($"Gag added for offline player {steamId} by {adminName} (ID: {penaltyId}, Duration: {duration}m)");
                break;
            case PenaltyType.Mute:
                Logger.LogInformation($"Mute added for offline player {steamId} by {adminName} (ID: {penaltyId}, Duration: {duration}m)");
                break;
            case PenaltyType.Silence:
                Logger.LogInformation($"Silence added for offline player {steamId} by {adminName} (ID: {penaltyId}, Duration: {duration}m)");
                break;
            case PenaltyType.Warn:
                Logger.LogInformation($"Warning added for offline player {steamId} by {adminName} (ID: {penaltyId}, Reason: {reason})");
                break;
        }
    }
}
