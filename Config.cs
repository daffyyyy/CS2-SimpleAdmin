using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2_SimpleAdmin
{
	public class DurationItem
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("duration")]
		public int Duration { get; set; }
	}

	public class AdminFlag
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("flag")]
		public required string Flag { get; set; }
	}

	public class Discord
	{
		[JsonPropertyName("DiscordLogWebhook")]
		public string DiscordLogWebhook { get; set; } = "";

		[JsonPropertyName("DiscordPenaltyWebhook")]
		public string DiscordPenaltyWebhook { get; set; } = "";
	}

	public class CustomServerCommandData
	{
		[JsonPropertyName("Flag")]
		public string Flag { get; set; } = "@css/generic";

		[JsonPropertyName("DisplayName")]
		public string DisplayName { get; set; } = "";

		[JsonPropertyName("Command")]
		public string Command { get; set; } = "";

		[JsonPropertyName("ExecuteOnClient")]
		public bool ExecuteOnClient { get; set; } = false;
	}

	public class MenuConfig
	{
		[JsonPropertyName("Durations")]
		public DurationItem[] Durations { get; set; } =
		[
			new DurationItem { Name = "1 minute", Duration = 1 },
			new DurationItem { Name = "5 minutes", Duration = 5 },
			new DurationItem { Name = "15 minutes", Duration = 15 },
			new DurationItem { Name = "1 hour", Duration = 60 },
			new DurationItem { Name = "1 day", Duration = 60 * 24 },
			new DurationItem { Name = "7 days", Duration = 60 * 24 * 7 },
			new DurationItem { Name = "14 days", Duration = 60 * 24 * 14 },
			new DurationItem { Name = "30 days", Duration = 60 * 24 * 30 },
			new DurationItem { Name = "Permanent", Duration = 0 }
		];

		[JsonPropertyName("BanReasons")]
		public List<string> BanReasons { get; set; } =
		[
			"Hacking",
			"Voice Abuse",
			"Chat Abuse",
			"Admin disrespect",
			"Other"
		];

		[JsonPropertyName("KickReasons")]
		public List<string> KickReasons { get; set; } =
		[
			"Voice Abuse",
			"Chat Abuse",
			"Admin disrespect",
			"Other"
		];

		[JsonPropertyName("MuteReasons")]
		public List<string> MuteReasons { get; set; } =
		[
			"Advertising",
			"Spamming",
			"Spectator camera abuse",
			"Hate",
			"Admin disrespect",
			"Other"
		];

		[JsonPropertyName("AdminFlags")]
		public AdminFlag[] AdminFlags { get; set; } =
		[
			new AdminFlag { Name = "Generic", Flag = "@css/generic" },
			new AdminFlag { Name = "Chat", Flag = "@css/chat" },
			new AdminFlag { Name = "Change Map", Flag = "@css/changemap" },
			new AdminFlag { Name = "Slay", Flag = "@css/slay" },
			new AdminFlag { Name = "Kick", Flag = "@css/kick" },
			new AdminFlag { Name = "Ban", Flag = "@css/ban" },
			new AdminFlag { Name = "Perm Ban", Flag = "@css/permban" },
			new AdminFlag { Name = "Unban", Flag = "@css/unban" },
			new AdminFlag { Name = "Cvar", Flag = "@css/cvar" },
			new AdminFlag { Name = "Rcon", Flag = "@css/rcon" },
			new AdminFlag { Name = "Root (all flags)", Flag = "@css/root" }
		];
	}

	public class CS2_SimpleAdminConfig : BasePluginConfig
	{
		[JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 14;

		[JsonPropertyName("DatabaseHost")]
		public string DatabaseHost { get; set; } = "";

		[JsonPropertyName("DatabasePort")]
		public int DatabasePort { get; set; } = 3306;

		[JsonPropertyName("DatabaseUser")]
		public string DatabaseUser { get; set; } = "";

		[JsonPropertyName("DatabasePassword")]
		public string DatabasePassword { get; set; } = "";

		[JsonPropertyName("DatabaseName")]
		public string DatabaseName { get; set; } = "";

		[JsonPropertyName("EnableMetrics")]
		public bool EnableMetrics { get; set; } = true;
        
		[JsonPropertyName("ReloadAdminsEveryMapChange")]
		public bool ReloadAdminsEveryMapChange { get; set; } = false;

		[JsonPropertyName("UseChatMenu")]
		public bool UseChatMenu { get; set; } = false;

		[JsonPropertyName("KickTime")]
		public int KickTime { get; set; } = 5;

		[JsonPropertyName("DisableDangerousCommands")]
		public bool DisableDangerousCommands { get; set; } = true;
		
		[JsonPropertyName("BanType")]
		public int BanType { get; set; } = 1;
		
		[JsonPropertyName("TimeMode")]
		public int TimeMode { get; set; } = 1;
		
		[JsonPropertyName("MaxBanDuration")]
		public int MaxBanDuration { get; set; } = 60 * 24 * 7; // 7 days
		[JsonPropertyName("MultiServerMode")]
		public bool MultiServerMode { get; set; } = true;

		[JsonPropertyName("ExpireOldIpBans")]
		public int ExpireOldIpBans { get; set; } = 0;

		[JsonPropertyName("TeamSwitchType")]
		public int TeamSwitchType { get; set; } = 1;

		[JsonPropertyName("Discord")]
		public Discord Discord { get; set; } = new();

		[JsonPropertyName("DefaultMaps")]
		public List<string> DefaultMaps { get; set; } = new();

		[JsonPropertyName("WorkshopMaps")]
		public List<string> WorkshopMaps { get; set; } = new();

		[JsonPropertyName("CustomServerCommands")]
		public List<CustomServerCommandData> CustomServerCommands { get; set; } = new();

		[JsonPropertyName("MenuConfig")]
		public MenuConfig MenuConfigs { get; set; } = new();
	}
}