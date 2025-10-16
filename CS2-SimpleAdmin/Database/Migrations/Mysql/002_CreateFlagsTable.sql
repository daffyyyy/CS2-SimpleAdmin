CREATE TABLE IF NOT EXISTS `sa_admins_flags` (
 `id` int(11) NOT NULL AUTO_INCREMENT,
 `admin_id` int(11) NOT NULL,
 `flag` varchar(64) NOT NULL,
 PRIMARY KEY (`id`),
 FOREIGN KEY (`admin_id`) REFERENCES `sa_admins` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

ALTER TABLE `sa_admins` CHANGE `flags` `flags` TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL;
