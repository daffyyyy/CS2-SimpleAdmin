using System.Drawing;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using System.Text;
using CounterStrikeSharp.API.Modules.UserMessages;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace CS2_SimpleAdmin;

public static class PlayerExtensions
{
    /// <summary>
    /// Slaps the player pawn by applying optional damage and adding a random velocity knockback.
    /// </summary>
    /// <param name="pawn">The player pawn to slap.</param>
    /// <param name="damage">The amount of damage to apply (default is 0).</param>
    public static void Slap(this CBasePlayerPawn pawn, int damage = 0)
    {
        PerformSlap(pawn, damage);
    }

    /// <summary>
    /// Prints a localized chat message to the player with a prefix.
    /// </summary>
    /// <param name="controller">The player controller to send the message to.</param>
    /// <param name="message">The message string.</param>
    public static void Print(this CCSPlayerController controller, string message = "")
    {
        StringBuilder _message = new(CS2_SimpleAdmin._localizer!["sa_prefix"]);
        _message.Append(message);
        controller.PrintToChat(_message.ToString());
    }

    /// <summary>
    /// Determines if the player controller can target another player controller, respecting admin permissions and immunity.
    /// </summary>
    /// <param name="controller">The player controller who wants to target.</param>
    /// <param name="target">The player controller being targeted.</param>
    /// <returns>True if targeting is allowed, false otherwise.</returns>
    public static bool CanTarget(this CCSPlayerController? controller, CCSPlayerController? target)
    {
        if (controller is null || target is null) return true;
        if (target.IsBot) return true;

        return AdminManager.CanPlayerTarget(controller, target) ||
                                  AdminManager.CanPlayerTarget(new SteamID(controller.SteamID),
                                      new SteamID(target.SteamID)) || 
                                      AdminManager.GetPlayerImmunity(controller) >= AdminManager.GetPlayerImmunity(target);
    }

    /// <summary>
    /// Checks if the controller can target a player by SteamID, considering targeting permissions and immunities.
    /// </summary>
    /// <param name="controller">The attacker player controller.</param>
    /// <param name="steamId">The SteamID of the target player.</param>
    /// <returns>True if targeting is permitted, false otherwise.</returns>
    public static bool CanTarget(this CCSPlayerController? controller, SteamID steamId)
    {
        if (controller is null) return true;

        return AdminManager.CanPlayerTarget(new SteamID(controller.SteamID), steamId) || 
               AdminManager.GetPlayerImmunity(controller) >= AdminManager.GetPlayerImmunity(steamId);
    }

    /// <summary>
    /// Sets the movement speed modifier of the player controller.
    /// </summary>
    /// <param name="controller">The player controller.</param>
    /// <param name="speed">The speed modifier value.</param>
    public static void SetSpeed(this CCSPlayerController? controller, float speed)
    {
        var playerPawnValue = controller?.PlayerPawn.Value;
        if (playerPawnValue == null) return;

        playerPawnValue.VelocityModifier = speed;
    }

    /// <summary>
    /// Sets the gravity scale for the player controller.
    /// </summary>
    /// <param name="controller">The player controller.</param>
    /// <param name="gravity">The gravity scale.</param>
    public static void SetGravity(this CCSPlayerController? controller, float gravity)
    {
        var playerPawnValue = controller?.PlayerPawn.Value;
        if (playerPawnValue == null) return;

        playerPawnValue.ActualGravityScale = gravity;
    }

    /// <summary>
    /// Sets the player's in-game money amount.
    /// </summary>
    /// <param name="controller">The player controller.</param>
    /// <param name="money">The amount of money to set.</param>
    public static void SetMoney(this CCSPlayerController? controller, int money)
    {
        var moneyServices = controller?.InGameMoneyServices;
        if (moneyServices == null) return;

        moneyServices.Account = money;

        if (controller != null) Utilities.SetStateChanged(controller, "CCSPlayerController", "m_pInGameMoneyServices");
    }

