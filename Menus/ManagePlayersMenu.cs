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

			// TODO: Localize options
			// options added in order

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
				options.Add(new ChatMenuOptionData("Ban", () => PlayersMenu.OpenMenu(admin, "Ban", (admin, player) => DurationMenu.OpenMenu(admin, "Ban", player, BanMenu))));
			}

			if (hasChat)
			{
				options.Add(new ChatMenuOptionData("Gag", () => PlayersMenu.OpenMenu(admin, "Gag", (admin, player) => DurationMenu.OpenMenu(admin, "Gag", player, GagMenu))));
				options.Add(new ChatMenuOptionData("Mute", () => PlayersMenu.OpenMenu(admin, "Mute", (admin, player) => DurationMenu.OpenMenu(admin, "Mute", player, MuteMenu))));
			}

			if (hasKick)
			{
				options.Add(new ChatMenuOptionData("Force Team", () => PlayersMenu.OpenMenu(admin, "Force Team", ForceTeamMenu)));
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
			CS2_SimpleAdmin.Instance.Who(admin, player);
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

		private static void ApplySlapAndKeepMenu(CCSPlayerController admin, CCSPlayerController player, int damage)
		{
			CS2_SimpleAdmin.Instance.Slap(admin, player, damage);
			SlapMenu(admin, player);
		}

		private static void Slay(CCSPlayerController admin, CCSPlayerController player)
		{
			CS2_SimpleAdmin.Instance.Slay(admin, player);
		}

		private static void Kick(CCSPlayerController admin, CCSPlayerController player)
		{
			CS2_SimpleAdmin.Instance.Kick(admin, player);
		}

		private static void BanMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			CenterHtmlMenu menu = new CenterHtmlMenu($"Ban {player.PlayerName}");
			List<string> options = new()
			{
				"Hacking",
				"Voice Abuse",
				"Chat Abuse",
				"Admin disrespect",
				"Other"
			};

			foreach (string option in options)
			{
				menu.AddMenuOption(option, (_, _) => { Ban(admin, player, duration, option); });
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}

		private static void Ban(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
		{
			CS2_SimpleAdmin.Instance.Ban(admin, player, duration, reason);
		}

		private static void GagMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			// TODO: Localize and make options in config?
			CenterHtmlMenu menu = new CenterHtmlMenu($"Gag {player.PlayerName}");
			List<string> options = new()
			{
				"Advertising",
				"Spamming",
				"Spectator camera abuse",
				"Hate",
				"Admin disrespect",
				"Other"
			};

			foreach (string option in options)
			{
				menu.AddMenuOption(option, (_, _) => { Gag(admin, player, duration, option); });
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}

		private static void Gag(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
		{
			CS2_SimpleAdmin.Instance.Gag(admin, player, duration, reason);
		}

		private static void MuteMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			// TODO: Localize and make options in config?
			CenterHtmlMenu menu = new CenterHtmlMenu($"Mute {player.PlayerName}");
			List<string> options = new()
			{
				"Shouting",
				"Playing music",
				"Advertising",
				"Spamming",
				"Spectator camera abuse",
				"Hate",
				"Admin disrespect",
				"Other"
			};

			foreach (string option in options)
			{
				menu.AddMenuOption(option, (_, _) => { Mute(admin, player, duration, option); });
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}

		private static void Mute(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
		{
			CS2_SimpleAdmin.Instance.Mute(admin, player, duration, reason);
		}

		private static void ForceTeamMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			// TODO: Localize
			CenterHtmlMenu menu = new CenterHtmlMenu($"Force {player.PlayerName}'s Team");
			List<ChatMenuOptionData> options = new();
			options.Add(new ChatMenuOptionData("CT", () => ForceTeam(admin, player, "ct")));
			options.Add(new ChatMenuOptionData("T", () => ForceTeam(admin, player, "t")));
			options.Add(new ChatMenuOptionData("Swap", () => ForceTeam(admin, player, "swap")));
			options.Add(new ChatMenuOptionData("Spectator", () => ForceTeam(admin, player, "spec")));

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}

		private static void ForceTeam(CCSPlayerController admin, CCSPlayerController player, string teamName)
		{
			// TODO: ForceTeam
		}
	}
}
