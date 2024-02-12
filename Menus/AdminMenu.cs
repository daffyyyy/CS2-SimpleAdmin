using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class AdminMenu
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

			CenterHtmlMenu menu = new CenterHtmlMenu("Simple Admin");
			ChatMenuOptionData[] options = new[]
			{
				new ChatMenuOptionData("Manage Players", () => ManagePlayersMenu.OpenMenu(admin)),
				new ChatMenuOptionData("Manage Server", () => ManageServerMenu.OpenMenu(admin)),
				new ChatMenuOptionData("Fun actions", null),
				new ChatMenuOptionData("Manage Admins", null)
			};

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}
	}
}
