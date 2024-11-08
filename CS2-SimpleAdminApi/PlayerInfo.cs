using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_SimpleAdminApi;

public class PlayerInfo(
    int? userId,
    int slot,
    SteamID steamId,
    string name,
    string? ipAddress,
    int totalBans = 0,
    int totalMutes = 0,
    int totalGags = 0,
    int totalSilences = 0,
    int totalWarns = 0)
{
    public int? UserId { get; } = userId;
    public int Slot { get; } = slot;
    public SteamID SteamId { get; } = steamId;
    public string Name { get; } = name;
    public string? IpAddress { get; } = ipAddress;
    public int TotalBans { get; set; } = totalBans;
    public int TotalMutes { get; set; } = totalMutes;
    public int TotalGags { get; set; } = totalGags;
    public int TotalSilences { get; set; } = totalSilences;
    public int TotalWarns { get; set; } = totalWarns;
    public DiePosition? DiePosition { get; set; }
}

public struct DiePosition(Vector? position = null, QAngle? angle = null)
{
    public Vector? Position { get; set; } = position;
    public QAngle? Angle { get; set; } = angle;
}
