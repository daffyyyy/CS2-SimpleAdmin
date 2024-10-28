using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    [CommandHelper(1, "<message>")]
    [RequiresPermissions("@css/chat")]
    public void OnAdminToAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        Helper.LogCommand(caller, command);

        var utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
        var utf8String = Encoding.UTF8.GetString(utf8BytesString);

        foreach (var player in Helper.GetValidPlayers()
                     .Where(p => AdminManager.PlayerHasPermissions(p, "@css/chat")))
        {
            if (_localizer != null)
                player.PrintToChat(_localizer["sa_adminchat_template_admin",
                    caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName,
                    utf8String]);
        }
    }

    [CommandHelper(1, "<message>")]
    [RequiresPermissions("@css/chat")]
    public void OnAdminCustomSayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;

        var utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
        var utf8String = Encoding.UTF8.GetString(utf8BytesString);

        Helper.LogCommand(caller, command);

        foreach (var player in Helper.GetValidPlayers())
        {
            player.PrintToChat(utf8String.ReplaceColorTags());
        }
    }

    [CommandHelper(1, "<message>")]
    [RequiresPermissions("@css/chat")]
    public void OnAdminSayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (command.GetCommandString[command.GetCommandString.IndexOf(' ')..].Length == 0) return;

        var utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
        var utf8String = Encoding.UTF8.GetString(utf8BytesString);

        Helper.LogCommand(caller, command);

        foreach (var player in Helper.GetValidPlayers())
        {
            player.SendLocalizedMessage(_localizer,
                "sa_adminsay_prefix",
                utf8String.ReplaceColorTags());
        }
    }

    [CommandHelper(2, "<#userid or name> <message>")]
    [RequiresPermissions("@css/chat")]
    public void OnAdminPrivateSayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        //Helper.LogCommand(caller, command);

        var range = command.GetArg(0).Length + command.GetArg(1).Length + 2;
        var message = command.GetCommandString[range..];

        var utf8BytesString = Encoding.UTF8.GetBytes(message);
        var utf8String = Encoding.UTF8.GetString(utf8BytesString);

        playersToTarget.ForEach(player =>
        {
            player.PrintToChat($"({callerName}) {utf8String}".ReplaceColorTags());
        });

        command.ReplyToCommand($" Private message sent!");
    }

    [CommandHelper(1, "<message>")]
    [RequiresPermissions("@css/chat")]
    public void OnAdminCenterSayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
        var utf8String = Encoding.UTF8.GetString(utf8BytesString);

        Helper.LogCommand(caller, command);

        Helper.PrintToCenterAll(utf8String.ReplaceColorTags());
    }

    [CommandHelper(1, "<message>")]
    [RequiresPermissions("@css/chat")]
    public void OnAdminHudSayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var utf8BytesString = Encoding.UTF8.GetBytes(command.GetCommandString[command.GetCommandString.IndexOf(' ')..]);
        var utf8String = Encoding.UTF8.GetString(utf8BytesString);

        Helper.LogCommand(caller, command);

        VirtualFunctions.ClientPrintAll(
            HudDestination.Alert,
            utf8String.ReplaceColorTags(),
            0, 0, 0, 0);
    }
}