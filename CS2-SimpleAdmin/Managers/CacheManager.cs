using System.Collections.Concurrent;
using CS2_SimpleAdmin.Database;
using CS2_SimpleAdmin.Models;
using Dapper;
using ZLinq;

namespace CS2_SimpleAdmin.Managers;

internal class CacheManager: IDisposable
{
    private readonly ConcurrentDictionary<int, BanRecord> _banCache = [];
    private readonly ConcurrentDictionary<ulong, List<BanRecord>> _steamIdIndex = [];
    private readonly ConcurrentDictionary<uint, List<BanRecord>> _ipIndex = [];

    private readonly ConcurrentDictionary<ulong, HashSet<IpRecord>> _playerIpsCache = [];
    private HashSet<uint> _cachedIgnoredIps = [];
    
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private bool _isInitialized;
    private bool _disposed;
    
    /// <summary>
    /// Initializes and builds the ban and IP cache from the database. Loads bans, player IP history, and config settings.
    /// </summary>
    /// <returns>Asynchronous task representing the initialization process.</returns>
    public async Task InitializeCacheAsync()
    {
        if (CS2_SimpleAdmin.DatabaseProvider == null) return;
        if (!CS2_SimpleAdmin.ServerLoaded) return;
        if (_isInitialized) return;
        
        try
        {
            Clear();
            _cachedIgnoredIps = CS2_SimpleAdmin.Instance.Config.OtherSettings.IgnoredIps
                .AsValueEnumerable()
                .Select(IpHelper.IpToUint)
                .ToHashSet();

            await using var connection = await CS2_SimpleAdmin.DatabaseProvider.CreateConnectionAsync();
            List<BanRecord> bans;
            
            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                bans = (await connection.QueryAsync<BanRecord>(
                    """
                    SELECT 
                        id AS Id,
                        player_name AS PlayerName,
                        player_steamid AS PlayerSteamId,
                        player_ip AS PlayerIp,
                        status AS Status 
                    FROM sa_bans
                    """)).ToList();
            }
            else
            {
                bans = (await connection.QueryAsync<BanRecord>(
                    """
                    SELECT 
                        id AS Id,
                        player_name AS PlayerName,
                        player_steamid AS PlayerSteamId,
                        player_ip AS PlayerIp,
                        status AS Status 
                    FROM sa_bans
                    WHERE server_id = @serverId
                    """, new {serverId = CS2_SimpleAdmin.ServerId})).ToList();
            }

            if (CS2_SimpleAdmin.Instance.Config.OtherSettings.CheckMultiAccountsByIp)
            {
                // Optimization: Load IP history and build cache in single pass
                var ipHistory = await connection.QueryAsync<(ulong steamid, string? name, uint address, DateTime used_at)>(
                    "SELECT steamid, name, address, used_at FROM sa_players_ips ORDER BY steamid, address, used_at DESC");

                var unknownName = CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
                var currentSteamId = 0UL;
                var currentIpSet = new HashSet<IpRecord>(new IpRecordComparer());
                var latestIpTimestamps = new Dictionary<uint, DateTime>();

                foreach (var record in ipHistory)
                {
                    // When we encounter a new steamid, save the previous one
                    if (record.steamid != currentSteamId && currentSteamId != 0)
                    {
                        _playerIpsCache[currentSteamId] = currentIpSet;
                        currentIpSet = new HashSet<IpRecord>(new IpRecordComparer());
                        latestIpTimestamps.Clear();
                    }

                    currentSteamId = record.steamid;

                    // Only keep the latest timestamp for each IP
                    if (!latestIpTimestamps.TryGetValue(record.address, out var existingTimestamp) ||
                        record.used_at > existingTimestamp)
                    {
                        latestIpTimestamps[record.address] = record.used_at;
                        currentIpSet.Add(new IpRecord(
                            record.address,
                            record.used_at,
                            string.IsNullOrEmpty(record.name) ? unknownName : record.name
                        ));
                    }
                }

                // Don't forget the last steamid
                if (currentSteamId != 0)
                {
                    _playerIpsCache[currentSteamId] = currentIpSet;
                }
            }

            foreach (var ban in bans.AsValueEnumerable())
                _banCache.TryAdd(ban.Id, ban);
            
            RebuildIndexes();
            
            _lastUpdateTime = Time.ActualDateTime().AddSeconds(-1);
            _isInitialized = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    
    /// <summary>
    /// Clears all cached data and reinitializes the cache from the database.
    /// </summary>
    /// <returns>Asynchronous task representing the reinitialization process.</returns>
    public async Task ForceReInitializeCacheAsync()
    {
        _isInitialized = false;
        
        _banCache.Clear();
        _playerIpsCache.Clear();
        _cachedIgnoredIps = [];
        _lastUpdateTime = DateTime.MinValue;
        
        await InitializeCacheAsync();
    }
    
    /// <summary>
    /// Refreshes the in-memory cache with updated or new data from the database since the last update time.
    /// Also updates multi-account IP history if enabled.
    /// </summary>
    /// <returns>Asynchronous task representing the refresh operation.</returns>
    public async Task RefreshCacheAsync()
    {
        if (CS2_SimpleAdmin.DatabaseProvider == null) return;
        if (!_isInitialized) return;

        try
        {
            await using var connection = await CS2_SimpleAdmin.DatabaseProvider.CreateConnectionAsync();
            IEnumerable<BanRecord> updatedBans;

            // Optimization: Only get IDs for comparison if we need to check for deletions
            // Most of the time bans are just added/updated, not deleted
            HashSet<int>? allIds = null;

            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                updatedBans = (await connection.QueryAsync<BanRecord>(
                    """
                    SELECT id AS Id,
                    player_name AS PlayerName,
                    player_steamid AS PlayerSteamId,
                    player_ip AS PlayerIp,
                    status AS Status
                    FROM `sa_bans` WHERE updated_at > @lastUpdate OR created > @lastUpdate ORDER BY updated_at DESC
                    """,
                    new { lastUpdate = _lastUpdateTime }
                ));

                // Optimization: Only fetch all IDs if there were updates
                var updatedList = updatedBans.ToList();
                if (updatedList.Count > 0)
                {
                    allIds = (await connection.QueryAsync<int>("SELECT id FROM sa_bans")).ToHashSet();
                }
                updatedBans = updatedList;
            }
            else
            {
                updatedBans = (await connection.QueryAsync<BanRecord>(
                    """
                    SELECT id AS Id,
                    player_name AS PlayerName,
                    player_steamid AS PlayerSteamId,
                    player_ip AS PlayerIp,
                    status AS Status
                    FROM `sa_bans` WHERE (updated_at > @lastUpdate OR created > @lastUpdate) AND server_id = @serverId ORDER BY updated_at DESC
                    """,
                    new { lastUpdate = _lastUpdateTime, serverId = CS2_SimpleAdmin.ServerId }
                ));

                // Optimization: Only fetch all IDs if there were updates
                var updatedList = updatedBans.ToList();
                if (updatedList.Count > 0)
                {
                    allIds = (await connection.QueryAsync<int>(
                        "SELECT id FROM sa_bans WHERE server_id = @serverId",
                        new { serverId = CS2_SimpleAdmin.ServerId }
                    )).ToHashSet();
                }
                updatedBans = updatedList;
            }

            // Optimization: Only process deletions if we have the full ID list
            if (allIds != null)
            {
                foreach (var id in _banCache.Keys)
                {
                    if (allIds.Contains(id) || !_banCache.TryRemove(id, out var ban)) continue;

                    if (ban.PlayerSteamId != null &&
                        _steamIdIndex.TryGetValue(ban.PlayerSteamId.Value, out var steamBans))
                    {
                        steamBans.RemoveAll(b => b.Id == id);
                        if (steamBans.Count == 0)
                            _steamIdIndex.TryRemove(ban.PlayerSteamId.Value, out _);
                    }

                    if (string.IsNullOrWhiteSpace(ban.PlayerIp) ||
                        !IpHelper.TryConvertIpToUint(ban.PlayerIp, out var ipUInt) ||
                        !_ipIndex.TryGetValue(ipUInt, out var ipBans)) continue;
                    {
                        ipBans.RemoveAll(b => b.Id == id);
                        if (ipBans.Count == 0)
                            _ipIndex.TryRemove(ipUInt, out _);
                    }
                }
            }
            
            if (CS2_SimpleAdmin.Instance.Config.OtherSettings.CheckMultiAccountsByIp)
            {
                var ipHistory = (await connection.QueryAsync<(ulong steamid, string? name, uint address, DateTime used_at)>(
                    "SELECT steamid, name, address, used_at FROM sa_players_ips WHERE used_at >= @lastUpdate ORDER BY used_at DESC LIMIT 300",
                    new { lastUpdate = _lastUpdateTime }));

                foreach (var group in ipHistory.AsValueEnumerable().GroupBy(x => x.steamid))
                {
                    var ipSet = new HashSet<IpRecord>(
                        group
                            .GroupBy(x => x.address)
                            .Select(g =>
                            {
                                var latest = g.MaxBy(x => x.used_at);
                                return new IpRecord(
                                    g.Key,
                                    latest.used_at,
                                    !string.IsNullOrEmpty(latest.name)
                                        ? latest.name
                                        : CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown"
                                );
                            }),
                        new IpRecordComparer()
                    );

                    _playerIpsCache.AddOrUpdate(
                        group.Key,
                        _ => ipSet,
                        (_, existingSet) =>
                        {
                            foreach (var newEntry in ipSet)
                            {
                                existingSet.Remove(newEntry);
                                existingSet.Add(newEntry);
                            }

                            return existingSet;
                        });
                }
            }

            // Update cache with new/modified bans
            var hasUpdates = false;
            foreach (var ban in updatedBans)
            {
                _banCache.AddOrUpdate(ban.Id, ban, (_, _) => ban);
                hasUpdates = true;
            }

            // Always rebuild indexes if there were any updates
            // This ensures status changes (ACTIVE -> UNBANNED) are reflected
            if (hasUpdates)
            {
                RebuildIndexes();
            }

            _lastUpdateTime = Time.ActualDateTime().AddSeconds(-1);
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Rebuilds the internal indexes for fast lookup of active bans by Steam ID and IP address.
    /// Clears and repopulates both indexes based on the current in-memory ban cache.
    /// </summary>
    private void RebuildIndexes()
    {
        _steamIdIndex.Clear();
        _ipIndex.Clear();

        // Optimization: Cache config value to avoid repeated property access
        var banType = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType;
        var checkIpBans = banType != 0;

        // Optimization: Pre-filter only ACTIVE bans to avoid checking status in loop
        var activeBans = _banCache.Values.Where(b => b.StatusEnum == BanStatus.ACTIVE);

        foreach (var ban in activeBans)
        {
            // Index by Steam ID
            if (ban.PlayerSteamId.HasValue)
            {
                var steamId = ban.PlayerSteamId.Value;
                if (!_steamIdIndex.TryGetValue(steamId, out var steamList))
                {
                    steamList = new List<BanRecord>();
                    _steamIdIndex[steamId] = steamList;
                }
                steamList.Add(ban);
            }

            // Index by IP (only if IP bans are enabled)
            if (checkIpBans && !string.IsNullOrEmpty(ban.PlayerIp) &&
                IpHelper.TryConvertIpToUint(ban.PlayerIp, out var ipUInt))
            {
                if (!_ipIndex.TryGetValue(ipUInt, out var ipList))
                {
                    ipList = new List<BanRecord>();
                    _ipIndex[ipUInt] = ipList;
                }
                ipList.Add(ban);
            }
        }
    }
    
    /// <summary>
    /// Retrieves all ban records currently stored in the cache.
    /// </summary>
    /// <returns>List of all <see cref="BanRecord"/> objects.</returns>
    public List<BanRecord> GetAllBans() => _banCache.Values.ToList();
    
    /// <summary>
    /// Retrieves only active ban records from the cache.
    /// </summary>
    /// <returns>List of active <see cref="BanRecord"/> objects.</returns>
    public List<BanRecord> GetActiveBans() => _banCache.Values.Where(b => b.StatusEnum == BanStatus.ACTIVE).ToList();
    
    /// <summary>
    /// Retrieves all ban records for a specific player by their Steam ID.
    /// </summary>
    /// <param name="steamId">64-bit Steam ID of the player.</param>
    /// <returns>List of <see cref="BanRecord"/> objects associated with the Steam ID.</returns>
    public List<BanRecord> GetPlayerBansBySteamId(ulong steamId) => _steamIdIndex.TryGetValue(steamId, out var bans) ? bans : [];
    
    /// <summary>
    /// Gets all known Steam accounts that have used the specified IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to search for, in string format.</param>
    /// <returns>
    /// List of tuples containing the Steam ID, last used time, and player name for each matching entry.
    /// </returns>
    public List<(ulong SteamId, DateTime UsedAt, string PlayerName)> GetAccountsByIp(string ipAddress)
    {
        var ipAsUint = IpHelper.IpToUint(ipAddress);
        var results = new List<(ulong, DateTime, string)>();

        // Optimization: Direct lookup using HashSet.Contains instead of TryGetValue
        var searchRecord = new IpRecord(ipAsUint, default, null!);

        foreach (var (steamId, ipSet) in _playerIpsCache)
        {
            // Optimization: Single pass through the set
            foreach (var entry in ipSet)
            {
                if (entry.Ip == ipAsUint)
                {
                    results.Add((steamId, entry.UsedAt, entry.PlayerName));
                }
            }
        }

        return results;
    }

    // public IEnumerable<(ulong SteamId, DateTime UsedAt, string PlayerName)> GetAccountsByIp(string ipAddress)
    // {
    //     var ipAsUint = IpHelper.IpToUint(ipAddress);
    //
    //     return _playerIpsCache.SelectMany(kvp => kvp.Value
    //         .Where(entry => entry.Ip == ipAsUint)
    //         .Select(entry => (kvp.Key, entry.UsedAt, entry.PlayerName)));
    // }

    private bool IsIpBanned(string ipAddress)
    {
        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0) return false;
        var ipUInt = IpHelper.IpToUint(ipAddress);
        return !_cachedIgnoredIps.Contains(ipUInt) && _ipIndex.ContainsKey(ipUInt);
    }
    
    // public bool IsPlayerBanned(ulong? steamId, string? ipAddress)
    // {
    //     if (steamId != null && _steamIdIndex.ContainsKey(steamId.Value))
    //         return true;
    //     
    //     if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0)
    //         return false;
    //
    //     if (string.IsNullOrEmpty(ipAddress) || !IpHelper.TryConvertIpToUint(ipAddress, out var ipUInt))
    //         return false;
    //
    //     return !_cachedIgnoredIps.Contains(ipUInt) && _ipIndex.ContainsKey(ipUInt);
    // }

    /// <summary>
    /// Checks if a player is currently banned by Steam ID or IP address.
    /// If a partial ban record is found, updates it with the latest player information.
    /// </summary>
    /// <param name="playerName">Name of the player attempting to connect.</param>
    /// <param name="steamId">Optional 64-bit Steam ID of the player.</param>
    /// <param name="ipAddress">Optional IP address of the player.</param>
    /// <returns>True if the player is banned, otherwise false.</returns>
    public bool IsPlayerBanned(string playerName, ulong? steamId, string? ipAddress)
    {
        BanRecord? record;
        if (steamId.HasValue && _steamIdIndex.TryGetValue(steamId.Value, out var steamRecords))
        {
            record = steamRecords.FirstOrDefault(r => r.StatusEnum == BanStatus.ACTIVE);
            if (record != null)
            {
                if ((string.IsNullOrEmpty(record.PlayerIp) && !string.IsNullOrEmpty(ipAddress)) ||
                    (!record.PlayerSteamId.HasValue))
                {
                    _ = Task.Run(() => UpdatePlayerData(playerName, steamId, ipAddress));
                }
                
                return true;
            }
        }
        
        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0)
            return false;

        if (string.IsNullOrEmpty(ipAddress) ||
            !IpHelper.TryConvertIpToUint(ipAddress, out var ipUInt) ||
            _cachedIgnoredIps.Contains(ipUInt) ||
            !_ipIndex.TryGetValue(ipUInt, out var ipRecords)) return false;
        
        record = ipRecords.FirstOrDefault(r => r.StatusEnum == BanStatus.ACTIVE);
        if (record == null) return false;
        if ((string.IsNullOrEmpty(record.PlayerIp) && !string.IsNullOrEmpty(ipAddress)) ||
            (!record.PlayerSteamId.HasValue && steamId.HasValue))
        {
            _ = Task.Run(() => UpdatePlayerData(playerName, steamId, ipAddress));
        }
        
        return true;
    }
    
