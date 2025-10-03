<p align="center">
  <a href="https://github.com/daffyyyy/CS2-SimpleAdmin/actions/workflows/build.yml">
    <img src="https://github.com/daffyyyy/CS2-SimpleAdmin/actions/workflows/build.yml/badge.svg" alt="Build and Publish" />
  </a>
<a href="https://github.com/daffyyyy/CS2-SimpleAdmin/releases/latest">
  <img src="https://img.shields.io/github/v/release/daffyyyy/CS2-SimpleAdmin?color=orange" alt="GitHub Release" />
</a>
  <img src="https://img.shields.io/badge/Made_with-a_lot_of_tea_%F0%9F%8D%B5-red" alt="Made with a lot of tea 🍵" />
</p>

# CS2-SimpleAdmin

---

> **Manage your Counter-Strike 2 server with simple commands!**  
> CS2-SimpleAdmin is a plugin designed to help you easily manage your Counter-Strike 2 server with user-friendly commands. Whether you're banning players, managing teams, or configuring server settings, CS2-SimpleAdmin has you covered.

---

## 🚀 Features

- 🎮 **Simple, Intuitive Commands:** Manage players, teams, bans, and server settings using an easy command system.
- 🗄 **Full Database Integration:** Reliable MySQL backend with optional experimental SQLite support for persistent data storage.
- ⚡ **Efficient and Lightweight:** Designed to minimize server resource usage while maintaining robust functionality.
- 🚨 **Real-time Notifications:** Instant in-game and Discord notifications to keep admins and players informed.
- 🎛 **User-Friendly Admin Interface:** Plugin menus for quick and efficient management without complex commands.
- 🔄 **Multi-Server Compatibility:** Manage and sync data across multiple servers seamlessly.
- 🧩 **Modular and Extensible:** Tailor the plugin with API access to add custom commands and automation.
- 📜 **Complete Auditing Logs:** Track all administrative actions rigorously for accountability.
- 🌐 **Discord Integration:** Stream logs and alerts to your Discord channels for centralized monitoring.
- 🤝 **Community Driven:** Open-source with ongoing contributions from a passionate community.

---

## ⚙️ Requirements

**Ensure all the following dependencies are installed before proceeding**
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)  
- [PlayerSettings](https://github.com/NickFox007/PlayerSettingsCS2) - Required by MenuManagerCS2
- [AnyBaseLibCS2](https://github.com/NickFox007/AnyBaseLibCS2) - Required by PlayerSettings
- [MenuManagerCS2](https://github.com/NickFox007/MenuManagerCS2)
- MySQL database / SQLite

---

## 🚀 Getting Started

1. **Clone or Download the Repository**:  
   Download or clone the repository and publish to your `addons/counterstrikesharp/plugins/` directory.

2. **First Launch Configuration**:  
   On the first launch, a configuration file will be generated at:
   ```
   addons/counterstrikesharp/configs/plugins/CS2-SimpleAdmin/CS2-SimpleAdmin.json
   ```
   Edit this file to customize the plugin settings according to your server needs.

3. **Enjoy Managing Your Server!**  
   Use the commands provided by the plugin to easily manage your server.

---

## 📁 Configuration

The configuration file (`CS2-SimpleAdmin.json`) will be auto-generated after the first launch. It contains settings for MySQL connections, command permissions, and other plugin-specific configurations.

---

## 📚 Documentation & Support

Access full documentation, guides, tutorials, and developer API references here:  
[CS2-SimpleAdmin Wiki](https://cs2-simpleadmin.daffyy.dev)

---

## 🛠️ Development

This project started as a base for other plugins but has grown into a standalone admin management tool. Contributions are welcome! If you'd like to help with development or have ideas for new features, feel free to submit a pull request or open an issue.

---

## 🤝 Contributing & Feedback

Help improve CS2-SimpleAdmin by:
- Reporting bugs or requesting features on GitHub
- Submitting pull requests with improvements
- Participating in discussions and sharing ideas

---

## 💡 Credits

This project is inspired by the work of [Hackmastr](https://github.com/Hackmastr/css-basic-admin/). Thanks for laying the groundwork!

---

## ☕ Support Development

If you find CS2-SimpleAdmin useful, consider supporting the ongoing development:  
[![Buy me a coffee](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Y8Y4THKXG)

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
