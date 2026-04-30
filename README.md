# Infection Bar

Infection Bar is a BepInEx plugin for *Lethal Company* that displays Cadaver Growth infection progress as an in-game HUD element when the lobby supports it.

The project is intentionally narrow in scope: it renders a local HUD indicator, preserves the existing vanilla/EladsHUD presentation paths, and adds a lightweight host-required compatibility check. It does not synchronize infection values, add gameplay logic, block connections, or kick players.

## Multiplayer model

Infection Bar now runs in host-required compatibility mode.

- The host must have Infection Bar installed.
- Clients must also have Infection Bar installed for the HUD to become active.
- When the host is not running Infection Bar, installed clients stay connected and hide the local HUD.
- When the host is running Infection Bar but one or more connected clients are not, the host marks the lobby as incompatible, hides the local HUD, and reports that state to installed clients.
- Mid-session joins are allowed. A newly connected client receives an 8 second grace period before being treated as missing the mod.
- Lobby connectivity is not modified. The mod does not reject joins, disconnect clients, or affect movement/gameplay.

The compatibility layer uses Unity Netcode named messages through `CustomMessagingManager`:

- `InfectionBar_ClientHello_v1`
- `InfectionBar_HostState_v1`

Messages are rate-limited and are not sent every frame. No `NetworkObject` prefab is added.

## Features

- Displays Cadaver Growth infection progress as a percentage.
- Stops reading infection data while compatibility mode has disabled the HUD.
- Supports an always-visible mode for checking the HUD at 0%.
- Provides automatic HUD presentation selection.
- Uses a compact current-style bar in compatible HUD environments.
- Uses a vanilla stamina-ring style with the base game HUD.
- Fades during terminal use and follows global HUD visibility where appropriate.
- Supports automatic Chinese/English label selection, with manual override available in configuration.
- Includes diagnostic logging and debug-only live layout refresh options.

## HUD modes

`HudStyleMode` controls presentation:

- `Auto`: recommended. Uses the compact current-style presentation with EladsHUD-compatible environments and the vanilla stamina-ring presentation otherwise.
- `CurrentStyle`: forces the compact bar presentation.
- `VanillaStaminaRingStyle`: forces the vanilla stamina-ring presentation.

## EladsHUD interoperability

EladsHUD support is optional and runtime-only.

- Infection Bar does not require EladsHUD.
- Infection Bar does not include EladsHUD source code or assets.
- Infection Bar does not include third-party prefabs, images, shaders, materials, fonts, or asset bundles.
- Infection Bar is not an EladsHUD module and is not packaged with EladsHUD.
- Compatibility is handled through BepInEx plugin detection and this plugin's own rendering logic.

EladsHUD is mentioned only to describe interoperability behavior. This project is not affiliated with, endorsed by, maintained by, or packaged with EladsHUD or its forks.

## Configuration

The configuration file is generated after first launch:

```text
BepInEx/config/InfectionBar.cfg
```

Important entries:

- `InfectionBarEnabled`: enables or disables the infection display.
- `InfectionBarAlwaysVisible`: keeps the display visible even at 0%.
- `HudStyleMode`: selects automatic, compact, or vanilla HUD style.
- `LabelLanguageMode`: selects automatic, English, or Chinese labels.
- `TerminalFadeAlpha`: controls terminal fade alpha.
- `VanillaRingScale`, `VanillaRingOffsetX`, `VanillaRingOffsetY`: tune vanilla HUD mode placement.
- `VanillaWarningTextOffsetEnabled`, `VanillaWarningTextOffsetX`, `VanillaWarningTextOffsetY`: move original warning text in vanilla HUD mode to reduce overlap.
- `DebugLogging`: enables diagnostic logging.
- `DebugVanillaHudLiveLayoutRefresh`: debug-only high-frequency layout refresh for visual validation.

## Build

The project targets `netstandard2.1`.

Before building, update the local reference paths in `IndependentCadaverInfectionBar.csproj` if needed:

- `GameManagedDir`
- `BepInExCoreDir`

Build command:

```text
dotnet build IndependentCadaverInfectionBar.csproj -c Release
```

## Verification

The repository includes lightweight PowerShell checks for pure logic and compatibility state behavior:

```text
powershell -ExecutionPolicy Bypass -File tools\TestInfectionBarCompatibilityState.ps1
powershell -ExecutionPolicy Bypass -File tools\TestLanguageHelper.ps1
powershell -ExecutionPolicy Bypass -File tools\TestVanillaArcTextLayout.ps1
powershell -ExecutionPolicy Bypass -File tools\TestVanillaRingFillMapping.ps1
powershell -ExecutionPolicy Bypass -File tools\TestVanillaWarningTextOffset.ps1
```

Manual multiplayer testing is still required for host/client installation matrix coverage.

## Notes

- This is an unofficial community mod.
- *Lethal Company* and its assets belong to their respective owners.
- This project does not redistribute game assets or third-party HUD mod assets.
