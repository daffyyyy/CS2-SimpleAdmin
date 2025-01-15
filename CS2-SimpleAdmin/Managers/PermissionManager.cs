using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace CS2_SimpleAdmin.Managers;

public class PermissionManager(Database.Database? database)
{
    // Unused for now
    //public static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _adminCache = new ConcurrentDictionary<string, ConcurrentBag<string>>();
    public static readonly ConcurrentDictionary<SteamID, DateTime?> AdminCache = new();

    // Get the relevant server groups from the sa_servers_groups table by searching if the serverId is in the servers set column
	public async Task<List<string>> GetServerGroups()
	{
		if (database == null) return [];
        
        await using MySqlConnection connection = await database.GetConnectionAsync();

		string sql = "SELECT id FROM sa_servers_groups WHERE FIND_IN_SET(@ServerId, servers)";
		List<dynamic>? serverGroups = (await connection.QueryAsync(sql, new { ServerId = CS2_SimpleAdmin.ServerId }))?.ToList();

		if (serverGroups == null)
		{
			return new List<string>();
		}

		List<string> serverGroupsList = new List<string>();

		foreach (dynamic serverGroup in serverGroups)
		{
			if (serverGroup is not IDictionary<string, object> serverGroupDict)
			{
				Console.WriteLine("[GetServerGroups] Failed to parse server group.");
				continue;
			}

			if (!serverGroupDict.TryGetValue("id", out var idObj))
			{
				Console.WriteLine("[GetServerGroups] Failed to get server group id.");
				continue;
			}

			if (idObj is not string id)
			{
				// Convert to string and add it to the list
				// Console.WriteLine("[GetServerGroups] Failed to parse server group id.");
				serverGroupsList.Add(idObj.ToString()!);

				continue;
			}

			serverGroupsList.Add(id);
		}

		Console.WriteLine($"[GetServerGroups] Server Groups List: {string.Join(", ", serverGroupsList)}");
		return serverGroupsList;
	}

	public async Task<List<string>> GetAdminFlagsAsString(string steamId)
	{
		if (database == null) return [];
        
        DateTime now = DateTime.UtcNow.ToLocalTime();

		await using MySqlConnection connection = await database.GetConnectionAsync();

		// string sql = "SELECT flags, immunity, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR FIND_IN_SET(@serverid, server_id) > 0)";
		// string sql = "SELECT player_steamid, flags, immunity, ends FROM sa_admins WHERE (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR FIND_IN_SET(@serverid, server_id) > 0)";
		List<string> serverGroups = await GetServerGroups();
		string serverGroupsCondition = string.Join(" OR ", serverGroups.Select(g => $"FIND_IN_SET({g}, servers_groups) > 0"));

		string GroupCheck = serverGroups.Count > 0 ? $"AND (((server_id IS NULL AND servers_groups IS NULL) OR FIND_IN_SET(@serverid, server_id) > 0) OR (servers_groups IS NULL OR {serverGroupsCondition}))" : "AND ((server_id IS NULL AND servers_groups IS NULL) OR FIND_IN_SET(@serverid, server_id) > 0)";
		string sql = $"SELECT flags, immunity, ends FROM sa_admins WHERE player_steamid = @PlayerSteamID AND (ends IS NULL OR ends > @CurrentTime) {GroupCheck}";

		List<dynamic>? activeFlags = (await connection.QueryAsync(sql, new { PlayerSteamID = steamId, CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId }))?.ToList();

		if (activeFlags == null)
		{
			return new List<string>();
		}

		List<string> flagsList = new List<string>();

		foreach (dynamic flags in activeFlags)
		{
			if (flags is not IDictionary<string, object> flagsDict)
			{
				continue;
			}

			if (!flagsDict.TryGetValue("flags", out var flagsValueObj))
			{
				continue;
			}

			if (!(flagsValueObj is string flagsValue))
			{
				continue;
			}

			// If flags start with '#', fetch flags from sa_admins_groups
			if (flagsValue.StartsWith("#"))
			{
				string groupSql = "SELECT flags FROM sa_admins_groups WHERE id = @GroupId";
				var group = await connection.QueryFirstOrDefaultAsync(groupSql, new { GroupId = flagsValue });

				if (group != null)
				{
					flagsValue = group.flags;
				}
			}

			flagsList.AddRange(flagsValue.Split(','));
		}

		return flagsList;
	}

