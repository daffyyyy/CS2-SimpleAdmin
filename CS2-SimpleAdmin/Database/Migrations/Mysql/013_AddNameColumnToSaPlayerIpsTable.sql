DELETE FROM sa_players_ips WHERE INET_ATON(address) IS NULL AND address IS NOT NULL;
UPDATE `sa_players_ips` SET `address` = INET_ATON(address);
ALTER TABLE `sa_players_ips` CHANGE `address` `address` INT UNSIGNED NOT NULL;
ALTER TABLE `sa_players_ips` ADD INDEX (used_at DESC);
ALTER TABLE `sa_players_ips` ADD `name` VARCHAR(64) NULL DEFAULT NULL AFTER `steamid`;