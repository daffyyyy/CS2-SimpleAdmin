CREATE TABLE IF NOT EXISTS `sa_bans` (
                                         `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                         `player_name` VARCHAR(128),
    `player_steamid` VARCHAR(64),
    `player_ip` VARCHAR(128),
    `admin_steamid` VARCHAR(64) NOT NULL,
    `admin_name` VARCHAR(128) NOT NULL,
    `reason` VARCHAR(255) NOT NULL,
    `duration` INTEGER NOT NULL,
    `ends` TIMESTAMP NULL,
    `created` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `server_id` INTEGER NULL,
    `status` TEXT NOT NULL DEFAULT 'ACTIVE'
    );

CREATE TABLE IF NOT EXISTS `sa_mutes` (
                                          `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                          `player_name` VARCHAR(128) NULL,
    `player_steamid` VARCHAR(64) NOT NULL,
    `admin_steamid` VARCHAR(64) NOT NULL,
    `admin_name` VARCHAR(128) NOT NULL,
    `reason` VARCHAR(255) NOT NULL,
    `duration` INTEGER NOT NULL,
    `ends` TIMESTAMP NULL,
    `created` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `type` TEXT NOT NULL DEFAULT 'GAG',
    `server_id` INTEGER NULL,
    `status` TEXT NOT NULL DEFAULT 'ACTIVE'
    );

CREATE TABLE IF NOT EXISTS `sa_admins` (
                                           `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                           `player_name` VARCHAR(128) NOT NULL,
    `player_steamid` VARCHAR(64) NOT NULL,
    `flags` TEXT NULL,
    `immunity` INTEGER NOT NULL DEFAULT 0,
    `server_id` INTEGER NULL,
    `ends` TIMESTAMP NULL,
    `created` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
    );

CREATE TABLE IF NOT EXISTS `sa_servers` (
                                            `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                            `hostname` VARCHAR(128) NOT NULL,
    `address` VARCHAR(64) NOT NULL,
    UNIQUE (`address`)
    );