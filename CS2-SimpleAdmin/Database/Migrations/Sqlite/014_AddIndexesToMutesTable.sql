CREATE INDEX IF NOT EXISTS `idx_sa_mutes_steamid_status_ends` ON `sa_mutes` (`player_steamid`, `status`, `ends`);
CREATE INDEX IF NOT EXISTS `idx_sa_mutes_steamid_status_server_duration` ON `sa_mutes` (`player_steamid`, `status`, `server_id`, `duration`);
CREATE INDEX IF NOT EXISTS `idx_sa_mutes_steamid_type` ON `sa_mutes` (`player_steamid`, `type`);