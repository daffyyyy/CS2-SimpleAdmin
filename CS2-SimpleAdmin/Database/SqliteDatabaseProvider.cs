using System.Data.Common;
using System.Data.SQLite;

namespace CS2_SimpleAdmin.Database;

public class SqliteDatabaseProvider(string filePath) : IDatabaseProvider
{
    private readonly string _connectionString = $"Data Source={filePath}";

    public async Task<DbConnection> CreateConnectionAsync()
    {
        var conn = new SQLiteConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    public async Task<(bool Success, string? Exception)> CheckConnectionAsync()
    {
        try
        {
            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public Task DatabaseMigrationAsync()
    {
        var migration = new Migration(CS2_SimpleAdmin.Instance.ModuleDirectory + "/Database/Migrations/Sqlite");
        return migration.ExecuteMigrationsAsync();
    }

    public string GetBanSelectQuery(bool multiServer) =>
        multiServer
            ? """
              SELECT id AS Id,
                     player_name AS PlayerName,
                     player_steamid AS PlayerSteamId,
                     player_ip AS PlayerIp,
                     status AS Status
              FROM sa_bans
              """
            : """
              SELECT id AS Id,
                     player_name AS PlayerName,
                     player_steamid AS PlayerSteamId,
                     player_ip AS PlayerIp,
                     status AS Status
              FROM sa_bans
              WHERE server_id = @serverId
              """;

    public static string GetBanUpdatedSelectQuery(bool multiServer) =>
        multiServer
            ? """
              SELECT id AS Id,
                     player_name AS PlayerName,
                     player_steamid AS PlayerSteamId,
                     player_ip AS PlayerIp,
                     status AS Status
              FROM sa_bans
              WHERE updated_at > @lastUpdate OR created > @lastUpdate
              ORDER BY updated_at DESC
              """
            : """
              SELECT id AS Id,
                     player_name AS PlayerName,
                     player_steamid AS PlayerSteamId,
                     player_ip AS PlayerIp,
                     status AS Status
              FROM sa_bans
              WHERE (updated_at > @lastUpdate OR created > @lastUpdate)
              AND server_id = @serverId
              ORDER BY updated_at DESC
              """;

    public string GetIpHistoryQuery() =>
        "SELECT steamid, name, address, used_at FROM sa_players_ips ORDER BY used_at DESC";

    public string GetBanUpdateQuery(bool multiServer) =>
        multiServer
            ? """
              UPDATE sa_bans
              SET player_ip   = COALESCE(player_ip, @PlayerIP),
                  player_name = COALESCE(player_name, @PlayerName)
              WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
                AND status = 'ACTIVE'
                AND (duration = 0 OR ends > @CurrentTime)
              """
            : """
              UPDATE sa_bans
              SET player_ip   = COALESCE(player_ip, @PlayerIP),
                  player_name = COALESCE(player_name, @PlayerName)
              WHERE (player_steamid = @PlayerSteamID OR player_ip = @PlayerIP)
                AND status = 'ACTIVE'
                AND (duration = 0 OR ends > @CurrentTime)
                AND server_id = @ServerId
              """;

    public string GetAddBanQuery() =>
        """
        INSERT INTO sa_bans
            (player_steamid, player_name, player_ip, admin_steamid, admin_name, reason, duration, ends, created, server_id)
        VALUES
            (@playerSteamid, @playerName, @playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid);
        SELECT last_insert_rowid();
        """;

    public string GetAddBanBySteamIdQuery() =>
        """
        INSERT INTO sa_bans
            (player_steamid, admin_steamid, admin_name, reason, duration, ends, created, server_id)
        VALUES
            (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid);
        SELECT last_insert_rowid();
        """;

    public string GetAddBanByIpQuery() =>
        """
        INSERT INTO sa_bans
            (player_ip, admin_steamid, admin_name, reason, duration, ends, created, server_id)
        VALUES
            (@playerIp, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @serverid);
        SELECT last_insert_rowid();
        """;

    public string GetUnbanRetrieveBansQuery(bool multiServer) =>
        multiServer
            ? "SELECT id FROM sa_bans WHERE (player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern) AND status = 'ACTIVE'"
            : "SELECT id FROM sa_bans WHERE (player_steamid = @pattern OR player_name = @pattern OR player_ip = @pattern) AND status = 'ACTIVE' AND server_id = @serverid";

    public string GetUnbanAdminIdQuery() =>
        "SELECT id FROM sa_admins WHERE player_steamid = @adminSteamId";

    public string GetInsertUnbanQuery(bool includeReason) =>
        includeReason
            ? "INSERT INTO sa_unbans (ban_id, admin_id, reason) VALUES (@banId, @adminId, @reason); SELECT last_insert_rowid();"
            : "INSERT INTO sa_unbans (ban_id, admin_id) VALUES (@banId, @adminId); SELECT last_insert_rowid();";

    public string GetUpdateBanStatusQuery() =>
        "UPDATE sa_bans SET status = 'UNBANNED', unban_id = @unbanId WHERE id = @banId";

    public string GetExpireBansQuery(bool multiServer) =>
        multiServer
            ? "UPDATE sa_bans SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND duration > 0 AND ends <= @currentTime"
            : "UPDATE sa_bans SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND duration > 0 AND ends <= @currentTime AND server_id = @serverid";

    public string GetExpireIpBansQuery(bool multiServer) =>
        multiServer
            ? "UPDATE sa_bans SET player_ip = NULL WHERE status = 'ACTIVE' AND ends <= @ipBansTime"
            : "UPDATE sa_bans SET player_ip = NULL WHERE status = 'ACTIVE' AND ends <= @ipBansTime AND server_id = @serverid";
    
    public string GetAdminsQuery() =>
        """
        SELECT sa_admins.player_steamid, sa_admins.player_name, sa_admins_flags.flag, sa_admins.immunity, sa_admins.ends
        FROM sa_admins_flags
        JOIN sa_admins ON sa_admins_flags.admin_id = sa_admins.id
        WHERE (sa_admins.ends IS NULL OR sa_admins.ends > @CurrentTime)
          AND (sa_admins.server_id IS NULL OR sa_admins.server_id = @serverid)
        ORDER BY sa_admins.player_steamid
        """;

    public string GetDeleteAdminQuery(bool globalDelete) =>
        globalDelete
            ? "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID"
            : "DELETE FROM sa_admins WHERE player_steamid = @PlayerSteamID AND server_id = @ServerId";

    public string GetAddAdminQuery() =>
        """
        INSERT INTO sa_admins (player_steamid, player_name, immunity, ends, created, server_id)
        VALUES (@playerSteamId, @playerName, @immunity, @ends, @created, @serverid);
        SELECT last_insert_rowid();
        """;

    public string GetGroupsQuery() =>
        """
        SELECT g.group_id, sg.name AS group_name, sg.immunity, f.flag
        FROM sa_groups_flags f
        JOIN sa_groups_servers g ON f.group_id = g.group_id
        JOIN sa_groups sg ON sg.id = g.group_id
        WHERE (g.server_id = @serverid OR server_id IS NULL)
        """;

    public string GetAddAdminFlagsQuery() =>
        "INSERT INTO sa_admins_flags (admin_id, flag) VALUES (@adminId, @flag);";

    public string GetUpdateAdminGroupQuery() =>
        "UPDATE sa_admins SET group_id = @groupId WHERE id = @adminId;";

    public string GetAddGroupQuery() =>
        """
        INSERT INTO sa_groups (name, immunity) VALUES (@groupName, @immunity);
        SELECT last_insert_rowid();
        """;

    public string GetGroupIdByNameQuery() =>
        """
        SELECT sgs.group_id
        FROM sa_groups_servers sgs
        JOIN sa_groups sg ON sgs.group_id = sg.id
        WHERE sg.name = @groupName
        ORDER BY (sgs.server_id = @serverId) DESC, sgs.server_id ASC
        LIMIT 1;
        """;

    public string GetAddGroupFlagsQuery() =>
        "INSERT INTO sa_groups_flags (group_id, flag) VALUES (@groupId, @flag);";

    public string GetAddGroupServerQuery() =>
        "INSERT INTO sa_groups_servers (group_id, server_id) VALUES (@groupId, @server_id);";

    public string GetDeleteGroupQuery() =>
        "DELETE FROM sa_groups WHERE name = @groupName;";

    public string GetDeleteOldAdminsQuery() =>
        "DELETE FROM sa_admins WHERE ends IS NOT NULL AND ends <= @CurrentTime;";
    
    public string GetAddMuteQuery(bool includePlayerName) =>
        includePlayerName
            ? """
              INSERT INTO sa_mutes
              (player_steamid, player_name, admin_steamid, admin_name, reason, duration, ends, created, type, server_id)
              VALUES (@playerSteamid, @playerName, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @type, @serverid);
              SELECT last_insert_rowid();
              """
            : """
              INSERT INTO sa_mutes
              (player_steamid, admin_steamid, admin_name, reason, duration, ends, created, type, server_id)
              VALUES (@playerSteamid, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @type, @serverid);
              SELECT last_insert_rowid();
              """;

    public string GetIsMutedQuery(bool multiServer, int timeMode) =>
        multiServer
            ? (timeMode == 1
                ? "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)"
                : "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR duration > COALESCE(passed, 0))")
            : (timeMode == 1
                ? "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime) AND server_id = @serverid"
                : "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR duration > COALESCE(passed, 0)) AND server_id = @serverid");

    public string GetMuteStatsQuery(bool multiServer) =>
        multiServer
            ? """
              SELECT
                  COUNT(CASE WHEN type = 'MUTE' THEN 1 END) AS TotalMutes,
                  COUNT(CASE WHEN type = 'GAG' THEN 1 END) AS TotalGags,
                  COUNT(CASE WHEN type = 'SILENCE' THEN 1 END) AS TotalSilences
              FROM sa_mutes
              WHERE player_steamid = @PlayerSteamID;
              """
            : """
              SELECT
                  COUNT(CASE WHEN type = 'MUTE' THEN 1 END) AS TotalMutes,
                  COUNT(CASE WHEN type = 'GAG' THEN 1 END) AS TotalGags,
                  COUNT(CASE WHEN type = 'SILENCE' THEN 1 END) AS TotalSilences
              FROM sa_mutes
              WHERE player_steamid = @PlayerSteamID AND server_id = @ServerId;
              """;

    public string GetUpdateMutePassedQuery(bool multiServer) =>
        multiServer
            ? "UPDATE sa_mutes SET passed = COALESCE(passed, 0) + 1 WHERE (player_steamid = @PlayerSteamID) AND duration > 0 AND status = 'ACTIVE'"
            : "UPDATE sa_mutes SET passed = COALESCE(passed, 0) + 1 WHERE (player_steamid = @PlayerSteamID) AND duration > 0 AND status = 'ACTIVE' AND server_id = @serverid";

    public string GetCheckExpiredMutesQuery(bool multiServer) =>
        multiServer
            ? "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND passed >= duration AND duration > 0 AND status = 'ACTIVE'"
            : "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND passed >= duration AND duration > 0 AND status = 'ACTIVE' AND server_id = @serverid";

    public string GetRetrieveMutesQuery(bool multiServer) =>
        multiServer
            ? "SELECT id FROM sa_mutes WHERE (player_steamid = @pattern OR player_name = @pattern) AND type = @muteType AND status = 'ACTIVE'"
            : "SELECT id FROM sa_mutes WHERE (player_steamid = @pattern OR player_name = @pattern) AND type = @muteType AND status = 'ACTIVE' AND server_id = @serverid";

    public string GetUnmuteAdminIdQuery() =>
        "SELECT id FROM sa_admins WHERE player_steamid = @adminSteamId";

    public string GetInsertUnmuteQuery(bool includeReason) =>
        includeReason
            ? "INSERT INTO sa_unmutes (mute_id, admin_id, reason) VALUES (@muteId, @adminId, @reason); SELECT last_insert_rowid();"
            : "INSERT INTO sa_unmutes (mute_id, admin_id) VALUES (@muteId, @adminId); SELECT last_insert_rowid();";

    public string GetUpdateMuteStatusQuery() =>
        "UPDATE sa_mutes SET status = 'UNMUTED', unmute_id = @unmuteId WHERE id = @muteId";

    public string GetExpireMutesQuery(bool multiServer, int timeMode) =>
        multiServer
            ? (timeMode == 1
                ? "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND duration > 0 AND ends <= @CurrentTime"
                : "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND duration > 0 AND passed >= duration")
            : (timeMode == 1
                ? "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND duration > 0 AND ends <= @CurrentTime AND server_id = @serverid"
                : "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND duration > 0 AND passed >= duration AND server_id = @serverid");
    
    public string GetAddWarnQuery(bool includePlayerName) =>
        includePlayerName
            ? """
              INSERT INTO sa_warns
              (player_steamid, player_name, admin_steamid, admin_name, reason, duration, ends, created, server_id)
              VALUES
              (@playerSteamid, @playerName, @adminSteamid, @adminName, @warnReason, @duration, @ends, @created, @serverid);
              SELECT last_insert_rowid();
              """
            : """
              INSERT INTO sa_warns
              (player_steamid, admin_steamid, admin_name, reason, duration, ends, created, server_id)
              VALUES
              (@playerSteamid, @adminSteamid, @adminName, @warnReason, @duration, @ends, @created, @serverid);
              SELECT last_insert_rowid();
              """;

    public string GetPlayerWarnsQuery(bool multiServer, bool active) =>
        multiServer
            ? active
                ? "SELECT * FROM sa_warns WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' ORDER BY id DESC"
                : "SELECT * FROM sa_warns WHERE player_steamid = @PlayerSteamID ORDER BY id DESC"
            : active
                ? "SELECT * FROM sa_warns WHERE player_steamid = @PlayerSteamID AND server_id = @serverid AND status = 'ACTIVE' ORDER BY id DESC"
                : "SELECT * FROM sa_warns WHERE player_steamid = @PlayerSteamID AND server_id = @serverid ORDER BY id DESC";

    public string GetPlayerWarnsCountQuery(bool multiServer, bool active) =>
        multiServer
            ? active
                ? "SELECT COUNT(*) FROM sa_warns WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE'"
                : "SELECT COUNT(*) FROM sa_warns WHERE player_steamid = @PlayerSteamID"
            : active
                ? "SELECT COUNT(*) FROM sa_warns WHERE player_steamid = @PlayerSteamID AND server_id = @serverid AND status = 'ACTIVE'"
                : "SELECT COUNT(*) FROM sa_warns WHERE player_steamid = @PlayerSteamID AND server_id = @serverid";

    public string GetUnwarnByIdQuery(bool multiServer) =>
        multiServer
            ? "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND player_steamid = @steamid AND id = @warnId"
            : "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND player_steamid = @steamid AND id = @warnId AND server_id = @serverid";

    public string GetUnwarnLastQuery(bool multiServer) =>
        multiServer
            ? """
              UPDATE sa_warns
              SET status = 'EXPIRED'
              WHERE status = 'ACTIVE'
              AND player_steamid = @steamid
              ORDER BY id DESC
              LIMIT 1
              """
            : """
              UPDATE sa_warns
              SET status = 'EXPIRED'
              WHERE status = 'ACTIVE'
              AND player_steamid = @steamid
              AND server_id = @serverid
              ORDER BY id DESC
              LIMIT 1
              """;

    public string GetExpireWarnsQuery(bool multiServer) =>
        multiServer
            ? "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND duration > 0 AND ends <= @CurrentTime"
            : "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND duration > 0 AND ends <= @CurrentTime AND server_id = @serverid";
}
