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
    public static readonly ConcurrentDictionary<SteamID, (DateTime? ExpirationTime, List<string> Flags)> AdminCache = new();

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

				    // Dapper returns string here, not dynamic
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
		    throw; // an empty list would strip every admin; abort the reload and keep the current permissions
	    }
    }

    /// <summary>
    /// Retrieves all groups' data including flags and immunity asynchronously.
    /// </summary>
    /// <returns>A dictionary with group names as keys and tuples of flags and immunity as values.</returns>
    private async Task<Dictionary<string, (List<string>, int)>> GetAllGroupsData()
    {
	    if (databaseProvider == null) return [];

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();
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

                if (!groupInfoDictionary.TryGetValue(groupName, out (List<string>, int) value))
                {
                    value = ([], immunity);
                    groupInfoDictionary[groupName] = value;
                }

                value.Item1.Add(flag);
            }

            return groupInfoDictionary;
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogError("Unable to load groups from database! {exception}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a JSON file containing groups data asynchronously.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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

        var options = new JsonSerializerOptions
        {
	        WriteIndented = true,
	        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(jsonData, options);
        var filePath = Path.Combine(CS2_SimpleAdmin.Instance.ModuleDirectory, "data", "groups.json");
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Creates a JSON file containing admins data asynchronously.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public async Task<List<(SteamID steamId, DateTime? ends, List<string> flags)>> CreateAdminsJsonFile()
    {
        List<(ulong identity, string name, List<string> flags, int immunity, DateTime? ends)> allPlayers = await GetAllPlayersFlags();
        var validPlayers = allPlayers
            .Where(player => SteamID.TryParse(player.identity.ToString(), out _))
            .ToList();

		var jsonData = validPlayers
			.GroupBy(player => player.name)
			.ToDictionary(
				group => group.Key,
				object (group) =>
				{
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
							if (string.IsNullOrEmpty(acc.identity) && !string.IsNullOrEmpty(player.identity.ToString()))
							{
								acc = acc with { identity = player.identity.ToString() };
							}

							acc = acc with { immunity = Math.Max(acc.immunity, player.immunity) };

							acc = acc with
							{
								flags = acc.flags.Concat(player.flags.Where(flag => flag.StartsWith($"@"))).Distinct().ToList(),
								groups = acc.groups.Concat(player.flags.Where(flag => flag.StartsWith($"#"))).Distinct().ToList()
							};

							return acc;
						});

					return consolidatedData;
				});

		var newCache = validPlayers.Select(player => (new SteamID(player.identity), player.ends, player.flags)).ToList();

		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		var json = JsonSerializer.Serialize(jsonData, options);
        var filePath = Path.Combine(CS2_SimpleAdmin.Instance.ModuleDirectory, "data", "admins.json");
        await File.WriteAllTextAsync(filePath, json);

        return newCache;
    }

    /// <summary>
    /// Removes permissions of all previously cached admins and replaces the cache with the given admins.
    /// Must run on the main thread, right before AdminManager.LoadAdminData so admins are not left stripped.
    /// </summary>
    /// <param name="admins">Admins loaded from the database.</param>
    public static void ApplyAdminCache(List<(SteamID steamId, DateTime? ends, List<string> flags)> admins)
    {
        foreach (var steamId in AdminCache.Keys.ToList())
        {
            if (!AdminCache.TryRemove(steamId, out var cached)) continue;

            var data = AdminManager.GetPlayerAdminData(steamId);
            if (data == null) continue;

            var flagsArray = cached.Flags.ToArray();
            AdminManager.RemovePlayerPermissions(steamId, flagsArray);
            AdminManager.RemovePlayerFromGroup(steamId, true, flagsArray);

            data = AdminManager.GetPlayerAdminData(steamId);
            if (data == null || (data.Flags.Count != 0 && data.Groups.Count != 0)) continue;

            AdminManager.ClearPlayerPermissions(steamId);
            AdminManager.RemovePlayerAdminData(steamId);
        }

        foreach (var (steamId, ends, flags) in admins)
        {
            AdminCache.TryAdd(steamId, (ends, flags));
        }
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
        DateTime? futureTime = time != 0 ? now.AddMinutes(time) : null;

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

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

            foreach (var flag in flagsList)
            {
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

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();

            var insertGroup = databaseProvider.GetAddGroupQuery();
            var groupId = await connection.ExecuteScalarAsync<int>(insertGroup, new
            {
                groupName,
                immunity
            });

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

        try
        {
            await using var connection = await databaseProvider.CreateConnectionAsync();
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
