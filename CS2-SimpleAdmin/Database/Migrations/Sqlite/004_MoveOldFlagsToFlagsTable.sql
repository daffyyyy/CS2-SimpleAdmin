INSERT INTO sa_admins_flags (admin_id, flag)
WITH RECURSIVE
    min_admins AS (
        SELECT MIN(id) AS admin_id, player_steamid, server_id
        FROM sa_admins
        WHERE player_steamid != 'Console'
GROUP BY player_steamid, server_id
    ),
    split_flags AS (
SELECT
    ma.admin_id,
    sa.flags,
    1 AS pos,
    CASE
    WHEN INSTR(sa.flags || ',', ',') = 0 THEN sa.flags
    ELSE SUBSTR(sa.flags, 1, INSTR(sa.flags || ',', ',') - 1)
    END AS flag,
    CASE
    WHEN INSTR(sa.flags || ',', ',') = 0 THEN ''
    ELSE SUBSTR(sa.flags, INSTR(sa.flags || ',', ',') + 1)
    END AS remaining
FROM min_admins ma
    JOIN sa_admins sa ON ma.player_steamid = sa.player_steamid
    AND (ma.server_id = sa.server_id OR (ma.server_id IS NULL AND sa.server_id IS NULL))
WHERE sa.flags IS NOT NULL AND sa.flags != ''

UNION ALL

SELECT
    admin_id,
    flags,
    pos + 1,
    CASE
    WHEN INSTR(remaining || ',', ',') = 0 THEN remaining
    ELSE SUBSTR(remaining, 1, INSTR(remaining || ',', ',') - 1)
    END AS flag,
    CASE
    WHEN INSTR(remaining || ',', ',') = 0 THEN ''
    ELSE SUBSTR(remaining, INSTR(remaining || ',', ',') + 1)
    END AS remaining
FROM split_flags
WHERE remaining != ''
    )
SELECT admin_id, TRIM(flag)
FROM split_flags
WHERE flag IS NOT NULL AND TRIM(flag) != '';