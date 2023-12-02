using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using static System.Net.Mime.MediaTypeNames;

namespace CS2_SimpleAdmin;
public partial class CS2_SimpleAdmin : BasePlugin, IPluginConfig<CS2_SimpleAdminConfig>
{
	internal string dbConnectionString = string.Empty;
	public override string ModuleName => "CS2-SimpleAdmin";
	public override string ModuleDescription => "";
	public override string ModuleAuthor => "daffyy";
	public override string ModuleVersion => "1.0.0";

	public CS2_SimpleAdminConfig Config { get; set; } = new();

	public override void Load(bool hotReload)
	{
		registerEvents();

		if (hotReload)
		{
			OnMapStart(string.Empty);
		}
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
		};

		dbConnectionString = builder.ConnectionString;

		try
		{
			using (var connection = new MySqlConnection(dbConnectionString))
			{
				connection.Open();

				string sql = @"CREATE TABLE IF NOT EXISTS `sa_bans` (
							  `id` int(11) NOT NULL,
							  `player_steamid` varchar(64) NOT NULL,
							  `player_name` varchar(128) NOT NULL,
							  `admin_steamid` varchar(64) NOT NULL,
							  `admin_name` varchar(128) NOT NULL,
							  `duration` int(11) NOT NULL,
							  `ends` timestamp NOT NULL,
							  `created` timestamp NOT NULL,
							  `status` enum('ACTIVE','UNBANNED','EXPIRED','') NOT NULL DEFAULT 'ACTIVE'
							) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;";

				MySqlCommand command = new MySqlCommand(sql, connection);
				command.ExecuteNonQuery();

				connection.Close();
			}

		}
		catch (MySqlException ex)
		{
			throw new Exception("[CS2-SimpleAdmin] Unable to connect to Database!" + ex.Message);
		}

		Config = config;
	}

	[ConsoleCommand("css_kick")]
	[RequiresPermissions("@css/kick")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player))
			return;

		player!.Pawn.Value!.Freeze();
		string reason = "Brak powodu";

		if (command.ArgCount >= 2)
			reason = command.GetArg(2);

		if (command.ArgCount >= 2)
		{
			player!.PrintToCenter($"{Config.Messages.PlayerKickMessage}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName));
			AddTimer(10.0f, () => Helper.KickPlayer(player!.UserId, reason));

		}
		else
		{
			AddTimer(10.0f, () => Helper.KickPlayer(player!.UserId));
		}

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminKickMessage}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
	}

	[ConsoleCommand("css_ban")]
	[RequiresPermissions("@css/ban")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player))
			return;
		if (command.ArgCount < 2)
			return;

		int time = 0;
		string reason = "Unknown";

		player!.Pawn.Value!.Freeze();

		BanManager _banManager = new(dbConnectionString);

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3)
			reason = command.GetArg(3);

		_banManager.BanPlayer(player, caller, reason, time);

		if (time == 0)
		{
			player!.PrintToCenter($"{Config.Messages.PlayerBanMessagePerm}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName));
			Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminBanMessagePerm}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
		}
		else
		{
			player!.PrintToCenter($"{Config.Messages.PlayerBanMessageTime}".Replace("{REASON}", reason).Replace("{TIME}", time.ToString()).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName));
			Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminBanMessageTime}".Replace("{REASON}", reason).Replace("{TIME}", time.ToString()).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
		}

		AddTimer(10.0f, () => Helper.KickPlayer(player!.UserId));

	}

	[ConsoleCommand("css_slay")]
	[RequiresPermissions("@css/slay")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnSlayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out CCSPlayerController? player))
			return;
		if (!player!.PawnIsAlive)
			return;

		player!.CommitSuicide(false, true);

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminSlayMessage}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
	}

	[ConsoleCommand("css_slap")]
	[RequiresPermissions("@css/slay")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [damage]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnSlapCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out CCSPlayerController? player))
			return;
		if (!player!.PawnIsAlive)
			return;

		int damage = 0;


		if (command.ArgCount >= 2)
		{
			int.TryParse(command.GetArg(2), out damage);
		}

		player!.Pawn.Value!.Slap(damage);

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminSlapMessage}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
	}

	[ConsoleCommand("css_map")]
	[RequiresPermissions("@css/map")]
	[CommandHelper(minArgs: 1, usage: "<mapname>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
	{
		string map = command.GetArg(1);

		if (!Server.IsMapValid(map))
		{
			command.ReplyToCommand($"Map {map} not found.");
			return;
		}

		AddTimer(5f, () =>
		{
			Server.ExecuteCommand($"changelevel {map}");
		});

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminChangeMap}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{MAP}", map)));
	}

	[ConsoleCommand("css_say", "Say to all players.")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		Server.PrintToChatAll(Helper.ReplaceTags(Config.Messages.AdminSayPrefix + command.GetCommandString[command.GetCommandString.IndexOf(' ')..]));
	}

	[ConsoleCommand("css_psay", "Private message a player.")]
	[CommandHelper(2, "<#userid or name> <message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminPrivateSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player))
			return;

		var range = command.GetArg(0).Length + command.GetArg(1).Length + 2;
		var message = command.GetCommandString[range..];

		command.ReplyToCommand(Helper.ReplaceTags($"({player!.PlayerName}) {message}"));
		player.PrintToChat(Helper.ReplaceTags($"({caller!.PlayerName}) {message}"));
	}

	[ConsoleCommand("css_csay", "Say to all players (in center).")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminCenterSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		Helper.PrintToCenterAll(Helper.ReplaceTags(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]));
	}

	[ConsoleCommand("css_hsay", "Say to all players (in hud).")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminHudSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		VirtualFunctions.ClientPrintAll(
			HudDestination.Alert,
			Helper.ReplaceTags(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]),
			0, 0, 0, 0);
	}


	private static bool GetTarget(CommandInfo command, out CCSPlayerController? player)
	{
		var matches = Helper.GetTarget(command.GetArg(1), out player);

		switch (matches)
		{
			case TargetResult.None:
				command.ReplyToCommand($"Target {command.GetArg(1)} not found.");
				return false;
			case TargetResult.Multiple:
				command.ReplyToCommand($"Multiple targets found for \"{command.GetArg(1)}\".");
				return false;
		}

		return true;
	}
}

