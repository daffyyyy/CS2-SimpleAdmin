using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class ManagePlayersMenu
	{
		public static void OpenMenu(CCSPlayerController admin)
		{
			if (admin == null || admin.IsValid == false)
				return;

			if (AdminManager.PlayerHasPermissions(admin, "@css/generic") == false)
			{
				// TODO: Localize
				admin.PrintToChat("[Simple Admin] You do not have permissions to use this command.");
				return;
			}

			CenterHtmlMenu menu = new CenterHtmlMenu("Manage Players");
			List<ChatMenuOptionData> options = new();

			// permissions
			bool hasSlay = AdminManager.PlayerHasPermissions(admin, "@css/slay");
			bool hasKick = AdminManager.PlayerHasPermissions(admin, "@css/kick");
			bool hasBan = AdminManager.PlayerHasPermissions(admin, "@css/ban");
			bool hasChat = AdminManager.PlayerHasPermissions(admin, "@css/chat");

			// options added in order
			options.Add(new ChatMenuOptionData("Who Is", () => PlayersMenu.OpenMenu(admin, "Who is", WhoIs)));

			if (hasSlay)
			{
				options.Add(new ChatMenuOptionData("Slap", () => PlayersMenu.OpenMenu(admin, "Slap", SlapMenu)));
				options.Add(new ChatMenuOptionData("Slay", () => PlayersMenu.OpenMenu(admin, "Slay", Slay)));
			}

			if (hasKick)
			{
				options.Add(new ChatMenuOptionData("Kick", () => PlayersMenu.OpenMenu(admin, "Kick", Kick)));
			}

			if (hasBan)
			{
				options.Add(new ChatMenuOptionData("Ban", () => PlayersMenu.OpenMenu(admin, "Ban", (admin, player) => DurationMenu.OpenMenu(admin, "Ban", player, Ban))));
			}

			if (hasChat)
			{
				options.Add(new ChatMenuOptionData("Gag", () => PlayersMenu.OpenMenu(admin, "Gag", (admin, player) => DurationMenu.OpenMenu(admin, "Gag", player, Gag))));
				options.Add(new ChatMenuOptionData("Mute", () => PlayersMenu.OpenMenu(admin, "Mute", (admin, player) => DurationMenu.OpenMenu(admin, "Mute", player, Mute))));
			}

			if (hasKick)
			{
				options.Add(new ChatMenuOptionData("Force Team", () => PlayersMenu.OpenMenu(admin, "Force Team", ForceTeam)));
			}

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}

		private static void WhoIs(CCSPlayerController admin, CCSPlayerController player)
		{
			BanManager banManager = new(CS2_SimpleAdmin.Instance.dbConnectionString, CS2_SimpleAdmin.Instance.Config);
			MuteManager muteManager = new(CS2_SimpleAdmin.Instance.dbConnectionString);

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Index = (int)player.Index,
				SteamId = player?.AuthorizedSteamID?.SteamId64.ToString(),
				Name = player?.PlayerName,
				IpAddress = player?.IpAddress?.Split(":")[0]
			};

			Task.Run(async () =>
			{
				int totalBans = 0;
				int totalMutes = 0;

				totalBans = await banManager.GetPlayerBans(playerInfo);
				totalMutes = await muteManager.GetPlayerMutes(playerInfo.SteamId!);

				Server.NextFrame(() =>
				{
					Action<string> printMethod = admin == null ? Server.PrintToConsole : admin.PrintToConsole;
					printMethod($"--------- INFO ABOUT \"{playerInfo.Name}\" ---------");

					printMethod($"• Clan: \"{player!.Clan}\" Name: \"{playerInfo.Name}\"");
					printMethod($"• UserID: \"{playerInfo.UserId}\"");
					if (playerInfo.SteamId != null)
						printMethod($"• SteamID64: \"{playerInfo.SteamId}\"");
					if (player.AuthorizedSteamID != null)
					{
						printMethod($"• SteamID2: \"{player.AuthorizedSteamID.SteamId2}\"");
						printMethod($"• Community link: \"{player.AuthorizedSteamID.ToCommunityUrl()}\"");
					}

					if (playerInfo.IpAddress != null)
						printMethod($"• IP Address: \"{playerInfo.IpAddress}\"");
					printMethod($"• Ping: \"{player.Ping}\"");
					if (player.AuthorizedSteamID != null)
					{
						printMethod($"• Total Bans: \"{totalBans}\"");
						printMethod($"• Total Mutes: \"{totalMutes}\"");
					}

					printMethod($"--------- END INFO ABOUT \"{player.PlayerName}\" ---------");
				});
			});
		}

		private static void SlapMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			CenterHtmlMenu menu = new CenterHtmlMenu($"Slap {player.PlayerName}");
			List<ChatMenuOptionData> options = new();

			// options added in order
			options.Add(new ChatMenuOptionData("0 hp", () => ApplySlapAndKeepMenu(admin, player, 0)));
			options.Add(new ChatMenuOptionData("1 hp", () => ApplySlapAndKeepMenu(admin, player, 1)));
			options.Add(new ChatMenuOptionData("5 hp", () => ApplySlapAndKeepMenu(admin, player, 5)));
			options.Add(new ChatMenuOptionData("10 hp", () => ApplySlapAndKeepMenu(admin, player, 10)));
			options.Add(new ChatMenuOptionData("50 hp", () => ApplySlapAndKeepMenu(admin, player, 50)));
			options.Add(new ChatMenuOptionData("100 hp", () => ApplySlapAndKeepMenu(admin, player, 100)));

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}

		private static void ApplySlapAndKeepMenu(CCSPlayerController admin, CCSPlayerController player, int i)
		{
			CS2_SimpleAdmin.Instance.Slap(admin, player, i);
			SlapMenu(admin, player);
		}

		private static void Slay(CCSPlayerController admin, CCSPlayerController player)
		{
			// TODO: Slay
		}

		private static void Kick(CCSPlayerController admin, CCSPlayerController player)
		{
			// TODO: Kick
		}

		private static void Ban(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			// TODO: Ban
		}

		private static void Gag(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			// TODO: Gag
		}

		private static void Mute(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			// TODO: Mute
		}

		private static void ForceTeam(CCSPlayerController admin, CCSPlayerController player)
		{
			// TODO: ForceTeam
		}
	}
}
