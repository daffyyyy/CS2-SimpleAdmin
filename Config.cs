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
		[JsonPropertyName("PlayerGagMessageTime")]
		public string PlayerGagMessageTime { get; set; } = "You have been gagged for {REASON} for {TIME} minutes by {ADMIN}!";
		[JsonPropertyName("PlayerGagMessagePerm")]
		public string PlayerGagMessagePerm { get; set; } = "You have been gagged permanently for {REASON} by {ADMIN}!";
		[JsonPropertyName("AdminBanMessageTime")]
		public string AdminBanMessageTime { get; set; } = "Admin {ADMIN} banned {PLAYER} for {REASON} for {TIME} minutes!";
		[JsonPropertyName("AdminBanMessagePerm")]
		public string AdminBanMessagePerm { get; set; } = "Admin {ADMIN} banned {PLAYER} permanently for {REASON}";
		[JsonPropertyName("AdminKickMessage")]
		public string AdminKickMessage { get; set; } = "Admin {ADMIN} kicked {PLAYER} for {REASON}!";
		[JsonPropertyName("AdminGagMessageTime")]
		public string AdminGagMessageTime { get; set; } = "Admin {ADMIN} gagged {PLAYER} for {REASON} for {TIME} minutes!";
		[JsonPropertyName("AdminGagMessagePerm")]
		public string AdminGagMessagePerm { get; set; } = "Admin {ADMIN} gagged {PLAYER} permanently for {REASON}";
		[JsonPropertyName("AdminSlayMessage")]
		public string AdminSlayMessage { get; set; } = "Admin {ADMIN} slayed {PLAYER}!";
		[JsonPropertyName("AdminSlapMessage")]
		public string AdminSlapMessage { get; set; } = "Admin {ADMIN} slapped {PLAYER}!";
		[JsonPropertyName("AdminChangeMap")]
		public string AdminChangeMap { get; set; } = "Admin {ADMIN} changed map to {MAP}!";
		[JsonPropertyName("AdminNoclipMessage")]
		public string AdminNoclipMessage { get; set; } = "Admin {ADMIN} toggled noclip for {PLAYER}!";
		[JsonPropertyName("AdminFreezeMessage")]
		public string AdminFreezeMessage { get; set; } = "Admin {ADMIN} freezed {PLAYER}!";
		[JsonPropertyName("AdminUnFreezeMessage")]
		public string AdminUnFreezeMessage { get; set; } = "Admin {ADMIN} unfreezed {PLAYER}!";
		[JsonPropertyName("AdminRespawnMessage")]
		public string AdminRespawnMessage { get; set; } = "Admin {ADMIN} respawned {PLAYER}!";
		[JsonPropertyName("AdminSayPrefix")]
		public string AdminSayPrefix { get; set; } = "{RED}ADMIN: {DEFAULT}";
		[JsonPropertyName("AdminHelpCommand")]
		public string AdminHelpCommand { get; set; } = "{GREEN}[ CS2-SimpleAdmin HELP ]{DEFAULT}\n- css_ban <#userid or name> [time in minutes/0 perm] [reason] - Ban player\n- css_addban <steamid> [time in minutes/0 perm] [reason] - Ban player via steamid64\n" +
			"- css_banip <ip> [time in minutes/0 perm] [reason] - Ban player via IP address\n- css_unban <steamid or name or ip> - Unban player\n" +
			"- css_kick <#userid or name> [reason] - Kick player\n- css_gag <#userid or name> [time in minutes/0 perm] [reason] - Gag player\n- css_addgag <steamid> [time in minutes/0 perm] [reason] - Gag player via steamid64\n" +
			"- css_unmute <steamid or name> <type [gag/mute] - Unmute player\n" +
			"- css_slay <#userid or name> - Kill player\n- css_slap <#userid or name> [damage] - Slap player\n- css_map <mapname> - Change map\n- css_say <message> - Say message as admin in chat\n" +
			"- css_psay <#userid or name> <message> - Sends private message to player\n- css_csay <message> - Say message as admin in center\n- css_hsay <message> - Say message as admin in hud\n" +
			"- css_noclip <#userid or name> - Toggle noclip for player\n- css_freeze <#userid or name> [duration] - Freeze player\n- css_unfreeze <#userid or name> - Unfreeze player\n" +
			"- css_respawn <#userid or name> - Respawn player\n- css_cvar <cvar> <value> - Change cvar value\n- css_rcon <command> - Run command as server";
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
		[JsonPropertyName("KickTime")]
		public int KickTime { get; set; } = 10;

		[JsonPropertyName("Messages")]
		public Messages Messages { get; set; } = new Messages();
	}

}