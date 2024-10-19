using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin_CleanModule;

public class CS2_SimpleAdmin_CleanModule: BasePlugin
{
    public override string ModuleName => "[CS2-SimpleAdmin] Clean module";
    public override string ModuleDescription => "Module allows you to remove all weapons lying on the ground";
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
        }
    }

    [ConsoleCommand("css_clean")]
    [ConsoleCommand("css_clear")]
    [RequiresPermissions("@css/cheat")]
    public void OnCleanCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var weapons = Utilities.FindAllEntitiesByDesignerName<CCSWeaponBaseGun>("weapon_");
        var defusers = Utilities.FindAllEntitiesByDesignerName<CSceneEntity>("item_cutters");

        foreach (var weapon in weapons)
        {
            if (!weapon.IsValid || weapon.State != CSWeaponState_t.WEAPON_NOT_CARRIED)
                continue;

            weapon.Remove();
        }
        
        foreach (var defuser in defusers)
        {
            if (!defuser.IsValid || defuser.OwnerEntity.Value != null)
                continue;

            defuser.Remove();
        }
        
        _sharedApi?.LogCommand(caller, commandInfo);
    }
} 