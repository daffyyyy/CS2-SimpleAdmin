using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
	private void RegisterEvents()
	{
		RegisterListener<Listeners.OnMapStart>(OnMapStart);
		RegisterListener<Listeners.OnGameServerSteamAPIActivated>(OnGameServerSteamAPIActivated);
		AddCommandListener("say", OnCommandSay);
		AddCommandListener("say_team", OnCommandTeamSay);
	}

	private void OnGameServerSteamAPIActivated()
	{
		AddTimer(8.5f, () =>
		{
			if (ServerId != null || _database == null) return;

			var ipAddress = ConVar.Find("ip")?.StringValue;

			if (string.IsNullOrEmpty(ipAddress) || ipAddress.StartsWith("0.0.0"))
			{
				ipAddress = Helper.GetServerIp();
			}

			if (string.IsNullOrEmpty(ipAddress) || ipAddress.StartsWith("0.0.0"))
			{
				OnGameServerSteamAPIActivated();
				return;
			}

			var address = $"{ipAddress}:{ConVar.Find("hostport")?.GetPrimitiveValue<int>()}";
			var hostname = ConVar.Find("hostname")!.StringValue;

			Task.Run(async () =>
			{
				PermissionManager adminManager = new(_database);

				try
				{
					await using var connection = await _database.GetConnectionAsync();
					var addressExists = await connection.ExecuteScalarAsync<bool>(
						"SELECT COUNT(*) FROM sa_servers WHERE address = @address",
						new { address });

					if (!addressExists)
					{
						await connection.ExecuteAsync(
							"INSERT INTO sa_servers (address, hostname) VALUES (@address, @hostname)",
							new { address, hostname });
					}
					else
					{
						await connection.ExecuteAsync(
							"UPDATE `sa_servers` SET `hostname` = @hostname, `id` = `id` WHERE `address` = @address",
							new { address, hostname });
					}

					int? serverId = await connection.ExecuteScalarAsync<int>(
						"SELECT `id` FROM `sa_servers` WHERE `address` = @address",
						new { address });

					ServerId = serverId;
				}
				catch (Exception ex)
				{
					_logger?.LogCritical("Unable to create or get server_id" + ex.Message);
				}

				if (Config.EnableMetrics)
				{
					var queryString = $"?address={address}&hostname={hostname}";
					using HttpClient client = new();

					try
					{
						await client.GetAsync($"https://api.daffyy.love/index.php{queryString}");
					}
					catch (HttpRequestException ex)
					{
						Logger.LogWarning($"Unable to make metrics call: {ex.Message}");
					}
				}
			});
		});
	}

	[GameEventHandler]
	public HookResult OnClientDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

#if DEBUG
        Logger.LogCritical("[OnClientDisconnect] Before");
#endif

		if (player == null || !player.IsValid || player.IsBot)
		{
			return HookResult.Continue;
		}

#if DEBUG
        Logger.LogCritical("[OnClientDisconnect] After Check");
