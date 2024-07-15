using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace CS2_SimpleAdmin;

public class PermissionManager(Database.Database database)
{
	// Unused for now
	//public static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _adminCache = new ConcurrentDictionary<string, ConcurrentBag<string>>();
	public static readonly ConcurrentDictionary<SteamID, DateTime?> AdminCache = new();

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

	private async Task<List<(string, string, List<string>, int, DateTime?)>> GetAllPlayersFlags()
	{
		var now = DateTime.UtcNow.ToLocalTime();

		try
		{
			await using var connection = await database.GetConnectionAsync();

			const string sql = """
			                               SELECT sa_admins.player_steamid, sa_admins.player_name, sa_admins_flags.flag, sa_admins.immunity, sa_admins.ends
			                               FROM sa_admins_flags
			                               JOIN sa_admins ON sa_admins_flags.admin_id = sa_admins.id
			                               WHERE (sa_admins.ends IS NULL OR sa_admins.ends > @CurrentTime)
			                               AND (sa_admins.server_id IS NULL OR sa_admins.server_id = @serverid)
			                               ORDER BY sa_admins.player_steamid
			                   """;

			var activeFlags = (await connection.QueryAsync(sql, new { CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId })).ToList();

			List<(string, string, List<string>, int, DateTime?)> filteredFlagsWithImmunity = [];
			var currentSteamId = string.Empty;
			var currentPlayerName = string.Empty;
			List<string> currentFlags = [];
			var immunityValue = 0;
			DateTime? ends = null;

			foreach (var flagInfo in activeFlags)
			{
				if (flagInfo is not IDictionary<string, object> flagInfoDict)
				{
					continue;
				}

				if (!flagInfoDict.TryGetValue("player_steamid", out var steamIdObj) ||
					!flagInfoDict.TryGetValue("player_name", out var playerNameObj) ||
					!flagInfoDict.TryGetValue("flag", out var flagObj) ||
					!flagInfoDict.TryGetValue("immunity", out var immunityValueObj) ||
					!flagInfoDict.TryGetValue("ends", out var endsObj))
				{
					continue;
				}

				if (steamIdObj is not string steamId ||
					playerNameObj is not string playerName ||
					flagObj is not string flag ||
					!int.TryParse(immunityValueObj.ToString(), out immunityValue))
				{
					continue;
				}

				if (ends != null)
				{
					if (DateTime.TryParse(endsObj.ToString(), out var parsedEnds))
					{
						ends = parsedEnds;
					}
				}

				if (currentSteamId != steamId && !string.IsNullOrEmpty(currentSteamId))
				{
					filteredFlagsWithImmunity.Add((currentSteamId, currentPlayerName, currentFlags, immunityValue, ends));
					currentFlags = [];
				}

				currentSteamId = steamId;
				currentPlayerName = playerName;
				currentFlags.Add(flag);

			}

			if (!string.IsNullOrEmpty(currentSteamId))
			{
				filteredFlagsWithImmunity.Add((currentSteamId, currentPlayerName, currentFlags, immunityValue, ends));
			}

			return filteredFlagsWithImmunity;
		}
		catch (Exception ex)
		{
			CS2_SimpleAdmin._logger?.LogError(ex.ToString());
			return [];
		}
	}

