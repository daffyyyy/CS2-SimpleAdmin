namespace AntiDLL_CS2_SimpleAdmin;

using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities;
using CS2_SimpleAdminApi;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Microsoft.Extensions.Logging;
using AntiDLL.API;

public class PluginConfig : IBasePluginConfig
{
    [JsonPropertyName("ConfigVersion")] public int Version { get; set; } = 1;
    [JsonPropertyName("Reason")] public string Reason { get; set; } = "Invalid event detected!";
    [JsonPropertyName("Duration")] public int Duration { get; set; } = 0;
    [JsonPropertyName("CommandToExecute")] public string CommandToExecute { get; set; } = "css_addban {steamid64} {duration} {reason}";
    [JsonPropertyName("BanType")] public string BanType { get; set; } = "auto";
}

public sealed class AntiDLL_CS2_SimpleAdmin : BasePlugin, IPluginConfig<PluginConfig>
{
    private int _banType;
    public PluginConfig Config { get; set; } = new();
    private readonly HashSet<int> _bannedPlayers = [];
    private readonly HashSet<int> _detections = [];
    private static PluginCapability<IAntiDLL> AntiDll { get; } = new("AntiDLL");
    private static PluginCapability<ICS2_SimpleAdminApi> SimpleAdminApi { get; } = new("simpleadmin:api");
    private static ICS2_SimpleAdminApi? _simpleAdminApi;
    
    public override string ModuleName => "AntiDLL [CS2-SimpleAdmin Module]";
    public override string ModuleDescription => "AntiDLL module for CS2-SimpleAdmin integration";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "daffyy";

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
    }
    
    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
    }
    
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        try
        {
            var antidll = AntiDll.Get();
            if (antidll == null)
            {
                Logger.LogError("Failed to get AntiDLL API");
                Unload(false);
                return;
            }
            
            antidll.OnDetection += OnDetection;
        }
        catch (Exception)
        {
            Logger.LogError("Failed to get AntiDLL API");
            Unload(false);
        }

        if (Config.BanType != "auto" && Config.BanType != "simpleadmin")
            return;

        try
        {
            _simpleAdminApi = SimpleAdminApi.Get();
            if (_simpleAdminApi != null)
                _banType = 1;
        }
        catch (Exception)
        {
            Logger.LogError("Failed to get CS2-SimpleAdmin API, using command as BanType");
        }
    }

    private void OnClientDisconnect(int playerSlot)
    {
        // var player = Utilities.GetPlayerFromSlot(playerSlot);
        // if (player == null || !player.IsValid || player.IsBot)
        //     return;

        _bannedPlayers.Remove(playerSlot);
        _detections.Remove(playerSlot);
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot || !_detections.Contains(player.Slot)) 
            return HookResult.Continue;
        
        if (!_bannedPlayers.Contains(player.Slot) && player.Connected == PlayerConnectedState.PlayerConnected && player.TeamNum != 0)
            PunishPlayer(player);

        return HookResult.Continue;
    }

    private void OnDetection(CCSPlayerController? player, string eventName)
    {
        if (player == null || !player.IsValid || player.IsBot) return;
        if (!_detections.Add(player.Slot))
            return;
        
        // if (player.Connected != PlayerConnectedState.PlayerConnected)
        // {
        //     _detections.Add(player.Slot);
        //     // AddTimer(3.0f, () => OnDetection(player, eventName));
        //     return;
        // }
        
        Logger.LogInformation("Detected \"{eventName}\" for \"{player}({steamid})\"", eventName, player.PlayerName, player.SteamID.ToString());
    }

    private void PunishPlayer(CCSPlayerController player)
    {
        if (!_bannedPlayers.Add(player.Slot))
            return;
        
        if (_banType == 1 && _simpleAdminApi != null)
        {
            _simpleAdminApi.IssuePenalty(new SteamID(player.SteamID), null, PenaltyType.Ban, Config.Reason, Config.Duration);
        }
        else if (Config.BanType == "kick")
        {
            player.Disconnect(NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED_VACNETABNORMALBEHAVIOR);
        }
        else
        {
            Server.ExecuteCommand(Config.CommandToExecute.Replace("{steamid64}", player.SteamID.ToString())
                .Replace("{duration}", Config.Duration.ToString()).Replace("{reason}", $"\"{Config.Reason}\"")
                .Replace("{userid}", player.UserId.Value.ToString()));
        }
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        
        var antidll = AntiDll.Get();
        if (antidll != null)
        {
            antidll.OnDetection -= OnDetection;
        }
    }
}