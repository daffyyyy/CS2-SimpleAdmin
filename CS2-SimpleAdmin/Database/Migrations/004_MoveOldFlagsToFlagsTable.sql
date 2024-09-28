INSERT INTO sa_admins_flags (admin_id, flag)
SELECT 
    min_admins.admin_id,
    TRIM(SUBSTRING_INDEX(SUBSTRING_INDEX(sa_admins.flags, ',', numbers.n), ',', -1)) AS flag
FROM (
    SELECT MIN(id) AS admin_id, player_steamid, server_id
    FROM sa_admins
    WHERE player_steamid != 'Console'
    GROUP BY player_steamid, server_id
) AS min_admins
JOIN sa_admins ON min_admins.player_steamid = sa_admins.player_steamid
JOIN (
    SELECT 1 AS n UNION ALL
    SELECT 2 UNION ALL
    SELECT 3 UNION ALL
    SELECT 4 UNION ALL
    SELECT 5 UNION ALL
    SELECT 6 UNION ALL
    SELECT 7 UNION ALL
    SELECT 8 UNION ALL
    SELECT 9 UNION ALL
    SELECT 10 UNION ALL
    SELECT 11 UNION ALL
    SELECT 12 UNION ALL
    SELECT 13 UNION ALL
    SELECT 14 UNION ALL
    SELECT 15 UNION ALL
    SELECT 16 UNION ALL
    SELECT 17 UNION ALL
    SELECT 18 UNION ALL
    SELECT 19 UNION ALL
    SELECT 20 
) AS numbers
ON CHAR_LENGTH(sa_admins.flags) - CHAR_LENGTH(REPLACE(sa_admins.flags, ',', '')) >= numbers.n - 1
AND (min_admins.server_id = sa_admins.server_id OR (min_admins.server_id IS NULL AND sa_admins.server_id IS NULL))
WHERE sa_admins.id IS NOT NULL;
