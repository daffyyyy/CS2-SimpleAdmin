using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using Discord;
using Discord.Webhook;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CS2_SimpleAdmin
{
	internal class Helper
	{
		private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
		private static readonly string CfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{AssemblyName}/{AssemblyName}.json";

		internal static CS2_SimpleAdminConfig? Config { get; set; }

		public static List<CCSPlayerController> GetPlayerFromName(string name)
		{
			return Utilities.GetPlayers().FindAll(x => x.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
		}

		public static List<CCSPlayerController> GetPlayerFromSteamid64(string steamid)
		{
			return GetValidPlayers().FindAll(x =>
				x.SteamID.ToString().Equals(steamid, StringComparison.OrdinalIgnoreCase)
			);
		}

		public static List<CCSPlayerController> GetPlayerFromIp(string ipAddress)
		{
			return GetValidPlayers().FindAll(x =>
				x.IpAddress != null &&
				x.IpAddress.Split(":")[0].Equals(ipAddress)
			);
		}

		public static List<CCSPlayerController> GetValidPlayers()
		{
			return Utilities.GetPlayers().FindAll(p => p != null && p.IsValid && p.SteamID.ToString().Length == 17 && !string.IsNullOrEmpty(p.IpAddress) && p.Connected == PlayerConnectedState.PlayerConnected && !p.IsBot && !p.IsHLTV);
		}

		public static List<CCSPlayerController> GetValidPlayersWithBots()
		{
			return Utilities.GetPlayers().FindAll(p =>
			p != null && p.IsValid && p.SteamID.ToString().Length == 17 && !string.IsNullOrEmpty(p.IpAddress) && p.Connected == PlayerConnectedState.PlayerConnected && !p.IsBot && !p.IsHLTV ||
			p != null && p.IsValid && p.Connected == PlayerConnectedState.PlayerConnected && p.IsBot && !p.IsHLTV
			);
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
					return;
				}

				if (flags != null)
				{
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
			}
		}

		public static void KickPlayer(int userId, string? reason = null)
		{
			if (!string.IsNullOrEmpty(reason))
			{
				int escapeChars = reason.IndexOfAny(new char[] { ';', '|' });

				if (escapeChars != -1)
				{
					reason = reason[..escapeChars];
				}
			}

			Server.ExecuteCommand($"kickid {userId} {reason}");
		}

		public static void PrintToCenterAll(string message)
		{
			Utilities.GetPlayers().Where(p => p is not null && p.IsValid && !p.IsBot && !p.IsHLTV).ToList().ForEach(controller =>
			{
				controller.PrintToCenter(message);
			});
		}

		internal static void HandleVotes(CCSPlayerController player, ChatMenuOption option)
		{
			if (!CS2_SimpleAdmin.voteInProgress)
				return;

			option.Disabled = true;
			CS2_SimpleAdmin.voteAnswers[option.Text]++;
		}

		internal static void LogCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (CS2_SimpleAdmin.Instance == null || CS2_SimpleAdmin._localizer == null)
				return;

			string playerName = caller?.PlayerName ?? "Console";

			string? hostname = ConVar.Find("hostname")!.StringValue ?? CS2_SimpleAdmin._localizer["sa_unknown"];

			CS2_SimpleAdmin.Instance.Logger.LogInformation($"{CS2_SimpleAdmin._localizer["sa_discord_log_command",
				playerName, command.GetCommandString]}".Replace("HOSTNAME", hostname).Replace("**", ""));
		}

		internal static void LogCommand(CCSPlayerController? caller, string command)
		{
			if (CS2_SimpleAdmin.Instance == null || CS2_SimpleAdmin._localizer == null)
				return;

			string playerName = caller?.PlayerName ?? "Console";

			string? hostname = ConVar.Find("hostname")!.StringValue ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";

			CS2_SimpleAdmin.Instance.Logger.LogInformation($"{CS2_SimpleAdmin._localizer?["sa_discord_log_command",
				playerName, command]}".Replace("HOSTNAME", hostname).Replace("**", ""));
		}

		public static IEnumerable<Embed> GenerateEmbedsDiscord(string title, string description, string thumbnailUrl, Color color, string[] fieldNames, string[] fieldValues, bool[] inlineFlags)
		{
			string? hostname = ConVar.Find("hostname")!.StringValue ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
			string? address = $"{ConVar.Find("ip")!.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";

			description = description.Replace("{hostname}", hostname ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown");
			description = description.Replace("{address}", address ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown");

			var embed = new EmbedBuilder
			{
				Title = title,
				Description = description,
				ThumbnailUrl = thumbnailUrl,
				Color = color,
			};

			for (int i = 0; i < fieldNames.Length; i++)
			{
				fieldValues[i] = fieldValues[i].Replace("{hostname}", hostname ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown");
				fieldValues[i] = fieldValues[i].Replace("{address}", address ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown");

				embed.AddField(fieldNames[i], fieldValues[i], inlineFlags[i]);

				if ((i + 1) % 2 == 0 && i < fieldNames.Length - 1)
				{
					embed.AddField("\u200b", "\u200b", false);
				}
			}

			return new List<Embed> { embed.Build() };
		}

		public static void SendDiscordLogMessage(CCSPlayerController? caller, CommandInfo command, DiscordWebhookClient? discordWebhookClientLog, IStringLocalizer? localizer)
		{
			if (discordWebhookClientLog != null && localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				string callerName = caller != null ? caller.PlayerName : "Console";
				discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}
		}

		public enum PenaltyType
		{
			Ban,
			Mute,
			Gag,
			Silence,
		}

		public static string ConvertMinutesToTime(int minutes)
		{
			TimeSpan time = TimeSpan.FromMinutes(minutes);

			return time.Days > 0 ? $"{time.Days}d {time.Hours}h {time.Minutes}m" : time.Hours > 0 ? $"{time.Hours}h {time.Minutes}m" : $"{time.Minutes}m";
		}

		public static void SendDiscordPenaltyMessage(CCSPlayerController? caller, CCSPlayerController? target, string reason, int duration, PenaltyType penalty, DiscordWebhookClient? discordWebhookClientPenalty, IStringLocalizer? localizer)
		{
			if (discordWebhookClientPenalty != null && localizer != null)
			{
				string callercommunityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				string targetcommunityUrl = target != null ? "<" + new SteamID(target.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				string callerName = caller != null ? caller.PlayerName : "Console";
				string targetName = target != null ? target.PlayerName : localizer?["sa_unknown"] ?? "Unknown";
				string targetSteamId = target != null ? new SteamID(target.SteamID).SteamId2.ToString() : localizer?["sa_unknown"] ?? "Unknown";

				string time = duration != 0 ? ConvertMinutesToTime(duration) : localizer?["sa_permanent"] ?? "Permanent";

				string[] fieldNames = [
					localizer?["sa_player"] ?? "Player:",
					localizer?["sa_steamid"] ?? "SteamID:",
					localizer?["sa_duration"] ?? "Duration:",
					localizer?["sa_reason"] ?? "Reason:",
					localizer?["sa_admin"] ?? "Admin:"];
				string[] fieldValues = [$"[{targetName}]({targetcommunityUrl})", targetSteamId, time, reason, $"[{callerName}]({callercommunityUrl})"];
				bool[] inlineFlags = [true, true, true, false, false];

				string? hostname = ConVar.Find("hostname")!.StringValue ?? localizer?["sa_unknown"] ?? "Unknown";

				var embed = new EmbedBuilder
				{
					Title = penalty switch
					{
						PenaltyType.Ban => localizer?["sa_discord_penalty_ban"] ?? "Ban registrered",
						PenaltyType.Mute => localizer?["sa_discord_penalty_mute"] ?? "Mute registrered",
						PenaltyType.Gag => localizer?["sa_discord_penalty_gag"] ?? "Gag registrered",
						PenaltyType.Silence => localizer?["sa_discord_penalty_silence"] ?? "Silence registrered",
						_ => localizer?["sa_discord_penalty_unknown"] ?? "Unknown registrered",
					},

					Color = penalty switch
					{
						PenaltyType.Ban => Color.Red,
						PenaltyType.Mute => Color.Blue,
						PenaltyType.Gag => Color.Gold,
						PenaltyType.Silence => Color.Green,
						_ => Color.Default,
					},

					Description = $"{hostname}",

					Timestamp = DateTimeOffset.UtcNow
				};

				for (int i = 0; i < fieldNames.Length; i++)
				{
					embed.AddField(fieldNames[i], fieldValues[i], inlineFlags[i]);
				}

				discordWebhookClientPenalty.SendMessageAsync(embeds: [embed.Build()]);
			}
		}

		public static string GenerateMessageDiscord(string message)
		{
			string? hostname = ConVar.Find("hostname")!.StringValue ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
			string? address = $"{ConVar.Find("ip")!.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";

			message = message.Replace("HOSTNAME", hostname);
			message = message.Replace("ADDRESS", address);

			return message;
		}

		public static void UpdateConfig<T>(T config) where T : BasePluginConfig, new()
		{
			// get newest config version
			var newCfgVersion = new T().Version;

			// loaded config is up to date
			if (config.Version == newCfgVersion)
				return;

			// update the version
			config.Version = newCfgVersion;

			// serialize the updated config back to json
			var updatedJsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
			File.WriteAllText(CfgPath, updatedJsonContent);
		}
	}

	public class SchemaString<SchemaClass> : NativeObject where SchemaClass : NativeObject
	{
		public SchemaString(SchemaClass instance, string member) : base(Schema.GetSchemaValue<nint>(instance.Handle, typeof(SchemaClass).Name!, member))
		{ }

		public unsafe void Set(string str)
		{
			byte[] bytes = SchemaString<SchemaClass>.GetStringBytes(str);

			for (int i = 0; i < bytes.Length; i++)
			{
				Unsafe.Write((void*)(Handle.ToInt64() + i), bytes[i]);
			}

			Unsafe.Write((void*)(Handle.ToInt64() + bytes.Length), 0);
		}

		private static byte[] GetStringBytes(string str)
		{
			return Encoding.UTF8.GetBytes(str);
		}
	}
}