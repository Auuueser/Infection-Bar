<h1 align="center">Infection Bar</h1>

<p align="center">
  <img alt="版本 1.2.0" src="https://img.shields.io/badge/版本-1.2.0-E35B18?style=flat-square&amp;labelColor=24282C" height="22">
  <img alt="Lethal Company V81" src="https://img.shields.io/badge/游戏-V81-E35B18?style=flat-square&amp;labelColor=24282C" height="22">
  <img alt="BepInEx 5" src="https://img.shields.io/badge/运行环境-BepInEx%205-526D82?style=flat-square&amp;labelColor=24282C" height="22">
  <a href="https://github.com/Auuueser/Infection-Bar/blob/main/LICENSE"><img alt="GNU GPL-3.0" src="https://img.shields.io/badge/许可-GPL--3.0-E35B18?style=flat-square&amp;labelColor=24282C" height="22"></a>
</p>

<p align="center">
  <a href="https://github.com/Auuueser/Infection-Bar/blob/main/CHANGELOG.md"><img alt="更新日志 / Changelog" src="https://img.shields.io/badge/%E6%9B%B4%E6%96%B0%E6%97%A5%E5%BF%97-Changelog-E35B18?style=flat-square&amp;labelColor=24282C" height="22"></a>
  <a href="https://github.com/Auuueser/Infection-Bar/issues"><img alt="反馈 / Issues" src="https://img.shields.io/badge/%E5%8F%8D%E9%A6%88-Issues-E35B18?style=flat-square&amp;labelColor=24282C" height="22"></a>
  <a href="https://github.com/Auuueser/Infection-Bar"><img alt="正式源码 / Source" src="https://img.shields.io/badge/%E6%AD%A3%E5%BC%8F%E6%BA%90%E7%A0%81-Source-E35B18?style=flat-square&amp;labelColor=24282C" height="22"></a>
</p>

<details>
<summary><strong>中文</strong></summary>

显示尸体感染百分比，支持原版 HUD 与 EladsHUD。

<table width="100%">
  <tr><th>原版 HUD</th><th>EladsHUD</th></tr>
  <tr>
    <td width="50%"><img src="https://raw.githubusercontent.com/Auuueser/Infection-Bar/main/docs/media/vanilla-zh.gif" alt="原版 HUD" width="100%"></td>
    <td width="50%"><img src="https://raw.githubusercontent.com/Auuueser/Infection-Bar/main/docs/media/eladshud-zh.gif" alt="EladsHUD" width="100%"></td>
  </tr>
</table>

### 安装

需要 **BepInEx 5**。将 DLL 放入 `BepInEx/plugins/InfectionBar/`。

启动后生成 `BepInEx/config/InfectionBar.cfg`；安装 **LethalConfig** 可在局内实时调整。

### 显示与兼容

- **语言**：自动、中文、英文。自动模式检测到 LC Chinese Project 时使用中文，否则英文。
- **多人**：主机与所有玩家均需安装。未安装者加入后自动隐藏；其退出且剩余玩家完成握手后恢复，不影响玩家连接。

反馈请附复现步骤与 `BepInEx/LogOutput.log`；布局问题请附截图。

</details>

<details>
<summary><strong>English</strong></summary>

Displays Cadaver Growth infection percentages with vanilla HUD and EladsHUD support.

<table width="100%">
  <tr><th>Vanilla HUD</th><th>EladsHUD</th></tr>
  <tr>
    <td width="50%"><img src="https://raw.githubusercontent.com/Auuueser/Infection-Bar/main/docs/media/vanilla-en.gif" alt="Vanilla HUD" width="100%"></td>
    <td width="50%"><img src="https://raw.githubusercontent.com/Auuueser/Infection-Bar/main/docs/media/eladshud-en.gif" alt="EladsHUD" width="100%"></td>
  </tr>
</table>

### Installation

Requires **BepInEx 5**. Place the DLL in `BepInEx/plugins/InfectionBar/`.

Launching creates `BepInEx/config/InfectionBar.cfg`. Install **LethalConfig** for live in-game settings.

### Display and compatibility

- **Language**: Auto, Chinese or English. Auto uses Chinese when LC Chinese Project is detected, English otherwise.
- **Multiplayer**: the host and all players must install the mod. Missing installations hide the HUD; it returns once those players leave and everyone remaining completes the handshake. Player connections are unaffected.

Include reproduction steps and `BepInEx/LogOutput.log` with feedback; add screenshots for layout issues.

</details>