	public async Task<List<(string, List<string>, int, DateTime?)>> GetAllPlayersFlags()
	{
		if (database == null) return [];
        
        DateTime now = DateTime.UtcNow.ToLocalTime();

		try
		{
			await using MySqlConnection connection = await database.GetConnectionAsync();

			// string sql = "SELECT player_steamid, flags, immunity, ends FROM sa_admins WHERE (ends IS NULL OR ends > @CurrentTime) AND (server_id IS NULL OR FIND_IN_SET(@serverid, server_id) > 0)";
			List<string> serverGroups = await GetServerGroups();
			string serverGroupsCondition = string.Join(" OR ", serverGroups.Select(g => $"FIND_IN_SET({g}, servers_groups) > 0"));

			string GroupCheck = serverGroups.Count > 0 ? $"AND (((server_id IS NULL AND servers_groups IS NULL) OR FIND_IN_SET(@serverid, server_id) > 0) OR (servers_groups IS NULL OR {serverGroupsCondition}))" : "AND ((server_id IS NULL AND servers_groups IS NULL) OR FIND_IN_SET(@serverid, server_id) > 0)";
			string sql = $"SELECT player_steamid, flags, immunity, ends FROM sa_admins WHERE (ends IS NULL OR ends > @CurrentTime) {GroupCheck}";

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

				//
				if (flagsValue.StartsWith("#"))
				{
					string groupSql = "SELECT flags, immunity FROM sa_admins_groups WHERE id = @GroupId";
					var group = await connection.QueryFirstOrDefaultAsync(groupSql, new { GroupId = flagsValue });

					if (group != null)
					{
						flagsValue = group.flags;
						if (int.TryParse(group.immunity.ToString(), out int immunityGroupValue))
						{
							filteredFlagsWithImmunity.Add((steamId, flagsValue.Split(',').ToList(), immunityGroupValue, ends));
							// Console.WriteLine($"Flags Check (Group): SteamId {steamId} Flags: {flagsValue}, Immunity: {immunityValue}");

							continue;
						}
						else
						{
							Console.WriteLine($"Failed to parse immunity: {group.immunity}");
						}
					}
				}

				filteredFlagsWithImmunity.Add((steamId, flagsValue.Split(',').ToList(), immunityValue, ends));
			}

			return filteredFlagsWithImmunity;
		}
		catch (Exception e)
		{
			Console.WriteLine($"Error: {e.Message}");
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
				if (!AdminCache.ContainsKey(steamId))
				{
					AdminCache.TryAdd(steamId, ends);
					//_adminCacheTimestamps.Add(steamId, ends);
				}

				Helper.GivePlayerFlags(steamId, flags, (uint)immunity);
				// Often need to call 2 times
				Helper.GivePlayerFlags(steamId, flags, (uint)immunity);
			}
		}
	}

    private async Task<List<(string, string, List<string>, int, DateTime?)>> GetAllPlayersFlagsSA()
    {
	    if (database == null) return [];

        var now = Time.ActualDateTime();

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

            var admins = (await connection.QueryAsync(sql, new { CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId })).ToList();

            // Group by player_steamid and aggregate the flags
            var groupedPlayers = admins
                .GroupBy(r => new { r.player_steamid, r.player_name, r.immunity, r.ends })
                .Select(g => (
                    PlayerSteamId: (string)g.Key.player_steamid,
                    PlayerName: (string)g.Key.player_name,
                    Flags: g.Select(r => (string)r.flag).Distinct().ToList(),
                    Immunity: g.Key.immunity is int i ? i : int.TryParse((string)g.Key.immunity, out var immunity) ? immunity : 0,
                    Ends: g.Key.ends is DateTime dateTime ? dateTime : (DateTime?)null
                ))
                .ToList();

            
			// foreach (var player in groupedPlayers)
			// {
			// 	Console.WriteLine($"Player SteamID: {player.PlayerSteamId}, Name: {player.PlayerName}, Flags: {string.Join(", ", player.Flags)}, Immunity: {player.Immunity}, Ends: {player.Ends}");
			// }
			
	            List<(string, string, List<string>, int, DateTime?)> filteredFlagsWithImmunity = [];

            // Add the grouped players to the list
            filteredFlagsWithImmunity.AddRange(groupedPlayers);

            return filteredFlagsWithImmunity;
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError("Unable to load admins from database! {exception}", ex.Message);
            return [];
        }
    }

    private async Task<Dictionary<string, (List<string>, int)>> GetAllGroupsData()
    {
	    if (database == null) return [];

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
            CS2_SimpleAdmin._logger?.LogError("Unable to load groups from database! {exception}", ex.Message);
        }

        return [];
    }

    public async Task CreateGroupsJsonFile()
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

    public async Task CreateAdminsJsonFile()
    {
        List<(string identity, string name, List<string> flags, int immunity, DateTime? ends)> allPlayers = await GetAllPlayersFlagsSA();
        var validPlayers = allPlayers
            .Where(player => SteamID.TryParse(player.identity, out _))
            .ToList();

		// foreach (var player in allPlayers)
		// {
		// 	var (steamId, name, flags, immunity, ends) = player;
		//           
		// 	Console.WriteLine($"Player SteamID: {steamId}");
		// 	Console.WriteLine($"Player Name: {name}");
		// 	Console.WriteLine($"Flags: {string.Join(", ", flags)}");
		// 	Console.WriteLine($"Immunity: {immunity}");
		// 	Console.WriteLine($"Ends: {(ends.HasValue ? ends.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}");
		// 	Console.WriteLine();
		// }

		var jsonData = validPlayers
			.GroupBy(player => player.name) // Group by player name
			.ToDictionary(
				group => group.Key, // Use the player name as the key
				group =>
				{
					// Consolidate data for players with the same name
					var consolidatedData = group.Aggregate(
						new
						{
							identity = string.Empty,
							immunity = 0,
							flags = new List<string>(),
							groups = new List<string>()
						},
						(acc, player) =>
						{
							// Merge identities and use the latest or first non-null identity
							if (string.IsNullOrEmpty(acc.identity) && !string.IsNullOrEmpty(player.identity))
							{
								acc = acc with { identity = player.identity };
							}

							// Combine immunities by taking the maximum value
							acc = acc with { immunity = Math.Max(acc.immunity, player.immunity) };

							// Combine flags and groups, ensuring no duplicates
							acc = acc with
							{
								flags = acc.flags.Concat(player.flags.Where(flag => flag.StartsWith($"@"))).Distinct().ToList(),
								groups = acc.groups.Concat(player.flags.Where(flag => flag.StartsWith($"#"))).Distinct().ToList()
							};

							return acc;
						});

					foreach (var player in group)
					{
						SteamID.TryParse(player.identity, out var steamId);
						if (steamId != null && !AdminCache.ContainsKey(steamId))
						{
							AdminCache.TryAdd(steamId, player.ends);
						}
					}

					return (object)consolidatedData;
				});
		
        var json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
        var filePath = Path.Combine(CS2_SimpleAdmin.Instance.ModuleDirectory, "data", "admins.json");
        
        await File.WriteAllTextAsync(filePath, json);

        //await File.WriteAllTextAsync(CS2_SimpleAdmin.Instance.ModuleDirectory + "/data/admins.json", json);
    }

    public async Task DeleteAdminBySteamId(string playerSteamId, bool globalDelete = false)
    {
	    if (database == null) return;
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
	    if (database == null) return;

        if (string.IsNullOrEmpty(playerSteamId) || flagsList.Count == 0) return;

        var now = Time.ActualDateTime();
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

            await Server.NextWorldUpdateAsync(() =>
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
	    if (database == null) return;

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

            await Server.NextWorldUpdateAsync(() =>
            {
                CS2_SimpleAdmin.Instance.ReloadAdmins(null);
            });

        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError("Problem with loading admins: {exception}", ex.Message);
        }
    }

    public async Task DeleteGroup(string groupName)
    {
	    if (database == null) return;

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
	    if (database == null) return;

        try
        {
            await using var connection = await database.GetConnectionAsync();

            const string sql = "DELETE FROM sa_admins WHERE ends IS NOT NULL AND ends <= @CurrentTime";
            await connection.ExecuteAsync(sql, new { CurrentTime = Time.ActualDateTime() });
        }
        catch (Exception)
        {
            CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired admins");
        }
    }
}