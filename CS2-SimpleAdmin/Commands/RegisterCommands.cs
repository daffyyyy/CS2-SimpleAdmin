using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin;

public static class RegisterCommands
{
    internal static readonly Dictionary<string, IList<CommandDefinition>> _commandDefinitions =
        new(StringComparer.InvariantCultureIgnoreCase);
    
    private delegate void CommandCallback(CCSPlayerController? caller, CommandInfo.CommandCallback callback);
    
    private static readonly string CommandsPath = Path.Combine(CS2_SimpleAdmin.ConfigDirectory, "Commands.json");
    private static readonly List<CommandMapping> CommandMappings =
    [
        new("css_ban", CS2_SimpleAdmin.Instance.OnBanCommand),
        new("css_addban", CS2_SimpleAdmin.Instance.OnAddBanCommand),
        new("css_banip", CS2_SimpleAdmin.Instance.OnBanIpCommand),
        new("css_unban", CS2_SimpleAdmin.Instance.OnUnbanCommand),
        new("css_warn", CS2_SimpleAdmin.Instance.OnWarnCommand),
        new("css_unwarn", CS2_SimpleAdmin.Instance.OnUnwarnCommand),

        new("css_asay", CS2_SimpleAdmin.Instance.OnAdminToAdminSayCommand),
        new("css_cssay", CS2_SimpleAdmin.Instance.OnAdminCustomSayCommand),
        new("css_say", CS2_SimpleAdmin.Instance.OnAdminSayCommand),
        new("css_psay", CS2_SimpleAdmin.Instance.OnAdminPrivateSayCommand),
        new("css_csay", CS2_SimpleAdmin.Instance.OnAdminCenterSayCommand),
        new("css_hsay", CS2_SimpleAdmin.Instance.OnAdminHudSayCommand),

        new("css_penalties", CS2_SimpleAdmin.Instance.OnPenaltiesCommand),
        new("css_admin", CS2_SimpleAdmin.Instance.OnAdminCommand),
        new("css_adminhelp", CS2_SimpleAdmin.Instance.OnAdminHelpCommand),
        new("css_addadmin", CS2_SimpleAdmin.Instance.OnAddAdminCommand),
        new("css_deladmin", CS2_SimpleAdmin.Instance.OnDelAdminCommand),
        new("css_addgroup", CS2_SimpleAdmin.Instance.OnAddGroup),
        new("css_delgroup", CS2_SimpleAdmin.Instance.OnDelGroupCommand),
        new("css_reloadadmins", CS2_SimpleAdmin.Instance.OnRelAdminCommand),
        new("css_reloadbans", CS2_SimpleAdmin.Instance.OnRelBans),
        new("css_hide", CS2_SimpleAdmin.Instance.OnHideCommand),
        new("css_hidecomms", CS2_SimpleAdmin.Instance.OnHideCommsCommand),
        new("css_who", CS2_SimpleAdmin.Instance.OnWhoCommand),
        new("css_disconnected", CS2_SimpleAdmin.Instance.OnDisconnectedCommand),
        new("css_warns", CS2_SimpleAdmin.Instance.OnWarnsCommand),
        new("css_players", CS2_SimpleAdmin.Instance.OnPlayersCommand),
        new("css_kick", CS2_SimpleAdmin.Instance.OnKickCommand),
        new("css_map", CS2_SimpleAdmin.Instance.OnMapCommand),
        new("css_wsmap", CS2_SimpleAdmin.Instance.OnWorkshopMapCommand),
        new("css_cvar", CS2_SimpleAdmin.Instance.OnCvarCommand),
        new("css_rcon", CS2_SimpleAdmin.Instance.OnRconCommand),
        new("css_rr", CS2_SimpleAdmin.Instance.OnRestartCommand),

        new("css_gag", CS2_SimpleAdmin.Instance.OnGagCommand),
        new("css_addgag", CS2_SimpleAdmin.Instance.OnAddGagCommand),
        new("css_ungag", CS2_SimpleAdmin.Instance.OnUngagCommand),
        new("css_mute", CS2_SimpleAdmin.Instance.OnMuteCommand),
        new("css_addmute", CS2_SimpleAdmin.Instance.OnAddMuteCommand),
        new("css_unmute", CS2_SimpleAdmin.Instance.OnUnmuteCommand),
        new("css_silence", CS2_SimpleAdmin.Instance.OnSilenceCommand),
        new("css_addsilence", CS2_SimpleAdmin.Instance.OnAddSilenceCommand),
        new("css_unsilence", CS2_SimpleAdmin.Instance.OnUnsilenceCommand),

        new("css_vote", CS2_SimpleAdmin.Instance.OnVoteCommand),

        new("css_noclip", CS2_SimpleAdmin.Instance.OnNoclipCommand),
        new("css_freeze", CS2_SimpleAdmin.Instance.OnFreezeCommand),
        new("css_unfreeze", CS2_SimpleAdmin.Instance.OnUnfreezeCommand),
        new("css_godmode", CS2_SimpleAdmin.Instance.OnGodCommand),

        new("css_slay", CS2_SimpleAdmin.Instance.OnSlayCommand),
        new("css_slap", CS2_SimpleAdmin.Instance.OnSlapCommand),
        new("css_give", CS2_SimpleAdmin.Instance.OnGiveCommand),
        new("css_strip", CS2_SimpleAdmin.Instance.OnStripCommand),
        new("css_hp", CS2_SimpleAdmin.Instance.OnHpCommand),
        new("css_speed", CS2_SimpleAdmin.Instance.OnSpeedCommand),
        new("css_gravity", CS2_SimpleAdmin.Instance.OnGravityCommand),
        new("css_resize", CS2_SimpleAdmin.Instance.OnResizeCommand),
        new("css_money", CS2_SimpleAdmin.Instance.OnMoneyCommand),
        new("css_team", CS2_SimpleAdmin.Instance.OnTeamCommand),
        new("css_rename", CS2_SimpleAdmin.Instance.OnRenameCommand),
        new("css_prename", CS2_SimpleAdmin.Instance.OnPrenameCommand),
        new("css_respawn", CS2_SimpleAdmin.Instance.OnRespawnCommand),
        new("css_tp", CS2_SimpleAdmin.Instance.OnGotoCommand),
        new("css_bring", CS2_SimpleAdmin.Instance.OnBringCommand),
        new("css_pluginsmanager", CS2_SimpleAdmin.Instance.OnPluginManagerCommand),
        new("css_adminvoice", CS2_SimpleAdmin.Instance.OnAdminVoiceCommand)
    ];

