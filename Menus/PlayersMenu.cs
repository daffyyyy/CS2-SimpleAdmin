using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class PlayersMenu
	{
		public static void OpenMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction)
		{
			CenterHtmlMenu menu = new CenterHtmlMenu(menuName);

			IEnumerable<CCSPlayerController> players = Utilities.GetPlayers().Where(p => p.Connected == PlayerConnectedState.PlayerConnected);
			foreach (CCSPlayerController player in players)
			{
				string optionName = player.PlayerName;
				menu.AddMenuOption(optionName, (_, _) => { onSelectAction?.Invoke(admin, player); });
			}

			MenuManager.OpenCenterHtmlMenu(CS2_SimpleAdmin.Instance, admin, menu);
		}
	}
}
