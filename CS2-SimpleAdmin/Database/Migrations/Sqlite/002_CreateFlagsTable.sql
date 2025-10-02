CREATE TABLE IF NOT EXISTS `sa_admins_flags` (
                                                 `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                                 `admin_id` INTEGER NOT NULL,
                                                 `flag` VARCHAR(64) NOT NULL,
    FOREIGN KEY (`admin_id`) REFERENCES `sa_admins` (`id`) ON DELETE CASCADE
    );