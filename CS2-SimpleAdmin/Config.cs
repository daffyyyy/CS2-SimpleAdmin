using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2_SimpleAdmin;

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

public class DiscordPenaltySetting
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; } = "";
}

public class Discord
{
    [JsonPropertyName("DiscordLogWebhook")]
    public string DiscordLogWebhook { get; set; } = "";

    [JsonPropertyName("DiscordPenaltyBanSettings")]
    public DiscordPenaltySetting[] DiscordPenaltyBanSettings { get; set; } =
    [
        new DiscordPenaltySetting { Name = "Color", Value = "" },
        new DiscordPenaltySetting { Name = "Webhook", Value = "" },
        new DiscordPenaltySetting { Name = "ThumbnailUrl", Value = "" },
        new DiscordPenaltySetting { Name = "ImageUrl", Value = "" },
        new DiscordPenaltySetting { Name = "Footer", Value = "" },
        new DiscordPenaltySetting { Name = "Time", Value = "{relative}" },
    ];

    [JsonPropertyName("DiscordPenaltyMuteSettings")]
    public DiscordPenaltySetting[] DiscordPenaltyMuteSettings { get; set; } =
    [
        new DiscordPenaltySetting { Name = "Color", Value = "" },
        new DiscordPenaltySetting { Name = "Webhook", Value = "" },
        new DiscordPenaltySetting { Name = "ThumbnailUrl", Value = "" },
        new DiscordPenaltySetting { Name = "ImageUrl", Value = "" },
        new DiscordPenaltySetting { Name = "Footer", Value = "" },
        new DiscordPenaltySetting { Name = "Time", Value = "{relative}" },
    ];

    [JsonPropertyName("DiscordPenaltyGagSettings")]
    public DiscordPenaltySetting[] DiscordPenaltyGagSettings { get; set; } =
    [
        new DiscordPenaltySetting { Name = "Color", Value = "" },
        new DiscordPenaltySetting { Name = "Webhook", Value = "" },
        new DiscordPenaltySetting { Name = "ThumbnailUrl", Value = "" },
        new DiscordPenaltySetting { Name = "ImageUrl", Value = "" },
        new DiscordPenaltySetting { Name = "Footer", Value = "" },
        new DiscordPenaltySetting { Name = "Time", Value = "{relative}" },
    ];

    [JsonPropertyName("DiscordPenaltySilenceSettings")]
    public DiscordPenaltySetting[] DiscordPenaltySilenceSettings { get; set; } =
    [
        new DiscordPenaltySetting { Name = "Color", Value = "" },
        new DiscordPenaltySetting { Name = "Webhook", Value = "" },
        new DiscordPenaltySetting { Name = "ThumbnailUrl", Value = "" },
        new DiscordPenaltySetting { Name = "ImageUrl", Value = "" },
        new DiscordPenaltySetting { Name = "Footer", Value = "" },
        new DiscordPenaltySetting { Name = "Time", Value = "{relative}" },
    ];

    [JsonPropertyName("DiscordPenaltyWarnSettings")]
    public DiscordPenaltySetting[] DiscordPenaltyWarnSettings { get; set; } =
    [
        new DiscordPenaltySetting { Name = "Color", Value = "" },
        new DiscordPenaltySetting { Name = "Webhook", Value = "" },
        new DiscordPenaltySetting { Name = "ThumbnailUrl", Value = "" },
        new DiscordPenaltySetting { Name = "ImageUrl", Value = "" },
        new DiscordPenaltySetting { Name = "Footer", Value = "" },
        new DiscordPenaltySetting { Name = "Time", Value = "{relative}" },
    ];
    
    [JsonPropertyName("DiscordAssociatedAccountsSettings")]
    public DiscordPenaltySetting[] DiscordAssociatedAccountsSettings { get; set; } =
    [
        new DiscordPenaltySetting { Name = "Color", Value = "" },
        new DiscordPenaltySetting { Name = "Webhook", Value = "" },
        new DiscordPenaltySetting { Name = "ThumbnailUrl", Value = "" },
        new DiscordPenaltySetting { Name = "ImageUrl", Value = "" },
        new DiscordPenaltySetting { Name = "Footer", Value = "" },
        new DiscordPenaltySetting { Name = "Time", Value = "{relative}" },
    ];
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
    [JsonPropertyName("MenuType")] public string MenuType { get; set; } = "selectable";

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

