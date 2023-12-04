using Dapper;
using MySqlConnector;

namespace CS2_SimpleAdmin
{
	internal class BanManager
	{
		private readonly MySqlConnection _dbConnection;

		public BanManager(string connectionString)
		{
			_dbConnection = new MySqlConnection(connectionString);
		}
		public async Task BanPlayer(PlayerInfo player, PlayerInfo issuer, string reason, int time = 0)
		{
			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			var sql = "INSERT INTO `sa_bans` (`player_steamid`, `player_name`, `player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`) " +
				"VALUES (@playerSteamid, @playerName, @playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = player.SteamId,
				playerName = player.Name,
				playerIp = player.IpAddress,
				adminSteamid = issuer.SteamId == null ? "Console" : issuer.SteamId,
				adminName = issuer.Name == null ? "Console" : issuer.Name,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now
			});
		}

		public async Task AddBanBySteamid(string playerSteamId, PlayerInfo issuer, string reason, int time = 0)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			var sql = "INSERT INTO `sa_bans` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`) " +
				"VALUES (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = playerSteamId,
				adminSteamid = issuer.SteamId == null ? "Console" : issuer.SteamId,
				adminName = issuer.Name == null ? "Console" : issuer.Name,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now
			});
		}

		public async Task AddBanByIp(string playerIp, PlayerInfo issuer, string reason, int time = 0)
		{
			if (string.IsNullOrEmpty(playerIp)) return;

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			var sql = "INSERT INTO `sa_bans` (`player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`) " +
				"VALUES (@playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created)";

			await connection.ExecuteAsync(sql, new
			{
				playerIp,
				adminSteamid = issuer.SteamId == null ? "Console" : issuer.SteamId,
				adminName = issuer.Name == null ? "Console" : issuer.Name,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now
			});
		}

		public async Task<bool> IsPlayerBanned(PlayerInfo player)
		{
			DateTime now = DateTime.Now;

			string sql = "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)";

			int banCount;

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			if (!string.IsNullOrEmpty(player.IpAddress))
			{
				banCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = player.SteamId, PlayerIP = player.IpAddress, CurrentTime = now });
			}
			else
			{
				banCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = player.SteamId, PlayerIP = DBNull.Value, CurrentTime = now });
			}

			return banCount > 0;
		}

		public async Task UnbanPlayer(string playerPattern)
		{
			if (playerPattern == null || playerPattern.Length <= 1)
			{
				return;
			}

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sqlUnban = "UPDATE sa_bans SET status = 'UNBANNED' WHERE player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern AND status = 'ACTIVE'";
			await connection.ExecuteAsync(sqlUnban, new { pattern = playerPattern });
		}

		public async Task ExpireOldBans()
		{
			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sql = "UPDATE sa_bans SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.Now });
		}
	}
}
