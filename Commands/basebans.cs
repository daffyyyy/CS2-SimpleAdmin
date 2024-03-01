using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;
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

			string reason = "Unknown";

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			Database database = new Database(dbConnectionString);

			BanManager _banManager = new(database, Config);

			int.TryParse(command.GetArg(2), out int time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Ban(caller, player, time, reason, callerName, _banManager);
				}
			});
		}

		internal void Ban(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, BanManager? banManager = null)
		{
			if (_database == null) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (player.PawnIsAlive)
			{
				player.Pawn.Value!.Freeze();
			}

			PlayerInfo playerInfo = new PlayerInfo
			{
				SteamId = player?.SteamID.ToString(),
				Name = player?.PlayerName,
				IpAddress = player?.IpAddress?.Split(":")[0]
			};

			PlayerInfo adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			Helper.LogCommand(caller, $"css_ban {player?.SteamID} {time} {reason}");

			Task.Run(async () =>
			{
				banManager ??= new BanManager(_database, Config);
				await banManager.BanPlayer(playerInfo, adminInfo, reason, time);
			});

			AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player!.UserId!), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			string reason = "Unknown";

			Database database = new Database(dbConnectionString);

			BanManager _banManager = new(database, Config);

			int.TryParse(command.GetArg(2), out int time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			PlayerInfo adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			Helper.LogCommand(caller, command);

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
					AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

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
			}

			Task.Run(async () =>
			{
				BanManager _banManager = new(_database, Config);
				await _banManager.AddBanBySteamid(steamid, adminInfo, reason, time);
			});

			command.ReplyToCommand($"Banned player with steamid {steamid}.");
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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			string reason = "Unknown";

			PlayerInfo adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			Helper.LogCommand(caller, command);

			int.TryParse(command.GetArg(2), out int time);

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

					AddTimer(Config.KickTime, () => Helper.KickPlayer((ushort)player.UserId!, "Banned"), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
				}
			}

			Task.Run(async () =>
			{
				BanManager _banManager = new(_database, Config);
				await _banManager.AddBanByIp(ipAddress, adminInfo, reason, time);
			});

			command.ReplyToCommand($"Banned player with IP address {ipAddress}.");
		}

		[ConsoleCommand("css_unban")]
		[RequiresPermissions("@css/unban")]
		[CommandHelper(minArgs: 1, usage: "<steamid or name or ip>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnUnbanCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			string callerName = caller == null ? "Console" : caller.PlayerName;
			if (command.GetArg(1).Length <= 1)
			{
				command.ReplyToCommand($"Too short pattern to search.");
				return;
			}

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			Helper.LogCommand(caller, command);

			string pattern = command.GetArg(1);

			BanManager _banManager = new BanManager(_database, Config);
			Task.Run(async () => await _banManager.UnbanPlayer(pattern));

			command.ReplyToCommand($"Unbanned player with pattern {pattern}.");
		}
	}
}