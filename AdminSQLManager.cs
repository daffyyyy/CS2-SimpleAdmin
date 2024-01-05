using Dapper;
using MySqlConnector;

namespace CS2_SimpleAdmin
{
	internal class AdminSQLManager
	{
		private readonly MySqlConnection _dbConnection;
		// Unused for now
		//public static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _adminCache = new ConcurrentDictionary<string, ConcurrentBag<string>>();

		public AdminSQLManager(string connectionString)
		{
			_dbConnection = new MySqlConnection(connectionString);
		}

		/*
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

				string sql = "SELECT flags, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime)";
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
		*/

		public async Task<List<object>> GetAdminFlags(string steamId)
		{
			/* Unused for now
			if (_adminCache.TryGetValue(steamId, out ConcurrentBag<string>? cachedFlags))
			{
				return cachedFlags.ToList<object>();
			}
			*/
			DateTime now = DateTime.Now;

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sql = "SELECT flags, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime)";
			List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { PlayerSteamID = steamId, CurrentTime = now }))?.ToList();

			if (activeFlags == null)
			{
				return new List<object>();
			}

			List<string> filteredFlags = new List<string>();

			foreach (var flags in activeFlags)
			{
				if (flags == null) continue;

				string flag = flags.flags.ToString();
				if (flag != null)
				{
					filteredFlags.Add(flag);
				}
			}

			/* Unused for now
			bool shouldCache = activeFlags.Any(flags =>
			{
				if (flags?.ends == null)
				{
					return true;
				}

				if (flags.ends is DateTime endsTime)
				{
					return (endsTime - now).TotalHours > 1;
				}

				return false;
			});

			if (shouldCache)
			{
				List<string> flagsToCache = new List<string>();

				foreach (var flags in activeFlags)
				{
					if (flags.ends == null || (DateTime.Now - (DateTime)flags.ends).TotalHours > 6)
					{
						if (flags == null) continue;
						flagsToCache.Add(flags.flags.ToString());
					}
				}

				_adminCache.AddOrUpdate(steamId, new ConcurrentBag<string>(flagsToCache), (_, existingBag) =>
				{
					foreach (var flag in flagsToCache)
					{
						existingBag.Add(flag);
					}
					return existingBag;
				});
				return flagsToCache.Cast<object>().ToList();
			}
			*/

			return filteredFlags.Cast<object>().ToList();
		}

		public async Task DeleteAdminBySteamId(string playerSteamId)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			//_adminCache.TryRemove(playerSteamId, out _);

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