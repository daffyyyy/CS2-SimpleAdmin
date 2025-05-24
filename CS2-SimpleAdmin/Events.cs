using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Models;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Logging;
using System.Text;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.ValveConstants.Protobuf;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    private bool _serverLoading;
    
    private void RegisterEvents()
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnClientConnect>(OnClientConnect);
        RegisterListener<Listeners.OnGameServerSteamAPIActivated>(OnGameServerSteamAPIActivated);
        if (Config.OtherSettings.UserMessageGagChatType)
            HookUserMessage(118, HookUmChat);
        
        AddCommandListener(null, ComamndListenerHandler);
        // AddCommandListener("callvote", OnCommandCallVote);
        // AddCommandListener("say", OnCommandSay);
        // AddCommandListener("say_team", OnCommandTeamSay);
    }
    
    // private HookResult OnCommandCallVote(CCSPlayerController? caller, CommandInfo info)
    // {
    //     var voteType = info.GetArg(1).ToLower();
    //     
    //     if (voteType != "kick")
    //         return HookResult.Continue;
    //
    //     var target = int.TryParse(info.GetArg(2), out var userId) 
    //         ? Utilities.GetPlayerFromUserid(userId) 
    //         : null;
    //     
    //     if (target == null || !target.IsValid || target.Connected != PlayerConnectedState.PlayerConnected)
    //         return HookResult.Continue;
    //
    //     return !AdminManager.CanPlayerTarget(caller, target) ? HookResult.Stop : HookResult.Continue;
    // }

    private void OnGameServerSteamAPIActivated()
    {
        if (_serverLoading)
            return;
        
        _serverLoading = true;
        new ServerManager().LoadServerData();
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnClientDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event.Reason is 149 or 6)
            info.DontBroadcast = true;

        var player = @event.Userid;

#if DEBUG
        Logger.LogCritical("[OnClientDisconnect] Before");
#endif

        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

#if DEBUG
        Logger.LogCritical("[OnClientDisconnect] After Check");