	/*
	public async Task<Dictionary<int, Tuple<List<string>, List<Tuple<string, DateTime?>>, int>>> GetAllGroupsFlags()
	{
		try
		{
			await using MySqlConnection connection = await _database.GetConnectionAsync();

			string sql = "SELECT group_id FROM sa_groups_servers WHERE server_id = @serverid";
			var groupIds = connection.Query<int>(sql, new { serverid = CS2_SimpleAdmin.ServerId }).ToList();

			sql = @"
            SELECT g.group_id, f.flag 
            FROM sa_groups_flags f
            JOIN sa_groups_servers g ON f.group_id = g.group_id
            WHERE g.server_id = @serverid";

			var groupFlagData = connection.Query(sql, new { serverid = CS2_SimpleAdmin.ServerId }).ToList();

			if (groupIds.Count == 0 || groupFlagData.Count == 0)
			{
				return [];
			}

			var groupInfoDictionary = new Dictionary<int, Tuple<List<string>, List<Tuple<string, DateTime?>>, int>>();

			foreach (var groupId in groupIds)
			{
				groupInfoDictionary[groupId] = new Tuple<List<string>, List<Tuple<string, DateTime?>>, int>([], [], 0);
			}

			foreach (var row in groupFlagData)
			{
				var groupId = (int)row.group_id;
				var flag = (string)row.flag;

				groupInfoDictionary[groupId].Item1.Add(flag);
			}

			sql = @"
            SELECT a.group_id, a.player_steamid, a.ends, g.immunity, g.name  
            FROM sa_admins a
            JOIN sa_groups g ON a.group_id = g.id
            WHERE a.group_id IN @groupIds";

			var playerData = (await connection.QueryAsync(sql, new { groupIds })).ToList();

			foreach (var row in playerData)
			{
				var groupId = (int)row.group_id;
				var playerSteamid = (string)row.player_steamid;
				var ends = row.ends as DateTime?;
				var immunity = (int)row.immunity;

				groupInfoDictionary[groupId].Item2.Add(new Tuple<string, DateTime?>(playerSteamid, ends));
				groupInfoDictionary[groupId] = new Tuple<List<string>, List<Tuple<string, DateTime?>>, int>(groupInfoDictionary[groupId].Item1, groupInfoDictionary[groupId].Item2, immunity);
			}

			return groupInfoDictionary;
		}
		catch { }

		return [];
	}
	*/

	private async Task<Dictionary<string, (List<string>, int)>> GetAllGroupsData()
	{
		await using MySqlConnection connection = await database.GetConnectionAsync();
		try
		{
			var sql = "SELECT group_id FROM sa_groups_servers WHERE (server_id = @serverid OR server_id IS NULL)";
			var groupDataSql = connection.Query<int>(sql, new { serverid = CS2_SimpleAdmin.ServerId }).ToList();

			sql = """
			      				SELECT g.group_id, sg.name AS group_name, sg.immunity, f.flag
			      				FROM sa_groups_flags f
			      				JOIN sa_groups_servers g ON f.group_id = g.group_id
			      				JOIN sa_groups sg ON sg.id = g.group_id
			      				WHERE (g.server_id = @serverid OR server_id IS NULL)
			      """;

			var groupData = connection.Query(sql, new { serverid = CS2_SimpleAdmin.ServerId }).ToList();

			if (groupDataSql.Count == 0 || groupData.Count == 0)
			{
				return [];
			}

			var groupInfoDictionary = new Dictionary<string, (List<string>, int)>();

			foreach (var row in groupData)
			{
				var groupName = (string)row.group_name;
				var flag = (string)row.flag;
				var immunity = (int)row.immunity;

				// Check if the group name already exists in the dictionary
				if (!groupInfoDictionary.TryGetValue(groupName, out (List<string>, int) value))
				{
					value = ([], immunity);
					// If it doesn't exist, add a new entry with an empty list of flags and immunity
					groupInfoDictionary[groupName] = value;
				}

				value.Item1.Add(flag);
			}

			return groupInfoDictionary;
		}
		catch (Exception ex)
		{
			CS2_SimpleAdmin._logger?.LogError(ex.ToString());
		}

		return [];
	}

	public async Task CrateGroupsJsonFile()
	{
		var groupsData = await GetAllGroupsData();

		var jsonData = new Dictionary<string, object>();

		foreach (var kvp in groupsData)
		{
			var groupData = new Dictionary<string, object>
			{
				["flags"] = kvp.Value.Item1,
				["immunity"] = kvp.Value.Item2
			};

			jsonData[kvp.Key] = groupData;
		}

		var json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
		var filePath = Path.Combine(CS2_SimpleAdmin.Instance.ModuleDirectory, "data", "groups.json");
		await File.WriteAllTextAsync(filePath, json);
	}

