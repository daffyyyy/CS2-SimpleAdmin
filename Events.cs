using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using MySqlConnector;
using System.Text;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
	private void registerEvents()
	{
		RegisterListener<OnClientAuthorized>(OnClientAuthorized);
		RegisterListener<OnClientConnected>(OnClientConnected);
		//RegisterEventHandler<EventPlayerConnectFull>(OnPlayerFullConnect);
		RegisterListener<OnClientDisconnect>(OnClientDisconnect);
		RegisterListener<OnMapStart>(OnMapStart);
		RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
		RegisterEventHandler<EventRoundStart>(OnRoundStart);
		AddCommandListener("say", OnCommandSay);
		AddCommandListener("say_team", OnCommandTeamSay);
		AddCommandListener("callvote", OnCommandCallVote);
	}

	/*private HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player == null || player.IsBot || player.IsHLTV) return HookResult.Continue;

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
			Server.NextFrame(() =>
			{
				if (player == null) return;
			});
		});
		return HookResult.Continue;
	}
	*/

	private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
	{
		GodPlayers.Clear();
		return HookResult.Continue;
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

	private void OnClientConnected(int playerSlot)
	{
		CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

		if (player == null || !player.IsValid || player.IpAddress == null || loadedPlayers.Contains((int)player.Index) || player.IsBot || player.IsHLTV)
			return;

		PlayerInfo playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Index = (int)player.Index,
			SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
			Name = player?.PlayerName,
			IpAddress = player?.IpAddress.Split(":")[0]
		};

		Task.Run(async () =>
		{
			BanManager _banManager = new(dbConnectionString);
			bool isBanned = await _banManager.IsPlayerBanned(playerInfo);
			Server.NextFrame(() =>
			{
				if (player == null || !player.IsValid) return;
				if (isBanned)
				{
					Helper.KickPlayer((ushort)player.UserId!, "Banned");
					return;
				}
			});
		});
	}

	private void OnClientAuthorized(int playerSlot, SteamID steamID)
	{
		CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

		if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
			return;

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
			BanManager _banManager = new(dbConnectionString);
			bool isBanned = await _banManager.IsPlayerBanned(playerInfo);

			MuteManager _muteManager = new(dbConnectionString);
			List<dynamic> activeMutes = await _muteManager.IsPlayerMuted(playerInfo.SteamId!);

			AdminSQLManager _adminManager = new(dbConnectionString);
			List<(List<string>, int)> activeFlags = await _adminManager.GetAdminFlags(playerInfo.SteamId!);

			Server.NextFrame(() =>
			{
				if (player == null || !player.IsValid) return;
				if (isBanned)
				{
					Helper.KickPlayer((ushort)player.UserId!, "Banned");
					Console.WriteLine("Authorized banned");
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

							if (durationInSeconds != 0 && duration.Minutes >= 0 && duration.Minutes <= 30)
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

							if (durationInSeconds != 0 && duration.Minutes >= 0 && duration.Minutes <= 30)
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

				AddTimer(6, () => Helper.GivePlayerFlags(player, activeFlags));

				/*

				if (_adminManager._adminCache != null && _adminManager._adminCache.Count > 0)
				{
					foreach (var flags in activeFlags)
					{
						if (flags == null) continue;
						string flagsValue = flags.flags.ToString();

						if (!string.IsNullOrEmpty(flagsValue))
						{
							string[] _flags = flagsValue.Split(",");

							AddTimer(10, () =>
							{
								if (player == null) return;
								foreach (var _flag in _flags)
								{
									if (_flag.StartsWith("@"))
									{
										AdminManager.AddPlayerPermissions(player, _flag);
									}
									if (_flag.StartsWith("3"))
									{
										AdminManager.AddPlayerToGroup(player, _flag);
									}
								}
							});
						}
					}
			}
				*/

				if (!loadedPlayers.Contains((int)player.Index))
					loadedPlayers.Add((int)player.Index);
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

		if (GodPlayers.Contains((int)player.Index))
		{
			GodPlayers.Remove((int)player.Index);
		}

		if (loadedPlayers.Contains((int)player.Index))
			loadedPlayers.Remove((int)player.Index);

		if (player.AuthorizedSteamID != null)
		{
			//string steamIdString = player.AuthorizedSteamID.SteamId64.ToString();

			//AdminSQLManager._adminCache.TryRemove(steamIdString, out _);
			AdminManager.RemovePlayerPermissions(player);
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
			AdminSQLManager _adminManager = new(dbConnectionString);
			_ = _adminManager.DeleteOldAdmins();
		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

		string? path = Path.GetDirectoryName(ModuleDirectory);
		if (Directory.Exists(path + "/CS2-Tags"))
		{
			TagsDetected = true;
		}

		using (var connection = new MySqlConnection(dbConnectionString))
		{
			connection.Open();

			connection.Execute(
				"INSERT INTO `sa_servers` (address, hostname) VALUES (@address, @hostname) " +
				"ON DUPLICATE KEY UPDATE hostname = @hostname",
				new { address = $"{ConVar.Find("ip")!.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}", hostname = ConVar.Find("hostname")!.StringValue });

			int? serverId = connection.ExecuteScalar<int>(
				"SELECT `id` FROM `sa_servers` WHERE `address` = @address",
				new { address = $"{ConVar.Find("ip")!.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}" });

			ServerId = serverId;

			connection.Close();
		}
	}

	private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player == null || !player.IsValid)
			return HookResult.Continue;

		if (GodPlayers.Contains((int)player.Index) && player.PawnIsAlive)
		{
			player.Health = 100;
			player.PlayerPawn.Value!.Health = 100;
		}

		return HookResult.Continue;
	}
}