    /// <summary>
    /// Initializes command registration.
    /// If the commands config file does not exist, creates it and then recurses to register commands.
    /// Otherwise, directly registers commands from the configuration.
    /// </summary>
    public static void InitializeCommands()
    {
        if (!File.Exists(CommandsPath))
        {
            CreateConfig();
            InitializeCommands();
        }
        else
        {
            Register();
        }
    }

    /// <summary>
    /// Creates the default commands configuration JSON file with built-in commands and aliases.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private static void CreateConfig()
    {
        var commands = new CommandsConfig
        {
            Commands = new Dictionary<string, Command>
            {
                { "css_ban", new Command { Aliases = ["css_ban"] } },
                { "css_addban", new Command { Aliases = ["css_addban"] } },
                { "css_banip", new Command { Aliases = ["css_banip"] } },
                { "css_unban", new Command { Aliases = ["css_unban"] } },
                { "css_warn", new Command { Aliases = ["css_warn"] } },
                { "css_unwarn", new Command { Aliases = ["css_unwarn"] } },
                { "css_asay", new Command { Aliases = ["css_asay"] } },
                { "css_cssay", new Command { Aliases = ["css_cssay"] } },
                { "css_say", new Command { Aliases = ["css_say"] } },
                { "css_psay", new Command { Aliases = ["css_psay"] } },
                { "css_csay", new Command { Aliases = ["css_csay"] } },
                { "css_hsay", new Command { Aliases = ["css_hsay"] } },
                { "css_penalties", new Command { Aliases = ["css_penalties", "css_mypenalties", "css_comms"] } },
                { "css_admin", new Command { Aliases = ["css_admin"] } },
                { "css_adminhelp", new Command { Aliases = ["css_adminhelp"] } },
                { "css_addadmin", new Command { Aliases = ["css_addadmin"] } },
                { "css_deladmin", new Command { Aliases = ["css_deladmin"] } },
                { "css_addgroup", new Command { Aliases = ["css_addgroup"] } },
                { "css_delgroup", new Command { Aliases = ["css_delgroup"] } },
                { "css_reloadadmins", new Command { Aliases = ["css_reloadadmins"] } },
                { "css_reloadbans", new Command { Aliases = ["css_reloadbans"] } },
                { "css_hide", new Command { Aliases = ["css_hide", "css_stealth"] } },
                { "css_hidecomms", new Command { Aliases = ["css_hidecomms"] } },
                { "css_who", new Command { Aliases = ["css_who"] } },
                { "css_disconnected", new Command { Aliases = ["css_disconnected", "css_last"] } },
                { "css_warns", new Command { Aliases = ["css_warns"] } },
                { "css_players", new Command { Aliases = ["css_players"] } },
                { "css_kick", new Command { Aliases = ["css_kick"] } },
                { "css_map", new Command { Aliases = ["css_map", "css_changemap"] } },
                { "css_wsmap", new Command { Aliases = ["css_wsmap", "css_changewsmap", "css_workshop"] } },
                { "css_cvar", new Command { Aliases = ["css_cvar"] } },
                { "css_rcon", new Command { Aliases = ["css_rcon"] } },
                { "css_rr", new Command { Aliases = ["css_rr", "css_rg", "css_restart", "css_restartgame"] } },
                { "css_gag", new Command { Aliases = ["css_gag"] } },
                { "css_addgag", new Command { Aliases = ["css_addgag"] } },
                { "css_ungag", new Command { Aliases = ["css_ungag"] } },
                { "css_mute", new Command { Aliases = ["css_mute"] } },
                { "css_addmute", new Command { Aliases = ["css_addmute"] } },
                { "css_unmute", new Command { Aliases = ["css_unmute"] } },
                { "css_silence", new Command { Aliases = ["css_silence"] } },
                { "css_addsilence", new Command { Aliases = ["css_addsilence"] } },
                { "css_unsilence", new Command { Aliases = ["css_unsilence"] } },
                { "css_vote", new Command { Aliases = ["css_vote"] } },
                { "css_noclip", new Command { Aliases = ["css_noclip"] } },
                { "css_freeze", new Command { Aliases = ["css_freeze"] } },
                { "css_unfreeze", new Command { Aliases = ["css_unfreeze"] } },
                { "css_godmode", new Command { Aliases = ["css_godmode"] } },
                { "css_slay", new Command { Aliases = ["css_slay"] } },
                { "css_slap", new Command { Aliases = ["css_slap"] } },
                { "css_give", new Command { Aliases = ["css_give"] } },
                { "css_strip", new Command { Aliases = ["css_strip"] } },
                { "css_hp", new Command { Aliases = ["css_hp"] } },
                { "css_speed", new Command { Aliases = ["css_speed"] } },
                { "css_gravity", new Command { Aliases = ["css_gravity"] } },
                { "css_resize", new Command { Aliases = ["css_resize", "css_size"] } },
                { "css_money", new Command { Aliases = ["css_money"] } },
                { "css_team", new Command { Aliases = ["css_team"] } },
                { "css_rename", new Command { Aliases = ["css_rename"] } },
                { "css_prename", new Command { Aliases = ["css_prename"] } },
                { "css_respawn", new Command { Aliases = ["css_respawn"] } },
                { "css_tp", new Command { Aliases = ["css_tp", "css_tpto", "css_goto"] } },
                { "css_bring", new Command { Aliases = ["css_bring", "css_tphere"] } },
                { "css_pluginsmanager", new Command { Aliases = ["css_pluginsmanager", "css_pluginmanager"] } },
                { "css_adminvoice", new Command { Aliases = ["css_adminvoice", "css_listenall"] } }
            }
        };
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(commands, options);
        File.WriteAllText(CommandsPath, json);
    }

