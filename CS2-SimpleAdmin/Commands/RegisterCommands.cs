using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CS2_SimpleAdmin;

public static class RegisterCommands
{
    private delegate void CommandCallback(CCSPlayerController? caller, CommandInfo.CommandCallback callback);
    
    private static readonly string CommandsPath = Path.Combine(CS2_SimpleAdmin.ConfigDirectory, "Commands.json");
    private static readonly List<CommandMapping> CommandMappings =
    [
        new CommandMapping("css_ban", CS2_SimpleAdmin.Instance.OnBanCommand),
        new CommandMapping("css_addban", CS2_SimpleAdmin.Instance.OnAddBanCommand),
        new CommandMapping("css_banip", CS2_SimpleAdmin.Instance.OnBanIpCommand),
        new CommandMapping("css_unban", CS2_SimpleAdmin.Instance.OnUnbanCommand),
        new CommandMapping("css_warn", CS2_SimpleAdmin.Instance.OnWarnCommand),
        new CommandMapping("css_unwarn", CS2_SimpleAdmin.Instance.OnUnwarnCommand),

        new CommandMapping("css_asay", CS2_SimpleAdmin.Instance.OnAdminToAdminSayCommand),
        new CommandMapping("css_cssay", CS2_SimpleAdmin.Instance.OnAdminCustomSayCommand),
        new CommandMapping("css_say", CS2_SimpleAdmin.Instance.OnAdminSayCommand),
        new CommandMapping("css_psay", CS2_SimpleAdmin.Instance.OnAdminPrivateSayCommand),
        new CommandMapping("css_csay", CS2_SimpleAdmin.Instance.OnAdminCenterSayCommand),
        new CommandMapping("css_hsay", CS2_SimpleAdmin.Instance.OnAdminHudSayCommand),

        new CommandMapping("css_penalties", CS2_SimpleAdmin.Instance.OnPenaltiesCommand),
        new CommandMapping("css_admin", CS2_SimpleAdmin.Instance.OnAdminCommand),
        new CommandMapping("css_adminhelp", CS2_SimpleAdmin.Instance.OnAdminHelpCommand),
        new CommandMapping("css_addadmin", CS2_SimpleAdmin.Instance.OnAddAdminCommand),
        new CommandMapping("css_deladmin", CS2_SimpleAdmin.Instance.OnDelAdminCommand),
        new CommandMapping("css_addgroup", CS2_SimpleAdmin.Instance.OnAddGroup),
        new CommandMapping("css_delgroup", CS2_SimpleAdmin.Instance.OnDelGroupCommand),
        new CommandMapping("css_reloadadmins", CS2_SimpleAdmin.Instance.OnRelAdminCommand),
        new CommandMapping("css_reloadbans", CS2_SimpleAdmin.Instance.OnRelBans),
        new CommandMapping("css_hide", CS2_SimpleAdmin.Instance.OnHideCommand),
        new CommandMapping("css_hidecomms", CS2_SimpleAdmin.Instance.OnHideCommsCommand),
        new CommandMapping("css_who", CS2_SimpleAdmin.Instance.OnWhoCommand),
        new CommandMapping("css_disconnected", CS2_SimpleAdmin.Instance.OnDisconnectedCommand),
        new CommandMapping("css_warns", CS2_SimpleAdmin.Instance.OnWarnsCommand),
        new CommandMapping("css_players", CS2_SimpleAdmin.Instance.OnPlayersCommand),
        new CommandMapping("css_kick", CS2_SimpleAdmin.Instance.OnKickCommand),
        new CommandMapping("css_map", CS2_SimpleAdmin.Instance.OnMapCommand),
        new CommandMapping("css_wsmap", CS2_SimpleAdmin.Instance.OnWorkshopMapCommand),
        new CommandMapping("css_cvar", CS2_SimpleAdmin.Instance.OnCvarCommand),
        new CommandMapping("css_rcon", CS2_SimpleAdmin.Instance.OnRconCommand),
        new CommandMapping("css_rr", CS2_SimpleAdmin.Instance.OnRestartCommand),

        new CommandMapping("css_gag", CS2_SimpleAdmin.Instance.OnGagCommand),
        new CommandMapping("css_addgag", CS2_SimpleAdmin.Instance.OnAddGagCommand),
        new CommandMapping("css_ungag", CS2_SimpleAdmin.Instance.OnUngagCommand),
        new CommandMapping("css_mute", CS2_SimpleAdmin.Instance.OnMuteCommand),
        new CommandMapping("css_addmute", CS2_SimpleAdmin.Instance.OnAddMuteCommand),
        new CommandMapping("css_unmute", CS2_SimpleAdmin.Instance.OnUnmuteCommand),
        new CommandMapping("css_silence", CS2_SimpleAdmin.Instance.OnSilenceCommand),
        new CommandMapping("css_addsilence", CS2_SimpleAdmin.Instance.OnAddSilenceCommand),
        new CommandMapping("css_unsilence", CS2_SimpleAdmin.Instance.OnUnsilenceCommand),

        new CommandMapping("css_vote", CS2_SimpleAdmin.Instance.OnVoteCommand),

        new CommandMapping("css_noclip", CS2_SimpleAdmin.Instance.OnNoclipCommand),
        new CommandMapping("css_freeze", CS2_SimpleAdmin.Instance.OnFreezeCommand),
        new CommandMapping("css_unfreeze", CS2_SimpleAdmin.Instance.OnUnfreezeCommand),
        new CommandMapping("css_godmode", CS2_SimpleAdmin.Instance.OnGodCommand),

        new CommandMapping("css_slay", CS2_SimpleAdmin.Instance.OnSlayCommand),
        new CommandMapping("css_slap", CS2_SimpleAdmin.Instance.OnSlapCommand),
        new CommandMapping("css_give", CS2_SimpleAdmin.Instance.OnGiveCommand),
        new CommandMapping("css_strip", CS2_SimpleAdmin.Instance.OnStripCommand),
        new CommandMapping("css_hp", CS2_SimpleAdmin.Instance.OnHpCommand),
        new CommandMapping("css_speed", CS2_SimpleAdmin.Instance.OnSpeedCommand),
        new CommandMapping("css_gravity", CS2_SimpleAdmin.Instance.OnGravityCommand),
        new CommandMapping("css_resize", CS2_SimpleAdmin.Instance.OnResizeCommand),
        new CommandMapping("css_money", CS2_SimpleAdmin.Instance.OnMoneyCommand),
        new CommandMapping("css_team", CS2_SimpleAdmin.Instance.OnTeamCommand),
        new CommandMapping("css_rename", CS2_SimpleAdmin.Instance.OnRenameCommand),
        new CommandMapping("css_prename", CS2_SimpleAdmin.Instance.OnPrenameCommand),
        new CommandMapping("css_respawn", CS2_SimpleAdmin.Instance.OnRespawnCommand),
        new CommandMapping("css_tp", CS2_SimpleAdmin.Instance.OnGotoCommand),
        new CommandMapping("css_bring", CS2_SimpleAdmin.Instance.OnBringCommand),
        new CommandMapping("css_pluginsmanager", CS2_SimpleAdmin.Instance.OnPluginManagerCommand),
        new CommandMapping("css_adminvoice", CS2_SimpleAdmin.Instance.OnAdminVoiceCommand)
    ];

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
        
        var json = JsonConvert.SerializeObject(commands, Formatting.Indented);
        File.WriteAllText(CommandsPath, json);
    }
    
    private static void Register()
    {
        var json = File.ReadAllText(CommandsPath);
        var commandsConfig = JsonConvert.DeserializeObject<CommandsConfig>(json);

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
    }
    
    private class CommandsConfig
    {
        public Dictionary<string, Command>? Commands { get; init; }
    }
    
    private class Command
    {
        public string[]? Aliases { get; init; }
    }
    
    private class CommandMapping(string commandKey, CommandInfo.CommandCallback callback)
    {
        public string CommandKey { get; } = commandKey;
        public CommandInfo.CommandCallback Callback { get; } = callback;
    }
}