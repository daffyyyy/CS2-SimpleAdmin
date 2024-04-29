using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace CS2_SimpleAdmin.Menus
{
	public static class ManageServerMenu
	{
		public static void OpenMenu(CCSPlayerController admin)
		{
			if (admin.IsValid == false)
				return;

			var localizer = CS2_SimpleAdmin._localizer;
			if (AdminManager.PlayerHasPermissions(admin, "@css/generic") == false)
			{
				admin.PrintToChat(localizer?["sa_prefix"] ??
				                  "[SimpleAdmin] " + 
				                  (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command")
				);
				return;
			}

			var menu = AdminMenu.CreateMenu(localizer?["sa_menu_server_manage"] ?? "Server Manage");
			List<ChatMenuOptionData> options = [];

			// permissions
			bool hasMap = AdminManager.PlayerHasPermissions(admin, "@css/changemap");

			// options added in order

			if (hasMap)
			{
				options.Add(new ChatMenuOptionData(localizer?["sa_changemap"] ?? "Change Map", () => ChangeMapMenu(admin)));
			}

			options.Add(new ChatMenuOptionData(localizer?["sa_restart_game"] ?? "Restart Game", () => CS2_SimpleAdmin.RestartGame(admin)));

			foreach (var menuOptionData in options)
			{
				var menuName = menuOptionData.Name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void ChangeMapMenu(CCSPlayerController admin)
		{
			var menu = AdminMenu.CreateMenu(CS2_SimpleAdmin._localizer?["sa_changemap"] ?? "Change Map");
			List<ChatMenuOptionData> options = [];

			var maps = CS2_SimpleAdmin.Instance.Config.DefaultMaps;
			options.AddRange(maps.Select(map => new ChatMenuOptionData(map, () => ExecuteChangeMap(admin, map, false))));

			var wsMaps = CS2_SimpleAdmin.Instance.Config.WorkshopMaps;
			options.AddRange(wsMaps.Select(map => new ChatMenuOptionData($"{map} (WS)", () => ExecuteChangeMap(admin, map, true))));

			foreach (var menuOptionData in options)
			{
				var menuName = menuOptionData.Name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}

		private static void ExecuteChangeMap(CCSPlayerController admin, string mapName, bool workshop)
		{
			if (workshop)
				CS2_SimpleAdmin.Instance.ChangeWorkshopMap(admin, mapName);
			else
				CS2_SimpleAdmin.Instance.ChangeMap(admin, mapName);
		}
	}
}