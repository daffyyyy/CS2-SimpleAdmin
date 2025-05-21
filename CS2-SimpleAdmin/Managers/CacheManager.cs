using System.Collections.Concurrent;
using CS2_SimpleAdmin.Models;
using Dapper;
using ZLinq;

namespace CS2_SimpleAdmin.Managers;

internal class CacheManager
{
    private readonly ConcurrentDictionary<int, BanRecord> _banCache = new();
    private readonly ConcurrentDictionary<ulong, (HashSet<string> ips, DateTime usedAt, string playerName)> _playerIpsCache = new();
    private HashSet<string> _cachedIgnoredIps;
    
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private bool _isInitialized;

    public async Task InitializeCacheAsync()
    {
        if (CS2_SimpleAdmin.Database == null) return;
        if (!CS2_SimpleAdmin.ServerLoaded) return;
        if (_isInitialized) return;

        _cachedIgnoredIps = [..CS2_SimpleAdmin.Instance.Config.OtherSettings.IgnoredIps];

        try
        {
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
                await connection.QueryAsync<(ulong steamid, string? name, string address, DateTime used_at)>(
                    "SELECT steamid, name, address, used_at FROM sa_players_ips ORDER BY used_at DESC");


            foreach (var ban in bans)
            {
                _banCache.TryAdd(ban.Id, ban);
            }

            foreach (var group in ipHistory.GroupBy(x => x.steamid))
            {
                var ips = new HashSet<string>(group.Select(x => x.address));
                var lastUsed = group.Max(x => x.used_at);
                var playerName = group.FirstOrDefault(x => !string.IsNullOrEmpty(x.name)).name
                                 ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";

                _playerIpsCache[group.Key] = (ips, lastUsed, playerName);
            }

            _lastUpdateTime = DateTime.Now;
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
            )).ToList().AsValueEnumerable();
            var ipHistory = (await connection.QueryAsync<(ulong steamid, string? name, string address, DateTime used_at)>(
                "SELECT steamid, name, address, used_at FROM sa_players_ips ORDER BY used_at DESC LIMIT 500")).ToList();

            // foreach (var group in ipHistory.GroupBy(x => x.steamid))
            // {
            //     var ips = new HashSet<string>(group.Select(x => x.address));
            //     var lastUsed = group.Max(x => x.used_at);
            //     _playerIpsCache[group.Key] = (ips, lastUsed);
            // }
            
            var groupedData = ipHistory.AsValueEnumerable()
                .GroupBy(x => x.steamid)
                .ToList();
            
            groupedData.ForEach(group =>
            {
                var ips = new HashSet<string>(
                    group.Select(x => x.address), 
                    StringComparer.OrdinalIgnoreCase
                );
    
                var lastUsed = group.Max(x => x.used_at);
                var playerName = group
                                     .OrderByDescending(x => x.used_at)  // Prefer newer records
                                     .Select(x => x.name)
                                     .FirstOrDefault(name => !string.IsNullOrEmpty(name))
                                 ?? CS2_SimpleAdmin._localizer?["sa_unknown"] 
                                 ?? "Unknown";

                _playerIpsCache.AddOrUpdate(
                    group.Key,
                    // Add new entry
                    _ => (ips, lastUsed, playerName),
                    // Update existing
                    (_, existing) => 
                    {
                        existing.ips.UnionWith(ips); 
                        return (
                            existing.ips,
                            lastUsed > existing.usedAt ? lastUsed : existing.usedAt,
                            string.IsNullOrEmpty(existing.playerName) ? playerName : existing.playerName
                        );
                    });
            });

            if (updatedBans.Count() == 0)
                return;
            
            foreach (var ban in updatedBans)
            {
                _banCache.AddOrUpdate(ban.Id, ban, (_, _) => ban);
            }

            _lastUpdateTime = DateTime.Now;
        }
        catch (Exception e)
        {
            // ignored
        }
    }
    
    public List<BanRecord> GetAllBans() => _banCache.Values.ToList();
    public List<BanRecord> GetActiveBans() => _banCache.Values.Where(b => b.Status == "ACTIVE").ToList();
    public List<BanRecord> GetPlayerBansBySteamId(string steamId) => _banCache.Values.Where(b => b.PlayerSteamId == steamId).ToList();
    public List<(ulong SteamId, string PlayerName)> GetAccountsByIp(string ipAddress)
    {
        return _playerIpsCache.AsValueEnumerable()
            .Where(kvp => kvp.Value.ips.Contains(ipAddress))
            .Select(kvp => (kvp.Key, kvp.Value.playerName))
            .ToList();
    }

    private bool IsIpBanned(string ipAddress)
    {
        return _banCache.Values.Any(b => 
            b.Status == "ACTIVE" && 
            !string.IsNullOrEmpty(b.PlayerIp) &&
            b.PlayerIp.Equals(ipAddress, StringComparison.OrdinalIgnoreCase) &&
            !_cachedIgnoredIps.Contains(ipAddress));
    }
    
    public bool IsPlayerBanned(string? steamId, string? ipAddress) => 
        _banCache.Values.Any(b =>
            b.Status == "ACTIVE" && (
                (steamId != null && 
                 b.PlayerSteamId != null && 
                 b.PlayerSteamId.Equals(steamId, StringComparison.OrdinalIgnoreCase)) ||
                (ipAddress != null && 
                 b.PlayerIp != null && 
                 b.PlayerIp.Equals(ipAddress, StringComparison.OrdinalIgnoreCase) &&
                 !_cachedIgnoredIps.Contains(ipAddress))
            ));
    
    public bool IsPlayerOrAnyIpBanned(ulong steamId)
    {
        var steamIdStr = steamId.ToString();
        if (_banCache.Values.Any(b => 
                b.Status == "ACTIVE" &&
                b.PlayerSteamId?.Equals(steamIdStr, StringComparison.OrdinalIgnoreCase) == true))
        {
            return true;
        }
        
        return _playerIpsCache.TryGetValue(steamId, out var ipList) && ipList.ips.Any(ip =>
            !_cachedIgnoredIps.Contains(ip) && IsIpBanned(ip));
    }
    
    public bool HasIpForPlayer(ulong steamId, string ipAddress)
    {
        return _playerIpsCache.TryGetValue(steamId, out var ipList) 
               && ipList.ips.Contains(ipAddress);
    }
}