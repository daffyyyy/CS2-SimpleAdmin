using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin_ExampleModule;

public class CS2_SimpleAdmin_ExampleModule: BasePlugin
{
    public override string ModuleName => "[CS2-SimpleAdmin] Example module";
    public override string ModuleVersion => "v1.0.1";
    public override string ModuleAuthor => "daffyy";

    private int? _serverId;
    private string _dbConnectionString = string.Empty;
    
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

        _serverId = _sharedApi.GetServerId();
        _dbConnectionString = _sharedApi.GetConnectionString();
        Logger.LogInformation($"{ModuleName} started with serverId {_serverId}");
        
        _sharedApi.OnPlayerPenaltied += OnPlayerPenaltied;
        _sharedApi.OnPlayerPenaltiedAdded += OnPlayerPenaltiedAdded;
    }
    
    [ConsoleCommand("css_kickme")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void KickMeCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null) return;
        
        _sharedApi?.IssuePenalty(caller, null, PenaltyType.Kick, "test");
    }
    
    [ConsoleCommand("css_serverAddress")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void ServerAddressCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        commandInfo.ReplyToCommand($"Our server IP: {_sharedApi?.GetServerAddress()}");
    }    
    
    [ConsoleCommand("css_getMyInfo")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void GetMyInfoCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null) return;
        
        var playerInfo = _sharedApi?.GetPlayerInfo(caller);
        commandInfo.ReplyToCommand($"Your total bans: {playerInfo?.TotalBans}");
        commandInfo.ReplyToCommand($"Your total gags: {playerInfo?.TotalGags}");
        commandInfo.ReplyToCommand($"Your total mutes: {playerInfo?.TotalMutes}");
        commandInfo.ReplyToCommand($"Your total silences: {playerInfo?.SteamId}");
    }
    
    [ConsoleCommand("css_testaddban")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void OnAddBanCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        _sharedApi?.IssuePenalty(new SteamID(76561197960287930), null, PenaltyType.Ban, "My super reason", 10);
    }

    private void OnPlayerPenaltied(PlayerInfo player, PlayerInfo? admin, PenaltyType penaltyType,
        string reason, int duration, int? penaltyId, int? serverId)
    {
        if (penaltyType == PenaltyType.Ban)
        {
            Server.PrintToChatAll($"{player.Name} is a dog");    
        }

        switch (penaltyType)
        {
            case PenaltyType.Ban:
            {
                Logger.LogInformation("Ban issued");
                Logger.LogInformation($"Id = {penaltyId}");
                break;
            }
            case PenaltyType.Kick:
            {
                Logger.LogInformation("Kick issued");
                break;
            }
            case PenaltyType.Gag:
            {
                Logger.LogInformation("Gag issued");
                Logger.LogInformation($"Id = {penaltyId}");
                break;
            }
            case PenaltyType.Mute:
            {
                Logger.LogInformation("Mute issued");
                break;
            }
            case PenaltyType.Silence:
            {
                Logger.LogInformation("Silence issued");
                break;
            }
            case PenaltyType.Warn:
            {
                Logger.LogInformation("Warn issued");
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(penaltyType), penaltyType, null);
        }
        
        Console.WriteLine(player.Name);
        Console.WriteLine(admin?.Name ?? "Console");
        Console.WriteLine(player.SteamId.ToString());
        Console.WriteLine(reason);
    }
    
    private void OnPlayerPenaltiedAdded(SteamID steamId, PlayerInfo? admin, PenaltyType penaltyType,
        string reason, int duration, int? penaltyId, int? serverId)
    {
        switch (penaltyType)
        {
            case PenaltyType.Ban:
            {
                Logger.LogInformation("Ban added");
                Logger.LogInformation($"Id = {penaltyId}");
                break;
            }
            case PenaltyType.Kick:
            {
                Logger.LogInformation("Kick added");
                break;
            }
            case PenaltyType.Gag:
            {
                Logger.LogInformation("Gag added");
                Logger.LogInformation($"Id = {penaltyId}");
                break;
            }
            case PenaltyType.Mute:
            {
                Logger.LogInformation("Mute added");
                break;
            }
            case PenaltyType.Silence:
            {
                Logger.LogInformation("Silence added");
                break;
            }
            case PenaltyType.Warn:
            {
                Logger.LogInformation("Warn added");
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(penaltyType), penaltyType, null);
        }
        
        Console.WriteLine(admin?.Name ?? "Console");
        Console.WriteLine(steamId.ToString());
        Console.WriteLine(reason);
    }
} 