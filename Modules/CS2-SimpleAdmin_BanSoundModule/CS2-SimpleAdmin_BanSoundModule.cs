using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin_BanSoundModule;

public class CS2_SimpleAdmin_BanSoundModule: BasePlugin
{
    public override string ModuleName => "[CS2-SimpleAdmin] BanSound Module";
    public override string ModuleVersion => "v1.0.0";
    public override string ModuleAuthor => "daffyy";

    private static ICS2_SimpleAdminApi? _sharedApi;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability  = new("simpleadmin:api");

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _sharedApi = _pluginCapability.Get();
        if (_sharedApi == null)
        {
            Logger.LogError("CS2-SimpleAdmin SharedApi not found");
            Unload(false);
            return;
        }
        
        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);

        _sharedApi.OnPlayerPenaltied += OnPlayerPenaltied;
    }

    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource("soundevents/soundevents_addon.vsndevts");
    }

    private void OnPlayerPenaltied(PlayerInfo playerInfo, PlayerInfo? admin, PenaltyType penaltyType,
        string reason, int duration, int? penaltyId, int? serverId)
    {
        if (penaltyType != PenaltyType.Ban || admin == null)
            return;
        
        foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
        {
            var filter = new RecipientFilter(player);
            player?.EmitSound("bansound", volume: 0.9f, recipients: filter);
        }
    }
} 