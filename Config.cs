using BepInEx.Configuration;
using UnityEngine;

namespace IndependentCadaverInfectionBar;

internal static class ModConfig
{
    internal static ConfigEntry<bool> InfectionBarEnabled { get; private set; }

    internal static ConfigEntry<bool> InfectionBarAlwaysVisible { get; private set; }

    internal static ConfigEntry<bool> DebugLogging { get; private set; }

    internal static ConfigEntry<bool> DebugVanillaHudLiveLayoutRefresh { get; private set; }

    internal static ConfigEntry<string> HudStyleMode { get; private set; }

    internal static ConfigEntry<float> UiWidth { get; private set; }

    internal static ConfigEntry<float> UiHeight { get; private set; }

    internal static ConfigEntry<string> AnchorPreset { get; private set; }

    internal static ConfigEntry<float> PositionOffsetX { get; private set; }

    internal static ConfigEntry<float> PositionOffsetY { get; private set; }

    internal static ConfigEntry<float> TextOffsetX { get; private set; }

    internal static ConfigEntry<float> TextOffsetY { get; private set; }

    internal static ConfigEntry<int> TextFontSize { get; private set; }

    internal static ConfigEntry<bool> ShowPanelBackground { get; private set; }

    internal static ConfigEntry<float> PanelBackgroundAlpha { get; private set; }

    internal static ConfigEntry<float> TerminalFadeAlpha { get; private set; }

    internal static ConfigEntry<bool> ReduceAliasing { get; private set; }

    internal static ConfigEntry<int> SortingOrder { get; private set; }

    internal static ConfigEntry<float> VanillaRingScale { get; private set; }

    internal static ConfigEntry<float> VanillaRingOffsetX { get; private set; }

    internal static ConfigEntry<float> VanillaRingOffsetY { get; private set; }

    internal static ConfigEntry<bool> VanillaWarningTextOffsetEnabled { get; private set; }

    internal static ConfigEntry<float> VanillaWarningTextOffsetX { get; private set; }

    internal static ConfigEntry<float> VanillaWarningTextOffsetY { get; private set; }

    internal static ConfigEntry<string> LabelLanguageMode { get; private set; }

    internal static void Bind(ConfigFile config)
    {
        InfectionBarEnabled = config.Bind("General", "InfectionBarEnabled", true, "Whether to show the standalone Cadaver infection bar.");
        InfectionBarAlwaysVisible = config.Bind("General", "InfectionBarAlwaysVisible", false, "Whether the infection bar should stay visible even when infection is at 0%.");
        DebugLogging = config.Bind("General", "DebugLogging", false, "Whether to write diagnostic logs for infection bar lifecycle and native HUD attachment.");
        DebugVanillaHudLiveLayoutRefresh = config.Bind("Debug", "DebugVanillaHudLiveLayoutRefresh", false, "For layout validation only. When enabled, the vanilla HUD ring and curved text are force-resynced every frame, which costs more CPU.");
        HudStyleMode = config.Bind("General", "HudStyleMode", "Auto", "HUD style mode for this isolated test. Supported values: Auto, CurrentStyle, VanillaStaminaRingStyle.");

        UiWidth = config.Bind("Layout", "UiWidth", 215f, "Overall width of the full infection bar UI root.");
        UiHeight = config.Bind("Layout", "UiHeight", 34f, "Overall height of the full infection bar UI root.");
        AnchorPreset = config.Bind("Layout", "AnchorPreset", "TopLeft", "Anchor preset used inside the native HUD container. Supported values: BottomLeft, BottomCenter, BottomRight, Center, TopLeft, TopCenter, TopRight.");
        PositionOffsetX = config.Bind("Layout", "PositionOffsetX", 30f, "Horizontal offset relative to the selected anchor preset.");
        PositionOffsetY = config.Bind("Layout", "PositionOffsetY", -135f, "Vertical offset relative to the selected anchor preset.");
        TextOffsetX = config.Bind("Layout", "TextOffsetX", 9f, "Horizontal offset of the infection label text inside the bar root.");
        TextOffsetY = config.Bind("Layout", "TextOffsetY", 1f, "Vertical offset of the infection label text inside the bar root.");
        TextFontSize = config.Bind("Layout", "TextFontSize", 13, "Font size of the infection label text.");
        ShowPanelBackground = config.Bind("Layout", "ShowPanelBackground", false, "Whether to show the faint panel background behind the infection bar.");
        PanelBackgroundAlpha = config.Bind("Layout", "PanelBackgroundAlpha", 0.22f, "Alpha of the panel background when it is enabled.");
        TerminalFadeAlpha = config.Bind("Layout", "TerminalFadeAlpha", 0.25f, "Alpha used while the in-game terminal is actively being used.");
        ReduceAliasing = config.Bind("Layout", "ReduceAliasing", true, "Uses more conservative UI alignment and minimum element thickness to reduce jagged edges and blur when the infection bar is slightly tilted.");
        SortingOrder = config.Bind("Layout", "SortingOrder", 5, "Sorting order for the standalone overlay canvas.");

        VanillaRingScale = config.Bind("VanillaHud", "VanillaRingScale", 1.3f, "Scale multiplier applied to the infection ring derived from the original sprint meter UI.");
        VanillaRingOffsetX = config.Bind("VanillaHud", "VanillaRingOffsetX", -32.5f, "Additional horizontal fine-tune offset after the automatic outer-ring placement.");
        VanillaRingOffsetY = config.Bind("VanillaHud", "VanillaRingOffsetY", 16f, "Additional vertical fine-tune offset after the automatic outer-ring placement.");
        VanillaWarningTextOffsetEnabled = config.Bind("VanillaHud", "VanillaWarningTextOffsetEnabled", true, "Whether vanilla HUD mode should move the original warning text right so it does not overlap the infection ring.");
        VanillaWarningTextOffsetX = config.Bind("VanillaHud", "VanillaWarningTextOffsetX", 60f, "Horizontal offset applied to the original warning text root while vanilla HUD mode is active.");
        VanillaWarningTextOffsetY = config.Bind("VanillaHud", "VanillaWarningTextOffsetY", 0f, "Vertical offset applied to the original warning text root while vanilla HUD mode is active.");

        LabelLanguageMode = config.Bind("Language", "LabelLanguageMode", "Auto", "Auto, English, or Chinese.");
    }
}
