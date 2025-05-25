using System.ComponentModel.DataAnnotations.Schema;

namespace CS2_SimpleAdmin.Models;

public record BanRecord
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("player_steamid")]
    public string? PlayerSteamId { get; set; }
    
    [Column("player_ip")]
    public string? PlayerIp { get; set; }
    
    [Column("status")]
    public string Status { get; set; }
}
