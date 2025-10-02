CREATE TABLE IF NOT EXISTS `sa_unbans` (
                                           `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                           `ban_id` INTEGER NOT NULL,
                                           `admin_id` INTEGER NOT NULL DEFAULT 0,
                                           `reason` VARCHAR(255) NOT NULL DEFAULT 'Unknown',
    `date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );

CREATE TABLE IF NOT EXISTS `sa_unmutes` (
                                            `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                            `mute_id` INTEGER NOT NULL,
                                            `admin_id` INTEGER NOT NULL DEFAULT 0,
                                            `reason` VARCHAR(255) NOT NULL DEFAULT 'Unknown',
    `date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );

INSERT OR IGNORE INTO `sa_admins` (`id`, `player_name`, `player_steamid`, `flags`, `immunity`, `server_id`, `ends`, `created`) 
VALUES (0, 'Console', 'Console', '', '0', NULL, NULL, CURRENT_TIMESTAMP);

UPDATE `sa_admins` SET `id` = 0 WHERE `id` = -1;

ALTER TABLE `sa_bans` ADD `unban_id` INTEGER NULL;
ALTER TABLE `sa_mutes` ADD `unmute_id` INTEGER NULL;