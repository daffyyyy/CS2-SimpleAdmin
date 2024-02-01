using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;
using System.Text;

namespace CS2_SimpleAdmin;

[MinimumApiVersion(159)]
public partial class CS2_SimpleAdmin : BasePlugin, IPluginConfig<CS2_SimpleAdminConfig>
{
	public static IStringLocalizer? _localizer;
	public static ConcurrentBag<string> gaggedPlayers = new ConcurrentBag<string>();
	//public static ConcurrentBag<int> mutedPlayers = new ConcurrentBag<int>();
	public static Dictionary<string, int> voteAnswers = new Dictionary<string, int>();
	public static HashSet<ushort> godPlayers = new HashSet<ushort>();
	public static List<ushort> silentPlayers = new List<ushort>();
	public static HashSet<string> bannedPlayers = new HashSet<string>();
	public static bool TagsDetected = false;
	public static bool voteInProgress = false;
	public static int? ServerId = null;

	internal string dbConnectionString = string.Empty;
	public override string ModuleName => "CS2-SimpleAdmin";
	public override string ModuleDescription => "Simple admin plugin for Counter-Strike 2 :)";
	public override string ModuleAuthor => "daffyy";
	public override string ModuleVersion => "1.2.9d";

	private MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>? CBasePlayerController_SetPawnFunc;

	public CS2_SimpleAdminConfig Config { get; set; } = new();

	public override void Load(bool hotReload)
	{
		registerEvents();

		if (hotReload)
		{
			OnMapStart(string.Empty);
		}

		CBasePlayerController_SetPawnFunc = new MemoryFunctionVoid<CBasePlayerController, CCSPlayerPawn, bool, bool>(@"\x55\x48\x89\xE5\x41\x57\x41\x56\x41\x55\x41\x54\x49\x89\xFC\x53\x48\x89\xF3\x48\x81\xEC\xC8\x00\x00\x00");
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
								`server_id` INT NULL,
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
						 `server_id` INT NULL,
						 `status` enum('ACTIVE','UNMUTED','EXPIRED','') NOT NULL DEFAULT 'ACTIVE',
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci";

				command = new MySqlCommand(sql, connection);
				command.ExecuteNonQuery();

				sql = @"CREATE TABLE IF NOT EXISTS `sa_admins` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `player_steamid` varchar(64) NOT NULL,
						 `player_name` varchar(128) NOT NULL,
						 `flags` TEXT NOT NULL,
						 `immunity` varchar(64) NOT NULL DEFAULT '0',
						 `server_id` INT NULL,
						 `ends` timestamp NULL,
						 `created` timestamp NOT NULL,
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci";

				command = new MySqlCommand(sql, connection);
				command.ExecuteNonQuery();

				sql = @"CREATE TABLE IF NOT EXISTS `sa_servers` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `address` varchar(64) NOT NULL,
						 `hostname` varchar(64) NOT NULL,
						 PRIMARY KEY (`id`),
						 UNIQUE KEY `address` (`address`)
						) ENGINE=InnoDB AUTO_INCREMENT=36 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci
						";

				command = new MySqlCommand(sql, connection);
				command.ExecuteNonQuery();

