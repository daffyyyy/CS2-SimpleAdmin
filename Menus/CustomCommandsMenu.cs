using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class CustomCommandsMenu
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

			BaseMenu menu = AdminMenu.CreateMenu("Custom Commands");
			List<ChatMenuOptionData> options = new();

			List<CustomServerCommandData> customCommands = CS2_SimpleAdmin.Instance.Config.CustomServerCommands;
			foreach (CustomServerCommandData customCommand in customCommands)
			{
				if (string.IsNullOrEmpty(customCommand.DisplayName) || string.IsNullOrEmpty(customCommand.Command))
					continue;

				bool hasRights = AdminManager.PlayerHasPermissions(admin, customCommand.Flag);
				if (!hasRights)
					continue;
				
				options.Add(new ChatMenuOptionData(customCommand.DisplayName, () =>
				{
					Helper.TryLogCommandOnDiscord(admin, customCommand.Command);
					
					if (customCommand.ExecuteOnClient)
						admin.ExecuteClientCommand(customCommand.Command);
					else
						Server.ExecuteCommand(customCommand.Command);
				}));
			}

			foreach (ChatMenuOptionData menuOptionData in options)
			{
				string menuName = menuOptionData.name;
				menu.AddMenuOption(menuName, (_, _) => { menuOptionData.action?.Invoke(); }, menuOptionData.disabled);
			}

			AdminMenu.OpenMenu(admin, menu);
		}
	}
}
