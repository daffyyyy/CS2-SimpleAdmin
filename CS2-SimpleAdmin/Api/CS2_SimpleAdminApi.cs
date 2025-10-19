using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdmin.Menus;
using CS2_SimpleAdminApi;
using Microsoft.Extensions.Localization;

namespace CS2_SimpleAdmin.Api;

public class CS2_SimpleAdminApi : ICS2_SimpleAdminApi
{
    public event Action? OnSimpleAdminReady;
    public void OnSimpleAdminReadyEvent() => OnSimpleAdminReady?.Invoke();

    public PlayerInfo GetPlayerInfo(CCSPlayerController player)
    {
        return !player.UserId.HasValue
            ? throw new KeyNotFoundException("Player with specific UserId not found")
            : CS2_SimpleAdmin.PlayersInfo[player.SteamID];
    }

    public string GetConnectionString() => CS2_SimpleAdmin.Instance.DbConnectionString;
    public string GetServerAddress() => CS2_SimpleAdmin.IpAddress;
    public int? GetServerId() => CS2_SimpleAdmin.ServerId;

    public Dictionary<PenaltyType, List<(DateTime EndDateTime, int Duration, bool Passed)>> GetPlayerMuteStatus(
        CCSPlayerController player)
    {
        return PlayerPenaltyManager.GetAllPlayerPenalties(player.Slot);
    }

    public event Action<PlayerInfo, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltied;
    public event Action<SteamID, PlayerInfo?, PenaltyType, string, int, int?, int?>? OnPlayerPenaltiedAdded;
    public event Action<string, string?, bool, object>? OnAdminShowActivity;
    public event Action<int, bool>? OnAdminToggleSilent;

    public void OnPlayerPenaltiedEvent(PlayerInfo player, PlayerInfo? admin, PenaltyType penaltyType, string reason,
        int duration, int? penaltyId) => OnPlayerPenaltied?.Invoke(player, admin, penaltyType, reason, duration,
        penaltyId, CS2_SimpleAdmin.ServerId);

    public void OnPlayerPenaltiedAddedEvent(SteamID player, PlayerInfo? admin, PenaltyType penaltyType, string reason,
        int duration, int? penaltyId) => OnPlayerPenaltiedAdded?.Invoke(player, admin, penaltyType, reason, duration,
        penaltyId, CS2_SimpleAdmin.ServerId);

    public void OnAdminShowActivityEvent(string messageKey, string? callerName = null, bool dontPublish = false,
        params object[] messageArgs) => OnAdminShowActivity?.Invoke(messageKey, callerName, dontPublish, messageArgs);

    public void OnAdminToggleSilentEvent(int slot, bool status) => OnAdminToggleSilent?.Invoke(slot, status);

    public void IssuePenalty(CCSPlayerController player, CCSPlayerController? admin, PenaltyType penaltyType,
        string reason, int duration = -1)
    {
        switch (penaltyType)
        {
            case PenaltyType.Ban:
            {
                CS2_SimpleAdmin.Instance.Ban(admin, player, duration, reason);
                break;
            }
            case PenaltyType.Kick:
            {
                CS2_SimpleAdmin.Instance.Kick(admin, player, reason);
                break;
            }
            case PenaltyType.Gag:
            {
                CS2_SimpleAdmin.Instance.Gag(admin, player, duration, reason);
                break;
            }
            case PenaltyType.Mute:
            {
                CS2_SimpleAdmin.Instance.Mute(admin, player, duration, reason);
                break;
            }
            case PenaltyType.Silence:
            {
                CS2_SimpleAdmin.Instance.Silence(admin, player, duration, reason);
                break;
            }
            case PenaltyType.Warn:
            {
                CS2_SimpleAdmin.Instance.Warn(admin, player, duration, reason);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(penaltyType), penaltyType, null);
        }
    }

