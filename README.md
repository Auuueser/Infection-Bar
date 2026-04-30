# Infection Bar

Infection Bar is a BepInEx plugin for *Lethal Company* that displays Cadaver Growth infection progress as an in-game HUD element when the lobby supports it.

The project is intentionally narrow in scope. It renders a local HUD indicator from infection state that already exists in the running game, preserves the vanilla and EladsHUD-compatible presentation paths, and includes a lightweight host-required compatibility check. It does not add infection gameplay, exchange infection progress between players, block connections, or kick players.

## Multiplayer Model

Infection Bar runs in host-required compatibility mode.

- The host must have Infection Bar installed.
- Every connected client must also have Infection Bar installed for the HUD to become active.
- In lobbies hosted without Infection Bar, installed clients remain connected and the HUD stays hidden automatically.
- When the host has Infection Bar installed but one or more connected clients do not, the host hides its local HUD and reports that compatibility state to installed clients.
- Mid-session joins are allowed. Newly connected clients receive an 8 second grace period before compatibility is evaluated.
- Lobby connectivity is not modified. The mod does not reject joins, disconnect clients, or affect movement or gameplay.

The compatibility layer uses Unity Netcode named messages through `CustomMessagingManager`:

- `InfectionBar_ClientHello_v1`
- `InfectionBar_HostState_v1`

Messages are rate-limited and are not sent every frame. No `NetworkObject` prefab is added.

## Features

- Displays Cadaver Growth infection progress as a percentage.
- Stops reading infection data while compatibility mode has disabled the HUD.
- Uses slower retry intervals when Cadaver Growth data is not available, reducing scene-scan overhead in modpacks or lobbies without active Cadaver Growth state.
- Supports an always-visible mode for checking the HUD at 0%.
- Provides automatic HUD presentation selection.
- Uses a compact current-style bar in compatible HUD environments.
- Uses a vanilla stamina-ring style with the base game HUD.
- Preserves terminal fade behavior and follows global HUD visibility where appropriate.
- Reuses the base HUD intro alpha behavior for both supported HUD presentation modes.
- Supports automatic Chinese/English label selection, with manual override available in configuration.
- Includes diagnostic logging and debug-only live layout refresh options.

## HUD Modes

`HudStyleMode` controls presentation:

- `Auto`: recommended. Uses the compact current-style presentation with EladsHUD-compatible environments and the vanilla stamina-ring presentation otherwise.
- `CurrentStyle`: forces the compact bar presentation.
- `VanillaStaminaRingStyle`: forces the vanilla stamina-ring presentation.

## EladsHUD Interoperability

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

Build verification should be run against the local game and BepInEx reference paths used for development. Manual multiplayer testing is still required for host/client installation matrix coverage.

## Notes

- This is an unofficial community mod.
- *Lethal Company* and its assets belong to their respective owners.
- This project does not redistribute game assets or third-party HUD mod assets.
