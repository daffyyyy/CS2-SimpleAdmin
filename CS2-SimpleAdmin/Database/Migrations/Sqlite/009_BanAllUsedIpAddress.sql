CREATE TABLE IF NOT EXISTS `sa_players_ips` (
                                                `steamid` INTEGER NOT NULL,
                                                `address` VARCHAR(64) NOT NULL,
                                                `used_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                                PRIMARY KEY (`steamid`, `address`)
);