namespace CS2_SimpleAdmin.Models;

public sealed class IpHistoryRow
{
    public long steamid { get; set; }
    public string? name { get; set; }
    public long address { get; set; }
    public DateTime used_at { get; set; }

    public ulong Steamid => Convert.ToUInt64(steamid);
    public string? Name => name;
    public uint Address => unchecked((uint)address);
    public DateTime Used_at => used_at;
}
