using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CS2_SimpleAdmin;

internal class MuteManager(Database database)
{
	private readonly Database _database = database;

	public async Task MutePlayer(PlayerInfo player, PlayerInfo issuer, string reason, int time = 0, int type = 0)
	{
		if (player == null || player.SteamId == null) return;


		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime futureTime = now.AddMinutes(time).ToLocalTime();

		string muteType = "GAG";
		if (type == 1)
			muteType = "MUTE";
		else if (type == 2)
			muteType = "SILENCE";

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();
			var sql = "INSERT INTO `sa_mutes` (`player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`, `server_id`) " +
				"VALUES (@playerSteamid, @playerName, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @type, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = player.SteamId,
				playerName = player.Name,
				adminSteamid = issuer.SteamId ?? "Console",
				adminName = issuer.SteamId == null ? "Console" : issuer.Name,
				muteReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType,
				serverid = CS2_SimpleAdmin.ServerId
			});
		}
		catch { };
	}

	public async Task AddMuteBySteamid(string playerSteamId, PlayerInfo issuer, string reason, int time = 0, int type = 0)
	{
		if (string.IsNullOrEmpty(playerSteamId)) return;


		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime futureTime = now.AddMinutes(time).ToLocalTime();

		string muteType = "GAG";
		if (type == 1)
			muteType = "MUTE";
		else if (type == 2)
			muteType = "SILENCE";

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();
			var sql = "INSERT INTO `sa_mutes` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`, `server_id`) " +
				"VALUES (@playerSteamid, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @type, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = playerSteamId,
				adminSteamid = issuer.SteamId ?? "Console",
				adminName = issuer.Name ?? "Console",
				muteReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType,
				serverid = CS2_SimpleAdmin.ServerId
			});
		}
		catch { };
	}

	public async Task<List<dynamic>> IsPlayerMuted(string steamId)
	{
		if (string.IsNullOrEmpty(steamId))
		{
			return [];
		}

#if DEBUG
		if (CS2_SimpleAdmin._logger!= null)
			CS2_SimpleAdmin._logger.LogCritical($"IsPlayerMuted for {steamId}");
#endif

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();
			DateTime currentTime = DateTime.Now.ToLocalTime();
			string sql = "";

			if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
			{
				sql = "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime) " +
					"AND server_id = @serverid";
			}
			else
			{
				sql = "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)";
			}

			var parameters = new { PlayerSteamID = steamId, CurrentTime = currentTime, serverid = CS2_SimpleAdmin.ServerId };
			var activeMutes = (await connection.QueryAsync(sql, parameters)).ToList();
			return activeMutes;
		}
		catch (Exception)
		{
			return [];
		}
	}

	public async Task<int> GetPlayerMutes(string steamId)
	{
		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			int muteCount;
			string sql = "";

			if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
			{
				sql = "SELECT COUNT(*) FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND server_id = @serverid";
			}
			else
			{
				sql = "SELECT COUNT(*) FROM sa_mutes WHERE player_steamid = @PlayerSteamID";
			}

			muteCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = steamId, serverid = CS2_SimpleAdmin.ServerId });
			return muteCount;
		}
		catch (Exception)
		{
			return 0;
		}
	}

	public async Task UnmutePlayer(string playerPattern, string adminSteamId, string reason, int type = 0)
	{
		if (playerPattern == null || playerPattern.Length <= 1)
		{
			return;
		}

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			string muteType = "GAG";
			if (type == 1)
			{
				muteType = "MUTE";
			}
			else if (type == 2)
				muteType = "SILENCE";

			string sqlRetrieveMutes = "";

			if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
			{
				sqlRetrieveMutes = "SELECT id FROM sa_mutes WHERE (player_steamid = @pattern OR player_name = @pattern) AND " +
					"type = @muteType AND status = 'ACTIVE' AND server_id = @serverid";
			}
			else
			{
				sqlRetrieveMutes = "SELECT id FROM sa_mutes WHERE (player_steamid = @pattern OR player_name = @pattern) AND " +
					"type = @muteType AND status = 'ACTIVE'";
			}

			var mutes = await connection.QueryAsync(sqlRetrieveMutes, new { pattern = playerPattern, muteType, serverid = CS2_SimpleAdmin.ServerId });

			if (!mutes.Any())
				return;

			string sqlAdmin = "SELECT id FROM sa_admins WHERE player_steamid = @adminSteamId";
			string sqlInsertUnmute = "INSERT INTO sa_unmutes (mute_id, admin_id, reason) VALUES (@muteId, @adminId, @reason); SELECT LAST_INSERT_ID();";

			int? sqlAdminId = await connection.ExecuteScalarAsync<int?>(sqlAdmin, new { adminSteamId });
			int adminId = sqlAdminId ?? 0;

			foreach (var mute in mutes)
			{
				int muteId = mute.id;
				int? unmuteId;

				// Insert into sa_unmutes
				if (reason != null)
				{
					unmuteId = await connection.ExecuteScalarAsync<int>(sqlInsertUnmute, new { muteId, adminId, reason });
				}
				else
				{
					sqlInsertUnmute = "INSERT INTO sa_unmutes (muteId, admin_id) VALUES (@muteId, @adminId); SELECT LAST_INSERT_ID();";
					unmuteId = await connection.ExecuteScalarAsync<int>(sqlInsertUnmute, new { muteId, adminId });
				}

				// Update sa_mutes to set unmute_id
				string sqlUpdateMute = "UPDATE sa_mutes SET status = 'UNMUTED', unmute_id = @unmuteId WHERE id = @muteId";
				await connection.ExecuteAsync(sqlUpdateMute, new { unmuteId, muteId });
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
	}

	public async Task ExpireOldMutes()
	{
		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			string sql = "";
			if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
			{
				sql = "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime AND server_id = @serverid";
			}
			else
			{
				sql = "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
			}

			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.Now.ToLocalTime(), serverid = CS2_SimpleAdmin.ServerId });
		}
		catch (Exception)
		{
			CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired mutes");
		}
	}
}