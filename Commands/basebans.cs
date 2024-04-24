using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using System.Text;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		[ConsoleCommand("css_ban")]
		[RequiresPermissions("@css/ban")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			if (command.ArgCount < 2)
				return;

			string reason = _localizer?["sa_unknown"] ?? "Unknown";

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			Database database = new(dbConnectionString);
			BanManager _banManager = new(database, Config);

			int.TryParse(command.GetArg(2), out int time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Ban(caller, player, time, reason, callerName, _banManager, command);
				}
			});
		}

		internal void Ban(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, BanManager? banManager = null, CommandInfo? command = null)
		{
			if (_database == null || player is null || !player.IsValid) return;
			
			if (CheckValidBan(caller, time) == false) 
				return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (player.PawnIsAlive)
			{
				player.Pawn.Value!.Freeze();
			}

			PlayerInfo playerInfo = new()
			{
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			PlayerInfo adminInfo = new()
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			Task.Run(async () =>
			{
				banManager ??= new BanManager(_database, Config);
				await banManager.BanPlayer(playerInfo, adminInfo, reason, time);
			});

			if (player.UserId.HasValue)
				AddTimer(Config.KickTime, () => Helper.KickPlayer(player.UserId.Value),
					CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			if (playerInfo.IpAddress != null && !bannedPlayers.Contains(playerInfo.IpAddress))
				bannedPlayers.Add(playerInfo.IpAddress);
			if (!bannedPlayers.Contains(player!.SteamID.ToString()))
				bannedPlayers.Add(player.SteamID.ToString());

			if (time == 0)
			{
				if (!player.IsBot && !player.IsHLTV)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player!.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
					}

				if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_ban_message_perm", callerName, player.PlayerName, reason]);
							_player.PrintToChat(sb.ToString());
						}
					}
				}
			}
			else
			{
				if (!player.IsBot && !player.IsHLTV)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player!.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
					}
				if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_ban_message_time", callerName, player.PlayerName, reason, time]);
							_player.PrintToChat(sb.ToString());
						}
					}
				}
			}

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}
			Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Ban, _discordWebhookClientPenalty, _localizer);
		}

		[ConsoleCommand("css_addban")]
		[RequiresPermissions("@css/ban")]
		[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnAddBanCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			
			string callerName = caller == null ? "Console" : caller.PlayerName;
			if (command.ArgCount < 2)
				return;
			if (string.IsNullOrEmpty(command.GetArg(1))) return;

			string steamid = command.GetArg(1);

			if (!Helper.IsValidSteamID64(steamid))
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			string reason = _localizer?["sa_unknown"] ?? "Unknown";

			Database database = new(dbConnectionString);
			BanManager _banManager = new(database, Config);

			int.TryParse(command.GetArg(2), out int time);
			
			if (CheckValidBan(caller, time) == false) 
				return;

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			PlayerInfo adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
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
					if (player.UserId.HasValue)
						AddTimer(Config.KickTime, () => Helper.KickPlayer(player.UserId.Value), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

					if (time == 0)
					{
						if (!player.IsBot && !player.IsHLTV)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player!.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_ban_message_perm", callerName, player.PlayerName, reason]);
									_player.PrintToChat(sb.ToString());
								}
							}
						}
					}
					else
					{
						if (!player.IsBot && !player.IsHLTV)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player!.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}

						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_ban_message_time", callerName, player.PlayerName, reason, time]);
									_player.PrintToChat(sb.ToString());
								}
							}
						}
					}
				}

				Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Ban, _discordWebhookClientPenalty, _localizer);
			}

			Task.Run(async () =>
			{
				BanManager _banManager = new(_database, Config);
				await _banManager.AddBanBySteamid(steamid, adminInfo, reason, time);
			});

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}
			//Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Ban, _discordWebhookClientPenalty, _localizer);

			command?.ReplyToCommand($"Banned player with steamid {steamid}.");
		}

		[ConsoleCommand("css_banip")]
		[RequiresPermissions("@css/ban")]
		[CommandHelper(minArgs: 1, usage: "<ip> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnBanIp(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			string callerName = caller == null ? "Console" : caller.PlayerName;

			if (command.ArgCount < 2)
				return;
			if (string.IsNullOrEmpty(command.GetArg(1))) return;

			string ipAddress = command.GetArg(1);

			if (!Helper.IsValidIP(ipAddress))
			{
				command.ReplyToCommand($"Invalid IP address.");
				return;
			}

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			string reason = _localizer?["sa_unknown"] ?? "Unknown";

			PlayerInfo adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			int.TryParse(command.GetArg(2), out int time);
			if (CheckValidBan(caller, time) == false) 
				return;

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
						if (!player.IsBot && !player.IsHLTV)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player!.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}

						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_ban_message_perm", callerName, player.PlayerName, reason]);
									_player.PrintToChat(sb.ToString());
								}
							}
						}
					}
					else
					{
						if (!player.IsBot && !player.IsHLTV)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player!.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_ban_message_time", callerName, player.PlayerName, reason, time]);
									_player.PrintToChat(sb.ToString());
								}
							}
						}
					}

					if (player.UserId.HasValue)
					{
						AddTimer(Config.KickTime, () =>
						{
							Helper.KickPlayer(player.UserId.Value, "Banned");
						}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
					}
				}
				Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Ban, _discordWebhookClientPenalty, _localizer);
			}

			Task.Run(async () =>
			{
				BanManager _banManager = new(_database, Config);
				await _banManager.AddBanByIp(ipAddress, adminInfo, reason, time);
			});

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			command?.ReplyToCommand($"Banned player with IP address {ipAddress}.");
		}
		
		private bool CheckValidBan(CCSPlayerController? caller, int duration)
		{
			bool validCaller = caller != null && caller.IsValid;
			
			bool canPermBan = validCaller && AdminManager.PlayerHasPermissions(caller, "@css/permban");
			
			if (duration > Config.MaxBanDuration && canPermBan == false)
			{
				if (validCaller)
					caller.PrintToChat($"{_localizer["sa_prefix"]} {_localizer["sa_ban_max_duration_exceeded", Config.MaxBanDuration]}");
				return false;
			}

			if (duration == 0 && canPermBan == false)
			{
				if (validCaller)
					caller.PrintToChat($"{_localizer!["sa_prefix"]} {_localizer["sa_ban_perm_restricted"]}");
				return false;
			}

			return true;
		}

		[ConsoleCommand("css_unban")]
		[RequiresPermissions("@css/unban")]
		[CommandHelper(minArgs: 1, usage: "<steamid or name or ip> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnUnbanCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			string callerName = caller?.PlayerName ?? "Console";
			string callerSteamId = caller?.SteamID.ToString() ?? "Console";

			if (command.GetArg(1).Length <= 1)
			{
				command.ReplyToCommand($"Too short pattern to search.");
				return;
			}

			string pattern = command.GetArg(1);
			string reason = command.GetArg(2);

			BanManager _banManager = new(_database, Config);
			Task.Run(async () => await _banManager.UnbanPlayer(pattern, callerSteamId, reason));

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			command.ReplyToCommand($"Unbanned player with pattern {pattern}.");
		}
	}
}