CREATE TABLE IF NOT EXISTS `sa_bans` (
                                `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                                `player_name` VARCHAR(128),
                                `player_steamid` VARCHAR(64),
                                `player_ip` VARCHAR(128),
                                `admin_steamid` VARCHAR(64) NOT NULL,
                                `admin_name` VARCHAR(128) NOT NULL,
                                `reason` VARCHAR(255) NOT NULL,
                                `duration` INT NOT NULL,
                                `ends` TIMESTAMP NULL,
                                `created` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
								`server_id` INT NULL,
                                `status` ENUM('ACTIVE', 'UNBANNED', 'EXPIRED', '') NOT NULL DEFAULT 'ACTIVE'
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_mutes` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `player_name` varchar(128) NULL,
						 `player_steamid` varchar(64) NOT NULL,
						 `admin_steamid` varchar(64) NOT NULL,
						 `admin_name` varchar(128) NOT NULL,
						 `reason` varchar(255) NOT NULL,
						 `duration` int(11) NOT NULL,
						 `ends` timestamp NULL,
						 `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
						 `type` enum('GAG','MUTE','SILENCE','') NOT NULL DEFAULT 'GAG',
						 `server_id` INT NULL,
						 `status` enum('ACTIVE','UNMUTED','EXPIRED','') NOT NULL DEFAULT 'ACTIVE',
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_admins` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `player_name` varchar(128) NOT NULL,
						 `player_steamid` varchar(64) NOT NULL,
						 `flags` TEXT NULL,
						 `immunity` int(11) NOT NULL DEFAULT 0,
						 `server_id` INT NULL,
						 `ends` timestamp NULL,
						 `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_servers` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `hostname` varchar(128) NOT NULL,
						 `address` varchar(64) NOT NULL,
						 `rcon` varchar(64) NOT NULL,
						 PRIMARY KEY (`id`),
						 UNIQUE KEY `address` (`address`)
						) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_chatlogs` (
						 `id` int(11) NOT NULL AUTO_INCREMENT,
						 `serverId` varchar(64) NOT NULL,
						 `playerSteam64` varchar(64) NOT NULL,
						 `playerName` varchar(64) NOT NULL,
						 `message` TEXT,
						 `team` BOOLEAN NOT NULL,
						 `created` timestamp NOT NULL,
						 PRIMARY KEY (`id`)
						) ENGINE=InnoDB AUTO_INCREMENT=36 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_statistics` (
							`id` INT(11) NOT NULL AUTO_INCREMENT,
							`serverId` VARCHAR(64) NOT NULL COLLATE 'utf8mb4_general_ci',
							`playerId` VARCHAR(64) NOT NULL COLLATE 'utf8mb4_general_ci',
							`playerName` VARCHAR(64) NOT NULL COLLATE 'utf8mb4_general_ci',
							`playerIP` VARCHAR(64) NOT NULL COLLATE 'utf8mb4_general_ci',
							`connectDate` timestamp DEFAULT CURRENT_TIMESTAMP,
							`connectTime` INT(20) NOT NULL,
							`disconnectDate` timestamp NULL DEFAULT NULL,
							`disconnectTime` INT(20) NULL DEFAULT NULL,
							`duration` INT(20) NULL DEFAULT NULL,
							`kills` INT(10) NULL DEFAULT NULL,
							`flags` TEXT COLLATE 'utf8mb4_general_ci',
							`map` VARCHAR(64) NOT NULL COLLATE 'utf8mb4_general_ci',
							PRIMARY KEY (`id`) USING BTREE,
							INDEX `playerId` (`playerId`) USING BTREE
						) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;