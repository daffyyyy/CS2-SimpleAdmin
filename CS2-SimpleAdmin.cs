using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Dapper;
using Discord.Webhook;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;

namespace CS2_SimpleAdmin;

[MinimumApiVersion(198)]
public partial class CS2_SimpleAdmin : BasePlugin, IPluginConfig<CS2_SimpleAdminConfig>
{
	public static CS2_SimpleAdmin Instance { get; private set; } = new();

	public static IStringLocalizer? _localizer;
	public static Dictionary<string, int> voteAnswers = new Dictionary<string, int>();
	public static ConcurrentBag<int> godPlayers = new ConcurrentBag<int>();
	public static ConcurrentBag<int> silentPlayers = new ConcurrentBag<int>();
	public static ConcurrentBag<string> bannedPlayers = new ConcurrentBag<string>();
	public static bool TagsDetected = false;
	public static bool voteInProgress = false;
	public static int? ServerId = null;

	public static DiscordWebhookClient? _discordWebhookClientLog;
	public static DiscordWebhookClient? _discordWebhookClientPenalty;

	internal string dbConnectionString = string.Empty;
	internal static Database? _database;

	internal static ILogger? _logger;

	public static MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>? CBasePlayerController_SetPawnFunc = null;
	public override string ModuleName => "CS2-SimpleAdmin";
	public override string ModuleDescription => "Simple admin plugin for Counter-Strike 2 :)";
	public override string ModuleAuthor => "daffyy & Dliix66";
	public override string ModuleVersion => "1.3.6d";

	public CS2_SimpleAdminConfig Config { get; set; } = new();

	public override void Load(bool hotReload)
	{
		Instance = this;

		RegisterEvents();

		if (hotReload)
		{
			OnMapStart(string.Empty);
		}

		CBasePlayerController_SetPawnFunc = new(GameData.GetSignature("CBasePlayerController_SetPawn"));
		_logger = Logger;
	}

	public void OnConfigParsed(CS2_SimpleAdminConfig config)
	{
		if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
		{
			throw new Exception("[CS2-SimpleAdmin] You need to setup Database credentials in config!");
		}

		MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
		{
			Server = config.DatabaseHost,
			Database = config.DatabaseName,
			UserID = config.DatabaseUser,
			Password = config.DatabasePassword,
			Port = (uint)config.DatabasePort,
			Pooling = true,
			MinimumPoolSize = 0,
			MaximumPoolSize = (uint)(Server.MaxPlayers > 21 ? 640 : 210),
			ConnectionIdleTimeout = 30
		};

		dbConnectionString = builder.ConnectionString;
		_database = new(dbConnectionString);

		Task.Run(async () =>
		{
			try
			{
				using MySqlConnection connection = await _database.GetConnectionAsync();
				using MySqlTransaction transaction = await connection.BeginTransactionAsync();

				try
				{
					string sqlFilePath = ModuleDirectory + "/Database/database_setup.sql";
					string sql = await File.ReadAllTextAsync(sqlFilePath);

					await connection.QueryAsync(sql, transaction: transaction);

					await transaction.CommitAsync();
				}
				catch (Exception)
				{
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Unable to connect to the database: {ex.Message}");
				throw;
			}
		});

		Config = config;
		Helper.UpdateConfig(config);

		_localizer = Localizer;

		if (!string.IsNullOrEmpty(Config.Discord.DiscordLogWebhook))
			_discordWebhookClientLog = new(Config.Discord.DiscordLogWebhook);
		if (!string.IsNullOrEmpty(Config.Discord.DiscordPenaltyWebhook))
			_discordWebhookClientPenalty = new(Config.Discord.DiscordPenaltyWebhook);
	}

	private static TargetResult? GetTarget(CommandInfo command)
	{
		TargetResult matches = command.GetArgTargetResult(1);

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

	public static void RemoveFromConcurrentBag(ConcurrentBag<int> bag, int playerSlot)
	{
		List<int> tempList = new List<int>();
		while (!bag.IsEmpty)
		{
			if (bag.TryTake(out int item) && item != playerSlot)
			{
				tempList.Add(item);
			}
		}

		foreach (int item in tempList)
		{
			bag.Add(item);
		}
	}
}