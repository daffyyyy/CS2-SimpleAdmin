using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using MySqlConnector;
using CounterStrikeSharp.API.Modules.Utils;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CS2_SimpleAdmin
{
	internal class Helper
	{
		internal static CS2_SimpleAdminConfig? Config { get; set; }

		public static List<CCSPlayerController> GetPlayerFromName(string name)
		{
			return Utilities.GetPlayers().FindAll(x => x.PlayerName.Contains(name, StringComparison.OrdinalIgnoreCase));
		}

		public static List<CCSPlayerController> GetPlayerFromSteamid64(string steamid)
		{
			return Utilities.GetPlayers().FindAll(x =>
				x.AuthorizedSteamID != null &&
				x.AuthorizedSteamID.SteamId64.ToString().Equals(steamid, StringComparison.OrdinalIgnoreCase)
			);
		}

		public static bool IsValidSteamID64(string input)
		{
			string pattern = @"^\d{17}$";

			return Regex.IsMatch(input, pattern);
		}

		public static TargetResult GetTarget(string target, out CCSPlayerController? player)
		{
			player = null;

			if (target.StartsWith("#") && int.TryParse(target.AsSpan(1), out var userid))
			{
				player = Utilities.GetPlayerFromUserid(userid);
			}
			else
			{
				var matches = GetPlayerFromName(target);
				if (matches.Count > 1)
					return TargetResult.Multiple;

				player = matches.FirstOrDefault();
			}

			return player?.IsValid == true ? TargetResult.Single : TargetResult.None;
		}

		public static void KickPlayer(int? userId, string? reason = null)
		{
			NativeAPI.IssueServerCommand($"kickid {userId} {reason}");
		}

		public static void PrintToCenterAll(string message)
		{
			Utilities.GetPlayers().ForEach(controller =>
			{
				controller.PrintToCenter(message);
			});
		}

		internal static string ReplaceTags(string message)
		{
			if (message.Contains('{'))
			{
				string modifiedValue = message;
				foreach (FieldInfo field in typeof(ChatColors).GetFields())
				{
					string pattern = $"{{{field.Name}}}";
					if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
					{
						modifiedValue = modifiedValue.Replace(pattern, field.GetValue(null)!.ToString(), StringComparison.OrdinalIgnoreCase);
					}
				}
				return modifiedValue;
			}

			return message;
		}

	}
}
