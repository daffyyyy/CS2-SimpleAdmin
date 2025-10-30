using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
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
    /// Registers a new menu category with per-player localization support for modules.
    /// 🆕 NEW: Enables modules to provide localized category names based on each player's css_lang!
    /// </summary>
    /// <param name="categoryId">Unique identifier for the category.</param>
    /// <param name="categoryNameKey">Translation key from module's lang files.</param>
    /// <param name="permission">Required permission to access this category.</param>
    /// <param name="moduleLocalizer">Module's IStringLocalizer for per-player translation.</param>
    public void RegisterCategory(string categoryId, string categoryNameKey, string permission, Microsoft.Extensions.Localization.IStringLocalizer moduleLocalizer)
    {
        _menuCategories[categoryId] = new MenuCategory
        {
            Id = categoryId,
            Name = categoryNameKey,  // Store the key, not translated text
            Permission = permission,
            MenuFactories = new Dictionary<string, Func<CCSPlayerController, MenuBuilder>>(),
            ModuleLocalizer = moduleLocalizer  // Store module's localizer
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
    /// <param name="commandName">Command name for permission override checking (optional, e.g., "css_god").</param>
    public void RegisterMenu(string categoryId, string menuId, string menuName, Func<CCSPlayerController, MenuBuilder> menuFactory, string? permission = null, string? commandName = null)
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
        if (commandName != null)
        {
            _menuCategories[categoryId].MenuCommandNames[menuId] = commandName;
        }
    }

    /// <summary>
    /// Registers a menu with per-player localization support for modules.
    /// 🆕 NEW: Enables modules to provide localized menu names based on each player's css_lang!
    /// </summary>
    /// <param name="categoryId">The category to add this menu to.</param>
    /// <param name="menuId">Unique identifier for the menu.</param>
    /// <param name="menuNameKey">Translation key from module's lang files.</param>
    /// <param name="menuFactory">Factory function that creates the menu for a player.</param>
    /// <param name="permission">Required permission to access this menu (optional).</param>
    /// <param name="commandName">Command name for permission override checking (optional).</param>
    /// <param name="moduleLocalizer">Module's IStringLocalizer for per-player translation.</param>
    public void RegisterMenu(string categoryId, string menuId, string menuNameKey, Func<CCSPlayerController, MenuBuilder> menuFactory, string? permission, string? commandName, Microsoft.Extensions.Localization.IStringLocalizer moduleLocalizer)
    {
        if (!_menuCategories.ContainsKey(categoryId))
        {
            RegisterCategory(categoryId, categoryId); // Auto-create category if it doesn't exist
        }

        _menuCategories[categoryId].MenuFactories[menuId] = menuFactory;
        _menuCategories[categoryId].MenuNames[menuId] = menuNameKey;  // Store the key
        _menuCategories[categoryId].MenuLocalizers[menuId] = moduleLocalizer;  // Store localizer
        if (permission != null)
        {
            _menuCategories[categoryId].MenuPermissions[menuId] = permission;
        }
        if (commandName != null)
        {
            _menuCategories[categoryId].MenuCommandNames[menuId] = commandName;
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
        _menuCategories[categoryId].MenuCommandNames.Remove(menuId);
    }

    /// <summary>
    /// Creates the main admin menu for a player with accessible categories.
    /// </summary>
    /// <param name="player">The player to create the menu for.</param>
    /// <returns>A MenuBuilder instance for the main menu.</returns>
    public MenuBuilder CreateMainMenu(CCSPlayerController player)
    {
        var localizer = CS2_SimpleAdmin._localizer;
        var mainMenu = new MenuBuilder("sa_title", player, localizer);

        foreach (var category in _menuCategories.Values)
        {
            if (category.MenuFactories.Count <= 0) continue;
            // Check category permissions
            var steamId = new SteamID(player.SteamID);
            if (!AdminManager.PlayerHasPermissions(steamId, category.Permission))
                continue;

            // Get localized category name for this player
            // If category has a module localizer, use it; otherwise use main plugin localizer
            string localizedCategoryName;
            using (new WithTemporaryCulture(player.GetLanguage()))
            {
                if (category.ModuleLocalizer != null)
                {
                    localizedCategoryName = category.ModuleLocalizer[category.Name] ?? category.Name;
                }
                else
                {
                    localizedCategoryName = localizer?[category.Name] ?? category.Name;
                }
            }

            // Pass player to CreateCategoryMenu
            mainMenu.AddSubMenu(localizedCategoryName, () => CreateCategoryMenu(category, player),
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
        var localizer = CS2_SimpleAdmin._localizer;

        // Get localized category name for this player
        // If category has a module localizer, use it; otherwise use main plugin localizer
        string localizedCategoryName;
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            if (category.ModuleLocalizer != null)
            {
                localizedCategoryName = category.ModuleLocalizer[category.Name] ?? category.Name;
            }
            else
            {
                localizedCategoryName = localizer?[category.Name] ?? category.Name;
            }
        }

        var categoryMenu = new MenuBuilder(localizedCategoryName);

        foreach (var kvp in category.MenuFactories)
        {
            var menuId = kvp.Key;
            var menuFactory = kvp.Value;
            var menuName = category.MenuNames.TryGetValue(menuId, out var name) ? name : menuId;
            var permission = category.MenuPermissions.TryGetValue(menuId, out var perm) ? perm : null;
            var commandName = category.MenuCommandNames.TryGetValue(menuId, out var cmd) ? cmd : null;

            // Check permissions with command override support
            var steamId = new SteamID(player.SteamID);

            // If commandName is provided, check for permission overrides
            if (!string.IsNullOrEmpty(commandName))
            {
                bool hasPermission;

                // Check if command has overridden permissions
                if (AdminManager.CommandIsOverriden(commandName))
                {
                    var overriddenPermission = AdminManager.GetPermissionOverrides(commandName);
                    hasPermission = AdminManager.PlayerHasPermissions(steamId, overriddenPermission);
                }
                else if (!string.IsNullOrEmpty(permission))
                {
                    // Use default permission if no override exists
                    hasPermission = AdminManager.PlayerHasPermissions(steamId, permission);
                }
                else
                {
                    // No permission required
                    hasPermission = true;
                }

                if (!hasPermission)
                    continue;
            }
            // Fallback to standard permission check if no commandName provided
            else if (!string.IsNullOrEmpty(permission))
            {
                if (!AdminManager.PlayerHasPermissions(steamId, permission))
                    continue;
            }

            // Get localized menu name for this player
            // If menu has its own localizer, use it; otherwise use category or main plugin localizer
            string localizedMenuName;
            using (new WithTemporaryCulture(player.GetLanguage()))
            {
                if (category.MenuLocalizers.TryGetValue(menuId, out var menuLocalizer))
                {
                    // Menu has its own module localizer
                    localizedMenuName = menuLocalizer[menuName] ?? menuName;
                }
                else if (category.ModuleLocalizer != null)
                {
                    // Use category's module localizer
                    localizedMenuName = category.ModuleLocalizer[menuName] ?? menuName;
                }
                else
                {
                    // Use main plugin localizer
                    localizedMenuName = localizer?[menuName] ?? menuName;
                }
            }

            // Call the actual factory with player parameter
            categoryMenu.AddSubMenu(localizedMenuName, () => menuFactory(player), permission: permission);
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
        // Register categories with translation keys instead of translated names
        // The actual translation will happen per-player in CreateMainMenu/CreateCategoryMenu
        RegisterCategory("players", "sa_menu_players_manage", "@css/generic");
        RegisterCategory("server", "sa_menu_server_manage", "@css/generic");
        // RegisterCategory("fun", "sa_menu_fun_commands", "@css/generic");
        RegisterCategory("admin", "sa_menu_admins_manage", "@css/root");
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
    public Dictionary<string, string> MenuCommandNames { get; set; } = [];

    // 🆕 NEW: Support for per-player localization in modules
    /// <summary>
    /// Optional IStringLocalizer from external module for per-player translation of category name.
    /// If null, Name is used as-is (for CS2-SimpleAdmin's built-in categories with translation keys).
    /// </summary>
    public Microsoft.Extensions.Localization.IStringLocalizer? ModuleLocalizer { get; set; }

    /// <summary>
    /// Stores IStringLocalizer for each menu that uses module localization.
    /// Key: menuId, Value: module's localizer
    /// </summary>
    public Dictionary<string, Microsoft.Extensions.Localization.IStringLocalizer> MenuLocalizers { get; set; } = [];
}
