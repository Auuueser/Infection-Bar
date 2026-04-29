# Infection Bar

Infection Bar is a BepInEx plugin for *Lethal Company* that displays Cadaver Growth infection progress as an in-game HUD element.

The plugin is designed to work as a standalone HUD mod. It can also adjust its presentation when EladsHUD or a compatible fork or variant is detected, while keeping its code, UI construction, configuration, and runtime behavior independent.

## Features

- Shows Cadaver Growth infection progress as a percentage.
- Supports an always-visible mode for monitoring infection state before infection is present.
- Provides two HUD presentations: a compact current-style bar and a vanilla stamina-ring style.
- Automatically selects a compatible presentation in `Auto` mode.
- Fades during terminal use and follows global HUD visibility where appropriate.
- Supports automatic Chinese/English label selection, with manual language override available in configuration.
- Includes debug-only layout refresh and diagnostic logging options for troubleshooting.

## HUD Modes

### Auto

`Auto` is the recommended mode.

- When EladsHUD or a compatible fork or variant is detected, Infection Bar uses its compact current-style presentation.
- When that environment is not detected, Infection Bar uses its vanilla stamina-ring presentation, based on the running game's original stamina meter layout.

### CurrentStyle

Forces the compact bar presentation. This is useful if you want the infection display to stay visually close to a compact HUD layout regardless of automatic detection.

### VanillaStaminaRingStyle

Forces the vanilla stamina-ring presentation. This mode places infection progress around the original HUD's stamina meter style and is intended to blend more naturally with the base game's HUD.

## EladsHUD Compatibility

This project is distributed as an independent plugin.

- EladsHUD compatibility is optional and runtime-only.
- Infection Bar does not require EladsHUD to run.
- If EladsHUD is not installed, the plugin falls back to its vanilla HUD mode.
- The compatibility path does not use EladsHUD as a dependency and does not attach Infection Bar as an EladsHUD module.
- It does not bundle third-party HUD plugins.
- It does not include third-party HUD source code, prefabs, images, shaders, materials, fonts, or AssetBundles.
- Compatibility is handled through BepInEx plugin detection and this plugin's own rendering logic.
- The vanilla HUD style derives its placement and visual basis from UI objects already present in the running game; those game assets are not redistributed by this repository.

EladsHUD is mentioned only to describe interoperability behavior. This project is not affiliated with, endorsed by, maintained by, or packaged with EladsHUD or its forks.

## Installation

1. Install BepInEx for *Lethal Company*.
2. Place `InfectionBar.dll` in a BepInEx plugin directory, for example:

   ```text
   BepInEx/plugins/InfectionBar/InfectionBar.dll
   ```

3. Launch the game once to generate the configuration file:

   ```text
   BepInEx/config/InfectionBar.cfg
   ```

4. Adjust configuration values if needed, then restart the game or reload the profile.

## Configuration Overview

Important configuration entries include:

- `InfectionBarEnabled`: Enables or disables the HUD element.
- `InfectionBarAlwaysVisible`: Keeps the infection display visible even at 0%.
- `HudStyleMode`: `Auto`, `CurrentStyle`, or `VanillaStaminaRingStyle`.
- `LabelLanguageMode`: `Auto`, `English`, or `Chinese`.
- `TerminalFadeAlpha`: Alpha used while the in-game terminal is active.
- `VanillaRingScale`, `VanillaRingOffsetX`, `VanillaRingOffsetY`: Fine-tuning values for vanilla HUD mode.
- `VanillaWarningTextOffsetEnabled`, `VanillaWarningTextOffsetX`, `VanillaWarningTextOffsetY`: Optional offset for original warning text in vanilla HUD mode.
- `DebugLogging` and `DebugVanillaHudLiveLayoutRefresh`: Diagnostic options intended for troubleshooting only.

## Building From Source

Requirements:

- .NET SDK compatible with `netstandard2.1`.
- *Lethal Company* managed assemblies.
- BepInEx core assemblies.

Example:

```powershell
dotnet build .\IndependentCadaverInfectionBar.csproj -c Release `
  "-p:GameManagedDir=D:\Steam\steamapps\common\Lethal Company\Lethal Company_Data\Managed\" `
  "-p:BepInExCoreDir=D:\Path\To\BepInEx\core\"
```

The compiled plugin is produced as:

```text
bin/Release/netstandard2.1/InfectionBar.dll
```

## Distribution Notes

Release packages should include only the compiled plugin and necessary package metadata. Do not include local test profiles, build caches, temporary preview files, snapshots, or unrelated third-party repositories.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Disclaimer

This is an unofficial community plugin. *Lethal Company* and its assets belong to their respective owners. This repository does not redistribute game assets or third-party HUD mod assets.