    /// <summary>
    /// Sets the player's health points.
    /// </summary>
    /// <param name="controller">The player controller.</param>
    /// <param name="health">The health value, default is 100.</param>
    public static void SetHp(this CCSPlayerController? controller, int health = 100)
    {
        if (controller == null) return;
        if (health <= 0 || controller.PlayerPawn.Value == null ||  controller.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE) return;

        controller.PlayerPawn.Value.Health = health;

        if (health > 100)
        {
            controller.PlayerPawn.Value.MaxHealth = health;
        }

        Utilities.SetStateChanged(controller.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
    }

    /// <summary>
    /// Buries the player pawn by moving it down by a depth offset.
    /// </summary>
    /// <param name="pawn">The player pawn to bury.</param>
    /// <param name="depth">The depth offset (default 10 units).</param>
    public static void Bury(this CBasePlayerPawn pawn, float depth = 10f)
    {
        var newPos = new Vector3(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y,
            pawn.AbsOrigin!.Z - depth);
        var newRotation = new Vector3(pawn.AbsRotation!.X, pawn.AbsRotation.Y, pawn.AbsRotation.Z);
        var newVelocity = new Vector3(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);

        pawn.Teleport(newPos, newRotation, newVelocity);
    }

    /// <summary>
    /// Unburies the player pawn by moving it up by a depth offset.
    /// </summary>
    /// <param name="pawn">The player pawn to unbury.</param>
    /// <param name="depth">The depth offset (default 15 units).</param>
    public static void Unbury(this CBasePlayerPawn pawn, float depth = 15f)
    {
        var newPos = new Vector3(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y,
            pawn.AbsOrigin!.Z + depth);
        var newRotation = new Vector3(pawn.AbsRotation!.X, pawn.AbsRotation.Y, pawn.AbsRotation.Z);
        var newVelocity = new Vector3(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);

        pawn.Teleport(newPos, newRotation, newVelocity);
    }

    /// <summary>
    /// Freezes the player pawn, disabling movement.
    /// </summary>
    /// <param name="pawn">The player pawn to freeze.</param>
    public static void Freeze(this CBasePlayerPawn pawn)
    {
        pawn.MoveType = MoveType_t.MOVETYPE_INVALID;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 11); // invalid
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }
    
    /// <summary>
    /// Unfreezes the player pawn, enabling movement.
    /// </summary>
    /// <param name="pawn">The player pawn to unfreeze.</param>
    public static void Unfreeze(this CBasePlayerPawn pawn)
    {
        pawn.MoveType = MoveType_t.MOVETYPE_WALK;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }
    
