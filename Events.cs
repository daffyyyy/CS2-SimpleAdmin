using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
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
		//RegisterListener<OnClientPutInServer>(OnClientPutInServer);
		//RegisterListener<OnClientDisconnect>(OnClientDisconnect);
		//RegisterEventHandler<EventPlayerConnectFull>(OnPlayerFullConnect);
		RegisterListener<OnMapStart>(OnMapStart);
		//RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
		//RegisterEventHandler<EventRoundStart>(OnRoundStart);
		AddCommandListener("say", OnCommandSay);
		AddCommandListener("say_team", OnCommandTeamSay);
	}

	/*private HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player is null || player.IsBot || player.IsHLTV) return HookResult.Continue;

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
				if (player is null) return;
			});
		});
		return HookResult.Continue;
	}
	*/

	[GameEventHandler]
	private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
	{
#if DEBUG
		Logger.LogCritical("[OnRoundEnd]");
#endif
		godPlayers.Clear();
		return HookResult.Continue;
	}

	private HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || info.GetArg(1).Length == 0) return HookResult.Continue;

		if (player != null && gaggedPlayers.Contains(player.Slot))
		{
			return HookResult.Handled;
		}

		return HookResult.Continue;
	}

	private HookResult OnCommandTeamSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || info.GetArg(1).Length == 0) return HookResult.Continue;

		if (player != null && gaggedPlayers.Contains(player.Slot))
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

	[GameEventHandler]
	public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
	{
		if (!@event.Userid.IsValid || !@event.Userid.PlayerPawn.IsValid)
			return HookResult.Continue;

		CCSPlayerController? player = @event.Userid;
#if DEBUG
		Logger.LogCritical("[OnPlayerConnect] Before check");
#endif
		if (_database == null || player is null || !player.IsValid || player.IsBot || player.IsHLTV)
			return HookResult.Continue;

#if DEBUG
		Logger.LogCritical("[OnPlayerConnect] After Check");
#endif

		string? ipAddress = !string.IsNullOrEmpty(player.IpAddress) ? player.IpAddress.Split(":")[0] : null;

		if (
			ipAddress != null && bannedPlayers.Contains(ipAddress) ||
			bannedPlayers.Contains(player.SteamID.ToString())
			)
		{
			Server.NextFrame(() =>
			{
				Helper.KickPlayer((ushort)player.UserId!, "Banned");
			});
			return HookResult.Continue;
		}

		PlayerInfo playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Index = (ushort)player.Index,
			Slot = player.Slot,
			SteamId = player.SteamID.ToString(),
			Name = player.PlayerName,
			IpAddress = ipAddress
		};

		Task.Run(async () =>
		{
			BanManager _banManager = new(_database, Config);

			MuteManager _muteManager = new(_database);
			List<dynamic> activeMutes = await _muteManager.IsPlayerMuted(playerInfo.SteamId);

			if (await _banManager.IsPlayerBanned(playerInfo))
			{
				if (playerInfo.IpAddress != null && !bannedPlayers.Contains(playerInfo.IpAddress))
					bannedPlayers.Add(playerInfo.IpAddress);

				if (!bannedPlayers.Contains(playerInfo.SteamId))
					bannedPlayers.Add(playerInfo.SteamId);

				Server.NextFrame(() =>
				{
					if (playerInfo.UserId != null)
						Helper.KickPlayer((ushort)playerInfo.UserId, "Banned");
				});

				return;
			}

			if (activeMutes.Count > 0)
			{
				foreach (var mute in activeMutes)
				{
					string muteType = mute.type;

					if (muteType == "GAG")
					{
						// Chat mute
						if (playerInfo.Slot.HasValue && !gaggedPlayers.Contains(playerInfo.Slot.Value))
							gaggedPlayers.Add(playerInfo.Slot.Value);

						if (TagsDetected)
						{
							Server.NextFrame(() =>
							{
								Server.ExecuteCommand($"css_tag_mute {playerInfo.SteamId}");
							});
						}
					}
					else if (muteType == "MUTE")
					{
						// Voice mute
						Server.NextFrame(() =>
						{
							player.VoiceFlags = VoiceFlags.Muted;
						});
					}
				}
			}
		});

		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
	{
		if (!@event.Userid.IsValid || @event.Userid.IsBot)
			return HookResult.Continue;

		CCSPlayerController? player = @event.Userid;
#if DEBUG
		Logger.LogCritical("[OnClientDisconnect] Before");
#endif

		if (player is null || !player.IsValid || player.IsBot || player.IsHLTV)
			return HookResult.Continue;

		if (player.Connected == PlayerConnectedState.PlayerConnecting)
			return HookResult.Continue;
#if DEBUG
		Logger.LogCritical("[OnClientDisconnect] After Check");
#endif

		if (gaggedPlayers.Contains(player.Slot))
		{
			gaggedPlayers = new ConcurrentBag<int>(gaggedPlayers.Where(item => item != player.Slot));
		}

		if (silentPlayers.Contains(player.Slot))
		{
			silentPlayers = new ConcurrentBag<int>(silentPlayers.Where(item => item != player.Slot));
		}

		if (godPlayers.Contains(player.Slot))
		{
			godPlayers = new ConcurrentBag<int>(godPlayers.Where(item => item != player.Slot));
		}

		if (player.AuthorizedSteamID != null && AdminSQLManager._adminCache.ContainsKey(player.AuthorizedSteamID))
		{
			if (AdminSQLManager._adminCache.TryGetValue(player.AuthorizedSteamID, out DateTime? expirationTime) &&
				expirationTime <= DateTime.Now)
			{
				AdminManager.ClearPlayerPermissions(player.AuthorizedSteamID);
				AdminManager.RemovePlayerAdminData(player.AuthorizedSteamID);
			}
		}

		if (TagsDetected)
			NativeAPI.IssueServerCommand($"css_tag_unmute {player!.SteamID}");

		return HookResult.Continue;
	}

	private void OnMapStart(string mapName)
	{
		gaggedPlayers.Clear();
		godPlayers.Clear();
		silentPlayers.Clear();

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

	[GameEventHandler]
	private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player is null || !player.IsValid || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || player.IsBot || player.IsHLTV || player.PlayerPawn.IsValid || player.Connected == PlayerConnectedState.PlayerDisconnecting)
			return HookResult.Continue;

		if (godPlayers.Contains(player.Slot) && player.PawnIsAlive)
		{
			player.Health = 100;
			player.PlayerPawn.Value.Health = 100;
		}

		return HookResult.Continue;
	}
}