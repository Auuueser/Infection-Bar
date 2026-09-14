using BepInEx.Configuration;
using UnityEngine;

namespace IndependentCadaverInfectionBar;

internal static class ModConfig
{
    internal static ConfigTextCatalog Texts { get; private set; }

    internal static ConfigEntry<bool> InfectionBarEnabled { get; private set; }

    internal static ConfigEntry<bool> InfectionBarAlwaysVisible { get; private set; }

    internal static ConfigEntry<bool> DebugLogging { get; private set; }

    internal static ConfigEntry<bool> DebugVanillaHudLiveLayoutRefresh { get; private set; }

    internal static ConfigEntry<string> DebugHudPreviewMode { get; private set; }

    internal static ConfigEntry<float> DebugHudPreviewCycleSeconds { get; private set; }

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

    internal static ConfigEntry<bool> VanillaReuseNativePosition { get; private set; }

    internal static ConfigEntry<bool> VanillaArcTextEnabled { get; private set; }

    internal static ConfigEntry<float> VanillaRingScale { get; private set; }

    internal static ConfigEntry<float> VanillaRingOffsetX { get; private set; }

    internal static ConfigEntry<float> VanillaRingOffsetY { get; private set; }

    internal static ConfigEntry<bool> VanillaWarningTextOffsetEnabled { get; private set; }

    internal static ConfigEntry<float> VanillaWarningTextOffsetX { get; private set; }

    internal static ConfigEntry<float> VanillaWarningTextOffsetY { get; private set; }

    internal static ConfigEntry<string> LabelLanguageMode { get; private set; }

    internal static void Bind(ConfigFile config, bool useChineseText)
    {
        Texts = ConfigTextCatalog.Create(useChineseText);
        InfectionBarEnabled = Bind(config, "General", "InfectionBarEnabled", true);
        InfectionBarAlwaysVisible = Bind(config, "General", "InfectionBarAlwaysVisible", false);
        DebugLogging = Bind(config, "General", "DebugLogging", false);
        DebugVanillaHudLiveLayoutRefresh = Bind(config, "Debug", "DebugVanillaHudLiveLayoutRefresh", false);
        DebugHudPreviewMode = Bind(config, "Debug", "HudPreviewMode", "Off");
        DebugHudPreviewCycleSeconds = Bind(config, "Debug", "HudPreviewCycleSeconds", 5f);
        HudStyleMode = Bind(config, "General", "HudStyleMode", "Auto");

        UiWidth = Bind(config, "Layout", "UiWidth", 215f);
        UiHeight = Bind(config, "Layout", "UiHeight", 34f);
        AnchorPreset = Bind(config, "Layout", "AnchorPreset", "TopLeft");
        PositionOffsetX = Bind(config, "Layout", "PositionOffsetX", 30f);
        PositionOffsetY = Bind(config, "Layout", "PositionOffsetY", -135f);
        TextOffsetX = Bind(config, "Layout", "TextOffsetX", 9f);
        TextOffsetY = Bind(config, "Layout", "TextOffsetY", 1f);
        TextFontSize = Bind(config, "Layout", "TextFontSize", 13);
        ShowPanelBackground = Bind(config, "Layout", "ShowPanelBackground", false);
        PanelBackgroundAlpha = Bind(config, "Layout", "PanelBackgroundAlpha", 0.22f);
        TerminalFadeAlpha = Bind(config, "Layout", "TerminalFadeAlpha", 0.25f);
        ReduceAliasing = Bind(config, "Layout", "ReduceAliasing", true);
        SortingOrder = Bind(config, "Layout", "SortingOrder", 5);

        VanillaReuseNativePosition = Bind(config, "VanillaHud", "VanillaReuseNativePosition", false);
        VanillaArcTextEnabled = Bind(config, "VanillaHud", "VanillaArcTextEnabled", false);
        VanillaRingScale = Bind(config, "VanillaHud", "VanillaRingScale", 1.3f);
        VanillaRingOffsetX = Bind(config, "VanillaHud", "VanillaRingOffsetX", -32.5f);
        VanillaRingOffsetY = Bind(config, "VanillaHud", "VanillaRingOffsetY", 16f);
        VanillaWarningTextOffsetEnabled = Bind(config, "VanillaHud", "VanillaWarningTextOffsetEnabled", true);
        VanillaWarningTextOffsetX = Bind(config, "VanillaHud", "VanillaWarningTextOffsetX", 60f);
        VanillaWarningTextOffsetY = Bind(config, "VanillaHud", "VanillaWarningTextOffsetY", 0f);

        LabelLanguageMode = Bind(config, "Language", "LabelLanguageMode", "Auto");
        config.Save();
    }

    private static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue)
    {
        return config.Bind(section, key, defaultValue, Texts.Description(key));
    }
}
