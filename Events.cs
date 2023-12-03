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

			_ = _banManager.CheckBan(player);
			_ = _muteManager.CheckMute(player);
		}

		private void OnClientDisconnect(int playerSlot)
		{
			CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

			if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return;

			if (gaggedPlayers.Contains((int)player.Index))
			{
				if (gaggedPlayers.TryTake(out int removedItem) && removedItem != (int)player.Index)
				{
					gaggedPlayers.Add(removedItem);
				}
			}

			if (TagsDetected)
				NativeAPI.IssueServerCommand($"css_tag_unmute {player!.Index.ToString()}");
		}

		private void OnMapStart(string mapName)
		{
			AddTimer(120.0f, () =>
			{
				BanManager _banManager = new(dbConnectionString);
				_ = _banManager.ExpireOldBans();
				MuteManager _muteManager = new(dbConnectionString);
				_ = _muteManager.ExpireOldMutes();
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT | CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			string? path = Path.GetDirectoryName(ModuleDirectory);
			if (Directory.Exists(path + "/CS2-Tags"))
			{
				TagsDetected = true;
			}
		}
	}
}
