# CS2-SimpleAdmin

> **Manage your Counter-Strike 2 server with simple commands!**  
> CS2-SimpleAdmin is a plugin designed to help you easily manage your Counter-Strike 2 server with user-friendly commands. Whether you're banning players, managing teams, or configuring server settings, CS2-SimpleAdmin has you covered.

## 📜 Features
- **Simple Commands**: Manage your server with an easy-to-use command system.
- **MySQL Integration**: Store and retrieve player data seamlessly with MySQL.
- **Ongoing Development**: New features and improvements are added whenever possible.
- **Performance Optimization**: Lightweight and optimized for minimal impact on server performance.
- **Extensible API**: Extend the functionality of CS2-SimpleAdmin by integrating it with your own plugins using the API. Create custom commands, automate tasks, and interact with the plugin programmatically to meet the specific needs of your server.

## ⚙️ Requirements
**Ensure all the following dependencies are installed before proceeding**
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)  
- [PlayerSettings](https://github.com/NickFox007/PlayerSettingsCS2) - Required by MenuManagerCS2
- [AnyBaseLibCS2](https://github.com/NickFox007/AnyBaseLibCS2) - Required by PlayerSettings
- [MenuManagerCS2](https://github.com/NickFox007/MenuManagerCS2)
- MySQL database

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

## 📁 Configuration
The configuration file (`CS2-SimpleAdmin.json`) will be auto-generated after the first launch. It contains settings for MySQL connections, command permissions, and other plugin-specific configurations.

## 📙 Wiki
For detailed documentation, guides, and tutorials, please visit [Wiki](https://cs2-simpleadmin.daffyy.dev).

## 🛠️ Development
This project started as a base for other plugins but has grown into a standalone admin management tool. Contributions are welcome! If you'd like to help with development or have ideas for new features, feel free to submit a pull request or open an issue.

## 💡 Credits
This project is inspired by the work of [Hackmastr](https://github.com/Hackmastr/css-basic-admin/). Thanks for laying the groundwork!

## ❤️ Support the Project
If you find this plugin helpful and would like to support further development, consider buying me a cup of tea! Your support is greatly appreciated.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Y8Y4THKXG)

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
