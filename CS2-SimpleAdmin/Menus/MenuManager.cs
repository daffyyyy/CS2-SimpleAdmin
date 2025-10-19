using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;

namespace CS2_SimpleAdmin.Menus;

public class MenuManager
{
    private static MenuManager? _instance;
    public static MenuManager Instance => _instance ??= new MenuManager();

    private readonly Dictionary<string, Func<CCSPlayerController, MenuBuilder>> _menuFactories = [];
    private readonly Dictionary<string, MenuCategory> _menuCategories = [];

    /// <summary>
    /// Provides public access to menu categories (for API usage).
    /// </summary>
    /// <returns>Dictionary of menu categories keyed by category ID.</returns>
    public Dictionary<string, MenuCategory> GetMenuCategories()
    {
        return _menuCategories;
    }

    /// <summary>
    /// Registers a new menu category with specified permissions.
    /// </summary>
    /// <param name="categoryId">Unique identifier for the category.</param>
    /// <param name="categoryName">Display name of the category.</param>
    /// <param name="permission">Required permission to access this category (default: @css/generic).</param>
    public void RegisterCategory(string categoryId, string categoryName, string permission = "@css/generic")
    {
        _menuCategories[categoryId] = new MenuCategory
        {
            Id = categoryId,
            Name = categoryName,
            Permission = permission,
            MenuFactories = new Dictionary<string, Func<CCSPlayerController, MenuBuilder>>()
        };
    }

    /// <summary>
    /// Registers a menu within a category (API for other plugins).
    /// </summary>
    /// <param name="categoryId">The category to add this menu to.</param>
    /// <param name="menuId">Unique identifier for the menu.</param>
    /// <param name="menuName">Display name of the menu.</param>
    /// <param name="menuFactory">Factory function that creates the menu for a player.</param>
    /// <param name="permission">Required permission to access this menu (optional).</param>
    public void RegisterMenu(string categoryId, string menuId, string menuName, Func<CCSPlayerController, MenuBuilder> menuFactory, string? permission = null)
    {
        if (!_menuCategories.ContainsKey(categoryId))
        {
            RegisterCategory(categoryId, categoryId); // Auto-create category if it doesn't exist
        }

        _menuCategories[categoryId].MenuFactories[menuId] = menuFactory;
        _menuCategories[categoryId].MenuNames[menuId] = menuName;
        if (permission != null)
        {
            _menuCategories[categoryId].MenuPermissions[menuId] = permission;
        }
    }

    /// <summary>
    /// Unregisters a menu from a category.
    /// </summary>
    /// <param name="categoryId">The category containing the menu.</param>
    /// <param name="menuId">The menu to unregister.</param>
    public void UnregisterMenu(string categoryId, string menuId)
    {
        if (!_menuCategories.TryGetValue(categoryId, out var category)) return;
        category.MenuFactories.Remove(menuId);
        _menuCategories[categoryId].MenuNames.Remove(menuId);
        _menuCategories[categoryId].MenuPermissions.Remove(menuId);
    }

    /// <summary>
    /// Creates the main admin menu for a player with accessible categories.
    /// </summary>
    /// <param name="player">The player to create the menu for.</param>
    /// <returns>A MenuBuilder instance for the main menu.</returns>
    public MenuBuilder CreateMainMenu(CCSPlayerController player)
    {
        var localizer = CS2_SimpleAdmin._localizer;
        var mainMenu = new MenuBuilder(localizer?["sa_title"] ?? "SimpleAdmin");

        foreach (var category in _menuCategories.Values)
        {
            if (category.MenuFactories.Count <= 0) continue;
            // Check category permissions
            var steamId = new SteamID(player.SteamID);
            if (!AdminManager.PlayerHasPermissions(steamId, category.Permission))
                continue;

            // Pass player to CreateCategoryMenu
            mainMenu.AddSubMenu(category.Name, () => CreateCategoryMenu(category, player),
                permission: category.Permission);
        }

        return mainMenu;
    }

    /// <summary>
    /// Creates a category submenu containing all registered menus in that category.
    /// </summary>
    /// <param name="category">The menu category to create.</param>
    /// <param name="player">The player to create the menu for.</param>
    /// <returns>A MenuBuilder instance for the category menu.</returns>
    private MenuBuilder CreateCategoryMenu(MenuCategory category, CCSPlayerController player)
    {
        var categoryMenu = new MenuBuilder(category.Name);

        foreach (var kvp in category.MenuFactories)
        {
            var menuId = kvp.Key;
            var menuFactory = kvp.Value;
            var menuName = category.MenuNames.TryGetValue(menuId, out var name) ? name : menuId;
            var permission = category.MenuPermissions.TryGetValue(menuId, out var perm) ? perm : null;

            // Check permissions
            if (!string.IsNullOrEmpty(permission))
            {
                var steamId = new SteamID(player.SteamID);
                if (!AdminManager.PlayerHasPermissions(steamId, permission))
                    continue;
            }

            // Call the actual factory with player parameter
            categoryMenu.AddSubMenu(menuName, () => menuFactory(player), permission: permission);
        }

        return categoryMenu.WithBackButton();
    }

    /// <summary>
    /// Opens the main admin menu for a player.
    /// </summary>
    /// <param name="player">The player to open the menu for.</param>
    public void OpenMainMenu(CCSPlayerController player)
    {
        var localizer = CS2_SimpleAdmin._localizer;
    
        var steamId = new SteamID(player.SteamID);
        if (!AdminManager.PlayerHasPermissions(steamId, "@css/generic"))
        {
            player.PrintToChat(localizer?["sa_prefix"] ?? "[SimpleAdmin] " +
                (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command"));
            return;
        }

        CreateMainMenu(player).OpenMenu(player);
    }

    /// <summary>
    /// Initializes default menu categories (Players, Server, Admin).
    /// </summary>
    public void InitializeDefaultCategories()
    {
        var localizer = CS2_SimpleAdmin._localizer;

        RegisterCategory("players", localizer?["sa_menu_players_manage"] ?? "Manage Players", "@css/generic");
        RegisterCategory("server", localizer?["sa_menu_server_manage"] ?? "Server Management", "@css/generic");
        // RegisterCategory("fun", localizer?["sa_menu_fun_commands"] ?? "Fun Commands", "@css/generic");
        RegisterCategory("admin", localizer?["sa_menu_admins_manage"] ?? "Admin Management", "@css/root");
    }

    /// <summary>
    /// Public method for creating category menus (for API usage).
    /// </summary>
    /// <param name="category">The menu category to create.</param>
    /// <param name="player">The player to create the menu for.</param>
    /// <returns>A MenuBuilder instance for the category menu.</returns>
    public MenuBuilder CreateCategoryMenuPublic(MenuCategory category, CCSPlayerController player)
    {
        return CreateCategoryMenu(category, player);
    }
}

/// <summary>
/// Represents a menu category containing multiple menus.
/// </summary>
public class MenuCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Permission { get; set; } = "@css/generic";
    public Dictionary<string, Func<CCSPlayerController, MenuBuilder>> MenuFactories { get; set; } = [];
    public Dictionary<string, string> MenuNames { get; set; } = [];
    public Dictionary<string, string> MenuPermissions { get; set; } = [];
}
