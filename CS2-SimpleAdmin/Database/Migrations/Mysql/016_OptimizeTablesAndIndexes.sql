-- Migration 016: Optimize tables and indexes
-- Add proper indexes for all tables to improve query performance

-- Optimize sa_players_ips table indexes
-- Add index on used_at for efficient date-based queries
ALTER TABLE `sa_players_ips` ADD INDEX IF NOT EXISTS `idx_used_at` (`used_at` DESC);

-- Optimize sa_bans table indexes
-- Add composite indexes for common query patterns
CREATE INDEX IF NOT EXISTS `idx_bans_steamid_status` ON `sa_bans` (`player_steamid`, `status`);
CREATE INDEX IF NOT EXISTS `idx_bans_ip_status` ON `sa_bans` (`player_ip`, `status`);
CREATE INDEX IF NOT EXISTS `idx_bans_status_ends` ON `sa_bans` (`status`, `ends`);
CREATE INDEX IF NOT EXISTS `idx_bans_server_status` ON `sa_bans` (`server_id`, `status`, `ends`);
CREATE INDEX IF NOT EXISTS `idx_bans_created` ON `sa_bans` (`created` DESC);

-- Optimize sa_admins table indexes
CREATE INDEX IF NOT EXISTS `idx_admins_steamid` ON `sa_admins` (`player_steamid`);
CREATE INDEX IF NOT EXISTS `idx_admins_server_ends` ON `sa_admins` (`server_id`, `ends`);
CREATE INDEX IF NOT EXISTS `idx_admins_ends` ON `sa_admins` (`ends`);

-- Optimize sa_mutes table indexes (in addition to migration 014)
-- Add index for expire queries
CREATE INDEX IF NOT EXISTS `idx_mutes_status_ends` ON `sa_mutes` (`status`, `ends`);
CREATE INDEX IF NOT EXISTS `idx_mutes_server_status` ON `sa_mutes` (`server_id`, `status`, `ends`);
CREATE INDEX IF NOT EXISTS `idx_mutes_created` ON `sa_mutes` (`created` DESC);

-- Optimize sa_warns table indexes (if exists)
CREATE INDEX IF NOT EXISTS `idx_warns_steamid_status` ON `sa_warns` (`player_steamid`, `status`);
CREATE INDEX IF NOT EXISTS `idx_warns_status_ends` ON `sa_warns` (`status`, `ends`);
CREATE INDEX IF NOT EXISTS `idx_warns_server_status` ON `sa_warns` (`server_id`, `status`, `ends`);

-- Add index on sa_servers for faster lookups
CREATE INDEX IF NOT EXISTS `idx_servers_hostname` ON `sa_servers` (`hostname`);
