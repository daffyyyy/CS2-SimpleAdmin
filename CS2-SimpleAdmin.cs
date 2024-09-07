﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Discord.Webhook;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Models;

namespace CS2_SimpleAdmin;

[MinimumApiVersion(260)]
public partial class CS2_SimpleAdmin : BasePlugin, IPluginConfig<CS2_SimpleAdminConfig>
{
	internal static CS2_SimpleAdmin Instance { get; private set; } = new();

	public static IStringLocalizer? _localizer;
	public static readonly Dictionary<string, int> VoteAnswers = [];
	public static bool ServerLoaded;
	private static readonly HashSet<int> GodPlayers = [];
	private static readonly HashSet<int> SilentPlayers = [];
	internal static readonly ConcurrentBag<string?> BannedPlayers = [];
	internal static readonly Dictionary<ulong, string> RenamedPlayers = [];
	public static bool TagsDetected;
	public static bool VoteInProgress;
	public static int? ServerId = null;
	public static readonly bool UnlockedCommands = CoreConfig.UnlockConCommands;
	internal static readonly Dictionary<int, PlayerInfo> PlayersInfo = [];
	internal static readonly List<DisconnectedPlayer> DisconnectedPlayers = [];

	internal static DiscordWebhookClient? DiscordWebhookClientLog;

	internal string DbConnectionString = string.Empty;
	internal static Database.Database? Database;

	internal static ILogger? _logger;
	private static MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>? _cBasePlayerControllerSetPawnFunc;
	
	public override string ModuleName => "CS2-SimpleAdmin" + (Helper.IsDebugBuild ? " (DEBUG)" : " (RELEASE)");
	public override string ModuleDescription => "Simple admin plugin for Counter-Strike 2 :)";
	public override string ModuleAuthor => "daffyy & Dliix66";
	public override string ModuleVersion => "1.6.0a";

	public CS2_SimpleAdminConfig Config { get; set; } = new();

	public override void Load(bool hotReload)
	{
		Instance = this;

		RegisterEvents();

		if (hotReload)
		{
			ServerLoaded = false;
			OnGameServerSteamAPIActivated();
			OnMapStart(string.Empty);

			AddTimer(2.0f, () =>
			{
				if (Database == null) return;
				
				Helper.GetValidPlayers().ForEach(player =>
				{
					var playerManager = new PlayerManager();
					playerManager.LoadPlayerData(player);
				});
			});
		}

		_cBasePlayerControllerSetPawnFunc = new MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>(GameData.GetSignature("CBasePlayerController_SetPawn"));
	}

	public override void Unload(bool hotReload)
	{
		if (hotReload) return;

		RemoveListener<Listeners.OnMapStart>(OnMapStart);
		RemoveCommandListener("say", OnCommandSay, HookMode.Post);
		RemoveCommandListener("say_team", OnCommandTeamSay, HookMode.Post);
	}

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		AddTimer(3.0f, () => ReloadAdmins(null));
	}

	public void OnConfigParsed(CS2_SimpleAdminConfig config)
	{
		if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
		{
			throw new Exception("[CS2-SimpleAdmin] You need to setup Database credentials in config!");
		}

		Instance = this;
		_logger = Logger;

		MySqlConnectionStringBuilder builder = new()
		{
			Server = config.DatabaseHost,
			Database = config.DatabaseName,
			UserID = config.DatabaseUser,
			Password = config.DatabasePassword,
			Port = (uint)config.DatabasePort,
			Pooling = true,
			MinimumPoolSize = 0,
			MaximumPoolSize = 640,
		};

		DbConnectionString = builder.ConnectionString;
		Database = new Database.Database(DbConnectionString);

		if (!Database.CheckDatabaseConnection())
		{
			Logger.LogError("Unable connect to database!");
			Unload(false);
			return;
		}

		Task.Run(() => Database.DatabaseMigration());

		Config = config;
		Helper.UpdateConfig(config);

		if (!Directory.Exists(ModuleDirectory + "/data"))
		{
			Directory.CreateDirectory(ModuleDirectory + "/data");
		}

		_localizer = Localizer;

		if (!string.IsNullOrEmpty(Config.Discord.DiscordLogWebhook))
			DiscordWebhookClientLog = new DiscordWebhookClient(Config.Discord.DiscordLogWebhook);

		PluginInfo.ShowAd(ModuleVersion);
		if (Config.EnableUpdateCheck)
			Task.Run(async () => await PluginInfo.CheckVersion(ModuleVersion, _logger));
	}

	private static TargetResult? GetTarget(CommandInfo command)
	{
		var matches = command.GetArgTargetResult(1);

		if (!matches.Any())
		{
			command.ReplyToCommand($"Target {command.GetArg(1)} not found.");
			return null;
		}

		if (command.GetArg(1).StartsWith('@'))
			return matches;

		if (matches.Count() == 1)
			return matches;

		command.ReplyToCommand($"Multiple targets found for \"{command.GetArg(1)}\".");
		return null;
	}
}