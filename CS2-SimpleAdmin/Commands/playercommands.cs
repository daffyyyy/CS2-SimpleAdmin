using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    /// <summary>
    /// Executes the 'slay' command, forcing the targeted players to commit suicide.
    /// Checks player validity and permissions.
    /// </summary>
    /// <param name="caller">Player or console issuing the command.</param>
    /// <param name="command">Command details, including targets.</param>
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSlayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is {IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            Slay(caller, player, callerName, command);
        });
        
        Helper.LogCommand(caller, command);
    }

    /// <summary>
    /// Performs the actual slay action on a player, with notification and logging.
    /// </summary>
    /// <param name="caller">Admin or console issuing the slay.</param>
    /// <param name="player">Target player to slay.</param>
    /// <param name="callerName">Optional name to display as the slayer.</param>
    /// <param name="command">Optional command info for logging.</param>
    internal static void Slay(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
    {
        if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected) return;
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Make the player commit suicide
        player.CommitSuicide(false, true);
        player.EmitSound("Player.Death");

        // Determine message keys and arguments for the slay notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_slay_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Log the command and send Discord notification
        if (command == null)
            Helper.LogCommand(caller, $"css_slay {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
    }

    /// <summary>
    /// Applies damage as a slap effect to the targeted players.
    /// </summary>
    /// <param name="caller">The player/admin executing the slap command.</param>
    /// <param name="command">The command including targets and optional damage value.</param>
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [damage]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSlapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var damage = 0;

        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        if (command.ArgCount >= 2)
        {
            int.TryParse(command.GetArg(2), out damage);
        }

        playersToTarget.ForEach(player =>
        {
            if (player.Connected != PlayerConnectedState.PlayerConnected)
                return;

            if (caller!.CanTarget(player))
            {
                Slap(caller, player, damage, command);
            }
        });
        
        Helper.LogCommand(caller, command);
    }

    /// <summary>
    /// Applies slap damage to a specific player with notifications and logging.
    /// </summary>
    /// <param name="caller">The player/admin applying the slap effect.</param>
    /// <param name="player">The target player to slap.</param>
    /// <param name="damage">The damage amount to apply.</param>
    /// <param name="command">Optional command info for logging.</param>
    internal static void Slap(CCSPlayerController? caller, CCSPlayerController player, int damage, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";
        
        // Apply slap damage to the player
        player.Pawn.Value?.Slap(damage);
        player.EmitSound("Player.DamageFall");

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_slap {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {damage}");
        
        // Determine message key and arguments for the slap notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_slap_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

        if (_localizer != null)
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }
    }

    /// <summary>
    /// Changes the team of targeted players with optional kill on switch.
    /// </summary>
    /// <param name="caller">The player/admin issuing the command.</param>
    /// <param name="command">The command containing targets, team info, and optional kill flag.</param>
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 2, usage: "<#userid or name> [<ct/tt/spec>] [-k]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTeamCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        var teamName = command.GetArg(2).ToLower();
        string _teamName;
        var teamNum = CsTeam.Spectator;

        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        switch (teamName)
        {
            case "ct":
            case "counterterrorist":
                teamNum = CsTeam.CounterTerrorist;
                _teamName = "CT";
                break;

            case "t":
            case "tt":
            case "terrorist":
                teamNum = CsTeam.Terrorist;
                _teamName = "TT";
                break;

            case "swap":
                _teamName = "SWAP";
                break;

            default:
                teamNum = CsTeam.Spectator;
                _teamName = "SPEC";
                break;
        }

        var kill = command.GetArg(3).ToLower().Equals("-k");

        playersToTarget.ForEach(player =>
        {
            ChangeTeam(caller, player, _teamName, teamNum, kill, command);
        });
        
        Helper.LogCommand(caller, command);
    }

    /// <summary>
    /// Changes the team of a player with various conditions and logs the operation.
    /// </summary>
    /// <param name="caller">The player/admin issuing the change.</param>
    /// <param name="player">The target player.</param>
    /// <param name="teamName">Team name string.</param>
    /// <param name="teamNum">Team enumeration value.</param>
    /// <param name="kill">If true, kills player on team change.</param>
    /// <param name="command">Optional command info for logging.</param>
    internal static void ChangeTeam(CCSPlayerController? caller, CCSPlayerController player, string teamName, CsTeam teamNum, bool kill, CommandInfo? command = null)
    {
        // Check if the player is valid and connected
        if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
            return;

        // Ensure the caller can target the player
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Change team based on the provided teamName and conditions
        if (!teamName.Equals("swap", StringComparison.OrdinalIgnoreCase))
        {
            if (player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && teamNum != CsTeam.Spectator && !kill && Instance.Config.OtherSettings.TeamSwitchType == 1)
                player.SwitchTeam(teamNum);
            else
                player.ChangeTeam(teamNum);
        }
        else
        {
            if (player.TeamNum != (byte)CsTeam.Spectator)
            {
                var _teamNum = (CsTeam)player.TeamNum == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                teamName = _teamNum == CsTeam.Terrorist ? "TT" : "CT";
                if (player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && !kill && Instance.Config.OtherSettings.TeamSwitchType == 1)
                    player.SwitchTeam(_teamNum);
                else
                    player.ChangeTeam(_teamNum);
            }
        }

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_team {player.PlayerName} {teamName}");

        // Determine message key and arguments for the team change notification
        var activityMessageKey = "sa_admin_team_message";
        var adminActivityArgs = new object[] { "CALLER", player.PlayerName, teamName };

        // Display admin activity message to other players
        if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

        Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
    }

    /// <summary>
    /// Renames targeted players to a new name.
    /// </summary>
    /// <param name="caller">The admin issuing the rename command.</param>
    /// <param name="command">The command including targets and new name.</param>
    [CommandHelper(1, "<#userid or name> <new name>")]
    [RequiresPermissions("@css/kick")]
    public void OnRenameCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Set default caller name if not provided
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Get the new name from the command arguments
        var newName = command.GetArg(2);

        // Check if the new name is valid
        if (string.IsNullOrEmpty(newName))
            return;

        // Retrieve the targets based on the command
        var targets = GetTarget(command);
        if (targets == null) return;

        // Filter out valid players from the targets
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        // Log the command
        Helper.LogCommand(caller, command);

        // Process each player to rename
        playersToTarget.ForEach(player =>
        {
            // Check if the player is connected and can be targeted
            if (player.Connected != PlayerConnectedState.PlayerConnected || !caller!.CanTarget(player))
                return;
            
            // Determine message key and arguments for the rename notification
            var activityMessageKey = "sa_admin_rename_message";
            var adminActivityArgs = new object[] { "CALLER", player.PlayerName, newName };

            // Display admin activity message to other players
            if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
            
            // Rename the player
            player.Rename(newName);
        });
    }

    /// <summary>
    /// Renames permamently targeted players to a new name.
    /// </summary>
    /// <param name="caller">The admin issuing the pre-rename command.</param>
    /// <param name="command">The command containing targets and new alias.</param>
    [CommandHelper(1, "<#userid or name> <new name>")]
    [RequiresPermissions("@css/ban")]
    public void OnPrenameCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Set default caller name if not provided
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Get the new name from the command arguments
        var newName = command.GetArg(2);

        // Retrieve the targets based on the command
        var targets = GetTarget(command);
        if (targets == null) return;

        // Filter out valid players from the targets
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        // Log the command
        Helper.LogCommand(caller, command);

        // Process each player to rename
        playersToTarget.ForEach(player =>
        {
            // Check if the player is connected and can be targeted
            if (player.Connected != PlayerConnectedState.PlayerConnected || !caller!.CanTarget(player))
                return;
            
            // Determine message key and arguments for the rename notification
            var activityMessageKey = "sa_admin_rename_message";
            var adminActivityArgs = new object[] { "CALLER", player.PlayerName, newName };

            // Display admin activity message to other players
            if (caller != null && !SilentPlayers.Contains(caller.Slot))
            {
                Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
            }
            
            // Determine if the new name is valid and update the renamed players list
            if (!string.IsNullOrEmpty(newName))
            {
                RenamedPlayers[player.SteamID] = newName;
                player.Rename(newName);
            }
            else
            {
                RenamedPlayers.Remove(player.SteamID);
            }
        });
    }

    /// <summary>
    /// Teleports targeted player(s) to another player's location.
    /// </summary>
    /// <param name="caller">Admin issuing teleport command.</param>
    /// <param name="command">Command containing teleport targets and destination.</param>
    [CommandHelper(1, "<#userid or name> [#userid or name]")]
    [RequiresPermissions("@css/kick")]
    public void OnGotoCommand(CCSPlayerController? caller, CommandInfo command)
    {
        IEnumerable<CCSPlayerController> playersToTeleport;
        CCSPlayerController? destinationPlayer;

        var targets = GetTarget(command);

        if (command.ArgCount < 3)
        {
            if (caller == null || caller.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE)
                return;

            if (targets == null || targets.Count() != 1)
                return;

            destinationPlayer = targets.Players.FirstOrDefault(p =>
                p is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE });

            if (destinationPlayer == null || !caller.CanTarget(destinationPlayer) || caller.PlayerPawn.Value == null)
                return;

            playersToTeleport = [caller];
        }
        else
        {
            var destination = GetTarget(command, 2);
            if (targets == null || destination == null || destination.Count() != 1)
                return;

            destinationPlayer = destination.Players.FirstOrDefault(p =>
                p is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE });

            if (destinationPlayer == null)
                return;

            playersToTeleport = targets.Players
                .Where(p => p is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE } && caller.CanTarget(p))
                .ToList();

            if (!playersToTeleport.Any())
                return;
        }

        // Log command
        Helper.LogCommand(caller, command);

        foreach (var player in playersToTeleport)
        {
            if (player.PlayerPawn?.Value == null || destinationPlayer?.PlayerPawn?.Value == null)
                continue;

            player.TeleportPlayer(destinationPlayer);

            player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

            destinationPlayer.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            destinationPlayer.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(destinationPlayer, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(destinationPlayer, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

            AddTimer(4, () =>
            {
                if (player is { IsValid: true, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE })
                {
                    player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                    player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                    Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                    Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
                }

                if (destinationPlayer.IsValid && destinationPlayer.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE)
                {
                    destinationPlayer.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                    destinationPlayer.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                    Utilities.SetStateChanged(destinationPlayer, "CCollisionProperty", "m_CollisionGroup");
                    Utilities.SetStateChanged(destinationPlayer, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
                }
            });

            if (caller != null && !SilentPlayers.Contains(caller.Slot) && _localizer != null)
            {
                Helper.ShowAdminActivity("sa_admin_tp_message", player.PlayerName, false, "CALLER", destinationPlayer.PlayerName);
            }
        }
    }
    
    /// <summary>
    /// Brings targeted player(s) to the caller or specified destination player's location.
    /// </summary>
    /// <param name="caller">Player issuing the bring command.</param>
    /// <param name="command">Command containing the destination and targets.</param>
    [CommandHelper(1, "<#destination or name> [#userid or name...]")]
    [RequiresPermissions("@css/kick")]
    public void OnBringCommand(CCSPlayerController? caller, CommandInfo command)
    {
        IEnumerable<CCSPlayerController> playersToTeleport;
        CCSPlayerController? destinationPlayer;

        if (command.ArgCount < 3)
        {
            if (caller == null || caller.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE)
                return;

            var targets = GetTarget(command);
            if (targets == null || !targets.Any())
                return;

            destinationPlayer = caller;

            playersToTeleport = targets.Players
                .Where(p => p is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE } && caller.CanTarget(p))
                .ToList();
        }
        else
        {
            var destination = GetTarget(command);
            if (destination == null || destination.Count() != 1)
                return;

            destinationPlayer = destination.Players.FirstOrDefault(p =>
                p is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE });

            if (destinationPlayer == null)
                return;

            // Rest args = targets to teleport
            var targets = GetTarget(command, 2);
            if (targets == null || !targets.Any())
                return;

            playersToTeleport = targets.Players
                .Where(p => p is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE } && caller!.CanTarget(p))
                .ToList();
        }

        if (destinationPlayer == null || !playersToTeleport.Any())
            return;

        // Log command
        Helper.LogCommand(caller, command);

        foreach (var player in playersToTeleport)
        {
            if (player.PlayerPawn?.Value == null || destinationPlayer.PlayerPawn?.Value == null)
                continue;

            // Teleport
            player.TeleportPlayer(destinationPlayer);

            player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

            destinationPlayer.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            destinationPlayer.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(destinationPlayer, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(destinationPlayer, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

            AddTimer(4, () =>
            {
                if (player is { IsValid: true, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE })
                {
                    player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                    player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                    Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                    Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
                }

                if (destinationPlayer.IsValid && destinationPlayer.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE)
                {
                    destinationPlayer.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                    destinationPlayer.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                    Utilities.SetStateChanged(destinationPlayer, "CCollisionProperty", "m_CollisionGroup");
                    Utilities.SetStateChanged(destinationPlayer, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
                }
            });

            if (caller != null && !SilentPlayers.Contains(caller.Slot) && _localizer != null)
            {
                Helper.ShowAdminActivity("sa_admin_bring_message", player.PlayerName, false, "CALLER", destinationPlayer.PlayerName);
            }
        }
    }

    // [CommandHelper(1, "<#userid or name> [#userid or name]")]
    // [RequiresPermissions("@css/kick")]
    // public void OnBringCommand(CCSPlayerController? caller, CommandInfo command)
    // {
    //     // Check if the caller is valid and has a live pawn
    //     if (caller == null || caller.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE) 
    //         return;
    //
    //     // Get the target players
    //     var targets = GetTarget(command);
    //     if (targets == null || targets.Count() > 1) return;
    //
    //     var playersToTarget = targets.Players
    //         .Where(player => player is { IsValid: true, IsHLTV: false })
    //         .ToList();
    //
    //     // Log the command
    //     Helper.LogCommand(caller, command);
    //
    //     // Process each player to teleport
    //     foreach (var player in playersToTarget.Where(player => player is { Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).Where(caller.CanTarget))
    //     {
    //         if (caller.PlayerPawn.Value == null || player.PlayerPawn.Value == null)
    //             continue;
    //
    //         // Teleport the player to the caller and toggle noclip
    //         player.TeleportPlayer(caller);
    //         // caller.PlayerPawn.Value.ToggleNoclip();
    //         
    //         caller.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
    //         caller.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
    //         
    //         Utilities.SetStateChanged(caller, "CCollisionProperty", "m_CollisionGroup");
    //         Utilities.SetStateChanged(caller, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
    //         
    //         player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
    //         player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
    //         
    //         Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
    //         Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
    //
    //         // Set a timer to toggle collision back after 4 seconds
    //         AddTimer(4, () =>
    //         {
    //             if (!player.IsValid || player.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE)
    //                 return;
    //             
    //             caller.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
    //             caller.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
    //         
    //             Utilities.SetStateChanged(caller, "CCollisionProperty", "m_CollisionGroup");
    //             Utilities.SetStateChanged(caller, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
    //             
    //             player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
    //             player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
    //         
    //             Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
    //             Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
    //         });
    //
    //         // Prepare message key and arguments for the bring notification
    //         var activityMessageKey = "sa_admin_bring_message";
    //         var adminActivityArgs = new object[] { "CALLER", player.PlayerName };
    //
    //         // Show admin activity
    //         if (!SilentPlayers.Contains(caller.Slot) && _localizer != null)
    //         {
    //             Helper.ShowAdminActivity(activityMessageKey, caller.PlayerName, false, adminActivityArgs);
    //         }
    //     }
    // }
}