using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2_SimpleAdmin
{
	public class Messages
	{
		[JsonPropertyName("PlayerBanMessageTime")]
		public string PlayerBanMessageTime { get; set; } = "You have been banned for {REASON} for {TIME} minutes by {ADMIN}!";
		[JsonPropertyName("PlayerBanMessagePerm")]
		public string PlayerBanMessagePerm { get; set; } = "You have been banned permanently for {REASON} by {ADMIN}!";
		[JsonPropertyName("PlayerKickMessage")]
		public string PlayerKickMessage { get; set; } = "You have been kicked for {REASON} by {ADMIN}!";
		[JsonPropertyName("AdminBanMessageTime")]
		public string AdminBanMessageTime { get; set; } = "Admin {ADMIN} banned {PLAYER} for {REASON} for {TIME} minutes!";
		[JsonPropertyName("AdminBanMessagePerm")]
		public string AdminBanMessagePerm { get; set; } = "Admin {ADMIN} banned {PLAYER} permanently for {REASON}";

		[JsonPropertyName("AdminKickMessage")]
		public string AdminKickMessage { get; set; } = "Admin {ADMIN} kicked {PLAYER} for {REASON}!";
		[JsonPropertyName("AdminSlayMessage")]
		public string AdminSlayMessage { get; set; } = "Admin {ADMIN} slayed {PLAYER}!";
		[JsonPropertyName("AdminSlapMessage")]
		public string AdminSlapMessage { get; set; } = "Admin {ADMIN} slapped {PLAYER}!";
		[JsonPropertyName("AdminChangeMap")]
		public string AdminChangeMap { get; set; } = "Admin {ADMIN} changed map to {MAP}!";
		[JsonPropertyName("AdminSayPrefix")]
		public string AdminSayPrefix { get; set; } = "{RED}ADMIN: {DEFAULT}!";

	}

	public class CS2_SimpleAdminConfig : BasePluginConfig
	{
		public override int Version { get; set; } = 1;

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

		[JsonPropertyName("Prefix")]
		public string Prefix { get; set; } = "{GREEN}[SimpleAdmin]";

		[JsonPropertyName("Messages")]
		public Messages Messages { get; set; } = new Messages();
	}

}