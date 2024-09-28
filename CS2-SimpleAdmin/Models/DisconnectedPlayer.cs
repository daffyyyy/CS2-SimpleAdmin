using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdmin.Models;

public class DisconnectedPlayer(
    SteamID steamId,
    string name,
    string? ipAddress,
    DateTime disconnectTime)
{
    public SteamID SteamId { get; } = steamId;
    public string Name { get; set; } = name;
    public string? IpAddress { get; set; } = ipAddress;
    public DateTime DisconnectTime = disconnectTime;
}