				connection.Close();
			}
		}
		catch (Exception ex)
		{
			Logger.LogError("Unable to connect to database!");
			Logger.LogDebug(ex.Message);
			throw new Exception("[CS2-SimpleAdmin] Unable to connect to Database!");
		}

		Config = config;
		_localizer = Localizer;
	}

	[ConsoleCommand("css_admin")]
	[RequiresPermissions("@css/generic")]
	public void OnAdminCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (caller == null || !caller.IsValid) return;

		var splitMessage = _localizer!["sa_adminhelp"].ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

		foreach (var line in splitMessage)
		{
			caller.PrintToChat(Helper.ReplaceTags($" {line}"));
		}
	}

	[ConsoleCommand("css_addadmin")]
	[CommandHelper(minArgs: 4, usage: "<steamid> <name> <flags/groups> <immunity> <duration>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	[RequiresPermissions("@css/root")]
	public void OnAddAdminCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!Helper.IsValidSteamID64(command.GetArg(1)))
		{
			command.ReplyToCommand($"Invalid SteamID64.");
			return;
		}
		if (command.GetArg(2).Length <= 0)
		{
			command.ReplyToCommand($"Invalid player name.");
			return;
		}
		if (!command.GetArg(3).Contains("@") && !command.GetArg(3).Contains("#"))
		{
			command.ReplyToCommand($"Invalid flag or group.");
			return;
		}

		string steamid = command.GetArg(1);
		string name = command.GetArg(2);
		string flags = command.GetArg(3);
		bool globalAdmin = command.GetArg(4).ToLower().Equals("-g") || command.GetArg(5).ToLower().Equals("-g") || command.GetArg(6).ToLower().Equals("-g");
		int immunity = 0;
		int.TryParse(command.GetArg(4), out immunity);
		int time = 0;
		int.TryParse(command.GetArg(5), out time);

		AdminSQLManager _adminManager = new(dbConnectionString);
		_ = _adminManager.AddAdminBySteamId(steamid, name, flags, immunity, time, globalAdmin);

		command.ReplyToCommand($"Added '{flags}' flags to '{name}' ({steamid})");
	}

	[ConsoleCommand("css_deladmin")]
	[CommandHelper(minArgs: 1, usage: "<steamid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	[RequiresPermissions("@css/root")]
	public void OnDelAdminCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (!Helper.IsValidSteamID64(command.GetArg(1)))
		{
			command.ReplyToCommand($"Invalid SteamID64.");
			return;
		}

		string steamid = command.GetArg(1);
		bool globalDelete = command.GetArg(2).ToLower().Equals("-g");

		AdminSQLManager _adminManager = new(dbConnectionString);
		_ = _adminManager.DeleteAdminBySteamId(steamid, globalDelete);

		AddTimer(2, () =>
		{
			if (!string.IsNullOrEmpty(steamid) && SteamID.TryParse(steamid, out var steamId) && steamId != null)
			{
				if (AdminSQLManager._adminCacheSet.Contains(steamId))
				{
					AdminSQLManager._adminCacheSet.Remove(steamId);
					AdminSQLManager._adminCacheTimestamps.Remove(steamId);
				}

				AdminManager.ClearPlayerPermissions(steamId);
				AdminManager.RemovePlayerAdminData(steamId);
			}
		});

		command.ReplyToCommand($"Removed flags from '{steamid}'");
	}

	[ConsoleCommand("css_reladmin")]
	[CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	[RequiresPermissions("@css/root")]
	public void OnRelAdminCommand(CCSPlayerController? caller, CommandInfo command)
	{
		foreach (SteamID steamId in AdminSQLManager._adminCacheSet)
		{
			if (AdminSQLManager._adminCacheSet.Contains(steamId))
			{
				AdminSQLManager._adminCacheSet.Remove(steamId);
				AdminSQLManager._adminCacheTimestamps.Remove(steamId);
			}

			AdminManager.ClearPlayerPermissions(steamId);
			AdminManager.RemovePlayerAdminData(steamId);
		}

		AdminSQLManager _adminManager = new(dbConnectionString);
		_ = _adminManager.GiveAllFlags();

		command.ReplyToCommand("Reloaded sql admins");
	}

	[ConsoleCommand("css_stealth")]
	[ConsoleCommand("css_hide")]
	[CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
	[RequiresPermissions("@css/kick")]
	public void OnHideCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (caller == null || caller.UserId == null) return;

		if (silentPlayers.Contains((ushort)caller.UserId))
		{
			silentPlayers.Remove((ushort)caller.UserId);
			caller.ChangeTeam(CsTeam.Spectator);
			caller.PrintToChat($"You aren't hidden now!");
			if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				_ = SendWebhookMessage($"{caller.PlayerName} isn't hidden now.");
		}
		else
		{
			silentPlayers.Add((ushort)caller.UserId);
			Server.ExecuteCommand("sv_disable_teamselect_menu 1");
			Server.NextFrame(() =>
			{
				if (caller.PlayerPawn.Value != null && caller.PawnIsAlive)
					caller.PlayerPawn.Value.CommitSuicide(true, false);

				AddTimer(1.0f, () => { caller.ChangeTeam(CsTeam.Spectator); });
				AddTimer(1.1f, () => { caller.ChangeTeam(CsTeam.None); });
				caller.PrintToChat($"You are hidden now!");
				if (Config.DiscordWebhook.Length > 0 && _localizer != null)
					_ = SendWebhookMessage($"{caller.PlayerName} is hidden now.");
			});
			Server.NextFrame(() =>
			{
				AddTimer(1.25f, () => { Server.ExecuteCommand("sv_disable_teamselect_menu 0"); });
			});
		}
	}

	[ConsoleCommand("css_who")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	[RequiresPermissions("@css/generic")]
	public void OnWhoCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && !player.IsBot && !player.IsHLTV).ToList();

		BanManager _banManager = new(dbConnectionString, Config);
		MuteManager _muteManager = new(dbConnectionString);

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				PlayerInfo playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Index = (int)player.Index,
					SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
					Name = player?.PlayerName,
					IpAddress = player?.IpAddress?.Split(":")[0]
				};

				Task.Run(async () =>
				{
					int totalBans = 0;
					int totalMutes = 0;

					totalBans = await _banManager.GetPlayerBans(playerInfo);
					totalMutes = await _muteManager.GetPlayerMutes(playerInfo.SteamId!);

					Server.NextFrame(() =>
					{
						if (caller != null)
						{
							caller!.PrintToConsole($"--------- INFO ABOUT \"{playerInfo.Name}\" ---------");

							caller!.PrintToConsole($"• Clan: \"{player!.Clan}\" Name: \"{playerInfo.Name}\"");
							caller!.PrintToConsole($"• UserID: \"{playerInfo.UserId}\"");
							if (playerInfo.SteamId != null)
								caller!.PrintToConsole($"• SteamID64: \"{playerInfo.SteamId}\"");
							if (player.AuthorizedSteamID != null)
							{
								caller!.PrintToConsole($"• SteamID2: \"{player.AuthorizedSteamID.SteamId2}\"");
								caller!.PrintToConsole($"• Community link: \"{player.AuthorizedSteamID.ToCommunityUrl()}\"");
							}
							if (playerInfo.IpAddress != null)
								caller!.PrintToConsole($"• IP Address: \"{playerInfo.IpAddress}\"");
							caller!.PrintToConsole($"• Ping: \"{player.Ping}\"");
							if (player.AuthorizedSteamID != null)
							{
								caller!.PrintToConsole($"• Total Bans: \"{totalBans}\"");
								caller!.PrintToConsole($"• Total Mutes: \"{totalMutes}\"");
							}

							caller!.PrintToConsole($"--------- END INFO ABOUT \"{player.PlayerName}\" ---------");
						}
						else
						{
							Server.PrintToConsole($"--------- INFO ABOUT \"{playerInfo.Name}\" ---------");

							Server.PrintToConsole($"• Clan: \"{player!.Clan}\" Name: \"{playerInfo.Name}\"");
							Server.PrintToConsole($"• UserID: \"{playerInfo.UserId}\"");
							if (playerInfo.SteamId != null)
								Server.PrintToConsole($"• SteamID64: \"{playerInfo.SteamId}\"");
							if (player.AuthorizedSteamID != null)
							{
								Server.PrintToConsole($"• SteamID2: \"{player.AuthorizedSteamID.SteamId2}\"");
								Server.PrintToConsole($"• Community link: \"{player.AuthorizedSteamID.ToCommunityUrl()}\"");
							}
							if (playerInfo.IpAddress != null)
								Server.PrintToConsole($"• IP Address: \"{playerInfo.IpAddress}\"");
							Server.PrintToConsole($"• Ping: \"{player.Ping}\"");
							if (player.AuthorizedSteamID != null)
							{
								Server.PrintToConsole($"• Total Bans: \"{totalBans}\"");
								Server.PrintToConsole($"• Total Mutes: \"{totalMutes}\"");
							}

							Server.PrintToConsole($"--------- END INFO ABOUT \"{player.PlayerName}\" ---------");
						}
					});
				});
			}
		});
	}

	[ConsoleCommand("css_players")]
	[CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	[RequiresPermissions("@css/generic")]
	public void OnPlayersCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV).ToList();

		if (caller != null)
		{
			caller!.PrintToConsole($"--------- PLAYER LIST ---------");
			playersToTarget.ForEach(player =>
			{
				caller!.PrintToConsole($"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{player.IpAddress?.Split(":")[0]}\" SteamID64: \"{player.AuthorizedSteamID?.SteamId64}\")");
			});
			caller!.PrintToConsole($"--------- END PLAYER LIST ---------");
		}
		else
		{
			Server.PrintToConsole($"--------- PLAYER LIST ---------");
			playersToTarget.ForEach(player =>
			{
				Server.PrintToConsole($"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{player.IpAddress?.Split(":")[0]}\" SteamID64: \"{player.AuthorizedSteamID?.SteamId64}\")");
			});
			Server.PrintToConsole($"--------- END PLAYER LIST ---------");
		}
	}

	[ConsoleCommand("css_kick")]
	[RequiresPermissions("@css/kick")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
	{
		string reason = "Unknown";

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid).ToList();

		if (playersToTarget.Count > 1 && Config.DisableDangerousCommands)
		{
			return;
		}

		if (command.ArgCount >= 2 && command.GetArg(2).Length > 0)
			reason = command.GetArg(2);

		targets.Players.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				if (player.PawnIsAlive)
				{
					player.Pawn.Value!.Freeze();
					player.CommitSuicide(true, true);
				}

				if (command.ArgCount >= 2)
				{
					player.PrintToCenter(_localizer!["sa_player_kick_message", reason, caller == null ? "Console" : caller.PlayerName]);
					AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!, reason));
				}
				else
				{
					AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!));
				}

				if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_kick_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason]);
					Server.PrintToChatAll(sb.ToString());
				}

				if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				{
					LocalizedString localizedMessage = _localizer["sa_admin_kick_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason];
					_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
				}
			}
		});
	}

	[ConsoleCommand("css_gag")]
	[RequiresPermissions("@css/chat")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnGagCommand(CCSPlayerController? caller, CommandInfo command)
	{
		int time = 0;
		string reason = "Unknown";

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid).ToList();

		if (playersToTarget.Count > 1 && Config.DisableDangerousCommands)
		{
			return;
		}

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
			reason = command.GetArg(3);

		MuteManager _muteManager = new(dbConnectionString);

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
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
					await _muteManager.MutePlayer(playerInfo, adminInfo, reason, time);
				});

				if (TagsDetected)
					NativeAPI.IssueServerCommand($"css_tag_mute {player!.SteamID.ToString()}");

				if (player != null && player.SteamID.ToString() != "" && !gaggedPlayers.Contains(player.SteamID.ToString()))
					gaggedPlayers.Add(player.SteamID.ToString());

				if (time > 0 && time <= 30)
				{
					AddTimer(time * 60, () =>
					{
						if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

						if (TagsDetected)
							NativeAPI.IssueServerCommand($"css_tag_unmute {player!.SteamID.ToString()}");

						if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
						{
							if (gaggedPlayers.TryTake(out string? removedItem) && removedItem != player.SteamID.ToString())
							{
								gaggedPlayers.Add(removedItem);
							}
						}

						//MuteManager _muteManager = new(dbConnectionString);
						//_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 0);
					}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
				}

				if (time == 0)
				{
					player!.PrintToCenter(_localizer!["sa_player_gag_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_gag_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_gag_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
				else
				{
					player!.PrintToCenter(_localizer!["sa_player_gag_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_gag_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_gag_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
			}
		});
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

		if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
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
			if (player != null && player.IsValid)
			{
				if (!caller!.CanTarget(player))
				{
					command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
					return;
				}

				if (time == 0)
				{
					player!.PrintToCenter(_localizer!["sa_player_gag_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_gag_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_gag_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
				else
				{
					player!.PrintToCenter(_localizer!["sa_player_gag_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_gag_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_gag_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}

				if (TagsDetected)
					NativeAPI.IssueServerCommand($"css_tag_mute {player!.SteamID.ToString()}");

				if (time > 0 && time <= 30)
				{
					AddTimer(time * 60, () =>
					{
						if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

						if (TagsDetected)
							NativeAPI.IssueServerCommand($"css_tag_unmute {player!.SteamID.ToString()}");

						if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
						{
							if (gaggedPlayers.TryTake(out string? removedItem) && removedItem != player.SteamID.ToString())
							{
								gaggedPlayers.Add(removedItem);
							}
						}

						//_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 0);
					}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
				}

				if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
					gaggedPlayers.Add(player.SteamID.ToString());
			}
		}
		_ = _muteManager.AddMuteBySteamid(steamid, adminInfo, reason, time, 0);
		command.ReplyToCommand($"Gagged player with steamid {steamid}.");
	}

	[ConsoleCommand("css_ungag")]
	[RequiresPermissions("@css/chat")]
	[CommandHelper(minArgs: 1, usage: "<steamid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnUngagCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.GetArg(1).Length <= 1)
		{
			command.ReplyToCommand($"Too short pattern to search.");
			return;
		}

		bool found = false;

		string pattern = command.GetArg(1);
		MuteManager _muteManager = new(dbConnectionString);

		if (Helper.IsValidSteamID64(pattern))
		{
			List<CCSPlayerController> matches = Helper.GetPlayerFromSteamid64(pattern);
			if (matches.Count == 1)
			{
				CCSPlayerController? player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
					{
						if (gaggedPlayers.TryTake(out string? removedItem) && removedItem != player.SteamID.ToString())
						{
							gaggedPlayers.Add(removedItem);
						}
					}

					if (TagsDetected)
						NativeAPI.IssueServerCommand($"css_tag_unmute {player!.SteamID.ToString()}");

					found = true;
				}
			}
		}
		else
		{
			List<CCSPlayerController> matches = Helper.GetPlayerFromName(pattern);
			if (matches.Count == 1)
			{
				CCSPlayerController? player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
					{
						if (gaggedPlayers.TryTake(out string? removedItem) && removedItem != player.SteamID.ToString())
						{
							gaggedPlayers.Add(removedItem);
						}
					}

					if (TagsDetected)
						NativeAPI.IssueServerCommand($"css_tag_unmute {player!.SteamID.ToString()}");

					pattern = player!.AuthorizedSteamID!.SteamId64.ToString();

					found = true;
				}
			}
		}
		if (found)
		{
			_ = _muteManager.UnmutePlayer(pattern, 0); // Unmute by type 0 (gag)
			command.ReplyToCommand($"Ungaged player with pattern {pattern}.");
			return;
		}

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid).ToList();

		if (playersToTarget.Count > 1 && Config.DisableDangerousCommands)
		{
			return;
		}

		if (playersToTarget.Count > 1)
		{
			playersToTarget.ForEach(player =>
			{
				if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
				{
					if (gaggedPlayers.TryTake(out string? removedItem) && removedItem != player.SteamID.ToString())
					{
						gaggedPlayers.Add(removedItem);
					}
				}

				if (player!.AuthorizedSteamID != null)
					_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 0); // Unmute by type 0 (gag)

				if (TagsDetected)
					NativeAPI.IssueServerCommand($"css_tag_unmute {player!.SteamID.ToString()}");
			});

			command.ReplyToCommand($"Ungaged player with pattern {pattern}.");
			if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				_ = SendWebhookMessage($"Ungaged player with pattern {pattern}.");
			return;
		}
	}

	[ConsoleCommand("css_mute")]
	[RequiresPermissions("@css/chat")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnMuteCommand(CCSPlayerController? caller, CommandInfo command)
	{
		int time = 0;
		string reason = "Unknown";

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid).ToList();

		if (playersToTarget.Count > 1 && Config.DisableDangerousCommands)
		{
			return;
		}

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
			reason = command.GetArg(3);

		MuteManager _muteManager = new(dbConnectionString);

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
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

				/*
				if (!mutedPlayers.Contains((ushort)player.UserId))
					mutedPlayers.Add((ushort)player.UserId);
				*/

				player!.VoiceFlags = VoiceFlags.Muted;

				Task.Run(async () =>
				{
					await _muteManager.MutePlayer(playerInfo, adminInfo, reason, time, 1);
				});


				if (time > 0 && time <= 30)
				{
					AddTimer(time * 60, () =>
					{
						if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

						//MuteManager _muteManager = new(dbConnectionString);
						//_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 1);

						/*
						if (mutedPlayers.Contains((int)player.Index))
						{
							if (mutedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
							{
								mutedPlayers.Add(removedItem);
							}
						}
						*/

						player.VoiceFlags = VoiceFlags.Normal;
					}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
				}

				if (time == 0)
				{
					player!.PrintToCenter(_localizer!["sa_player_mute_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_mute_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_mute_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
				else
				{
					player!.PrintToCenter(_localizer!["sa_player_mute_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_mute_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_mute_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
			}
		});
	}

	[ConsoleCommand("css_addmute")]
	[RequiresPermissions("@css/chat")]
	[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnAddMuteCommand(CCSPlayerController? caller, CommandInfo command)
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

		if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
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
			if (player != null && player.IsValid)
			{
				if (!caller!.CanTarget(player))
				{
					command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
					return;
				}

				if (time == 0)
				{
					player!.PrintToCenter(_localizer!["sa_player_mute_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_mute_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_mute_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
				else
				{
					player!.PrintToCenter(_localizer!["sa_player_mute_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_mute_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_mute_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}

				/*
				if (!mutedPlayers.Contains((ushort)player.UserId))
					mutedPlayers.Add((ushort)player.UserId);
				*/

				if (time > 0 && time <= 30)
				{
					AddTimer(time * 60, () =>
					{
						if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

						/*
						if (mutedPlayers.Contains((int)player.Index))
						{
							if (mutedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
							{
								mutedPlayers.Add(removedItem);
							}
						}
						*/

						player.VoiceFlags = VoiceFlags.Normal;

						_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 1);
					}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
				}
			}
		}
		_ = _muteManager.AddMuteBySteamid(steamid, adminInfo, reason, time, 1);
		command.ReplyToCommand($"Muted player with steamid {steamid}.");
	}

	[ConsoleCommand("css_unmute")]
	[RequiresPermissions("@css/chat")]
	[CommandHelper(minArgs: 1, usage: "<steamid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnUnmuteCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.GetArg(1).Length <= 1)
		{
			command.ReplyToCommand($"Too short pattern to search.");
			return;
		}

		string pattern = command.GetArg(1);
		bool found = false;
		MuteManager _muteManager = new(dbConnectionString);

		if (Helper.IsValidSteamID64(pattern))
		{
			List<CCSPlayerController> matches = Helper.GetPlayerFromSteamid64(pattern);
			if (matches.Count == 1)
			{
				CCSPlayerController? player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					/*
					if (mutedPlayers.Contains((int)player.Index))
					{
						if (mutedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
						{
							mutedPlayers.Add(removedItem);
						}
					}
					*/

					player.VoiceFlags = VoiceFlags.Normal;
					found = true;
				}
			}
		}
		else
		{
			List<CCSPlayerController> matches = Helper.GetPlayerFromName(pattern);
			if (matches.Count == 1)
			{
				CCSPlayerController? player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					/*
					if (mutedPlayers.Contains((int)player.Index))
					{
						if (mutedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
						{
							mutedPlayers.Add(removedItem);
						}
					}
					*/

					player.VoiceFlags = VoiceFlags.Normal;
					pattern = player.AuthorizedSteamID!.SteamId64.ToString();
					found = true;
				}
			}
		}

		if (found)
		{
			_ = _muteManager.UnmutePlayer(pattern, 1); // Unmute by type 1 (mute)
			command.ReplyToCommand($"Unmuted player with pattern {pattern}.");
			return;
		}

		TargetResult? targets = GetTarget(command);
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid).ToList();

		if (playersToTarget.Count > 1 && Config.DisableDangerousCommands)
		{
			return;
		}

		if (playersToTarget.Count > 1)
		{
			playersToTarget.ForEach(player =>
			{
				/*
				if (mutedPlayers.Contains((int)player.Index))
				{
					if (mutedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
					{
						mutedPlayers.Add(removedItem);
					}
				}
				*/

				if (player.AuthorizedSteamID != null)
					_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 1); // Unmute by type 1 (mute)

				player.VoiceFlags = VoiceFlags.Normal;
			});

			command.ReplyToCommand($"Unmuted player with pattern {pattern}.");
			if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				_ = SendWebhookMessage($"Unmuted player with pattern {pattern}.");
			return;
		}
	}

	[ConsoleCommand("css_ban")]
	[RequiresPermissions("@css/ban")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.ArgCount < 2)
			return;

		int time = 0;
		string reason = "Unknown";

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid).ToList();

		if (playersToTarget.Count > 1 && Config.DisableDangerousCommands)
		{
			return;
		}

		BanManager _banManager = new(dbConnectionString, Config);

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
			reason = command.GetArg(3);

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				if (player.PawnIsAlive)
				{
					player.Pawn.Value!.Freeze();
					player.CommitSuicide(true, true);
				}

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
					await _banManager.BanPlayer(playerInfo, adminInfo, reason, time);
				});

				AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player!.UserId!));

				if (time == 0)
				{
					player!.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_ban_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_ban_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
				else
				{
					player!.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_ban_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_ban_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
			}
		});
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

		BanManager _banManager = new(dbConnectionString, Config);

		int.TryParse(command.GetArg(2), out time);

		if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
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
			if (player != null && player.IsValid)
			{
				if (!caller!.CanTarget(player))
				{
					command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
					return;
				}

				player!.Pawn.Value!.Freeze();
				AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!));

				if (time == 0)
				{
					player!.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_ban_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_ban_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
				else
				{
					player!.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_ban_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_ban_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
			}
		}

		Task.Run(async () =>
		{
			BanManager _banManager = new(dbConnectionString, Config);
			await _banManager.AddBanBySteamid(steamid, adminInfo, reason, time);
		});

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

		if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
			reason = command.GetArg(3);

		List<CCSPlayerController> matches = Helper.GetPlayerFromIp(ipAddress);
		if (matches.Count == 1)
		{
			CCSPlayerController? player = matches.FirstOrDefault();
			if (player != null && player.IsValid)
			{
				if (!caller!.CanTarget(player))
				{
					command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
					return;
				}

				player!.Pawn.Value!.Freeze();

				if (time == 0)
				{
					player!.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_ban_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_ban_message_perm", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
				else
				{
					player!.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_ban_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_ban_message_time", caller == null ? "Console" : caller.PlayerName, player.PlayerName, reason, time];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}

				AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!, "Banned"));
			}
		}

		Task.Run(async () =>
		{
			BanManager _banManager = new(dbConnectionString, Config);
			await _banManager.AddBanByIp(ipAddress, adminInfo, reason, time);
		});

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
		BanManager _banManager = new(dbConnectionString, Config);

		_ = _banManager.UnbanPlayer(pattern);

		command.ReplyToCommand($"Unbanned player with pattern {pattern}.");
		if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			_ = SendWebhookMessage($"Unbanned player with pattern {pattern}.");
	}

	[ConsoleCommand("css_slay")]
	[RequiresPermissions("@css/slay")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnSlayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		playersToTarget.ForEach(player =>
		{
			player.CommitSuicide(false, true);

			if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
			{
				StringBuilder sb = new(_localizer!["sa_prefix"]);
				sb.Append(_localizer["sa_admin_slay_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
				Server.PrintToChatAll(sb.ToString());
				if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				{
					LocalizedString localizedMessage = _localizer["sa_admin_slay_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
					_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
				}
			}
		});
	}

	[ConsoleCommand("css_give")]
	[RequiresPermissions("@css/cheats")]
	[CommandHelper(minArgs: 2, usage: "<#userid or name> <weapon>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnGiveCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		string weaponName = command.GetArg(2);

		// check if item is typed
		if (weaponName == null || weaponName.Length < 5)
		{
			command.ReplyToCommand($"No weapon typed.");
			return;
		}

		// check if item is valid
		if (!weaponName.Contains("weapon_") && !weaponName.Contains("item_"))
		{
			command.ReplyToCommand($"{weaponName} is not a valid item.");
			return;
		}

		// check if weapon is knife
		if (weaponName.Contains("_knife") || weaponName.Contains("bayonet"))
		{
			if (CoreConfig.FollowCS2ServerGuidelines)
			{
				command.ReplyToCommand($"Cannot Give {weaponName} because it's illegal to be given.");
				return;
			}
		}

		playersToTarget.ForEach(player =>
		{
			player.GiveNamedItem(weaponName);

			if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
			{
				StringBuilder sb = new(_localizer!["sa_prefix"]);
				sb.Append(_localizer["sa_admin_give_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName, weaponName]);
				Server.PrintToChatAll(sb.ToString());
				if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				{
					LocalizedString localizedMessage = _localizer["sa_admin_give_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName, weaponName];
					_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
				}
			}
		});
	}

	[ConsoleCommand("css_strip")]
	[RequiresPermissions("@css/slay")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnStripCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				player.RemoveWeapons();

				StringBuilder sb = new(_localizer!["sa_prefix"]);

				if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
				{
					sb.Append(_localizer["sa_admin_strip_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
					Server.PrintToChatAll(sb.ToString());
					if (Config.DiscordWebhook.Length > 0 && _localizer != null)
					{
						LocalizedString localizedMessage = _localizer["sa_admin_strip_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
						_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
					}
				}
			}
		});
	}

	[ConsoleCommand("css_hp")]
	[RequiresPermissions("@css/slay")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> <health>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnHpCommand(CCSPlayerController? caller, CommandInfo command)
	{
		int health = 100;
		int.TryParse(command.GetArg(2), out health);

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				player.SetHp(health);

				StringBuilder sb = new(_localizer!["sa_prefix"]);

				if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
				{
					sb.Append(_localizer["sa_admin_hp_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
					Server.PrintToChatAll(sb.ToString());
					if (Config.DiscordWebhook.Length > 0 && _localizer != null)
					{
						LocalizedString localizedMessage = _localizer["sa_admin_hp_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
						_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
					}
				}
			}
		});
	}

	[ConsoleCommand("css_speed")]
	[RequiresPermissions("@css/slay")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> <speed>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnSpeedCommand(CCSPlayerController? caller, CommandInfo command)
	{
		double speed = 1.0;
		double.TryParse(command.GetArg(2), out speed);

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				/*
				player.Speed = (float)speed;
				player.PlayerPawn.Value!.Speed = (float)speed;
				*/
				player.SetSpeed((float)speed);

				StringBuilder sb = new(_localizer!["sa_prefix"]);

				if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
				{
					sb.Append(_localizer["sa_admin_speed_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
					Server.PrintToChatAll(sb.ToString());
					if (Config.DiscordWebhook.Length > 0 && _localizer != null)
					{
						LocalizedString localizedMessage = _localizer["sa_admin_speed_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
						_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
					}
				}
			}
		});
	}

	[ConsoleCommand("css_god")]
	[RequiresPermissions("@css/cheats")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnGodCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				if (player != null && player.UserId != null)
				{
					if (!godPlayers.Contains((ushort)player.UserId))
						godPlayers.Add((ushort)player.UserId);
					else
						godPlayers.Remove((ushort)player.UserId);

					StringBuilder sb = new(_localizer!["sa_prefix"]);

					if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller!.UserId))
					{
						sb.Append(_localizer["sa_admin_god_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
						Server.PrintToChatAll(sb.ToString());
						if (Config.DiscordWebhook.Length > 0 && _localizer != null)
						{
							LocalizedString localizedMessage = _localizer["sa_admin_god_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
							_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
						}
					}
				}
			}
		});
	}

	[ConsoleCommand("css_slap")]
	[RequiresPermissions("@css/slay")]
	[CommandHelper(minArgs: 1, usage: "<#userid or name> [damage]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnSlapCommand(CCSPlayerController? caller, CommandInfo command)
	{
		int damage = 0;

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		if (command.ArgCount >= 2)
		{
			int.TryParse(command.GetArg(2), out damage);
		}

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				player!.Pawn.Value!.Slap(damage);
				StringBuilder sb = new(_localizer!["sa_prefix"]);

				if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
				{
					sb.Append(_localizer["sa_admin_slap_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
					Server.PrintToChatAll(sb.ToString());
					if (Config.DiscordWebhook.Length > 0 && _localizer != null)
					{
						LocalizedString localizedMessage = _localizer["sa_admin_slap_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
						_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
					}
				}
			}
		});
	}

	[ConsoleCommand("css_team")]
	[RequiresPermissions("@css/kick")]
	[CommandHelper(minArgs: 2, usage: "<#userid or name> [<ct/tt/spec>] [-k]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnTeamCommand(CCSPlayerController? caller, CommandInfo command)
	{
		string teamName = command.GetArg(2).ToLower();
		string _teamName;
		CsTeam teamNum = CsTeam.Spectator;

		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid).ToList();

		switch (teamName)
		{
			case "ct":
			case "counterterrorist":
				teamNum = CsTeam.CounterTerrorist;
				_teamName = "CT";
				break;

			case "t":
			case "tt":
			case "terrorist":
				teamNum = CsTeam.Terrorist;
				_teamName = "TT";
				break;

			case "swap":
				_teamName = "SWAP";
				break;

			default:
				teamNum = CsTeam.Spectator;
				_teamName = "SPEC";
				break;
		}

		playersToTarget.ForEach(player =>
		{
			if (!teamName.Equals("swap"))
			{
				if (player.PawnIsAlive && teamNum != CsTeam.Spectator && !command.GetArg(3).ToLower().Equals("-k"))
					player.SwitchTeam(teamNum);
				else
					player.ChangeTeam(teamNum);
			}
			else
			{
				if (player.TeamNum != (byte)CsTeam.Spectator)
				{
					CsTeam teamNum = (CsTeam)player.TeamNum == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
					_teamName = teamNum == CsTeam.Terrorist ? "TT" : "CT";
					if (player.PawnIsAlive && !command.GetArg(3).ToLower().Equals("-k"))
					{
						player.SwitchTeam(teamNum);
					}
					else
					{
						player.ChangeTeam(teamNum);
					}
				}
			}

			if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
			{
				StringBuilder sb = new(_localizer!["sa_prefix"]);
				sb.Append(_localizer["sa_admin_team_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName, _teamName]);
				Server.PrintToChatAll(sb.ToString());
				if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				{
					LocalizedString localizedMessage = _localizer["sa_admin_team_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName, _teamName];
					_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
				}
			}
		});
	}

	[ConsoleCommand("css_vote")]
	[RequiresPermissions("@css/generic")]
	[CommandHelper(minArgs: 2, usage: "<question> [... options ...]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnVoteCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.GetArg(1) == null || command.GetArg(1).Length < 0 || command.ArgCount < 2)
			return;

		voteAnswers.Clear();

		string question = command.GetArg(1);
		int answersCount = command.ArgCount;

		ChatMenu voteMenu = new(_localizer!["sa_admin_vote_menu_title", question]);

		for (int i = 2; i <= answersCount - 1; i++)
		{
			voteAnswers.Add(command.GetArg(i), 0);
			voteMenu.AddMenuOption(command.GetArg(i), Helper.handleVotes);
		}

		if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
		{
			Helper.PrintToCenterAll(_localizer!["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question]);
			StringBuilder sb = new(_localizer!["sa_prefix"]);
			sb.Append(_localizer["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question]);
			Server.PrintToChatAll(sb.ToString());
			if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			{
				LocalizedString localizedMessage = _localizer["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question];
				_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
			}
		}

		voteInProgress = true;

		foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
		{
			if (p == null) continue;
			MenuManager.OpenChatMenu(p, voteMenu);
		}

		AddTimer(40, () =>
		{
			StringBuilder sb = new(_localizer!["sa_prefix"]);
			sb.Append(_localizer["sa_admin_vote_message_results", question]);
			Server.PrintToChatAll(sb.ToString());
			if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			{
				LocalizedString localizedMessage = _localizer["sa_admin_vote_message_results", question];
				_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
			}

			foreach (KeyValuePair<string, int> kvp in voteAnswers)
			{
				sb = new(_localizer!["sa_prefix"]);
				sb.Append(_localizer["sa_admin_vote_message_results_answer", kvp.Key, kvp.Value]);
				Server.PrintToChatAll(sb.ToString());
				if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				{
					LocalizedString localizedMessage = _localizer["sa_admin_vote_message_results_answer", kvp.Key, kvp.Value];
					_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
				}
			}
			voteAnswers.Clear();
			voteInProgress = false;
		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
	}

	[ConsoleCommand("css_changemap")]
	[ConsoleCommand("css_map")]
	[RequiresPermissions("@css/changemap")]
	[CommandHelper(minArgs: 1, usage: "<mapname>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
	public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
	{
		string _command = string.Empty;
		string? map = command.GetCommandString.Split(" ")[1];

		if (map.StartsWith("ws:"))
		{
			if (long.TryParse(map.Replace("ws:", ""), out long mapId))
			{
				_command = $"host_workshop_map {mapId}";
			}
			else
			{
				_command = $"ds_workshop_changelevel {map.Replace("ws:", "")}";
			}

			AddTimer(2.0f, () =>
			{
				Server.ExecuteCommand(_command);
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
		}
		else
		{
			if (!Server.IsMapValid(map))
			{
				command.ReplyToCommand($"Map {map} not found.");
				return;
			}
		}

		if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
		{
			StringBuilder sb = new(_localizer!["sa_prefix"]);
			sb.Append(_localizer["sa_admin_changemap_message", caller == null ? "Console" : caller.PlayerName, map]);
			Server.PrintToChatAll(sb.ToString());
			if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			{
				LocalizedString localizedMessage = _localizer["sa_admin_changemap_message", caller == null ? "Console" : caller.PlayerName, map];
				_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
			}
		}

		if (!map.StartsWith("ws:"))
		{
			AddTimer(2.0f, () =>
			{
				Server.ExecuteCommand($"changelevel {map}");
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
		}
	}

	[ConsoleCommand("css_changewsmap", "Change workshop map.")]
	[ConsoleCommand("css_wsmap", "Change workshop map.")]
	[ConsoleCommand("css_workshop", "Change workshop map.")]
	[CommandHelper(1, "<name or id>")]
	[RequiresPermissions("@css/changemap")]
	public void OnWorkshopMapCommand(CCSPlayerController? caller, CommandInfo command)
	{
		string _command = string.Empty;
		string? map = command.GetArg(1);

		if (long.TryParse(map, out long mapId))
		{
			_command = $"host_workshop_map {mapId}";
		}
		else
		{
			_command = $"ds_workshop_changelevel {map}";
		}

		if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
		{
			StringBuilder sb = new(_localizer!["sa_prefix"]);
			sb.Append(_localizer["sa_admin_changemap_message", caller == null ? "Console" : caller.PlayerName, map]);
			Server.PrintToChatAll(sb.ToString());
			if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			{
				LocalizedString localizedMessage = _localizer["sa_admin_changemap_message", caller == null ? "Console" : caller.PlayerName, map];
				_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
			}
		}

		AddTimer(2.0f, () =>
		{
			Server.ExecuteCommand(_command);
		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
	}

	[ConsoleCommand("css_asay", "Say to all admins.")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminToAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (caller == null || !caller.IsValid || command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;

		byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
		string utf8String = Encoding.UTF8.GetString(utf8BytesString);

		StringBuilder sb = new();
		sb.Append(_localizer!["sa_adminchat_template_admin", caller == null ? "Console" : caller.PlayerName, utf8String]);
		if (Config.DiscordWebhook.Length > 0 && _localizer != null)
		{
			LocalizedString localizedMessage = _localizer["sa_adminchat_template_admin", caller == null ? "Console" : caller.PlayerName, utf8String];
			_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
		}

		foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/chat")))
		{
			p.PrintToChat(sb.ToString());
		}
	}

	[ConsoleCommand("css_say", "Say to all players.")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;

		byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
		string utf8String = Encoding.UTF8.GetString(utf8BytesString);

		StringBuilder sb = new();
		sb.Append(_localizer!["sa_adminsay_prefix", utf8String]);
		Server.PrintToChatAll(sb.ToString());
		if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			_ = SendWebhookMessage($"ASAY: {caller!.PlayerName}: {utf8String}");
	}

	[ConsoleCommand("css_psay", "Private message a player.")]
	[CommandHelper(2, "<#userid or name> <message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminPrivateSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		if (targets == null) return;
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid).ToList();

		int range = command.GetArg(0).Length + command.GetArg(1).Length + 2;
		string message = command.GetCommandString[range..];

		byte[] utf8BytesString = Encoding.UTF8.GetBytes(message);
		string utf8String = Encoding.UTF8.GetString(utf8BytesString);

		playersToTarget.ForEach(player =>
		{
			player.PrintToChat(Helper.ReplaceTags($"({caller!.PlayerName}) {utf8String}"));
			if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				_ = SendWebhookMessage($"PSAY: {caller!.PlayerName} --> {player!.PlayerName}: {utf8String}");
		});

		command.ReplyToCommand(Helper.ReplaceTags($" Private message sent!"));
	}

	[ConsoleCommand("css_csay", "Say to all players (in center).")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminCenterSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
		string utf8String = Encoding.UTF8.GetString(utf8BytesString);

		Helper.PrintToCenterAll(Helper.ReplaceTags(utf8String));
		if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			_ = SendWebhookMessage($"CSAY: {caller!.PlayerName}: {utf8String}");
	}

	[ConsoleCommand("css_hsay", "Say to all players (in hud).")]
	[CommandHelper(1, "<message>")]
	[RequiresPermissions("@css/chat")]
	public void OnAdminHudSayCommand(CCSPlayerController? caller, CommandInfo command)
	{
		byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
		string utf8String = Encoding.UTF8.GetString(utf8BytesString);

		VirtualFunctions.ClientPrintAll(
			HudDestination.Alert,
			Helper.ReplaceTags(utf8String),
			0, 0, 0, 0);

		if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			_ = SendWebhookMessage($"HSAY: {caller!.PlayerName}: {utf8String}");
	}

	[ConsoleCommand("css_noclip", "Noclip a player.")]
	[CommandHelper(1, "<#userid or name>")]
	[RequiresPermissions("@css/cheats")]
	public void OnNoclipCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				player!.Pawn.Value!.ToggleNoclip();

				if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_noclip_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
					Server.PrintToChatAll(sb.ToString());
					if (Config.DiscordWebhook.Length > 0 && _localizer != null)
					{
						LocalizedString localizedMessage = _localizer["sa_admin_noclip_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
						_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
					}
				}
			}
		});
	}

	[ConsoleCommand("css_freeze", "Freeze a player.")]
	[CommandHelper(1, "<#userid or name> [duration]")]
	[RequiresPermissions("@css/slay")]
	public void OnFreezeCommand(CCSPlayerController? caller, CommandInfo command)
	{
		int time = 0;
		int.TryParse(command.GetArg(2), out time);

		TargetResult? targets = GetTarget(command);
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				player!.Pawn.Value!.Freeze();

				if (time > 0)
					AddTimer(time, () => player.Pawn.Value!.Unfreeze());

				if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_freeze_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
					Server.PrintToChatAll(sb.ToString());
					if (Config.DiscordWebhook.Length > 0 && _localizer != null)
					{
						LocalizedString localizedMessage = _localizer["sa_admin_freeze_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
						_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
					}
				}
			}
		});
	}

	[ConsoleCommand("css_unfreeze", "Unfreeze a player.")]
	[CommandHelper(1, "<#userid or name>")]
	[RequiresPermissions("@css/slay")]
	public void OnUnfreezeCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid && player.PawnIsAlive).ToList();

		playersToTarget.ForEach(player =>
		{
			player!.Pawn.Value!.Unfreeze();

			if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
			{
				StringBuilder sb = new(_localizer!["sa_prefix"]);
				sb.Append(_localizer["sa_admin_unfreeze_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
				Server.PrintToChatAll(sb.ToString());
				if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				{
					LocalizedString localizedMessage = _localizer["sa_admin_unfreeze_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
					_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
				}
			}
		});
	}

	[ConsoleCommand("css_respawn", "Respawn a dead player.")]
	[CommandHelper(1, "<#userid or name>")]
	[RequiresPermissions("@css/cheats")]
	public void OnRespawnCommand(CCSPlayerController? caller, CommandInfo command)
	{
		TargetResult? targets = GetTarget(command);
		List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => caller!.CanTarget(player) && player != null && player.IsValid).ToList();

		playersToTarget.ForEach(player =>
		{
			if (caller!.CanTarget(player))
			{
				if (CBasePlayerController_SetPawnFunc == null || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

				var playerPawn = player.PlayerPawn.Value;
				CBasePlayerController_SetPawnFunc.Invoke(player, playerPawn, true, false);
				VirtualFunction.CreateVoid<CCSPlayerController>(player.Handle,
					GameData.GetOffset("CCSPlayerController_Respawn"))(player);

				if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains((ushort)caller.UserId))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_respawn_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName]);
					Server.PrintToChatAll(sb.ToString());
				}

				if (Config.DiscordWebhook.Length > 0 && _localizer != null)
				{
					LocalizedString localizedMessage = _localizer["sa_admin_respawn_message", caller == null ? "Console" : caller.PlayerName, player.PlayerName];
					_ = SendWebhookMessage(localizedMessage.ToString().Replace("", "").Replace("", ""));
				}
			}
		});
	}

	[ConsoleCommand("css_cvar", "Change a cvar.")]
	[CommandHelper(2, "<cvar> <value>")]
	[RequiresPermissions("@css/cvar")]
	public void OnCvarCommand(CCSPlayerController? caller, CommandInfo command)
	{
		var cvar = ConVar.Find(command.GetArg(1));
		string playerName = caller == null ? "Console" : caller.PlayerName;

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

		command.ReplyToCommand($"{playerName} changed cvar {cvar.Name} to {value}.");
		Logger.LogInformation($"{playerName} changed cvar {cvar.Name} to {value}.");
		if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			_ = SendWebhookMessage($"{playerName} changed cvar {cvar.Name} to {value}.");
	}

	[ConsoleCommand("css_rcon", "Run a server console command.")]
	[CommandHelper(1, "<command>")]
	[RequiresPermissions("@css/rcon")]
	public void OnRconCommand(CCSPlayerController? caller, CommandInfo command)
	{
		string playerName = caller == null ? "Console" : caller.PlayerName;
		Server.ExecuteCommand(command.ArgString);
		command.ReplyToCommand($"{playerName} executed command {command.ArgString}.");
		Logger.LogInformation($"{playerName} executed command ({command.ArgString}).");
		if (Config.DiscordWebhook.Length > 0 && _localizer != null)
			_ = SendWebhookMessage($"{playerName} executed command ({command.ArgString}).");
	}

	private static TargetResult? GetTarget(CommandInfo command)
	{
		var matches = command.GetArgTargetResult(1);

		if (!matches.Any())
		{
			command.ReplyToCommand($"Target {command.GetArg(1)} not found.");
			return null;
		}

		if (matches.Count() > 1 || command.GetArg(1).StartsWith('@'))
			return matches;

		if (matches.Count() == 1 || !command.GetArg(1).StartsWith('@'))
			return matches;

		command.ReplyToCommand($"Multiple targets found for \"{command.GetArg(1)}\".");
		return null;
	}

	public async Task SendWebhookMessage(string message)
	{
		using (var httpClient = new HttpClient())
		{
			var payload = new
			{
				content = message
			};

			var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
			var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

			var response = await httpClient.PostAsync(Config.DiscordWebhook, content);
		}
	}
}