    // public bool IsPlayerOrAnyIpBanned(ulong steamId, string? ipAddress)
    // {
    //     if (_steamIdIndex.ContainsKey(steamId))
    //         return true;
    //     
    //     if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0)
    //         return false;
    //
    //     if (!_playerIpsCache.TryGetValue(steamId, out var ipData))
    //         return false;
    //
    //     // var now = Time.ActualDateTime();
    //     var cutoff = Time.ActualDateTime().AddDays(-CS2_SimpleAdmin.Instance.Config.OtherSettings.ExpireOldIpBans);
    //     var unknownName = CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
    //     
    //     if (ipAddress != null && IpHelper.TryConvertIpToUint(ipAddress, out var ipAsUint))
    //     {
    //         if (!_cachedIgnoredIps.Contains(ipAsUint))
    //         {
    //             ipData.Add(new IpRecord(
    //                 ipAsUint,
    //                 Time.ActualDateTime().AddSeconds(-2),
    //                 unknownName
    //             ));
    //         }
    //     }
    //     
    //     // foreach (var ipRecord in ipData)
    //     // {
    //     //     // Skip if too old or in ignored list
    //     //     if (ipRecord.UsedAt < cutoff || _cachedIgnoredIps.Contains(ipRecord.Ip))
    //     //         continue;
    //     //
    //     //     // Check if IP is banned
    //     //     if (_ipIndex.ContainsKey(ipRecord.Ip))
    //     //         return true;
    //     // }
    //
    //     foreach (var ipRecord in ipData)
    //     {
    //         if (ipRecord.UsedAt < cutoff || _cachedIgnoredIps.Contains(ipRecord.Ip))
    //             continue;
    //
    //         if (!_ipIndex.TryGetValue(ipRecord.Ip, out var banRecords)) continue;
    //         
    //         var activeBan = banRecords.FirstOrDefault(r => r.StatusEnum == BanStatus.ACTIVE);
    //         if (activeBan == null) continue;
    //         
    //         if (!string.IsNullOrEmpty(activeBan.PlayerName) && activeBan.PlayerSteamId.HasValue) return true;
    //                 
    //         _ = Task.Run(() => UpdatePlayerData(
    //             activeBan.PlayerName,
    //             steamId,
    //             ipAddress
    //         ));
    //
    //         if (string.IsNullOrEmpty(activeBan.PlayerName) && !string.IsNullOrEmpty(unknownName))
    //             activeBan.PlayerName = unknownName;
    //         
    //         activeBan.PlayerSteamId ??= steamId;
    //
    //         return true;
    //     }
    //
    //     return false;
    // }

