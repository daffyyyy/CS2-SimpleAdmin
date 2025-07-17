using Dapper;
using MySqlConnector;

namespace CS2_SimpleAdmin_PlayTimeModule;

public class Database(string dbConnectionString)
{
    private async Task<MySqlConnection> GetConnectionAsync()
    {
        var connection = new MySqlConnection(dbConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<PlayTimeModel?> GetPlayTimeAsync(ulong steamId, int? serverId)
    {
        if (!serverId.HasValue)
            return null;
    
        try
        {
            await using var connection = await GetConnectionAsync();
            const string query = """
                                     SELECT total_playtime AS TotalPlayTime,
                                            spec_playtime AS SpecPlayTime,
                                            hidden_playtime AS HiddenPlayTime,
                                            ct_playtime AS CtPlayTime,
                                            tt_playtime AS TtPlayTime
                                     FROM sa_playtime
                                     WHERE steamid = @SteamId AND server_id = @ServerId
                                     LIMIT 1;
                                 """;

            var rawResult = await connection.QueryFirstOrDefaultAsync(query, new { SteamId = steamId, ServerId = serverId });
            
            var model = new PlayTimeModel();

            if (rawResult != null)
            {
                model.TotalPlayTime = rawResult.TotalPlayTime;
                model.Teams[0].PlayTime = rawResult.HiddenPlayTime;
                model.Teams[1].PlayTime = rawResult.SpecPlayTime;
                model.Teams[2].PlayTime = rawResult.TtPlayTime;
                model.Teams[3].PlayTime = rawResult.CtPlayTime;
            }
            
            // var result = await connection.QueryFirstOrDefaultAsync<PlayTimeModel>(query, new { SteamId = steamId, ServerId = serverId })
            //              ?? new PlayTimeModel(totalPlayTime: 0);
        
            model.JoinedTime = DateTime.Now;
            return model;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving playtime data for SteamId {steamId}: {ex.Message}");
            return null;
        }
    }

    public async Task UpdatePlayTimeAsync(ulong steamId, PlayTimeModel model, int? serverId)
    {
        if (!serverId.HasValue)
            return;
        try
        {
            const string checkQuery =
                "SELECT EXISTS(SELECT 1 FROM sa_playtime WHERE steamid = @SteamId AND server_id = @ServerId LIMIT 1);";

            const string insertQuery = 
                "INSERT INTO sa_playtime (steamid, server_id, total_playtime, spec_playtime, hidden_playtime, ct_playtime, tt_playtime) " +
                "VALUES (@SteamId, @ServerId, @TotalPlayTime, @SpecPlayTime, @HiddenPlayTime, @CtPlayTime, @TtPlayTime);";

            const string updateQuery = 
                "UPDATE sa_playtime " +
                "SET total_playtime = @TotalPlayTime, spec_playtime = @SpecPlayTime, hidden_playtime = @HiddenPlayTime, ct_playtime = @CtPlayTime, tt_playtime = @TtPlayTime " +
                "WHERE steamid = @SteamId AND server_id = @ServerId;";

            await using var connection = await GetConnectionAsync();

            var exists =
                await connection.ExecuteScalarAsync<bool>(checkQuery, new { SteamId = steamId, ServerId = serverId });
            
            if (exists)
            {
                await connection.ExecuteAsync(updateQuery, new
                {
                    SteamId = steamId,
                    ServerId = serverId,
                    model.TotalPlayTime,
                    HiddenPlayTime = model.Teams[0].PlayTime,
                    SpecPlayTime = model.Teams[1].PlayTime,
                    TtPlayTime = model.Teams[2].PlayTime,
                    CtPlayTime = model.Teams[3].PlayTime
                });
            }
            else
            {
                await connection.ExecuteAsync(insertQuery, new
                {
                    SteamId = steamId,
                    ServerId = serverId,
                    model.TotalPlayTime,
                    HiddenPlayTime = model.Teams[0].PlayTime,
                    SpecPlayTime = model.Teams[1].PlayTime,
                    TtPlayTime = model.Teams[2].PlayTime,
                    CtPlayTime = model.Teams[3].PlayTime
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}