	/*
	public async Task GiveAllGroupsFlags()
	{
		Dictionary<int, Tuple<List<string>, List<Tuple<string, DateTime?>>, int>> groupFlags = await GetAllGroupsFlags();

		foreach (var kvp in groupFlags)
		{
			var flags = kvp.Value.Item1;
			var players = kvp.Value.Item2;
			int immunity = kvp.Value.Item3;

			foreach (var playerTuple in players)
			{
				var steamIdStr = playerTuple.Item1;
				var ends = playerTuple.Item2;

				if (!string.IsNullOrEmpty(steamIdStr) && SteamID.TryParse(steamIdStr, out var steamId) && steamId != null)
				{
					if (!_adminCache.ContainsKey(steamId))
					{
						_adminCache.TryAdd(steamId, ends);
					}

					Helper.GivePlayerFlags(steamId, flags, (uint)immunity);
					// Often need to call 2 times
					Helper.GivePlayerFlags(steamId, flags, (uint)immunity);
				}
			}
		}
	}
	*/
	/*
	public async Task GiveAllFlags()
	{
		List<(string, string, List<string>, int, DateTime?)> allPlayers = await GetAllPlayersFlags();

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
	*/

	public async Task CreateAdminsJsonFile()
	{
		List<(string identity, string name, List<string> flags, int immunity, DateTime? ends)> allPlayers = await GetAllPlayersFlags();

		var jsonData = allPlayers
			.Select(player =>
			{
				SteamID? steamId = null;

				if (!string.IsNullOrEmpty(player.identity) && SteamID.TryParse(player.identity, out var id) && id != null)
				{
					steamId = id;
				}

				if (steamId != null && !AdminCache.ContainsKey(steamId))
				{
					AdminCache.TryAdd(steamId, player.ends);
				}

				return new
				{
					playerName = player.name,
					playerData = new
					{
						player.identity,
						player.immunity,
						flags = player.flags.Where(flag => flag.StartsWith($"@")).ToList(),
						groups = player.flags.Where(flag => flag.StartsWith($"#")).ToList()
					}
				};
			})
			.ToDictionary(item => item.playerName, item => item.playerData);

		var json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
		var filePath = Path.Combine(CS2_SimpleAdmin.Instance.ModuleDirectory, "data", "admins.json");
		await File.WriteAllTextAsync(filePath, json);
		
		//await File.WriteAllTextAsync(CS2_SimpleAdmin.Instance.ModuleDirectory + "/data/admins.json", json);
	}

	public async Task DeleteAdminBySteamId(string playerSteamId, bool globalDelete = false)
	{
		if (string.IsNullOrEmpty(playerSteamId)) return;

		//_adminCache.TryRemove(playerSteamId, out _);

		try
		{
			await using var connection = await database.GetConnectionAsync();

			var sql = globalDelete
				? "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID"
				: "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID AND server_id = @ServerId";

			await connection.ExecuteAsync(sql, new { PlayerSteamID = playerSteamId, CS2_SimpleAdmin.ServerId });
		}
		catch (Exception ex)
		{
			CS2_SimpleAdmin._logger?.LogError(ex.ToString());
		}
	}

