using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace CS2_SimpleAdmin_FunCommands;

public partial class CS2_SimpleAdmin_FunCommands
{
    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        // Check if player has god mode (similar to main plugin)
        if (!GodPlayers.Contains(player.Slot)) return HookResult.Continue;
        
        // Cancel damage
        @event.DmgHealth = 0;
        @event.DmgArmor = 0;

        // Reset health to full
        if (player.PlayerPawn?.Value == null) return HookResult.Continue;
        
        player.PlayerPawn.Value.Health = player.PlayerPawn.Value.MaxHealth;
        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        // Remove player from god mode, speed, and gravity tracking on death
        GodPlayers.Remove(player.Slot);
        SpeedPlayers.Remove(player);
        GravityPlayers.Remove(player);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Clear all fun command modifications at round start
        GodPlayers.Clear();
        SpeedPlayers.Clear();
        GravityPlayers.Clear();

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        // Clean up player from all tracking when they disconnect
        GodPlayers.Remove(player.Slot);
        SpeedPlayers.Remove(player);
        GravityPlayers.Remove(player);

        return HookResult.Continue;
    }
}