CREATE TABLE IF NOT EXISTS `sa_warns` (
                                          `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                          `player_name` VARCHAR(128) DEFAULT NULL,
    `player_steamid` VARCHAR(64) NOT NULL,
    `admin_steamid` VARCHAR(64) NOT NULL,
    `admin_name` VARCHAR(128) NOT NULL,
    `reason` VARCHAR(255) NOT NULL,
    `duration` INTEGER NOT NULL,
    `ends` TIMESTAMP NOT NULL,
    `created` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `server_id` INTEGER DEFAULT NULL,
    `status` TEXT NOT NULL DEFAULT 'ACTIVE'
    );