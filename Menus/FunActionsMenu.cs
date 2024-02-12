using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class FunActionsMenu
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

			BaseMenu menu = AdminMenu.CreateMenu("Fun Actions");
			List<ChatMenuOptionData> options = new();

			// permissions
			bool hasCheats = AdminManager.PlayerHasPermissions(admin, "@css/cheats");
			bool hasSlay = AdminManager.PlayerHasPermissions(admin, "@css/slay");

			// TODO: Localize options
			// options added in order

			if (hasCheats)
			{
				options.Add(new ChatMenuOptionData("God Mode", () => PlayersMenu.OpenMenu(admin, "God Mode", GodMode)));
				options.Add(new ChatMenuOptionData("No Clip", () => PlayersMenu.OpenMenu(admin, "No Clip", NoClip)));
				options.Add(new ChatMenuOptionData("Respawn", () => PlayersMenu.OpenMenu(admin, "Respawn", Respawn)));
				options.Add(new ChatMenuOptionData("Give Weapon", () => PlayersMenu.OpenMenu(admin, "Give Weapon", GiveWeaponMenu)));
			}

			if (hasSlay)
			{
				options.Add(new ChatMenuOptionData("Strip All Weapons", () => PlayersMenu.OpenMenu(admin, "Strip All Weapons", StripWeapons)));
				options.Add(new ChatMenuOptionData("Freeze", () => PlayersMenu.OpenMenu(admin, "Freeze", Freeze)));
				options.Add(new ChatMenuOptionData("HP", () => PlayersMenu.OpenMenu(admin, "HP", HP)));
				options.Add(new ChatMenuOptionData("Speed", () => PlayersMenu.OpenMenu(admin, "Speed", Speed)));
			}


			options.Add(new ChatMenuOptionData("Restart Game", () => CS2_SimpleAdmin.Instance.RestartGame(admin)));

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}


		private static void GodMode(CCSPlayerController admin, CCSPlayerController player) { }

		private static void NoClip(CCSPlayerController admin, CCSPlayerController player) { }

		private static void Respawn(CCSPlayerController admin, CCSPlayerController player) { }

		private static void GiveWeaponMenu(CCSPlayerController admin, CCSPlayerController player) { }

		private static void StripWeapons(CCSPlayerController admin, CCSPlayerController player) { }

		private static void Freeze(CCSPlayerController admin, CCSPlayerController player) { }

		private static void HP(CCSPlayerController admin, CCSPlayerController player) { }

		private static void Speed(CCSPlayerController admin, CCSPlayerController player) { }
	}
}
