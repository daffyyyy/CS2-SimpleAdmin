TRUNCATE TABLE `sa_players_ips`;

ALTER TABLE `sa_players_ips` ADD `name` VARCHAR(64) NULL DEFAULT NULL;
CREATE INDEX IF NOT EXISTS `idx_sa_players_ips_used_at` ON `sa_players_ips` (`used_at` DESC);