# Changelog

All notable changes to this project are documented here.

## 1.1.2 - Performance and HUD Visibility Refinements

### Fixed

- Reduced frame-time cost while the HUD is enabled by backing off repeated `CadaverGrowthAI` scene scans when no usable Cadaver Growth data is available.
- Cached automatic HUD style resolution and EladsHUD detection so plugin metadata is not scanned every frame.
- Preserved terminal fade behavior in vanilla HUD mode by applying terminal fade before global hidden-HUD handling.
- Reused the base HUD intro alpha behavior for both `CurrentStyle` and `VanillaStaminaRingStyle`.

### Changed

- Avoided redundant original weight counter visibility writes in vanilla HUD mode.
- Avoided rebuilding infection text every frame when the displayed percentage and label have not changed.
- Sampled reflected native HUD visibility state at a short fixed interval instead of every frame.
- Added and updated static guard tests for compatibility, performance, terminal fade, and HUD intro alpha behavior.

## 1.1.1 - Thunderstore Packaging Cleanup

### Changed

- Updated project and plugin version metadata to `1.1.1`.
- Clarified multiplayer installation guidance for host-required compatibility mode.
- Removed debug symbol files from the Thunderstore release package.

## 1.1.0 - Host-Required Compatibility Mode

### Added

- Added a Unity Netcode named-message compatibility handshake between installed clients and the host.
- Added host-side tracking for clients that have sent an Infection Bar hello message.
- Added an 8 second grace period for newly connected clients before they are treated as missing the mod.
- Added host state broadcasts for installed clients so the HUD can be enabled or disabled consistently across the lobby.
- Added a focused compatibility state test script.

### Changed

- Changed multiplayer behavior to host-required compatibility mode.
- Installed clients now keep the connection and hide the HUD when the host is not running Infection Bar.
- The host now marks the HUD unavailable when one or more connected clients do not have Infection Bar installed.
- Installed clients now follow the host's compatibility state.
- The controller now stops before reading Cadaver Growth infection data while compatibility mode has disabled the HUD.
- Documentation now describes multiplayer requirements and compatibility behavior explicitly.

### Notes

- Infection Bar still does not block clients from joining, kick players, or affect lobby connectivity.
- Infection Bar only reads local infection state that already exists in the running game; it does not add networked infection gameplay.
- No `NetworkObject` prefab was added.

## 1.0.0 - Initial Public Source Release

- Added an independent Cadaver Growth infection HUD display.
- Added automatic HUD style selection.
- Added vanilla HUD mode based on the running game's built-in stamina meter presentation.
- Added compact current-style HUD presentation for compatible HUD environments.
- Added configurable always-visible behavior.
- Added terminal fade behavior and global HUD visibility handling.
- Added Chinese and English infection label handling.
- Added vanilla HUD curved text layout for infection and carried weight labels.
- Added vanilla HUD warning text offset controls to reduce overlap with the infection ring.
- Added debug-only diagnostics and live layout refresh options for troubleshooting.
