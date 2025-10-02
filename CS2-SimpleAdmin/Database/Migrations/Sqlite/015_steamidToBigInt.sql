UPDATE `sa_bans`
SET admin_steamid = '0'
WHERE admin_steamid NOT GLOB '[0-9]*';

UPDATE `sa_mutes`
SET admin_steamid = '0'
WHERE admin_steamid NOT GLOB '[0-9]*';

UPDATE `sa_warns`
SET admin_steamid = '0'
WHERE admin_steamid NOT GLOB '[0-9]*';

UPDATE `sa_admins`
SET player_steamid = '0'
WHERE player_steamid NOT GLOB '[0-9]*';