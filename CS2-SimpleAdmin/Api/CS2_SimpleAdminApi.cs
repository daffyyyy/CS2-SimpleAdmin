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
    
    public event Action<PlayerInfo, PlayerInfo?, PenaltyType, string, int, int?>? OnPlayerPenaltied;
    public event Action<SteamID, PlayerInfo?, PenaltyType, string, int, int?>? OnPlayerPenaltiedAdded;

    public void OnPlayerPenaltiedEvent(PlayerInfo player, PlayerInfo? admin, PenaltyType penaltyType, string reason,
        int duration = -1) =>  OnPlayerPenaltied?.Invoke(player, admin, penaltyType, reason, duration, CS2_SimpleAdmin.ServerId);
    
    public void OnPlayerPenaltiedAddedEvent(SteamID player, PlayerInfo? admin, PenaltyType penaltyType, string reason,
        int duration) =>  OnPlayerPenaltiedAdded?.Invoke(player, admin, penaltyType, reason, duration, CS2_SimpleAdmin.ServerId);

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

    public void LogCommand(CCSPlayerController? caller, string command)
    {
        Helper.LogCommand(caller, command);
    }

    public void LogCommand(CCSPlayerController? caller, CommandInfo command)
    {
        Helper.LogCommand(caller, command);
    }
}