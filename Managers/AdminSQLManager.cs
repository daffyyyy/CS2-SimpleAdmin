using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;

namespace CS2_SimpleAdmin;

public class AdminSQLManager(Database database)
{
	private readonly Database _database = database;

	// Unused for now
	//public static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _adminCache = new ConcurrentDictionary<string, ConcurrentBag<string>>();
	public static readonly ConcurrentDictionary<SteamID, DateTime?> _adminCache = new();

	/*
	public async Task<List<(List<string>, int)>> GetAdminFlags(string steamId)
	{
		DateTime now = DateTime.UtcNow.ToLocalTime();

		await using MySqlConnection connection = await _database.GetConnectionAsync();

		string sql = "SELECT flags, immunity, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR server_id = @serverid)";
		List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { PlayerSteamID = steamId, CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId }))?.ToList();

		if (activeFlags == null)
		{
			return new List<(List<string>, int)>();
		}

		List<(List<string>, int)> filteredFlagsWithImmunity = [];

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

		return filteredFlagsWithImmunity;
	}
	*/

	public async Task<List<(string, List<string>, int, DateTime?)>> GetAllPlayersFlags()
	{
		DateTime now = DateTime.UtcNow.ToLocalTime();

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			string sql = @"
            SELECT sa_admins.player_steamid, sa_admins_flags.flag, sa_admins.immunity, sa_admins.ends
            FROM sa_admins_flags
            JOIN sa_admins ON sa_admins_flags.admin_id = sa_admins.id
            WHERE (sa_admins.ends IS NULL OR sa_admins.ends > @CurrentTime)
            AND (sa_admins.server_id IS NULL OR sa_admins.server_id = @serverid)
            ORDER BY sa_admins.player_steamid";

			List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId }))?.ToList();

			if (activeFlags == null)
			{
				return [];
			}

			List<(string, List<string>, int, DateTime?)> filteredFlagsWithImmunity = [];
			string currentSteamId = string.Empty;
			List<string> currentFlags = [];
			int immunityValue = 0;
			DateTime? ends = null;

			foreach (dynamic flagInfo in activeFlags)
			{
				if (flagInfo is not IDictionary<string, object> flagInfoDict)
				{
					continue;
				}

				if (!flagInfoDict.TryGetValue("player_steamid", out var steamIdObj) ||
					!flagInfoDict.TryGetValue("flag", out var flagObj) ||
					!flagInfoDict.TryGetValue("immunity", out var immunityValueObj) ||
					!flagInfoDict.TryGetValue("ends", out var endsObj))
				{
					continue;
				}

				if (steamIdObj is not string steamId ||
					flagObj is not string flag ||
					!int.TryParse(immunityValueObj.ToString(), out immunityValue))
				{
					continue;
				}

				if (endsObj != null && DateTime.TryParse(endsObj.ToString(), out var parsedEnds))
				{
					ends = parsedEnds;
				}

				if (currentSteamId != steamId && !string.IsNullOrEmpty(currentSteamId))
				{
					filteredFlagsWithImmunity.Add((currentSteamId, currentFlags, immunityValue, ends));
					currentFlags = [];
				}

				currentSteamId = steamId;
				currentFlags.Add(flag);
			}

			if (!string.IsNullOrEmpty(currentSteamId))
			{
				filteredFlagsWithImmunity.Add((currentSteamId, currentFlags, immunityValue, ends));
			}

			return filteredFlagsWithImmunity;
		}
		catch (Exception)
		{
			return [];
		}
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
				if (!_adminCache.ContainsKey(steamId))
				{
					_adminCache.TryAdd(steamId, ends);
					//_adminCacheTimestamps.Add(steamId, ends);
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

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();
			string sql = "";

			if (globalDelete)
			{
				sql = "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID";
			}
			else
			{
				sql = "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID AND server_id = @ServerId";
			}

			await connection.ExecuteAsync(sql, new { PlayerSteamID = playerSteamId, CS2_SimpleAdmin.ServerId });
		}
		catch { };
	}

	public async Task AddAdminBySteamId(string playerSteamId, string playerName, List<string> flagsList, int immunity = 0, int time = 0, bool globalAdmin = false)
	{
		if (string.IsNullOrEmpty(playerSteamId) || flagsList == null || flagsList.Count == 0) return;

		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime? futureTime;

		if (time != 0)
			futureTime = now.ToLocalTime().AddMinutes(time);
		else
			futureTime = null;

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			// Insert admin into sa_admins table
			var insertAdminSql = "INSERT INTO `sa_admins` (`player_steamid`, `player_name`, `immunity`, `ends`, `created`, `server_id`) " +
								 "VALUES (@playerSteamid, @playerName, @immunity, @ends, @created, @serverid); SELECT LAST_INSERT_ID();";

			int adminId = await connection.ExecuteScalarAsync<int>(insertAdminSql, new
			{
				playerSteamId,
				playerName,
				immunity,
				ends = futureTime,
				created = now,
				serverid = globalAdmin ? null : CS2_SimpleAdmin.ServerId
			});

			// Insert flags into sa_admins_flags table
			foreach (var flag in flagsList)
			{
				Console.WriteLine(flag);
				var insertFlagsSql = "INSERT INTO `sa_admins_flags` (`admin_id`, `flag`) " +
									 "VALUES (@adminId, @flag)";

				await connection.ExecuteAsync(insertFlagsSql, new
				{
					adminId,
					flag
				});
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}

	public async Task DeleteOldAdmins()
	{
		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			string sql = "DELETE FROM sa_admins WHERE ends IS NOT NULL AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.Now.ToLocalTime() });
		}
		catch (Exception)
		{
			CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired admins");
		}
	}
}