CREATE TABLE IF NOT EXISTS `sa_warns` (
                            `id` int(11) NOT NULL AUTO_INCREMENT,
                            `player_name` varchar(128) DEFAULT NULL,
                            `player_steamid` varchar(64) NOT NULL,
                            `admin_steamid` varchar(64) NOT NULL,
                            `admin_name` varchar(128) NOT NULL,
                            `reason` varchar(255) NOT NULL,
                            `duration` int(11) NOT NULL,
                            `ends` timestamp NOT NULL,
                            `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            `server_id` int(11) DEFAULT NULL,
                            `status` enum('ACTIVE','EXPIRED','') NOT NULL DEFAULT 'ACTIVE',
                            PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
