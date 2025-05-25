using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/cheats")]
    public void OnNoclipCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player =>
            player.IsValid &&
            player is { IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                NoClip(caller, player, callerName);
            }
        });
        
        Helper.LogCommand(caller, command);
    }

    internal static void NoClip(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Toggle no-clip mode for the player
        player.Pawn.Value?.ToggleNoclip();

        // Determine message keys and arguments for the no-clip notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_noclip_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_noclip {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
    }
    
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGodCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is {IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (player.Connected != PlayerConnectedState.PlayerConnected)
                return;

            if (caller!.CanTarget(player))
            {
                God(caller, player, command);
            }
        });
        
        Helper.LogCommand(caller, command);
    }

    internal static void God(CCSPlayerController? caller, CCSPlayerController player, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Toggle god mode for the player
        if (!GodPlayers.Add(player.Slot))
        {
            GodPlayers.Remove(player.Slot);
        }

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_god {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");

        // Determine message key and arguments for the god mode notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_god_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }
    }

    [CommandHelper(1, "<#userid or name> [duration]")]
    [RequiresPermissions("@css/slay")]
    public void OnFreezeCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        int.TryParse(command.GetArg(2), out var time);

        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                Freeze(caller, player, time, callerName, command);
            }
        });
        
        Helper.LogCommand(caller, command);
    }
    
    [CommandHelper(1, "<#userid or name> [size]")]
    [RequiresPermissions("@css/slay")]
    public void OnResizeCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var size);

        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (!caller!.CanTarget(player)) return;
            
            var sceneNode = player.PlayerPawn.Value!.CBodyComponent?.SceneNode;
            if (sceneNode == null) return;
            
            sceneNode.GetSkeletonInstance().Scale = size;
            player.PlayerPawn.Value.AcceptInput("SetScale", null, null, size.ToString(CultureInfo.InvariantCulture));

            Server.NextFrame(() =>
            {
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_CBodyComponent");
            });
            
            var (activityMessageKey, adminActivityArgs) =
                ("sa_admin_resize_message",
                    new object[] { "CALLER", player.PlayerName });

            // Display admin activity message to other players
            if (caller == null || !SilentPlayers.Contains(caller.Slot))
            {
                Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
            }
        });
        
        Helper.LogCommand(caller, command);
    }

    internal static void Freeze(CCSPlayerController? caller, CCSPlayerController player, int time, string? callerName = null, CommandInfo? command = null)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Freeze player pawn
        player.Pawn.Value?.Freeze();

        // Determine message keys and arguments for the freeze notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_freeze_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Schedule unfreeze for the player if time is specified
        if (time > 0)
        {
            Instance.AddTimer(time, () => player.Pawn.Value?.Unfreeze(), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Log the command and send Discord notification
        if (command == null)
            Helper.LogCommand(caller, $"css_freeze {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {time}");
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/slay")]
    public void OnUnfreezeCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            Unfreeze(caller, player, callerName, command);
        });
        
        Helper.LogCommand(caller, command);
    }

    internal static void Unfreeze(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Unfreeze player pawn
        player.Pawn.Value?.Unfreeze();

        // Determine message keys and arguments for the unfreeze notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_unfreeze_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Log the command and send Discord notification
        if (command == null)
            Helper.LogCommand(caller, $"css_unfreeze {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
    }
}