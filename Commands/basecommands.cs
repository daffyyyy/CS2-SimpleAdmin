using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Text;
using Discord.Rest;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		[ConsoleCommand("css_sa_upgrade")]
		[CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
		public void OnSaUpgradeCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller != null || _database == null) return;

			Task.Run(async () =>
			{
				try
				{
					using (var connection = await _database.GetConnectionAsync())
					{
						var commandText = "ALTER TABLE `sa_mutes` CHANGE `type` `type` ENUM('GAG','MUTE', 'SILENCE', '') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'GAG';";

						using (var command = connection.CreateCommand())
						{
							command.CommandText = commandText;
							await command.ExecuteNonQueryAsync();
						}
					}
				}
				catch (Exception ex)
				{
					Logger.LogError($"{ex.Message}");
				}
			});
		}

		[ConsoleCommand("css_admin")]
		[RequiresPermissions("@css/generic")]
		public void OnAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller == null || !caller.IsValid) return;

			using (new WithTemporaryCulture(caller.GetLanguage()))
			{
				var splitMessage = _localizer!["sa_adminhelp"].ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

				foreach (var line in splitMessage)
				{
					caller.PrintToChat(Helper.ReplaceTags($" {line}"));
				}
			}
		}

		[ConsoleCommand("css_addadmin")]
		[CommandHelper(minArgs: 4, usage: "<steamid> <name> <flags/groups> <immunity> <duration>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnAddAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			string callerName = caller == null ? "Console" : caller.PlayerName;

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			string steamid = command.GetArg(1);
			string name = command.GetArg(2);
			string flags = command.GetArg(3);
			bool globalAdmin = command.GetArg(4).ToLower().Equals("-g") || command.GetArg(5).ToLower().Equals("-g") || command.GetArg(6).ToLower().Equals("-g");
			int immunity = 0;
			int.TryParse(command.GetArg(4), out immunity);
			int time = 0;
			int.TryParse(command.GetArg(5), out time);

			AdminSQLManager _adminManager = new(_database);
			_ = _adminManager.AddAdminBySteamId(steamid, name, flags, immunity, time, globalAdmin);

			command.ReplyToCommand($"Added '{flags}' flags to '{name}' ({steamid})");
		}

		[ConsoleCommand("css_deladmin")]
		[CommandHelper(minArgs: 1, usage: "<steamid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnDelAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			string callerName = caller == null ? "Console" : caller.PlayerName;

			if (!Helper.IsValidSteamID64(command.GetArg(1)))
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			string steamid = command.GetArg(1);
			bool globalDelete = command.GetArg(2).ToLower().Equals("-g");

			AdminSQLManager _adminManager = new(_database);
			_ = _adminManager.DeleteAdminBySteamId(steamid, globalDelete);

			AddTimer(2, () =>
			{
				if (!string.IsNullOrEmpty(steamid) && SteamID.TryParse(steamid, out var steamId) && steamId != null)
				{
					if (AdminSQLManager._adminCache.ContainsKey(steamId))
					{
						AdminSQLManager._adminCache.TryRemove(steamId, out _);
					}

					AdminManager.ClearPlayerPermissions(steamId);
					AdminManager.RemovePlayerAdminData(steamId);
				}
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			command.ReplyToCommand($"Removed flags from '{steamid}'");
		}

		[ConsoleCommand("css_reladmin")]
		[CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnRelAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			foreach (SteamID steamId in AdminSQLManager._adminCache.Keys.ToList())
			{
				if (AdminSQLManager._adminCache.TryRemove(steamId, out _))
				{
					AdminManager.ClearPlayerPermissions(steamId);
					AdminManager.RemovePlayerAdminData(steamId);
				}
			}

			AdminSQLManager _adminManager = new(_database);
			_ = _adminManager.GiveAllFlags();

			command.ReplyToCommand("Reloaded sql admins");
		}

		[ConsoleCommand("css_stealth")]
		[ConsoleCommand("css_hide")]
		[CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
		[RequiresPermissions("@css/kick")]
		public void OnHideCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller == null) return;

			if (silentPlayers.Contains(caller.Slot))
			{
				RemoveFromConcurrentBag(silentPlayers, caller.Slot);
				caller.PrintToChat($"You aren't hidden now!");
				caller.ChangeTeam(CsTeam.Spectator);
			}
			else
			{
				silentPlayers.Add(caller.Slot);
				Server.ExecuteCommand("sv_disable_teamselect_menu 1");
				Server.NextFrame(() =>
				{
					if (caller.PlayerPawn.Value != null && caller.PawnIsAlive)
						caller.PlayerPawn.Value.CommitSuicide(true, false);

					AddTimer(1.0f, () => { caller.ChangeTeam(CsTeam.Spectator); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
					AddTimer(1.15f, () => { caller.ChangeTeam(CsTeam.None); }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
					caller.PrintToChat($"You are hidden now!");
					AddTimer(1.22f, () => { Server.ExecuteCommand("sv_disable_teamselect_menu 0"); });
				});
			}
		}

		[ConsoleCommand("css_who")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/generic")]
		public void OnWhoCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			string callerName = caller == null ? "Console" : caller.PlayerName;

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

			Database database = new Database(dbConnectionString);
			BanManager _banManager = new(database, Config);
			MuteManager _muteManager = new(_database);

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					PlayerInfo playerInfo = new PlayerInfo
					{
						UserId = player.UserId,
						Index = (int)player.Index,
						SteamId = player?.SteamID.ToString(),
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
								if (player.SteamID.ToString().Length == 17)
								{
									caller!.PrintToConsole($"• SteamID2: \"{player.SteamID}\"");
									caller!.PrintToConsole($"• Community link: \"{new SteamID(player.SteamID).ToCommunityUrl()}\"");
								}
								if (playerInfo.IpAddress != null)
									caller!.PrintToConsole($"• IP Address: \"{playerInfo.IpAddress}\"");
								caller!.PrintToConsole($"• Ping: \"{player.Ping}\"");
								if (player.SteamID.ToString().Length == 17)
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
								if (player.SteamID.ToString().Length == 17)
								{
									Server.PrintToConsole($"• SteamID2: \"{player.SteamID}\"");
									Server.PrintToConsole($"• Community link: \"{new SteamID(player.SteamID).ToCommunityUrl()}\"");
								}
								if (playerInfo.IpAddress != null)
									Server.PrintToConsole($"• IP Address: \"{playerInfo.IpAddress}\"");
								Server.PrintToConsole($"• Ping: \"{player.Ping}\"");
								if (player.SteamID.ToString().Length == 17)
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
			List<CCSPlayerController> playersToTarget = Helper.GetValidPlayers();

			if (caller != null)
			{
				caller!.PrintToConsole($"--------- PLAYER LIST ---------");
				playersToTarget.ForEach(player =>
				{
					caller!.PrintToConsole($"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{player.IpAddress?.Split(":")[0]}\" SteamID64: \"{player.SteamID}\")");
				});
				caller!.PrintToConsole($"--------- END PLAYER LIST ---------");
			}
			else
			{
				Server.PrintToConsole($"--------- PLAYER LIST ---------");
				playersToTarget.ForEach(player =>
				{
					Server.PrintToConsole($"• [#{player.UserId}] \"{player.PlayerName}\" (IP Address: \"{player.IpAddress?.Split(":")[0]}\" SteamID64: \"{player.SteamID}\")");
				});
				Server.PrintToConsole($"--------- END PLAYER LIST ---------");
			}
		}

		[ConsoleCommand("css_kick")]
		[RequiresPermissions("@css/kick")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			string reason = "Unknown";

			TargetResult? targets = GetTarget(command);

			if (targets == null)
				return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			if (command.ArgCount >= 2 && command.GetArg(2).Length > 0)
				reason = command.GetArg(2);

			targets.Players.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					Kick(caller, player, reason, callerName);
				}
			});
		}

		public void Kick(CCSPlayerController? caller, CCSPlayerController player, string reason = "Unknown", string callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			if (player.PawnIsAlive)
			{
				player.Pawn.Value!.Freeze();
			}

			if (string.IsNullOrEmpty(reason) == false)
			{
				if (!player.IsBot && !player.IsHLTV)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_kick_message", reason, caller == null ? "Console" : caller.PlayerName]);
					}
				AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!, reason), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}
			else
			{
				AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}

			if (caller == null || caller != null && caller.UserId != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_kick_message", callerName, player.PlayerName, reason]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_changemap")]
		[ConsoleCommand("css_map")]
		[RequiresPermissions("@css/changemap")]
		[CommandHelper(minArgs: 1, usage: "<mapname>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string? map = command.GetCommandString.Split(" ")[1];
			ChangeMap(caller, map, command);
		}

		public void ChangeMap(CCSPlayerController caller, string map, CommandInfo? command = null)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			string _command = string.Empty;

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

				if (_discordWebhookClientLog != null && _localizer != null)
				{
					string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
					string commandName = command?.GetCommandString ?? "css_changemap";
					_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", commandName]));
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
					string msg = $"Map {map} not found.";
					if (command != null)
						command.ReplyToCommand(msg);
					else if (caller != null && caller.IsValid)
						caller.PrintToChat(msg);
					else
						Server.PrintToConsole(msg);
					return;
				}
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_changemap_message", caller == null ? "Console" : caller.PlayerName, map]);
						_player.PrintToChat(sb.ToString());
					}
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
			string? map = command.GetArg(1);
			ChangeWorkshopMap(caller, map, command);
		}

		public void ChangeWorkshopMap(CCSPlayerController caller, string map, CommandInfo? command = null)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			string _command = string.Empty;

			if (long.TryParse(map, out long mapId))
			{
				_command = $"host_workshop_map {mapId}";
			}
			else
			{
				_command = $"ds_workshop_changelevel {map}";
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_changemap_message", caller == null ? "Console" : caller.PlayerName, map]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				string commandName = command?.GetCommandString ?? "css_changewsmap";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", commandName]));
			}

			AddTimer(2.0f, () =>
			{
				Server.ExecuteCommand(_command);
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
		}

		[ConsoleCommand("css_cvar", "Change a cvar.")]
		[CommandHelper(2, "<cvar> <value>")]
		[RequiresPermissions("@css/cvar")]
		public void OnCvarCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var cvar = ConVar.Find(command.GetArg(1));
			string callerName = caller == null ? "Console" : caller.PlayerName;

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			var value = command.GetArg(2);

			Server.ExecuteCommand($"{cvar.Name} {value}");

			command.ReplyToCommand($"{callerName} changed cvar {cvar.Name} to {value}.");
			Logger.LogInformation($"{callerName} changed cvar {cvar.Name} to {value}.");
		}

		[ConsoleCommand("css_rcon", "Run a server console command.")]
		[CommandHelper(1, "<command>")]
		[RequiresPermissions("@css/rcon")]
		public void OnRconCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			Server.ExecuteCommand(command.ArgString);
			command.ReplyToCommand($"{callerName} executed command {command.ArgString}.");
			Logger.LogInformation($"{callerName} executed command ({command.ArgString}).");
		}
		
		[ConsoleCommand("css_rr")]
		[ConsoleCommand("css_rg")]
		[ConsoleCommand("css_restart")]
		[ConsoleCommand("css_restartgame")]
		[RequiresPermissions("@css/generic")]
		[CommandHelper(minArgs: 1, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnRestartCommand(CCSPlayerController? caller, CommandInfo command)
		{
			RestartGame(caller);
		}

		public void RestartGame(CCSPlayerController admin)
		{
			// TODO: Localize
			var name = admin == null ? "Console" : admin.PlayerName;
			Server.PrintToChatAll($"[SimpleAdmin] {name}: Restarting game...");
			Server.ExecuteCommand("mp_restartgame 2");
		}
	}
}
