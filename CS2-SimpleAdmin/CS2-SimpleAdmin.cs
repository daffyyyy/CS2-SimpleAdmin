using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CS2_SimpleAdmin.Database;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Menus;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CS2_SimpleAdmin;

[MinimumApiVersion(300)]
public partial class CS2_SimpleAdmin : BasePlugin, IPluginConfig<CS2_SimpleAdminConfig>
{
    internal static CS2_SimpleAdmin Instance { get; private set; } = new();

    public override string ModuleName => "CS2-SimpleAdmin" + (Helper.IsDebugBuild ? " (DEBUG)" : " (RELEASE)");
    public override string ModuleDescription => "Simple admin plugin for Counter-Strike 2 :)";
    public override string ModuleAuthor => "daffyy & Dliix66";
    public override string ModuleVersion => "1.7.8-beta-1";
    
    public override void Load(bool hotReload)
    {
        Instance = this;
        
        if (hotReload)
        {
            ServerLoaded = false;
            _serverLoading = false;
            
            CacheManager?.Dispose();
            CacheManager = new CacheManager();
            
            // OnGameServerSteamAPIActivated();
            OnMapStart(string.Empty);

            AddTimer(6.0f, () =>
            {
                if (DatabaseProvider == null) return;
                
                PlayersInfo.Clear();
                CachedPlayers.Clear();
                BotPlayers.Clear();
                
                foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV).ToArray()) 
                {
                    if (!player.IsBot)
                        PlayerManager.LoadPlayerData(player, true);
                    else
                        BotPlayers.Add(player);
                };
            });
            
