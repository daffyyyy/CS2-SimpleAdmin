using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
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
		return AdminManager.CanPlayerTarget(controller, target);
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

		var weaponServices = controller.PlayerPawn.Value!.WeaponServices;
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
	}

	public static void Unfreeze(this CBasePlayerPawn pawn)
	{
		pawn.MoveType = MoveType_t.MOVETYPE_WALK;
	}

	public static void ToggleNoclip(this CBasePlayerPawn pawn)
	{
		if (pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP)
			pawn.MoveType = MoveType_t.MOVETYPE_WALK;
		else
			pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
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