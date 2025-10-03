using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

public class ChatManager
{
	public void AddChatMessageDB(string playerSteam64, string playerName, string message, bool? team)
	{
		if (CS2_SimpleAdmin.DatabaseProvider == null) return;

		Task.Run(async () =>
		{
			try
			{
				await using var connection = await CS2_SimpleAdmin.DatabaseProvider.CreateConnectionAsync();

				var sql = "INSERT INTO `sa_chatlogs` (`playerSteam64`, `playerName`, `message`, `team`, `created`, `serverId`) " +
					"VALUES (@playerSteam64, @playerName, @message, @team, @created, @serverId)";
				int? serverId = CS2_SimpleAdmin.ServerId;
				if (serverId == null)
					return;
				DateTime now = DateTime.Now;
				await connection.ExecuteAsync(sql, new
				{
					playerSteam64,
					playerName,
					message,
					team = team ?? null,
					created = now,
					serverid = serverId
				});
			}
			catch (Exception e)
			{
				CS2_SimpleAdmin._logger?.LogError(e.Message);
			}
		});
	}
}