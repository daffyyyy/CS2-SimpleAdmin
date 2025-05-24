UPDATE `sa_players_ips` SET `address` = INET_ATON(address);
ALTER TABLE `sa_players_ips` CHANGE `address` `address` INT UNSIGNED NOT NULL;
ALTER TABLE `sa_players_ips` ADD `name` VARCHAR(64) NULL DEFAULT NULL AFTER `steamid`;
ALTER TABLE `sa_players_ips` ADD INDEX(`used_at`);