    /// <summary>
    /// Checks if the player or any IP previously associated with them is currently banned.
    /// Also updates ban records with missing player info if found.
    /// </summary>
    /// <param name="playerName">Current player name.</param>
    /// <param name="steamId">64-bit Steam ID of the player.</param>
    /// <param name="ipAddress">Current IP address of the player (optional).</param>
    /// <returns>True if the player or their known IPs are banned, otherwise false.</returns>
    public bool IsPlayerOrAnyIpBanned(string playerName, ulong steamId, string? ipAddress)
    {
        if (_steamIdIndex.TryGetValue(steamId, out var steamBans))
        {
            var activeBan = steamBans.FirstOrDefault(b => b.StatusEnum == BanStatus.ACTIVE);
            if (activeBan != null)
            {
                if (string.IsNullOrEmpty(activeBan.PlayerName) || string.IsNullOrEmpty(activeBan.PlayerIp))
                    _ = Task.Run(() => UpdatePlayerData(playerName, steamId, ipAddress));
                
                return true;
            }
        }

        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0)
            return false;

        if (!_playerIpsCache.TryGetValue(steamId, out var ipData))
            return false;

        var cutoff = Time.ActualDateTime().AddDays(-CS2_SimpleAdmin.Instance.Config.OtherSettings.ExpireOldIpBans);
        var unknownName = CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";

