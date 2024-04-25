using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2_SimpleAdmin
{
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

	public class CS2_SimpleAdminConfig : BasePluginConfig
	{
		[JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 10;

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

		[JsonPropertyName("UseChatMenu")]
		public bool UseChatMenu { get; set; } = false;

		[JsonPropertyName("KickTime")]
		public int KickTime { get; set; } = 5;

		[JsonPropertyName("DisableDangerousCommands")]
		public bool DisableDangerousCommands { get; set; } = true;

		[JsonPropertyName("BanType")]
		public int BanType { get; set; } = 1;
		[JsonPropertyName("MultiServerMode")]
		public bool MultiServerMode { get; set; } = true;

		[JsonPropertyName("ExpireOldIpBans")]
		public int ExpireOldIpBans { get; set; } = 0;

		[JsonPropertyName("TeamSwitchType")]
		public int TeamSwitchType { get; set; } = 1;

		[JsonPropertyName("Discord")]
		public Discord Discord { get; set; } = new Discord();

		[JsonPropertyName("DefaultMaps")]
		public List<string> DefaultMaps { get; set; } = new List<string>();

		[JsonPropertyName("WorkshopMaps")]
		public List<string> WorkshopMaps { get; set; } = new List<string>();

		[JsonPropertyName("CustomServerCommands")]
		public List<CustomServerCommandData> CustomServerCommands { get; set; } = new List<CustomServerCommandData>();
	}
}