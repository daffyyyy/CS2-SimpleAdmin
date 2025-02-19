using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CS2_SimpleAdmin_RedisInform;

public class PluginConfig : IBasePluginConfig
{
    [JsonPropertyName("ConfigVersion")] public int Version { get; set; } = 1;
    [JsonPropertyName("RedisConnectionString")] public string RedisConnectionString { get; set; } = "172.18.0.1";
    [JsonPropertyName("RedisPassword")] public string RedisPassword { get; set; } = "";
}

public class CS2_SimpleAdmin_RedisInform: BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "[CS2-SimpleAdmin] Redis Inform";
    public override string ModuleVersion => "v1.0.0";
    public override string ModuleAuthor => "daffyy";

    internal static CS2_SimpleAdmin_RedisInform Instance = new ();
    public PluginConfig Config { get; set; } = new();
    internal static ICS2_SimpleAdminApi? SharedApi;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability  = new("simpleadmin:api");
    
    private RedisSubscriber? _redisSubscriber;

    public override void Load(bool hotReload)
    {
        Instance = this;
        
        if (hotReload)
        {
            _ = _redisSubscriber?.DisposeAsync();
        }
    }    
    
    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
    }
    
    public override void Unload(bool hotReload)
    {
        _ = _redisSubscriber?.DisposeAsync();
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        SharedApi = _pluginCapability.Get();

        if (SharedApi == null)
        {
            Logger.LogError("CS2-SimpleAdmin SharedApi not found");
            Unload(false);
            return;
        }
        
        SharedApi.OnAdminShowActivity += OnAdminShowActivity;

        if (hotReload)
        {
            _ = _redisSubscriber?.DisposeAsync();
        }
        
        _redisSubscriber = new RedisSubscriber(Guid.NewGuid().ToString("N"));
        if (!_redisSubscriber.IsRunning)
            _redisSubscriber.Start();
    }

    private void OnAdminShowActivity(string messageKey, string? callerName, bool dontPublish, object messageArgs)
    {
        var message = new
        {
            MessageKey = messageKey,
            CallerName = callerName,
            MessageArgs = messageArgs
        };
        
        var jsonMessage = JsonConvert.SerializeObject(message);
        _redisSubscriber?.PublishMessageAsync(jsonMessage);
    }

    // [ConsoleCommand("css_redistest")]
    // public void RedisTestCommand(CCSPlayerController? caller, CommandInfo commandInfo)
    // {
    //     SharedApi?.ShowAdminActivity("sa_admin_gag_message_time", "🦊 daffyy | UtopiaFPS.pl 🦊", [
    //         "CALLER",
    //         "🦊 daffyy | UtopiaFPS.pl 🦊",
    //         "test",
    //         1
    //     ]);
    // }
} 