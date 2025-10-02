CREATE TABLE IF NOT EXISTS `sa_players_ips` (
                                                `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                                `steamid` INTEGER NOT NULL,
                                                `address` VARCHAR(64) NOT NULL,
    `used_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (`steamid`, `address`)
    );