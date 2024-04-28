using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

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

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				Slay(caller, player, callerName, command);
			});
		}

		public void Slay(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null, CommandInfo? command = null)
		{
			if (player != null && !player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player?.CommitSuicide(false, true);

			if (command != null)
			{
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
				Helper.LogCommand(caller, command);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_slay_message", callerName, player?.PlayerName ?? string.Empty]);
					controller.PrintToChat(sb.ToString());
				}
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

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();
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
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				GiveWeapon(caller, player, weaponName, callerName, command);
			});
		}

		public void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, CsItem weapon, string? callerName = null)
		{
			Helper.LogCommand(caller, $"css_give {player?.PlayerName} {weapon.ToString()}");

			player?.GiveNamedItem(weapon);
			SubGiveWeapon(caller, player!, weapon.ToString(), callerName);
		}

		private void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null, CommandInfo? command = null)
		{
			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			player?.GiveNamedItem(weaponName);
			SubGiveWeapon(caller, player!, weaponName, callerName);
		}

		private void SubGiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (caller != null && (silentPlayers.Contains(caller.Slot))) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_give_message", callerName, player.PlayerName, weaponName]);
					controller.PrintToChat(sb.ToString());
				}
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

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

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
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (player != null && !player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			player?.RemoveWeapons();

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_strip_message", callerName, player?.PlayerName ?? string.Empty]);
					controller.PrintToChat(sb.ToString());
				}
			}
		}

		[ConsoleCommand("css_hp")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <health>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnHpCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			int.TryParse(command.GetArg(2), out var health);

			var targets = GetTarget(command);
			if (targets == null) return;

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					SetHp(caller, player, health, callerName, command);
				}
			});
		}

		public void SetHp(CCSPlayerController? caller, CCSPlayerController? player, int health, string? callerName = null, CommandInfo? command = null)
		{
			if (player != null && !player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			callerName = caller == null ? "Console" : caller.PlayerName;

			player.SetHp(health);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_hp_message", callerName, player?.PlayerName ?? string.Empty]);
					controller.PrintToChat(sb.ToString());
				}
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

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					SetSpeed(caller, player, speed, callerName, command);
				}
			});
		}

		public void SetSpeed(CCSPlayerController? caller, CCSPlayerController? player, double speed, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetSpeed((float)speed);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_speed_message", callerName, player!.PlayerName]);
					controller.PrintToChat(sb.ToString());
				}
			}
		}

		[ConsoleCommand("css_gravity")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <gravity>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGravityCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var gravity = 1.0;
			double.TryParse(command.GetArg(2), out gravity);

			var targets = GetTarget(command);
			if (targets == null) return;

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					SetGravity(caller, player, gravity, callerName, command);
				}
			});
		}

		public void SetGravity(CCSPlayerController? caller, CCSPlayerController? player, double gravity, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetGravity((float)gravity);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_gravity_message", callerName, player!.PlayerName]);
					controller.PrintToChat(sb.ToString());
				}
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

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					SetMoney(caller, player, money, callerName, command);
				}
			});
		}

		public void SetMoney(CCSPlayerController? caller, CCSPlayerController? player, int money, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetMoney(money);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_money_message", callerName, player!.PlayerName]);
					controller.PrintToChat(sb.ToString());
				}
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

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					God(caller, player, callerName, command);
				}
			});
		}

		public void God(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (player == null) return;
			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (!godPlayers.Contains(player.Slot))
			{
				godPlayers.Add(player.Slot);
			}
			else
			{
				RemoveFromConcurrentBag(godPlayers, player.Slot);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_god_message", callerName, player.PlayerName]);
					controller.PrintToChat(sb.ToString());
				}
			}
		}

		[ConsoleCommand("css_slap")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [damage]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnSlapCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var damage = 0;

			var targets = GetTarget(command);
			if (targets == null) return;

			var playersToTarget = targets!.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			if (command.ArgCount >= 2)
			{
				int.TryParse(command.GetArg(2), out damage);
			}

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					Slap(caller, player, damage, command);
				}
			});
		}

		public void Slap(CCSPlayerController? caller, CCSPlayerController? player, int damage, CommandInfo? command = null)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			player!.Pawn.Value!.Slap(damage);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_slap_message", callerName, player.PlayerName]);
					controller.PrintToChat(sb.ToString());
				}
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

			var playersToTarget = targets!.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

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
			if (player != null && !player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (!teamName.Equals("swap"))
			{
				if (player != null && player.PawnIsAlive && teamNum != CsTeam.Spectator && !kill && Config.TeamSwitchType == 1)
					player.SwitchTeam(teamNum);
				else
					player?.ChangeTeam(teamNum);
			}
			else
			{
				if (player != null && player.TeamNum != (byte)CsTeam.Spectator)
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

			if (caller == null || !silentPlayers.Contains(caller.Slot))
			{
				foreach (var controller in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(controller.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_team_message", callerName, player?.PlayerName ?? string.Empty, teamName]);
						controller.PrintToChat(sb.ToString());
					}
				}
			}

			if (command == null) return;
			Helper.LogCommand(caller, command);
			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
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
			var playersToTarget = targets!.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			Helper.LogCommand(caller, command);
			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (!caller!.CanTarget(player)) return;
				if (caller == null || !silentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_rename_message", callerName, player.PlayerName, newName]);
							controller.PrintToChat(sb.ToString());
						}
					}
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
			var playersToTarget = targets!.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					Respawn(caller, player, callerName, command);
				}
			});
		}

		public void Respawn(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (CBasePlayerController_SetPawnFunc == null || player?.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

			var playerPawn = player.PlayerPawn.Value;
			CBasePlayerController_SetPawnFunc.Invoke(player, playerPawn, true, false);
			VirtualFunction.CreateVoid<CCSPlayerController>(player.Handle,
															GameData.GetOffset("CCSPlayerController_Respawn"))(player);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_respawn_message", callerName, player.PlayerName]);
					controller.PrintToChat(sb.ToString());
				}
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

			var callerName = caller.PlayerName;

			var targets = GetTarget(command);

			if (targets == null || targets.Count() > 1)
				return;

			var playersToTarget = targets!.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			Helper.LogCommand(caller, command);
			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17 || !player.PawnIsAlive)
					return;

				if (!caller!.CanTarget(player)) return;
				caller!.TeleportPlayer(player);
				caller!.Pawn.Value!.ToggleNoclip();

				AddTimer(3, () =>
				{
					caller!.Pawn.Value!.ToggleNoclip();
				});

				if (silentPlayers.Contains(caller.Slot)) return;
				foreach (var controller in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(controller.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_tp_message", caller.PlayerName, player.PlayerName]);
						controller.PrintToChat(sb.ToString());
					}
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

			var callerName = caller.PlayerName;

			var targets = GetTarget(command);

			if (targets == null || targets.Count() > 1)
				return;

			var playersToTarget = targets!.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			Helper.LogCommand(caller, command);
			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17 || !player.PawnIsAlive)
					return;

				if (!caller!.CanTarget(player)) return;
				player!.TeleportPlayer(caller!);
				caller!.Pawn.Value!.ToggleNoclip();

				AddTimer(3, () =>
				{
					caller!.Pawn.Value!.ToggleNoclip();
				});

				if (silentPlayers.Contains(caller.Slot)) return;
				foreach (var controller in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(controller.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_bring_message", caller.PlayerName, player.PlayerName]);
						controller.PrintToChat(sb.ToString());
					}
				}
			});
		}
	}
}