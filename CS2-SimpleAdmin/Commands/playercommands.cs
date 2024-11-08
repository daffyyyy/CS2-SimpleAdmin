using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    internal static readonly Dictionary<int, float> SpeedPlayers = [];
    internal static readonly Dictionary<CCSPlayerController, float> GravityPlayers = [];
    
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSlayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            Slay(caller, player, callerName, command);
        });
    }

    internal static void Slay(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
    {
        if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected) return;
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Make the player commit suicide
        player.CommitSuicide(false, true);

        // Determine message keys and arguments for the slay notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_slay_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }

        // Log the command and send Discord notification
        if (command == null)
            Helper.LogCommand(caller, $"css_slay {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
        else
            Helper.LogCommand(caller, command);
    }

    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, usage: "<#userid or name> <weapon>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGiveCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();
        var weaponName = command.GetArg(2);

        // check if item is typed
        // if (weaponName.Length < 2)
        // {
        //     command.ReplyToCommand($"No weapon typed.");
        //     return;
        // }

        // check if weapon is knife
        if (weaponName.Contains("_knife") || weaponName.Contains("bayonet"))
        {
            if (CoreConfig.FollowCS2ServerGuidelines)
            {
                command.ReplyToCommand($"Cannot Give {weaponName} because it's illegal to be given.");
                return;
            }
        }

        playersToTarget.ForEach(player =>
        {
            if (player.Connected != PlayerConnectedState.PlayerConnected)
                return;

            GiveWeapon(caller, player, weaponName, callerName, command);
        });
    }

    private static void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";
        var weapons = WeaponHelper.GetWeaponsByPartialName(weaponName);

        switch (weapons.Count)
        {
            case 0:
                return;
            case > 1:
            {
                var weaponList = string.Join(", ", weapons.Select(w => w.EnumMemberValue));
                command?.ReplyToCommand($"Found weapons with a similar name: {weaponList}");
                return;
            }
        }

        // Give weapon to the player
        player.GiveNamedItem(weapons.First().EnumValue);

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_giveweapon {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {weaponName}");
        else
            Helper.LogCommand(caller, command);

        // Determine message keys and arguments for the weapon give notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_give_message",
                new object[] { "CALLER", player.PlayerName, weaponName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }
    }

    internal static void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, CsItem weapon, string? callerName = null, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Give weapon to the player
        player.GiveNamedItem(weapon);

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_giveweapon {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {weapon.ToString()}");
        else
            Helper.LogCommand(caller, command);

        // Determine message keys and arguments for the weapon give notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_give_message",
                new object[] { "CALLER", player.PlayerName, weapon.ToString() });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }
    }

    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnStripCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                StripWeapons(caller, player, callerName, command);
            }
        });
    }

    internal static void StripWeapons(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Check if player is valid, alive, and connected
        if (!player.IsValid || !player.PawnIsAlive || player.Connected != PlayerConnectedState.PlayerConnected)
            return;

        // Strip weapons from the player
        player.RemoveWeapons();

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_strip {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
        else
            Helper.LogCommand(caller, command);

        // Determine message keys and arguments for the weapon strip notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_strip_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }
    }

    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> <health>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnHpCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int.TryParse(command.GetArg(2), out var health);

        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                SetHp(caller, player, health, command);
            }
        });
    }

    internal static void SetHp(CCSPlayerController? caller, CCSPlayerController player, int health, CommandInfo? command = null)
    {
        if (!player.IsValid || player.IsHLTV) return;
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Set player's health
        player.SetHp(health);

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_hp {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {health}");
        else
            Helper.LogCommand(caller, command);

        // Determine message keys and arguments for the HP set notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_hp_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }
    }

    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> <speed>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSpeedCommand(CCSPlayerController? caller, CommandInfo command)
    {
        float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var speed);

        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (player.Connected != PlayerConnectedState.PlayerConnected)
                return;

            if (caller!.CanTarget(player))
            {
                SetSpeed(caller, player, speed, command);
            }
        });
    }

    internal static void SetSpeed(CCSPlayerController? caller, CCSPlayerController player, float speed, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Set player's speed
        player.SetSpeed(speed);
        
        if (speed == 1f)
            SpeedPlayers.Remove(player.Slot);
        else
            SpeedPlayers[player.Slot] = speed;

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_speed {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {speed}");
        else
            Helper.LogCommand(caller, command);

        // Determine message keys and arguments for the speed set notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_speed_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }
    }

    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> <gravity>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGravityCommand(CCSPlayerController? caller, CommandInfo command)
    {
        float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var gravity);
        
        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (player.Connected != PlayerConnectedState.PlayerConnected)
                return;

            if (caller!.CanTarget(player))
            {
                SetGravity(caller, player, gravity, command);
            }
        });
    }

    internal static void SetGravity(CCSPlayerController? caller, CCSPlayerController player, float gravity, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Set player's gravity
        player.SetGravity(gravity);
        
        if (gravity == 1f)
            GravityPlayers.Remove(player);
        else
            GravityPlayers[player] = gravity;
        
        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_gravity {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {gravity}");
        else
            Helper.LogCommand(caller, command);

        // Determine message keys and arguments for the gravity set notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_gravity_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }
    }

    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> <money>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnMoneyCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        int.TryParse(command.GetArg(2), out var money);

        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (player.Connected != PlayerConnectedState.PlayerConnected)
                return;

            if (caller!.CanTarget(player))
            {
                SetMoney(caller, player, money, command);
            }
        });
    }

    internal static void SetMoney(CCSPlayerController? caller, CCSPlayerController player, int money, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Set player's money
        player.SetMoney(money);

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_money {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {money}");
        else
            Helper.LogCommand(caller, command);

        // Determine message keys and arguments for the money set notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_money_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }
    }

    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<#userid or name> [damage]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSlapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var damage = 0;

        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

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
    }

    internal static void Slap(CCSPlayerController? caller, CCSPlayerController player, int damage, CommandInfo? command = null)
    {
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";
        
        // Apply slap damage to the player
        player.Pawn.Value?.Slap(damage);

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_slap {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {damage}");
        else
            Helper.LogCommand(caller, command);

        // Determine message key and arguments for the slap notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_slap_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

        if (_localizer != null)
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
        }
    }

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
    }

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
            if (player.PawnIsAlive && teamNum != CsTeam.Spectator && !kill && Instance.Config.OtherSettings.TeamSwitchType == 1)
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
                if (player.PawnIsAlive && !kill && Instance.Config.OtherSettings.TeamSwitchType == 1)
                    player.SwitchTeam(_teamNum);
                else
                    player.ChangeTeam(_teamNum);
            }
        }

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_team {player.PlayerName} {teamName}");
        else
            Helper.LogCommand(caller, command);

        // Determine message key and arguments for the team change notification
        var activityMessageKey = "sa_admin_team_message";
        var adminActivityArgs = new object[] { "CALLER", player.PlayerName, teamName };

        // Display admin activity message to other players
        if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

        Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
    }

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

            Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
            
            // Rename the player
            player.Rename(newName);
        });
    }

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
                Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
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

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/cheats")]
    public void OnRespawnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (player.Connected != PlayerConnectedState.PlayerConnected)
                return;

            if (caller!.CanTarget(player))
            {
                Respawn(caller, player, callerName, command);
            }
        });
    }

    internal static void Respawn(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
    {
        // Check if the caller can target the player
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Ensure the player's pawn is valid before attempting to respawn
        if (_cBasePlayerControllerSetPawnFunc == null || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

        // Perform the respawn operation
        var playerPawn = player.PlayerPawn.Value;
        _cBasePlayerControllerSetPawnFunc.Invoke(player, playerPawn, true, false);
        VirtualFunction.CreateVoid<CCSPlayerController>(player.Handle, GameData.GetOffset("CCSPlayerController_Respawn"))(player);

        if (player.UserId.HasValue && PlayersInfo.TryGetValue(player.UserId.Value, out var value) && value.DiePosition != null)
            playerPawn.Teleport(value.DiePosition?.Position, value.DiePosition?.Angle);

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_respawn {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
        else
            Helper.LogCommand(caller, command);

        // Determine message key and arguments for the respawn notification
        var activityMessageKey = "sa_admin_respawn_message";
        var adminActivityArgs = new object[] { "CALLER", player.PlayerName };

        // Display admin activity message to other players
        if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

        Helper.ShowAdminActivity(activityMessageKey, callerName, adminActivityArgs);
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/kick")]
    public void OnGotoCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Check if the caller is valid and has a live pawn
        if (caller == null || !caller.PawnIsAlive) return;

        // Get the target players
        var targets = GetTarget(command);
        if (targets == null || targets.Count() > 1) return;

        var playersToTarget = targets.Players
            .Where(player => player is { IsValid: true, IsHLTV: false })
            .ToList();

        // Log the command
        Helper.LogCommand(caller, command);

        // Process each player to teleport
        foreach (var player in playersToTarget.Where(player => player is { Connected: PlayerConnectedState.PlayerConnected, PawnIsAlive: true }).Where(caller.CanTarget))
        {
            if (caller.PlayerPawn.Value == null)
                continue;

            // Teleport the caller to the player and toggle noclip
            caller.TeleportPlayer(player);
            caller.PlayerPawn.Value.ToggleNoclip();

            // Set a timer to toggle noclip back after 3 seconds
            AddTimer(3, () => caller.PlayerPawn.Value.ToggleNoclip());

            // Prepare message key and arguments for the teleport notification
            var activityMessageKey = "sa_admin_tp_message";
            var adminActivityArgs = new object[] { "CALLER", player.PlayerName };

            // Show admin activity
            if (!SilentPlayers.Contains(caller.Slot) && _localizer != null)
            {
                Helper.ShowAdminActivity(activityMessageKey, caller.PlayerName, adminActivityArgs);
            }
        }
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/kick")]
    public void OnBringCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Check if the caller is valid and has a live pawn
        if (caller == null || !caller.PawnIsAlive) return;

        // Get the target players
        var targets = GetTarget(command);
        if (targets == null || targets.Count() > 1) return;

        var playersToTarget = targets.Players
            .Where(player => player is { IsValid: true, IsHLTV: false })
            .ToList();

        // Log the command
        Helper.LogCommand(caller, command);

        // Process each player to teleport
        foreach (var player in playersToTarget.Where(player => player is { Connected: PlayerConnectedState.PlayerConnected, PawnIsAlive: true }).Where(caller.CanTarget))
        {
            if (caller.PlayerPawn.Value == null)
                continue;

            // Teleport the player to the caller and toggle noclip
            player.TeleportPlayer(caller);
            caller.PlayerPawn.Value.ToggleNoclip();

            // Set a timer to toggle noclip back after 3 seconds
            AddTimer(3, () => caller.PlayerPawn.Value.ToggleNoclip());

            // Prepare message key and arguments for the bring notification
            var activityMessageKey = "sa_admin_bring_message";
            var adminActivityArgs = new object[] { "CALLER", player.PlayerName };

            // Show admin activity
            if (!SilentPlayers.Contains(caller.Slot) && _localizer != null)
            {
                Helper.ShowAdminActivity(activityMessageKey, caller.PlayerName, adminActivityArgs);
            }
        }
    }
}