    public void IssuePenalty(SteamID steamid, CCSPlayerController? admin, PenaltyType penaltyType, string reason,
        int duration = -1)
    {
        switch (penaltyType)
        {
            case PenaltyType.Ban:
            {
                CS2_SimpleAdmin.Instance.AddBan(admin, steamid, duration, reason);
                break;
            }
            case PenaltyType.Gag:
            {
                CS2_SimpleAdmin.Instance.AddGag(admin, steamid, duration, reason);
                break;
            }
            case PenaltyType.Mute:
            {
                CS2_SimpleAdmin.Instance.AddMute(admin, steamid, duration, reason);
                break;
            }
            case PenaltyType.Silence:
            {
                CS2_SimpleAdmin.Instance.AddSilence(admin, steamid, duration, reason);
                break;
            }
            case PenaltyType.Warn:
            {
                CS2_SimpleAdmin.Instance.AddWarn(admin, steamid, duration, reason);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(penaltyType), penaltyType, null);
        }
    }

    public void LogCommand(CCSPlayerController? caller, string command)
    {
        Helper.LogCommand(caller, command);
    }

    public void LogCommand(CCSPlayerController? caller, CommandInfo command)
    {
        Helper.LogCommand(caller, command);
    }

    public bool IsAdminSilent(CCSPlayerController player)
    {
        return CS2_SimpleAdmin.SilentPlayers.Contains(player.Slot);
    }

    public HashSet<int> ListSilentAdminsSlots()
    {
        return CS2_SimpleAdmin.SilentPlayers;
    }

    public void RegisterCommand(string name, string? description, CommandInfo.CommandCallback callback)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Command name cannot be null or empty.", nameof(name));

        ArgumentNullException.ThrowIfNull(callback);

        var definition = new CommandDefinition(name, description ?? "No description", callback);
        if (!RegisterCommands._commandDefinitions.TryGetValue(name, out var list))
        {
            list = new List<CommandDefinition>();
            RegisterCommands._commandDefinitions[name] = list;
        }
        
