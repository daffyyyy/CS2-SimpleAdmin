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

		public static bool IsDebugBuild
		{
			get
			{
				#if DEBUG
				        return true;
				#else
						return false;
				#endif
			}
		}
		
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
			return Utilities.GetPlayers().FindAll(p =>
				p.IsValid && p.SteamID.ToString().Length == 17 && !string.IsNullOrEmpty(p.IpAddress) && p is
					{ Connected: PlayerConnectedState.PlayerConnected, IsBot: false, IsHLTV: false });
		}

		public static IEnumerable<CCSPlayerController?> GetValidPlayersWithBots()
		{
			return Utilities.GetPlayers().FindAll(p =>
			p.IsValid && p.SteamID.ToString().Length == 17 && !string.IsNullOrEmpty(p.IpAddress) && p is { Connected: PlayerConnectedState.PlayerConnected, IsBot: false, IsHLTV: false } ||
			p is { IsValid: true, Connected: PlayerConnectedState.PlayerConnected, IsBot: true, IsHLTV: false }
			);
		}

		public static bool IsValidSteamId64(string input)
		{
			const string pattern = @"^\d{17}$";
			return Regex.IsMatch(input, pattern);
		}

		public static bool IsValidIp(string input)
		{
			const string pattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
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

				if (flags == null) return;
				foreach (var flag in flags)
				{
					if (string.IsNullOrEmpty(flag)) continue;
					
					if (flag.StartsWith($"@"))
					{
						//Console.WriteLine($"Adding permission {flag} to SteamID {steamid}");
						AdminManager.AddPlayerPermissions(steamid, flag);
					}
					else if (flag.StartsWith($"#"))
					{
						//Console.WriteLine($"Adding SteamID {steamid} to group {flag}");
						AdminManager.AddPlayerToGroup(steamid, flag);
					}
				}

				AdminManager.SetPlayerImmunity(steamid, immunity);
			}
			catch
			{
			}
		}

		public static void KickPlayer(int userId, string? reason = null)
		{
			if (!string.IsNullOrEmpty(reason))
			{
				var escapeChars = reason.IndexOfAny([';', '|']);

				if (escapeChars != -1)
				{
					reason = reason[..escapeChars];
				}
			}

			Server.ExecuteCommand($"kickid {userId} {reason}");
		}

		public static void PrintToCenterAll(string message)
		{
			Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false }).ToList().ForEach(controller =>
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
			if (CS2_SimpleAdmin._localizer == null)
				return;

			var playerName = caller?.PlayerName ?? "Console";

			var hostname = ConVar.Find("hostname")?.StringValue ?? CS2_SimpleAdmin._localizer["sa_unknown"];

			CS2_SimpleAdmin.Instance.Logger.LogInformation($"{CS2_SimpleAdmin._localizer[
				"sa_discord_log_command",
				playerName, command.GetCommandString]}".Replace("HOSTNAME", hostname).Replace("**", ""));
		}

		internal static void LogCommand(CCSPlayerController? caller, string command)
		{
			if (CS2_SimpleAdmin._localizer == null)
				return;

			var playerName = caller?.PlayerName ?? "Console";

			var hostname = ConVar.Find("hostname")?.StringValue ?? CS2_SimpleAdmin._localizer["sa_unknown"];

			CS2_SimpleAdmin.Instance.Logger.LogInformation($"{CS2_SimpleAdmin._localizer["sa_discord_log_command",
				playerName, command]}".Replace("HOSTNAME", hostname).Replace("**", ""));
		}

		public static IEnumerable<Embed> GenerateEmbedsDiscord(string title, string description, string thumbnailUrl, Color color, string[] fieldNames, string[] fieldValues, bool[] inlineFlags)
		{
			var hostname = ConVar.Find("hostname")?.StringValue ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
			var address = $"{ConVar.Find("ip")?.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";

			description = description.Replace("{hostname}", hostname);
			description = description.Replace("{address}", address);

			var embed = new EmbedBuilder
			{
				Title = title,
				Description = description,
				ThumbnailUrl = thumbnailUrl,
				Color = color,
			};

			for (var i = 0; i < fieldNames.Length; i++)
			{
				fieldValues[i] = fieldValues[i].Replace("{hostname}", hostname ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown");
				fieldValues[i] = fieldValues[i].Replace("{address}", address ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown");

				embed.AddField(fieldNames[i], fieldValues[i], inlineFlags[i]);

				if ((i + 1) % 2 == 0 && i < fieldNames.Length - 1)
				{
					embed.AddField("\u200b", "\u200b");
				}
			}

			return new List<Embed> { embed.Build() };
		}

		public static void SendDiscordLogMessage(CCSPlayerController? caller, CommandInfo command, DiscordWebhookClient? discordWebhookClientLog, IStringLocalizer? localizer)
		{
			if (discordWebhookClientLog == null || localizer == null) return;
			
			var communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl() + ">" : "<https://steamcommunity.com/profiles/0>";
			var callerName = caller != null ? caller.PlayerName : "Console";
			discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
		}

		public enum PenaltyType
		{
			Ban,
			Mute,
			Gag,
			Silence,
		}

		private static string ConvertMinutesToTime(int minutes)
		{
			var time = TimeSpan.FromMinutes(minutes);

			return time.Days > 0 ? $"{time.Days}d {time.Hours}h {time.Minutes}m" : time.Hours > 0 ? $"{time.Hours}h {time.Minutes}m" : $"{time.Minutes}m";
		}

		public static void SendDiscordPenaltyMessage(CCSPlayerController? caller, CCSPlayerController? target, string reason, int duration, PenaltyType penalty, DiscordWebhookClient? discordWebhookClientPenalty, IStringLocalizer? localizer)
		{
			if (discordWebhookClientPenalty == null || localizer == null) return;
			
			var callerCommunityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl() + ">" : "<https://steamcommunity.com/profiles/0>";
			var targetCommunityUrl = target != null ? "<" + new SteamID(target.SteamID).ToCommunityUrl() + ">" : "<https://steamcommunity.com/profiles/0>";
			var callerName = caller != null ? caller.PlayerName : "Console";
			var targetName = target != null ? target.PlayerName : localizer["sa_unknown"];
			var targetSteamId = target != null ? new SteamID(target.SteamID).SteamId2 : localizer["sa_unknown"];

			var time = duration != 0 ? ConvertMinutesToTime(duration) : localizer["sa_permanent"];

			string[] fieldNames = [
				localizer["sa_player"],
				localizer["sa_steamid"],
				localizer["sa_duration"],
				localizer["sa_reason"],
				localizer["sa_admin"]];
			string[] fieldValues = [$"[{targetName}]({targetCommunityUrl})", targetSteamId, time, reason, $"[{callerName}]({callerCommunityUrl})"];
			bool[] inlineFlags = [true, true, true, false, false];

			var hostname = ConVar.Find("hostname")?.StringValue ?? localizer["sa_unknown"];

			var embed = new EmbedBuilder
			{
				Title = penalty switch
				{
					PenaltyType.Ban => localizer["sa_discord_penalty_ban"],
					PenaltyType.Mute => localizer["sa_discord_penalty_mute"],
					PenaltyType.Gag => localizer["sa_discord_penalty_gag"],
					PenaltyType.Silence => localizer["sa_discord_penalty_silence"],
					_ => localizer["sa_discord_penalty_unknown"],
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

			for (var i = 0; i < fieldNames.Length; i++)
			{
				embed.AddField(fieldNames[i], fieldValues[i], inlineFlags[i]);
			}

			discordWebhookClientPenalty.SendMessageAsync(embeds: [embed.Build()]);
		}

		private static string GenerateMessageDiscord(string message)
		{
			var hostname = ConVar.Find("hostname")?.StringValue ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
			var address = $"{ConVar.Find("ip")?.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";

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
			var updatedJsonContent = JsonSerializer.Serialize(config,
				new JsonSerializerOptions
				{
					WriteIndented = true,
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
				});
			File.WriteAllText(CfgPath, updatedJsonContent);
		}

		public static void TryLogCommandOnDiscord(CCSPlayerController? caller, string commandString)
		{
			if (CS2_SimpleAdmin._discordWebhookClientLog == null || CS2_SimpleAdmin._localizer == null)
				return;

			if (caller != null && caller.IsValid == false)
				caller = null;

			var callerName = caller == null ? "Console" : caller.PlayerName;
			var communityUrl = caller != null
				? "<" + new SteamID(caller.SteamID).ToCommunityUrl() + ">"
				: "<https://steamcommunity.com/profiles/0>";
			CS2_SimpleAdmin._discordWebhookClientLog.SendMessageAsync(GenerateMessageDiscord(
				CS2_SimpleAdmin._localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})",
					commandString]));
		}
	}

	public class SchemaString<TSchemaClass>(TSchemaClass instance, string member)
		: NativeObject(Schema.GetSchemaValue<nint>(instance.Handle, typeof(TSchemaClass).Name, member))
		where TSchemaClass : NativeObject
	{
		public unsafe void Set(string str)
		{
			var bytes = GetStringBytes(str);

			for (var i = 0; i < bytes.Length; i++)
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