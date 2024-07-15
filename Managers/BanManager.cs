using CounterStrikeSharp.API;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Text;

namespace CS2_SimpleAdmin;

internal class BanManager(Database.Database database, CS2_SimpleAdminConfig config)
{
	public async Task BanPlayer(PlayerInfo player, PlayerInfo issuer, string reason, int time = 0)
	{
		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime futureTime = now.AddMinutes(time);

		await using MySqlConnection connection = await database.GetConnectionAsync();
		try
		{
			const string sql =
				"INSERT INTO `sa_bans` (`player_steamid`, `player_name`, `player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
							   "VALUES (@playerSteamid, @playerName, @playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = player.SteamId,
				playerName = player.Name,
				playerIp = config.BanType == 1 ? player.IpAddress : null,
				adminSteamid = issuer.SteamId ?? "Console",
				adminName = issuer.Name ?? "Console",
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				serverid = CS2_SimpleAdmin.ServerId
			});
		}
		catch { }
	}

	public async Task AddBanBySteamid(string playerSteamId, PlayerInfo issuer, string reason, int time = 0)
	{
		if (string.IsNullOrEmpty(playerSteamId)) return;

		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime futureTime = now.AddMinutes(time);

		try
		{
			await using MySqlConnection connection = await database.GetConnectionAsync();

			var sql = "INSERT INTO `sa_bans` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
				"VALUES (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = playerSteamId,
				adminSteamid = issuer.SteamId ?? "Console",
				adminName = issuer.Name ?? "Console",
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				serverid = CS2_SimpleAdmin.ServerId
			});
		}
		catch { }
	}

	public async Task AddBanByIp(string playerIp, PlayerInfo issuer, string reason, int time = 0)
	{
		if (string.IsNullOrEmpty(playerIp)) return;

		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime futureTime = now.AddMinutes(time);

		try
		{
			await using MySqlConnection connection = await database.GetConnectionAsync();

			var sql = "INSERT INTO `sa_bans` (`player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
				"VALUES (@playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerIp,
				adminSteamid = issuer.SteamId ?? "Console",
				adminName = issuer.Name ?? "Console",
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				serverid = CS2_SimpleAdmin.ServerId
			});
		}
		catch { }
	}

	public async Task<bool> IsPlayerBanned(PlayerInfo player)
	{
		if (player.SteamId == null || player.IpAddress == null)
		{
			return false;
		}

#if DEBUG
		if (CS2_SimpleAdmin._logger!= null)
			CS2_SimpleAdmin._logger.LogCritical($"IsPlayerBanned for {player.Name}");
#endif

		int banCount;

		DateTime currentTime = DateTime.UtcNow.ToLocalTime();

		try
		{
			var sql = config.MultiServerMode ? """
			                                   					UPDATE sa_bans
			                                   					SET player_ip = CASE WHEN player_ip IS NULL THEN @PlayerIP ELSE player_ip END,
			                                   						player_name = CASE WHEN player_name IS NULL THEN @PlayerName ELSE player_name END
			                                   					WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
			                                   					AND status = 'ACTIVE'
			                                   					AND (duration = 0 OR ends > @CurrentTime);
			                                   
			                                   					SELECT COUNT(*) FROM sa_bans
			                                   					WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
			                                   					AND status = 'ACTIVE'
			                                   					AND (duration = 0 OR ends > @CurrentTime);
			                                   """ : @"
					UPDATE sa_bans
					SET player_ip = CASE WHEN player_ip IS NULL THEN @PlayerIP ELSE player_ip END,
						player_name = CASE WHEN player_name IS NULL THEN @PlayerName ELSE player_name END
					WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
					AND status = 'ACTIVE'
					AND (duration = 0 OR ends > @CurrentTime) AND server_id = @serverid;

					SELECT COUNT(*) FROM sa_bans
					WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
					AND status = 'ACTIVE'
					AND (duration = 0 OR ends > @CurrentTime) AND server_id = @serverid;";

			await using var connection = await database.GetConnectionAsync();

			var parameters = new
			{
				PlayerSteamID = player.SteamId,
				PlayerIP = config.BanType == 0 || string.IsNullOrEmpty(player.IpAddress) ? null : player.IpAddress,
				PlayerName = !string.IsNullOrEmpty(player.Name) ? player.Name : string.Empty,
				CurrentTime = currentTime,
				serverid = CS2_SimpleAdmin.ServerId
			};

			banCount = await connection.ExecuteScalarAsync<int>(sql, parameters);

			if (config.BanType == 1 && banCount == 0)
			{
				sql = """
				      SELECT
				      							COUNT(*)
				      						FROM
				      							sa_bans
				      						JOIN sa_players_ips ON sa_bans.player_steamid = sa_players_ips.steamid
				      						WHERE
				      							sa_bans.status = 'ACTIVE'
				      							AND sa_players_ips.address = @PlayerIP;
				      """;

				banCount = await connection.ExecuteScalarAsync<int>(sql, new
				{
					PlayerIP = player.IpAddress
				});
			}
		}
		catch (Exception)
		{
			return false;
		}

		return banCount > 0;
	}

	public async Task<int> GetPlayerBans(PlayerInfo player)
	{
		try
		{
			var sql = "";

			sql = config.MultiServerMode
				? "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)"
				: "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) AND server_id = @serverid";

			int banCount;

			await using var connection = await database.GetConnectionAsync();

			if (config.BanType > 0 && !string.IsNullOrEmpty(player.IpAddress))
			{
				banCount = await connection.ExecuteScalarAsync<int>(sql,
					new
					{
						PlayerSteamID = player.SteamId,
						PlayerIP = player.IpAddress,
						serverid = CS2_SimpleAdmin.ServerId
					});
			}
			else
			{
				banCount = await connection.ExecuteScalarAsync<int>(sql,
					new
					{
						PlayerSteamID = player.SteamId,
						PlayerIP = DBNull.Value,
						serverid = CS2_SimpleAdmin.ServerId
					});
			}

			return banCount;
		}
		catch { }

		return 0;
	}

	public async Task UnbanPlayer(string playerPattern, string adminSteamId, string reason)
	{
		if (playerPattern is not { Length: > 1 })
		{
			return;
		}
		try
		{
			await using var connection = await database.GetConnectionAsync();

			var sqlRetrieveBans = config.MultiServerMode
				? "SELECT id FROM sa_bans WHERE (player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern) AND status = 'ACTIVE'"
				: "SELECT id FROM sa_bans WHERE (player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern) AND status = 'ACTIVE' AND server_id = @serverid";

			var bans = await connection.QueryAsync(sqlRetrieveBans, new { pattern = playerPattern, serverid = CS2_SimpleAdmin.ServerId });

			var bansList = bans as dynamic[] ?? bans.ToArray();
			if (bansList.Length == 0)
				return;

			const string sqlAdmin = "SELECT id FROM sa_admins WHERE player_steamid = @adminSteamId";
			var sqlInsertUnban = "INSERT INTO sa_unbans (ban_id, admin_id, reason) VALUES (@banId, @adminId, @reason); SELECT LAST_INSERT_ID();";

			var sqlAdminId = await connection.ExecuteScalarAsync<int?>(sqlAdmin, new { adminSteamId });
			var adminId = sqlAdminId ?? 0;

			foreach (var ban in bansList)
			{
				int banId = ban.id;
				int? unbanId;

				// Insert into sa_unbans
				if (reason != null)
				{
					unbanId = await connection.ExecuteScalarAsync<int>(sqlInsertUnban, new { banId, adminId, reason });
				}
				else
				{
					sqlInsertUnban = "INSERT INTO sa_unbans (ban_id, admin_id) VALUES (@banId, @adminId); SELECT LAST_INSERT_ID();";
					unbanId = await connection.ExecuteScalarAsync<int>(sqlInsertUnban, new { banId, adminId });
				}

				// Update sa_bans to set unban_id
				const string sqlUpdateBan = "UPDATE sa_bans SET status = 'UNBANNED', unban_id = @unbanId WHERE id = @banId";
				await connection.ExecuteAsync(sqlUpdateBan, new { unbanId, banId });
			}

			/*
			string sqlUnban = "UPDATE sa_bans SET status = 'UNBANNED' WHERE player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern AND status = 'ACTIVE'";
			await connection.ExecuteAsync(sqlUnban, new { pattern = playerPattern });
			*/

		}
		catch { }
	}

	/*
	public async Task CheckOnlinePlayers(List<(string? IpAddress, ulong SteamID, int? UserId, int Slot)> players)
	{
		try
		{
			await using var connection = await database.GetConnectionAsync();
			bool checkIpBans = config.BanType > 0;

			var sql = config.MultiServerMode
				? "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) AND status = 'ACTIVE'"
				: "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) AND status = 'ACTIVE' AND server_id = @serverid";

			foreach (var (IpAddress, SteamID, UserId, Slot) in players)
			{
				if (!UserId.HasValue) continue;

				var banCount = 0;
				if (checkIpBans && !string.IsNullOrEmpty(IpAddress))
				{
					banCount = await connection.ExecuteScalarAsync<int>(sql,
						new { PlayerSteamID = SteamID, PlayerIP = IpAddress, serverid = CS2_SimpleAdmin.ServerId });
				}
				else
				{
					banCount = await connection.ExecuteScalarAsync<int>(sql,
						new { PlayerSteamID = SteamID, PlayerIP = DBNull.Value, serverid = CS2_SimpleAdmin.ServerId });
				}

				if (banCount > 0)
				{
					await Server.NextFrameAsync(() =>
					{
						Helper.KickPlayer(UserId.Value, "Banned");
					});
				}
			}
		}
		catch { }
	}
	*/

	public async Task CheckOnlinePlayers(List<(string? IpAddress, ulong SteamID, int? UserId, int Slot)> players)
	{
		try
		{
			await using var connection = await database.GetConnectionAsync();
			bool checkIpBans = config.BanType > 0;

			var filteredPlayers = players.Where(p => p.UserId.HasValue).ToList();

			var steamIds = filteredPlayers.Select(p => p.SteamID).Distinct().ToList();
			var ipAddresses = filteredPlayers
				.Where(p => !string.IsNullOrEmpty(p.IpAddress))
				.Select(p => p.IpAddress)
				.Distinct()
				.ToList();

			var sql = new StringBuilder();
			sql.Append("SELECT `player_steamid`, `player_ip` FROM `sa_bans` WHERE `status` = 'ACTIVE' ");

			if (config.MultiServerMode)
			{
				sql.Append("AND (player_steamid IN @SteamIDs ");
				if (checkIpBans && ipAddresses.Count != 0)
				{
					sql.Append("OR player_ip IN @IpAddresses");
				}
				sql.Append(')');
			}
			else
			{
				sql.Append("AND server_id = @ServerId AND (player_steamid IN @SteamIDs ");
				if (checkIpBans && ipAddresses.Count != 0)
				{
					sql.Append("OR player_ip IN @IpAddresses");
				}
				sql.Append(')');
			}

			var bannedPlayers = await connection.QueryAsync<(ulong PlayerSteamID, string PlayerIP)>(
				sql.ToString(),
				new
				{
					SteamIDs = steamIds,
					IpAddresses = checkIpBans ? ipAddresses : [],
					ServerId = CS2_SimpleAdmin.ServerId
				});

			var valueTuples = bannedPlayers.ToList();
			var bannedSteamIds = valueTuples.Select(b => b.PlayerSteamID).ToHashSet();
			var bannedIps = valueTuples.Select(b => b.PlayerIP).ToHashSet();

			foreach (var player in filteredPlayers.Where(player => bannedSteamIds.Contains(player.SteamID) ||
																   (checkIpBans && bannedIps.Contains(player.IpAddress ?? ""))))
			{
				if (!player.UserId.HasValue) continue;

				await Server.NextFrameAsync(() =>
				{
					Helper.KickPlayer(player.UserId.Value, "Banned");
				});
			}
		}
		catch (Exception ex)
		{
			CS2_SimpleAdmin._logger?.LogError($"Error checking online players: {ex.Message}");
		}
	}

	public async Task ExpireOldBans()
	{
		var currentTime = DateTime.UtcNow.ToLocalTime();

		try
		{
			await using var connection = await database.GetConnectionAsync();
			/*
			string sql = "";
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			sql = "UPDATE sa_bans SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.UtcNow.ToLocalTime() });
			*/

			var sql = "";

			sql = config.MultiServerMode ? """
			                                
			                                				UPDATE sa_bans
			                                				SET
			                                					status = 'EXPIRED'
			                                				WHERE
			                                					status = 'ACTIVE'
			                                					AND
			                                					`duration` > 0
			                                					AND
			                                					ends <= @currentTime
			                                """ : """
			                                      
			                                      				UPDATE sa_bans
			                                      				SET
			                                      					status = 'EXPIRED'
			                                      				WHERE
			                                      					status = 'ACTIVE'
			                                      					AND
			                                      					`duration` > 0
			                                      					AND
			                                      					ends <= @currentTime
			                                      					AND server_id = @serverid
			                                      """;

			await connection.ExecuteAsync(sql, new { currentTime, serverid = CS2_SimpleAdmin.ServerId });

			if (config.ExpireOldIpBans > 0)
			{
				var ipBansTime = currentTime.AddDays(-config.ExpireOldIpBans);
				sql = config.MultiServerMode ? """
				                                
				                                				UPDATE sa_bans
				                                				SET
				                                					player_ip = NULL
				                                				WHERE
				                                					status = 'ACTIVE'
				                                					AND
				                                					ends <= @ipBansTime
				                                """ : """
				                                      
				                                      				UPDATE sa_bans
				                                      				SET
				                                      					player_ip = NULL
				                                      				WHERE
				                                      					status = 'ACTIVE'
				                                      					AND
				                                      					ends <= @ipBansTime
				                                      					AND server_id = @serverid
				                                      """;

				await connection.ExecuteAsync(sql, new { ipBansTime, CS2_SimpleAdmin.ServerId });
			}
		}
		catch (Exception)
		{
			CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired bans");
		}
	}
}