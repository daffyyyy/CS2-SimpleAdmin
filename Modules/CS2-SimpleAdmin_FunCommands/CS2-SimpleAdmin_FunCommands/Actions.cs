using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin_FunCommands;

public partial class CS2_SimpleAdmin_FunCommands
{
    private void God(CCSPlayerController? caller, CCSPlayerController player)
    {
        if (!caller.CanTarget(player)) return;

        var callerName = caller?.PlayerName ?? "Console";

        // Toggle god mode for the player (like in main plugin)
        if (!GodPlayers.Add(player.Slot))
        {
            GodPlayers.Remove(player.Slot);
        }

        // Show admin activity using module's own localizer with per-player language support
        var activityArgs = new object[] { "CALLER", player.PlayerName };
        if (caller == null || !_sharedApi!.IsAdminSilent(caller))
        {
            if (Localizer != null)
            {
                _sharedApi!.ShowAdminActivityLocalized(Localizer, "fun_admin_god_message", callerName, false,
                    activityArgs);
            }
            else
            {
                _sharedApi!.ShowAdminActivity("fun_admin_god_message", callerName, false, activityArgs);
            }
        }

        // Log command using API
        _sharedApi!.LogCommand(caller,
            $"css_god {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
    }

    private void NoClip(CCSPlayerController? caller, CCSPlayerController player)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        var callerName = caller?.PlayerName ?? "Console";

        // Toggle no-clip mode using PlayerExtensions
        player.Pawn.Value?.ToggleNoclip();

        // Show admin activity using module's own localizer with per-player language support
        var activityArgs = new object[] { "CALLER", player.PlayerName };
        if (caller == null || !_sharedApi!.IsAdminSilent(caller))
        {
            if (Localizer != null)
            {
                _sharedApi!.ShowAdminActivityLocalized(Localizer, "fun_admin_noclip_message", callerName, false,
                    activityArgs);
            }
            else
            {
                _sharedApi!.ShowAdminActivity("fun_admin_noclip_message", callerName, false, activityArgs);
            }
        }

        // Log command using API
        _sharedApi!.LogCommand(caller,
            $"css_noclip {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
    }

    private void Freeze(CCSPlayerController? caller, CCSPlayerController player, int time)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        var callerName = caller?.PlayerName ?? "Console";

        // Freeze player using PlayerExtensions
        player.Pawn.Value?.Freeze();

        // Show admin activity using module's own localizer with per-player language support
        var activityArgs = new object[] { "CALLER", player.PlayerName };
        if (caller == null || !_sharedApi!.IsAdminSilent(caller))
        {
            if (Localizer != null)
            {
                _sharedApi!.ShowAdminActivityLocalized(Localizer, "fun_admin_freeze_message", callerName, false,
                    activityArgs);
            }
            else
            {
                _sharedApi!.ShowAdminActivity("fun_admin_freeze_message", callerName, false, activityArgs);
            }
        }

        // Auto-unfreeze after duration
        if (time > 0)
        {
            AddTimer(time, () => player.Pawn.Value?.Unfreeze(),
                CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Log command using API
        _sharedApi!.LogCommand(caller,
            $"css_freeze {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {time}");
    }

    private void Unfreeze(CCSPlayerController? caller, CCSPlayerController player)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        var callerName = caller?.PlayerName ?? "Console";

        // Unfreeze player using PlayerExtensions
        player.Pawn.Value?.Unfreeze();

        // Show admin activity using module's own localizer with per-player language support
        var activityArgs = new object[] { "CALLER", player.PlayerName };
        if (caller == null || !_sharedApi!.IsAdminSilent(caller))
        {
            if (Localizer != null)
            {
                _sharedApi!.ShowAdminActivityLocalized(Localizer, "fun_admin_unfreeze_message", callerName, false,
                    activityArgs);
            }
            else
            {
                _sharedApi!.ShowAdminActivity("fun_admin_unfreeze_message", callerName, false, activityArgs);
            }
        }

        // Log command using API
        _sharedApi!.LogCommand(caller,
            $"css_unfreeze {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
    }

    /// <summary>
    /// Respawns a player and teleports them to their last death position if available.
    /// This demonstrates using the GetPlayerInfo API to access player data.
    /// </summary>
    private void Respawn(CCSPlayerController? caller, CCSPlayerController player)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        var callerName = caller?.PlayerName ?? "Console";

        // Respawn the player
        player.Respawn();

        // Get death position from API and teleport player to it
        // BEST PRACTICE: Use API to access player data like death position
        if (_sharedApi != null && player.UserId.HasValue)
        {
            try
            {
                var playerInfo = _sharedApi.GetPlayerInfo(player);

                // Teleport to death position if available
                if (playerInfo?.DiePosition != null && player.PlayerPawn?.Value != null)
                {
                    player.PlayerPawn.Value.Teleport(
                        playerInfo.DiePosition.Position,
                        playerInfo.DiePosition.Angle);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to get player info for respawn: {ex.Message}");
            }
        }

        // Show admin activity using module's own localizer with per-player language support
        var activityArgs = new object[] { "CALLER", player.PlayerName };
        if (caller == null || !_sharedApi!.IsAdminSilent(caller))
        {
            if (Localizer != null)
            {
                _sharedApi!.ShowAdminActivityLocalized(Localizer, "fun_admin_respawn_message", callerName, false,
                    activityArgs);
            }
            else
            {
                _sharedApi!.ShowAdminActivity("fun_admin_respawn_message", callerName, false, activityArgs);
            }
        }

        // Log command using API
        _sharedApi!.LogCommand(caller,
            $"css_respawn {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
    }

    /// <summary>
    /// Resizes a player's model to the specified scale.
    /// </summary>
    private void Resize(CCSPlayerController? caller, CCSPlayerController player, float size)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        var callerName = caller?.PlayerName ?? "Console";

        // Resize the player
        var sceneNode = player.PlayerPawn.Value!.CBodyComponent?.SceneNode;
        if (sceneNode != null)
        {
            sceneNode.GetSkeletonInstance().Scale = size;
            player.PlayerPawn.Value.AcceptInput("SetScale", null, null, size.ToString(CultureInfo.InvariantCulture));

            Server.NextWorldUpdate(() =>
            {
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_CBodyComponent");
            });
        }

        // Show admin activity using module's own localizer with per-player language support
        var activityArgs = new object[] { "CALLER", player.PlayerName, size.ToString(CultureInfo.InvariantCulture) };
        if (caller == null || !_sharedApi!.IsAdminSilent(caller))
        {
            if (Localizer != null)
            {
                _sharedApi!.ShowAdminActivityLocalized(Localizer, "fun_admin_resize_message", callerName, false,
                    activityArgs);
            }
            else
            {
                _sharedApi!.ShowAdminActivity("fun_admin_resize_message", callerName, false, activityArgs);
            }
        }

        // Log command using API
        _sharedApi!.LogCommand(caller,
            $"css_resize {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)} {size}");
    }
}