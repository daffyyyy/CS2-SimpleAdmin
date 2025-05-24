using System.Collections.Concurrent;
using CS2_SimpleAdmin.Models;
using Dapper;
using ZLinq;

namespace CS2_SimpleAdmin.Managers;

internal class CacheManager: IDisposable
{
    private readonly ConcurrentDictionary<int, BanRecord> _banCache = [];
    private readonly ConcurrentDictionary<string, List<BanRecord>> _steamIdIndex = [];
    private readonly ConcurrentDictionary<uint, List<BanRecord>> _ipIndex = [];

    private readonly ConcurrentDictionary<ulong, HashSet<IpRecord>> _playerIpsCache = [];
    private HashSet<uint> _cachedIgnoredIps = [];
    
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private bool _isInitialized;
    private bool _disposed;

    public async Task InitializeCacheAsync()
    {
        if (CS2_SimpleAdmin.Database == null) return;
        if (!CS2_SimpleAdmin.ServerLoaded) return;
        if (_isInitialized) return;
        
        try
        {
            Clear();
            _cachedIgnoredIps = new HashSet<uint>(
                CS2_SimpleAdmin.Instance.Config.OtherSettings.IgnoredIps
                    .Select(IpHelper.IpToUint));

            await using var connection = await CS2_SimpleAdmin.Database.GetConnectionAsync();
            List<BanRecord> bans;
            
            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                bans = (await connection.QueryAsync<BanRecord>(
                    """
                    SELECT 
                        id AS Id,
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
                        player_steamid AS PlayerSteamId,
                        player_ip AS PlayerIp,
                        status AS Status 
                    FROM sa_bans
                    WHERE server_id = @serverId
                    """, new {serverId = CS2_SimpleAdmin.ServerId})).ToList();
            }
            
            var ipHistory =
                await connection.QueryAsync<(ulong steamid, string? name, uint address, DateTime used_at)>(
                    "SELECT steamid, name, address, used_at FROM sa_players_ips ORDER BY used_at DESC");

            foreach (var ban in bans)
            {
                _banCache.TryAdd(ban.Id, ban);
            }

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
                        foreach (var ip in ipSet)
                        {
                            existingSet.Remove(ip); 
                            existingSet.Add(ip); 
                        }

                        return existingSet;
                    });
            }
            
            RebuildIndexes();
            
            _lastUpdateTime = DateTime.Now.AddSeconds(-1);
            _isInitialized = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    
    public async Task ForceReInitializeCacheAsync()
    {
        _isInitialized = false;
        
        _banCache.Clear();
        _playerIpsCache.Clear();
        _cachedIgnoredIps.Clear();
        _lastUpdateTime = DateTime.MinValue;
        
        await InitializeCacheAsync();
    }

    public async Task RefreshCacheAsync()
    {
        if (CS2_SimpleAdmin.Database == null) return;
        if (!_isInitialized) return;

        try
        {
            await using var connection = await CS2_SimpleAdmin.Database.GetConnectionAsync();
            List<BanRecord> updatedBans;
            
            var allIds = (await connection.QueryAsync<int>("SELECT id FROM sa_bans")).ToHashSet();

            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                updatedBans = (await connection.QueryAsync<BanRecord>(
                    """
                    SELECT id AS Id,
                    player_steamid AS PlayerSteamId,
                    player_ip AS PlayerIp,
                    status AS Status 
                    FROM `sa_bans` WHERE updated_at > @lastUpdate OR created > @lastUpdate ORDER BY updated_at DESC
                    """,
                    new { lastUpdate = _lastUpdateTime }
                )).ToList();
            }
            else
            {
                updatedBans = (await connection.QueryAsync<BanRecord>(
                    """
                    SELECT id AS Id,
                    player_steamid AS PlayerSteamId,
                    player_ip AS PlayerIp,
                    status AS Status 
                    FROM `sa_bans` WHERE (updated_at > @lastUpdate OR created > @lastUpdate) AND server_id = @serverId ORDER BY updated_at DESC
                    """,
                    new { lastUpdate = _lastUpdateTime, serverId = CS2_SimpleAdmin.ServerId }
                )).ToList();
            }
            
            foreach (var id in _banCache.Keys)
            {
                if (allIds.Contains(id) || !_banCache.TryRemove(id, out var ban)) continue;
                
                // Remove from steamIdIndex
                if (!string.IsNullOrWhiteSpace(ban.PlayerSteamId) &&
                    _steamIdIndex.TryGetValue(ban.PlayerSteamId, out var steamBans))
                {
                    steamBans.RemoveAll(b => b.Id == id);
                    if (steamBans.Count == 0)
                        _steamIdIndex.TryRemove(ban.PlayerSteamId, out _);
                }

                // Remove from ipIndex
                if (!string.IsNullOrWhiteSpace(ban.PlayerIp) &&
                    IpHelper.TryConvertIpToUint(ban.PlayerIp, out var ipUInt) &&
                    _ipIndex.TryGetValue(ipUInt, out var ipBans))
                {
                    ipBans.RemoveAll(b => b.Id == id);
                    if (ipBans.Count == 0)
                        _ipIndex.TryRemove(ipUInt, out _);
                }
            }
            
            var ipHistory = (await connection.QueryAsync<(ulong steamid, string? name, uint address, DateTime used_at)>(
                "SELECT steamid, name, address, used_at FROM sa_players_ips WHERE used_at >= @lastUpdate ORDER BY used_at DESC LIMIT 300", new {lastUpdate = _lastUpdateTime})).ToList();

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
            
            if (updatedBans.Count == 0)
                return;
            
            foreach (var ban in updatedBans)
            {
                _banCache.AddOrUpdate(ban.Id, ban, (_, _) => ban);
            }
            
            RebuildIndexes();
            _lastUpdateTime = DateTime.Now.AddSeconds(-1);
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    private void RebuildIndexes()
    {
        _steamIdIndex.Clear();
        _ipIndex.Clear();
        
        foreach (var ban in _banCache.Values)
        {
            if (ban.Status != "ACTIVE")
                continue;

            if (!string.IsNullOrWhiteSpace(ban.PlayerSteamId))
            {
                var steamId = ban.PlayerSteamId;
                _steamIdIndex.AddOrUpdate(
                    steamId,
                    key => [ban],
                    (key, list) =>
                    {
                        list.Add(ban);
                        return list;
                    });
            }

            if (ban.PlayerIp != null &&
                IpHelper.TryConvertIpToUint(ban.PlayerIp, out var ipUInt))
            {
                _ipIndex.AddOrUpdate(
                    ipUInt,
                    key => [ban],
                    (key, list) =>
                    {
                        list.Add(ban);
                        return list;
                    });
            }
        }
    }
    
    public List<BanRecord> GetAllBans() => _banCache.Values.ToList();
    public List<BanRecord> GetActiveBans() => _banCache.Values.Where(b => b.Status == "ACTIVE").ToList();
    public List<BanRecord> GetPlayerBansBySteamId(string steamId) => _steamIdIndex.TryGetValue(steamId, out var bans) ? bans : [];
    public List<(ulong SteamId, DateTime UsedAt, string PlayerName)> GetAccountsByIp(string ipAddress)
    {
        var ipAsUint = IpHelper.IpToUint(ipAddress);

        return _playerIpsCache.AsValueEnumerable()
            .SelectMany(kvp => kvp.Value
                .Where(entry => entry.Ip == ipAsUint)
                .Select(entry => (kvp.Key, entry.UsedAt, entry.PlayerName)))
            .ToList();
    }

    private bool IsIpBanned(string ipAddress)
    {
        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0) return false;
        var ipUInt = IpHelper.IpToUint(ipAddress);
        return !_cachedIgnoredIps.Contains(ipUInt) && _ipIndex.ContainsKey(ipUInt);
    }

    
    public bool IsPlayerBanned(string? steamId, string? ipAddress)
    {
        if (steamId != null && _steamIdIndex.ContainsKey(steamId))
            return true;
        
        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0) return false;

        if (ipAddress == null)
            return false;

        if (!IpHelper.TryConvertIpToUint(ipAddress, out var ipUInt))
            return false;

        return !_cachedIgnoredIps.Contains(ipUInt) &&
               _ipIndex.ContainsKey(ipUInt);
    }
    
    public bool IsPlayerOrAnyIpBanned(ulong steamId, string? ipAddress)
    {
        var steamIdStr = steamId.ToString();
        
        if (_steamIdIndex.ContainsKey(steamIdStr))
            return true;
        
        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.BanType == 0) return false;

        if (!_playerIpsCache.TryGetValue(steamId, out var ipData))
            return false;

        var now = DateTime.Now;
        var cutoff = now.AddDays(-7);

        if (ipAddress != null)
        {
            var ipAsUint = IpHelper.IpToUint(ipAddress);

            if (!_cachedIgnoredIps.Contains(ipAsUint))
            {
                ipData.Add(new IpRecord(
                    ipAsUint,
                    now.AddSeconds(-2), // artificially recent
                    CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown"
                ));
            }
        }

        foreach (var ipRecord in ipData)
        {
            if (ipRecord.UsedAt < cutoff || _cachedIgnoredIps.Contains(ipRecord.Ip))
                continue;

            if (_ipIndex.ContainsKey(ipRecord.Ip))
                return true;
        }

        return false;
    }
    
    public bool HasIpForPlayer(ulong steamId, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return _playerIpsCache.TryGetValue(steamId, out var ipData) 
               && ipData.Any(x => x.Ip == IpHelper.IpToUint(ipAddress));
    }

    private void Clear()
    {
        _steamIdIndex.Clear();
        _ipIndex.Clear();

        _banCache.Clear();
        _playerIpsCache.Clear();
        _cachedIgnoredIps.Clear();
    }
    
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