using CounterStrikeSharp.API.Core;

namespace CS2_SimpleAdmin.Menus;
public class MenuBuilder(string title)
{
    private readonly List<MenuOption> _options = [];
    private MenuBuilder? _parentMenu;
    private Action<CCSPlayerController>? _backAction;
    private Action<CCSPlayerController>? _resetAction;

    /// <summary>
    /// Adds a menu option with an action.
    /// </summary>
    public MenuBuilder AddOption(string name, Action<CCSPlayerController> action, bool disabled = false, string? permission = null)
    {
        _options.Add(new MenuOption
        {
            Name = name,
            Action = action,
            Disabled = disabled,
            Permission = permission
        });
        return this;
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

        // Use MenuManager dependency
        var menu = Helper.CreateMenu(title, _backAction);
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

            menu.AddMenuOption(option.Name, (menuPlayer, menuOption) =>
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
}
    