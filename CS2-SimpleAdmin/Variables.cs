using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CS2_SimpleAdmin.Models;
using CS2_SimpleAdminApi;
using Discord.Webhook;
using MenuManager;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    // Paths
    internal static string ConfigDirectory = Path.Combine(Application.RootDirectory, "configs/plugins/CS2-SimpleAdmin");

    // Localization
    public static IStringLocalizer? _localizer;

    // Voting System
    public static readonly Dictionary<string, int> VoteAnswers = [];
    public static bool VoteInProgress;

    // Command and Server Settings
    public static readonly bool UnlockedCommands = CoreConfig.UnlockConCommands;
    internal static string IpAddress = string.Empty;
    public static bool ServerLoaded;
    public static int? ServerId = null;
    internal static readonly HashSet<ulong> AdminDisabledJoinComms = [];

    // Player Management
    private static readonly HashSet<int> GodPlayers = [];
    private static readonly HashSet<int> SilentPlayers = [];
    internal static readonly ConcurrentBag<string?> BannedPlayers = [];
    internal static readonly Dictionary<ulong, string> RenamedPlayers = [];
    internal static readonly Dictionary<int, PlayerInfo> PlayersInfo = [];
    private static readonly List<DisconnectedPlayer> DisconnectedPlayers = [];

    // Discord Integration
    internal static DiscordWebhookClient? DiscordWebhookClientLog;

    // Database Settings
    internal string DbConnectionString = string.Empty;
    internal static Database.Database? Database;

    // Logger
    internal static ILogger? _logger;

    // Memory Function (Game-related)
    private static MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>? _cBasePlayerControllerSetPawnFunc;

    // Menu API and Capabilities
    internal static IMenuApi? MenuApi;
    private static readonly PluginCapability<IMenuApi> MenuCapability = new("menu:nfcore");

    // Shared API
    private Api.CS2_SimpleAdminApi? SimpleAdminApi { get; set; }
}