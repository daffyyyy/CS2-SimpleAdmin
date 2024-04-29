using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
	private static readonly HashSet<int> LoadedPlayers = [];

	private void RegisterEvents()
	{
		RegisterListener<Listeners.OnMapStart>(OnMapStart);
		//RegisterListener<Listeners.OnClientConnected>(OnClientConnected);
		//RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
		AddCommandListener("say", OnCommandSay);
		AddCommandListener("say_team", OnCommandTeamSay);
	}

	[GameEventHandler]
	public HookResult OnClientDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

#if DEBUG
        Logger.LogCritical("[OnClientDisconnect] Before");
#endif

		if (player == null || !player.IsValid || string.IsNullOrEmpty(player.IpAddress) || player.IsBot || player.IsHLTV)
		{
			return HookResult.Continue;
		}

		if (!LoadedPlayers.Contains(player.Slot))
		{
			return HookResult.Continue;
		}

#if DEBUG
        Logger.LogCritical("[OnClientDisconnect] After Check");
#endif
		try
		{
			PlayerPenaltyManager.RemoveAllPenalties(player.Slot);

			if (TagsDetected)
			{
				Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");
			}

			if (silentPlayers.Contains(player.Slot))
			{
				RemoveFromConcurrentBag(silentPlayers, player.Slot);
			}

			if (godPlayers.Contains(player.Slot))
			{
				RemoveFromConcurrentBag(godPlayers, player.Slot);
			}

			SteamID? authorizedSteamId = player.AuthorizedSteamID;
			if (authorizedSteamId != null && AdminSQLManager.AdminCache.TryGetValue(authorizedSteamId, out var expirationTime)
				&& expirationTime <= DateTime.Now)
			{
				AdminManager.ClearPlayerPermissions(authorizedSteamId);
				AdminManager.RemovePlayerAdminData(authorizedSteamId);
			}

			LoadedPlayers.Remove(player.Slot);

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
			|| player.IsBot || player.IsHLTV || !player.UserId.HasValue)
			return HookResult.Continue;

		var ipAddress = player.IpAddress.Split(":")[0];

		// Check if the player's IP or SteamID is in the bannedPlayers list
		if (bannedPlayers.Contains(ipAddress) || bannedPlayers.Contains(player.SteamID.ToString()))
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
					if (playerInfo.IpAddress != null && !bannedPlayers.Contains(playerInfo.IpAddress))
						bannedPlayers.Add(playerInfo.IpAddress);

					if (playerInfo.SteamId != null && !bannedPlayers.Contains(playerInfo.SteamId))
						bannedPlayers.Add(playerInfo.SteamId);

					// Kick the player if banned
					await Server.NextFrameAsync(() =>
					{
						var victim = Utilities.GetPlayerFromUserid(playerInfo.UserId);
						if (victim.UserId.HasValue)
						{
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
									if (TagsDetected)
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
									if (TagsDetected)
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

		// Add player to loadedPlayers
		LoadedPlayers.Add(player.Slot);

		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnRoundEnd(EventRoundStart @event, GameEventInfo info)
	{
#if DEBUG
		Logger.LogCritical("[OnRoundEnd]");
#endif

		godPlayers.Clear();
		return HookResult.Continue;
	}

	public HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || info.GetArg(1).StartsWith($"/")
			 || info.GetArg(1).StartsWith($"!") && info.GetArg(1).Length >= 12)
			return HookResult.Continue;

		if (info.GetArg(1).Length == 0)
			return HookResult.Handled;

		if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) || PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
			return HookResult.Handled;

		return HookResult.Continue;
	}

	public HookResult OnCommandTeamSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || info.GetArg(1).StartsWith($"/")
			 || info.GetArg(1).StartsWith($"!") && info.GetArg(1).Length >= 12)
			return HookResult.Continue;

		if (info.GetArg(1).Length == 0)
			return HookResult.Handled;

		if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) || PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
			return HookResult.Handled;

		if (!info.GetArg(1).StartsWith($"@")) return HookResult.Continue;
		
		StringBuilder sb = new();

		if (AdminManager.PlayerHasPermissions(player, "@css/chat"))
		{
			sb.Append(_localizer!["sa_adminchat_template_admin", player!.PlayerName, info.GetArg(1).Remove(0, 1)]);
			foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && p is { IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(p, "@css/chat")))
			{
				p.PrintToChat(sb.ToString());
			}
		}
		else
		{
			sb.Append(_localizer!["sa_adminchat_template_player", player!.PlayerName, info.GetArg(1).Remove(0, 1)]);
			player.PrintToChat(sb.ToString());
			foreach (var p in Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(p, "@css/chat")))
			{
				p.PrintToChat(sb.ToString());
			}
		}

		return HookResult.Handled;

	}

	private void OnMapStart(string mapName)
	{
		var path = Path.GetDirectoryName(ModuleDirectory);
		if (Directory.Exists(path + "/CS2-Tags"))
		{
			TagsDetected = true;
		}

		godPlayers.Clear();
		silentPlayers.Clear();

		PlayerPenaltyManager.RemoveAllPenalties();

		_database = new Database(dbConnectionString);

		AddTimer(61.0f, () =>
		{
#if DEBUG
			Logger.LogCritical("[OnMapStart] Expired check");
#endif

			var players = Helper.GetValidPlayers();
			var onlinePlayers = players
				.Where(player => player.IpAddress != null && player.SteamID.ToString().Length == 17)
				.Select(player => (player.IpAddress, player.SteamID, player.UserId))
				.ToList();

			Task.Run(async () =>
			{
				AdminSQLManager adminManager = new(_database);
				BanManager banManager = new(_database, Config);
				MuteManager muteManager = new(_database);

				await banManager.ExpireOldBans();
				await muteManager.ExpireOldMutes();
				await adminManager.DeleteOldAdmins();

				try
				{
					await banManager.CheckOnlinePlayers(onlinePlayers);
				}
				catch { }

				bannedPlayers.Clear();

				await Server.NextFrameAsync(() =>
				{
					try
					{
						foreach (var player in players.Where(player => PlayerPenaltyManager.IsSlotInPenalties(player.Slot)))
						{
							if (!PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Mute) && !PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
								player.VoiceFlags = VoiceFlags.Normal;

							if (!PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) && !PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
							{
								if (TagsDetected)
									Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");
							}

							if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence) ||
							    PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Mute) ||
							    PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag)) continue;
							player.VoiceFlags = VoiceFlags.Normal;

							if (TagsDetected)
								Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");
						}

						PlayerPenaltyManager.RemoveExpiredPenalties();
					}
					catch { }
				});
			});
		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

		AddTimer(2.0f, () =>
		{
			var address = $"{ConVar.Find("ip")!.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";
			var hostname = ConVar.Find("hostname")!.StringValue;

			Task.Run(async () =>
			{
				AdminSQLManager adminManager = new(_database);
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
						var response = await client.GetAsync($"https://api.daffyy.love/index.php{queryString}");
					}
					catch (HttpRequestException ex)
					{
						Logger.LogWarning($"Unable to make metrics call: {ex.Message}");
					}
				}

				//await _adminManager.GiveAllGroupsFlags();
				//await _adminManager.GiveAllFlags();

				await adminManager.CrateGroupsJsonFile();
				await adminManager.CreateAdminsJsonFile();

				AdminManager.LoadAdminData(ModuleDirectory + "/data/admins.json");
				AdminManager.LoadAdminGroups(ModuleDirectory + "/data/groups.json");
			});
		}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
	}

	[GameEventHandler]
	public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController? player = @event.Userid;

		if (player is null || @event.Attacker is null || !player.PawnIsAlive || player.PlayerPawn.Value == null)
			return HookResult.Continue;

		if (!godPlayers.Contains(player.Slot)) return HookResult.Continue;
		
		player.PlayerPawn.Value.Health = player.PlayerPawn.Value.MaxHealth;
		player.PlayerPawn.Value.ArmorValue = 100;

		return HookResult.Continue;
	}
}