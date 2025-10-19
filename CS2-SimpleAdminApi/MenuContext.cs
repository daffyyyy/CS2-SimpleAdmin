using CounterStrikeSharp.API.Core;

namespace CS2_SimpleAdminApi;

/// <summary>
/// Provides contextual information about a menu to its factory function.
/// This eliminates the need to duplicate category IDs and menu titles when creating menus.
/// </summary>
public class MenuContext
{
    /// <summary>
    /// The category ID this menu belongs to (e.g., "fun", "players").
    /// Used for automatic "Back" button navigation.
    /// </summary>
    public string CategoryId { get; init; } = string.Empty;

    /// <summary>
    /// The unique identifier for this menu within its category.
    /// </summary>
    public string MenuId { get; init; } = string.Empty;

    /// <summary>
    /// The display title of the menu (from registration).
    /// </summary>
    public string MenuTitle { get; init; } = string.Empty;

    /// <summary>
    /// The permission required to access this menu (if any).
    /// </summary>
    public string? Permission { get; init; }

    /// <summary>
    /// The command name for permission override checking (if any).
    /// </summary>
    public string? CommandName { get; init; }

    /// <summary>
    /// Creates a new MenuContext with the specified values.
    /// </summary>
    public MenuContext(string categoryId, string menuId, string menuTitle, string? permission = null, string? commandName = null)
    {
        CategoryId = categoryId;
        MenuId = menuId;
        MenuTitle = menuTitle;
        Permission = permission;
        CommandName = commandName;
    }
}
