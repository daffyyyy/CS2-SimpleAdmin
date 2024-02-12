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

	public class CS2_SimpleAdminConfig : BasePluginConfig
	{
		public override int Version { get; set; } = 6;

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

		[JsonPropertyName("KickTime")]
		public int KickTime { get; set; } = 5;

		[JsonPropertyName("DisableDangerousCommands")]
		public bool DisableDangerousCommands { get; set; } = true;

		[JsonPropertyName("BanType")]
		public int BanType { get; set; } = 1;

		[JsonPropertyName("TeamSwitchType")]
		public int TeamSwitchType { get; set; } = 1;

		[JsonPropertyName("Discord")]
		public Discord Discord { get; set; } = new Discord();
		
		[JsonPropertyName("DefaultMaps")]
		public List<string> DefaultMaps { get; set; } = new List<string>();
		
		[JsonPropertyName("WorkshopMaps")]
		public List<string> WorkshopMaps { get; set; } = new List<string>();
	}
}