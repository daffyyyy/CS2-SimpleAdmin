using CounterStrikeSharp.API.Core;

namespace CS2_SimpleAdmin_PlayTimeModule;

public class Config : IBasePluginConfig
{
    public int Version { get; set; } = 1;
    public string AdminFlag { get; set; } = "@css/ban";
    public int MinPlayers { get; set; } = 4;
}