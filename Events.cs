using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		private void registerEvents()
		{
			RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
			RegisterListener<Listeners.OnMapStart>(OnMapStart);
		}

		private void OnClientAuthorized(int playerSlot, SteamID steamID)
		{
			int playerIndex = playerSlot + 1;

			CCSPlayerController? player = Utilities.GetPlayerFromIndex(playerIndex);

			if (player.AuthorizedSteamID == null)
			{
				AddTimer(3.0f, () =>
				{
					OnClientAuthorized(playerSlot, steamID);
				});
				return;
			}

			BanManager _banManager = new(dbConnectionString);
			bool isBanned = _banManager.IsPlayerBanned(player.AuthorizedSteamID.SteamId64.ToString());

			if (isBanned)
			{
				Helper.KickPlayer(player.UserId, "Banned");
			}
		}

		private void OnMapStart(string mapName)
		{
			AddTimer(120.0f, () =>
			{
				BanManager _banManager = new(dbConnectionString);
				_banManager.ExpireOldBans();
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
		}
	}
}
