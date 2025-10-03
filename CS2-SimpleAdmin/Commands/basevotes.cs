using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using Menu;
using Menu.Enums;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    /// <summary>
    /// Handles the vote command, creates voting menu for players, and collects answers.
    /// Displays results after timeout and resets voting state.
    /// </summary>
    /// <param name="caller">The player/admin who initiated the vote, or null for console.</param>
    /// <param name="command">Command object containing question and options.</param>
    [RequiresPermissions("@css/generic")]
    [CommandHelper(minArgs: 2, usage: "<question> [... options ...]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnVoteCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (command.ArgCount < 2 || _localizer == null)
            return;

        Helper.LogCommand(caller, command);

        VoteAnswers.Clear();

        var question = command.GetArg(1);
        var answersCount = command.ArgCount;

        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            for (var i = 2; i <= answersCount - 1; i++)
            {
                VoteAnswers.Add(command.GetArg(i), 0);
            }

            foreach (var player in Helper.GetValidPlayers())
            {
                using (new WithTemporaryCulture(player.GetLanguage()))
                {
                    List<MenuItem> items = new();
                    var optionMap = new Dictionary<int, Action>();
                    int i = 0;

                    for (var argIndex = 2; argIndex <= answersCount - 1; argIndex++)
                    {
                        string answer = command.GetArg(argIndex);

                        items.Add(new MenuItem(MenuItemType.Button, [new MenuValue(answer)]));

                        optionMap[i++] = () =>
                        {
                            if (!VoteInProgress)
                                return;

                            VoteAnswers[answer]++;
                        };
                    }

                    // If no answers, stop
                    if (i == 0) return;

                    // Broadcast vote message
                    Helper.PrintToCenterAll(
                        _localizer["sa_admin_vote_message",
                            caller == null ? _localizer["sa_console"] : caller.PlayerName,
                            question]);

                    player.SendLocalizedMessage(
                        _localizer,
                        "sa_admin_vote_message",
                        caller == null ? _localizer["sa_console"] : caller.PlayerName,
                        question);

                    // Show the vote menu
                    Menu?.ShowScrollableMenu(
                        player,
                        _localizer["sa_admin_vote_menu_title", question],
                        items,
                        (buttons, menu, selected) =>
                        {
                            if (selected == null) return;

                            if (buttons == MenuButtons.Select && optionMap.TryGetValue(menu.Option, out var action))
                            {
                                action.Invoke();
                            }
                        },
                        false, freezePlayer: false, disableDeveloper: true);
                }
            }

            VoteInProgress = true;
        }

        if (VoteInProgress)
        {
            AddTimer(30, () =>
            {
                foreach (var player in Helper.GetValidPlayers())
                {
                    Menu?.ClearMenus(player);
                    if (_localizer != null)
                        player.SendLocalizedMessage(_localizer,
                            "sa_admin_vote_message_results",
                            question);
                }

                foreach (var (key, value) in VoteAnswers)
                {
                    foreach (var player in Helper.GetValidPlayers())
                    {
                        if (_localizer != null)
                            player.SendLocalizedMessage(_localizer,
                                "sa_admin_vote_message_results_answer",
                                key,
                                value);
                    }
                }
                VoteAnswers.Clear();
                VoteInProgress = false;
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }
    }
}