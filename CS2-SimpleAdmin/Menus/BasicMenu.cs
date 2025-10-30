using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_SimpleAdminApi;

namespace CS2_SimpleAdmin.Menus;

public abstract class BasicMenu
{
        /// <summary>
        /// Initializes all menus in the system by registering them with the MenuManager.
        /// Register with translation keys instead of static names - translation happens per-player.
        /// </summary>
        public static void Initialize()
        {
            var manager = MenuManager.Instance;

            // Players category menus - using translation keys
            manager.RegisterMenu("players", "slap", "sa_slap", CreateSlapMenu, "@css/slay");
            manager.RegisterMenu("players", "slay", "sa_slay", CreateSlayMenu, "@css/slay");
            manager.RegisterMenu("players", "kick", "sa_kick", CreateKickMenu, "@css/kick");
            manager.RegisterMenu("players", "warn", "sa_warn", CreateWarnMenu, "@css/kick");
            manager.RegisterMenu("players", "ban", "sa_ban", CreateBanMenu, "@css/ban");
            manager.RegisterMenu("players", "gag", "sa_gag", CreateGagMenu, "@css/chat");
            manager.RegisterMenu("players", "mute", "sa_mute", CreateMuteMenu, "@css/chat");
            manager.RegisterMenu("players", "silence", "sa_silence", CreateSilenceMenu, "@css/chat");
            manager.RegisterMenu("players", "team", "sa_team_force", CreateForceTeamMenu, "@css/kick");

            // Server category menus - using translation keys
            manager.RegisterMenu("server", "plugins", "sa_menu_pluginsmanager_title", CreatePluginsMenu, "@css/root");
            manager.RegisterMenu("server", "changemap", "sa_changemap", CreateChangeMapMenu, "@css/changemap");
            manager.RegisterMenu("server", "restart", "sa_restart_game", CreateRestartGameMenu, "@css/generic");
            manager.RegisterMenu("server", "custom", "sa_menu_custom_commands", CreateCustomCommandsMenu, "@css/generic");

            // Admin category menus - using translation keys
            manager.RegisterMenu("admin", "add", "sa_admin_add", CreateAddAdminMenu, "@css/root");
            manager.RegisterMenu("admin", "remove", "sa_admin_remove", CreateRemoveAdminMenu, "@css/root");
            manager.RegisterMenu("admin", "reload", "sa_admin_reload", CreateReloadAdminsMenu, "@css/root");
        }

        /// <summary>
        /// Creates menu for slapping players with selectable damage amounts.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the slap menu.</returns>

        private static MenuBuilder CreateSlapMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var slapMenu = new MenuBuilder("sa_slap", admin, localizer);