    /// <summary>
    /// Reads the command configuration JSON file and registers all commands and their aliases with their callbacks.
    /// Also registers any custom commands previously stored.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private static void Register()
    {
        var json = File.ReadAllText(CommandsPath);
        var commandsConfig = JsonSerializer.Deserialize<CommandsConfig>(json, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (commandsConfig?.Commands == null) return;
        
        foreach (var command in commandsConfig.Commands)
        {
            if (command.Value.Aliases == null) continue;
            
            CS2_SimpleAdmin._logger?.LogInformation(
                $"Registering command: `{command.Key}` with aliases: `{string.Join(", ", command.Value.Aliases)}`");
            
            var mapping = CommandMappings.FirstOrDefault(m => m.CommandKey == command.Key);
            if (mapping == null || command.Value.Aliases.Length == 0) continue;
            
            foreach (var alias in command.Value.Aliases)
            {
                CS2_SimpleAdmin.Instance.AddCommand(alias, "", mapping.Callback);
            }
        }
        
        foreach (var (name, definitions) in RegisterCommands._commandDefinitions)
        {
            foreach (var definition in definitions)
            {
                CS2_SimpleAdmin._logger?.LogInformation($"Registering custom command: `{name}`");
                CS2_SimpleAdmin.Instance.AddCommand(name, definition.Description, definition.Callback);
            }
        }
    }
    
    /// <summary>
    /// Represents the JSON configuration structure for commands.
    /// </summary>
    private class CommandsConfig
    {
        public Dictionary<string, Command>? Commands { get; init; }
    }
    
    /// <summary>
    /// Represents a command definition containing a list of aliases.
    /// </summary>
    private class Command
    {
        public string[]? Aliases { get; init; }
    }
    
    /// <summary>
    /// Maps a command key to its respective command callback handler.
    /// </summary>
    private class CommandMapping(string commandKey, CommandInfo.CommandCallback callback)
    {
        public string CommandKey { get; } = commandKey;
        public CommandInfo.CommandCallback Callback { get; } = callback;
    }
}