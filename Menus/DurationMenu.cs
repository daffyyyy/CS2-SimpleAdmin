using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace CS2_SimpleAdmin.Menus
{
	public static class DurationMenu
	{
		// TODO: Localize
		public static Tuple<string, int>[] _durations = new[]
		{
			new Tuple<string, int>("1 minute", 1),
			new Tuple<string, int>("5 minutes", 5),
			new Tuple<string, int>("15 minutes", 15),
			new Tuple<string, int>("1 hour", 60),
			new Tuple<string, int>("1 day", 60 * 24),
			new Tuple<string, int>("Permanent", 0)
		};

		public static void OpenMenu(CCSPlayerController admin, string menuName, CCSPlayerController player, Action<CCSPlayerController, CCSPlayerController, int> onSelectAction)
		{
			BaseMenu menu = AdminMenu.CreateMenu(menuName);

			foreach (Tuple<string, int> duration in _durations)
			{
				string optionName = duration.Item1;
				menu.AddMenuOption(optionName, (_, _) => { onSelectAction?.Invoke(admin, player, duration.Item2); });
			}

			AdminMenu.OpenMenu(admin, menu);
		}
	}
}
