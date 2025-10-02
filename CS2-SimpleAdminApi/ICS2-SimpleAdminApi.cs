using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdminApi;

public interface ICS2_SimpleAdminApi
{
    public static readonly PluginCapability<ICS2_SimpleAdminApi?> PluginCapability = new("simpleadmin:api");

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
}