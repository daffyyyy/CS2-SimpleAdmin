using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using MySqlConnector;
using System.Data;
using System.Xml.Linq;

namespace CS2_SimpleAdmin
{
	internal class BanManager
	{
		private readonly MySqlConnection _dbConnection;

		public BanManager(string connectionString)
		{
			_dbConnection = new MySqlConnection(connectionString);
		}
		public async Task BanPlayer(CCSPlayerController? player, CCSPlayerController? issuer, string reason, int time = 0)
		{
			if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			var sql = "INSERT INTO `sa_bans` (`player_steamid`, `player_name`, `player_ip`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`) " +
				"VALUES (@playerSteamid, @playerName, @playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = player.AuthorizedSteamID.SteamId64.ToString(),
				playerName = player.PlayerName,
				playerIp = player.IpAddress!.Split(":")[0],
				adminSteamid = issuer == null ? "Console" : issuer.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now
			});
		}

		public async Task AddBanBySteamid(string playerSteamId, CCSPlayerController? issuer, string reason, int time = 0)
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
				adminSteamid = issuer == null ? "Console" : issuer.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now
			});
		}

		public async Task AddBanByIp(string playerIp, CCSPlayerController? issuer, string reason, int time = 0)
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
				adminSteamid = issuer == null ? "Console" : issuer?.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now
			});
		}

		public async Task<bool> IsPlayerBanned(string steamId, string? ipAddress = null)
		{
			DateTime now = DateTime.Now;

			string sql = "SELECT COUNT(*) FROM sa_bans WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP) AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)";

			int banCount;

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			if (!string.IsNullOrEmpty(ipAddress))
			{
				banCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = steamId, PlayerIP = ipAddress, CurrentTime = now });
			}
			else
			{
				banCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = steamId, PlayerIP = DBNull.Value, CurrentTime = now });
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

		public async Task CheckBan(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

			string steamId = player.AuthorizedSteamID.SteamId64.ToString();
			string? ipAddress = player.IpAddress?.Split(":")[0];

			bool isBanned = false;

			if (ipAddress != null)
			{
				isBanned = await IsPlayerBanned(steamId, ipAddress);
			}
			else
			{
				isBanned = await IsPlayerBanned(steamId);
			}

			if (isBanned)
			{
				Helper.KickPlayer(player.UserId, "Banned");
			}
		}

	}
}
