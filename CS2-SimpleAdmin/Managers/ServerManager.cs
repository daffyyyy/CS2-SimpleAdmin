using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

public class ServerManager
{
    private int _getIpTryCount;

    public static void CheckHibernationStatus()
    {
        var convar = ConVar.Find("sv_hibernate_when_empty");
        
        if (convar == null || !convar.GetPrimitiveValue<bool>())
            return;
        
        CS2_SimpleAdmin._logger?.LogError("Detected setting \"sv_hibernate_when_empty true\", set false to make plugin work properly");
    }
    
    public void LoadServerData()
    {
        CS2_SimpleAdmin.Instance.AddTimer(1.2f, () =>
        {
            if (CS2_SimpleAdmin.ServerLoaded || CS2_SimpleAdmin.ServerId != null || CS2_SimpleAdmin.Database == null) return;
            
            if (_getIpTryCount > 32 && Helper.GetServerIp().StartsWith("0.0.0.0") || string.IsNullOrEmpty(Helper.GetServerIp()))
            {
                CS2_SimpleAdmin._logger?.LogError("Unable to load server data - can't fetch ip address!");
                return;
            }

            var ipAddress = ConVar.Find("ip")?.StringValue;

            if (string.IsNullOrEmpty(ipAddress) || ipAddress.StartsWith("0.0.0"))
            {
                ipAddress = Helper.GetServerIp();

                if (_getIpTryCount <= 32 && (string.IsNullOrEmpty(ipAddress) || ipAddress.StartsWith("0.0.0")))
                {
                    _getIpTryCount++;
                    
                    LoadServerData();
                    return;
                }
            }

            var address = $"{ipAddress}:{ConVar.Find("hostport")?.GetPrimitiveValue<int>()}";
            var hostname = ConVar.Find("hostname")!.StringValue;
            var rconPassword = ConVar.Find("rcon_password")!.StringValue;
            CS2_SimpleAdmin.IpAddress = address;
            
            Task.Run(async () =>
            {
                try
                {
                    await using var connection = await CS2_SimpleAdmin.Database.GetConnectionAsync();
                    
                    int? serverId = await connection.ExecuteScalarAsync<int?>(
                        "SELECT id FROM sa_servers WHERE address = @address",
                        new { address });

                    if (serverId == null)
                    {
                        await connection.ExecuteAsync(
                            "INSERT INTO sa_servers (address, hostname, rcon_password) VALUES (@address, @hostname, @rconPassword)",
                            new { address, hostname, rconPassword });

                        serverId = await connection.ExecuteScalarAsync<int>(
                            "SELECT id FROM sa_servers WHERE address = @address",
                            new { address });
                    }
                    else
                    {
                        await connection.ExecuteAsync(
                            "UPDATE sa_servers SET hostname = @hostname, rcon_password = @rconPassword WHERE address = @address",
                            new { address, hostname, rconPassword });
                    }

                    CS2_SimpleAdmin.ServerId = serverId;
                    
                    CS2_SimpleAdmin._logger?.LogInformation("Loaded server with ip {ip}", ipAddress);

                    if (CS2_SimpleAdmin.ServerId != null)
                    {
                        await Server.NextWorldUpdateAsync(() => CS2_SimpleAdmin.Instance.ReloadAdmins(null));
                    }

                    CS2_SimpleAdmin.ServerLoaded = true;
                    if (CS2_SimpleAdmin.Instance.CacheManager != null)
                        await CS2_SimpleAdmin.Instance.CacheManager.InitializeCacheAsync();
                }
                catch (Exception ex)
                {
                    CS2_SimpleAdmin._logger?.LogCritical("Unable to create or get server_id: " + ex.Message);
                }

                if (CS2_SimpleAdmin.Instance.Config.EnableMetrics)
                {
                    var queryString = $"?address={address}&hostname={hostname}";
                    var client = CS2_SimpleAdmin.HttpClient;

                    try
                    {
                        await client.GetAsync($"https://api.daffyy.dev/index.php{queryString}");
                    }
                    catch (HttpRequestException ex)
                    {
                        CS2_SimpleAdmin._logger?.LogWarning($"Unable to make metrics call: {ex.Message}");
                    }
                }
            });
        });
    }
}