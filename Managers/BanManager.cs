﻿using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin;

internal class BanManager
{
	private readonly Database _database;
	private readonly CS2_SimpleAdminConfig _config;

	public BanManager(Database database, CS2_SimpleAdminConfig config)
	{
		_database = database;
		_config = config;
	}

	public async Task BanPlayer(PlayerInfo player, PlayerInfo issuer, string reason, int time = 0)
	{
		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime futureTime = now.AddMinutes(time).ToLocalTime();

		await using var connection = await _database.GetConnectionAsync();

		var sql = "INSERT INTO `sa_bans` (`player_steamid`, `player_name`, `player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
			"VALUES (@playerSteamid, @playerName, @playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

		await connection.ExecuteAsync(sql, new
		{
			playerSteamid = player.SteamId,
			playerName = player.Name,
			playerIp = _config.BanType == 1 ? player.IpAddress : null,
			adminSteamid = issuer.SteamId == null ? "Console" : issuer.SteamId,
			adminName = issuer.Name == null ? "Console" : issuer.Name,
			banReason = reason,
			duration = time,
			ends = futureTime,
			created = now,
			serverid = CS2_SimpleAdmin.ServerId
		});
	}

	public async Task AddBanBySteamid(string playerSteamId, PlayerInfo issuer, string reason, int time = 0)
	{
		if (string.IsNullOrEmpty(playerSteamId)) return;

		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime futureTime = now.AddMinutes(time).ToLocalTime();

		await using var connection = await _database.GetConnectionAsync();

		var sql = "INSERT INTO `sa_bans` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
			"VALUES (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

		await connection.ExecuteAsync(sql, new
		{
			playerSteamid = playerSteamId,
			adminSteamid = issuer.SteamId == null ? "Console" : issuer.SteamId,
			adminName = issuer.Name == null ? "Console" : issuer.Name,
			banReason = reason,
			duration = time,
			ends = futureTime,
			created = now,
			serverid = CS2_SimpleAdmin.ServerId
		});
	}

	public async Task AddBanByIp(string playerIp, PlayerInfo issuer, string reason, int time = 0)
	{
		if (string.IsNullOrEmpty(playerIp)) return;

		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime futureTime = now.AddMinutes(time).ToLocalTime();

		await using var connection = await _database.GetConnectionAsync();

		var sql = "INSERT INTO `sa_bans` (`player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
			"VALUES (@playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid)";

		await connection.ExecuteAsync(sql, new
		{
			playerIp,
			adminSteamid = issuer.SteamId == null ? "Console" : issuer.SteamId,
			adminName = issuer.Name == null ? "Console" : issuer.Name,
			banReason = reason,
			duration = time,
			ends = futureTime,
			created = now,
			serverid = CS2_SimpleAdmin.ServerId
		});
	}

	public async Task<bool> IsPlayerBanned(PlayerInfo player)
	{
		if (player.SteamId == null && player.IpAddress == null)
		{
			return false;
		}

#if DEBUG
		if (CS2_SimpleAdmin._logger!= null)
			CS2_SimpleAdmin._logger.LogCritical($"IsPlayerBanned for {player.Name}");
#endif

		int banCount = 0;

		DateTime currentTime = DateTime.Now.ToLocalTime();

		try
		{
			string sql = @"
					UPDATE sa_bans
					SET player_ip = CASE WHEN player_ip IS NULL THEN @PlayerIP ELSE player_ip END,
						player_name = CASE WHEN player_name IS NULL THEN @PlayerName ELSE player_name END
					WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
					AND status = 'ACTIVE'
					AND (duration = 0 OR ends > @CurrentTime);

					SELECT COUNT(*) FROM sa_bans
					WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
					AND status = 'ACTIVE'
					AND (duration = 0 OR ends > @CurrentTime);";

			await using var connection = await _database.GetConnectionAsync();

			var parameters = new
			{
				PlayerSteamID = player.SteamId,
				PlayerIP = _config.BanType == 0 || string.IsNullOrEmpty(player.IpAddress) ? null : player.IpAddress,
				PlayerName = !string.IsNullOrEmpty(player.Name) ? player.Name : string.Empty,
				CurrentTime = currentTime
			};

			banCount = await connection.ExecuteScalarAsync<int>(sql, parameters);
		}
		catch (Exception)
		{
			return false;
		}

		return banCount > 0;
	}

	public async Task<int> GetPlayerBans(PlayerInfo player)
	{
		string sql = "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)";
		int banCount;

		await using var connection = await _database.GetConnectionAsync();

		if (!string.IsNullOrEmpty(player.IpAddress))
		{
			banCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = player.SteamId, PlayerIP = player.IpAddress });
		}
		else
		{
			banCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = player.SteamId, PlayerIP = DBNull.Value });
		}

		return banCount;
	}

	public async Task UnbanPlayer(string playerPattern)
	{
		if (playerPattern == null || playerPattern.Length <= 1)
		{
			return;
		}

		await using var connection = await _database.GetConnectionAsync();

		string sqlUnban = "UPDATE sa_bans SET status = 'UNBANNED' WHERE player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern AND status = 'ACTIVE'";
		await connection.ExecuteAsync(sqlUnban, new { pattern = playerPattern });
	}

	public async Task ExpireOldBans()
	{
		try
		{
			DateTime currentTime = DateTime.UtcNow.ToLocalTime();

			await using var connection = await _database.GetConnectionAsync();

			/*
			string sql = "";
			await using var connection = await _database.GetConnectionAsync();

			sql = "UPDATE sa_bans SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.UtcNow.ToLocalTime() });
			*/

			string sql = @"
				UPDATE sa_bans
				SET
					status = 'EXPIRED'
				WHERE
					status = 'ACTIVE'
					AND
					`duration` > 0
					AND
					ends <= @currentTime";

			await connection.ExecuteAsync(sql, new { currentTime });

			if (_config.ExpireOldIpBans > 0)
			{
				DateTime ipBansTime = currentTime.AddDays(-_config.ExpireOldIpBans).ToLocalTime();

				sql = @"
				UPDATE sa_bans
				SET
					player_ip = NULL
				WHERE
					status = 'ACTIVE'
					AND
					ends <= @ipBansTime";

				await connection.ExecuteAsync(sql, new { ipBansTime });
			}
		}
		catch (Exception)
		{
			CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired bans");
		}
	}
}