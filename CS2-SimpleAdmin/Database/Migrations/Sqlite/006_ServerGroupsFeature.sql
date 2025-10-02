CREATE TABLE IF NOT EXISTS `sa_groups` (
                                           `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                           `name` VARCHAR(255) NOT NULL,
    `immunity` INTEGER NOT NULL DEFAULT 0
    );

CREATE TABLE IF NOT EXISTS `sa_groups_flags` (
                                                 `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                                 `group_id` INTEGER NOT NULL,
                                                 `flag` VARCHAR(64) NOT NULL,
    FOREIGN KEY (`group_id`) REFERENCES `sa_groups` (`id`) ON DELETE CASCADE
    );

CREATE TABLE IF NOT EXISTS `sa_groups_servers` (
                                                   `id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                                   `group_id` INTEGER NOT NULL,
                                                   `server_id` INTEGER NULL,
                                                   FOREIGN KEY (`group_id`) REFERENCES `sa_groups` (`id`) ON DELETE CASCADE
    );

ALTER TABLE `sa_admins` ADD `group_id` INTEGER NULL;