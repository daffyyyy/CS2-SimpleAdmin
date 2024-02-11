using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
				x.SteamID.ToString().Equals(steamid, StringComparison.OrdinalIgnoreCase)
			);
		}

		public static List<CCSPlayerController> GetPlayerFromIp(string ipAddress)
		{
			return Utilities.GetPlayers().FindAll(x =>
				x.IpAddress != null &&
				x.IpAddress.Split(":")[0].Equals(ipAddress)
			);
		}

		public static List<CCSPlayerController> GetValidPlayers()
		{
			return Utilities.GetPlayers().FindAll(p => p != null && p.IsValid && p.SteamID.ToString().Length == 17 && p.Connected == PlayerConnectedState.PlayerConnected && !p.IsBot && !p.IsHLTV);
		}


		public static bool IsValidSteamID64(string input)
		{
			string pattern = @"^\d{17}$";

			return Regex.IsMatch(input, pattern);
		}

		public static bool IsValidIP(string input)
		{
			string pattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

			return Regex.IsMatch(input, pattern);
		}

		public static void GivePlayerFlags(SteamID? steamid, List<string>? flags = null, uint immunity = 0)
		{
			try
			{
				if (steamid == null || (flags == null && immunity == 0))
				{
					//Console.WriteLine("Invalid input: steamid is null or both flags and immunity are not provided.");
					return;
				}

				//Console.WriteLine($"Setting immunity for SteamID {steamid} to {immunity}");

				if (flags != null)
				{
					//Console.WriteLine($"Applying flags to SteamID {steamid}:");

					foreach (var flag in flags)
					{
						if (!string.IsNullOrEmpty(flag))
						{
							if (flag.StartsWith("@"))
							{
								//Console.WriteLine($"Adding permission {flag} to SteamID {steamid}");
								AdminManager.AddPlayerPermissions(steamid, flag);
							}
							else if (flag.StartsWith("#"))
							{
								//Console.WriteLine($"Adding SteamID {steamid} to group {flag}");
								AdminManager.AddPlayerToGroup(steamid, flag);
							}
						}
					}
					AdminManager.SetPlayerImmunity(steamid, (uint)immunity);
				}
			}
			catch (Exception)
			{
				return;
				//Console.WriteLine($"An error occurred: {ex}");
			}
		}


		/*
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
		*/

		public static void KickPlayer(ushort userId, string? reason = null)
		{
			Server.ExecuteCommand($"kickid {userId} {reason}");
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

		internal static void handleVotes(CCSPlayerController player, ChatMenuOption option)
		{
			if (CS2_SimpleAdmin.voteInProgress && !CS2_SimpleAdmin.votePlayers.Contains(player.Slot))
			{
				option.Disabled = true;
				CS2_SimpleAdmin.votePlayers.Add(player.Slot);
				CS2_SimpleAdmin.voteAnswers[option.Text]++;
			}
		}
	}

	public class SchemaString<SchemaClass> : NativeObject where SchemaClass : NativeObject
	{
		public SchemaString(SchemaClass instance, string member) : base(Schema.GetSchemaValue<nint>(instance.Handle, typeof(SchemaClass).Name!, member))
		{ }

		public unsafe void Set(string str)
		{
			byte[] bytes = this.GetStringBytes(str);

			for (int i = 0; i < bytes.Length; i++)
			{
				Unsafe.Write((void*)(this.Handle.ToInt64() + i), bytes[i]);
			}

			Unsafe.Write((void*)(this.Handle.ToInt64() + bytes.Length), 0);
		}

		private byte[] GetStringBytes(string str)
		{
			return Encoding.ASCII.GetBytes(str);
		}
	}
}