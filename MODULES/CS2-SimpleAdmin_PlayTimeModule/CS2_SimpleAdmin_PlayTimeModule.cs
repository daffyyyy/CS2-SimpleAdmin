using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdmin_PlayTimeModule;

public sealed class CS2_SimpleAdmin_PlayTimeModule : BasePlugin
{
    public override string ModuleName => "[CS2-SimpleAdmin] PlayTime Module";
    public override string ModuleVersion => "1.0.0";

    private static ICS2_SimpleAdminApi? _sharedApi;
    private readonly PluginCapability<ICS2_SimpleAdminApi> _pluginCapability  = new("simpleadmin:api");
    
    private Database? _database;
    private readonly Dictionary<ulong, PlayTimeModel> _playTimes = [];
    
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _sharedApi = _pluginCapability.Get();

        if (_sharedApi == null)
        {
            Logger.LogError("CS2-SimpleAdmin SharedApi not found");
            Unload(false);
            return;
        }
        
        AddCommandListener("jointeam", JoinTeamListener);
        // ## Zrobic to na jointeam
        
        _database = new Database(_sharedApi.GetConnectionString());
    }

    private HookResult JoinTeamListener(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;
        
        int team = GetTeamNumber(commandinfo);

        if (player.TeamNum == team)
            return HookResult.Continue;
        
        var steamId = player.SteamID;
        if (!_playTimes.TryGetValue(steamId, out var playTimeModel)) return HookResult.Continue;

        HandlePlayTimeUpdate(playTimeModel.OldTeam, team, playTimeModel, x => x.Teams[playTimeModel.OldTeam].JoinedTime, time => playTimeModel.Teams[playTimeModel.OldTeam].PlayTime += time);
        playTimeModel.Teams[player.TeamNum].JoinedTime = DateTime.Now;
        playTimeModel.OldTeam = team;
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult EventPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo _)
    {
        if (_sharedApi == null || _database == null)
            return HookResult.Continue;
        
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        var steamId = player.SteamID;
        
        Task.Run(async () =>
        {
            var playTimeModel = await _database.GetPlayTimeAsync(steamId, _sharedApi.GetServerId());
            if (playTimeModel == null)
                return;
            
            _playTimes.Add(steamId, playTimeModel);
        });

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        if (_sharedApi == null || _database == null)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        var steamId = player.SteamID;

        if (!_playTimes.TryGetValue(steamId, out var playTimeModel)) return HookResult.Continue;

        var totalPlayTime = (int)(DateTime.Now - playTimeModel.JoinedTime).TotalSeconds;
        if (totalPlayTime <= 10) return HookResult.Continue;
        playTimeModel.TotalPlayTime += totalPlayTime;
        
        HandlePlayTimeUpdate(playTimeModel.OldTeam, playTimeModel.OldTeam, playTimeModel, x => x.Teams[playTimeModel.OldTeam].JoinedTime, time => playTimeModel.Teams[playTimeModel.OldTeam].PlayTime += time);
        
        Task.Run(async () =>
        {
            await _database.UpdatePlayTimeAsync(steamId, playTimeModel, _sharedApi.GetServerId());
            _playTimes.Remove(steamId);
        });

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult EventPlayerChangeTeam(EventPlayerTeam @event, GameEventInfo _)
    {
        if (_sharedApi == null || _database == null)
            return HookResult.Continue;
        
        var player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;
        
        var steamId = player.SteamID;
        if (!_playTimes.TryGetValue(steamId, out var playTimeModel)) return HookResult.Continue;

        // Set the appropriate join time based on the team
        playTimeModel.Teams[@event.Team].JoinedTime = DateTime.Now;
        
        HandlePlayTimeUpdate(playTimeModel.OldTeam, @event.Team, playTimeModel, x => x.Teams[playTimeModel.OldTeam].JoinedTime, time => playTimeModel.Teams[playTimeModel.OldTeam].PlayTime += time);
        playTimeModel.OldTeam = @event.Team;
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo _)
    {
        if (_sharedApi == null || _database == null)
            return HookResult.Continue;
    
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot || player.Connected != PlayerConnectedState.PlayerConnected)
            return HookResult.Continue;
        
        var steamId = player.SteamID;
        // if (AdminManager.PlayerHasPermissions(new SteamID(steamId)))
        if (!_playTimes.TryGetValue(steamId, out var playTimeModel)) return HookResult.Continue;
    
        if (playTimeModel.OldTeam != player.TeamNum)
        {
            HandlePlayTimeUpdate(playTimeModel.OldTeam, player.TeamNum, playTimeModel, x => x.Teams[playTimeModel.OldTeam].JoinedTime, time => playTimeModel.Teams[playTimeModel.OldTeam].PlayTime += time);
        }
        
        playTimeModel.OldTeam = player.TeamNum;
        playTimeModel.Teams[player.TeamNum].JoinedTime = DateTime.Now;
        
        return HookResult.Continue;
    }

    private static void HandlePlayTimeUpdate(int oldTeam, int newTeam, PlayTimeModel playTimeModel, Func<PlayTimeModel, DateTime?> getJoinTime, Action<int> updatePlayTime)
    {
        var joinedTime = getJoinTime(playTimeModel);
        if (!joinedTime.HasValue) return;

        // Add playtime to the appropriate team
        var playTime = (int)(DateTime.Now - joinedTime.Value).TotalSeconds;
        
        updatePlayTime(playTime); // Update playtime for the corresponding team

        // Clear the join time after updating playtime
        playTimeModel.Teams[oldTeam].JoinedTime = null;
    }
    
    private static int GetTeamNumber(CommandInfo info)
    {
        var startIndex = info.ArgByIndex(0).Equals("jointeam", StringComparison.CurrentCultureIgnoreCase) ? 1 : 0;
        return info.ArgCount > startIndex && int.TryParse(info.ArgByIndex(startIndex), out var teamId) ? teamId : -1;
    }

}