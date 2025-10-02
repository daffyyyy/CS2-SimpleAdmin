ALTER TABLE sa_mutes ADD INDEX (player_steamid, status, ends);
ALTER TABLE sa_mutes ADD INDEX(player_steamid, status, server_id, duration);
ALTER TABLE sa_mutes ADD INDEX(player_steamid, type);
