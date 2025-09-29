using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Menus;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    [ConsoleCommand("css_panel_say", "Say to all players from the panel.")]
    [CommandHelper(1, "<message>")]
    [RequiresPermissions("@css/root")]
    public void OnPanelSayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!Config.IsCSSPanel) return;
        if (command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;

        byte[] utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
        string utf8String = Encoding.UTF8.GetString(utf8BytesString);

        foreach (CCSPlayerController player in Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV))
        {
            player.PrintToChat(StringExtensions.ReplaceColorTags(utf8String));
        }
    }

    /**
    * Prints the server info and a list of players to the console
    */
    [ConsoleCommand("css_query")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnQueryCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!Config.IsCSSPanel) return;

        var playersToTarget = Utilities.GetPlayers()
            .Where(player => caller!.CanTarget(player) && !player.IsHLTV)
            .ToList();

        string mapName = Server.MapName;
        int playersCount = playersToTarget.Count;
        int maxPlayers = ConVar.Find("sv_visiblemaxplayers")?.GetPrimitiveValue<int>() is int value and > 0 ? value : Server.MaxPlayers;

        string serverName = ConVar.Find("hostname")?.StringValue ?? "Unknown";

        // string[] maps;
        // try
        // {
        //     maps = Server.GetMapList();
        // }
        // catch (Exception)
        // {
        //     maps = Array.Empty<string>();
        // }

        var server = new
        {
            map = mapName,
            hN = serverName,
            p = playersCount,
            mP = maxPlayers,
            // maps = maps,
            pr = ModuleVersion
        };

        try
        {
            var filteredPlayers = playersToTarget
                .Where(player => !player.IsBot && !player.IsHLTV && !string.IsNullOrWhiteSpace(player.PlayerName));

            var players = filteredPlayers.Select(player =>
            {
                var stats = player.ActionTrackingServices!.MatchStats;

                return new
                {
                    id = player.UserId,
                    // playerName = player.PlayerName,
                    // ipAddress = player.IpAddress?.Split(":")[0],
                    // accountId = player.AuthorizedSteamID?.AccountId.ToString() ?? "",
                    // steamId2 = player.AuthorizedSteamID?.SteamId2.ToString() ?? "",
                    // steamId3 = player.AuthorizedSteamID?.SteamId3.ToString() ?? "",
                    pn = player.PlayerName,
                    s64 = player.AuthorizedSteamID?.SteamId64.ToString() ?? "",
                    // ping = player.Ping,
                    t = player.Team,
                    // clanName = player.ClanName,
                    k = stats.Kills.ToString(),
                    d = stats.Deaths.ToString(),
                    // stats.Assists.ToString(),
                    // stats.HeadShotKills.ToString(),
                    // stats.Damage.ToString(),
                    s = player.Score,
                    // roundScore = player.RoundScore,
                    // roundsWon = player.RoundsWon,
                    // mvps = player.MVPs,
                    // stats.LiveTime.ToString(), // ? Fix this, it's not the time the player has been connected
                    // avatar = player.AuthorizedSteamID != null ? await GetProfilePictureAsync(player.AuthorizedSteamID.SteamId64.ToString(), true) : ""
                };
            }).ToList();

            string jsonString = JsonSerializer.Serialize(new { server, players });
            Server.PrintToConsole(jsonString);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while query command");
        }
    }

    [ConsoleCommand("css_fexec")]
    [CommandHelper(minArgs: 2, usage: "<#userid or name or steamid> <command>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnFexecCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = command.GetArg(1);
        var exec = command.GetArg(2);

        List<CCSPlayerController> playersToTarget = Helper.GetValidPlayers();

        // Find the player by name, userid or steamid
        if (target.StartsWith("#"))
        {
            playersToTarget = playersToTarget.Where(player => player.UserId.ToString() == target.Replace("#", "")).ToList();
        }
        else if (Helper.IsValidSteamId64(target))
        {
            playersToTarget = playersToTarget.Where(player => player.SteamID.ToString() == target).ToList();
        }
        else
        {
            playersToTarget = playersToTarget.Where(player => player.PlayerName.ToLower().Contains(target.ToLower())).ToList();
        }

        playersToTarget.ForEach(player =>
        {
            if (caller.CanTarget(player))
            {
                player.ExecuteClientCommandFromServer(exec);
            }
        });
    }
}