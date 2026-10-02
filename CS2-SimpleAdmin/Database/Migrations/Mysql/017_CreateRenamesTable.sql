CREATE TABLE IF NOT EXISTS `sa_renames` (
 `player_steamid` BIGINT NOT NULL,
 `name` VARCHAR(128) NOT NULL,
 PRIMARY KEY (`player_steamid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
