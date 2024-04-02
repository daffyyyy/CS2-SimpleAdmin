using System.Web;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class PlayersMenu
	{
		public static void OpenRealPlayersMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
		{
			OpenMenu(admin, menuName, onSelectAction, p => p.IsBot == false);
		}

		public static void OpenAdminPlayersMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
		{
			OpenMenu(admin, menuName, onSelectAction, p => AdminManager.GetPlayerAdminData(p)?.Flags?.Count > 0);
		}

		public static void OpenAliveMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
		{
			OpenMenu(admin, menuName, onSelectAction, p => p.PawnIsAlive);
		}

		public static void OpenDeadMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
		{
			OpenMenu(admin, menuName, onSelectAction, p => p.PawnIsAlive == false);
		}

		public static void OpenMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter = null)
		{
			BaseMenu menu = AdminMenu.CreateMenu(menuName);

			IEnumerable<CCSPlayerController> players = Helper.GetValidPlayersWithBots();
			foreach (CCSPlayerController player in players)
			{
				string optionName = HttpUtility.HtmlEncode(player.PlayerName);
				if (enableFilter != null && enableFilter(player) == false)
					continue;

				bool enabled = admin.CanTarget(player);
				menu.AddMenuOption(optionName, (_, _) => { onSelectAction?.Invoke(admin, player); }, enabled == false);
			}

			AdminMenu.OpenMenu(admin, menu);
		}
	}
}
