using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using MySqlConnector;
using System.Data;
using System.Xml.Linq;

namespace CS2_SimpleAdmin
{
	public class MuteManager
	{
		private readonly IDbConnection _dbConnection;

		public MuteManager(string connectionString)
		{
			_dbConnection = new MySqlConnection(connectionString);
		}

		public void MutePlayer(CCSPlayerController? player, CCSPlayerController? issuer, string reason, int time = 0, int type = 0)
		{
			_dbConnection.Open();

			if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			string muteType = "GAG";

			if (type == 1)
				muteType = "MUTE";

			var sql = "INSERT INTO `sa_mutes` (`player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`) " +
				"VALUES (@playerSteamid, @playerName, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @type)";

			_dbConnection.Execute(sql, new
			{
				playerSteamid = player.AuthorizedSteamID.SteamId64.ToString(),
				playerName = player.PlayerName,
				adminSteamid = issuer == null ? "Console" : issuer?.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType,
			});

			_dbConnection.Close();
		}

		public void AddMuteBySteamid(string playerSteamId, CCSPlayerController? issuer, string reason, int time = 0, int type = 0)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			_dbConnection.Open();

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			string muteType = "GAG";

			if (type == 1)
				muteType = "MUTE";

			var sql = "INSERT INTO `sa_mutes` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`) " +
				"VALUES (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @type)";

			_dbConnection.Execute(sql, new
			{
				playerSteamid = playerSteamId,
				adminSteamid = issuer == null ? "Console" : issuer?.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType
			});

			_dbConnection.Close();
		}

		public List<dynamic> IsPlayerMuted(string steamId)
		{
			_dbConnection.Open();

			DateTime now = DateTime.Now;

			string sql = "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)";
			List<dynamic> activeMutes = _dbConnection.Query<dynamic>(sql, new { PlayerSteamID = steamId, CurrentTime = now }).ToList();

			_dbConnection.Close();

			return activeMutes;
		}

		public void UnmutePlayer(string playerPattern, int type = 0)
		{
			if (playerPattern == null || playerPattern.Length <= 1)
			{
				return;
			}

			if (type == 2)
			{
				string _unbanSql = "UPDATE sa_mutes SET status = 'UNMUTED' WHERE (player_steamid = @pattern OR player_name = @pattern) AND status = 'ACTIVE'";
				_dbConnection.Execute(_unbanSql, new { pattern = playerPattern });
				_dbConnection.Close();

				return;
			}

			string muteType = "GAG";

			if (type == 1)
				muteType = "MUTE";

			_dbConnection.Open();

			string sqlUnban = "UPDATE sa_mutes SET status = 'UNMUTED' WHERE (player_steamid = @pattern OR player_name = @pattern) AND type = @muteType AND status = 'ACTIVE'";
			_dbConnection.Execute(sqlUnban, new { pattern = playerPattern, muteType });
			_dbConnection.Close();
		}

		public void ExpireOldMutes()
		{
			_dbConnection.Open();
			string sql = "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
			_dbConnection.Execute(sql, new { CurrentTime = DateTime.Now });
			//int affectedRows = _dbConnection.Execute(sql, new { CurrentTime = DateTime.Now });

			_dbConnection.Close();
		}

	}
}
