using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Localization;

namespace CS2_SimpleAdmin.Menus;
public class MenuBuilder
{
    private readonly string _title;
    private readonly CCSPlayerController? _player;
    private readonly IStringLocalizer? _localizer;
    private readonly List<MenuOption> _options = [];
    private MenuBuilder? _parentMenu;
    private Action<CCSPlayerController>? _backAction;
    private Action<CCSPlayerController>? _resetAction;

    /// <summary>
    /// Constructor for player-localized menu with translation key
    /// </summary>
    public MenuBuilder(string titleKey, CCSPlayerController player, IStringLocalizer? localizer = null)
    {
        _title = titleKey;
        _player = player;
        _localizer = localizer ?? CS2_SimpleAdmin._localizer;
    }

    /// <summary>
    /// Constructor for static title (backward compatibility)
    /// </summary>
    public MenuBuilder(string title)
    {
        _title = title;
        _player = null;
        _localizer = null;
    }

    /// <summary>
    /// Gets the localized title for the player
    /// </summary>
    private string GetLocalizedTitle()
    {
        if (_player != null && _localizer != null)
        {
            using (new WithTemporaryCulture(_player.GetLanguage()))
            {
                return _localizer[_title];
            }
        }
        return _title;
    }

    /// <summary>
    /// Adds a menu option with an action.
    /// </summary>
    /// <param name="name">Display name or translation key</param>
    /// <param name="action">Action to perform when selected</param>
    /// <param name="disabled">Whether the option is disabled</param>
    /// <param name="permission">Required permission</param>
    /// <param name="isTranslationKey">If true, name is a translation key to be localized</param>
    public MenuBuilder AddOption(string name, Action<CCSPlayerController> action, bool disabled = false, string? permission = null, bool isTranslationKey = false)
    {
        _options.Add(new MenuOption
        {
            Name = name,
            Action = action,
            Disabled = disabled,
            Permission = permission,
            IsTranslationKey = isTranslationKey
        });
        return this;
    }

    /// <summary>
    /// Gets the localized name for a menu option
    /// </summary>
    private string GetLocalizedOptionName(MenuOption option)
    {
        if (option.IsTranslationKey && _player != null && _localizer != null)
        {
            using (new WithTemporaryCulture(_player.GetLanguage()))
            {
                return _localizer[option.Name];
            }
        }
        return option.Name;
    }

    /// <summary>
    /// Adds a menu option that opens a submenu.
    /// </summary>
    public MenuBuilder AddSubMenu(string name, Func<MenuBuilder> subMenuFactory, bool disabled = false, string? permission = null)
    {
        _options.Add(new MenuOption
        {
            Name = name,
            Action = player =>
            {
                var subMenu = subMenuFactory();
                subMenu.SetParent(this);
                // Automatically add back button to submenu
                subMenu.WithBackButton();
                subMenu.OpenMenu(player);
            },
            Disabled = disabled,
            Permission = permission
        });
        return this;
    }

    /// <summary>
    /// Adds a menu option that opens a submenu (with player parameter in factory).
    /// </summary>
    public MenuBuilder AddSubMenu(string name, Func<CCSPlayerController, MenuBuilder> subMenuFactory, bool disabled = false, string? permission = null)
    {
        _options.Add(new MenuOption
        {
            Name = name,
            Action = player =>
            {
                var subMenu = subMenuFactory(player);
                subMenu.SetParent(this);
                // Automatically add back button to submenu
                subMenu.WithBackButton();
                subMenu.OpenMenu(player);
            },
            Disabled = disabled,
            Permission = permission
        });
        return this;
    }

    /// <summary>
    /// Adds a back button to return to the previous menu.
    /// </summary>
    public MenuBuilder WithBackButton()
    {
        if (_parentMenu != null)
        {
            _backAction = player => _parentMenu.OpenMenu(player);

            // Add back option at the end of menu
            // AddOption(backButtonText, _backAction);
        }
        return this;
    }

    /// <summary>
    /// Sets the parent menu (for navigation).
    /// </summary>
    private void SetParent(MenuBuilder parent)
    {
        _parentMenu = parent;
        _backAction = player => _parentMenu.OpenMenu(player);
    }

    /// <summary>
    /// Opens the menu for a player.
    /// </summary>
    /// <param name="player">The player to open the menu for.</param>
    public void OpenMenu(CCSPlayerController player)
    {
        if (!player.IsValid) return;

        // Get localized title
        var localizedTitle = GetLocalizedTitle();

        // Use MenuManager dependency
        var menu = Helper.CreateMenu(localizedTitle, _backAction);
        if (menu == null) return;

        foreach (var option in _options)
        {
            // Check permissions if required
            if (!string.IsNullOrEmpty(option.Permission))
            {
                var steamId = new CounterStrikeSharp.API.Modules.Entities.SteamID(player.SteamID);
                if (!CounterStrikeSharp.API.Modules.Admin.AdminManager.PlayerHasPermissions(steamId, option.Permission))
                {
                    continue; // Skip option if player doesn't have permission
                }
            }

            // Get localized option name
            var localizedName = GetLocalizedOptionName(option);

            menu.AddMenuOption(localizedName, (menuPlayer, menuOption) =>
            {
                option.Action?.Invoke(menuPlayer);
            }, option.Disabled);
        }

        menu.Open(player);
    }

    /// <summary>
    /// Clears all menu options.
    /// </summary>
    /// <returns>This MenuBuilder instance for chaining.</returns>
    public MenuBuilder Clear()
    {
        _options.Clear();
        return this;
    }

    /// <summary>
    /// Sets a reset action for the menu.
    /// </summary>
    /// <param name="resetAction">The action to execute on reset.</param>
    /// <returns>This MenuBuilder instance for chaining.</returns>
    public MenuBuilder WithResetAction(Action<CCSPlayerController> resetAction)
    {
        _resetAction = resetAction;
        return this;
    }
    
    /// <summary>
    /// Sets a custom back action for the menu.
    /// </summary>
    /// <param name="backAction">The action to execute when going back (nullable).</param>
    /// <returns>This MenuBuilder instance for chaining.</returns>
    public MenuBuilder WithBackAction(Action<CCSPlayerController>? backAction)
    {
        _backAction = backAction;
        return this;
    }
}

/// <summary>
/// Represents an option within a menu.
/// </summary>
public class MenuOption
{
    public string Name { get; set; } = string.Empty;
    public Action<CCSPlayerController>? Action { get; set; }
    public bool Disabled { get; set; }
    public string? Permission { get; set; }
    public bool IsTranslationKey { get; set; }
}
    