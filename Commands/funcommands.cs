using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using System.Text;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		[ConsoleCommand("css_noclip", "Noclip a player.")]
		[CommandHelper(1, "<#userid or name>")]
		[RequiresPermissions("@css/cheats")]
		public void OnNoclipCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;

			var targets = GetTarget(command);
			var playersToTarget = targets!.Players.Where(player =>
				player.IsValid && player.SteamID.ToString().Length == 17 &&
				player is { PawnIsAlive: true, IsHLTV: false }).ToList();

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					NoClip(caller, player, callerName);
				}
			});
		}

		public void NoClip(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;
			player!.Pawn.Value!.ToggleNoclip();

			Helper.LogCommand(caller, $"css_noclip {player.PlayerName}");

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_noclip_message", callerName, player.PlayerName]);
					controller.PrintToChat(sb.ToString());
				}
			}
		}

		[ConsoleCommand("css_freeze", "Freeze a player.")]
		[CommandHelper(1, "<#userid or name> [duration]")]
		[RequiresPermissions("@css/slay")]
		public void OnFreezeCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			int.TryParse(command.GetArg(2), out var time);

			var targets = GetTarget(command);
			var playersToTarget = targets!.Players.Where(player => player is { IsValid: true, PawnIsAlive: true, IsHLTV: false }).ToList();

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					Freeze(caller, player, time, callerName);
				}
			});
		}

		public void Freeze(CCSPlayerController? caller, CCSPlayerController? player, int time, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player?.Pawn.Value!.Freeze();

			Helper.LogCommand(caller, $"css_freeze {player?.PlayerName}");

			if (time > 0)
				AddTimer(time, () => player?.Pawn.Value!.Unfreeze(), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			if (caller != null && silentPlayers.Contains(caller.Slot)) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_freeze_message", callerName, player?.PlayerName ?? string.Empty]);
					controller.PrintToChat(sb.ToString());
				}
			}
		}

		[ConsoleCommand("css_unfreeze", "Unfreeze a player.")]
		[CommandHelper(1, "<#userid or name>")]
		[RequiresPermissions("@css/slay")]
		public void OnUnfreezeCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;

			var targets = GetTarget(command);
			var playersToTarget = targets!.Players.Where(player => player is { IsValid: true, PawnIsAlive: true, IsHLTV: false }).ToList();

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				Unfreeze(caller, player, callerName, command);
			});
		}

		public void Unfreeze(CCSPlayerController? caller, CCSPlayerController? player, string? callerName = null, CommandInfo? command = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			player!.Pawn.Value!.Unfreeze();

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
					sb.Append(_localizer["sa_admin_unfreeze_message", callerName, player.PlayerName]);
					controller.PrintToChat(sb.ToString());
				}
			}
		}
	}
}