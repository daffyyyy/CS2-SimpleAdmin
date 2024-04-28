using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using System.Text;

namespace CS2_SimpleAdmin
{
	public partial class CS2_SimpleAdmin
	{
		[ConsoleCommand("css_vote")]
		[RequiresPermissions("@css/generic")]
		[CommandHelper(minArgs: 2, usage: "<question> [... options ...]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnVoteCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			if (command.GetArg(1) == null || command.GetArg(1).Length < 0 || command.ArgCount < 2)
				return;

			Helper.SendDiscordLogMessage(caller, command, _discordWebhookClientLog, _localizer);
			Helper.LogCommand(caller, command);

			voteAnswers.Clear();

			var question = command.GetArg(1);
			var answersCount = command.ArgCount;

			if (caller == null || !silentPlayers.Contains(caller.Slot))
			{
				for (var i = 2; i <= answersCount - 1; i++)
				{
					voteAnswers.Add(command.GetArg(i), 0);
				}

				foreach (var player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						ChatMenu voteMenu = new(_localizer!["sa_admin_vote_menu_title", question]);

						for (var i = 2; i <= answersCount - 1; i++)
						{
							voteMenu.AddMenuOption(command.GetArg(i), Helper.HandleVotes);
						}

						voteMenu.PostSelectAction = PostSelectAction.Close;

						Helper.PrintToCenterAll(_localizer!["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question]);
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question]);
						player.PrintToChat(sb.ToString());

						MenuManager.OpenChatMenu(player, voteMenu);
					}
				}

				voteInProgress = true;
			}

			if (voteInProgress)
			{
				AddTimer(30, () =>
				{
					foreach (var player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_vote_message_results", question]);
							player.PrintToChat(sb.ToString());
						}
					}

					foreach (var (key, value) in voteAnswers)
					{
						foreach (var player in Helper.GetValidPlayers())
						{
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								StringBuilder sb = new(_localizer!["sa_prefix"]);
								sb.Append(_localizer["sa_admin_vote_message_results_answer", key, value]);
								player.PrintToChat(sb.ToString());
							}
						}
					}
					voteAnswers.Clear();
					voteInProgress = false;
				}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}
		}
	}
}