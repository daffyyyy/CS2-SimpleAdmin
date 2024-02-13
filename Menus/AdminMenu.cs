using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class AdminMenu
	{
		public static BaseMenu CreateMenu(string title)
		{
			return CS2_SimpleAdmin.Instance.Config.UseChatMenu ? new ChatMenu(title) : new CenterHtmlMenu(title);
		}

		public static void OpenMenu(CCSPlayerController player, BaseMenu menu)
		{
			if (menu is CenterHtmlMenu centerHtmlMenu)
			{
				MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, player, centerHtmlMenu);
			}
			else if (menu is ChatMenu chatMenu)
			{
				MenuManager.OpenChatMenu(player, chatMenu);
			}
		}

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

			BaseMenu menu = AdminMenu.CreateMenu("Simple Admin");
			List<ChatMenuOptionData> options = new()
			{
				new ChatMenuOptionData("Manage Players", () => ManagePlayersMenu.OpenMenu(admin)),
				new ChatMenuOptionData("Manage Server", () => ManageServerMenu.OpenMenu(admin)),
				new ChatMenuOptionData("Fun actions", () => FunActionsMenu.OpenMenu(admin)),
			};

			if (AdminManager.PlayerHasPermissions(admin, "@css/root"))
				options.Add(new ChatMenuOptionData("Manage Admins", () => ManageAdminsMenu.OpenMenu(admin)));

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			OpenMenu(admin, menu);
		}
	}
}
