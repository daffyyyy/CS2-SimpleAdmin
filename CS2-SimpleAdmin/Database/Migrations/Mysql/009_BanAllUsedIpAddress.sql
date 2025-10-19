CREATE TABLE IF NOT EXISTS `sa_players_ips` (
                                  `steamid` bigint(20) NOT NULL,
                                  `address` varchar(64) NOT NULL,
                                  `used_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                  PRIMARY KEY (`steamid`, `address`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
