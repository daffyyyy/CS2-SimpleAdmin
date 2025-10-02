using CounterStrikeSharp.API.Core;

namespace CS2_SimpleAdmin_StealthModule;

public class PluginConfig : IBasePluginConfig
{
    public int Version { get; set; } = 1;
    public List<string> Permissions { get; set; } = ["@css/ban"];
    public bool BlockStatusCommand { get; set; } = true;
    public bool HideAdminsOnJoin { get; set; } = true;
}