using System.ComponentModel.DataAnnotations.Schema;

namespace CS2_SimpleAdmin.Models;

public record BanRecord
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("player_name")]
    public string PlayerName { get; set; }
    
    [Column("player_steamid")]
    public string? PlayerSteamId { get; set; }
    
    [Column("player_ip")]
    public string? PlayerIp { get; set; }
    
    [Column("admin_steamid")]
    public string AdminSteamId { get; set; }
    
    [Column("admin_name")]
    public string AdminName { get; set; }
    
    [Column("reason")]
    public string Reason { get; set; }
    
    [Column("duration")]
    public int Duration { get; set; }
    
    [Column("ends")]
    public DateTime? Ends { get; set; }
    
    [Column("created")]
    public DateTime Created { get; set; }
    
    [Column("server_id")]
    public int? ServerId { get; set; }
    
    [Column("status")]
    public string Status { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
