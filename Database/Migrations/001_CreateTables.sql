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
						 PRIMARY KEY (`id`),
						 UNIQUE KEY `address` (`address`)
						) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
