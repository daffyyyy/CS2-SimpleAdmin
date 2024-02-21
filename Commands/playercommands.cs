using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;
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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				Slay(caller, player, callerName);
			});
		}

		public void Slay(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null)
		{
			if (!player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.CommitSuicide(false, true);

			Helper.LogCommand(caller, $"css_slay {player?.PlayerName}");

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				GiveWeapon(caller, player, weaponName, callerName);
			});
		}

		public void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, CsItem weapon, string? callerName = null)
		{
			Helper.LogCommand(caller, $"css_give {player?.PlayerName} {weapon.ToString()}");

			player?.GiveNamedItem(weapon);
			SubGiveWeapon(caller, player!, weapon.ToString(), callerName);
		}

		public void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null)
		{
			Helper.LogCommand(caller, $"css_give {player?.PlayerName} {weaponName}");

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					StripWeapons(caller, player, callerName);
				}
			});
		}

		public void StripWeapons(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (!player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			player.RemoveWeapons();

			Helper.LogCommand(caller, $"css_strip {player?.PlayerName}");

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					SetHp(caller, player, health, callerName);
				}
			});
		}

		public void SetHp(CCSPlayerController? caller, CCSPlayerController player, int health, string? callerName = null)
		{
			if (!player.IsBot && player.SteamID.ToString().Length != 17)
				return;

			callerName = caller == null ? "Console" : caller.PlayerName;

			player.SetHp(health);

			Helper.LogCommand(caller, $"css_hp {player?.PlayerName} {health}");

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					SetSpeed(caller, player, speed, callerName);
				}
			});
		}

		public void SetSpeed(CCSPlayerController? caller, CCSPlayerController player, double speed, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player.SetSpeed((float)speed);

			Helper.LogCommand(caller, $"css_speed {player?.PlayerName} {speed}");

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

		[ConsoleCommand("css_god")]
		[RequiresPermissions("@css/cheats")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGodCommand(CCSPlayerController? caller, CommandInfo command)
		{
			string callerName = caller == null ? "Console" : caller.PlayerName;
			TargetResult? targets = GetTarget(command);
			if (targets == null) return;

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.PawnIsAlive && !player.IsHLTV).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					God(caller, player, callerName);
				}
			});
		}

		public void God(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (player != null)
			{
				Helper.LogCommand(caller, $"css_god {player.PlayerName}");

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

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
					Slap(caller, player, damage);
				}
			});
		}

		public void Slap(CCSPlayerController? caller, CCSPlayerController player, int damage, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			player!.Pawn.Value!.Slap(damage);

			Helper.LogCommand(caller, $"css_slap {player?.PlayerName} {damage}");

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

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

			Helper.LogCommand(caller, command);

			playersToTarget.ForEach(player =>
			{
				ChangeTeam(caller, player, _teamName, teamNum, kill, callerName);
			});
		}

		public void ChangeTeam(CCSPlayerController? caller, CCSPlayerController player, string teamName, CsTeam teamNum, bool kill, string? callerName = null)
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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			Helper.LogCommand(caller, command);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					player.Rename(newName);

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					Respawn(caller, player, callerName);
				}
			});
		}

		public void Respawn(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (CBasePlayerController_SetPawnFunc == null || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

			var playerPawn = player.PlayerPawn.Value;
			CBasePlayerController_SetPawnFunc.Invoke(player, playerPawn, true, false);
			VirtualFunction.CreateVoid<CCSPlayerController>(player.Handle,
															GameData.GetOffset("CCSPlayerController_Respawn"))(player);

			Helper.LogCommand(caller, $"css_respawn {player.PlayerName}");

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			Helper.LogCommand(caller, command);

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

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			Helper.LogCommand(caller, command);

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