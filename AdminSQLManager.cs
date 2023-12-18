using Dapper;
using MySqlConnector;

namespace CS2_SimpleAdmin
{
	internal class AdminSQLManager
	{
		private readonly MySqlConnection _dbConnection;
		public static readonly Dictionary<string, List<string>> _adminCache = new Dictionary<string, List<string>>();

		public AdminSQLManager(string connectionString)
		{
			_dbConnection = new MySqlConnection(connectionString);
		}

		public async Task<List<dynamic>> GetAdminFlags(string steamId)
		{
			if (_adminCache.ContainsKey(steamId))
			{
				return _adminCache[steamId].Select(flag => (dynamic)flag).ToList();
			}
			else
			{
				await using var connection = _dbConnection;
				await connection.OpenAsync();

				DateTime now = DateTime.Now;

				string sql = "SELECT flags FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime)";
				List<dynamic> activeFlags = (await connection.QueryAsync(sql, new { PlayerSteamID = steamId, CurrentTime = now })).ToList();

				_adminCache[steamId] = new List<string>();
				foreach (var flags in activeFlags)
				{
					if (flags == null) continue;
					string flagsValue = flags.flags.ToString();
					_adminCache[steamId].Add(flagsValue);
				}
			}
			return _adminCache[steamId].Select(flag => (dynamic)flag).ToList();
		}

		public async Task DeleteAdminBySteamId(string playerSteamId)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			if (_adminCache.ContainsKey(playerSteamId))
				_adminCache.Remove(playerSteamId);

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sql = "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID";
			await connection.ExecuteAsync(sql, new { PlayerSteamID = playerSteamId });
		}

		public async Task AddAdminBySteamId(string playerSteamId, string playerName, string flags, int immunity = 0, int time = 0)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			flags = flags.Replace(" ", "");

			DateTime now = DateTime.Now;
			DateTime? futureTime;
			if (time != 0)
				futureTime = now.AddMinutes(time);
			else
				futureTime = null;

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			var sql = "INSERT INTO `sa_admins` (`player_steamid`, `player_name`, `flags`, `immunity`, `ends`, `created`) " +
				"VALUES (@playerSteamid, @playerName, @flags, @immunity, @ends, @created)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamId,
				playerName,
				flags,
				immunity,
				ends = futureTime,
				created = now
			});
		}

		public async Task DeleteOldAdmins()
		{
			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sql = "DELETE FROM sa_admins WHERE ends IS NOT NULL AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.Now });
		}
	}
}
