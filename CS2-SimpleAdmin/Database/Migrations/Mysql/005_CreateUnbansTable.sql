CREATE TABLE IF NOT EXISTS `sa_unbans` (
 `id` int(11) NOT NULL AUTO_INCREMENT,
 `ban_id` int(11) NOT NULL,
 `admin_id` int(11) NOT NULL DEFAULT 0,
 `reason` varchar(255) NOT NULL DEFAULT 'Unknown',
 `date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
 PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_unmutes` (
 `id` int(11) NOT NULL AUTO_INCREMENT,
 `mute_id` int(11) NOT NULL,
 `admin_id` int(11) NOT NULL DEFAULT 0,
 `reason` varchar(255) NOT NULL DEFAULT 'Unknown',
 `date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
 PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT IGNORE INTO `sa_admins` (`id`, `player_name`, `player_steamid`, `flags`, `immunity`, `server_id`, `ends`, `created`) 
VALUES (0, 'Console', 'Console', '', '0', NULL, NULL, NOW());

UPDATE `sa_admins` SET `id` = 0 WHERE `id` = -1;

ALTER TABLE `sa_bans` ADD `unban_id` INT NULL AFTER `server_id`;
ALTER TABLE `sa_mutes` ADD `unmute_id` INT NULL AFTER `server_id`;
ALTER TABLE `sa_bans` ADD FOREIGN KEY (`unban_id`) REFERENCES `sa_unbans`(`id`) ON DELETE CASCADE;
ALTER TABLE `sa_mutes` ADD FOREIGN KEY (`unmute_id`) REFERENCES `sa_unmutes`(`id`) ON DELETE CASCADE;
ALTER TABLE `sa_unbans` ADD FOREIGN KEY (`admin_id`) REFERENCES `sa_admins`(`id`) ON DELETE CASCADE;
ALTER TABLE `sa_unmutes` ADD FOREIGN KEY (`admin_id`) REFERENCES `sa_admins`(`id`) ON DELETE CASCADE;