            var players = Helper.GetValidPlayers().Where(admin.CanTarget);

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                slapMenu.AddSubMenu(playerName, () => CreateSlapDamageMenu(admin, player));
            }

            return slapMenu.WithBackButton();
        }

        /// <summary>
        /// Creates damage selection submenu for slapping a specific player.
        /// </summary>
        /// <param name="admin">The admin player executing the slap.</param>
        /// <param name="target">The target player to be slapped.</param>
        /// <returns>A MenuBuilder instance for the slap damage menu.</returns>
        private static MenuBuilder CreateSlapDamageMenu(CCSPlayerController admin, CCSPlayerController target)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            string localizedTitle;
            using (new WithTemporaryCulture(admin.GetLanguage()))
            {
                localizedTitle = $"{localizer?["sa_slap"] ?? "Slap"}: {target.PlayerName}";
            }

            var slapDamageMenu = new MenuBuilder(localizedTitle);
            var damages = new[] { 0, 1, 5, 10, 50, 100 };

            foreach (var damage in damages)
            {
                slapDamageMenu.AddOption($"{damage} HP", currentAdmin =>
                {
                    if (target.IsValid)
                    {
                        CS2_SimpleAdmin.Slap(currentAdmin, target, damage);
                        // Reopen the same menu (not create new one) to keep back button working
                        slapDamageMenu.OpenMenu(currentAdmin);
                    }
                });
            }

            return slapDamageMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for slaying (killing) players.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the slay menu.</returns>
        private static MenuBuilder CreateSlayMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var slayMenu = new MenuBuilder("sa_slay", admin, localizer);

            var players = Helper.GetValidPlayers().Where(admin.CanTarget);

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                slayMenu.AddOption(playerName, _ =>
                {
                    if (player.IsValid)
                    {
                        CS2_SimpleAdmin.Slay(admin, player);
                    }
                });
            }

            return slayMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for kicking players with reason selection.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the kick menu.</returns>
        private static MenuBuilder CreateKickMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var kickMenu = new MenuBuilder("sa_kick", admin, localizer);

            var players = Helper.GetValidPlayers().Where(p => !p.IsBot && admin.CanTarget(p));

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                kickMenu.AddSubMenu(playerName, () => CreateReasonMenu(admin, player, "Kick", PenaltyType.Kick,
                    (_, _, reason) =>
                    {
                        if (player.IsValid)
                        {
                            CS2_SimpleAdmin.Instance.Kick(admin, player, reason, admin.PlayerName);
                        }
                    }));
            }

            return kickMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for warning players with duration and reason selection.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the warn menu.</returns>
        private static MenuBuilder CreateWarnMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var warnMenu = new MenuBuilder("sa_warn", admin, localizer);

            var players = Helper.GetValidPlayers().Where(p => !p.IsBot && admin.CanTarget(p));

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                warnMenu.AddSubMenu(playerName, () => CreateDurationMenu(admin, player, "Warn", 
                    (_, _, duration) => CreateReasonMenu(admin, player, "Warn", PenaltyType.Warn,
                        (_, _, reason) =>
                        {
                            if (player.IsValid)
                            {
                                CS2_SimpleAdmin.Instance.Warn(admin, player, duration, reason, admin.PlayerName);
                            }
                        })));
            }

            return warnMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for banning players with duration and reason selection.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the ban menu.</returns>
        private static MenuBuilder CreateBanMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var banMenu = new MenuBuilder("sa_ban", admin, localizer);

            var players = Helper.GetValidPlayers().Where(p => !p.IsBot && admin.CanTarget(p));

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                banMenu.AddSubMenu(playerName, () => CreateDurationMenu(admin, player, "Ban", 
                    (_, _, duration) => CreateReasonMenu(admin, player, "Ban", PenaltyType.Ban,
                        (_, _, reason) =>
                        {
                            if (player.IsValid)
                            {
                                CS2_SimpleAdmin.Instance.Ban(admin, player, duration, reason, admin.PlayerName);
                            }
                        })));
            }

            return banMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for gagging (text chat muting) players with duration and reason selection.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the gag menu.</returns>
        private static MenuBuilder CreateGagMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var gagMenu = new MenuBuilder("sa_gag", admin, localizer);

            var players = Helper.GetValidPlayers().Where(p => !p.IsBot && admin.CanTarget(p));

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                gagMenu.AddSubMenu(playerName, () => CreateDurationMenu(admin, player, "Gag", 
                    (_, _, duration) => CreateReasonMenu(admin, player, "Gag", PenaltyType.Gag,
                        (_, _, reason) =>
                        {
                            if (player.IsValid)
                            {
                                CS2_SimpleAdmin.Instance.Gag(admin, player, duration, reason);
                            }
                        })));
            }

            return gagMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for muting (voice chat muting) players with duration and reason selection.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the mute menu.</returns>
        private static MenuBuilder CreateMuteMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var muteMenu = new MenuBuilder("sa_mute", admin, localizer);

            var players = Helper.GetValidPlayers().Where(p => !p.IsBot && admin.CanTarget(p));

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                muteMenu.AddSubMenu(playerName, () => CreateDurationMenu(admin, player, "Mute", 
                    (_, _, duration) => CreateReasonMenu(admin, player, "Mute", PenaltyType.Mute,
                        (_, _, reason) =>
                        {
                            if (player.IsValid)
                            {
                                CS2_SimpleAdmin.Instance.Mute(admin, player, duration, reason);
                            }
                        })));
            }

            return muteMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for silencing (both text and voice chat muting) players with duration and reason selection.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the silence menu.</returns>
        private static MenuBuilder CreateSilenceMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var silenceMenu = new MenuBuilder("sa_silence", admin, localizer);

            var players = Helper.GetValidPlayers().Where(p => !p.IsBot && admin.CanTarget(p));

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                silenceMenu.AddSubMenu(playerName, () => CreateDurationMenu(admin, player, "Silence", 
                    (_, _, duration) => CreateReasonMenu(admin, player, "Silence", PenaltyType.Silence,
                        (_, _, reason) =>
                        {
                            if (player.IsValid)
                            {
                                CS2_SimpleAdmin.Instance.Silence(admin, player, duration, reason);
                            }
                        })));
            }

            return silenceMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for forcing players to switch teams.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the force team menu.</returns>
        private static MenuBuilder CreateForceTeamMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var teamMenu = new MenuBuilder("sa_team_force", admin, localizer);

            var players = Helper.GetValidPlayers().Where(p => admin.CanTarget(p));

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                teamMenu.AddSubMenu(playerName, () => CreateTeamSelectionMenu(admin, player));
            }

            return teamMenu.WithBackButton();
        }

        /// <summary>
        /// Creates team selection submenu for forcing a specific player to a team.
        /// </summary>
        /// <param name="admin">The admin player executing the team change.</param>
        /// <param name="target">The target player to be moved.</param>
        /// <returns>A MenuBuilder instance for the team selection menu.</returns>
        private static MenuBuilder CreateTeamSelectionMenu(CCSPlayerController admin, CCSPlayerController target)
        {
            var localizer = CS2_SimpleAdmin._localizer;

            // Localize title for admin's language
            string localizedTitle;
            using (new WithTemporaryCulture(admin.GetLanguage()))
            {
                localizedTitle = $"{localizer?["sa_team_force"] ?? "Force Team"}: {target.PlayerName}";
            }

            var teamSelectionMenu = new MenuBuilder(localizedTitle);

            // Localize team options for admin's language
            string ctName, tName, swapName, specName;
            using (new WithTemporaryCulture(admin.GetLanguage()))
            {
                ctName = localizer?["sa_team_ct"] ?? "CT";
                tName = localizer?["sa_team_t"] ?? "T";
                swapName = localizer?["sa_team_swap"] ?? "Swap";
                specName = localizer?["sa_team_spec"] ?? "Spec";
            }

            var teams = new[]
            {
                (ctName, "ct", CsTeam.CounterTerrorist),
                (tName, "t", CsTeam.Terrorist),
                (swapName, "swap", CsTeam.Spectator),
                (specName, "spec", CsTeam.Spectator)
            };

            foreach (var (name, teamName, teamNum) in teams)
            {
                teamSelectionMenu.AddOption(name, _ =>
                {
                    if (target.IsValid)
                    {
                        CS2_SimpleAdmin.ChangeTeam(admin, target, teamName, teamNum, true);
                    }
                });
            }

            return teamSelectionMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for managing server plugins.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the plugins menu.</returns>
        private static MenuBuilder CreatePluginsMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var pluginsMenu = new MenuBuilder("sa_menu_pluginsmanager_title", admin, localizer);

            pluginsMenu.AddOption("Open Plugins Manager", _ =>
            {
                admin.ExecuteClientCommandFromServer("css_pluginsmanager");
            });

            return pluginsMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for changing the current map (includes default and workshop maps).
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the change map menu.</returns>
        private static MenuBuilder CreateChangeMapMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var mapMenu = new MenuBuilder("sa_changemap", admin, localizer);

            // Add default maps
            var maps = CS2_SimpleAdmin.Instance.Config.DefaultMaps;
            foreach (var map in maps)
            {
                mapMenu.AddOption(map, _ =>
                {
                    CS2_SimpleAdmin.Instance.ChangeMap(admin, map);
                });
            }

            // Add workshop maps
            var wsMaps = CS2_SimpleAdmin.Instance.Config.WorkshopMaps;
            foreach (var wsMap in wsMaps)
            {
                mapMenu.AddOption($"{wsMap.Key} (WS)", _ =>
                {
                    CS2_SimpleAdmin.Instance.ChangeWorkshopMap(admin, wsMap.Value?.ToString() ?? wsMap.Key);
                });
            }

            return mapMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for restarting the current game/round.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the restart game menu.</returns>
        private static MenuBuilder CreateRestartGameMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var restartMenu = new MenuBuilder("sa_restart_game", admin, localizer);

            restartMenu.AddOption("Restart Round", _ =>
            {
                CS2_SimpleAdmin.RestartGame(admin);
            });

            return restartMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for executing custom server commands defined in configuration.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the custom commands menu.</returns>
        private static MenuBuilder CreateCustomCommandsMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var customMenu = new MenuBuilder("sa_menu_custom_commands", admin, localizer);

            var customCommands = CS2_SimpleAdmin.Instance.Config.CustomServerCommands;
            
            foreach (var customCommand in customCommands)
            {
                if (string.IsNullOrEmpty(customCommand.DisplayName) || string.IsNullOrEmpty(customCommand.Command))
                    continue;
                    
                var steamId = new SteamID(admin.SteamID);
                if (!AdminManager.PlayerHasPermissions(steamId, customCommand.Flag))
                    continue;

                customMenu.AddOption(customCommand.DisplayName, _ =>
                {
                    Helper.TryLogCommandOnDiscord(admin, customCommand.Command);
                    
                    if (customCommand.ExecuteOnClient)
                        admin.ExecuteClientCommandFromServer(customCommand.Command);
                    else
                        Server.ExecuteCommand(customCommand.Command);
                });
            }

            return customMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for adding admin privileges to players.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the add admin menu.</returns>
        private static MenuBuilder CreateAddAdminMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var addAdminMenu = new MenuBuilder("sa_admin_add", admin, localizer);

            var players = Helper.GetValidPlayers().Where(p => !p.IsBot && admin.CanTarget(p));

            foreach (var player in players)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                addAdminMenu.AddSubMenu(playerName, () => CreateAdminFlagsMenu(admin, player));
            }

            return addAdminMenu.WithBackButton();
        }

        /// <summary>
        /// Creates admin flags selection submenu for granting specific permissions to a player.
        /// </summary>
        /// <param name="admin">The admin player granting permissions.</param>
        /// <param name="target">The target player to receive admin privileges.</param>
        /// <returns>A MenuBuilder instance for the admin flags menu.</returns>
        private static MenuBuilder CreateAdminFlagsMenu(CCSPlayerController admin, CCSPlayerController target)
        {
            var localizer = CS2_SimpleAdmin._localizer;

            // Localize title for admin's language
            string localizedTitle;
            using (new WithTemporaryCulture(admin.GetLanguage()))
            {
                localizedTitle = $"{localizer?["sa_admin_add"] ?? "Add Admin"}: {target.PlayerName}";
            }

            var flagsMenu = new MenuBuilder(localizedTitle);

            foreach (var adminFlag in CS2_SimpleAdmin.Instance.Config.MenuConfigs.AdminFlags)
            {
                var hasFlag = AdminManager.PlayerHasPermissions(target, adminFlag.Flag);
                flagsMenu.AddOption(adminFlag.Name, _ =>
                {
                    if (target.IsValid)
                    {
                        CS2_SimpleAdmin.AddAdmin(admin, target.SteamID.ToString(), target.PlayerName, adminFlag.Flag, 10);
                    }
                }, hasFlag); // Disabled if player already has this flag
            }

            return flagsMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for removing admin privileges from players.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the remove admin menu.</returns>
        private static MenuBuilder CreateRemoveAdminMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var removeAdminMenu = new MenuBuilder("sa_admin_remove", admin, localizer);

            var adminPlayers = Helper.GetValidPlayers().Where(p => 
                AdminManager.GetPlayerAdminData(p)?.Flags.Count > 0 && 
                p != admin && 
                admin.CanTarget(p));
            
            foreach (var player in adminPlayers)
            {
                var playerName = player.PlayerName.Length > 26 ? player.PlayerName[..26] : player.PlayerName;
                removeAdminMenu.AddOption(playerName, _ =>
                {
                    if (player.IsValid)
                    {
                        CS2_SimpleAdmin.Instance.RemoveAdmin(admin, player.SteamID.ToString());
                    }
                });
            }

            return removeAdminMenu.WithBackButton();
        }

        /// <summary>
        /// Creates menu for reloading admin list from database.
        /// </summary>
        /// <param name="admin">The admin player opening the menu.</param>
        /// <returns>A MenuBuilder instance for the reload admins menu.</returns>
        private static MenuBuilder CreateReloadAdminsMenu(CCSPlayerController admin)
        {
            var localizer = CS2_SimpleAdmin._localizer;
            var reloadMenu = new MenuBuilder("sa_admin_reload", admin, localizer);

            reloadMenu.AddOption("Reload Admins", _ =>
            {
                CS2_SimpleAdmin.Instance.ReloadAdmins(admin);
            });

            return reloadMenu.WithBackButton();
        }

        /// <summary>
        /// Creates duration selection submenu for time-based penalties (ban, mute, gag, etc.).
        /// </summary>
        /// <param name="admin">The admin player selecting duration.</param>
        /// <param name="player">The target player for the penalty.</param>
        /// <param name="actionName">The name of the penalty action (e.g., "Kick", "Ban").</param>
        /// <param name="onSelectAction">Callback action executed when duration is selected.</param>
        /// <returns>A MenuBuilder instance for the duration menu.</returns>
        private static MenuBuilder CreateDurationMenu(CCSPlayerController admin, CCSPlayerController player, string actionName,
            Action<CCSPlayerController, CCSPlayerController, int> onSelectAction)
        {
            var localizer = CS2_SimpleAdmin._localizer;

            // Convert action name to translation key (e.g., "Ban" -> "sa_ban")
            var actionKey = actionName.ToLower() switch
            {
                "kick" => "sa_kick",
                "ban" => "sa_ban",
                "warn" => "sa_warn",
                "gag" => "sa_gag",
                "mute" => "sa_mute",
                "silence" => "sa_silence",
                _ => actionName
            };

            // Localize title for admin's language
            string localizedAction, durationText;
            using (new WithTemporaryCulture(admin.GetLanguage()))
            {
                localizedAction = localizer?[actionKey] ?? actionName;
                durationText = localizer?["sa_duration"] ?? "Duration";
            }

            var durationMenu = new MenuBuilder($"{localizedAction} {durationText}: {player.PlayerName}");

            foreach (var durationItem in CS2_SimpleAdmin.Instance.Config.MenuConfigs.Durations)
            {
                durationMenu.AddOption(durationItem.Name, _ =>
                {
                    onSelectAction(admin, player, durationItem.Duration);
                });
            }

            return durationMenu.WithBackButton();
        }

        /// <summary>
        /// Creates reason selection submenu for penalties with predefined reasons from configuration.
        /// </summary>
        /// <param name="admin">The admin player selecting reason.</param>
        /// <param name="player">The target player for the penalty.</param>
        /// <param name="actionName">The name of the penalty action (e.g., "Kick", "Ban").</param>
        /// <param name="penaltyType">The type of penalty to determine which reason list to use.</param>
        /// <param name="onSelectAction">Callback action executed when reason is selected.</param>
        /// <returns>A MenuBuilder instance for the reason menu.</returns>
        private static MenuBuilder CreateReasonMenu(CCSPlayerController admin, CCSPlayerController player, string actionName,
            PenaltyType penaltyType, Action<CCSPlayerController, CCSPlayerController, string> onSelectAction)
        {
            var localizer = CS2_SimpleAdmin._localizer;

            // Convert action name to translation key
            var actionKey = actionName.ToLower() switch
            {
                "kick" => "sa_kick",
                "ban" => "sa_ban",
                "warn" => "sa_warn",
                "gag" => "sa_gag",
                "mute" => "sa_mute",
                "silence" => "sa_silence",
                _ => actionName
            };

            // Localize title for admin's language
            string localizedAction, reasonText;
            using (new WithTemporaryCulture(admin.GetLanguage()))
            {
                localizedAction = localizer?[actionKey] ?? actionName;
                reasonText = localizer?["sa_reason"] ?? "Reason";
            }

            var reasonMenu = new MenuBuilder($"{localizedAction} {reasonText}: {player.PlayerName}");

            var reasons = penaltyType switch
            {
                PenaltyType.Ban => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons,
                PenaltyType.Kick => CS2_SimpleAdmin.Instance.Config.MenuConfigs.KickReasons,
                PenaltyType.Mute => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
                PenaltyType.Warn => CS2_SimpleAdmin.Instance.Config.MenuConfigs.WarnReasons,
                PenaltyType.Gag or PenaltyType.Silence => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
                _ => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons
            };

            foreach (var reason in reasons)
            {
                reasonMenu.AddOption(reason, _ =>
                {
                    onSelectAction(admin, player, reason);
                });
            }

            return reasonMenu.WithBackButton();
        }
    }