            PlayersTimer?.Kill();
            PlayersTimer = null;
        }
        _cBasePlayerControllerSetPawnFunc = new MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>(GameData.GetSignature("CBasePlayerController_SetPawn"));

        SimpleAdminApi = new Api.CS2_SimpleAdminApi();
        Capabilities.RegisterPluginCapability(ICS2_SimpleAdminApi.PluginCapability, () => SimpleAdminApi);
        
        PlayersTimer?.Kill();
        PlayersTimer = null;
        PlayerManager.CheckPlayersTimer();
        
        Menus.MenuManager.Instance.InitializeDefaultCategories();
        BasicMenu.Initialize();
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        try
        {
            MenuApi = MenuCapability.Get();
        }
        catch (Exception ex)
        {
            Logger.LogError("Unable to load required plugins ... \n{exception}", ex.Message);
            Unload(false);
        }

        AddTimer(6.0f, () => ReloadAdmins(null));
        RegisterEvents();
        AddTimer(0.5f, RegisterCommands.InitializeCommands);

        if (!CoreConfig.UnlockConCommands)
        {
            _logger?.LogError(
                $"⚠️ Warning: 'UnlockConCommands' is disabled in core.json. " +
                $"Players will not be automatically banned when kicked and will be able " +
                $"to rejoin the server for 60 seconds. " +
                $"To enable instant banning, set 'UnlockConCommands': true"
            );
            _logger?.LogError(
                $"⚠️ Warning: 'UnlockConCommands' is disabled in core.json. " +
                $"Players will not be automatically banned when kicked and will be able " +
                $"to rejoin the server for 60 seconds. " +
                $"To enable instant banning, set 'UnlockConCommands': true"
            );
        }
    }

    public void OnConfigParsed(CS2_SimpleAdminConfig config)
    {
        if (System.Diagnostics.Debugger.IsAttached)
            Environment.FailFast(":(!");
        
        Helper.UpdateConfig(config);

        _logger = Logger;
        Config = config;

        bool missing = false;
        var cssPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp");
        var pluginsPath = Path.Combine(cssPath, "plugins");
        var sharedPath = Path.Combine(cssPath, "shared");

        foreach (var plugin in _requiredPlugins)
        {
            var pluginDirPath = Path.Combine(pluginsPath, plugin);
            var pluginDllPath = Path.Combine(pluginDirPath, $"{plugin}.dll");

            if (!Directory.Exists(pluginDirPath))
            {
                _logger?.LogError(
                    $"❌ Plugin directory '{plugin}' missing at: {pluginDirPath}"
                );
                missing = true;
            }

            if (!File.Exists(pluginDllPath))
            {
                _logger?.LogError(
                    $"❌ Plugin DLL '{plugin}.dll' missing at: {pluginDllPath}"
                );
                missing = true;
            }
        }

        foreach (var shared in _requiredShared)
        {
            var sharedDirPath = Path.Combine(sharedPath, shared);
            var sharedDllPath = Path.Combine(sharedDirPath, $"{shared}.dll");

            if (!Directory.Exists(sharedDirPath))
            {
                _logger?.LogError(
                    $"❌ Shared library directory '{shared}' missing at: {sharedDirPath}"
                );
                missing = true;
            }

            if (!File.Exists(sharedDllPath))
            {
                _logger?.LogError(
                    $"❌ Shared library DLL '{shared}.dll' missing at: {sharedDllPath}"
                );
                missing = true;
            }
        }
        
        if (missing)
            Server.ExecuteCommand($"css_plugins unload {ModuleName}");
        
        Instance = this;
        
    if (Config.DatabaseConfig.DatabaseType.Contains("mysql", StringComparison.CurrentCultureIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(config.DatabaseConfig.DatabaseHost) ||
            string.IsNullOrWhiteSpace(config.DatabaseConfig.DatabaseName) ||
            string.IsNullOrWhiteSpace(config.DatabaseConfig.DatabaseUser))
        {
            throw new Exception("[CS2-SimpleAdmin] You need to setup MySQL credentials in config!");
        }

        var builder = new MySqlConnectionStringBuilder()
        {
            Server = config.DatabaseConfig.DatabaseHost,
            Database = config.DatabaseConfig.DatabaseName,
            UserID = config.DatabaseConfig.DatabaseUser,
            Password = config.DatabaseConfig.DatabasePassword,
            Port = (uint)config.DatabaseConfig.DatabasePort,
            SslMode = Enum.TryParse(config.DatabaseConfig.DatabaseSSlMode, true, out MySqlSslMode sslMode)
                        ? sslMode
                        : MySqlSslMode.Preferred,
            Pooling = true,
        };

        DbConnectionString = builder.ConnectionString;
        DatabaseProvider = new MySqlDatabaseProvider(DbConnectionString);
    }
    else 
    {
        if (string.IsNullOrWhiteSpace(config.DatabaseConfig.SqliteFilePath))
        {
            throw new Exception("[CS2-SimpleAdmin] You need to specify SQLite file path in config!");
        }
        
        DatabaseProvider = new SqliteDatabaseProvider(ModuleDirectory + "/" + config.DatabaseConfig.SqliteFilePath);
    }

    var (success, exception) = Task.Run(() => DatabaseProvider.CheckConnectionAsync()).GetAwaiter().GetResult();
    if (!success)
    {
        if (exception != null)
            Logger.LogError("Problem with database connection! \n{exception}", exception);

        Unload(false);
        return;
    }

        Task.Run(() => DatabaseProvider.DatabaseMigrationAsync());
        
        if (!Directory.Exists(ModuleDirectory + "/data"))
        {
            Directory.CreateDirectory(ModuleDirectory + "/data");
        }

        _localizer = Localizer;

        if (!string.IsNullOrEmpty(Config.Discord.DiscordLogWebhook))
            DiscordWebhookClientLog = new DiscordManager(Config.Discord.DiscordLogWebhook);

        if (Config.EnableUpdateCheck)
            Task.Run(async () => await PluginInfo.CheckVersion(ModuleVersion, Logger));
        
        PermissionManager = new PermissionManager(DatabaseProvider);
        BanManager = new BanManager(DatabaseProvider);
        MuteManager = new MuteManager(DatabaseProvider);
        WarnManager = new WarnManager(DatabaseProvider);
    }

    internal static TargetResult? GetTarget(CommandInfo command, int argument = 1)
    {
        var matches = command.GetArgTargetResult(argument);

        if (!matches.Any())
        {
            command.ReplyToCommand($"Target {command.GetArg(argument)} not found.");
            return null;
        }

        if (command.GetArg(argument).StartsWith('@'))
            return matches;

        if (matches.Count() == 1)
            return matches;

        command.ReplyToCommand($"Multiple targets found for \"{command.GetArg(argument)}\".");
        return null;
    }

    public override void Unload(bool hotReload)
    {
        CacheManager?.Dispose();
        CacheManager = null;
        PlayersTimer?.Kill();
        PlayersTimer = null;
        UnregisterEvents();
        
        if (hotReload)
            PlayersInfo.Clear();
        else
            Server.ExecuteCommand($"css_plugins unload {ModuleDirectory}");
    }
}