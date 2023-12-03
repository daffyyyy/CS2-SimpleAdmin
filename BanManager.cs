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
		private readonly IDbConnection _dbConnection;

		public BanManager(string connectionString)
		{
			_dbConnection = new MySqlConnection(connectionString);
		}

		public void BanPlayer(CCSPlayerController? player, CCSPlayerController? issuer, string reason, int time = 0)
		{
			_dbConnection.Open();

			if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			var sql = "INSERT INTO `sa_bans` (`player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`) " +
				"VALUES (@playerSteamid, @playerName, @adminSteamid, @adminName, @banReason, @duration, @ends, @created)";
			_dbConnection.Execute(sql, new
			{
				playerSteamid = player.AuthorizedSteamID.SteamId64.ToString(),
				playerName = player.PlayerName,
				adminSteamid = issuer == null ? "Console" : issuer?.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now
			});

			_dbConnection.Close();
		}

		public void AddBanBySteamid(string playerSteamId, CCSPlayerController? issuer, string reason, int time = 0)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			_dbConnection.Open();

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			var sql = "INSERT INTO `sa_bans` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`) " +
				"VALUES (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created)";

			_dbConnection.Execute(sql, new
			{
				playerSteamid = playerSteamId,
				adminSteamid = issuer == null ? "Console" : issuer?.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now
			});

			_dbConnection.Close();
		}

		public bool IsPlayerBanned(string steamId)
		{
			_dbConnection.Open();

			DateTime now = DateTime.Now;

			string sql = "SELECT COUNT(*) FROM sa_bans WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)";
			int banCount = _dbConnection.ExecuteScalar<int>(sql, new { PlayerSteamID = steamId, CurrentTime = now });

			_dbConnection.Close();

			return banCount > 0;
		}

		public void UnbanPlayer(string playerPattern)
		{
			if (playerPattern == null || playerPattern.Length <= 1)
			{
				return;
			}

			_dbConnection.Open();

			string sqlUnban = "UPDATE sa_bans SET status = 'UNBANNED' WHERE player_steamid = @pattern OR player_name = @pattern AND status = 'ACTIVE'";
			_dbConnection.Execute(sqlUnban, new { pattern = playerPattern });

			_dbConnection.Close();
		}

		public void ExpireOldBans()
		{
			_dbConnection.Open();

			string sql = "UPDATE sa_bans SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
			_dbConnection.Execute(sql, new { CurrentTime = DateTime.Now });
			//int affectedRows = _dbConnection.Execute(sql, new { CurrentTime = DateTime.Now });

			_dbConnection.Close();
		}

	}
}
