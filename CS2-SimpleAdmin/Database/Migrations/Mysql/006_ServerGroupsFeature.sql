CREATE TABLE IF NOT EXISTS `sa_groups` (
 `id` int(11) NOT NULL AUTO_INCREMENT,
 `name` varchar(255) NOT NULL,
 `immunity` int(11) NOT NULL DEFAULT 0,
 PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_groups_flags` (
 `id` int(11) NOT NULL AUTO_INCREMENT,
 `group_id` int(11) NOT NULL,
 `flag` varchar(64) NOT NULL,
 PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `sa_groups_servers` (
 `id` int(11) NOT NULL AUTO_INCREMENT,
 `group_id` int(11) NOT NULL,
 `server_id` int(11) NOT NULL,
 PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

ALTER TABLE `sa_admins` ADD `group_id` INT NULL AFTER `created`;
ALTER TABLE `sa_groups_flags` ADD FOREIGN KEY (`group_id`) REFERENCES `sa_groups`(`id`) ON DELETE CASCADE;
ALTER TABLE `sa_groups_servers` ADD FOREIGN KEY (`group_id`) REFERENCES `sa_groups`(`id`) ON DELETE CASCADE;
ALTER TABLE `sa_admins` ADD FOREIGN KEY (`group_id`) REFERENCES `sa_groups`(`id`) ON DELETE SET NULL;