#endif
        try
        {
            if (DisconnectedPlayers.Count >= Config.OtherSettings.DisconnectedPlayersHistoryCount)
                DisconnectedPlayers.RemoveAt(0);

            var steamId = new SteamID(player.SteamID);
            var disconnectedPlayer = DisconnectedPlayers.FirstOrDefault(p => p.SteamId == steamId);

            if (disconnectedPlayer != null)
            {
                disconnectedPlayer.Name = player.PlayerName;
                disconnectedPlayer.IpAddress = player.IpAddress?.Split(":")[0];
                disconnectedPlayer.DisconnectTime = Time.ActualDateTime();
            }
            else
            {
                DisconnectedPlayers.Add(new DisconnectedPlayer(steamId, player.PlayerName, player.IpAddress?.Split(":")[0], Time.ActualDateTime()));
            }

            PlayerPenaltyManager.RemoveAllPenalties(player.Slot);

            SilentPlayers.Remove(player.Slot);
            GodPlayers.Remove(player.Slot);
            SpeedPlayers.Remove(player.Slot);
            GravityPlayers.Remove(player);

            if (player.UserId.HasValue)
                PlayersInfo.TryRemove(player.UserId.Value, out _);
            
            if (!PermissionManager.AdminCache.TryGetValue(steamId, out var data) 
                || !(data.ExpirationTime <= Time.ActualDateTime()))
            {
                return HookResult.Continue;
            }
            
            AdminManager.RemovePlayerPermissions(steamId, PermissionManager.AdminCache[steamId].Flags.ToArray());
            AdminManager.RemovePlayerFromGroup(steamId, true, PermissionManager.AdminCache[steamId].Flags.ToArray());
            var adminData = AdminManager.GetPlayerAdminData(steamId);
            
            if (adminData == null || data.Flags.ToList().Count != 0 && adminData.Groups.ToList().Count != 0)
                return HookResult.Continue;
							
            AdminManager.ClearPlayerPermissions(steamId);
            AdminManager.RemovePlayerAdminData(steamId);

            return HookResult.Continue;
        }
        catch (Exception ex)
        {
            Logger.LogError($"An error occurred in OnClientDisconnect: {ex.Message}");
            return HookResult.Continue;
        }
    }
    
    private void OnClientConnect(int playerslot, string name, string ipaddress)
    {
#if DEBUG
        Logger.LogCritical("[OnClientConnect]");
#endif
        if (Config.OtherSettings.BanType == 0)
            return;
        
        if (Instance.CacheManager != null && !Instance.CacheManager.IsPlayerBanned(null, ipaddress.Split(":")[0]))
                return;

        Server.NextFrame((() =>
        {
            var player = Utilities.GetPlayerFromSlot(playerslot);
            if (player == null || !player.IsValid || player.IsBot)
                return;

            Helper.KickPlayer(player, NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
        }));
        
        // Server.NextFrame(() =>
        // {
        //     var player = Utilities.GetPlayerFromSlot(playerslot);
        //
        //     if (player == null || !player.IsValid || player.IsBot)
        //         return;
        //
        //     new PlayerManager().LoadPlayerData(player);
        // });
    }

    [GameEventHandler]
    public HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
#if DEBUG
        Logger.LogCritical("[OnPlayerFullConnect]");
#endif

        var player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

//         if (player.UserId.HasValue && PlayersInfo.TryGetValue(player.UserId.Value, out PlayerInfo? value) &&
// value.WaitingForKick)
//             return HookResult.Continue;
        
        new PlayerManager().LoadPlayerData(player);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
#if DEBUG
        Logger.LogCritical("[OnRoundStart]");
#endif

        GodPlayers.Clear();
        SpeedPlayers.Clear();
        GravityPlayers.Clear();
        
        foreach (var player in PlayersInfo.Values)
        {
            player.DiePosition = null;
        }

        AddTimer(0.41f, () =>
        {
            foreach (var list in RenamedPlayers)
            {
                var player = Utilities.GetPlayerFromSteamId(list.Key);

                if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
                    continue;

                if (player.PlayerName.Equals(list.Value))
                    continue;

                player.Rename(list.Value);
            }
        });

        return HookResult.Continue;
    }
    
    private HookResult HookUmChat(UserMessage um)
    {
        var author = Utilities.GetPlayerFromIndex(um.ReadInt("entityindex"));
        if (author == null || !author.IsValid || author.IsBot)
            return HookResult.Continue;

        if (!PlayerPenaltyManager.IsPenalized(author.Slot, PenaltyType.Gag, out DateTime? endDateTime) &&
            !PlayerPenaltyManager.IsPenalized(author.Slot, PenaltyType.Silence, out endDateTime))
            return HookResult.Continue;

        var message = um.ReadString("param2");

        if (_localizer == null || endDateTime is null) return HookResult.Continue;

        if (CoreConfig.PublicChatTrigger.Concat(CoreConfig.SilentChatTrigger).Any(trigger => message.StartsWith(trigger)))
        {
            foreach (var recipient in um.Recipients)
            {
                if (recipient == author)
                    continue;
            
                um.Recipients.Remove(recipient);
            }

            return HookResult.Continue;
        }
        
        author.SendLocalizedMessage(_localizer, "sa_player_penalty_chat_active", endDateTime.Value.ToString("g", author.GetLanguage()));
        return HookResult.Stop;
    }

    private HookResult ComamndListenerHandler(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        var command = info.GetArg(0).ToLower();

        if (Config.OtherSettings.AdditionalCommandsToLog.Contains(command))
            Helper.LogCommand(player, info);

        switch (command)
        {
            case "css_admins_reload":
                AddTimer(1.0f, () => ReloadAdmins(null));
                return HookResult.Continue;
            case "callvote":
            {
                var voteType = info.GetArg(1).ToLower();
            
                if (voteType != "kick")
                    return HookResult.Continue;

                var target = int.TryParse(info.GetArg(2), out var userId) 
                    ? Utilities.GetPlayerFromUserid(userId) 
                    : null;
        
                if (target == null || !target.IsValid || target.Connected != PlayerConnectedState.PlayerConnected)
                    return HookResult.Continue;
                
                return !player.CanTarget(target) ? HookResult.Stop : HookResult.Continue;
            }
        }

        if (!command.Contains("say"))
            return HookResult.Continue;
        
        if (!Config.OtherSettings.UserMessageGagChatType)
        {
            if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag, out DateTime? endDateTime) ||
                PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence, out endDateTime))
            {
                if (_localizer != null && endDateTime is not null)
                    player.SendLocalizedMessage(_localizer, "sa_player_penalty_chat_active", endDateTime.Value.ToString("g", player.GetLanguage()));
                return HookResult.Stop;
            }
        }

        if (info.GetArg(1).StartsWith($"/")
            || info.GetArg(1).StartsWith($"!"))
            return HookResult.Continue;

        if (info.GetArg(1).Length == 0)
            return HookResult.Stop;
        
        if (command == "say" && info.GetArg(1).StartsWith($"@") &&
            AdminManager.PlayerHasPermissions(new SteamID(player.SteamID), "@css/chat"))
        {
            player.ExecuteClientCommandFromServer($"css_say {info.GetArg(1).Remove(0, 1)}");
            return HookResult.Stop;
        }
        
        if (command != "say_team" || !info.GetArg(1).StartsWith($"@")) return HookResult.Continue;

        StringBuilder sb = new();
        if (AdminManager.PlayerHasPermissions(new SteamID(player.SteamID), "@css/chat"))
        {
            sb.Append(_localizer!["sa_adminchat_template_admin", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && p is { IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(new SteamID(p.SteamID), "@css/chat")))
            {
                p.PrintToChat(sb.ToString());
            }
        }
        else
        {
            sb.Append(_localizer!["sa_adminchat_template_player", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
            player.PrintToChat(sb.ToString());
            foreach (var p in Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(new SteamID(p.SteamID), "@css/chat")))
            {
                p.PrintToChat(sb.ToString());
            }
        }

        return HookResult.Stop;
    }

    /*public HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player == null ||  !player.IsValid || player.IsBot)
			return HookResult.Continue;

		if (info.GetArg(1).StartsWith($"/")
			|| info.GetArg(1).StartsWith($"!"))
			return HookResult.Continue;

		if (info.GetArg(1).Length == 0)
			return HookResult.Handled;

		if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) || PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
			return HookResult.Handled;

		return HookResult.Continue;
	}*/

    public HookResult OnCommandTeamSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (info.GetArg(1).StartsWith($"/")
            || info.GetArg(1).StartsWith($"!"))
            return HookResult.Continue;

        if (info.GetArg(1).Length == 0)
            return HookResult.Handled;

        if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag, out _) || PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence, out _))
            return HookResult.Stop;

        if (!info.GetArg(1).StartsWith($"@")) return HookResult.Continue;

        StringBuilder sb = new();

        if (AdminManager.PlayerHasPermissions(new SteamID(player.SteamID), "@css/chat"))
        {
            sb.Append(_localizer!["sa_adminchat_template_admin", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
            foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && p is { IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(new SteamID(p.SteamID), "@css/chat")))
            {
                p.PrintToChat(sb.ToString());
            }
        }
        else
        {
            sb.Append(_localizer!["sa_adminchat_template_player", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
            player.PrintToChat(sb.ToString());
            foreach (var p in Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(new SteamID(p.SteamID), "@css/chat")))
            {
                p.PrintToChat(sb.ToString());
            }
        }

        return HookResult.Handled;
    }

    private void OnMapStart(string mapName)
    {
        if (Config.OtherSettings.ReloadAdminsEveryMapChange && ServerLoaded && ServerId != null)
            AddTimer(5.0f, () => ReloadAdmins(null));

        AddTimer(1.0f, () => ServerManager.CheckHibernationStatus());

        // AddTimer(34, () =>
        // {
        //     if (!ServerLoaded)
        //         OnGameServerSteamAPIActivated();
        // });

        GodPlayers.Clear();
        SilentPlayers.Clear();
        SpeedPlayers.Clear();
        GravityPlayers.Clear();

        PlayerPenaltyManager.RemoveAllPenalties();
    }

    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player is null || @event.Attacker is null || player.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE || player.PlayerPawn.Value == null)
            return HookResult.Continue;

        if (SpeedPlayers.TryGetValue(player.Slot, out var speedPlayer))
            AddTimer(0.15f, () => player.SetSpeed(speedPlayer));
        
        if (!GodPlayers.Contains(player.Slot)) return HookResult.Continue;

        player.PlayerPawn.Value.Health = player.PlayerPawn.Value.MaxHealth;
        player.PlayerPawn.Value.ArmorValue = 100;

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player?.UserId == null || !player.IsValid || player.IsHLTV || player.Connected != PlayerConnectedState.PlayerConnected)
            return HookResult.Continue;

        SpeedPlayers.Remove(player.Slot);
        GravityPlayers.Remove(player);

        if (!PlayersInfo.ContainsKey(player.UserId.Value) || @event.Attacker == null)
            return HookResult.Continue;

        var playerPosition = player.PlayerPawn.Value?.AbsOrigin; 
        var playerRotation = player.PlayerPawn.Value?.AbsRotation;
        
        PlayersInfo[player.UserId.Value].DiePosition = new DiePosition(
            new Vector(
                playerPosition?.X ?? 0,
                playerPosition?.Y ?? 0,
                playerPosition?.Z ?? 0
            ),
            new QAngle(
                playerRotation?.X ?? 0,
                playerRotation?.Y ?? 0,
                playerRotation?.Z ?? 0
            )
        );

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (!SilentPlayers.Contains(player.Slot))
            return HookResult.Continue;

        info.DontBroadcast = true;

        if (@event.Team > 1)
            SilentPlayers.Remove(player.Slot);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerInfo(EventPlayerInfo @event, GameEventInfo _)
    {
        var player = @event.Userid;

        if (player is null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (!RenamedPlayers.TryGetValue(player.SteamID, out var name)) return HookResult.Continue;

        if (player.PlayerName.Equals(name))
            return HookResult.Continue;

        player.Rename(name);

        return HookResult.Continue;
    }
}