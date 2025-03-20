using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

public class ServerManager
{
    private int _getIpTryCount;

    public void CheckHibernationStatus()
    {
        ConVar? convar = ConVar.Find("sv_hibernate_when_empty");
        
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

            string address = $"{(!string.IsNullOrWhiteSpace(CS2_SimpleAdmin.Instance.Config.DefaultServerIP) ? CS2_SimpleAdmin.Instance.Config.DefaultServerIP : ConVar.Find("ip")!.StringValue)}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";

            var hostname = ConVar.Find("hostname")!.StringValue;
            var rcon = ConVar.Find("rcon_password")!.StringValue;
            CS2_SimpleAdmin.IpAddress = address;

            CS2_SimpleAdmin._logger?.LogInformation("Loaded server with ip {ip}", ipAddress);

            Task.Run(async () =>
            {
                try
                {
                    await using var connection = await CS2_SimpleAdmin.Database.GetConnectionAsync();
                    var addressExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT COUNT(*) FROM sa_servers WHERE address = @address",
                        new { address });

                    if (!addressExists)
                    {
                        string query = "INSERT INTO sa_servers (address, hostname) VALUES (@address, @hostname)";

                        if(CS2_SimpleAdmin.Instance.Config.IsCSSPanel)
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

                        if(CS2_SimpleAdmin.Instance.Config.IsCSSPanel)
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

                    if (CS2_SimpleAdmin.ServerId != null)
                    {
                        await Server.NextWorldUpdateAsync(() => CS2_SimpleAdmin.Instance.ReloadAdmins(null));
                    }

                    CS2_SimpleAdmin.ServerLoaded = true;
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