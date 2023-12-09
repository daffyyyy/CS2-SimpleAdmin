using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;

namespace CS2_SimpleAdmin;
[MinimumApiVersion(101)]
public partial class CS2_SimpleAdmin : BasePlugin, IPluginConfig<CS2_SimpleAdminConfig>
{
	public static ConcurrentBag<int> gaggedPlayers = new ConcurrentBag<int>();
	public static bool TagsDetected = false;

	internal string dbConnectionString = string.Empty;
	public override string ModuleName => "CS2-SimpleAdmin";
	public override string ModuleDescription => "";
	public override string ModuleAuthor => "daffyy";
	public override string ModuleVersion => "1.0.5a";

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
                                `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                                `player_steamid` VARCHAR(64),
                                `player_name` VARCHAR(128),
                                `player_ip` VARCHAR(128),
                                `admin_steamid` VARCHAR(64) NOT NULL,
                                `admin_name` VARCHAR(128) NOT NULL,
                                `reason` VARCHAR(255) NOT NULL,
                                `duration` INT NOT NULL,
                                `ends` TIMESTAMP NOT NULL,
                                `created` TIMESTAMP NOT NULL,
                                `status` ENUM('ACTIVE', 'UNBANNED', 'EXPIRED', '') NOT NULL DEFAULT 'ACTIVE'
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;";

				MySqlCommand command = new MySqlCommand(sql, connection);
				command.ExecuteNonQuery();

				sql = @"CREATE TABLE IF NOT EXISTS `sa_mutes` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `player_steamid` varchar(64) NOT NULL,
						 `player_name` varchar(128) NOT NULL,
						 `admin_steamid` varchar(64) NOT NULL,
						 `admin_name` varchar(128) NOT NULL,
						 `reason` varchar(255) NOT NULL,
						 `duration` int(11) NOT NULL,
						 `ends` timestamp NOT NULL,
						 `created` timestamp NOT NULL,
						 `type` enum('GAG','MUTE','') NOT NULL DEFAULT 'GAG',
						 `status` enum('ACTIVE','UNMUTED','EXPIRED','') NOT NULL DEFAULT 'ACTIVE',
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci";

				command = new MySqlCommand(sql, connection);
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

	[ConsoleCommand("css_admin")]
	[RequiresPermissions("@css/generic")]
	public void OnAdminCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (caller == null || !caller.IsValid) return;

		var splitMessage = Config.Messages.AdminHelpCommand.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

		foreach (var line in splitMessage)
		{
			caller.PrintToChat(Helper.ReplaceTags($" {line}"));
		}
	}

	[ConsoleCommand("css_kick")]
	[RequiresPermissions("@css/kick")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player) || player == null || !player.IsValid)
			return;

		if (!caller!.CanTarget(player))
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

		player!.Pawn.Value!.Freeze();
		string reason = "Unknown";

		if (command.ArgCount >= 2)
			reason = command.GetArg(2);

		if (command.ArgCount >= 2)
		{
			player!.PrintToCenter($"{Config.Messages.PlayerKickMessage}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName));
			AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!, reason));
		}
		else
		{
			AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!));
		}

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminKickMessage}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
	}

	[ConsoleCommand("css_gag")]
	[RequiresPermissions("@css/chat")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnGagCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player) || player == null || !player.IsValid)
			return;

		if (!caller!.CanTarget(player) || command.ArgCount < 2)
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

		int time = 0;
		string reason = "Unknown";

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3)
			reason = command.GetArg(3);

		PlayerInfo playerInfo = new PlayerInfo
		{
			SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
			Name = player?.PlayerName,
			IpAddress = player?.IpAddress?.Split(":")[0]
		};

		PlayerInfo adminInfo = new PlayerInfo
		{
			SteamId = caller?.AuthorizedSteamID?.SteamId64.ToString(),
			Name = caller?.PlayerName,
			IpAddress = caller?.IpAddress?.Split(":")[0]
		};

		Task.Run(async () =>
		{
			MuteManager _muteManager = new(dbConnectionString);
			await _muteManager.MutePlayer(playerInfo, adminInfo, reason, time);
		});

		if (TagsDetected)
			NativeAPI.IssueServerCommand($"css_tag_mute {player!.Index.ToString()}");

		if (!gaggedPlayers.Contains((int)player!.Index))
			gaggedPlayers.Add((int)player.Index);

		if (time > 0 && time <= 30)
		{
			AddTimer(time * 60, () =>
			{
				if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

				if (TagsDetected)
					NativeAPI.IssueServerCommand($"css_tag_unmute {player.Index}");

				MuteManager _muteManager = new(dbConnectionString);
				_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 0);
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
		}

		if (time == 0)
		{
			player!.PrintToCenter($"{Config.Messages.PlayerGagMessagePerm}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName));
			Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminGagMessagePerm}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
		}
		else
		{
			player!.PrintToCenter($"{Config.Messages.PlayerGagMessageTime}".Replace("{REASON}", reason).Replace("{TIME}", time.ToString()).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName));
			Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminGagMessageTime}".Replace("{REASON}", reason).Replace("{TIME}", time.ToString()).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
		}
	}

	[ConsoleCommand("css_addgag")]
	[RequiresPermissions("@css/chat")]
	[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnAddGagCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.ArgCount < 2)
			return;
		if (string.IsNullOrEmpty(command.GetArg(1))) return;

		string steamid = command.GetArg(1);

		if (!Helper.IsValidSteamID64(steamid))
		{
			command.ReplyToCommand($"Invalid SteamID64.");
			return;
		}

		int time = 0;
		string reason = "Unknown";

		MuteManager _muteManager = new(dbConnectionString);

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3)
			reason = command.GetArg(3);

		PlayerInfo adminInfo = new PlayerInfo
		{
			SteamId = caller?.AuthorizedSteamID?.SteamId64.ToString(),
			Name = caller?.PlayerName,
			IpAddress = caller?.IpAddress?.Split(":")[0]
		};

		List<CCSPlayerController> matches = Helper.GetPlayerFromSteamid64(steamid);
		if (matches.Count == 1)
		{
			CCSPlayerController? player = matches.FirstOrDefault();
			if (player != null)
			{
				if (!caller!.CanTarget(player))
				{
					command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
					return;
				}

				if (time == 0)
				{
					player!.PrintToCenter($"{Config.Messages.PlayerGagMessagePerm}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName));
					Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminGagMessagePerm}".Replace("{REASON}", reason).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
				}
				else
				{
					player!.PrintToCenter($"{Config.Messages.PlayerGagMessageTime}".Replace("{REASON}", reason).Replace("{TIME}", time.ToString()).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName));
					Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminGagMessageTime}".Replace("{REASON}", reason).Replace("{TIME}", time.ToString()).Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
				}

				if (TagsDetected)
					NativeAPI.IssueServerCommand($"css_tag_mute {player!.Index.ToString()}");

				if (time > 0 && time <= 30)
				{
					AddTimer(time * 60, () =>
					{
						if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

						if (TagsDetected)
							NativeAPI.IssueServerCommand($"css_tag_unmute {player.Index.ToString()}");

						_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 0);
					}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
				}

				if (!gaggedPlayers.Contains((int)player.Index))
					gaggedPlayers.Add((int)player.Index);
			}
		}
		_ = _muteManager.AddMuteBySteamid(steamid, adminInfo, reason, time, 0);
		command.ReplyToCommand($"Gagged player with steamid {steamid}.");
	}

	[ConsoleCommand("css_unmute")]
	[RequiresPermissions("@css/chat")]
	[CommandHelper(minArgs: 1, usage: "<steamid or name> <type [gag/mute]>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnUnmuteCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.GetArg(1).Length <= 1)
		{
			command.ReplyToCommand($"Too short pattern to search.");
			return;
		}

		string pattern = command.GetArg(1);

		MuteManager _muteManager = new(dbConnectionString);

		if (Helper.IsValidSteamID64(pattern))
		{
			List<CCSPlayerController> matches = Helper.GetPlayerFromSteamid64(pattern);
			if (matches.Count == 1)
			{
				CCSPlayerController? player = matches.FirstOrDefault();
				if (player != null)
				{
					if (gaggedPlayers.Contains((int)player.Index))
					{

						if (gaggedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
						{
							gaggedPlayers.Add(removedItem);
						}
					}

					if (TagsDetected)
						NativeAPI.IssueServerCommand($"css_tag_unmute {player!.Index.ToString()}");
				}
			}
		}
		else
		{
			List<CCSPlayerController> matches = Helper.GetPlayerFromName(pattern);
			if (matches.Count == 1)
			{
				CCSPlayerController? player = matches.FirstOrDefault();
				if (player != null)
				{
					if (gaggedPlayers.Contains((int)player.Index))
					{
						if (gaggedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
						{
							gaggedPlayers.Add(removedItem);
						}
					}

					if (TagsDetected)
						NativeAPI.IssueServerCommand($"css_tag_unmute {player!.Index.ToString()}");

					pattern = player.AuthorizedSteamID!.SteamId64.ToString();
				}
			}
		}

		if (command.ArgCount >= 3)
		{
			string? action = command.GetArg(2)?.ToLower();

			if (action == "gag")
			{
				_ = _muteManager.UnmutePlayer(pattern, 0); // Unmute by type 0 (gag)
			}
			else if (action == "mute")
			{
				_ = _muteManager.UnmutePlayer(pattern, 1); // Unmute by type 1 (mute)
			}
		}
		else
		{
			_ = _muteManager.UnmutePlayer(pattern, 2); // Default unmute (all types)
		}

		command.ReplyToCommand($"Unmuted player with pattern {pattern}.");
	}

	[ConsoleCommand("css_ban")]
	[RequiresPermissions("@css/ban")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player) || command.ArgCount < 2)
			return;
		if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

		if (!caller!.CanTarget(player))
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

		int time = 0;
		string reason = "Unknown";

		player!.Pawn.Value!.Freeze();

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3)
			reason = command.GetArg(3);

		PlayerInfo playerInfo = new PlayerInfo
		{
			SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
			Name = player?.PlayerName,
			IpAddress = player?.IpAddress?.Split(":")[0]
		};

		PlayerInfo adminInfo = new PlayerInfo
		{
			SteamId = caller?.AuthorizedSteamID?.SteamId64.ToString(),
			Name = caller?.PlayerName,
			IpAddress = caller?.IpAddress?.Split(":")[0]
		};

		Task.Run(async () =>
		{
			BanManager _banManager = new(dbConnectionString);
			await _banManager.BanPlayer(playerInfo, adminInfo, reason, time);
		});

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

		AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!));
	}

	[ConsoleCommand("css_addban")]
	[RequiresPermissions("@css/ban")]
	[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnAddBanCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.ArgCount < 2)
			return;
		if (string.IsNullOrEmpty(command.GetArg(1))) return;

		string steamid = command.GetArg(1);

		if (!Helper.IsValidSteamID64(steamid))
		{
			command.ReplyToCommand($"Invalid SteamID64.");
			return;
		}

		int time = 0;
		string reason = "Unknown";

		BanManager _banManager = new(dbConnectionString);

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3)
			reason = command.GetArg(3);

		PlayerInfo adminInfo = new PlayerInfo
		{
			SteamId = caller?.AuthorizedSteamID?.SteamId64.ToString(),
			Name = caller?.PlayerName,
			IpAddress = caller?.IpAddress?.Split(":")[0]
		};


		List<CCSPlayerController> matches = Helper.GetPlayerFromSteamid64(steamid);
		if (matches.Count == 1)
		{
			CCSPlayerController? player = matches.FirstOrDefault();
			if (player != null)
			{
				if (!caller!.CanTarget(player))
				{
					command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
					return;
				}

				player!.Pawn.Value!.Freeze();

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

				Task.Run(async () =>
				{
					BanManager _banManager = new(dbConnectionString);
					await _banManager.AddBanBySteamid(steamid, adminInfo, reason, time);
				});

				AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!));
			}
		}
		command.ReplyToCommand($"Banned player with steamid {steamid}.");
	}

	[ConsoleCommand("css_banip")]
	[RequiresPermissions("@css/ban")]
	[CommandHelper(minArgs: 1, usage: "<ip> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnBanIp(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.ArgCount < 2)
			return;
		if (string.IsNullOrEmpty(command.GetArg(1))) return;

		string ipAddress = command.GetArg(1);

		if (!Helper.IsValidIP(ipAddress))
		{
			command.ReplyToCommand($"Invalid IP address.");
			return;
		}

		int time = 0;
		string reason = "Unknown";

		PlayerInfo adminInfo = new PlayerInfo
		{
			SteamId = caller?.AuthorizedSteamID?.SteamId64.ToString(),
			Name = caller?.PlayerName,
			IpAddress = caller?.IpAddress?.Split(":")[0]
		};

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3)
			reason = command.GetArg(3);

		List<CCSPlayerController> matches = Helper.GetPlayerFromIp(ipAddress);
		if (matches.Count == 1)
		{
			CCSPlayerController? player = matches.FirstOrDefault();
			if (player != null)
			{
				if (!caller!.CanTarget(player))
				{
					command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
					return;
				}

				player!.Pawn.Value!.Freeze();

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

				Task.Run(async () =>
				{
					BanManager _banManager = new(dbConnectionString);
					await _banManager.AddBanByIp(ipAddress, adminInfo, reason, time);
				});

				AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!, "Banned"));
			}
		}

		command.ReplyToCommand($"Banned player with IP address {ipAddress}.");
	}

	[ConsoleCommand("css_unban")]
	[RequiresPermissions("@css/unban")]
	[CommandHelper(minArgs: 1, usage: "<steamid or name or ip>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnUnbanCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.GetArg(1).Length <= 1)
		{
			command.ReplyToCommand($"Too short pattern to search.");
			return;
		}

		string pattern = command.GetArg(1);
		BanManager _banManager = new(dbConnectionString);

		_ = _banManager.UnbanPlayer(pattern);

		command.ReplyToCommand($"Unbanned player with pattern {pattern}.");
	}

	[ConsoleCommand("css_slay")]
	[RequiresPermissions("@css/slay")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnSlayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out CCSPlayerController? player) || player == null || !player.IsValid)
			return;

		if (!caller!.CanTarget(player))
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

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
		if (!GetTarget(command, out CCSPlayerController? player) || player == null || !player.IsValid)
			return;

		if (!caller!.CanTarget(player))
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

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

	[ConsoleCommand("css_team")]
	[RequiresPermissions("@css/kick")]
	[CommandHelper(minArgs: 2, usage: "<#userid or name> [<ct/tt/spec>]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnTeamCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out CCSPlayerController? player) || player == null || !player.IsValid)
			return;

		if (!caller!.CanTarget(player))
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

		string teamName = command.GetArg(2).ToLower();
		CsTeam teamNum = CsTeam.Spectator;

		switch (teamName)
		{
			case "ct":
			case "counterterrorist":
				teamNum = CsTeam.CounterTerrorist;
				break;
			case "t":
			case "tt":
			case "terrorist":
				teamNum = CsTeam.Terrorist;
				break;
			default:
				teamNum = CsTeam.Spectator;
				break;
		}

		if (player.TeamNum == ((byte)teamNum))
		{
			command.ReplyToCommand($"{player.PlayerName} is already in selected team!");
			return;
		}

		if (player.PawnIsAlive && teamNum != CsTeam.Spectator)
			player.SwitchTeam(teamNum);
		else
			player.ChangeTeam(teamNum);

		command.ReplyToCommand($"Successfully changed team for {player.PlayerName}");
	}

	[ConsoleCommand("css_map")]
	[RequiresPermissions("@css/changemap")]
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

	[ConsoleCommand("css_wsmap", "Change workshop map.")]
	[ConsoleCommand("css_workshop", "Change workshop map.")]
	[CommandHelper(1, "<name or id>")]
	[RequiresPermissions("@css/changemap")]
	public void OnWorkshopMapCommand(CCSPlayerController? caller, CommandInfo command)
	{
		string? _command = null;
		var map = command.GetArg(1);

		_command = ulong.TryParse(map, out var mapId) ? $"host_workshop_map {mapId}" : $"ds_workshop_changelevel {map}";

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminChangeMap}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{MAP}", map)));

		AddTimer(5f, () =>
		{
			Server.ExecuteCommand(_command);
		});
	}

	[ConsoleCommand("css_asay", "Say to all admins.")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminToAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (caller == null || !caller.IsValid || command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;
		foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/chat")))
		{
			p.PrintToChat($" {ChatColors.Lime}(ADMIN) {ChatColors.Default}{caller.PlayerName}: {command.GetCommandString[command.GetCommandString.IndexOf(' ')..]}");
		}
	}

	[ConsoleCommand("css_say", "Say to all players.")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Messages.AdminSayPrefix}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName) + command.GetCommandString[command.GetCommandString.IndexOf(' ')..]));
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

	[ConsoleCommand("css_noclip", "Noclip a player.")]
	[CommandHelper(1, "<#userid or name>")]
	[RequiresPermissions("@css/cheats")]
	public void OnNoclipCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player) || player == null || !player.IsValid)
			return;

		if (!caller!.CanTarget(player))
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

		player!.Pawn.Value!.ToggleNoclip();

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminNoclipMessage}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
	}

	[ConsoleCommand("css_freeze", "Freeze a player.")]
	[CommandHelper(1, "<#userid or name> [duration]")]
	[RequiresPermissions("@css/slay")]
	public void OnFreezeCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player) || player == null || !player.IsValid)
			return;

		if (!caller!.CanTarget(player))
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

		int time = 0;
		int.TryParse(command.GetArg(2), out time);

		player!.Pawn.Value!.Freeze();

		if (time > 0)
			AddTimer(time, () => player.Pawn.Value!.Unfreeze());

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminFreezeMessage}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
	}

	[ConsoleCommand("css_unfreeze", "Unfreeze a player.")]
	[CommandHelper(1, "<#userid or name>")]
	[RequiresPermissions("@css/slay")]
	public void OnUnfreezeCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player))
			return;

		player!.Pawn.Value!.Unfreeze();

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminUnFreezeMessage}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
	}

	[ConsoleCommand("css_respawn", "Respawn a dead player.")]
	[CommandHelper(1, "<#userid or name>")]
	[RequiresPermissions("@css/cheats")]
	public void OnRespawnCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!GetTarget(command, out var player) || player == null || !player.IsValid)
			return;

		if (!caller!.CanTarget(player))
		{
			command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
			return;
		}

		player!.Respawn();

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminRespawnMessage}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{PLAYER}", player.PlayerName)));
	}

	[ConsoleCommand("css_cvar", "Change a cvar.")]
	[CommandHelper(2, "<cvar> <value>")]
	[RequiresPermissions("@css/cvar")]
	public void OnCvarCommand(CCSPlayerController? caller, CommandInfo command)
	{
		var cvar = ConVar.Find(command.GetArg(1));

		if (cvar == null)
		{
			command.ReplyToCommand($"Cvar \"{command.GetArg(1)}\" not found.");
			return;
		}

		if (cvar.Name.Equals("sv_cheats") && !AdminManager.PlayerHasPermissions(caller, "@css/cheats"))
		{
			command.ReplyToCommand($"You don't have permissions to change \"{command.GetArg(1)}\".");
			return;
		}

		var value = command.GetArg(2);

		Server.ExecuteCommand($"{cvar.Name} {value}");

		command.ReplyToCommand($"{caller!.PlayerName} changed cvar {cvar.Name} to {value}.");
		Logger.LogInformation($"{caller.PlayerName} changed cvar {cvar.Name} to {value}.");
	}

	[ConsoleCommand("css_rcon", "Run a server console command.")]
	[CommandHelper(1, "<command>")]
	[RequiresPermissions("@css/rcon")]
	public void OnRcomCommand(CCSPlayerController? caller, CommandInfo command)
	{
		Server.ExecuteCommand(command.ArgString);

		Logger.LogInformation($"{caller!.PlayerName} executed command ({command.ArgString}).");
	}

    [ConsoleCommand("css_give")]
	[RequiresPermissions("@css/cheats")]
	[CommandHelper(minArgs: 2, usage: "<#UserId Or Name> <WeaponName>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnGiveCommand(CCSPlayerController? caller, CommandInfo command)
	{

		if(!GetTarget(command, out var player) || player == null || !player.IsValid) return;

		string weapon = command.GetArg(2);
		player.GiveNamedItem(weapon);

		Server.PrintToChatAll(Helper.ReplaceTags($" {Config.Prefix} {Config.Messages.AdminChangeMap}".Replace("{ADMIN}", caller?.PlayerName == null ? "Console" : caller.PlayerName).Replace("{WEAPON}", weapon)));
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

