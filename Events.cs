using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		private void registerEvents()
		{
			RegisterListener<OnClientAuthorized>(OnClientAuthorized);
			RegisterListener<OnClientDisconnect>(OnClientDisconnect);
			RegisterListener<OnMapStart>(OnMapStart);
			AddCommandListener("say", OnCommandSay);
			AddCommandListener("say_team", OnCommandSay);
		}

		private HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
		{
			if (player == null || !player.IsValid || info.GetArg(1).Length == 0) return HookResult.Continue;

			if (gaggedPlayers.Contains((int)player.Index))
			{
				return HookResult.Handled;
			}

			return HookResult.Continue;
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
			MuteManager _muteManager = new(dbConnectionString);
			bool isBanned = _banManager.IsPlayerBanned(player.AuthorizedSteamID.SteamId64.ToString());
			List<dynamic> activeMutes = _muteManager.IsPlayerMuted(player.AuthorizedSteamID.SteamId64.ToString());

			if (activeMutes.Count > 0)
			{
				// Player is muted, handle mute
				foreach (var mute in activeMutes)
				{
					string muteType = mute.type;

					if (muteType == "GAG")
					{
						if (!gaggedPlayers.Contains((int)player.Index))
							gaggedPlayers.Add((int)player.Index);
					}
					else
					{
						continue;
					}
				}
			}

			// Player is banned, kick him
			if (isBanned)
			{
				Helper.KickPlayer(player.UserId, "Banned");
			}
		}

		private void OnClientDisconnect(int playerSlot)
		{
			CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return;

			gaggedPlayers.Remove((int)player.Index);
		}

		private void OnMapStart(string mapName)
		{
			AddTimer(120.0f, () =>
			{
				BanManager _banManager = new(dbConnectionString);
				_banManager.ExpireOldBans();
				MuteManager _muteManager = new(dbConnectionString);
				_muteManager.ExpireOldMutes();
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
		}
	}
}