	public async Task AddAdminBySteamId(string playerSteamId, string playerName, List<string> flagsList, int immunity = 0, int time = 0, bool globalAdmin = false)
	{
		if (string.IsNullOrEmpty(playerSteamId) || flagsList.Count == 0) return;

		var now = DateTime.UtcNow.ToLocalTime();
		DateTime? futureTime;

		if (time != 0)
			futureTime = now.AddMinutes(time);
		else
			futureTime = null;

		try
		{
			await using var connection = await database.GetConnectionAsync();

			// Insert admin into sa_admins table
			const string insertAdminSql = "INSERT INTO `sa_admins` (`player_steamid`, `player_name`, `immunity`, `ends`, `created`, `server_id`) " +
										  "VALUES (@playerSteamid, @playerName, @immunity, @ends, @created, @serverid); SELECT LAST_INSERT_ID();";

			var adminId = await connection.ExecuteScalarAsync<int>(insertAdminSql, new
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
				if (flag.StartsWith($"#"))
				{
					const string sql = "SELECT id FROM `sa_groups` WHERE name = @groupName";
					var groupId = await connection.QuerySingleOrDefaultAsync<int?>(sql, new { groupName = flag });

					if (groupId != null)
					{
						const string updateAdminGroup = "UPDATE `sa_admins` SET group_id = @groupId WHERE id = @adminId";
						await connection.ExecuteAsync(updateAdminGroup, new
						{
							groupId,
							adminId
						});
					}
				}

				const string insertFlagsSql = "INSERT INTO `sa_admins_flags` (`admin_id`, `flag`) " +
											  "VALUES (@adminId, @flag)";

				await connection.ExecuteAsync(insertFlagsSql, new
				{
					adminId,
					flag
				});
			}

			await Server.NextFrameAsync(() =>
			{
				CS2_SimpleAdmin.Instance.ReloadAdmins(null);
			});
		}
		catch (Exception ex)
		{
			CS2_SimpleAdmin._logger?.LogError(ex.ToString());
		}
	}

	public async Task AddGroup(string groupName, List<string> flagsList, int immunity = 0, bool globalGroup = false)
	{
		if (string.IsNullOrEmpty(groupName) || flagsList.Count == 0) return;

		await using var connection = await database.GetConnectionAsync();
		try
		{
			// Insert group into sa_groups table
			const string insertGroup = "INSERT INTO `sa_groups` (`name`, `immunity`) " +
									   "VALUES (@groupName, @immunity); SELECT LAST_INSERT_ID();";
			var groupId = await connection.ExecuteScalarAsync<int>(insertGroup, new
			{
				groupName,
				immunity
			});

			// Insert flags into sa_groups_flags table
			foreach (var flag in flagsList)
			{
				const string insertFlagsSql = "INSERT INTO `sa_groups_flags` (`group_id`, `flag`) " +
											  "VALUES (@groupId, @flag)";

				await connection.ExecuteAsync(insertFlagsSql, new
				{
					groupId,
					flag
				});
			}

			const string insertGroupServer = "INSERT INTO `sa_groups_servers` (`group_id`, `server_id`) " +
											 "VALUES (@groupId, @server_id)";

			await connection.ExecuteAsync(insertGroupServer, new { groupId, server_id = globalGroup ? null : CS2_SimpleAdmin.ServerId });

			await Server.NextFrameAsync(() =>
			{
				CS2_SimpleAdmin.Instance.ReloadAdmins(null);
			});

		}
		catch (Exception ex)
		{
			CS2_SimpleAdmin._logger?.LogError(ex.ToString());
		}
	}

	public async Task DeleteGroup(string groupName)
	{
		if (string.IsNullOrEmpty(groupName)) return;

		await using var connection = await database.GetConnectionAsync();
		try
		{
			const string sql = "DELETE FROM `sa_groups` WHERE name = @groupName";
			await connection.ExecuteAsync(sql, new { groupName });
		}
		catch (Exception ex)
		{
			CS2_SimpleAdmin._logger?.LogError(ex.ToString());
		}
	}

	public async Task DeleteOldAdmins()
	{
		try
		{
			await using var connection = await database.GetConnectionAsync();

			const string sql = "DELETE FROM sa_admins WHERE ends IS NOT NULL AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.UtcNow.ToLocalTime() });
		}
		catch (Exception)
		{
			CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired admins");
		}
	}
}