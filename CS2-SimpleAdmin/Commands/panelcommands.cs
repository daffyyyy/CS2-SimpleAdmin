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
using Newtonsoft.Json;
using System.Globalization;
using System.Text;

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
        
        List<CCSPlayerController> playersToTarget = Utilities.GetPlayers().Where(player => caller!.CanTarget(player) && !player.IsHLTV).ToList();

        string Map = Server.MapName;
        int Players = playersToTarget.Count;
        int MaxPlayers = Server.MaxPlayers;
        string[] Maps;

        MaxPlayers = ConVar.Find("sv_visiblemaxplayers")?.GetPrimitiveValue<int>() ?? -1;
		MaxPlayers = MaxPlayers <= -1 ? Server.MaxPlayers : MaxPlayers;

        try
        {
            Maps = Server.GetMapList();
        }
        catch (Exception)
        {
            Maps = Array.Empty<string>(); // return an empty array
        }

        var server = new
        {
            map = Map,
            p = Players,
            mP = MaxPlayers,
            // maps = Maps,
            pr = ModuleVersion
        };

        List<Task<object>> playerTasks = playersToTarget
        .FindAll(player => !player.IsBot && !player.IsHLTV && player.PlayerName != "")
        .Select(player =>
        {
            string deaths = player.ActionTrackingServices!.MatchStats.Deaths.ToString();
            string headshots = player.ActionTrackingServices!.MatchStats.HeadShotKills.ToString();
            string assists = player.ActionTrackingServices!.MatchStats.Assists.ToString();
            string damage = player.ActionTrackingServices!.MatchStats.Damage.ToString();
            string kills = player.ActionTrackingServices!.MatchStats.Kills.ToString();
            string time = player.ActionTrackingServices!.MatchStats.LiveTime.ToString();
            var user = new
            {
                id = player.UserId,
                // playerName = player.PlayerName,
                // ipAddress = player.IpAddress?.Split(":")[0],
                // accountId = player.AuthorizedSteamID?.AccountId.ToString(),
                // steamId2 = player.AuthorizedSteamID?.SteamId2,
                // steamId3 = player.AuthorizedSteamID?.SteamId3,
                s64 = player.AuthorizedSteamID?.SteamId64.ToString(),
                // ping = player.Ping,
                t = player.Team,
                // clanName = player.ClanName,
                k = kills,
                d = deaths,
                // assists,
                // headshots,
                // damage,
                s = player.Score,
                // roundScore = player.RoundScore,
                // roundsWon = player.RoundsWon,
                // mvps = player.MVPs,
                // time, // ? Fix this, it's not the time the player has been connected
                // avatar = player.AuthorizedSteamID != null ? await GetProfilePictureAsync(player.AuthorizedSteamID.SteamId64.ToString(), true) : ""
            };
            return Task.FromResult((object)user);
        }).ToList();

        List<object> players = new List<object>();
        try
        {
            players = Task.WhenAll(playerTasks).Result.ToList();
        }
        catch (AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
            {
                Logger.LogError(innerEx, "Error while querying players");
            }
        }

        string jsonString = JsonConvert.SerializeObject(
            new
            {
                server,
                players
            }
        );

        Server.PrintToConsole(jsonString);
    }
}