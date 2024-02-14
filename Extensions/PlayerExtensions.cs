using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using System.Text;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace CS2_SimpleAdmin;

public static class PlayerExtensions
{
	public static void Slap(this CBasePlayerPawn pawn, int damage = 0)
	{
		PerformSlap(pawn, damage);
	}

	public static void Print(this CCSPlayerController controller, string message = "")
	{
		StringBuilder _message = new(CS2_SimpleAdmin._localizer!["sa_prefix"]);
		_message.Append(message);
		controller.PrintToChat(_message.ToString());
	}

	public static bool CanTarget(this CCSPlayerController controller, CCSPlayerController target)
	{
		if (target.IsBot) return true;
		if (controller is null) return true;

		return AdminManager.CanPlayerTarget(controller, target) || AdminManager.CanPlayerTarget(new SteamID(controller.SteamID), new SteamID(target.SteamID));
	}

	public static void SetSpeed(this CCSPlayerController controller, float speed)
	{
		CCSPlayerPawn? playerPawnValue = controller.PlayerPawn.Value;
		if (playerPawnValue == null) return;

		playerPawnValue.VelocityModifier = speed;
	}

	public static void SetHp(this CCSPlayerController controller, int health = 100)
	{
		if (health <= 0 || !controller.PawnIsAlive || controller.PlayerPawn.Value == null) return;

		controller.PlayerPawn.Value.Health = health;

		if (health > 100)
		{
			controller.PlayerPawn.Value.MaxHealth = health;
		}

		Utilities.SetStateChanged(controller.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
	}

	public static void Bury(this CBasePlayerPawn pawn, float depth = 10f)
	{
		var newPos = new Vector(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y,
			pawn.AbsOrigin!.Z - depth);

		pawn.Teleport(newPos, pawn.AbsRotation!, pawn.AbsVelocity);
	}

	public static void Unbury(this CBasePlayerPawn pawn, float depth = 15f)
	{
		var newPos = new Vector(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y,
			pawn.AbsOrigin!.Z + depth);

		pawn.Teleport(newPos, pawn.AbsRotation!, pawn.AbsVelocity);
	}

	public static void Freeze(this CBasePlayerPawn pawn)
	{
		pawn.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
		Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 1); // obsolete
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
	}

	public static void Unfreeze(this CBasePlayerPawn pawn)
	{
		pawn.MoveType = MoveType_t.MOVETYPE_WALK;
		Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
	}

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

	public static void Rename(this CCSPlayerController controller, string newName = "Unknown")
	{
		if (CS2_SimpleAdmin.Instance == null)
			return;

		SchemaString<CBasePlayerController> playerName = new SchemaString<CBasePlayerController>(controller, "m_iszPlayerName");
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

		CS2_SimpleAdmin.Instance.AddTimer(0.4f, () =>
		{
			Utilities.SetStateChanged(controller, "CBasePlayerController", "m_iszPlayerName");
		});
	}

	public static void TeleportPlayer(this CCSPlayerController controller, CCSPlayerController target)
	{
		if (controller.PlayerPawn?.Value == null && target!.PlayerPawn?.Value == null)
			return;

		if (
			controller?.PlayerPawn?.Value?.AbsOrigin != null &&
			controller?.PlayerPawn?.Value?.AbsRotation != null &&
			target?.PlayerPawn?.Value?.AbsOrigin != null &&
			target?.PlayerPawn?.Value?.AbsRotation != null
		)
		{
			controller.PlayerPawn.Value.Teleport(
				target.PlayerPawn.Value.AbsOrigin,
				target.PlayerPawn.Value.AbsRotation,
				target.PlayerPawn.Value.AbsVelocity
			);
		}
	}

	private static void PerformSlap(CBasePlayerPawn pawn, int damage = 0)
	{
		if (pawn.LifeState != (int)LifeState_t.LIFE_ALIVE)
			return;

		/* Teleport in a random direction - thank you, Mani!*/
		/* Thank you AM & al!*/
		var random = new Random();
		var vel = new Vector(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);

		vel.X += ((random.Next(180) + 50) * ((random.Next(2) == 1) ? -1 : 1));
		vel.Y += ((random.Next(180) + 50) * ((random.Next(2) == 1) ? -1 : 1));
		vel.Z += random.Next(200) + 100;

		pawn.AbsVelocity.X = vel.X;
		pawn.AbsVelocity.Y = vel.Y;
		pawn.AbsVelocity.Z = vel.Z;

		if (damage <= 0)
			return;

		pawn.Health -= damage;
		Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

		if (pawn.Health <= 0)
			pawn.CommitSuicide(true, true);
	}
}