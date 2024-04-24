INSERT INTO sa_admins_flags (admin_id, flag)
WITH RECURSIVE numbers AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM numbers
    WHERE n < (SELECT MAX(CHAR_LENGTH(flags) - CHAR_LENGTH(REPLACE(flags, ',', '')) + 1) FROM sa_admins)
)
SELECT 
    min_admins.admin_id,
    TRIM(SUBSTRING_INDEX(SUBSTRING_INDEX(sa_admins.flags, ',', numbers.n), ',', -1)) AS flag
FROM numbers
JOIN (
    SELECT MIN(id) AS admin_id, player_steamid, server_id
    FROM sa_admins
    WHERE player_steamid != 'Console'
    GROUP BY player_steamid, server_id
) AS min_admins ON 1=1
JOIN sa_admins ON CHAR_LENGTH(sa_admins.flags) - CHAR_LENGTH(REPLACE(sa_admins.flags, ',', '')) >= numbers.n - 1
               AND min_admins.player_steamid = sa_admins.player_steamid
               AND (min_admins.server_id = sa_admins.server_id OR (min_admins.server_id IS NULL AND sa_admins.server_id IS NULL))

UNION

SELECT 
    (SELECT MAX(id) + 1 FROM sa_admins WHERE server_id IS NULL) AS admin_id,
    TRIM(SUBSTRING_INDEX(SUBSTRING_INDEX(sa_admins.flags, ',', numbers.n), ',', -1)) AS flag
FROM numbers
JOIN sa_admins ON CHAR_LENGTH(sa_admins.flags) - CHAR_LENGTH(REPLACE(sa_admins.flags, ',', '')) >= numbers.n - 1
               AND sa_admins.server_id IS NULL
WHERE sa_admins.player_steamid != 'Console';
