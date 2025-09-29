using CounterStrikeSharp.API.Core;
using CS2_SimpleAdmin.Models;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using CS2_SimpleAdmin.Managers;

using Menu;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    // Config
    public CS2_SimpleAdminConfig Config { get; set; } = new();

    // HttpClient
    internal static readonly HttpClient HttpClient = new();

    // Paths
    internal static readonly string ConfigDirectory = Path.Combine(Application.RootDirectory, "configs/plugins/CS2-SimpleAdmin");

    // Localization
    public static IStringLocalizer? _localizer;

    // Voting System
    public static readonly Dictionary<string, int> VoteAnswers = [];
    public static bool VoteInProgress;

    // Command and Server Settings
    public static readonly bool UnlockedCommands = CoreConfig.UnlockConCommands;
    internal static string IpAddress = string.Empty;
    internal static bool ServerLoaded;
    internal static int? ServerId = null;
    internal static readonly HashSet<ulong> AdminDisabledJoinComms = [];

    // Player Management
    private static readonly HashSet<int> GodPlayers = [];
    internal static readonly HashSet<int> SilentPlayers = [];
    internal static readonly Dictionary<ulong, string> RenamedPlayers = [];
    internal static readonly ConcurrentDictionary<int, PlayerInfo> PlayersInfo = [];
    private static readonly List<DisconnectedPlayer> DisconnectedPlayers = [];

    // Discord Integration
    internal static DiscordManager? DiscordWebhookClientLog;

    // Database Settings
    internal string DbConnectionString = string.Empty;
    internal static Database.Database? Database;

    // Logger
    internal static ILogger? _logger;

    // Menu API and Capabilities
    public static KitsuneMenu Menu { get; private set; } = null!;

    // Shared API
    internal static Api.CS2_SimpleAdminApi? SimpleAdminApi { get; set; }

    // Managers
    internal PermissionManager PermissionManager = new(Database);
    internal BanManager BanManager = new(Database);
    internal MuteManager MuteManager = new(Database);
    internal WarnManager WarnManager = new(Database);
    internal CacheManager? CacheManager = new();
    internal ChatManager ChatManager = new();

    static string firstMessage = "";
    static string secondMessage = "";
}