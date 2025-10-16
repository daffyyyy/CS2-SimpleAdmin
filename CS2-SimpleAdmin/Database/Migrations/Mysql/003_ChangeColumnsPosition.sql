ALTER TABLE `sa_bans` CHANGE `player_name` `player_name` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL AFTER `id`;
ALTER TABLE `sa_mutes` CHANGE `player_name` `player_name` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL AFTER `id`;
ALTER TABLE `sa_admins` CHANGE `player_name` `player_name` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL AFTER `id`;
ALTER TABLE `sa_servers` CHANGE `hostname` `hostname` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL AFTER `id`;