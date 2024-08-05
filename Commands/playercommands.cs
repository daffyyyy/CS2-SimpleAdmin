using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		[ConsoleCommand("css_slay")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnSlayCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var targets = GetTarget(command);
			if (targets == null) return;

			var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				Slay(caller, player, callerName, command);
			});
		}

		public void Slay(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null, CommandInfo? command = null)
		{
			if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
				return;
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player?.CommitSuicide(false, true);

			Helper.LogCommand(caller, $"css_slay {player?.PlayerName}");

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_slay_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_give")]
		[RequiresPermissions("@css/cheats")]
		[CommandHelper(minArgs: 2, usage: "<#userid or name> <weapon>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGiveCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var targets = GetTarget(command);
			if (targets == null) return;

			var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();
			var weaponName = command.GetArg(2);

			// check if item is typed
			if (weaponName.Length < 5)
			{
				command.ReplyToCommand($"No weapon typed.");
				return;
			}

			// check if item is valid
			if (!weaponName.Contains("weapon_") && !weaponName.Contains("item_"))
			{
				command.ReplyToCommand($"{weaponName} is not a valid item.");
				return;
			}

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

		public void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, CsItem weapon, string? callerName = null)
		{
			if (!caller.CanTarget(player)) return;

			Helper.LogCommand(caller, $"css_give {player.PlayerName} {weapon.ToString()}");

			player.GiveNamedItem(weapon);
			SubGiveWeapon(caller, player, weapon.ToString(), callerName);
		}

		private void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null, CommandInfo? command = null)
		{
			if (!caller.CanTarget(player)) return;

			Helper.LogCommand(caller, $"css_give {player.PlayerName} {weaponName}");

			player.GiveNamedItem(weaponName);
			SubGiveWeapon(caller, player, weaponName, callerName);
		}

		private void SubGiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null)
		{
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (caller != null && (SilentPlayers.Contains(caller.Slot))) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_give_message",
										callerName,
										player?.PlayerName ?? string.Empty,
										weaponName);
			}
		}

		[ConsoleCommand("css_strip")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnStripCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
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

		public void StripWeapons(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null, CommandInfo? command = null)
		{
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (player == null || !player.IsValid || !player.PawnIsAlive || player.Connected != PlayerConnectedState.PlayerConnected)
				return;

			player.RemoveWeapons();

			Helper.LogCommand(caller, $"css_strip {player.PlayerName}");

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_strip_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_hp")]
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

		public void SetHp(CCSPlayerController? caller, CCSPlayerController? player, int health, CommandInfo? command = null)
		{
			if (player == null || !player.IsValid || player.IsHLTV)
				return;

			if (!caller.CanTarget(player)) return;

			var callerName = caller == null ? "Console" : caller.PlayerName;

			player.SetHp(health);

			Helper.LogCommand(caller, $"css_hp {player.PlayerName} {health}");

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_hp_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_speed")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <speed>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnSpeedCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			double.TryParse(command.GetArg(2), out var speed);

			var targets = GetTarget(command);
			if (targets == null) return;

			var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (player.Connected != PlayerConnectedState.PlayerConnected)
					return;

				if (caller!.CanTarget(player))
				{
					SetSpeed(caller, player, speed, callerName, command);
				}
			});
		}

		public void SetSpeed(CCSPlayerController? caller, CCSPlayerController? player, double speed, string? callerName = null, CommandInfo? command = null)
		{
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetSpeed((float)speed);

			Helper.LogCommand(caller, $"css_speed {player?.PlayerName} {speed}");

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_speed_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_gravity")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <gravity>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGravityCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			double.TryParse(command.GetArg(2), out var gravity);

			var targets = GetTarget(command);
			if (targets == null) return;
			
			var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (player.Connected != PlayerConnectedState.PlayerConnected)
					return;

				if (caller!.CanTarget(player))
				{
					SetGravity(caller, player, gravity, callerName, command);
				}
			});
		}

		public void SetGravity(CCSPlayerController? caller, CCSPlayerController? player, double gravity, string? callerName = null, CommandInfo? command = null)
		{
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetGravity((float)gravity);

			Helper.LogCommand(caller, $"css_gravity {player?.PlayerName} {gravity}");

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_gravity_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_money")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <money>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnMoneyCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
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
					SetMoney(caller, player, money, callerName, command);
				}
			});
		}

		public void SetMoney(CCSPlayerController? caller, CCSPlayerController? player, int money, string? callerName = null, CommandInfo? command = null)
		{
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetMoney(money);

			Helper.LogCommand(caller, $"css_money {player?.PlayerName} {money}");

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_money_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_god")]
		[RequiresPermissions("@css/cheats")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGodCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var targets = GetTarget(command);
			if (targets == null) return;

			var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (player.Connected != PlayerConnectedState.PlayerConnected)
					return;

				if (caller!.CanTarget(player))
				{
					God(caller, player, callerName, command);
				}
			});
		}

		public void God(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null, CommandInfo? command = null)
		{
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (player == null) return;
			
			Helper.LogCommand(caller, $"css_god {player.PlayerName}");

			if (!GodPlayers.Add(player.Slot))
			{
				GodPlayers.Remove(player.Slot);
			}

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_god_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_slap")]
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

		public void Slap(CCSPlayerController? caller, CCSPlayerController? player, int damage, CommandInfo? command = null)
		{
			if (!caller.CanTarget(player)) return;

			var callerName = caller == null ? "Console" : caller.PlayerName;
			player!.Pawn.Value!.Slap(damage);

			Helper.LogCommand(caller, $"css_slap {player?.PlayerName} {damage}");

			if (_localizer == null)
				return;

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_slap_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_team")]
		[RequiresPermissions("@css/kick")]
		[CommandHelper(minArgs: 2, usage: "<#userid or name> [<ct/tt/spec>] [-k]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnTeamCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var teamName = command.GetArg(2).ToLower();
			var _teamName = "SPEC";
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
				ChangeTeam(caller, player, _teamName, teamNum, kill, callerName, command);
			});
		}

		public void ChangeTeam(CCSPlayerController? caller, CCSPlayerController? player, string teamName, CsTeam teamNum, bool kill, string? callerName = null, CommandInfo? command = null)
		{
			if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
				return;

			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (!teamName.Equals("swap"))
			{
				if (player.PawnIsAlive && teamNum != CsTeam.Spectator && !kill && Config.TeamSwitchType == 1)
					player.SwitchTeam(teamNum);
				else
					player?.ChangeTeam(teamNum);
			}
			else
			{
				if (player.TeamNum != (byte)CsTeam.Spectator)
				{
					var _teamNum = (CsTeam)player.TeamNum == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
					teamName = _teamNum == CsTeam.Terrorist ? "TT" : "CT";
					if (player.PawnIsAlive && !kill && Config.TeamSwitchType == 1)
					{
						player.SwitchTeam(_teamNum);
					}
					else
					{
						player.ChangeTeam(_teamNum);
					}
				}
			}

			if (caller == null || !SilentPlayers.Contains(caller.Slot))
			{
				foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
				{
					if (_localizer != null)
						controller.SendLocalizedMessage(_localizer,
											"sa_admin_team_message",
											callerName,
											player?.PlayerName ?? string.Empty,
											teamName);
				}
			}

			Helper.LogCommand(caller, $"css_team {player?.PlayerName} {teamName}");
		}

		[ConsoleCommand("css_rename", "Rename a player.")]
		[CommandHelper(1, "<#userid or name> <new name>")]
		[RequiresPermissions("@css/kick")]
		public void OnRenameCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var newName = command.GetArg(2);

			if (string.IsNullOrEmpty(newName))
				return;

			var targets = GetTarget(command);
			if (targets == null) return;
			var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			Helper.LogCommand(caller, command);

			playersToTarget.ForEach(player =>
			{
				if (player.Connected != PlayerConnectedState.PlayerConnected)
					return;

				if (!caller!.CanTarget(player)) return;
				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
					{
						if (_localizer != null)
							controller.SendLocalizedMessage(_localizer,
												"sa_admin_rename_message",
												callerName,
												player?.PlayerName ?? string.Empty,
												newName);
					}
				}

				player.Rename(newName);
			});
		}

		[ConsoleCommand("css_prename", "Permanent rename a player.")]
		[CommandHelper(1, "<#userid or name> <new name>")]
		[RequiresPermissions("@css/ban")]
		public void OnPRenameCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var newName = command.GetArg(2);

			var targets = GetTarget(command);
			if (targets == null) return;
			var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			Helper.LogCommand(caller, command);

			playersToTarget.ForEach(player =>
			{
				if (player.Connected != PlayerConnectedState.PlayerConnected)
					return;

				if (!caller!.CanTarget(player)) return;
				if (caller == null || !SilentPlayers.Contains(caller.Slot) && !string.IsNullOrEmpty(newName))
				{
					foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
					{
						if (_localizer != null)
							controller.SendLocalizedMessage(_localizer,
												"sa_admin_rename_message",
												callerName,
												player.PlayerName ?? string.Empty,
												newName);
					}
				}

				if (!string.IsNullOrEmpty(newName))
				{
					RenamedPlayers[player.SteamID] = newName;
				}
				else
				{
					RenamedPlayers.Remove(player.SteamID);
				}

				player.Rename(newName);
			});
		}

		[ConsoleCommand("css_respawn", "Respawn a dead player.")]
		[CommandHelper(1, "<#userid or name>")]
		[RequiresPermissions("@css/cheats")]
		public void OnRespawnCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;

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

		public void Respawn(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null, CommandInfo? command = null)
		{
			if (!caller.CanTarget(player)) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (_cBasePlayerControllerSetPawnFunc == null || player?.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

			var playerPawn = player.PlayerPawn.Value;
			_cBasePlayerControllerSetPawnFunc.Invoke(player, playerPawn, true, false);
			VirtualFunction.CreateVoid<CCSPlayerController>(player.Handle,
															GameData.GetOffset("CCSPlayerController_Respawn"))(player);

			Helper.LogCommand(caller, $"css_respawn {player?.PlayerName}");

			if (caller != null && SilentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
			{
				if (_localizer != null)
					controller.SendLocalizedMessage(_localizer,
										"sa_admin_respawn_message",
										callerName,
										player?.PlayerName ?? string.Empty);
			}
		}

		[ConsoleCommand("css_tp", "Teleport to a player.")]
		[ConsoleCommand("css_tpto", "Teleport to a player.")]
		[ConsoleCommand("css_goto", "Teleport to a player.")]
		[CommandHelper(1, "<#userid or name>")]
		[RequiresPermissions("@css/kick")]
		public void OnGotoCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller == null || !caller.PawnIsAlive) return;

			var targets = GetTarget(command);

			if (targets == null || targets.Count() > 1)
				return;

			var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			Helper.LogCommand(caller, command);

			playersToTarget.ForEach(player =>
			{
				if (player.Connected != PlayerConnectedState.PlayerConnected || !player.PawnIsAlive)
					return;

				if (!caller.CanTarget(player)) return;
				caller.TeleportPlayer(player);
				caller.Pawn.Value!.ToggleNoclip();

				AddTimer(3, () =>
				{
					caller.Pawn.Value!.ToggleNoclip();
				});

				if (SilentPlayers.Contains(caller.Slot)) return;
				foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
				{
					if (_localizer != null)
						controller.SendLocalizedMessage(_localizer,
											"sa_admin_tp_message",
											caller.PlayerName,
											player.PlayerName ?? string.Empty);
				}
			});
		}

		[ConsoleCommand("css_bring", "Teleport a player to you.")]
		[ConsoleCommand("css_tphere", "Teleport a player to you.")]
		[CommandHelper(1, "<#userid or name>")]
		[RequiresPermissions("@css/kick")]
		public void OnBringCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (caller == null || !caller.PawnIsAlive) return;

			var targets = GetTarget(command);

			if (targets == null || targets.Count() > 1)
				return;

			var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			Helper.LogCommand(caller, command);

			playersToTarget.ForEach(player =>
			{
				if (player.Connected != PlayerConnectedState.PlayerConnected || !player.PawnIsAlive)
					return;

				if (!caller.CanTarget(player)) return;
				player.TeleportPlayer(caller);
				caller.Pawn.Value!.ToggleNoclip();

				AddTimer(3, () =>
				{
					caller.Pawn.Value!.ToggleNoclip();
				});

				if (SilentPlayers.Contains(caller.Slot)) return;
				foreach (var controller in Helper.GetValidPlayers().Where(controller => controller is { IsValid: true, IsBot: false }))
				{
					if (_localizer != null)
						controller.SendLocalizedMessage(_localizer,
											"sa_admin_bring_message",
											caller.PlayerName,
											player.PlayerName ?? string.Empty);
				}
			});
		}
	}
}