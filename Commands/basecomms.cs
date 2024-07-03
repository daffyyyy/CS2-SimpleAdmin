using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
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
			var callerName = caller == null ? "Console" : caller.PlayerName;

			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			var targets = GetTarget(command);
			if (targets == null) return;
			var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			int.TryParse(command.GetArg(2), out var time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			MuteManager muteManager = new(_database);
			var playerPenaltyManager = new PlayerPenaltyManager();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Gag(caller, player, time, reason, callerName, muteManager, playerPenaltyManager, command);
				}
			});
		}

		internal static void Gag(CCSPlayerController? caller, CCSPlayerController? player, int time, string reason, string? callerName = null, MuteManager? muteManager = null, PlayerPenaltyManager? playerPenaltyManager = null, CommandInfo? command = null)
		{
			if (_database == null) return;
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			muteManager ??= new MuteManager(_database);

			var playerInfo = new PlayerInfo
			{
				SteamId = player?.SteamID.ToString(),
				Name = player?.PlayerName,
				IpAddress = player?.IpAddress?.Split(":")[0]
			};

			var adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			Task.Run(async () =>
			{
				await muteManager.MutePlayer(playerInfo, adminInfo, reason, time);
			});

			if (_tagsDetected)
				Server.ExecuteCommand($"css_tag_mute {player!.SteamID}");

			PlayerPenaltyManager.AddPenalty(player!.Slot, PenaltyType.Gag, DateTime.Now.AddMinutes(time), time);
			if (time == 0)
			{
				if (!player.IsBot)
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_gag_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
					}
				}

				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_gag_message_perm", callerName, player.PlayerName, reason]);
							controller.PrintToChat(sb.ToString());
						}
					}
				}
			}
			else
			{
				if (!player.IsBot)
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_gag_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
					}
				}

				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_gag_message_time", callerName, player.PlayerName, reason, time]);
							controller.PrintToChat(sb.ToString());
						}
					}
				}
			}

			if (command == null) return;
			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Gag, DiscordWebhookClientPenalty, _localizer);
			Helper.LogCommand(caller, command);
		}

		[ConsoleCommand("css_addgag")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnAddGagCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerName = caller == null ? "Console" : caller.PlayerName;

			if (command.ArgCount < 2)
				return;

			if (string.IsNullOrEmpty(command.GetArg(1))) return;

			if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			var steamid = steamId.SteamId64.ToString();

			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			MuteManager muteManager = new(_database);

			int.TryParse(command.GetArg(2), out var time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			var adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			var matches = Helper.GetPlayerFromSteamid64(steamid);
			if (matches.Count == 1)
			{
				var player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					if (!caller!.CanTarget(player))
					{
						command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
						return;
					}

					if (time == 0)
					{
						if (!player.IsBot)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_gag_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}

						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_gag_message_perm", callerName, player.PlayerName, reason]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}
					else
					{
						if (player is { IsBot: false, IsHLTV: false })
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_gag_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}

						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_gag_message_time", callerName, player.PlayerName, reason, time]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}

					if (_tagsDetected)
						Server.ExecuteCommand($"css_tag_mute {player.SteamID}");

					PlayerPenaltyManager.AddPenalty(player.Slot, PenaltyType.Gag, DateTime.Now.AddMinutes(time), time);
				}

				Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Gag, DiscordWebhookClientPenalty, _localizer);
			}

			Task.Run(async () =>
			{
				await muteManager.AddMuteBySteamid(steamid, adminInfo, reason, time);
			});

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			command.ReplyToCommand($"Gagged player with steamid {steamid}.");
		}

		[ConsoleCommand("css_ungag")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<steamid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnUngagCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerSteamId = caller?.SteamID.ToString() ?? "Console";

			var foundPlayerName = string.Empty;
			var foundPlayerSteamId64 = string.Empty;
			var reason = command.GetArg(2);

			if (command.GetArg(1).Length <= 1)
			{
				command.ReplyToCommand($"Too short pattern to search.");
				return;
			}

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			var found = false;

			var pattern = command.GetArg(1);
			MuteManager muteManager = new(_database);

			if (Helper.ValidateSteamId(pattern, out var steamId) && steamId != null)
			{
				var matches = Helper.GetPlayerFromSteamid64(steamId.SteamId64.ToString());
				if (matches.Count == 1)
				{
					var player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Gag);

						if (_tagsDetected)
							Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");

						found = true;
						foundPlayerName = player.PlayerName;
						foundPlayerSteamId64 = player.SteamID.ToString();
					}
				}
			}
			else
			{
				var matches = Helper.GetPlayerFromName(pattern);
				if (matches.Count == 1)
				{
					var player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Gag);

						if (_tagsDetected)
							Server.ExecuteCommand($"css_tag_unmute {player.SteamID.ToString()}");

						pattern = player.SteamID.ToString();

						found = true;
						foundPlayerName = player.PlayerName;
						foundPlayerSteamId64 = player.SteamID.ToString();
					}
				}
			}

			if (found)
			{
				Task.Run(async () => { await muteManager.UnmutePlayer(foundPlayerSteamId64, callerSteamId, reason); }); // Unmute by type 0 (gag)
				command.ReplyToCommand($"Ungaged player {foundPlayerName}.");
			}
			else
			{
				Task.Run(async () => { await muteManager.UnmutePlayer(pattern, callerSteamId, reason); }); // Unmute by type 0 (gag)
				command.ReplyToCommand($"Ungaged offline player with pattern {pattern}.");
			}

			/*
			TargetResult? targets = GetTarget(command);
			if (targets == null) return;
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player!= null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected &&!player.IsHLTV).ToList();

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
						Task.Run(async () =>
						{
							await _muteManager.UnmutePlayer(player.SteamID.ToString(), 0); // Unmute by type 0 (gag)
						});

					if (TagsDetected)
						Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");
				});

				command.ReplyToCommand($"Ungaged player with pattern {pattern}.");
				return;
			*/
		}

		[ConsoleCommand("css_mute")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnMuteCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerName = caller == null ? "Console" : caller.PlayerName;

			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			var targets = GetTarget(command);
			if (targets == null) return;
			var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			int.TryParse(command.GetArg(2), out var time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			MuteManager muteManager = new(_database);
			var playerPenaltyManager = new PlayerPenaltyManager();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Mute(caller, player, time, reason, callerName, muteManager, playerPenaltyManager, command);
				}
			});
		}

		internal void Mute(CCSPlayerController? caller, CCSPlayerController? player, int time, string reason, string? callerName = null, MuteManager? muteManager = null, PlayerPenaltyManager? playerPenaltyManager = null, CommandInfo? command = null)
		{
			if (_database == null) return;
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			muteManager ??= new MuteManager(_database);

			var playerInfo = new PlayerInfo
			{
				SteamId = player?.SteamID.ToString(),
				Name = player?.PlayerName,
				IpAddress = player?.IpAddress?.Split(":")[0]
			};

			var adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			player!.VoiceFlags = VoiceFlags.Muted;

			Task.Run(async () =>
			{
				await muteManager.MutePlayer(playerInfo, adminInfo, reason, time, 1);
			});

			PlayerPenaltyManager.AddPenalty(player.Slot, PenaltyType.Mute, DateTime.Now.AddMinutes(time), time);

			if (time == 0)
			{
				if (!player.IsBot)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_mute_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
					}

				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_mute_message_perm", callerName, player.PlayerName, reason]);
							controller.PrintToChat(sb.ToString());
						}
					}
				}
			}
			else
			{
				if (player is { IsBot: false, IsHLTV: false })
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_mute_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
					}
				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_mute_message_time", callerName, player.PlayerName, reason, time]);
							controller.PrintToChat(sb.ToString());
						}
					}
				}
			}

			if (command != null)
			{
				Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
				Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Mute, DiscordWebhookClientPenalty, _localizer);
				Helper.LogCommand(caller, command);
			}
		}

		[ConsoleCommand("css_addmute")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnAddMuteCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerName = caller == null ? "Console" : caller.PlayerName;

			if (command.ArgCount < 2)
				return;

			if (string.IsNullOrEmpty(command.GetArg(1))) return;

			if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			var steamid = steamId.SteamId64.ToString();

			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			MuteManager muteManager = new(_database);

			int.TryParse(command.GetArg(2), out var time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			var adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			var matches = Helper.GetPlayerFromSteamid64(steamid);
			if (matches.Count == 1)
			{
				var player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					if (!caller!.CanTarget(player))
					{
						command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
						return;
					}

					PlayerPenaltyManager.AddPenalty(player.Slot, PenaltyType.Mute, DateTime.Now.AddMinutes(time), time);

					if (time == 0)
					{
						if (player is { IsBot: false, IsHLTV: false })
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_mute_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_mute_message_perm", callerName, player.PlayerName, reason]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}
					else
					{
						if (player is { IsBot: false, IsHLTV: false })
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_mute_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_mute_message_time", callerName, player.PlayerName, reason, time]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}
				}

				Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Mute, DiscordWebhookClientPenalty, _localizer);
			}

			Task.Run(async () =>
			{
				await muteManager.AddMuteBySteamid(steamid, adminInfo, reason, time, 1);
			});

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			command.ReplyToCommand($"Muted player with steamid {steamid}.");
		}

		[ConsoleCommand("css_unmute")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<steamid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnUnmuteCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerSteamId = caller?.SteamID.ToString() ?? "Console";

			var foundPlayerName = string.Empty;
			var foundPlayerSteamId64 = string.Empty;
			var reason = command.GetArg(2);

			if (command.GetArg(1).Length <= 1)
			{
				command.ReplyToCommand($"Too short pattern to search.");
				return;
			}

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			var pattern = command.GetArg(1);
			var found = false;
			MuteManager muteManager = new(_database);

			if (Helper.ValidateSteamId(pattern, out var steamId) && steamId != null)
			{
				var matches = Helper.GetPlayerFromSteamid64(steamId.SteamId64.ToString());
				if (matches.Count == 1)
				{
					var player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Mute);
						player.VoiceFlags = VoiceFlags.Normal;
						found = true;
						foundPlayerName = player.PlayerName;
						foundPlayerSteamId64 = player.SteamID.ToString();
					}
				}
			}
			else
			{
				var matches = Helper.GetPlayerFromName(pattern);
				if (matches.Count == 1)
				{
					var player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Mute);
						player.VoiceFlags = VoiceFlags.Normal;
						pattern = player.SteamID.ToString();
						found = true;
						foundPlayerName = player.PlayerName;
						foundPlayerSteamId64 = player.SteamID.ToString();
					}
				}
			}

			if (found)
			{
				Task.Run(async () => { await muteManager.UnmutePlayer(foundPlayerSteamId64, callerSteamId, reason, 1); }); // Unmute by type 1 (mute)
				command.ReplyToCommand($"Unmuted player {foundPlayerName}.");
			}
			else
			{
				Task.Run(async () => { await muteManager.UnmutePlayer(pattern, callerSteamId, reason, 1); }); // Unmute by type 1 (mute)
				command.ReplyToCommand($"Unmuted offline player with pattern {pattern}.");
			}

			/*
			TargetResult? targets = GetTarget(command);
			if (targets == null) return;
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player!= null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected &&!player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (playersToTarget.Count > 1)
			{
				playersToTarget.ForEach(player =>
				{
					if (player.Connected == PlayerConnectedState.PlayerConnected)
						Task.Run(async () =>
						{
							await _muteManager.UnmutePlayer(player.SteamID.ToString(), 1); // Unmute by type 1 (mute)
						});

					playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Mute);
					player.VoiceFlags = VoiceFlags.Normal;
				});
			*/
		}

		[ConsoleCommand("css_silence")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnSilenceCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerName = caller == null ? "Console" : caller.PlayerName;

			var reason = CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";

			var targets = GetTarget(command);
			if (targets == null) return;
			var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			int.TryParse(command.GetArg(2), out var time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			MuteManager muteManager = new(_database);
			var playerPenaltyManager = new PlayerPenaltyManager();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Silence(caller, player, time, reason, callerName, muteManager, playerPenaltyManager, command);
				}
			});
		}

		internal void Silence(CCSPlayerController? caller, CCSPlayerController? player, int time, string reason, string? callerName = null, MuteManager? muteManager = null, PlayerPenaltyManager? playerPenaltyManager = null, CommandInfo? command = null)
		{
			if (_database == null) return;
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			muteManager ??= new MuteManager(_database);

			var playerInfo = new PlayerInfo
			{
				SteamId = player?.SteamID.ToString(),
				Name = player?.PlayerName,
				IpAddress = player?.IpAddress?.Split(":")[0]
			};

			var adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			Task.Run(async () =>
			{
				await muteManager.MutePlayer(playerInfo, adminInfo, reason, time, 2);
			});

			if (_tagsDetected)
				Server.ExecuteCommand($"css_tag_mute {player!.SteamID}");

			player!.VoiceFlags = VoiceFlags.Muted;
			PlayerPenaltyManager.AddPenalty(player.Slot, PenaltyType.Silence, DateTime.Now.AddMinutes(time), time);

			if (time == 0)
			{
				if (!player.IsBot)
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_silence_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
					}
				}

				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_silence_message_perm", callerName, player.PlayerName, reason]);
							controller.PrintToChat(sb.ToString());
						}
					}
				}
			}
			else
			{
				if (!player.IsBot)
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_silence_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
					}
				}

				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_silence_message_time", callerName, player.PlayerName, reason, time]);
							controller.PrintToChat(sb.ToString());
						}
					}
				}
			}

			if (command == null) return;

			Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Mute, DiscordWebhookClientPenalty, _localizer);
			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);
		}

		[ConsoleCommand("css_addsilence")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnAddSilenceCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerName = caller == null ? "Console" : caller.PlayerName;

			if (command.ArgCount < 2)
				return;
			if (string.IsNullOrEmpty(command.GetArg(1))) return;

			if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			var steamid = steamId.SteamId64.ToString();

			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			MuteManager muteManager = new(_database);

			int.TryParse(command.GetArg(2), out var time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			var adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			var matches = Helper.GetPlayerFromSteamid64(steamid);
			if (matches.Count == 1)
			{
				var player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					if (!caller!.CanTarget(player))
					{
						command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
						return;
					}

					if (_tagsDetected)
						Server.ExecuteCommand($"css_tag_mute {player.SteamID}");

					PlayerPenaltyManager.AddPenalty(player.Slot, PenaltyType.Silence, DateTime.Now.AddMinutes(time), time);

					if (time == 0)
					{
						if (player is { IsBot: false, IsHLTV: false })
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_silence_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_silence_message_perm", callerName, player.PlayerName, reason]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}
					else
					{
						if (player is { IsBot: false, IsHLTV: false })
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_silence_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_silence_message_time", callerName, player.PlayerName, reason, time]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}

					Helper.SendDiscordPenaltyMessage(caller, player, reason, time, Helper.PenaltyType.Mute, DiscordWebhookClientPenalty, _localizer);
				}
			}
			Task.Run(async () =>
			{
				await muteManager.AddMuteBySteamid(steamid, adminInfo, reason, time, 2);
			});

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			command.ReplyToCommand($"Silenced player with steamid {steamid}.");
		}

		[ConsoleCommand("css_unsilence")]
		[RequiresPermissions("@css/chat")]
		[CommandHelper(minArgs: 1, usage: "<steamid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnUnsilenceCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerSteamId = caller?.SteamID.ToString() ?? "Console";

			var foundPlayerName = string.Empty;
			var foundPlayerSteamId64 = string.Empty;
			var reason = command.GetArg(2);

			if (command.GetArg(1).Length <= 1)
			{
				command.ReplyToCommand($"Too short pattern to search.");
				return;
			}

			Helper.SendDiscordLogMessage(caller, command, DiscordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			var pattern = command.GetArg(1);
			var found = false;
			MuteManager muteManager = new(_database);

			if (Helper.ValidateSteamId(pattern, out var steamId) && steamId != null)
			{
				var matches = Helper.GetPlayerFromSteamid64(steamId.SteamId64.ToString());
				if (matches.Count == 1)
				{
					var player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						if (_tagsDetected)
							Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");

						PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Silence);
						player.VoiceFlags = VoiceFlags.Normal;
						found = true;
						foundPlayerName = player.PlayerName;
						foundPlayerSteamId64 = player.SteamID.ToString();
					}
				}
			}
			else
			{
				var matches = Helper.GetPlayerFromName(pattern);
				if (matches.Count == 1)
				{
					var player = matches.FirstOrDefault();
					if (player != null && player.IsValid)
					{
						if (_tagsDetected)
							Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");

						PlayerPenaltyManager.RemovePenaltiesByType(player.Slot, PenaltyType.Silence);
						player.VoiceFlags = VoiceFlags.Normal;
						pattern = player.SteamID.ToString();
						found = true;
						foundPlayerName = player.PlayerName;
						foundPlayerSteamId64 = player.SteamID.ToString();
					}
				}
			}

			if (found)
			{
				Task.Run(async () => { await muteManager.UnmutePlayer(foundPlayerSteamId64, callerSteamId, reason, 2); }); // Unmute by type 2 (silence)
				command.ReplyToCommand($"Unsilenced player {foundPlayerName}.");
			}
			else
			{
				Task.Run(async () => { await muteManager.UnmutePlayer(pattern, callerSteamId, reason, 2); }); // Unmute by type 2 (silence)
				command.ReplyToCommand($"Unsilenced offline player with pattern {pattern}.");
			}
			/*
			TargetResult? targets = GetTarget(command);
			if (targets == null) return;
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player!= null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected &&!player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (playersToTarget.Count > 1)
			{
				playersToTarget.ForEach(player =>
				{
					if (player.Connected == PlayerConnectedState.PlayerConnected)
						Task.Run(async () => { await _muteManager.UnmutePlayer(player.SteamID.ToString(), 2); }); // Unmute by type 2 (silence)

					if (TagsDetected)
						Server.ExecuteCommand($"css_tag_unmute {player!.SteamID}");

					playerPenaltyManager.RemovePenaltiesByType(player!.Slot, PenaltyType.Silence);
					player.VoiceFlags = VoiceFlags.Normal;
				});

				command.ReplyToCommand($"Unsilenced player with pattern {pattern}.");
				return;
			*/
		}
	}
}