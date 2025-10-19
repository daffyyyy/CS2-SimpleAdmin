using CounterStrikeSharp.API.Core;

namespace CS2_SimpleAdmin_FunCommands;

public class Config : IBasePluginConfig
{
    public int Version { get; set; } = 1;

    public List<string> NoclipCommands { get; set; } = ["css_noclip"];
    public List<string> GodCommands { get; set; } = ["css_god"];
    public List<string> FreezeCommands { get; set; } = ["css_freeze"];
    public List<string> UnfreezeCommands { get; set; } = ["css_unfreeze"];
    public List<string> RespawnCommands { get; set; } = ["css_respawn"];
    public List<string> GiveCommands { get; set; } = ["css_give"];
    public List<string> StripCommands { get; set; } = ["css_strip"];
    public List<string> HpCommands { get; set; } = ["css_hp"];
    public List<string> SpeedCommands { get; set; } = ["css_speed"];
    public List<string> GravityCommands { get; set; } = ["css_gravity"];
    public List<string> MoneyCommands { get; set; } = ["css_money"];
    public List<string> ResizeCommands { get; set; } = ["css_resize"];
}