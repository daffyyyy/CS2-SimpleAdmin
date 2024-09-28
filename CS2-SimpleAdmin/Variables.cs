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
    public static IStringLocalizer? _localizer;
    public static readonly Dictionary<string, int> VoteAnswers = [];
    public static bool ServerLoaded;
    private static readonly HashSet<int> GodPlayers = [];
    private static readonly HashSet<int> SilentPlayers = [];
    internal static readonly ConcurrentBag<string?> BannedPlayers = [];
    internal static readonly Dictionary<ulong, string> RenamedPlayers = [];
    public static bool VoteInProgress;
    public static int? ServerId = null;
    public static readonly bool UnlockedCommands = CoreConfig.UnlockConCommands;
    internal static readonly Dictionary<int, PlayerInfo> PlayersInfo = [];
    private static readonly List<DisconnectedPlayer> DisconnectedPlayers = [];

    internal static DiscordWebhookClient? DiscordWebhookClientLog;

    internal string DbConnectionString = string.Empty;
    internal static Database.Database? Database;
    internal static string IpAddress = string.Empty;

    internal static ILogger? _logger;
    private static MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>? _cBasePlayerControllerSetPawnFunc;

    // Menus
    internal static IMenuApi? MenuApi;
    private static readonly PluginCapability<IMenuApi> MenuCapability = new("menu:nfcore");

    // Shared
    private Api.CS2_SimpleAdminApi? SimpleAdminApi { get; set; }
}