    /// <summary>
    /// Changes the player's color tint to specified RGBA values.
    /// </summary>
    /// <param name="pawn">The pawn to colorize.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha (transparency) component (0-255).</param>
    public static void Colorize(this CBasePlayerPawn pawn, int r = 255, int g = 255, int b = 255, int a = 255)
    {
        pawn.Render = Color.FromArgb(a, r, g, b);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    /// <summary>
    /// Toggles noclip mode for the player pawn.
    /// </summary>
    /// <param name="pawn">The player pawn.</param>
    public static void ToggleNoclip(this CBasePlayerPawn pawn)
    {
        if (pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP)
        {
            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
        else
        {
            pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 8); // noclip
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
    }

    /// <summary>
    /// Renames the player controller to a new name, with fallback to a localized "Unknown".
    /// </summary>
    /// <param name="controller">The player controller to rename.</param>
    /// <param name="newName">The new name to assign.</param>
    public static void Rename(this CCSPlayerController? controller, string newName = "Unknown")
    {
        newName ??= CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";

        if (controller != null)
        {
            var playerName = new SchemaString<CBasePlayerController>(controller, "m_iszPlayerName");
            playerName.Set(newName + " ");

            CS2_SimpleAdmin.Instance.AddTimer(0.25f, () =>
            {
                Utilities.SetStateChanged(controller, "CCSPlayerController", "m_szClan");
                Utilities.SetStateChanged(controller, "CBasePlayerController", "m_iszPlayerName");
            });

            CS2_SimpleAdmin.Instance.AddTimer(0.3f, () =>
            {
                playerName.Set(newName);
            });
        }

        CS2_SimpleAdmin.Instance.AddTimer(0.4f, () =>
        {
            if (controller != null) Utilities.SetStateChanged(controller, "CBasePlayerController", "m_iszPlayerName");
        });
    }

    /// <summary>
    /// Teleports a player controller to the position, rotation, and velocity of another player controller.
    /// </summary>
    /// <param name="controller">The controller to teleport.</param>
    /// <param name="target">The target controller whose position to copy.</param>
    public static void TeleportPlayer(this CCSPlayerController? controller, CCSPlayerController? target)
    {
        if (controller?.PlayerPawn.Value == null && target?.PlayerPawn.Value == null)
            return;

        if (
            controller?.PlayerPawn.Value is { AbsOrigin: not null, AbsRotation: not null } &&
            target?.PlayerPawn.Value is { AbsOrigin: not null, AbsRotation: not null }
        )
        {
            controller.PlayerPawn.Value.Teleport(
                target.PlayerPawn.Value.AbsOrigin,
                target.PlayerPawn.Value.AbsRotation,
                target.PlayerPawn.Value.AbsVelocity
            );
        }
    }

    /// <summary>
    /// Applies a slap effect to the given player pawn, optionally inflicting damage and adding velocity knockback.
    /// </summary>
    /// <param name="pawn">The player pawn to slap.</param>
    /// <param name="damage">The amount of damage to deal (default is 0).</param>
    private static void PerformSlap(CBasePlayerPawn pawn, int damage = 0)
    {
        if (pawn.LifeState != (int)LifeState_t.LIFE_ALIVE)
            return;

        var controller = pawn.Controller.Value?.As<CCSPlayerController>();

        /* Teleport in a random direction - thank you, Mani!*/
        /* Thank you AM & al!*/
        var random = new Random();
        var vel = new Vector3(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);

        vel.X += (random.Next(180) + 50) * (random.Next(2) == 1 ? -1 : 1);
        vel.Y += (random.Next(180) + 50) * (random.Next(2) == 1 ? -1 : 1);
        vel.Z += random.Next(200) + 100;

        pawn.AbsVelocity.X = vel.X;
        pawn.AbsVelocity.Y = vel.Y;
        pawn.AbsVelocity.Z = vel.Z;

        if (controller != null && controller.IsValid)
        {
            var shakeMessage = UserMessage.FromPartialName("Shake");
            shakeMessage.SetFloat("duration", 1);
            shakeMessage.SetFloat("amplitude", 10);
            shakeMessage.SetFloat("frequency", 1f);
            shakeMessage.SetInt("command", 0);
            shakeMessage.Recipients.Add(controller);
            shakeMessage.Send();
        }

        if (damage <= 0)
            return;

        pawn.Health -= damage;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        if (pawn.Health <= 0)
            pawn.CommitSuicide(true, true);
    }

    /// <summary>
    /// Sends a localized chat message to the player controller.
    /// The message is retrieved from the specified localizer using the given message key and optional formatting arguments.
    /// Each line of the message is prefixed with a localized prefix string.
    /// The message respects the player's configured language for proper localization.
    /// </summary>
    /// <param name="controller">The target player controller to receive the message.</param>
    /// <param name="localizer">The string localizer used for localization.</param>
    /// <param name="messageKey">The key identifying the localized message.</param>
    /// <param name="messageArgs">Optional arguments to format the localized message.</param>
    public static void SendLocalizedMessage(this CCSPlayerController? controller, IStringLocalizer? localizer,
        string messageKey, params object[] messageArgs)
    {
        if (controller == null || localizer == null) return;

        using (new WithTemporaryCulture(controller.GetLanguage()))
        {
            StringBuilder sb = new();
            sb.Append(localizer[messageKey, messageArgs]);

            foreach (var part in Helper.SeparateLines(sb.ToString()))
            {
                var lineWithPrefix = localizer["sa_prefix"] + part.Trim();
                controller.PrintToChat(lineWithPrefix);
            }
        }
    }

    /// <summary>
    /// Sends a localized chat message to the player controller, centered horizontally on the player's screen.
    /// The message is retrieved from the specified localizer using the given message key and optional formatting arguments.
    /// Each line of the message is centered and prefixed with a localized prefix string.
    /// The message respects the player's configured language for localization.
    /// </summary>
    /// <param name="controller">The target player controller to receive the message.</param>
    /// <param name="localizer">The string localizer used for localization.</param>
    /// <param name="messageKey">The key identifying the localized message.</param>
    /// <param name="messageArgs">Optional arguments to format the localized message.</param>
    public static void SendLocalizedMessageCenter(this CCSPlayerController? controller, IStringLocalizer? localizer,
        string messageKey, params object[] messageArgs)
    {
        if (controller == null || localizer == null) return;

        using (new WithTemporaryCulture(controller.GetLanguage()))
        {
            StringBuilder sb = new();
            sb.Append(localizer[messageKey, messageArgs]);

            foreach (var part in Helper.SeparateLines(sb.ToString()))
            {
                string _part;
                _part = Helper.CenterMessage(part);
                var lineWithPrefix = localizer["sa_prefix"] + _part;
                controller.PrintToChat(lineWithPrefix);
            }
        }
    }
}