using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using System.Web;

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
			string playerName = string.Empty;

			foreach (CCSPlayerController player in players)
			{
				playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;

				string optionName = HttpUtility.HtmlEncode(playerName);
				if (enableFilter != null && enableFilter(player) == false)
					continue;

				bool enabled = admin.CanTarget(player);
				menu.AddMenuOption(optionName, (_, _) => { onSelectAction?.Invoke(admin, player); }, enabled == false);
			}

			AdminMenu.OpenMenu(admin, menu);
		}
	}
}