        if (ipAddress != null && IpHelper.TryConvertIpToUint(ipAddress, out var ipAsUint))
        {
            if (!_cachedIgnoredIps.Contains(ipAsUint))
            {
                ipData.Add(new IpRecord(ipAsUint, Time.ActualDateTime().AddSeconds(-2), unknownName));
            }
        }

        foreach (var ipRecord in ipData)
        {
            if (ipRecord.UsedAt < cutoff || _cachedIgnoredIps.Contains(ipRecord.Ip))
                continue;

            if (!_ipIndex.TryGetValue(ipRecord.Ip, out var banRecords)) 
                continue;

            var activeBan = banRecords.FirstOrDefault(r => r.StatusEnum == BanStatus.ACTIVE);
            if (activeBan == null) 
                continue;

            if (string.IsNullOrEmpty(activeBan.PlayerName))
                activeBan.PlayerName = unknownName;
            
            activeBan.PlayerSteamId ??= steamId;

            _ = Task.Run(() => UpdatePlayerData(playerName, steamId, ipAddress));

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the given IP address is known (previously recorded) for the specified Steam ID.
    /// </summary>
    /// <param name="steamId">64-bit Steam ID of the player.</param>
    /// <param name="ipAddress">IP address to check.</param>
    /// <returns>True if the IP is recorded for the player, otherwise false.</returns>
    public bool HasIpForPlayer(ulong steamId, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;
        
        if (!IpHelper.TryConvertIpToUint(ipAddress, out var ipUint))
            return false;

        return _playerIpsCache.TryGetValue(steamId, out var ipData) && 
               ipData.Contains(new IpRecord(ipUint, default, null!));
    }
    
    // public bool HasIpForPlayer(ulong steamId, string ipAddress)
    // {
    //     if (string.IsNullOrWhiteSpace(ipAddress))
    //         return false;
    //
    //     return _playerIpsCache.TryGetValue(steamId, out var ipData) 
    //            && ipData.Any(x => x.Ip == IpHelper.IpToUint(ipAddress));
    // }

    /// <summary>
    /// Updates existing active ban records in the database with the latest known player name and IP address.
    /// Also updates in-memory cache to reflect these changes.
    /// </summary>
    /// <param name="playerName">Current player name.</param>
    /// <param name="steamId">Optional Steam ID of the player.</param>
    /// <param name="ipAddress">Optional IP address of the player.</param>
    /// <returns>Asynchronous task representing the update operation.</returns>
    private async Task UpdatePlayerData(string? playerName, ulong? steamId, string? ipAddress)
    {
        if (CS2_SimpleAdmin.DatabaseProvider == null)
            return;

        var baseSql = """
                          UPDATE sa_bans
                          SET 
                              player_ip = COALESCE(player_ip, @PlayerIP),
                              player_name = COALESCE(player_name, @PlayerName)
                          WHERE 
                              (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
                              AND status = 'ACTIVE'
                              AND (duration = 0 OR ends > @CurrentTime)
                      """;

        if (!CS2_SimpleAdmin.Instance.Config.MultiServerMode)
        {
            baseSql += " AND server_id = @ServerId;";
        }

        var parameters = new
        {
            PlayerSteamID = steamId,
            PlayerIP = CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0
                       || string.IsNullOrEmpty(ipAddress)
                       || CS2_SimpleAdmin.Instance.Config.OtherSettings.IgnoredIps.Contains(ipAddress)
                ? null
                : ipAddress,
            PlayerName = string.IsNullOrEmpty(playerName) ? string.Empty : playerName,
            CurrentTime = Time.ActualDateTime(),
            CS2_SimpleAdmin.ServerId
        };
        
        await using var connection = await CS2_SimpleAdmin.DatabaseProvider.CreateConnectionAsync();
        await connection.ExecuteAsync(baseSql, parameters);
        
        if (steamId.HasValue && _steamIdIndex.TryGetValue(steamId.Value, out var steamRecords))
        {
            foreach (var rec in steamRecords.Where(r => r.StatusEnum == BanStatus.ACTIVE))
            {
                if (string.IsNullOrEmpty(rec.PlayerIp) && !string.IsNullOrEmpty(ipAddress))
                    rec.PlayerIp = ipAddress;

                if (string.IsNullOrEmpty(rec.PlayerName) && !string.IsNullOrEmpty(playerName))
                    rec.PlayerName = playerName;
            }
        }

        if (!string.IsNullOrEmpty(ipAddress) && IpHelper.TryConvertIpToUint(ipAddress, out var ipUInt) 
                                             && _ipIndex.TryGetValue(ipUInt, out var ipRecords))
        {
            foreach (var rec in ipRecords.Where(r => r.StatusEnum == BanStatus.ACTIVE))
            {
                if (!rec.PlayerSteamId.HasValue && steamId.HasValue)
                    rec.PlayerSteamId = steamId;

                if (string.IsNullOrEmpty(rec.PlayerName) && !string.IsNullOrEmpty(playerName))
                    rec.PlayerName = playerName;
            }
        }
    }

    private void Clear()
    {
        _steamIdIndex.Clear();
        _ipIndex.Clear();

        _banCache.Clear();
        _playerIpsCache.Clear();
        _cachedIgnoredIps.Clear();
    }
    
    /// <summary>
    /// Clears and disposes of all cached data and marks the object as disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        Clear();
        _disposed = true;
    }
}

public class IpRecordComparer : IEqualityComparer<IpRecord>
{
    public bool Equals(IpRecord x, IpRecord y)
        => x.Ip == y.Ip;

    public int GetHashCode(IpRecord obj)
        => obj.Ip.GetHashCode();
}