using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace CS2_SimpleAdmin_FunCommands;

public partial class CS2_SimpleAdmin_FunCommands
{
    // =================================
    // COMMAND HANDLERS
    // =================================

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/cheats")]
    private void OnNoclipCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var targets = _sharedApi!.GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player =>
            player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                NoClip(caller, player);
            }
        });
    }

    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    private void OnGodCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var targets = _sharedApi!.GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player =>
            player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                God(caller, player);
            }
        });
    }

    [CommandHelper(1, "<#userid or name> [duration]")]
    [RequiresPermissions("@css/slay")]
    private void OnFreezeCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int.TryParse(command.GetArg(2), out var time);
        var targets = _sharedApi!.GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player =>
            player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                Freeze(caller, player, time);
            }
        });
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/slay")]
    private void OnUnfreezeCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var targets = _sharedApi!.GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player =>
            player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                Unfreeze(caller, player);
            }
        });
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/cheats")]
    private void OnRespawnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var targets = _sharedApi!.GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player =>
            player is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                Respawn(caller, player);
            }
        });
    }

    [CommandHelper(2, "<#userid or name> <weapon>")]
    [RequiresPermissions("@css/cheats")]
    private void OnGiveWeaponCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var weaponName = command.GetArg(2);
        if (Enum.TryParse(weaponName, true, out CsItem weapon))
        {
            var targets = _sharedApi!.GetTarget(command);
            if (targets == null) return;

            var playersToTarget = targets.Players.Where(player =>
                player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

            playersToTarget.ForEach(player =>
            {
                if (caller!.CanTarget(player))
                {
                    player.GiveNamedItem(weapon);
                    LogAndShowActivity(caller, player, "fun_admin_give_message", "css_give", weapon.ToString());
                }
            });
        }
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/slay")]
    private void OnStripWeaponsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var targets = _sharedApi!.GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player =>
            player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                player.RemoveWeapons();
                LogAndShowActivity(caller, player, "fun_admin_strip_message", "css_strip");
            }
        });
    }

    [CommandHelper(2, "<#userid or name> <hp>")]
    [RequiresPermissions("@css/slay")]
    private void OnSetHpCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (int.TryParse(command.GetArg(2), out var hp))
        {
            var targets = _sharedApi!.GetTarget(command);
            if (targets == null) return;

            var playersToTarget = targets.Players.Where(player =>
                player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

            playersToTarget.ForEach(player =>
            {
                if (caller!.CanTarget(player))
                {
                    player.SetHp(hp);
                    LogAndShowActivity(caller, player, "fun_admin_hp_message", "css_hp", hp.ToString());
                }
            });
        }
    }

    [CommandHelper(2, "<#userid or name> <speed>")]
    [RequiresPermissions("@css/slay")]
    private void OnSetSpeedCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var speed))
        {
            var targets = _sharedApi!.GetTarget(command);
            if (targets == null) return;

            var playersToTarget = targets.Players.Where(player =>
                player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

            playersToTarget.ForEach(player =>
            {
                if (caller!.CanTarget(player))
                {
                    player.SetSpeed(speed);

                    // Track speed modification for timer
                    if (speed == 1f)
                        SpeedPlayers.Remove(player);
                    else
                        SpeedPlayers[player] = speed;

                    LogAndShowActivity(caller, player, "fun_admin_speed_message", "css_speed", speed.ToString(CultureInfo.InvariantCulture));
                }
            });
        }
    }

    [CommandHelper(2, "<#userid or name> <gravity>")]
    [RequiresPermissions("@css/slay")]
    private void OnSetGravityCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var gravity))
        {
            var targets = _sharedApi!.GetTarget(command);
            if (targets == null) return;

            var playersToTarget = targets.Players.Where(player =>
                player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

            playersToTarget.ForEach(player =>
            {
                if (caller!.CanTarget(player))
                {
                    player.SetGravity(gravity);

                    // Track gravity modification for timer
                    if (gravity == 1f)
                        GravityPlayers.Remove(player);
                    else
                        GravityPlayers[player] = gravity;

                    LogAndShowActivity(caller, player, "fun_admin_gravity_message", "css_gravity", gravity.ToString(CultureInfo.InvariantCulture));
                }
            });
        }
    }

    [CommandHelper(2, "<#userid or name> <money>")]
    [RequiresPermissions("@css/slay")]
    private void OnSetMoneyCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (int.TryParse(command.GetArg(2), out var money))
        {
            var targets = _sharedApi!.GetTarget(command);
            if (targets == null) return;

            var playersToTarget = targets.Players.Where(player =>
                player is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected }).ToList();

            playersToTarget.ForEach(player =>
            {
                if (caller!.CanTarget(player))
                {
                    player.SetMoney(money);
                    LogAndShowActivity(caller, player, "fun_admin_money_message", "css_money", money.ToString());
                }
            });
        }
    }

    [CommandHelper(2, "<#userid or name> <size>")]
    [RequiresPermissions("@css/slay")]
    private void OnSetResizeCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
        {
            var targets = _sharedApi!.GetTarget(command);
            if (targets == null) return;

            var playersToTarget = targets.Players.Where(player =>
                player is { IsValid: true, IsHLTV: false, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

            playersToTarget.ForEach(player =>
            {
                if (caller!.CanTarget(player))
                {
                    Resize(caller, player, size);
                }
            });
        }
    }

    // =================================
    // HELPER METHOD FOR ACTIVITIES WITH INDIVIDUAL COMMAND LOGGING
    // =================================

    private void LogAndShowActivity(CCSPlayerController? caller, CCSPlayerController target, string messageKey, string baseCommand, params string[] extraArgs)
    {
        var callerName = caller?.PlayerName ?? "Console";

        // Build activity args
        var args = new List<object> { "CALLER", target.PlayerName };
        args.AddRange(extraArgs);

        // Show admin activity using module's own localizer with per-player language support
        if (caller == null || !_sharedApi!.IsAdminSilent(caller))
        {
            // Use module's own translations with automatic per-player language support
            if (Localizer != null)
            {
                // This will send the message in each player's configured language
                _sharedApi!.ShowAdminActivityLocalized(Localizer, messageKey, callerName, false, args.ToArray());
            }
            else
            {
                // Fallback to old method if localizer is not available
                _sharedApi!.ShowAdminActivity(messageKey, callerName, false, args.ToArray());
            }
        }

        // Build and log command using API string method
        var logCommand = $"{baseCommand} {(string.IsNullOrEmpty(target.PlayerName) ? target.SteamID.ToString() : target.PlayerName)}";
        if (extraArgs.Length > 0)
        {
            logCommand += $" {string.Join(" ", extraArgs)}";
        }

        _sharedApi!.LogCommand(caller, logCommand);
    }
}