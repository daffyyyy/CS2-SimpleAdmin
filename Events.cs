using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
	private void registerEvents()
	{
		//RegisterListener<OnClientAuthorized>(OnClientAuthorized);
		//RegisterListener<OnClientConnect>(OnClientConnect);
		RegisterListener<OnClientPutInServer>(OnClientPutInServer);
		//RegisterEventHandler<EventPlayerConnectFull>(OnPlayerFullConnect);
		RegisterListener<OnClientDisconnect>(OnClientDisconnect);
		RegisterListener<OnMapStart>(OnMapStart);
		RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
		RegisterEventHandler<EventRoundStart>(OnRoundStart);
		AddCommandListener("say", OnCommandSay);
		AddCommandListener("say_team", OnCommandTeamSay);
	}

	/*private HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player == null || player.IsBot || player.IsHLTV) return HookResult.Continue;

		PlayerInfo playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Index = (ushort)player.UserId,
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
		godPlayers.Clear();
#if DEBUG
		Logger.LogCritical("[OnRoundStart]");
#endif
		return HookResult.Continue;
	}

	private HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || info.GetArg(1).Length == 0) return HookResult.Continue;

		if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
		{
			return HookResult.Handled;
		}

		return HookResult.Continue;
	}

	private HookResult OnCommandTeamSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || info.GetArg(1).Length == 0) return HookResult.Continue;

		if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
		{
			return HookResult.Handled;
		}

		if (info.GetArg(1).StartsWith("@"))
		{
			StringBuilder sb = new();

			if (AdminManager.PlayerHasPermissions(player, "@css/chat"))
			{
				sb.Append(_localizer!["sa_adminchat_template_admin", player!.PlayerName, info.GetArg(1).Remove(0, 1)]);
				foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && AdminManager.PlayerHasPermissions(p, "@css/chat")))
				{
					p.PrintToChat(sb.ToString());
				}
			}
			else
			{
				sb.Append(_localizer!["sa_adminchat_template_player", player!.PlayerName, info.GetArg(1).Remove(0, 1)]);
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

	private void OnClientPutInServer(int playerSlot)
	{
		CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
#if DEBUG
		Logger.LogCritical("[OnClientPutInServer] Before check");
#endif

		if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || player.Connected == PlayerConnectedState.PlayerDisconnecting)
			return;
#if DEBUG
		Logger.LogCritical("[OnClientPutInServer] After Check");
#endif

		string? ipAddress = !string.IsNullOrEmpty(player.IpAddress) ? player.IpAddress.Split(":")[0] : null;

		if (
			ipAddress != null && bannedPlayers.Contains(ipAddress) ||
			player.SteamID.ToString() != "" && bannedPlayers.Contains(player.SteamID.ToString())
			)
			Helper.KickPlayer((ushort)player.UserId!, "Banned");

		PlayerInfo playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Index = (ushort)player.Index,
			SteamId = player?.SteamID.ToString(),
			Name = player?.PlayerName,
			IpAddress = ipAddress
		};

		_ = Task.Run(async () =>
		{
			if (_database == null) return;

			BanManager _banManager = new(_database, Config);
			bool isBanned = await _banManager.IsPlayerBanned(playerInfo);

			MuteManager _muteManager = new(_database);
			List<dynamic> activeMutes = await _muteManager.IsPlayerMuted(playerInfo.SteamId!);

			AdminSQLManager _adminManager = new(_database);
			List<(List<string>, int)> activeFlags = await _adminManager.GetAdminFlags(playerInfo.SteamId!);

			Server.NextFrame(() =>
			{
				if (player == null || !player.IsValid) return;
				if (isBanned)
				{
					if (playerInfo.IpAddress != null && !bannedPlayers.Contains(playerInfo.IpAddress))
						bannedPlayers.Add(playerInfo.IpAddress);
					if (player.SteamID.ToString() != "" && !bannedPlayers.Contains(player.SteamID.ToString()))
						bannedPlayers.Add(player.SteamID.ToString());

					Helper.KickPlayer((ushort)player.UserId!, "Banned");
					return;
				}

				//Helper.GivePlayerFlags(player, activeFlags);

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
							if (player.SteamID.ToString() != "" && !gaggedPlayers.Any(steamid => steamid.Equals(player.SteamID.ToString())))
								gaggedPlayers.Add(player.SteamID.ToString());

							if (TagsDetected)
								NativeAPI.IssueServerCommand($"css_tag_mute {player!.SteamID.ToString()}");

							if (durationInSeconds != 0 && duration.Minutes >= 0 && duration.Minutes <= 30)
							{
								AddTimer(durationInSeconds, () =>
								{
									if (player == null || !player.IsValid || player.SteamID.ToString() == "") return;

									if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
									{
										if (gaggedPlayers.TryTake(out string? removedItem) && removedItem != player.SteamID.ToString())
										{
											gaggedPlayers.Add(removedItem);
										}
									}

									if (TagsDetected)
										NativeAPI.IssueServerCommand($"css_tag_unmute {player!.SteamID.ToString()}");

									MuteManager _muteManager = new(_database);
									_ = _muteManager.UnmutePlayer(player!.SteamID.ToString(), 0);
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
						else if (muteType == "MUTE")
						{
							// Voice mute
							player.VoiceFlags = VoiceFlags.Muted;

							if (durationInSeconds != 0 && duration.Minutes >= 0 && duration.Minutes <= 30)
							{
								AddTimer(durationInSeconds, () =>
								{
									if (player == null || !player.IsValid || player.SteamID.ToString() == "") return;

									/*
									if (mutedPlayers.Contains((ushort)player.UserId))
									{
										if (mutedPlayers.TryTake(out int removedItem) && removedItem != (ushort)player.UserId)
										{
											mutedPlayers.Add(removedItem);
										}
									}
									*/

									player.VoiceFlags = VoiceFlags.Normal;

									//MuteManager _muteManager = new(_database);
									//_ = _muteManager.UnmutePlayer(player.AuthorizedSteamID.SteamId64.ToString(), 1);
								}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
							}
						}
					}
				}

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
			});
		});

		/*
		CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

		if (player == null || !player.IsValid || player.IpAddress == null || player.UserId == null || loadedPlayers.Contains((ushort)player.UserId) || player.IsBot || player.IsHLTV)
			return;

		if (bannedPlayers.Contains(player.IpAddress) || player.AuthorizedSteamID != null && bannedPlayers.Contains(player.AuthorizedSteamID.SteamId64.ToString()))
			Helper.KickPlayer((ushort)player.UserId!, "Banned");

		PlayerInfo playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Index = (ushort)player.UserId,
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
					if (player.IpAddress != null && !bannedPlayers.Contains(player.IpAddress))
						bannedPlayers.Add(player.IpAddress);
					if (player.AuthorizedSteamID != null && !bannedPlayers.Contains(player.AuthorizedSteamID.SteamId64.ToString()))
						bannedPlayers.Add(player.AuthorizedSteamID.SteamId64.ToString());
					Helper.KickPlayer((ushort)player.UserId!, "Banned");
				}
			});
		});
		*/
	}

	private void OnClientDisconnect(int playerSlot)
	{
		CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
#if DEBUG
		Logger.LogCritical("[OnClientDisconnect] Before");
#endif

		if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return;
#if DEBUG
		Logger.LogCritical("[OnClientDisconnect] After Check");
#endif

		if (player != null && player.SteamID.ToString() != "" && gaggedPlayers.Contains(player.SteamID.ToString()))
		{
			if (gaggedPlayers.TryTake(out string? removedItem) && removedItem != player.SteamID.ToString())
			{
				gaggedPlayers.Add(removedItem);
			}
		}

		/*
		if (mutedPlayers.Contains((ushort)player.UserId))
		{
			if (mutedPlayers.TryTake(out int removedItem) && removedItem != (ushort)player.UserId)
			{
				mutedPlayers.Add(removedItem);
			}
		}
		*/

		if (player!.UserId != null && silentPlayers.Contains((ushort)player.UserId))
		{
			silentPlayers.Remove((ushort)player.UserId);
		}

		if (player.UserId != null && godPlayers.Contains((ushort)player.UserId))
		{
			godPlayers.Remove((ushort)player.UserId);
		}

		if (player.AuthorizedSteamID != null && AdminSQLManager._adminCacheSet.Contains(player.AuthorizedSteamID))
		{
			if (AdminSQLManager._adminCacheTimestamps != null && AdminSQLManager._adminCacheTimestamps.TryGetValue(player.AuthorizedSteamID, out DateTime? expirationTime) && expirationTime.HasValue && expirationTime.Value <= DateTime.Now)
			{
				AdminManager.ClearPlayerPermissions(player.AuthorizedSteamID);
				AdminManager.RemovePlayerAdminData(player.AuthorizedSteamID);
			}
		}

		if (TagsDetected)
			NativeAPI.IssueServerCommand($"css_tag_unmute {player!.SteamID.ToString()}");
	}

	private void OnMapStart(string mapName)
	{
		gaggedPlayers.Clear();

		if (_database == null) return;

		AdminSQLManager _adminManager = new(_database);

		AddTimer(60.0f, bannedPlayers.Clear, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
		AddTimer(120.0f, async () =>
		{
			BanManager _banManager = new(_database, Config);
			MuteManager _muteManager = new(_database);
			await _banManager.ExpireOldBans();
			await _muteManager.ExpireOldMutes();
			await _adminManager.DeleteOldAdmins();
#if DEBUG
			Logger.LogCritical("[OnMapStart] Expired check");
#endif

		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

		string? path = Path.GetDirectoryName(ModuleDirectory);
		if (Directory.Exists(path + "/CS2-Tags"))
		{
			TagsDetected = true;
		}

		AddTimer(2.0f, async () =>
		{
			string? address = $"{ConVar.Find("ip")!.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";
			string? hostname = ConVar.Find("hostname")!.StringValue;

			await Task.Run(async () =>
			{
				using (var connection = _database.GetConnection())
				{
					await connection.ExecuteAsync(
						"INSERT INTO `sa_servers` (address, hostname) VALUES (@address, @hostname) " +
						"ON DUPLICATE KEY UPDATE hostname = @hostname",
						new { address = $"{address}", hostname });

					int? serverId = await connection.ExecuteScalarAsync<int>(
						"SELECT `id` FROM `sa_servers` WHERE `address` = @address",
						new { address = $"{address}" });

					ServerId = serverId;

					await _adminManager.GiveAllFlags();
				}
			});
		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
	}

	private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || player.Connected == PlayerConnectedState.PlayerDisconnecting)
			return HookResult.Continue;

		if (player.UserId != null && godPlayers.Contains((ushort)player.UserId) && player.PawnIsAlive)
		{
			player.Health = 100;
			player.PlayerPawn.Value!.Health = 100;
		}

		return HookResult.Continue;
	}
}