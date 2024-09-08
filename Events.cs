﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using System.Text;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CS2_SimpleAdmin.Enums;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Models;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
	private void RegisterEvents()
	{
		RegisterListener<Listeners.OnMapStart>(OnMapStart);
		RegisterListener<Listeners.OnMapStart>(OnMapStart);
		RegisterListener<Listeners.OnGameServerSteamAPIActivated>(OnGameServerSteamAPIActivated);
		AddCommandListener("say", OnCommandSay);
		AddCommandListener("say_team", OnCommandTeamSay);
	}

	private void OnGameServerSteamAPIActivated()
	{
		new ServerManager().LoadServerData();
	}

	[GameEventHandler]
	public HookResult OnClientDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
	{
		var player = @event.Userid;

#if DEBUG
        Logger.LogCritical("[OnClientDisconnect] Before");
#endif

		if (player == null || !player.IsValid || player.IsBot)
		{
			return HookResult.Continue;
		}

#if DEBUG
        Logger.LogCritical("[OnClientDisconnect] After Check");
#endif
		try
		{
			if (DisconnectedPlayers.Count > 5)
				DisconnectedPlayers.RemoveAt(0);
			
			DisconnectedPlayers.Add(new DisconnectedPlayer(new SteamID(player.SteamID), player.PlayerName, player.IpAddress?.Split(":")[0], DateTime.Now));
			PlayerPenaltyManager.RemoveAllPenalties(player.Slot);
		
			if (TagsDetected)
				Server.ExecuteCommand($"css_tag_unmute {player.SteamID}");

			SilentPlayers.Remove(player.Slot);
			GodPlayers.Remove(player.Slot);
			
			if (player.UserId.HasValue)
				PlayersInfo.Remove(player.UserId.Value);

			var authorizedSteamId = player.AuthorizedSteamID;
			if (authorizedSteamId == null || !PermissionManager.AdminCache.TryGetValue(authorizedSteamId,
											  out var expirationTime)
										  || !(expirationTime <= DateTime.UtcNow)) return HookResult.Continue;

			AdminManager.ClearPlayerPermissions(authorizedSteamId);
			AdminManager.RemovePlayerAdminData(authorizedSteamId);

			return HookResult.Continue;
		}
		catch (Exception ex)
		{
			Logger.LogError($"An error occurred in OnClientDisconnect: {ex.Message}");
			return HookResult.Continue;
		}
	}

	[GameEventHandler]
	public HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
	{
		var player = @event.Userid;

		if (player == null || !player.IsValid || player.IsBot)
			return HookResult.Continue;
		
		new PlayerManager().LoadPlayerData(player);
		
		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnRoundEnd(EventRoundStart @event, GameEventInfo info)
	{
#if DEBUG
		Logger.LogCritical("[OnRoundEnd]");
#endif

		GodPlayers.Clear();
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

	public HookResult OnCommandSay(CCSPlayerController? player, CommandInfo info)
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
	}

	public HookResult OnCommandTeamSay(CCSPlayerController? player, CommandInfo info)
	{
		if (player == null || !player.IsValid || player.IsBot)
			return HookResult.Continue;

		if (info.GetArg(1).StartsWith($"/")
			|| info.GetArg(1).StartsWith($"!"))
			return HookResult.Continue;

		if (info.GetArg(1).Length == 0)
			return HookResult.Handled;

		if (PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Gag) || PlayerPenaltyManager.IsPenalized(player.Slot, PenaltyType.Silence))
			return HookResult.Handled;

		if (!info.GetArg(1).StartsWith($"@")) return HookResult.Continue;

		StringBuilder sb = new();

		if (AdminManager.PlayerHasPermissions(player, "@css/chat"))
		{
			sb.Append(_localizer!["sa_adminchat_template_admin", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
			foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && p is { IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(p, "@css/chat")))
			{
				p.PrintToChat(sb.ToString());
			}
		}
		else
		{
			sb.Append(_localizer!["sa_adminchat_template_player", player.PlayerName, info.GetArg(1).Remove(0, 1)]);
			player.PrintToChat(sb.ToString());
			foreach (var p in Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(p, "@css/chat")))
			{
				p.PrintToChat(sb.ToString());
			}
		}

		return HookResult.Handled;
	}

	private void OnMapStart(string mapName)
	{
		if (Config.OtherSettings.ReloadAdminsEveryMapChange && ServerLoaded && ServerId != null)
			AddTimer(3.0f, () => ReloadAdmins(null));

		AddTimer(34, () =>
		{
			if (!ServerLoaded)
				OnGameServerSteamAPIActivated();
		});

		var path = Path.GetDirectoryName(ModuleDirectory);
		if (Directory.Exists(path + "/CS2-Tags"))
		{
			TagsDetected = true;
		}

		GodPlayers.Clear();
		SilentPlayers.Clear();

		PlayerPenaltyManager.RemoveAllPenalties();
		new PlayerManager().CheckPlayersTimer();
	}

	[GameEventHandler]
	public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		var player = @event.Userid;

		if (player is null || @event.Attacker is null || !player.PawnIsAlive || player.PlayerPawn.Value == null)
			return HookResult.Continue;

		if (!GodPlayers.Contains(player.Slot)) return HookResult.Continue;

		player.PlayerPawn.Value.Health = player.PlayerPawn.Value.MaxHealth;
		player.PlayerPawn.Value.ArmorValue = 100;

		return HookResult.Continue;
	}

	[GameEventHandler]
	public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		var player = @event.Userid;

		if (player?.UserId == null || player.IsBot || player.Connected != PlayerConnectedState.PlayerConnected)
			return HookResult.Continue;
		
		PlayersInfo[player.UserId.Value].DiePosition =
			new DiePosition(
				new Vector(player.PlayerPawn.Value?.AbsOrigin?.X, player.PlayerPawn.Value?.AbsOrigin?.Y,
					player.PlayerPawn.Value?.AbsOrigin?.Z),
				new QAngle(player.PlayerPawn.Value?.AbsRotation?.X, player.PlayerPawn.Value?.AbsRotation?.Y,
					player.PlayerPawn.Value?.AbsRotation?.Z));
		
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