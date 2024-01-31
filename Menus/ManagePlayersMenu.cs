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

			//bool xpRights = AdminManager.PlayerHasPermissions(admin, "@wcs/xp");

			CenterHtmlMenu menu = new CenterHtmlMenu("Manage Players");
			List<ChatMenuOptionData> options = new();

			// permissions
			bool hasSlay = AdminManager.PlayerHasPermissions(admin, "@css/slay");
			bool hasKick = AdminManager.PlayerHasPermissions(admin, "@css/kick");
			bool hasBan = AdminManager.PlayerHasPermissions(admin, "@css/ban");
			bool hasChat = AdminManager.PlayerHasPermissions(admin, "@css/chat");

			// options added in order
			options.Add(new ChatMenuOptionData("Who is", () => PlayersMenu.OpenMenu(admin, "Who is", WhoIs)));

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
			// TODO: WhoIs
		}

		private static void SlapMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			// TODO: Slap
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
