using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS2_SimpleAdmin.Models;

public enum BanStatus
{
    [Description("ACTIVE")] ACTIVE,
    [Description("UNBANNED")] UNBANNED,
    [Description("EXPIRED")] EXPIRED,
    [Description("")] UNKNOWN
}

public record BanRecord
{
    [Column("id")]
    public int Id { get; init; }

    [Column("player_name")]
    public string? PlayerName { get; set; }

    [Column("player_steamid")]
    public ulong? PlayerSteamId { get; set; }

    [Column("player_ip")]
    public string? PlayerIp { get; set; }

    [Column("status")]
    public required string Status { get; init; }

    [NotMapped]
    public BanStatus StatusEnum => Status.ToUpper() switch
    {
        "ACTIVE" => BanStatus.ACTIVE,
        "UNBANNED" => BanStatus.UNBANNED,
        "EXPIRED" => BanStatus.EXPIRED,
        _ => BanStatus.UNKNOWN
    };
}
