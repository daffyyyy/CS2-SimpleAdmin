using CounterStrikeSharp.API;
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
		[ConsoleCommand("css_gag")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGagCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			string callerName = caller == null ? "Console" : caller.PlayerName;

			int time = 0;
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

			int.TryParse(command.GetArg(2), out time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Gag(caller, player, time, reason, callerName, _muteManager, playerPenaltyManager);
				}
			});
		}

		internal void Gag(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, MuteManager? muteManager = null, PlayerPenaltyManager? playerPenaltyManager = null)
		{
			if (_database == null) return;
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			muteManager ??= new MuteManager(_database);
			playerPenaltyManager ??= new PlayerPenaltyManager();

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

			Helper.LogCommand(caller, $"css_gag {player?.SteamID} {time} {reason}");

			Task.Run(async () =>
			{
				await muteManager.MutePlayer(playerInfo, adminInfo, reason, time);
			});

			if (TagsDetected)
				Server.ExecuteCommand($"css_tag_mute {player!.SteamID}");

			playerPenaltyManager.AddPenalty(player!.Slot, PenaltyType.Gag, DateTime.Now.AddMinutes(time), time);

			if (time == 0)
			{
				if (!player!.IsBot && !player.IsHLTV)
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_gag_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
					}
				}

				if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_gag_message_perm", callerName, player.PlayerName, reason]);
							_player.PrintToChat(sb.ToString());
						}
					}
				}
			}
			else
			{
				if (!player!.IsBot && !player.IsHLTV)
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player!.PrintToCenter(_localizer!["sa_player_gag_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
					}
				}

				if (caller == null || caller != null && caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_gag_message_time", callerName, player.PlayerName, reason, time]);
							_player.PrintToChat(sb.ToString());
						}
					}
				}
			}
		}

		[ConsoleCommand("css_addgag")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnAddGagCommand(CCSPlayerController? caller, CommandInfo command)
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

			int time = 0;
			string reason = "Unknown";

			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			int.TryParse(command.GetArg(2), out time);

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

					if (time == 0)
					{
						if (!player.IsBot && !player.IsHLTV)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player!.PrintToCenter(_localizer!["sa_player_gag_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}

						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_gag_message_perm", callerName, player.PlayerName, reason]);
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
								player!.PrintToCenter(_localizer!["sa_player_gag_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}

						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_gag_message_time", callerName, player.PlayerName, reason, time]);
									_player.PrintToChat(sb.ToString());
								}
							}
						}
					}

					if (TagsDetected)
						Server.ExecuteCommand($"css_tag_mute {player!.SteamID}");

					playerPenaltyManager.AddPenalty(player!.Slot, PenaltyType.Gag, DateTime.Now.AddMinutes(time), time);
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

			bool found = false;

			string pattern = command.GetArg(1);
			MuteManager _muteManager = new(_database);

			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			if (Helper.IsValidSteamID64(pattern))
			{
				List<CCSPlayerController> matches = Helper.GetPlayerFromSteamid64(pattern);
				if (matches.Count == 1)
				{
					CCSPlayerController? player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Gag);

						if (TagsDetected)
							Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");

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
						playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Gag);

						if (TagsDetected)
							Server.ExecuteCommand($"css_tag_unmute {player!.SteamID.ToString()}");

						pattern = player!.SteamID.ToString();

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
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (playersToTarget.Count > 1)
			{
				playersToTarget.ForEach(player =>
				{
					playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Gag);

					if (player!.SteamID.ToString().Length == 17)
						_ = _muteManager.UnmutePlayer(player.SteamID.ToString(), 0); // Unmute by type 0 (gag)

					if (TagsDetected)
						Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");
				});

				command.ReplyToCommand($"Ungaged player with pattern {pattern}.");
				return;
			}
		}

		[ConsoleCommand("css_mute")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnMuteCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			string callerName = caller == null ? "Console" : caller.PlayerName;

			int time = 0;
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

			int.TryParse(command.GetArg(2), out time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Mute(caller, player, time, reason, callerName, _muteManager, playerPenaltyManager);
				}
			});
		}

		internal void Mute(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, MuteManager? muteManager = null, PlayerPenaltyManager? playerPenaltyManager = null)
		{
			if (_database == null) return;
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			muteManager ??= new MuteManager(_database);
			playerPenaltyManager ??= new PlayerPenaltyManager();

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

			Helper.LogCommand(caller, $"css_mute {player?.SteamID} {time} {reason}");

			player!.VoiceFlags = VoiceFlags.Muted;

			Task.Run(async () =>
			{
				await muteManager.MutePlayer(playerInfo, adminInfo, reason, time, 1);
			});

			playerPenaltyManager.AddPenalty(player!.Slot, PenaltyType.Mute, DateTime.Now.AddMinutes(time), time);

			if (time == 0)
			{
				if (!player.IsBot && !player.IsHLTV)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player!.PrintToCenter(_localizer!["sa_player_mute_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
					}

				if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_mute_message_perm", callerName, player.PlayerName, reason]);
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
						player!.PrintToCenter(_localizer!["sa_player_mute_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
					}
				if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_mute_message_time", callerName, player.PlayerName, reason, time]);
							_player.PrintToChat(sb.ToString());
						}
					}
				}
			}
		}

		[ConsoleCommand("css_addmute")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnAddMuteCommand(CCSPlayerController? caller, CommandInfo command)
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

			Helper.LogCommand(caller, command);

			int time = 0;
			string reason = "Unknown";

			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			int.TryParse(command.GetArg(2), out time);

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

					playerPenaltyManager.AddPenalty(player!.Slot, PenaltyType.Mute, DateTime.Now.AddMinutes(time), time);

					if (time == 0)
					{
						if (!player.IsBot && !player.IsHLTV)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player!.PrintToCenter(_localizer!["sa_player_mute_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_mute_message_perm", callerName, player.PlayerName, reason]);
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
								player!.PrintToCenter(_localizer!["sa_player_mute_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_mute_message_time", callerName, player.PlayerName, reason, time]);
									_player.PrintToChat(sb.ToString());
								}
							}
						}
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
			bool found = false;
			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			if (Helper.IsValidSteamID64(pattern))
			{
				List<CCSPlayerController> matches = Helper.GetPlayerFromSteamid64(pattern);
				if (matches.Count == 1)
				{
					CCSPlayerController? player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Mute);
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
						playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Mute);
						player.VoiceFlags = VoiceFlags.Normal;
						pattern = player.SteamID.ToString();
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
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (playersToTarget.Count > 1)
			{
				playersToTarget.ForEach(player =>
				{
					if (player.SteamID.ToString().Length == 17)
						_ = _muteManager.UnmutePlayer(player.SteamID.ToString(), 1); // Unmute by type 1 (mute)

					playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Mute);
					player.VoiceFlags = VoiceFlags.Normal;
				});

				command.ReplyToCommand($"Unmuted player with pattern {pattern}.");
				return;
			}
		}

		[ConsoleCommand("css_silence")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnSilenceCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			string callerName = caller == null ? "Console" : caller.PlayerName;

			int time = 0;
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

			int.TryParse(command.GetArg(2), out time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Silence(caller, player, time, reason, callerName, _muteManager, playerPenaltyManager);
				}
			});
		}

		internal void Silence(CCSPlayerController? caller, CCSPlayerController player, int time, string reason, string? callerName = null, MuteManager? muteManager = null, PlayerPenaltyManager? playerPenaltyManager = null)
		{
			if (_database == null) return;
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			muteManager ??= new MuteManager(_database);
			playerPenaltyManager ??= new PlayerPenaltyManager();

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

			Helper.LogCommand(caller, $"css_silence {player?.SteamID} {time} {reason}");

			Task.Run(async () =>
			{
				await muteManager.MutePlayer(playerInfo, adminInfo, reason, time, 2);
			});

			if (TagsDetected)
				Server.ExecuteCommand($"css_tag_mute {player!.SteamID}");

			player!.VoiceFlags = VoiceFlags.Muted;

			playerPenaltyManager.AddPenalty(player!.Slot, PenaltyType.Silence, DateTime.Now.AddMinutes(time), time);

			if (time == 0)
			{
				if (!player!.IsBot && !player.IsHLTV)
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_silence_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
					}
				}

				if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_silence_message_perm", callerName, player.PlayerName, reason]);
							_player.PrintToChat(sb.ToString());
						}
					}
				}
			}
			else
			{
				if (!player!.IsBot && !player.IsHLTV)
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player!.PrintToCenter(_localizer!["sa_player_silence_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
					}
				}

				if (caller == null || caller != null && caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_silence_message_time", callerName, player.PlayerName, reason, time]);
							_player.PrintToChat(sb.ToString());
						}
					}
				}
			}
		}

		[ConsoleCommand("css_addsilence")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnAddSilenceCommand(CCSPlayerController? caller, CommandInfo command)
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

			Helper.LogCommand(caller, command);

			int time = 0;
			string reason = "Unknown";

			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			int.TryParse(command.GetArg(2), out time);

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

					if (TagsDetected)
						Server.ExecuteCommand($"css_tag_mute {player!.SteamID}");

					playerPenaltyManager.AddPenalty(player!.Slot, PenaltyType.Silence, DateTime.Now.AddMinutes(time), time);

					if (time == 0)
					{
						if (!player.IsBot && !player.IsHLTV)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player!.PrintToCenter(_localizer!["sa_player_silence_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_silence_message_perm", callerName, player.PlayerName, reason]);
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
								player!.PrintToCenter(_localizer!["sa_player_silence_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
						{
							foreach (CCSPlayerController _player in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(_player.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_silence_message_time", callerName, player.PlayerName, reason, time]);
									_player.PrintToChat(sb.ToString());
								}
							}
						}
					}
				}
			}
			_ = _muteManager.AddMuteBySteamid(steamid, adminInfo, reason, time, 2);
			command.ReplyToCommand($"Silenced player with steamid {steamid}.");
		}

		[ConsoleCommand("css_unsilence")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<steamid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnUnsilenceCommand(CCSPlayerController? caller, CommandInfo command)
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
			bool found = false;
			MuteManager _muteManager = new(_database);
			PlayerPenaltyManager playerPenaltyManager = new PlayerPenaltyManager();

			if (Helper.IsValidSteamID64(pattern))
			{
				List<CCSPlayerController> matches = Helper.GetPlayerFromSteamid64(pattern);
				if (matches.Count == 1)
				{
					CCSPlayerController? player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						if (TagsDetected)
							Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");

						playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Silence);
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
						if (TagsDetected)
							Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");

						playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Silence);
						player.VoiceFlags = VoiceFlags.Normal;
						pattern = player.SteamID.ToString();
						found = true;
					}
				}
			}

			if (found)
			{
				_ = _muteManager.UnmutePlayer(pattern, 2); // Unmute by type 2 (silence)
				command.ReplyToCommand($"Unsilenced player with pattern {pattern}.");
				return;
			}

			TargetResult? targets = GetTarget(command);
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (playersToTarget.Count > 1)
			{
				playersToTarget.ForEach(player =>
				{
					if (player.SteamID.ToString().Length == 17)
						_ = _muteManager.UnmutePlayer(player.SteamID.ToString(), 2); // Unmute by type 2 (silence)

					if (TagsDetected)
						Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");

					playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Silence);
					player.VoiceFlags = VoiceFlags.Normal;
				});

				command.ReplyToCommand($"Unsilenced player with pattern {pattern}.");
				return;
			}
		}
	}
}