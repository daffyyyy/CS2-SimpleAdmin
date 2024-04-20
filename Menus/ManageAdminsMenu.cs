using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class ManageAdminsMenu
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

			BaseMenu menu = AdminMenu.CreateMenu("Manage Admins");
			List<ChatMenuOptionData> options = new();

			// TODO: Localize options
			// options added in order

			options.Add(new ChatMenuOptionData("Add Admin", () => PlayersMenu.OpenRealPlayersMenu(admin, "Add Admin", AddAdminMenu)));
			options.Add(new ChatMenuOptionData("Remove Admin", () => PlayersMenu.OpenAdminPlayersMenu(admin, "Remove Admin", RemoveAdmin, player => player != admin && admin.CanTarget(player))));
			options.Add(new ChatMenuOptionData("Reload Admins", () => ReloadAdmins(admin)));

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void AddAdminMenu(CCSPlayerController admin, CCSPlayerController player)
		{
			Tuple<string, string>[] flags = new[]
			{
				new Tuple<string, string>("Generic", "@css/generic"),
				new Tuple<string, string>("Chat", "@css/chat"),
				new Tuple<string, string>("Change Map", "@css/changemap"),
				new Tuple<string, string>("Slay", "@css/slay"),
				new Tuple<string, string>("Kick", "@css/kick"),
				new Tuple<string, string>("Ban", "@css/ban"),
				new Tuple<string, string>("Unban", "@css/unban"),
				new Tuple<string, string>("Cheats", "@css/cheats"),
				new Tuple<string, string>("CVAR", "@css/cvar"),
				new Tuple<string, string>("RCON", "@css/rcon"),
				new Tuple<string, string>("Root", "@css/root"),
			};

			BaseMenu menu = AdminMenu.CreateMenu($"Add Admin: {player.PlayerName}");

			foreach (Tuple<string, string> flagsTuple in flags)
			{
				string optionName = flagsTuple.Item1;
				bool disabled = AdminManager.PlayerHasPermissions(player, flagsTuple.Item2);
				menu.AddMenuOption(optionName, (_, _) => { AddAdmin(admin, player, flagsTuple.Item2); }, disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void AddAdmin(CCSPlayerController admin, CCSPlayerController player, string flag)
		{
			// TODO: Change default immunity?
			CS2_SimpleAdmin.AddAdmin(admin, player.SteamID.ToString(), player.PlayerName, flag, 10);
		}

		private static void RemoveAdmin(CCSPlayerController admin, CCSPlayerController player)
		{
			CS2_SimpleAdmin.Instance.RemoveAdmin(admin, player.SteamID.ToString());
		}

		private static void ReloadAdmins(CCSPlayerController admin)
		{
			CS2_SimpleAdmin.Instance.ReloadAdmins(admin);
		}
	}
}