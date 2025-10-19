using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

public class ServerManager
{
    private int _getIpTryCount;

    /// <summary>
    /// Checks whether the server setting <c>sv_hibernate_when_empty</c> is enabled.
    /// Logs an error if this setting is true, since it prevents the plugin from working properly.
    /// </summary>
    public static void CheckHibernationStatus()
    {
        var convar = ConVar.Find("sv_hibernate_when_empty");
        if (convar == null || !convar.GetPrimitiveValue<bool>())
            return;
        
        CS2_SimpleAdmin._logger?.LogError("Detected setting \"sv_hibernate_when_empty true\", set false to make plugin work properly");
    }
    
    /// <summary>
    /// Initiates the asynchronous process to load server data such as IP address, port, hostname, and RCON password.
    /// Handles retry attempts if IP address is not immediately available.
    /// Updates or inserts the server record in the database accordingly.
    /// After loading, triggers admin reload and cache initialization.
    /// Also optionally sends plugin usage metrics if enabled in configuration.
    /// </summary>
    public void LoadServerData()
    {
        CS2_SimpleAdmin.Instance.AddTimer(2.0f, () =>
        {
            if (CS2_SimpleAdmin.ServerLoaded || CS2_SimpleAdmin.DatabaseProvider == null) return;

            // Optimization: Get server IP once and reuse
            var serverIp = Helper.GetServerIp();
            var isInvalidIp = string.IsNullOrEmpty(serverIp) || serverIp.StartsWith("0.0.0");

            // Check if we've exceeded retry limit with invalid IP
            if (_getIpTryCount > 32 && isInvalidIp)
            {
                CS2_SimpleAdmin._logger?.LogError("Unable to load server data - can't fetch ip address!");
                return;
            }

            // Optimization: Cache ConVar lookups
            var ipConVar = ConVar.Find("ip");
            var ipAddress = ipConVar?.StringValue;

            // Use Helper IP if ConVar IP is invalid
            if (string.IsNullOrEmpty(ipAddress) || ipAddress.StartsWith("0.0.0"))
            {
                ipAddress = serverIp;

                // Retry if still invalid and under retry limit
                if (_getIpTryCount <= 32 && isInvalidIp)
                {
                    _getIpTryCount++;
                    LoadServerData();
                    return;
                }
            }

            // Optimization: Cache remaining ConVar lookups
            var hostportConVar = ConVar.Find("hostport");
            var hostnameConVar = ConVar.Find("hostname");
            var rconPasswordConVar = ConVar.Find("rcon_password");

            var address = $"{ipAddress}:{hostportConVar?.GetPrimitiveValue<int>()}";
            var hostname = hostnameConVar?.StringValue ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
            var rconPassword = rconPasswordConVar?.StringValue ?? "";
            CS2_SimpleAdmin.IpAddress = address;
            
            Task.Run(async () =>
            {
                try
                {
                    await using var connection = await CS2_SimpleAdmin.DatabaseProvider.CreateConnectionAsync();
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
            
            CS2_SimpleAdmin.SimpleAdminApi?.OnSimpleAdminReadyEvent();
        });
    }
}