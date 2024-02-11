using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

namespace CS2_SimpleAdmin;

public static class PlayerUtils
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

		controller.Health = health;
		controller.PlayerPawn.Value.Health = health;

		if (health > 100)
		{
			controller.MaxHealth = health;
			controller.PlayerPawn.Value.MaxHealth = health;
		}

		CPlayer_WeaponServices? weaponServices = controller.PlayerPawn.Value!.WeaponServices;
		if (weaponServices == null) return;

		controller.GiveNamedItem("weapon_healthshot");

		foreach (var weapon in weaponServices.MyWeapons)
		{
			if (weapon != null && weapon.IsValid && weapon.Value!.DesignerName == "weapon_healthshot")
			{
				weapon.Value.Remove();
				break;
			}
		}
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
		if (CS2_SimpleAdmin._plugin == null)
			return;

		SchemaString<CBasePlayerController> playerName = new SchemaString<CBasePlayerController>(controller, "m_iszPlayerName");
		playerName.Set(newName + " ");

		CS2_SimpleAdmin._plugin.AddTimer(0.25f, () =>
		{
			Utilities.SetStateChanged(controller, "CCSPlayerController", "m_szClan");
			Utilities.SetStateChanged(controller, "CBasePlayerController", "m_iszPlayerName");
		});

		CS2_SimpleAdmin._plugin.AddTimer(0.3f, () =>
		{
			playerName.Set(newName);
		});

		CS2_SimpleAdmin._plugin.AddTimer(0.4f, () =>
		{
			Utilities.SetStateChanged(controller, "CBasePlayerController", "m_iszPlayerName");
		});
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

		pawn.Teleport(pawn.AbsOrigin!, pawn.AbsRotation!, vel);

		if (damage <= 0)
			return;

		pawn.Health -= damage;

		if (pawn.Health <= 0)
			pawn.CommitSuicide(true, true);
	}
}