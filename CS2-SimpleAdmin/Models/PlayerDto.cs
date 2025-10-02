namespace CS2_SimpleAdmin.Models;

public record PlayerStats(int Score, int Kills, int Deaths, int MVPs);
public record PlayerDto(
    int UserId,
    string Name,
    string SteamId,
    string IpAddress,
    uint Ping,
    bool IsAdmin,
    PlayerStats Stats
);
