using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class ManageServerMenu
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

			CenterHtmlMenu menu = new CenterHtmlMenu("Manage Server");
			List<ChatMenuOptionData> options = new();

			// permissions
			bool hasMap = AdminManager.PlayerHasPermissions(admin, "@css/changemap");

			// TODO: Localize options
			// options added in order

			if (hasMap)
			{
				options.Add(new ChatMenuOptionData("Change Map", () => ChangeMapMenu(admin)));
			}

			options.Add(new ChatMenuOptionData("Restart Game", () => CS2_SimpleAdmin.Instance.RestartGame(admin)));

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}

		public static void ChangeMapMenu(CCSPlayerController admin)
		{
			CenterHtmlMenu menu = new CenterHtmlMenu($"Change Map");
			List<ChatMenuOptionData> options = new();

			string[] maps = Server.GetMapList();
			foreach (string map in maps)
			{
				options.Add(new ChatMenuOptionData(map, () => ExecuteChangeMap(admin, map, false)));
			}

			List<string> wsMaps = new(); // TODO: Get from config to add workshopmaps
			foreach (string map in wsMaps)
			{
				options.Add(new ChatMenuOptionData($"{map} (WS)", () => ExecuteChangeMap(admin, map, true)));
			}

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}

		private static void ExecuteChangeMap(CCSPlayerController admin, string mapName, bool workshop)
		{
			if (workshop)
				CS2_SimpleAdmin.Instance.ChangeWSMap(admin, mapName);
		}
	}
}