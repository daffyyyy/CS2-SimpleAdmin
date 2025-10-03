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

            string address = $"{(!string.IsNullOrWhiteSpace(CS2_SimpleAdmin.Instance.Config.DefaultServerIP) ? CS2_SimpleAdmin.Instance.Config.DefaultServerIP : ConVar.Find("ip")!.StringValue)}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";

            var hostname = ConVar.Find("hostname")!.StringValue;
            var rcon = ConVar.Find("rcon_password")!.StringValue;
            CS2_SimpleAdmin.IpAddress = address;

            CS2_SimpleAdmin._logger?.LogInformation("Loaded server with ip {ip}", ipAddress);

            Task.Run(async () =>
            {
                try
                {
                    await using var connection = await CS2_SimpleAdmin.DatabaseProvider.CreateConnectionAsync();
                    var addressExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT COUNT(*) FROM sa_servers WHERE address = @address",
                        new { address });

                    if (!addressExists)
                    {
                        string query = "INSERT INTO sa_servers (address, hostname) VALUES (@address, @hostname)";

                        if (CS2_SimpleAdmin.Instance.Config.IsCSSPanel)
                        {
                            query = "INSERT INTO sa_servers (address, hostname, rcon) VALUES (@address, @hostname, @rcon)";
                        }

                        await connection.ExecuteAsync(
                            query,
                            new { address, hostname, rcon });
                    }
                    else
                    {
                        string query = "UPDATE `sa_servers` SET `hostname` = @hostname, `id` = `id` WHERE `address` = @address";

                        if (CS2_SimpleAdmin.Instance.Config.IsCSSPanel)
                        {
                            query = "UPDATE `sa_servers` SET `hostname` = @hostname, rcon = @rcon, `id` = `id` WHERE `address` = @address";
                        }

                        await connection.ExecuteAsync(
                            query,
                            new { address, rcon, hostname });
                    }

                    int? serverId = await connection.ExecuteScalarAsync<int>(
                        "SELECT `id` FROM `sa_servers` WHERE `address` = @address",
                        new { address });

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
            });
        });
    }
}