using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.Concurrent;

namespace CS2_SimpleAdmin;

public class AdminSQLManager
{
	private readonly Database _database;

	// Unused for now
	//public static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _adminCache = new ConcurrentDictionary<string, ConcurrentBag<string>>();
	public static readonly ConcurrentDictionary<SteamID, DateTime?> _adminCache = new ConcurrentDictionary<SteamID, DateTime?>();

	//public static readonly ConcurrentDictionary<SteamID, DateTime?> _adminCacheTimestamps = new ConcurrentDictionary<SteamID, DateTime?>();

	public AdminSQLManager(Database database)
	{
		_database = database;
	}

	public async Task<List<(List<string>, int)>> GetAdminFlags(string steamId)
	{
		DateTime now = DateTime.UtcNow.ToLocalTime();

		await using MySqlConnection connection = await _database.GetConnectionAsync();

		string groupId = CS2_SimpleAdmin.GroupId != null ? string.Join("", CS2_SimpleAdmin.GroupId) : string.Empty;

		string sql = "SELECT flags, immunity, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime) AND ((server_id IS NULL OR server_id = @serverid) OR ((@groupid IS NOT NULL AND @groupid <> 0) AND group_id LIKE CONCAT('%', @groupid, '%')));";
		List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { PlayerSteamID = steamId, CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId, groupid = groupId }))?.ToList();

		if (activeFlags == null)
		{
			return new List<(List<string>, int)>();
		}

		List<(List<string>, int)> filteredFlagsWithImmunity = new List<(List<string>, int)>();

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

	public async Task<List<(string, List<string>, int, DateTime?)>> GetAllPlayersFlags()
	{
		DateTime now = DateTime.UtcNow.ToLocalTime();

		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			string groupId = CS2_SimpleAdmin.GroupId != null ? string.Join("", CS2_SimpleAdmin.GroupId) : string.Empty;

			string sql = "SELECT player_steamid, flags, immunity, ends FROM sa_admins  WHERE (ends IS NULL OR ends > @CurrentTime) AND ((server_id IS NULL OR server_id = @serverid) OR ((@groupid IS NOT NULL AND @groupid <> 0) AND group_id LIKE CONCAT('%', @groupid, '%')));";
			List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId, groupid = groupId }))?.ToList();

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

			return filteredFlagsWithImmunity;
		}
		catch (Exception)
		{
			return new List<(string, List<string>, int, DateTime?)>();
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

		await connection.ExecuteAsync(sql, new { PlayerSteamID = playerSteamId, ServerId = CS2_SimpleAdmin.ServerId });
	}

	public async Task AddAdminBySteamId(string playerSteamId, string playerName, string flags, int immunity = 0, int time = 0, bool globalAdmin = false)
	{
		if (string.IsNullOrEmpty(playerSteamId)) return;

		flags = flags.Replace(" ", "");

		DateTime now = DateTime.UtcNow.ToLocalTime();
		DateTime? futureTime;
		if (time != 0)
			futureTime = now.ToLocalTime().AddMinutes(time);
		else
			futureTime = null;

		await using MySqlConnection connection = await _database.GetConnectionAsync();

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
			if (CS2_SimpleAdmin._logger != null)
				CS2_SimpleAdmin._logger.LogCritical("Unable to remove expired admins");
		}
	}
}