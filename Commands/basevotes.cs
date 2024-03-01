using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
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
			string callerName = caller == null ? "Console" : caller.PlayerName;
			if (command.GetArg(1) == null || command.GetArg(1).Length < 0 || command.ArgCount < 2)
				return;

			if (_discordWebhookClientLog != null && _localizer != null)
			{
				string communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl().ToString() + ">" : "<https://steamcommunity.com/profiles/0>";
				_discordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(_localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
			}

			Helper.LogCommand(caller, command);

			voteAnswers.Clear();

			string question = command.GetArg(1);
			int answersCount = command.ArgCount;

			if (caller == null || caller != null && !silentPlayers.Contains(caller.Slot))
			{
				for (int i = 2; i <= answersCount - 1; i++)
				{
					voteAnswers.Add(command.GetArg(i), 0);
				}

				foreach (CCSPlayerController _player in Helper.GetValidPlayers())
				{
					using (new WithTemporaryCulture(_player.GetLanguage()))
					{
						ChatMenu voteMenu = new(_localizer!["sa_admin_vote_menu_title", question]);

						for (int i = 2; i <= answersCount - 1; i++)
						{
							voteMenu.AddMenuOption(command.GetArg(i), Helper.HandleVotes);
						}

						voteMenu.PostSelectAction = PostSelectAction.Close;

						Helper.PrintToCenterAll(_localizer!["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question]);
						StringBuilder sb = new(_localizer!["sa_prefix"]);
						sb.Append(_localizer["sa_admin_vote_message", caller == null ? "Console" : caller.PlayerName, question]);
						_player.PrintToChat(sb.ToString());

						MenuManager.OpenChatMenu(_player, voteMenu);
					}
				}

				voteInProgress = true;
			}

			if (voteInProgress)
			{
				AddTimer(30, () =>
				{
					foreach (CCSPlayerController _player in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(_player.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_vote_message_results", question]);
							_player.PrintToChat(sb.ToString());
						}
					}

					foreach (KeyValuePair<string, int> kvp in voteAnswers)
					{
						foreach (CCSPlayerController _player in Helper.GetValidPlayers())
						{
							using (new WithTemporaryCulture(_player.GetLanguage()))
							{
								StringBuilder sb = new(_localizer!["sa_prefix"]);
								sb.Append(_localizer["sa_admin_vote_message_results_answer", kvp.Key, kvp.Value]);
								_player.PrintToChat(sb.ToString());
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