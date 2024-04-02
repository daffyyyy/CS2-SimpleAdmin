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
			string callerName = caller == null ? "Console" : caller.PlayerName;
			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				Slay(caller, player, callerName, command);
			});
		}

		public void Slay(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
		{
			if (!player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.CommitSuicide(false, true);

			if (command != null)
			{
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
				Helper.LogCommand(caller, command);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_slay_message", callerName, player.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_give")]
		[RequiresPermissions("@css/cheats")]
		[CommandHelper(minArgs: 2, usage: "<#userid or name> <weapon>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGiveCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();
			string weaponName = command.GetArg(2);

			// check if item is typed
			if (weaponName == null || weaponName.Length < 5)
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

		public void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null, CommandInfo? command = null)
		{
			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			player?.GiveNamedItem(weaponName);
			SubGiveWeapon(caller, player!, weaponName, callerName);
		}

		public void SubGiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_give_message", callerName, player.PlayerName, weaponName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_strip")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnStripCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					StripWeapons(caller, player, callerName, command);
				}
			});
		}

		public void StripWeapons(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (!player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			player.RemoveWeapons();

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_strip_message", callerName, player.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_hp")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <health>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnHpCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			int health = 100;
			int.TryParse(command.GetArg(2), out health);

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					SetHp(caller, player, health, callerName, command);
				}
			});
		}

		public void SetHp(CCSPlayerController? caller, CCSPlayerController player, int health, string? callerName = null, CommandInfo? command = null)
		{
			if (!player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			callerName = caller == null ? "Console" : caller.PlayerName;

			player.SetHp(health);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_hp_message", callerName, player.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_speed")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <speed>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnSpeedCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			double speed = 1.0;
			double.TryParse(command.GetArg(2), out speed);

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

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

		public void SetSpeed(CCSPlayerController? caller, CCSPlayerController player, double speed, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetSpeed((float)speed);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_speed_message", callerName, player!.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_gravity")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <gravity>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGravityCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			double gravity = 1.0;
			double.TryParse(command.GetArg(2), out gravity);

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

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

		public void SetGravity(CCSPlayerController? caller, CCSPlayerController player, double gravity, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetGravity((float)gravity);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_gravity_message", callerName, player!.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_money")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> <money>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnMoneyCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			int money = 0;
			int.TryParse(command.GetArg(2), out money);

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

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

		public void SetMoney(CCSPlayerController? caller, CCSPlayerController player, int money, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetMoney(money);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_money_message", callerName, player!.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_god")]
		[RequiresPermissions("@css/cheats")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGodCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

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

		public void God(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (player != null)
			{
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

				if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_god_message", callerName, player.PlayerName]);
							_player.PrintToChat(sb.ToString());
						}
					}
				}
			}
		}

		[ConsoleCommand("css_slap")]
		[RequiresPermissions("@css/slay")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [damage]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnSlapCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			int damage = 0;

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

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

		public void Slap(CCSPlayerController? caller, CCSPlayerController player, int damage, CommandInfo? command = null)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			player!.Pawn.Value!.Slap(damage);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_slap_message", callerName, player.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}
		}

		[ConsoleCommand("css_team")]
		[RequiresPermissions("@css/kick")]
		[CommandHelper(minArgs: 2, usage: "<#userid or name> [<ct/tt/spec>] [-k]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnTeamCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			string teamName = command.GetArg(2).ToLower();
			string _teamName = "SPEC";
			CsTeam teamNum = CsTeam.Spectator;

			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

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

			bool kill = command.GetArg(3).ToLower().Equals("-k");

			playersToTarget.ForEach(player =>
			{
				ChangeTeam(caller, player, _teamName, teamNum, kill, callerName, command);
			});
		}

		public void ChangeTeam(CCSPlayerController? caller, CCSPlayerController player, string teamName, CsTeam teamNum, bool kill, string? callerName = null, CommandInfo? command = null)
		{
			if (!player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (!teamName.Equals("swap"))
			{
				if (player.PawnIsAlive && teamNum != CsTeam.Spectator && !kill && Config.TeamSwitchType == 1)
					player.SwitchTeam(teamNum);
				else
					player.ChangeTeam(teamNum);
			}
			else
			{
				if (player.TeamNum != (byte)CsTeam.Spectator)
				{
					CsTeam _teamNum = (CsTeam)player.TeamNum == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
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

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_team_message", callerName, player.PlayerName, teamName]);
						_player.PrintToChat(sb.ToString());
					}
				}
			}

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}
		}

		[ConsoleCommand("css_rename", "Rename a player.")]
		[CommandHelper(1, "<#userid or name> <new name>")]
		[RequiresPermissions("@css/kick")]
		public void OnRenameCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			string? newName = command.GetArg(2);

			if (string.IsNullOrEmpty(newName))
				return;

			TargetResult? targets = GetTarget(command);
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

			Helper.LogCommand(caller, command);
			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
					{
						foreach (CCSPlayerController _player in Helper.GetValidPlayers())
						{
							using (new WithTemporaryCulture(_player.GetLanguage()))
							{
								StringBuilder sb = new(_localizer!["sa_prefix"]);
								sb.Append(_localizer["sa_admin_rename_message", callerName, player.PlayerName, newName]);
								_player.PrintToChat(sb.ToString());
							}
						}
					}

					player.Rename(newName);
				}
			});
		}

		[ConsoleCommand("css_respawn", "Respawn a dead player.")]
		[CommandHelper(1, "<#userid or name>")]
		[RequiresPermissions("@css/cheats")]
		public void OnRespawnCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;

			TargetResult? targets = GetTarget(command);
			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

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

		public void Respawn(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (CBasePlayerController_SetPawnFunc == null || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

			var playerPawn = player.PlayerPawn.Value;
			CBasePlayerController_SetPawnFunc.Invoke(player, playerPawn, true, false);
			VirtualFunction.CreateVoid<CCSPlayerController>(player.Handle,
															GameData.GetOffset("CCSPlayerController_Respawn"))(player);

			if (command != null)
			{
				Helper.LogCommand(caller, command);
				Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			}

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_respawn_message", callerName, player.PlayerName]);
						_player.PrintToChat(sb.ToString());
					}
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

			string callerName = caller == null ? "Console" : caller.PlayerName;

			TargetResult? targets = GetTarget(command);

			if (targets == null || targets.Count() > 1)
				return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

			Helper.LogCommand(caller, command);
			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17 || !player.PawnIsAlive)
					return;

				if (caller!.CanTarget(player))
				{
					caller!.TeleportPlayer(player);
					caller!.Pawn.Value!.ToggleNoclip();

					AddTimer(3, () =>
					{
						caller!.Pawn.Value!.ToggleNoclip();
					});

					if (caller != null && !silentPlayers.Contains(caller.Slot))
					{
						foreach (CCSPlayerController _player in Helper.GetValidPlayers())
						{
							using (new WithTemporaryCulture(_player.GetLanguage()))
							{
								StringBuilder sb = new(_localizer!["sa_prefix"]);
								sb.Append(_localizer["sa_admin_tp_message", caller.PlayerName, player.PlayerName]);
								_player.PrintToChat(sb.ToString());
							}
						}
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

			string callerName = caller == null ? "Console" : caller.PlayerName;

			TargetResult? targets = GetTarget(command);

			if (targets == null || targets.Count() > 1)
				return;

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && !player.IsHLTV).ToList();

			Helper.LogCommand(caller, command);
			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17 || !player.PawnIsAlive)
					return;

				if (caller!.CanTarget(player))
				{
					player!.TeleportPlayer(caller!);
					caller!.Pawn.Value!.ToggleNoclip();

					AddTimer(3, () =>
					{
						caller!.Pawn.Value!.ToggleNoclip();
					});

					if (caller != null && !silentPlayers.Contains(caller.Slot))
					{
						foreach (CCSPlayerController _player in Helper.GetValidPlayers())
						{
							using (new WithTemporaryCulture(_player.GetLanguage()))
							{
								StringBuilder sb = new(_localizer!["sa_prefix"]);
								sb.Append(_localizer["sa_admin_bring_message", caller.PlayerName, player.PlayerName]);
								_player.PrintToChat(sb.ToString());
							}
						}
					}
				}
			});
		}
	}
}