using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdminApi;

public interface ICS2_SimpleAdminApi
{
    public static readonly PluginCapability<ICS2_SimpleAdminApi?> PluginCapability = new("simpleadmin:api");
    
    public event Action? OnSimpleAdminReady;

    /// <summary>
    /// Gets player information associated with the specified player controller.
    /// </summary>
    /// <param name="player">The player controller.</param>
    /// <returns>PlayerInfo object representing player data.</returns>
    public PlayerInfo GetPlayerInfo(CCSPlayerController player);
    
    /// <summary>
    /// Returns the database connection string used by the plugin.
    /// </summary>
    public string GetConnectionString();
    
    /// <summary>
    /// Returns the configured server IP address with port.
    /// </summary>
    public string GetServerAddress();
    
    /// <summary>
    /// Returns the internal server ID assigned in the plugin's database.
    /// </summary>
    public int? GetServerId();

    /// <summary>
    /// Returns mute-related penalties for the specified player.
    /// </summary>
    /// <param name="player">The player controller.</param>
    /// <returns>A dictionary mapping penalty types to lists of penalties with end date, duration, and pass state.</returns>
    public Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration, bool Passed)>> GetPlayerMuteStatus(CCSPlayerController player);

    /// <summary>
    /// Event fired when a player receives a penalty.
    /// </summary>
    public event Action<PlayerInfo, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltied;
    
    /// <summary>
    /// Event fired when a penalty is added to a player by SteamID.
    /// </summary>
    public event Action<SteamID, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltiedAdded;
    
    /// <summary>
    /// Event to show admin activity messages.
    /// </summary>
    public event Action<string, string?, bool, object>? OnAdminShowActivity;
    
    /// <summary>
    /// Event fired when an admin toggles silent mode.
    /// </summary>
    public event Action<int, bool>? OnAdminToggleSilent;
    
    /// <summary>
    /// Issues a penalty to a player controller with specified type, reason, and optional duration.
    /// </summary>
    public void IssuePenalty(CCSPlayerController player, CCSPlayerController? admin, PenaltyType penaltyType, string reason, int duration = -1);
    
    /// <summary>
    /// Issues a penalty to a player identified by SteamID with specified type, reason, and optional duration.
    /// </summary>
    public void IssuePenalty(SteamID steamid, CCSPlayerController? admin, PenaltyType penaltyType, string reason, int duration = -1);
    
    /// <summary>
    /// Logs a command invoked by a caller with the command string.
    /// </summary>
    public void LogCommand(CCSPlayerController? caller, string command);
    
    /// <summary>
    /// Logs a command invoked by a caller with the command info object.
    /// </summary>
    public void LogCommand(CCSPlayerController? caller, CommandInfo command);
    
    /// <summary>
    /// Shows an admin activity message, optionally suppressing broadcasting.
    /// </summary>
    public void ShowAdminActivity(string messageKey, string? callerName = null, bool dontPublish = false, params object[] messageArgs);

    /// <summary>
    /// Shows an admin activity message with a custom translated message (for modules with their own localizer).
    /// </summary>
    /// <param name="translatedMessage">Already translated message to display to players.</param>
    /// <param name="callerName">Name of the admin executing the action (optional).</param>
    /// <param name="dontPublish">If true, won't trigger publish events.</param>
    public void ShowAdminActivityTranslated(string translatedMessage, string? callerName = null, bool dontPublish = false);

    /// <summary>
    /// Shows an admin activity message using module's localizer for per-player language support.
    /// This method sends messages in each player's configured language.
    /// </summary>
    /// <param name="moduleLocalizer">The module's IStringLocalizer instance.</param>
    /// <param name="messageKey">The translation key from the module's lang files.</param>
    /// <param name="callerName">Name of the admin executing the action (optional).</param>
    /// <param name="dontPublish">If true, won't trigger publish events.</param>
    /// <param name="messageArgs">Arguments to format the localized message.</param>
    public void ShowAdminActivityLocalized(object moduleLocalizer, string messageKey, string? callerName = null, bool dontPublish = false, params object[] messageArgs);

    /// <summary>
    /// Returns true if the specified admin player is in silent mode (not broadcasting activity).
    /// </summary>
    public bool IsAdminSilent(CCSPlayerController player);
    
    /// <summary>
    /// Returns a set of player slots representing admins currently in silent mode.
    /// </summary>
    public HashSet<int> ListSilentAdminsSlots();
    
    /// <summary>
    /// Registers a new command with the specified name, description, and callback.
    /// </summary>
    public void RegisterCommand(string name, string? description, CommandInfo.CommandCallback callback);
    
    /// <summary>
    /// Unregisters an existing command by its name.
    /// </summary>
    public void UnRegisterCommand(string name);
    
    /// <summary>
    /// Gets target players from command
    /// </summary>
    TargetResult? GetTarget(CommandInfo command);
    
    /// <summary>
    /// Returns the list of current valid players, available to call from other plugins.
    /// </summary>
    List<CCSPlayerController> GetValidPlayers();

    /// <summary>
    /// Registers a menu category.
    /// </summary>
    void RegisterMenuCategory(string categoryId, string categoryName, string permission = "@css/generic");

    /// <summary>
    /// Registers a menu category with per-player localization support for modules.
    /// 🆕 NEW: Supports per-player localization using module's IStringLocalizer!
    /// </summary>
    /// <param name="categoryId">The category ID (unique identifier).</param>
    /// <param name="categoryNameKey">Translation key from module's lang files.</param>
    /// <param name="permission">Required permission to access this category.</param>
    /// <param name="moduleLocalizer">Module's IStringLocalizer for per-player translation.</param>
    void RegisterMenuCategory(string categoryId, string categoryNameKey, string permission, object moduleLocalizer);

    /// <summary>
    /// Registers a menu in a category.
    /// </summary>
    /// <param name="categoryId">The category to add this menu to.</param>
    /// <param name="menuId">Unique identifier for the menu.</param>
    /// <param name="menuName">Display name of the menu.</param>
    /// <param name="menuFactory">Factory function that creates the menu for a player.</param>
    /// <param name="permission">Required permission to access this menu (optional).</param>
    /// <param name="commandName">Command name for permission override checking (optional, e.g., "css_god").</param>
    void RegisterMenu(string categoryId, string menuId, string menuName, Func<CCSPlayerController, object> menuFactory, string? permission = null, string? commandName = null);

    /// <summary>
    /// Registers a menu in a category with automatic context passing.
    /// RECOMMENDED: Use this overload to eliminate duplication of categoryId and menuName in factory methods.
    /// </summary>
    /// <param name="categoryId">The category to add this menu to.</param>
    /// <param name="menuId">Unique identifier for the menu.</param>
    /// <param name="menuName">Display name of the menu.</param>
    /// <param name="menuFactory">Factory function that receives player and menu context.</param>
    /// <param name="permission">Required permission to access this menu (optional).</param>
    /// <param name="commandName">Command name for permission override checking (optional, e.g., "css_god").</param>
    void RegisterMenu(string categoryId, string menuId, string menuName, Func<CCSPlayerController, MenuContext, object> menuFactory, string? permission = null, string? commandName = null);

    /// <summary>
    /// Registers a menu with per-player localization support for modules.
    /// 🆕 NEW: Supports per-player localization using module's IStringLocalizer!
    /// </summary>
    /// <param name="categoryId">The category to add this menu to.</param>
    /// <param name="menuId">Unique identifier for the menu.</param>
    /// <param name="menuNameKey">Translation key from module's lang files.</param>
    /// <param name="menuFactory">Factory function that receives player and menu context.</param>
    /// <param name="permission">Required permission to access this menu (optional).</param>
    /// <param name="commandName">Command name for permission override checking (optional).</param>
    /// <param name="moduleLocalizer">Module's IStringLocalizer for per-player translation.</param>
    void RegisterMenu(string categoryId, string menuId, string menuNameKey, Func<CCSPlayerController, MenuContext, object> menuFactory, string? permission, string? commandName, object moduleLocalizer);

    /// <summary>
    /// Unregisters a menu from a category.
    /// </summary>
    void UnregisterMenu(string categoryId, string menuId);

    /// <summary>
    /// Creates a menu with an automatic back button.
    /// </summary>
    object CreateMenuWithBack(string title, string categoryId, CCSPlayerController player);

    /// <summary>
    /// Creates a menu with an automatic back button using menu context.
    /// RECOMMENDED: Use this overload when calling from a context-aware menu factory to avoid title/category duplication.
    /// </summary>
    /// <param name="context">Menu context containing title and category information.</param>
    /// <param name="player">The player who will see the menu.</param>
    object CreateMenuWithBack(MenuContext context, CCSPlayerController player);

    /// <summary>
    /// Creates a menu with a list of players with filter and action.
    /// </summary>
    object CreateMenuWithPlayers(string title, string categoryId, CCSPlayerController admin, Func<CCSPlayerController, bool> filter, Action<CCSPlayerController, CCSPlayerController> onSelect);

    /// <summary>
    /// Creates a menu with a list of players using menu context.
    /// RECOMMENDED: Use this overload when calling from a context-aware menu factory to avoid title/category duplication.
    /// </summary>
    /// <param name="context">Menu context containing title and category information.</param>
    /// <param name="admin">The admin player opening the menu.</param>
    /// <param name="filter">Filter function to determine which players to show.</param>
    /// <param name="onSelect">Action to execute when a player is selected.</param>
    object CreateMenuWithPlayers(MenuContext context, CCSPlayerController admin, Func<CCSPlayerController, bool> filter, Action<CCSPlayerController, CCSPlayerController> onSelect);

    /// <summary>
    /// Adds an option to the menu (extension method helper).
    /// </summary>
    void AddMenuOption(object menu, string name, Action<CCSPlayerController> action, bool disabled = false, string? permission = null);

    /// <summary>
    /// Adds a submenu to the menu (extension method helper).
    /// </summary>
    void AddSubMenu(object menu, string name, Func<CCSPlayerController, object> subMenuFactory, bool disabled = false, string? permission = null);

    /// <summary>
    /// Opens a menu for a player.
    /// </summary>
    void OpenMenu(object menu, CCSPlayerController player);
}