#endif
		try
		{
			PlayerPenaltyManager.RemoveAllPenalties(player.Slot);

			if (_tagsDetected)
				Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");

			SilentPlayers.Remove(player.Slot);
			GodPlayers.Remove(player.Slot);

			var authorizedSteamId = player.AuthorizedSteamID;
			if (authorizedSteamId == null || !PermissionManager.AdminCache.TryGetValue(authorizedSteamId,
											  out var expirationTime)
										  || !(expirationTime <= DateTime.UtcNow.ToLocalTime())) return HookResult.Continue;

			AdminManager.ClearPlayerPermissions(authorizedSteamId);
			AdminManager.RemovePlayerAdminData(authorizedSteamId);

			return HookResult.Continue;
		}
		catch (Exception ex)
		{
			Logger.LogError($"An error occurred in OnClientDisconnect: {ex.Message}");
			return HookResult.Continue;
		}
	}

	[GameEventHandler]
	public HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player == null || string.IsNullOrEmpty(player.IpAddress) || player.IpAddress.Contains("127.0.0.1")
			|| player.IsBot || !player.UserId.HasValue)
			return HookResult.Continue;

		var ipAddress = player.IpAddress.Split(":")[0];

		// Check if the player's IP or SteamID is in the bannedPlayers list
		if (Config.BanType > 0 && BannedPlayers.Contains(ipAddress) || BannedPlayers.Contains(player.SteamID.ToString()))
		{
			// Kick the player if banned
			if (player.UserId.HasValue)
				Helper.KickPlayer(player.UserId.Value, "Banned");

			return HookResult.Continue;
		}

		if (_database == null) return HookResult.Continue;

		var playerInfo = new PlayerInfo
		{
			UserId = player.UserId.Value,
			Slot = player.Slot,
			SteamId = player.SteamID.ToString(),
			Name = player.PlayerName,
			IpAddress = ipAddress
		};

		// Perform asynchronous database operations within a single method
		Task.Run(async () =>
		{
			// Initialize managers
			BanManager banManager = new(_database, Config);
			MuteManager muteManager = new(_database);

			try
			{
				// Check if the player is banned
				bool isBanned = await banManager.IsPlayerBanned(playerInfo);
				if (isBanned)
				{
					// Add player's IP and SteamID to bannedPlayers list if not already present
					if (Config.BanType > 0 && playerInfo.IpAddress != null &&
						!BannedPlayers.Contains(playerInfo.IpAddress))
					{
						BannedPlayers.Add(playerInfo.IpAddress);
					}

					if (playerInfo.SteamId != null && !BannedPlayers.Contains(playerInfo.SteamId))
					{
						BannedPlayers.Add(playerInfo.SteamId);
					}

					// Kick the player if banned
					await Server.NextFrameAsync(() =>
					{
						var victim = Utilities.GetPlayerFromUserid(playerInfo.UserId);

						if (victim?.UserId != null)
						{
							if (UnlockedCommands)
								Server.ExecuteCommand($"banid 2 {playerInfo.UserId}");

							Helper.KickPlayer(victim.UserId.Value, "Banned");
						}
					});

					return;
				}

				// Check if the player is muted
				var activeMutes = await muteManager.IsPlayerMuted(playerInfo.SteamId);
				if (activeMutes.Count > 0)
				{
					foreach (var mute in activeMutes)
					{
						string muteType = mute.type;
						DateTime ends = mute.ends;
						int duration = mute.duration;
						switch (muteType)
						{
							// Apply mute penalty based on mute type
							case "GAG":
								PlayerPenaltyManager.AddPenalty(playerInfo.Slot, PenaltyType.Gag, ends, duration);
								await Server.NextFrameAsync(() =>
								{
									if (_tagsDetected)
									{
										Server.ExecuteCommand($"css_tag_mute {playerInfo.SteamId}");
									}
								});
								break;
							case "MUTE":
								PlayerPenaltyManager.AddPenalty(playerInfo.Slot, PenaltyType.Mute, ends, duration);
								await Server.NextFrameAsync(() =>
								{
									player.VoiceFlags = VoiceFlags.Muted;
								});
								break;
							default:
								PlayerPenaltyManager.AddPenalty(playerInfo.Slot, PenaltyType.Silence, ends, duration);
								await Server.NextFrameAsync(() =>
								{
									player.VoiceFlags = VoiceFlags.Muted;
									if (_tagsDetected)
									{
										Server.ExecuteCommand($"css_tag_mute {playerInfo.SteamId}");
									}
								});
								break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error processing player connection: {ex}");
			}
		});

		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnRoundEnd(EventRoundStart @event, GameEventInfo info)
	{
#if DEBUG
		Logger.LogCritical("[OnRoundEnd]");
#endif

		GodPlayers.Clear();
		return HookResult.Continue;
	}

	public HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid || player.IsBot)
			return HookResult.Continue;

		if (info.GetArg(1).StartsWith($"/")
			|| info.GetArg(1).StartsWith($"!"))
			return HookResult.Continue;

		if (info.GetArg(1).Length == 0)
			return HookResult.Handled;

		if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) || PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
			return HookResult.Handled;

		return HookResult.Continue;
	}

	public HookResult OnCommandTeamSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid || player.IsBot)
			return HookResult.Continue;

		if (info.GetArg(1).StartsWith($"/")
			|| info.GetArg(1).StartsWith($"!"))
			return HookResult.Continue;

		if (info.GetArg(1).Length == 0)
			return HookResult.Handled;

		if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) || PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
			return HookResult.Handled;

		if (!info.GetArg(1).StartsWith($"@")) return HookResult.Continue;

		StringBuilder sb = new();

		if (AdminManager.PlayerHasPermissions(player, "@css/chat"))
		{
			sb.Append(_localizer!["sa_adminchat_template_admin", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
			foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && p is { IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(p, "@css/chat")))
			{
				p.PrintToChat(sb.ToString());
			}
		}
		else
		{
			sb.Append(_localizer!["sa_adminchat_template_player", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
			player.PrintToChat(sb.ToString());
			foreach (var p in Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(p, "@css/chat")))
			{
				p.PrintToChat(sb.ToString());
			}
		}

		return HookResult.Handled;
	}

	public void OnMapStart(string mapName)
	{
		if (Config.ReloadAdminsEveryMapChange)
			AddTimer(3.0f, () => ReloadAdmins(null));

		var path = Path.GetDirectoryName(ModuleDirectory);
		if (Directory.Exists(path + "/CS2-Tags"))
		{
			_tagsDetected = true;
		}

		GodPlayers.Clear();
		SilentPlayers.Clear();

		PlayerPenaltyManager.RemoveAllPenalties();

		_database = new Database.Database(_dbConnectionString);

		AddTimer(2f, () =>
		{
			if (ServerId != null) return;

			var ipAddress = ConVar.Find("ip")?.StringValue;

			if (string.IsNullOrEmpty(ipAddress) || ipAddress.StartsWith("0.0.0"))
			{
				return;
				ipAddress = Helper.GetServerIp();
			}

			var address = $"{ipAddress}:{ConVar.Find("hostport")?.GetPrimitiveValue<int>()}";
			var hostname = ConVar.Find("hostname")!.StringValue;

			Task.Run(async () =>
			{
				PermissionManager adminManager = new(_database);

				try
				{
					await using var connection = await _database.GetConnectionAsync();
					var addressExists = await connection.ExecuteScalarAsync<bool>(
						"SELECT COUNT(*) FROM sa_servers WHERE address = @address",
						new { address });

					if (!addressExists)
					{
						await connection.ExecuteAsync(
							"INSERT INTO sa_servers (address, hostname) VALUES (@address, @hostname)",
							new { address, hostname });
					}
					else
					{
						await connection.ExecuteAsync(
							"UPDATE `sa_servers` SET `hostname` = @hostname, `id` = `id` WHERE `address` = @address",
							new { address, hostname });
					}

					int? serverId = await connection.ExecuteScalarAsync<int>(
						"SELECT `id` FROM `sa_servers` WHERE `address` = @address",
						new { address });

					ServerId = serverId;
				}
				catch (Exception ex)
				{
					_logger?.LogCritical("Unable to create or get server_id" + ex.Message);
				}

				if (Config.EnableMetrics)
				{
					var queryString = $"?address={address}&hostname={hostname}";
					using HttpClient client = new();

					try
					{
						await client.GetAsync($"https://api.daffyy.love/index.php{queryString}");
					}
					catch (HttpRequestException ex)
					{
						Logger.LogWarning($"Unable to make metrics call: {ex.Message}");
					}
				}
			});
		});

		AddTimer(61.0f, () =>
		{
#if DEBUG
			Logger.LogCritical("[OnMapStart] Expired check");
#endif

			var players = Helper.GetValidPlayers();
			var onlinePlayers = players
				.Where(player => player.IpAddress != null)
				.Select(player => (player.IpAddress, player.SteamID, player.UserId, player.Slot))
				.ToList();

			Task.Run(async () =>
			{
				PermissionManager adminManager = new(_database);
				BanManager banManager = new(_database, Config);
				MuteManager muteManager = new(_database);

				await banManager.ExpireOldBans();
				await adminManager.DeleteOldAdmins();

				BannedPlayers.Clear();

				if (onlinePlayers.Count > 0)
				{
					try
					{
						await banManager.CheckOnlinePlayers(onlinePlayers);
						if (Config.TimeMode == 0)
						{
							await muteManager.CheckOnlineModeMutes(onlinePlayers);
						}
					}
					catch { }
				}

				await muteManager.ExpireOldMutes();

				await Server.NextFrameAsync(() =>
				{
					if (onlinePlayers.Count > 0)
					{
						try
						{
							foreach (var player in players.Where(player => PlayerPenaltyManager.IsSlotInPenalties(player.Slot)))
							{
								if (!PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Mute) && !PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
									player.VoiceFlags = VoiceFlags.Normal;

								if (!PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) && !PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
								{
									if (_tagsDetected)
										Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");
								}

								if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence) ||
									PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Mute) ||
									PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag)) continue;
								player.VoiceFlags = VoiceFlags.Normal;

								if (_tagsDetected)
									Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");
							}

							PlayerPenaltyManager.RemoveExpiredPenalties();
						}
						catch { }
					}

				});
			});
		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
	}

	[GameEventHandler]
	public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player is null || @event.Attacker is null || !player.PawnIsAlive || player.PlayerPawn.Value == null)
			return HookResult.Continue;

		if (!GodPlayers.Contains(player.Slot)) return HookResult.Continue;

		player.PlayerPawn.Value.Health = player.PlayerPawn.Value.MaxHealth;
		player.PlayerPawn.Value.ArmorValue = 100;

		return HookResult.Continue;
	}
}