        list.Add(definition);
    }

    public void UnRegisterCommand(string commandName)
    {
        var definitions = RegisterCommands._commandDefinitions[commandName];
        if (definitions.Count == 0)
            return;

        foreach (var definition in definitions)
        {
            CS2_SimpleAdmin.Instance.RemoveCommand(commandName, definition.Callback);
        }
    }

    public TargetResult? GetTarget(CommandInfo command)
    {
        return CS2_SimpleAdmin.GetTarget(command);
    }

    public void ShowAdminActivity(string messageKey, string? callerName = null, bool dontPublish = false,
        params object[] messageArgs)
    {
        Helper.ShowAdminActivity(messageKey, callerName, dontPublish, messageArgs);
    }

    public void ShowAdminActivityTranslated(string translatedMessage, string? callerName = null,
        bool dontPublish = false)
    {
        Helper.ShowAdminActivityTranslated(translatedMessage, callerName, dontPublish);
    }

    public void ShowAdminActivityLocalized(object moduleLocalizer, string messageKey, string? callerName = null,
        bool dontPublish = false, params object[] messageArgs)
    {
        if (moduleLocalizer is not IStringLocalizer localizer)
            throw new InvalidOperationException("moduleLocalizer must be an IStringLocalizer instance");

        Helper.ShowAdminActivityLocalized(localizer, messageKey, callerName, dontPublish, messageArgs);
    }

    public void RegisterMenuCategory(string categoryId, string categoryName, string permission = "@css/generic")
    {
        Menus.MenuManager.Instance.RegisterCategory(categoryId, categoryName, permission);
    }

    public void RegisterMenu(string categoryId, string menuId, string menuName,
        Func<CCSPlayerController, object> menuFactory, string? permission = null, string? commandName = null)
    {
        Menus.MenuManager.Instance.RegisterMenu(categoryId, menuId, menuName, BuilderFactory, permission, commandName);
        return;

        MenuBuilder BuilderFactory(CCSPlayerController player)
        {
            if (menuFactory(player) is not MenuBuilder menuBuilder)
                throw new InvalidOperationException("Menu factory must return MenuBuilder");

            // Dodaj automatyczną obsługę przycisku 'Wróć'
            menuBuilder.WithBackAction(p =>
            {
                if (Menus.MenuManager.Instance.GetMenuCategories().TryGetValue(categoryId, out var category))
                {
                    Menus.MenuManager.Instance.CreateCategoryMenuPublic(category, p).OpenMenu(p);
                }
                else
                {
                    Menus.MenuManager.Instance.OpenMainMenu(p);
                }
            });

            return menuBuilder;
        }
    }

    public void RegisterMenu(string categoryId, string menuId, string menuName,
        Func<CCSPlayerController, MenuContext, object> menuFactory, string? permission = null, string? commandName = null)
    {
        Menus.MenuManager.Instance.RegisterMenu(categoryId, menuId, menuName, BuilderFactory, permission, commandName);
        return;

        MenuBuilder BuilderFactory(CCSPlayerController player)
        {
            var context = new MenuContext(categoryId, menuId, menuName, permission, commandName);

            if (menuFactory(player, context) is not MenuBuilder menuBuilder)
                throw new InvalidOperationException("Menu factory must return MenuBuilder");

            // Dodaj automatyczną obsługę przycisku 'Wróć'
            menuBuilder.WithBackAction(p =>
            {
                if (Menus.MenuManager.Instance.GetMenuCategories().TryGetValue(categoryId, out var category))
                {
                    Menus.MenuManager.Instance.CreateCategoryMenuPublic(category, p).OpenMenu(p);
                }
                else
                {
                    Menus.MenuManager.Instance.OpenMainMenu(p);
                }
            });

            return menuBuilder;
        }
    }


    public void UnregisterMenu(string categoryId, string menuId)
    {
        Menus.MenuManager.Instance.UnregisterMenu(categoryId, menuId);
    }

    public object CreateMenuWithBack(string title, string categoryId, CCSPlayerController player)
    {
        var builder = new MenuBuilder(title);
        builder.WithBackAction(p =>
        {
            if (Menus.MenuManager.Instance.GetMenuCategories().TryGetValue(categoryId, out var category))
            {
                Menus.MenuManager.Instance.CreateCategoryMenuPublic(category, p).OpenMenu(p);
            }
            else
            {
                Menus.MenuManager.Instance.OpenMainMenu(p);
            }
        });

        return builder;
    }

    public object CreateMenuWithBack(MenuContext context, CCSPlayerController player)
    {
        return CreateMenuWithBack(context.MenuTitle, context.CategoryId, player);
    }

    public List<CCSPlayerController> GetValidPlayers()
    {
        return Helper.GetValidPlayers();
    }

    public object CreateMenuWithPlayers(string title, string categoryId, CCSPlayerController admin,
        Func<CCSPlayerController, bool> filter, Action<CCSPlayerController, CCSPlayerController> onSelect)
    {
        var menu = (MenuBuilder)CreateMenuWithBack(title, categoryId, admin);
        var players = Helper.GetValidPlayers().Where(filter);

        foreach (var player in players)
        {
            var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
            menu.AddOption(playerName, _ =>
            {
                if (player.IsValid)
                {
                    onSelect(admin, player);
                }
            });
        }

        return menu;
    }

    public object CreateMenuWithPlayers(MenuContext context, CCSPlayerController admin,
        Func<CCSPlayerController, bool> filter, Action<CCSPlayerController, CCSPlayerController> onSelect)
    {
        return CreateMenuWithPlayers(context.MenuTitle, context.CategoryId, admin, filter, onSelect);
    }

    public void AddMenuOption(object menu, string name, Action<CCSPlayerController> action, bool disabled = false,
        string? permission = null)
    {
        if (menu is not MenuBuilder menuBuilder)
            throw new InvalidOperationException("Menu must be a MenuBuilder instance");

        menuBuilder.AddOption(name, action, disabled, permission);
    }

    public void AddSubMenu(object menu, string name, Func<CCSPlayerController, object> subMenuFactory,
        bool disabled = false, string? permission = null)
    {
        if (menu is not MenuBuilder menuBuilder)
            throw new InvalidOperationException("Menu must be a MenuBuilder instance");

        menuBuilder.AddSubMenu(name, player =>
        {
            var subMenu = subMenuFactory(player);
            if (subMenu is not MenuBuilder builder)
                throw new InvalidOperationException("SubMenu factory must return MenuBuilder");
            return builder;
        }, disabled, permission);
    }

    public void OpenMenu(object menu, CCSPlayerController player)
    {
        if (menu is not MenuBuilder menuBuilder)
            throw new InvalidOperationException("Menu must be a MenuBuilder instance");

        menuBuilder.OpenMenu(player);
    }
}