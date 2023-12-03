using CounterStrikeSharp.API.Core;
using Dapper;
using MySqlConnector;
using System.Data;

namespace CS2_SimpleAdmin
{
	public class MuteManager
	{
		private readonly MySqlConnection _dbConnection;

		public MuteManager(string connectionString)
		{
			_dbConnection = new MySqlConnection(connectionString);
		}

		public async Task MutePlayer(CCSPlayerController? player, CCSPlayerController? issuer, string reason, int time = 0, int type = 0)
		{
			if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			string muteType = "GAG";
			if (type == 1)
				muteType = "MUTE";

			var sql = "INSERT INTO `sa_mutes` (`player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`) " +
				"VALUES (@playerSteamid, @playerName, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @type)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = player.AuthorizedSteamID.SteamId64.ToString(),
				playerName = player.PlayerName,
				adminSteamid = issuer == null ? "Console" : issuer?.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType,
			});

			if (connection.State != ConnectionState.Closed)
			{
				connection.Close();
			}
		}

		public async Task AddMuteBySteamid(string playerSteamId, CCSPlayerController? issuer, string reason, int time = 0, int type = 0)
		{
			if (string.IsNullOrEmpty(playerSteamId)) return;

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			DateTime now = DateTime.Now;
			DateTime futureTime = now.AddMinutes(time);

			string muteType = "GAG";
			if (type == 1)
				muteType = "MUTE";

			var sql = "INSERT INTO `sa_mutes` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `type`) " +
				"VALUES (@playerSteamid, @adminSteamid, @adminName, @banReason, @duration, @ends, @created, @type)";

			await connection.ExecuteAsync(sql, new
			{
				playerSteamid = playerSteamId,
				adminSteamid = issuer == null ? "Console" : issuer?.AuthorizedSteamID?.SteamId64.ToString(),
				adminName = issuer == null ? "Console" : issuer.PlayerName,
				banReason = reason,
				duration = time,
				ends = futureTime,
				created = now,
				type = muteType
			});

			if (connection.State != ConnectionState.Closed)
			{
				connection.Close();
			}
		}

		public async Task<List<dynamic>> IsPlayerMuted(string steamId)
		{
			await using var connection = _dbConnection;
			await connection.OpenAsync();

			DateTime now = DateTime.Now;

			string sql = "SELECT * FROM sa_mutes WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' AND (duration = 0 OR ends > @CurrentTime)";
			var activeMutes = (await connection.QueryAsync(sql, new { PlayerSteamID = steamId, CurrentTime = now })).ToList();

			if (connection.State != ConnectionState.Closed)
			{
				connection.Close();
			}

			return activeMutes;
		}

		public async Task UnmutePlayer(string playerPattern, int type = 0)
		{
			if (playerPattern == null || playerPattern.Length <= 1)
			{
				return;
			}

			await using var connection = _dbConnection;
			await connection.OpenAsync();

			if (type == 2)
			{
				string _unbanSql = "UPDATE sa_mutes SET status = 'UNMUTED' WHERE (player_steamid = @pattern OR player_name = @pattern) AND status = 'ACTIVE'";
				await connection.ExecuteAsync(_unbanSql, new { pattern = playerPattern });

				if (connection.State != ConnectionState.Closed)
				{
					connection.Close();
				}

				return;
			}

			string muteType = "GAG";
			if (type == 1)
			{
				muteType = "MUTE";
			}

			string sqlUnban = "UPDATE sa_mutes SET status = 'UNMUTED' WHERE (player_steamid = @pattern OR player_name = @pattern) AND type = @muteType AND status = 'ACTIVE'";
			await connection.ExecuteAsync(sqlUnban, new { pattern = playerPattern, muteType });

			if (connection.State != ConnectionState.Closed)
			{
				connection.Close();
			}
		}

		public async Task ExpireOldMutes()
		{
			await using var connection = _dbConnection;
			await connection.OpenAsync();

			string sql = "UPDATE sa_mutes SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime";
			await connection.ExecuteAsync(sql, new { CurrentTime = DateTime.Now });

			if (connection.State != ConnectionState.Closed)
			{
				connection.Close();
			}
		}

		public async Task CheckMute(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return;

			string steamId = player.AuthorizedSteamID.SteamId64.ToString();
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
						if (!CS2_SimpleAdmin.gaggedPlayers.Contains((int)player.Index))
							CS2_SimpleAdmin.gaggedPlayers.Add((int)player.Index);

						if (CS2_SimpleAdmin.TagsDetected)
							NativeAPI.IssueServerCommand($"css_tag_mute {player!.Index.ToString()}");

						CCSPlayerController currentPlayer = player;

						if (mute.duration == 0 || durationInSeconds >= 1800) continue;

						await Task.Delay(TimeSpan.FromSeconds(durationInSeconds));

						if (currentPlayer != null && currentPlayer.IsValid)
						{
							NativeAPI.IssueServerCommand($"css_tag_unmute {currentPlayer.Index.ToString()}");
							await UnmutePlayer(currentPlayer.AuthorizedSteamID.SteamId64.ToString(), 0);
						}
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
