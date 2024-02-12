using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

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

			BaseMenu menu = AdminMenu.CreateMenu("Manage Players");
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
				options.Add(new ChatMenuOptionData("Kick", () => PlayersMenu.OpenMenu(admin, "Kick", KickMenu)));
			}

			if (hasBan)
			{
				options.Add(new ChatMenuOptionData("Ban", () => PlayersMenu.OpenMenu(admin, "Ban", (admin, player) => DurationMenu.OpenMenu(admin, $"Ban: {player.PlayerName}", player, BanMenu))));
			}

			if (hasChat)
			{
				options.Add(new ChatMenuOptionData("Gag", () => PlayersMenu.OpenMenu(admin, "Gag", (admin, player) => DurationMenu.OpenMenu(admin, $"Gag: {player.PlayerName}", player, GagMenu))));
				options.Add(new ChatMenuOptionData("Mute", () => PlayersMenu.OpenMenu(admin, "Mute", (admin, player) => DurationMenu.OpenMenu(admin, $"Mute: {player.PlayerName}", player, MuteMenu))));
				options.Add(new ChatMenuOptionData("Silence", () => PlayersMenu.OpenMenu(admin, "Silence", (admin, player) => DurationMenu.OpenMenu(admin, $"Silence: {player.PlayerName}", player, SilenceMenu))));
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

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void SlapMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			BaseMenu menu = AdminMenu.CreateMenu($"Slap: {player.PlayerName}");
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

			AdminMenu.OpenMenu(admin, menu);
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

		private static void KickMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			BaseMenu menu = AdminMenu.CreateMenu($"Kick: {player.PlayerName}");
			List<string> options = new()
			{
				"Voice Abuse",
				"Chat Abuse",
				"Admin disrespect",
				"Other"
			};

			foreach (string option in options)
			{
				menu.AddMenuOption(option, (_, _) => { Kick(admin, player, option); });
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Kick(CCSPlayerController admin, CCSPlayerController player, string reason)
		{
			CS2_SimpleAdmin.Instance.Kick(admin, player, reason);
		}

		private static void BanMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			BaseMenu menu = AdminMenu.CreateMenu($"Ban: {player.PlayerName}");
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

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Ban(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
		{
			CS2_SimpleAdmin.Instance.Ban(admin, player, duration, reason);
		}

		private static void GagMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			// TODO: Localize and make options in config?
			BaseMenu menu = AdminMenu.CreateMenu($"Gag: {player.PlayerName}");
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

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Gag(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
		{
			CS2_SimpleAdmin.Instance.Gag(admin, player, duration, reason);
		}

		private static void MuteMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			// TODO: Localize and make options in config?
			BaseMenu menu = AdminMenu.CreateMenu($"Mute: {player.PlayerName}");
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

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Mute(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
		{
			CS2_SimpleAdmin.Instance.Mute(admin, player, duration, reason);
		}

		private static void SilenceMenu(CCSPlayerController admin, CCSPlayerController player, int duration)
		{
			// TODO: Localize and make options in config?
			BaseMenu menu = AdminMenu.CreateMenu($"Silence: {player.PlayerName}");
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
				menu.AddMenuOption(option, (_, _) => { Silence(admin, player, duration, option); });
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void Silence(CCSPlayerController admin, CCSPlayerController player, int duration, string reason)
		{
			CS2_SimpleAdmin.Instance.Silence(admin, player, duration, reason);
		}

		private static void ForceTeamMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			// TODO: Localize
			BaseMenu menu = AdminMenu.CreateMenu($"Force {player.PlayerName}'s Team");
			List<ChatMenuOptionData> options = new();
			options.Add(new ChatMenuOptionData("CT", () => ForceTeam(admin, player, "ct", CsTeam.CounterTerrorist)));
			options.Add(new ChatMenuOptionData("T", () => ForceTeam(admin, player, "t", CsTeam.Terrorist)));
			options.Add(new ChatMenuOptionData("Swap", () => ForceTeam(admin, player, "swap", CsTeam.Spectator)));
			options.Add(new ChatMenuOptionData("Spectator", () => ForceTeam(admin, player, "spec", CsTeam.Spectator)));

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void ForceTeam(CCSPlayerController admin, CCSPlayerController player, string teamName, CsTeam teamNum)
		{
			CS2_SimpleAdmin.Instance.ChangeTeam(admin, player, teamName, teamNum, true);
		}
	}
}
