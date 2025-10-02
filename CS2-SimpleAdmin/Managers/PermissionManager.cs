using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Admin;
using System.Diagnostics.CodeAnalysis;
using CS2_SimpleAdmin.Database;

namespace CS2_SimpleAdmin.Managers;

public class PermissionManager(IDatabaseProvider? databaseProvider)
{
    // Unused for now
    //public static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _adminCache = new ConcurrentDictionary<string, ConcurrentBag<string>>();
    // public static readonly ConcurrentDictionary<SteamID, DateTime?> AdminCache = new();
    public static readonly ConcurrentDictionary<SteamID, (DateTime? ExpirationTime, List<string> Flags)> AdminCache = new();

    /*
	public async Task<List<(List<string>, int)>> GetAdminFlags(string steamId)
	{
		DateTime now = Time.ActualDateTime();

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

    /// <summary>
    /// Retrieves all players' flags and associated data asynchronously.
    /// </summary>
    /// <returns>A list of tuples containing player SteamID, name, flags, immunity, and expiration time.</returns>
    private async Task<List<(ulong, string ,List<string>, int, DateTime?)>> GetAllPlayersFlags()
    {
	    if (databaseProvider == null)
		    return new List<(ulong, string, List<string>, int, DateTime?)>();

	    var now = Time.ActualDateTime();

	    try
	    {
		    await using var connection = await databaseProvider.CreateConnectionAsync();
		    var sql = databaseProvider.GetAdminsQuery();
		    var admins = (await connection.QueryAsync(sql, new { CurrentTime = now, serverid = CS2_SimpleAdmin.ServerId })).ToList();

		    var groupedPlayers = admins
			    .GroupBy(r => new { playerSteamId = r.player_steamid, playerName = r.player_name, r.immunity, r.ends })
			    .Select(g =>
			    {
				    ulong steamId = g.Key.playerSteamId switch
				    {
					    long l => (ulong)l,
					    int i => (ulong)i,
					    string s when ulong.TryParse(s, out var parsed) => parsed,
					    _ => 0UL
				    };

				    int immunity = g.Key.immunity switch
				    {
					    int i => i,
					    string s when int.TryParse(s, out var parsed) => parsed,
					    _ => 0
				    };

				    DateTime? ends = g.Key.ends as DateTime?;

				    string playerName = g.Key.playerName as string ?? string.Empty;

				    // tutaj zakładamy, że Dapper zwraca już string (nie dynamic)
				    var flags = g.Select(r => r.flag as string ?? string.Empty)
					    .Distinct()
					    .ToList();

				    return (steamId, playerName, flags, immunity, ends);
			    })
			    .ToList();

		    return groupedPlayers;
	    }
	    catch (Exception ex)
	    {
		    CS2_SimpleAdmin._logger?.LogError("Unable to load admins from database! {exception}", ex.Message);
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

    /// <summary>
    /// Retrieves all groups' data including flags and immunity asynchronously.
    /// </summary>
    /// <returns>A dictionary with group names as keys and tuples of flags and immunity as values.</returns>
    private async Task<Dictionary<string, (List<string>, int)>> GetAllGroupsData()
    {
	    if (databaseProvider == null) return [];

	    await using var connection = await databaseProvider.CreateConnectionAsync();
	    ;
        try
        {
            // var sql = "SELECT group_id FROM sa_groups_servers WHERE (server_id = @serverid OR server_id IS NULL)";
            // var groupDataSql = connection.Query<int>(sql, new { serverid = CS2_SimpleAdmin.ServerId }).ToList();

            var sql = databaseProvider.GetGroupsQuery();
            var groupData = connection.Query(sql, new { serverid = CS2_SimpleAdmin.ServerId }).ToList();
            if (groupData.Count == 0)
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

    /// <summary>
    /// Creates a JSON file containing groups data asynchronously.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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

        var options = new JsonSerializerOptions
        {
	        WriteIndented = true,
	        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };

        var json = JsonSerializer.Serialize(jsonData, options);
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

    /// <summary>
    /// Creates a JSON file containing admins data asynchronously.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public async Task CreateAdminsJsonFile()
    {
        List<(ulong identity, string name, List<string> flags, int immunity, DateTime? ends)> allPlayers = await GetAllPlayersFlags();
        var validPlayers = allPlayers
            .Where(player => SteamID.TryParse(player.identity.ToString(), out _))
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
				group => group.Key, // Use the player name as key
				object (group) =>
				{
					// Consolidate data for players with same name
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
							// Merge identities
							if (string.IsNullOrEmpty(acc.identity) && !string.IsNullOrEmpty(player.identity.ToString()))
							{
								acc = acc with { identity = player.identity.ToString() };
							}

							// Combine immunities by maximum value
							acc = acc with { immunity = Math.Max(acc.immunity, player.immunity) };

							// Combine flags and groups
							acc = acc with
							{
								flags = acc.flags.Concat(player.flags.Where(flag => flag.StartsWith($"@"))).Distinct().ToList(),
								groups = acc.groups.Concat(player.flags.Where(flag => flag.StartsWith($"#"))).Distinct().ToList()
							};

							return acc;
						});
					
					Server.NextWorldUpdate(() =>
					{
						var keysToRemove = new List<SteamID>();

						foreach (var steamId in AdminCache.Keys.ToList()) 
						{
							var data = AdminManager.GetPlayerAdminData(steamId);
							if (data != null)
							{
								var flagsArray = AdminCache[steamId].Flags.ToArray();
								AdminManager.RemovePlayerPermissions(steamId, flagsArray);
								AdminManager.RemovePlayerFromGroup(steamId, true, flagsArray);
							}

							keysToRemove.Add(steamId);
						}

						foreach (var steamId in keysToRemove)
						{
							if (!AdminCache.TryRemove(steamId, out _)) continue;

							var data = AdminManager.GetPlayerAdminData(steamId);
							if (data == null) continue;
							if (data.Flags.Count != 0 && data.Groups.Count != 0) continue;

							AdminManager.ClearPlayerPermissions(steamId);
							AdminManager.RemovePlayerAdminData(steamId);
						}

						foreach (var player in group)
						{
							if (SteamID.TryParse(player.identity.ToString(), out var steamId) && steamId != null)
							{
								AdminCache.TryAdd(steamId, (player.ends, player.flags));
							}
						}
					});

					// Server.NextFrameAsync(() =>
					// {
					// 	for (var index = 0; index < AdminCache.Keys.ToList().Count; index++)
					// 	{
					// 		var steamId = AdminCache.Keys.ToList()[index];
					// 		
					// 		var data = AdminManager.GetPlayerAdminData(steamId);
					// 		if (data != null)
					// 		{
					// 			AdminManager.RemovePlayerPermissions(steamId, AdminCache[steamId].Flags.ToArray());
					// 			AdminManager.RemovePlayerFromGroup(steamId, true, AdminCache[steamId].Flags.ToArray());
					// 		}
					// 		
					// 		if (!AdminCache.TryRemove(steamId, out _)) continue;
					//
					// 		if (data == null) continue;
					// 		if (data.Flags.ToList().Count != 0 && data.Groups.ToList().Count != 0)
					// 			continue;
					// 		
					// 		AdminManager.ClearPlayerPermissions(steamId);
					// 		AdminManager.RemovePlayerAdminData(steamId);
					// 	}
					// 	
					// 	foreach (var player in group)
					// 	{
					// 		SteamID.TryParse(player.identity, out var steamId);
					// 		if (steamId == null) continue;
					// 		AdminCache.TryAdd(steamId, (player.ends, player.flags));
					// 	}
					// });

					return consolidatedData;
				});
		
		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		var json = JsonSerializer.Serialize(jsonData, options);
        var filePath = Path.Combine(CS2_SimpleAdmin.Instance.ModuleDirectory, "data", "admins.json");
        
        await File.WriteAllTextAsync(filePath, json);

        //await File.WriteAllTextAsync(CS2_SimpleAdmin.Instance.ModuleDirectory + "/data/admins.json", json);
    }

    /// <summary>
    /// Deletes an admin by their SteamID from the database asynchronously.
    /// </summary>
    /// <param name="playerSteamId">The SteamID of the admin to delete.</param>
    /// <param name="globalDelete">Whether to delete the admin globally or only for the current server.</param>
    public async Task DeleteAdminBySteamId(string playerSteamId, bool globalDelete = false)
    {
	    if (databaseProvider == null) return;
        if (string.IsNullOrEmpty(playerSteamId)) return;

        //_adminCache.TryRemove(playerSteamId, out _);

        try
        {
	        await using var connection = await databaseProvider.CreateConnectionAsync();
            var sql = databaseProvider.GetDeleteAdminQuery(globalDelete);
            await connection.ExecuteAsync(sql, new { PlayerSteamID = playerSteamId, CS2_SimpleAdmin.ServerId });
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError(ex.Message);
        }
    }

    /// <summary>
    /// Adds a new admin with specified details asynchronously.
    /// </summary>
    /// <param name="playerSteamId">SteamID of the admin.</param>
    /// <param name="playerName">Name of the admin.</param>
    /// <param name="flagsList">List of flags assigned to the admin.</param>
    /// <param name="immunity">Immunity level.</param>
    /// <param name="time">Duration in minutes for admin expiration; 0 means permanent.</param>
    /// <param name="globalAdmin">Whether the admin is global or server-specific.</param>
    public async Task AddAdminBySteamId(string playerSteamId, string playerName, List<string> flagsList, int immunity = 0, int time = 0, bool globalAdmin = false)
    {
	    if (databaseProvider == null) return;

        if (string.IsNullOrEmpty(playerSteamId) || flagsList.Count == 0) return;

        var now = Time.ActualDateTime();
        DateTime? futureTime;

        if (time != 0)
            futureTime = now.AddMinutes(time);
        else
            futureTime = null;

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            // Insert admin into sa_admins table
            var insertAdminSql = databaseProvider.GetAddAdminQuery();
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
                // if (flag.StartsWith($"#"))
                // {
                //     // const string sql = "SELECT id FROM `sa_groups` WHERE name = @groupName";
                //     // var groupId = await connection.QuerySingleOrDefaultAsync<int?>(sql, new { groupName = flag });
                //
                //     var sql = databaseProvider.GetGroupIdByNameQuery();
                //     var groupId = await connection.QuerySingleOrDefaultAsync<int?>(sql, new { groupName = flag, CS2_SimpleAdmin.ServerId });
                //     
                //     if (groupId != null)
                //     {
                //         var updateAdminGroup = "UPDATE `sa_admins` SET group_id = @groupId WHERE id = @adminId";
                //         await connection.ExecuteAsync(updateAdminGroup, new
                //         {
                //             groupId,
                //             adminId
                //         });
                //     }
                // }

                var insertFlagsSql = databaseProvider.GetAddAdminFlagsQuery();
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

    /// <summary>
    /// Adds a new group with flags and immunity asynchronously.
    /// </summary>
    /// <param name="groupName">Name of the group.</param>
    /// <param name="flagsList">List of flags assigned to the group.</param>
    /// <param name="immunity">Immunity level of the group.</param>
    /// <param name="globalGroup">Whether the group is global or server-specific.</param>
    public async Task AddGroup(string groupName, List<string> flagsList, int immunity = 0, bool globalGroup = false)
    {
	    if (databaseProvider == null) return;

        if (string.IsNullOrEmpty(groupName) || flagsList.Count == 0) return;

        await using var connection = await databaseProvider.CreateConnectionAsync();
        try
        {
            // Insert group into sa_groups table
            var insertGroup = databaseProvider.GetAddGroupQuery();
            var groupId = await connection.ExecuteScalarAsync<int>(insertGroup, new
            {
                groupName,
                immunity
            });

            // Insert flags into sa_groups_flags table
            foreach (var flag in flagsList)
            {
	            var insertFlagsSql = databaseProvider.GetAddGroupFlagsQuery();

                await connection.ExecuteAsync(insertFlagsSql, new
                {
                    groupId,
                    flag
                });
            }

            var insertGroupServer = databaseProvider.GetAddGroupServerQuery();
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

    /// <summary>
    /// Deletes a group by name asynchronously.
    /// </summary>
    /// <param name="groupName">Name of the group to delete.</param>
    public async Task DeleteGroup(string groupName)
    {
	    if (databaseProvider == null) return;

        if (string.IsNullOrEmpty(groupName)) return;

        await using var connection = await databaseProvider.CreateConnectionAsync();
        try
        {
	        var sql = databaseProvider.GetDeleteGroupQuery();
            await connection.ExecuteAsync(sql, new { groupName });
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError(ex.ToString());
        }
    }

    /// <summary>
    /// Deletes admins whose permissions have expired asynchronously.
    /// </summary>
    public async Task DeleteOldAdmins()
    {
	    if (databaseProvider == null) return;

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var sql = databaseProvider.GetDeleteOldAdminsQuery();
            await connection.ExecuteAsync(sql, new { CurrentTime = Time.ActualDateTime() });
        }
        catch (Exception)
        {
            CS2_SimpleAdmin._logger?.LogCritical("Unable to remove expired admins");
        }
    }
}