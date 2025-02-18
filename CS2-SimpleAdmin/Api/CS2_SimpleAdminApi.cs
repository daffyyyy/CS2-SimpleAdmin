using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdminApi;

namespace CS2_SimpleAdmin.Api;

public class CS2_SimpleAdminApi : ICS2_SimpleAdminApi
{
    public PlayerInfo GetPlayerInfo(CCSPlayerController player)
    {
        if (!player.UserId.HasValue)
            throw new KeyNotFoundException("Player with specific UserId not found");
        
        return CS2_SimpleAdmin.PlayersInfo[player.UserId.Value];
    }
    
    public string GetConnectionString()  => CS2_SimpleAdmin.Instance.DbConnectionString;
    public string GetServerAddress()  => CS2_SimpleAdmin.IpAddress;
    public int? GetServerId()  => CS2_SimpleAdmin.ServerId;

    public Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration, bool Passed)>> GetPlayerMuteStatus(CCSPlayerController player)
    {
        return PlayerPenaltyManager.GetAllPlayerPenalties(player.Slot);
    }
    
    public event Action<PlayerInfo, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltied;
    public event Action<SteamID, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltiedAdded;
    public event Action<string, string?, bool, object>? OnAdminShowActivity;

    public void OnPlayerPenaltiedEvent(PlayerInfo player, PlayerInfo? admin, PenaltyType penaltyType, string reason,
        int duration, int? penaltyId) =>  OnPlayerPenaltied?.Invoke(player, admin, penaltyType, reason, duration, penaltyId, CS2_SimpleAdmin.ServerId);
    
    public void OnPlayerPenaltiedAddedEvent(SteamID player, PlayerInfo? admin, PenaltyType penaltyType, string reason,
        int duration, int? penaltyId) =>  OnPlayerPenaltiedAdded?.Invoke(player, admin, penaltyType, reason, duration, penaltyId, CS2_SimpleAdmin.ServerId);
    
    public void OnAdminShowActivityEvent(string messageKey, string? callerName = null, bool dontPublish = false, params object[] messageArgs) =>  OnAdminShowActivity?.Invoke(messageKey, callerName, dontPublish, messageArgs);

    public void IssuePenalty(CCSPlayerController player, CCSPlayerController? admin, PenaltyType penaltyType, string reason, int duration = -1)
    {
        switch (penaltyType)
        {
            case PenaltyType.Ban:
            {
                CS2_SimpleAdmin.Instance.Ban(admin, player, duration, reason);
                break;
            }
            case PenaltyType.Kick:
            {
                CS2_SimpleAdmin.Instance.Kick(admin, player, reason);
                break;
            }
            case PenaltyType.Gag:
            {
                CS2_SimpleAdmin.Instance.Gag(admin, player, duration, reason);
                break;
            }
            case PenaltyType.Mute:
            {
                CS2_SimpleAdmin.Instance.Mute(admin, player, duration, reason);
                break;
            }
            case PenaltyType.Silence:
            {
                CS2_SimpleAdmin.Instance.Silence(admin, player, duration, reason);
                break;
            }
            case PenaltyType.Warn:
            {
                CS2_SimpleAdmin.Instance.Warn(admin, player, duration, reason);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(penaltyType), penaltyType, null);
        }
    }
    
    public void IssuePenalty(SteamID steamid, CCSPlayerController? admin, PenaltyType penaltyType, string reason, int duration = -1)
    {
        switch (penaltyType)
        {
            case PenaltyType.Ban:
            {
                CS2_SimpleAdmin.Instance.AddBan(admin, steamid, duration, reason);
                break;
            }
            case PenaltyType.Gag:
            {
                CS2_SimpleAdmin.Instance.AddGag(admin, steamid, duration, reason);
                break;
            }
            case PenaltyType.Mute:
            {
                CS2_SimpleAdmin.Instance.AddMute(admin, steamid, duration, reason);
                break;
            }
            case PenaltyType.Silence:
            {
                CS2_SimpleAdmin.Instance.AddSilence(admin, steamid, duration, reason);
                break;
            }
            case PenaltyType.Warn:
            {
                CS2_SimpleAdmin.Instance.AddWarn(admin, steamid, duration, reason);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(penaltyType), penaltyType, null);
        }
    }

    public void LogCommand(CCSPlayerController? caller, string command)
    {
        Helper.LogCommand(caller, command);
    }

    public void LogCommand(CCSPlayerController? caller, CommandInfo command)
    {
        Helper.LogCommand(caller, command);
    }
    
    public bool IsAdminSilent(CCSPlayerController player)
    {
        return CS2_SimpleAdmin.SilentPlayers.Contains(player.Slot);
    }

    public void ShowAdminActivity(string messageKey, string? callerName = null, bool dontPublish = false, params object[] messageArgs)
    {
        Helper.ShowAdminActivity(messageKey, callerName, dontPublish, messageArgs);
    }
}