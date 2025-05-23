using System.Collections.Concurrent;
using CS2_SimpleAdmin.Models;
using Dapper;
using ZLinq;

namespace CS2_SimpleAdmin.Managers;

internal class CacheManager: IDisposable
{
    private readonly ConcurrentDictionary<int, BanRecord> _banCache = [];
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
            var bans = await connection.QueryAsync<BanRecord>(
                """
                SELECT 
                        id AS Id,
                        player_name AS PlayerName,
                        player_steamid AS PlayerSteamId,
                        player_ip AS PlayerIp,
                        admin_steamid AS AdminSteamId,
                        admin_name AS AdminName,
                        reason AS Reason,
                        duration AS Duration,
                        ends AS Ends,
                        created AS Created,
                        server_id AS ServerId,
                        status AS Status,
                        updated_at AS UpdatedAt
                      FROM sa_bans
                """);
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
            var updatedBans = (await connection.QueryAsync<BanRecord>(
                "SELECT * FROM `sa_bans` WHERE updated_at > @lastUpdate OR created > @lastUpdate ORDER BY updated_at DESC",
                new { lastUpdate = _lastUpdateTime }
            )).ToList();
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

            _lastUpdateTime = DateTime.Now.AddSeconds(-1);
        }
        catch (Exception e)
        {
            // ignored
        }
    }
    
    public List<BanRecord> GetAllBans() => _banCache.Values.ToList();
    public List<BanRecord> GetActiveBans() => _banCache.Values.Where(b => b.Status == "ACTIVE").ToList();
    public List<BanRecord> GetPlayerBansBySteamId(string steamId) => _banCache.Values.Where(b => b.PlayerSteamId == steamId).ToList();
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
        var ipUInt = IpHelper.IpToUint(ipAddress);

        return _banCache.Values.Any(b =>
            b is { Status: "ACTIVE", PlayerIp: not null } &&
            IpHelper.IpToUint(b.PlayerIp) == ipUInt &&
            !_cachedIgnoredIps.Contains(ipUInt));
    }
    
    public bool IsPlayerBanned(string? steamId, string? ipAddress)
    {
        if (ipAddress == null)
            return _banCache.Values.Any(b =>
                b.Status == "ACTIVE" &&
                steamId != null &&
                b.PlayerSteamId != null &&
                b.PlayerSteamId.Equals(steamId, StringComparison.OrdinalIgnoreCase));

        if (!IpHelper.TryConvertIpToUint(ipAddress, out var ipUInt))
            return false;

        return _banCache.Values.Any(b =>
            b is { Status: "ACTIVE", PlayerIp: not null } &&
            (
                (steamId != null &&
                 b.PlayerSteamId != null &&
                 b.PlayerSteamId.Equals(steamId, StringComparison.OrdinalIgnoreCase))
                ||
                (IpHelper.TryConvertIpToUint(b.PlayerIp, out var bIpUint) &&
                 bIpUint == ipUInt &&
                 !_cachedIgnoredIps.Contains(ipUInt))
            )
        );
    }
    
    public bool IsPlayerOrAnyIpBanned(ulong steamId, string? ipAddress)
    {
        var steamIdStr = steamId.ToString();
        if (_banCache.Values.Any(b =>
                b.Status == "ACTIVE" &&
                b.PlayerSteamId?.Equals(steamIdStr, StringComparison.OrdinalIgnoreCase) == true))
        {
            return true;
        }

        if (!_playerIpsCache.TryGetValue(steamId, out var ipData))
            return false;

        var now = DateTime.Now;
        var cutoff = now.AddDays(-7);

        if (ipAddress != null)
        {
            var ipAsUint = IpHelper.IpToUint(ipAddress);

            ipData.Add(new IpRecord(
                ipAsUint,
                now.AddSeconds(-2),
                CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown"
            ));
        }

        return ipData.Any(x =>
            x.UsedAt >= cutoff &&
            !_cachedIgnoredIps.Contains(x.Ip) &&
            _banCache.Values.Any(b =>
                b is { Status: "ACTIVE", PlayerIp: not null } &&
                IpHelper.TryConvertIpToUint(b.PlayerIp, out var banIpUint) &&
                banIpUint == x.Ip
            ));
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