    [JsonPropertyName("WarnReasons")]
    public List<string> WarnReasons { get; set; } =
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
        new AdminFlag { Name = "Show IP", Flag = "@css/showip" },
        new AdminFlag { Name = "Cvar", Flag = "@css/cvar" },
        new AdminFlag { Name = "Rcon", Flag = "@css/rcon" },
        new AdminFlag { Name = "Root (all flags)", Flag = "@css/root" }
    ];
}

public class OtherSettings
{
    [JsonPropertyName("ShowActivityType")]
    public int ShowActivityType { get; set; } = 2;

    [JsonPropertyName("TeamSwitchType")]
    public int TeamSwitchType { get; set; } = 1;

    [JsonPropertyName("KickTime")]
    public int KickTime { get; set; } = 5;

    [JsonPropertyName("BanType")]
    public int BanType { get; set; } = 1;

    [JsonPropertyName("TimeMode")]
    public int TimeMode { get; set; } = 1;

    [JsonPropertyName("DisableDangerousCommands")]
    public bool DisableDangerousCommands { get; set; } = true;

    [JsonPropertyName("MaxBanDuration")]
    public int MaxBanDuration { get; set; } = 60 * 24 * 7;
    
    [JsonPropertyName("MaxMuteDuration")]
    public int MaxMuteDuration { get; set; } = 60 * 24 * 7;

    [JsonPropertyName("ExpireOldIpBans")]
    public int ExpireOldIpBans { get; set; } = 0;

    [JsonPropertyName("ReloadAdminsEveryMapChange")]
    public bool ReloadAdminsEveryMapChange { get; set; } = false;

    [JsonPropertyName("DisconnectedPlayersHistoryCount")]
    public int DisconnectedPlayersHistoryCount { get; set; } = 10;
    
    [JsonPropertyName("NotifyPenaltiesToAdminOnConnect")]
    public bool NotifyPenaltiesToAdminOnConnect { get; set; } = true;
    
    [JsonPropertyName("ShowBanMenuIfNoTime")]
    public bool ShowBanMenuIfNoTime { get; set; } = true;
    
    [JsonPropertyName("UserMessageGagChatType")]
    public bool UserMessageGagChatType { get; set; } = false;
    
    [JsonPropertyName("CheckMultiAccountsByIp")]
    public bool CheckMultiAccountsByIp { get; set; } = true;

    [JsonPropertyName("AdditionalCommandsToLog")]
    public List<string> AdditionalCommandsToLog { get; set; } = new();
    [JsonPropertyName("IgnoredIps")]
    public List<string> IgnoredIps { get; set; } = new();
}

public class CS2_SimpleAdminConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 24;

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
    
    [JsonPropertyName("DatabaseSSlMode")]
    public string DatabaseSSlMode { get; set; } = "preferred";

    [JsonPropertyName("OtherSettings")]
    public OtherSettings OtherSettings { get; set; } = new();

    [JsonPropertyName("EnableMetrics")]
    public bool EnableMetrics { get; set; } = true;

    [JsonPropertyName("EnableUpdateCheck")]
    public bool EnableUpdateCheck { get; set; } = true;

    [JsonPropertyName("Timezone")]
    public string Timezone { get; set; } = "UTC";

    [JsonPropertyName("WarnThreshold")]
    public Dictionary<int, string> WarnThreshold { get; set; } = new()
    {
        { 998, "css_addban STEAMID64 60 \"3/4 Warn\"" },
        { 999, "css_ban #USERID 120 \"4/4 Warn\"" },
    };

    [JsonPropertyName("MultiServerMode")]
    public bool MultiServerMode { get; set; } = true;

    [JsonPropertyName("Discord")]
    public Discord Discord { get; set; } = new();

    [JsonPropertyName("DefaultMaps")]
    public List<string> DefaultMaps { get; set; } = new();

    [JsonPropertyName("WorkshopMaps")]
    public Dictionary<string, long?> WorkshopMaps { get; set; } = new();

    [JsonPropertyName("CustomServerCommands")]
    public List<CustomServerCommandData> CustomServerCommands { get; set; } = new();

    [JsonPropertyName("MenuConfig")]
    public MenuConfig MenuConfigs { get; set; } = new();
}