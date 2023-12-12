using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using System.Text;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		private void registerEvents()
		{
			RegisterListener<OnClientAuthorized>(OnClientAuthorized);
			RegisterListener<OnClientDisconnect>(OnClientDisconnect);
			RegisterListener<OnMapStart>(OnMapStart);
			AddCommandListener("say", OnCommandSay);
			AddCommandListener("say_team", OnCommandTeamSay);
			AddCommandListener("callvote", OnCommandCallVote);
		}
		private HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
		{
			if (player == null || !player.IsValid || info.GetArg(1).Length == 0) return HookResult.Continue;

			if (gaggedPlayers.Contains((int)player.Index))
			{
				return HookResult.Handled;
			}

			return HookResult.Continue;
		}

		private HookResult OnCommandTeamSay(CCSPlayerController? player, CommandInfo info)
		{
			if (player == null || !player.IsValid || info.GetArg(1).Length == 0) return HookResult.Continue;

			if (gaggedPlayers.Contains((int)player.Index))
			{
				return HookResult.Handled;
			}

			if (info.GetArg(1).StartsWith("@"))
			{
				StringBuilder sb = new();

				if (AdminManager.PlayerHasPermissions(player, "@css/chat"))
				{
					sb.Append(_localizer!["sa_adminchat_template_admin", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
					foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/chat")))
					{
						p.PrintToChat(sb.ToString());
					}
				}
				else
				{
					sb.Append(_localizer!["sa_adminchat_template_player", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
					player.PrintToChat(sb.ToString());
					foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/chat")))
					{
						p.PrintToChat(sb.ToString());
					}
				}

				return HookResult.Handled;
			}

			return HookResult.Continue;
		}

		private HookResult OnCommandCallVote(CCSPlayerController? player, CommandInfo info)
		{
			string reason = info.GetArg(1);

			if (reason == "kick" || reason == "ban")
			{
				int.TryParse(info.GetArg(2), out int target);
				if (target > 0)
				{
					if (!player!.CanTarget(Utilities.GetPlayerFromUserid(target)))
						return HookResult.Handled;
				}
			}

			return HookResult.Continue;
		}

		private void OnClientAuthorized(int playerSlot, SteamID steamID)
		{
			CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
				return;

			if (player.AuthorizedSteamID == null)
			{
				AddTimer(3.0f, () =>
				{
					OnClientAuthorized(playerSlot, steamID);
				});
				return;
			}

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
				if (player == null || !player.IsValid) return;
				BanManager _banManager = new(dbConnectionString);
				bool isBanned = await _banManager.IsPlayerBanned(playerInfo);

				MuteManager _muteManager = new(dbConnectionString);
				List<dynamic> activeMutes = await _muteManager.IsPlayerMuted(playerInfo.SteamId!);

				Server.NextFrame(() =>
				{
					if (player == null || !player.IsValid) return;
					if (isBanned)
					{
						Helper.KickPlayer((ushort)player.UserId!, "Banned");
						return;
					}

					if (activeMutes.Count > 0)
					{
						foreach (var mute in activeMutes)
						{
							string muteType = mute.type;
							TimeSpan duration = mute.ends - mute.created;
							int durationInSeconds = (int)duration.TotalSeconds;

							if (muteType == "GAG")
							{
								// Chat mute
								if (!gaggedPlayers.Any(index => index == player.Index))
									gaggedPlayers.Add((int)player.Index);

								if (TagsDetected)
									NativeAPI.IssueServerCommand($"css_tag_mute {player.Index}");

								if (duration.Minutes >= 0 && duration.Minutes <= 30)
								{
									AddTimer(durationInSeconds, () =>
									{
										if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

										if (gaggedPlayers.Contains((int)player.Index))
										{
											if (gaggedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
											{
												gaggedPlayers.Add(removedItem);
											}
										}

										if (TagsDetected)
											NativeAPI.IssueServerCommand($"css_tag_unmute {player.Index}");

										MuteManager _muteManager = new(dbConnectionString);
										_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 0);
									}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
								}


								/*
								CCSPlayerController currentPlayer = player;

								if (mute.duration == 0 || durationInSeconds >= 1800) continue;

								await Task.Delay(TimeSpan.FromSeconds(durationInSeconds));

								if (currentPlayer != null && currentPlayer.IsValid)
								{
									NativeAPI.IssueServerCommand($"css_tag_unmute {currentPlayer.Index.ToString()}");
									await UnmutePlayer(currentPlayer.AuthorizedSteamID.SteamId64.ToString(), 0);
								}
								*/
							}
							else
							{
								// Voice mute
								player.VoiceFlags = VoiceFlags.Muted;

								if (duration.Minutes >= 0 && duration.Minutes <= 30)
								{
									AddTimer(durationInSeconds, () =>
									{
										if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

										if (mutedPlayers.Contains((int)player.Index))
										{
											if (mutedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
											{
												mutedPlayers.Add(removedItem);
											}
										}

										player.VoiceFlags = VoiceFlags.Normal;

										//MuteManager _muteManager = new(dbConnectionString);
										//_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 1);
									}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
								}
							}
						}
					}
				});
			});
		}

		private void OnClientDisconnect(int playerSlot)
		{
			CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return;

			if (gaggedPlayers.Contains((int)player.Index))
			{
				if (gaggedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
				{
					gaggedPlayers.Add(removedItem);
				}
			}

			if (mutedPlayers.Contains((int)player.Index))
			{
				if (mutedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
				{
					mutedPlayers.Add(removedItem);
				}
			}

			if (TagsDetected)
				NativeAPI.IssueServerCommand($"css_tag_unmute {player!.Index.ToString()}");
		}

		private void OnMapStart(string mapName)
		{
			AddTimer(120.0f, () =>
			{
				BanManager _banManager = new(dbConnectionString);
				_ = _banManager.ExpireOldBans();
				MuteManager _muteManager = new(dbConnectionString);
				_ = _muteManager.ExpireOldMutes();
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			string? path = Path.GetDirectoryName(ModuleDirectory);
			if (Directory.Exists(path + "/CS2-Tags"))
			{
				TagsDetected = true;
			}
		}
	}
}
