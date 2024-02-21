using CounterStrikeSharp.API.Core;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin
{
	internal class MuteManager
	{
		private readonly Database _database;

		public MuteManager(Database database)
		{
			_database = database;
		}

		public async Task MutePlayer(PlayerInfo player, PlayerInfo issuer, string reason, int time = 0, int type = 0)
		{
			if (player == null || player.SteamId == null) return;

			await using var connection = await _database.GetConnectionAsync();

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			string muteType = "GAG";
			if (type == 1)
				muteType = "MUTE";
			else if (type == 2)
				muteType = "SILENCE";

			var sql = "INSERT INTO `sa_mutes` (`player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`, `server_id`) " +
				"VALUES (@playerSteamid, @playerName, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @type, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = player.SteamId,
				playerName = player.Name,
				adminSteamid = issuer.SteamId == null ? "Console" : issuer.SteamId,
				adminName = issuer.SteamId == null ? "Console" : issuer.Name,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType,
				serverid = CS2_SimpleAdmin.ServerId
			});
		}

		public async Task AddMuteBySteamid(string playerSteamId, PlayerInfo issuer, string reason, int time = 0, int type = 0)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			await using var connection = await _database.GetConnectionAsync();

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			string muteType = "GAG";
			if (type == 1)
				muteType = "MUTE";
			else if (type == 2)
				muteType = "SILENCE";

			var sql = "INSERT INTO `sa_mutes` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`, `server_id`) " +
				"VALUES (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @type, @serverid)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = playerSteamId,
				adminSteamid = issuer.SteamId == null ? "Console" : issuer.SteamId,
				adminName = issuer.Name == null ? "Console" : issuer.Name,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType,
				serverid = CS2_SimpleAdmin.ServerId
			});
		}

		public async Task<List<dynamic>> IsPlayerMuted(string steamId)
		{
			if (string.IsNullOrEmpty(steamId))
			{
				return new List<dynamic>();
			}

#if DEBUG
			if (CS2_SimpleAdmin._logger != null)
				CS2_SimpleAdmin._logger.LogCritical($"IsPlayerMuted for {steamId}");
#endif

			try
			{
				await using var connection = await _database.GetConnectionAsync();
				DateTime currentTime = DateTime.Now;
				string sql = "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)";

				var parameters = new { PlayerSteamID = steamId, CurrentTime = currentTime };
				var activeMutes = (await connection.QueryAsync(sql, parameters)).ToList();
				return activeMutes;
			}
			catch (Exception)
			{
				return new List<dynamic>();
			}
		}

		public async Task<int> GetPlayerMutes(string steamId)
		{
			await using var connection = await _database.GetConnectionAsync();

			int muteCount;
			string sql = "SELECT COUNT(*) FROM sa_mutes WHERE player_steamid = @PlayerSteamID";

			muteCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = steamId });

			return muteCount;
		}

		public async Task UnmutePlayer(string playerPattern, int type = 0)
		{
			if (playerPattern == null || playerPattern.Length <= 1)
			{
				return;
			}

			await using var connection = await _database.GetConnectionAsync();

			if (type == 2)
			{
				string _unbanSql = "UPDATE sa_mutes SET status = 'UNMUTED' WHERE (player_steamid = @pattern OR player_name = @pattern) AND status = 'ACTIVE'";
				await connection.ExecuteAsync(_unbanSql, new { pattern = playerPattern });

				return;
			}

			string muteType = "GAG";
			if (type == 1)
			{
				muteType = "MUTE";
			}
			else if (type == 2)
				muteType = "SILENCE";

			string sqlUnban = "UPDATE sa_mutes SET status = 'UNMUTED' WHERE (player_steamid = @pattern OR player_name = @pattern) AND type = @muteType AND status = 'ACTIVE'";
			await connection.ExecuteAsync(sqlUnban, new { pattern = playerPattern, muteType });
		}

		public async Task ExpireOldMutes()
		{
			try
			{
				await using var connection = await _database.GetConnectionAsync();

				string sql = "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
				await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.Now });
			}
			catch (Exception)
			{
				if (CS2_SimpleAdmin._logger != null)
					CS2_SimpleAdmin._logger.LogCritical("Unable to remove expired mutes");
			}
		}

		public async Task CheckMute(PlayerInfo player)
		{
			if (player.UserId == null || player.SteamId == null) return;

			string steamId = player.SteamId;
			List<dynamic> activeMutes = await IsPlayerMuted(steamId);

			if (activeMutes.Count > 0)
			{
				foreach (var mute in activeMutes)
				{
					string muteType = mute.type;
					TimeSpan duration = mute.ends - mute.created;
					int durationInSeconds = (int)duration.TotalSeconds;

					if (muteType == "GAG")
					{
						if (CS2_SimpleAdmin.TagsDetected)
							NativeAPI.IssueServerCommand($"css_tag_mute {player!.SteamId}");

						/*
						CCSPlayerController currentPlayer = player;

						if (mute.duration == 0 || durationInSeconds >= 1800) continue;

						await Task.Delay(TimeSpan.FromSeconds(durationInSeconds));

						if (currentPlayer != null && currentPlayer.IsValid)
						{
							NativeAPI.IssueServerCommand($"css_tag_unmute {currentPlayer.Index.ToString()}");
							await UnmutePlayer(currentPlayer.AuthorizedSteamID.SteamId64.ToString(), 0);
						}
						*/
					}
					else
					{
						// Mic mute
					}
				}
			}
		}
	}
}