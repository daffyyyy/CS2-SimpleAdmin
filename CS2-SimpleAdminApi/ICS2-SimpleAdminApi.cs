using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdminApi;

public interface ICS2_SimpleAdminApi
{
    public static readonly PluginCapability<ICS2_SimpleAdminApi?> PluginCapability = new("simpleadmin:api");

    public PlayerInfo GetPlayerInfo(CCSPlayerController player);
    
    public string GetConnectionString();
    public string GetServerAddress();
    public int? GetServerId();

    public Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration, bool Passed)>> GetPlayerMuteStatus(CCSPlayerController player);

    public event Action<PlayerInfo, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltied;
    public event Action<SteamID, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltiedAdded;
    public event Action<string, string?, bool, object>? OnAdminShowActivity;
    
    public void IssuePenalty(CCSPlayerController player, CCSPlayerController? admin, PenaltyType penaltyType, string reason, int duration = -1);
    public void IssuePenalty(SteamID steamid, CCSPlayerController? admin, PenaltyType penaltyType, string reason, int duration = -1);
    public void LogCommand(CCSPlayerController? caller, string command);
    public void LogCommand(CCSPlayerController? caller, CommandInfo command);
    public void ShowAdminActivity(string messageKey, string? callerName = null, bool dontPublish = false, params object[] messageArgs);

    public bool IsAdminSilent(CCSPlayerController player);
}