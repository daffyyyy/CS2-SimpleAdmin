using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using MySqlConnector;

namespace CS2_SimpleAdmin
{
	internal class AdminSQLManager
	{
		private readonly MySqlConnection _dbConnection;
		// Unused for now
		//public static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _adminCache = new ConcurrentDictionary<string, ConcurrentBag<string>>();
		public static readonly HashSet<SteamID> _adminCacheSet = new HashSet<SteamID>();
		public static readonly Dictionary<SteamID, DateTime?> _adminCacheTimestamps = new Dictionary<SteamID, DateTime?>();

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

		public async Task<List<(List<string>, int)>> GetAdminFlags(string steamId)
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

			string sql = "SELECT flags, immunity, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR server_id = @serverid)";
			List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { PlayerSteamID = steamId, CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId }))?.ToList();

			if (activeFlags == null)
			{
				return new List<(List<string>, int)>();
			}

			List<(List<string>, int)> filteredFlagsWithImmunity = new List<(List<string>, int)>();

			/*
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
			*/

			foreach (dynamic flags in activeFlags)
			{
				if (flags is not IDictionary<string, object> flagsDict)
				{
					continue;
				}

				if (!flagsDict.TryGetValue("flags", out var flagsValueObj) || !flagsDict.TryGetValue("immunity", out var immunityValueObj))
				{
					continue;
				}

				if (!(flagsValueObj is string flagsValue) || !int.TryParse(immunityValueObj.ToString(), out var immunityValue))
				{
					continue;
				}

				//Console.WriteLine($"Flags: {flagsValue}, Immunity: {immunityValue}");

				filteredFlagsWithImmunity.Add((flagsValue.Split(',').ToList(), immunityValue));
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
			await connection.CloseAsync();

			return filteredFlagsWithImmunity;
			//return filteredFlags.Cast<object>().ToList();
		}

		public async Task<List<(string, List<string>, int, DateTime?)>> GetAllPlayersFlags()
		{
			DateTime now = DateTime.Now;

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sql = "SELECT player_steamid, flags, immunity, ends FROM sa_admins WHERE (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR server_id = @serverid)";
			List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId }))?.ToList();

			if (activeFlags == null)
			{
				return new List<(string, List<string>, int, DateTime?)>();
			}

			List<(string, List<string>, int, DateTime?)> filteredFlagsWithImmunity = new List<(string, List<string>, int, DateTime?)>();

			foreach (dynamic flags in activeFlags)
			{
				if (flags is not IDictionary<string, object> flagsDict)
				{
					continue;
				}

				if (!flagsDict.TryGetValue("player_steamid", out var steamIdObj) ||
					!flagsDict.TryGetValue("flags", out var flagsValueObj) ||
					!flagsDict.TryGetValue("immunity", out var immunityValueObj) ||
					!flagsDict.TryGetValue("ends", out var endsObj))
				{
					//Console.WriteLine("One or more required keys are missing.");
					continue;
				}

				DateTime? ends = null;

				if (endsObj != null) // Check if "ends" is not null
				{
					if (!DateTime.TryParse(endsObj.ToString(), out var parsedEnds))
					{
						//Console.WriteLine("Failed to parse 'ends' value.");
						continue;
					}

					ends = parsedEnds;
				}

				if (!(steamIdObj is string steamId) ||
					!(flagsValueObj is string flagsValue) ||
					!int.TryParse(immunityValueObj.ToString(), out var immunityValue))
				{
					//Console.WriteLine("Failed to parse one or more values.");
					continue;
				}

				filteredFlagsWithImmunity.Add((steamId, flagsValue.Split(',').ToList(), immunityValue, ends));
			}

			await connection.CloseAsync();

			return filteredFlagsWithImmunity;
		}

		public async Task GiveAllFlags()
		{
			List<(string, List<string>, int, DateTime?)> allPlayers = await GetAllPlayersFlags();

			foreach (var record in allPlayers)
			{
				string steamIdStr = record.Item1;
				List<string> flags = record.Item2;
				int immunity = record.Item3;

				DateTime? ends = record.Item4;

				if (!string.IsNullOrEmpty(steamIdStr) && SteamID.TryParse(steamIdStr, out var steamId) && steamId != null)
				{
					if (!_adminCacheSet.Contains(steamId))
					{
						_adminCacheSet.Add(steamId);
						_adminCacheTimestamps.Add(steamId, ends);
					}

					Helper.GivePlayerFlags(steamId, flags, (uint)immunity);
					// Often need to call 2 times
					Helper.GivePlayerFlags(steamId, flags, (uint)immunity);
				}
			}
		}

		public async Task DeleteAdminBySteamId(string playerSteamId, bool globalDelete = false)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			//_adminCache.TryRemove(playerSteamId, out _);

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sql = "";

			if (globalDelete)
			{
				sql = "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID";
			}
			else
			{
				sql = "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID AND server_id = @ServerId";
			}

			await connection.ExecuteAsync(sql, new { PlayerSteamID = playerSteamId, ServerId = CS2_SimpleAdmin.ServerId });

			await connection.CloseAsync();
		}

		public async Task AddAdminBySteamId(string playerSteamId, string playerName, string flags, int immunity = 0, int time = 0, bool globalAdmin = false)
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

			var sql = "INSERT INTO `sa_admins` (`player_steamid`, `player_name`, `flags`, `immunity`, `ends`, `created`, `server_id`) " +
				"VALUES (@playerSteamid, @playerName, @flags, @immunity, @ends, @created, @serverid)";

			int? serverId = globalAdmin ? null : CS2_SimpleAdmin.ServerId;

			await connection.ExecuteAsync(sql, new
			{
				playerSteamId,
				playerName,
				flags,
				immunity,
				ends = futureTime,
				created = now,
				serverid = serverId
			});

			await connection.CloseAsync();
		}

		public async Task DeleteOldAdmins()
		{
			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sql = "DELETE FROM sa_admins WHERE ends IS NOT NULL AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.Now });

			await connection.CloseAsync();
		}
	}
}