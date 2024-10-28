ALTER TABLE `sa_servers` ADD `rcon` varchar(64) NOT NULL AFTER `address`;

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