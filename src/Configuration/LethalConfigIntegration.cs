using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;

namespace IndependentCadaverInfectionBar;

internal static class LethalConfigIntegration
{
    internal const string PluginGuid = "ainavt.lc.lethalconfig";

    internal static void Register(ManualLogSource logger)
    {
        try
        {
            ConfigTextCatalog texts = ModConfig.Texts;
            LethalConfigManager.SetModDescription(texts.ModDescription);

            AddBool(ModConfig.InfectionBarEnabled, "General", "InfectionBarEnabled", texts);
            AddBool(ModConfig.InfectionBarAlwaysVisible, "General", "InfectionBarAlwaysVisible", texts);
            AddBool(ModConfig.DebugLogging, "General", "DebugLogging", texts);
            AddTextDropDown(ModConfig.HudStyleMode, "General", "HudStyleMode", texts,
                "Auto", "CurrentStyle", "VanillaStaminaRingStyle");

            AddBool(ModConfig.DebugVanillaHudLiveLayoutRefresh, "Debug", "DebugVanillaHudLiveLayoutRefresh", texts);
            AddTextDropDown(ModConfig.DebugHudPreviewMode, "Debug", "HudPreviewMode", texts,
                "Off", "Visible", "Empty", "Half", "Full", "Growing", "Decreasing");
            AddFloat(ModConfig.DebugHudPreviewCycleSeconds, "Debug", "HudPreviewCycleSeconds", texts);

            AddFloat(ModConfig.UiWidth, "Layout", "UiWidth", texts);
            AddFloat(ModConfig.UiHeight, "Layout", "UiHeight", texts);
            AddTextDropDown(ModConfig.AnchorPreset, "Layout", "AnchorPreset", texts,
                "BottomLeft", "BottomCenter", "BottomRight", "Center", "TopLeft", "TopCenter", "TopRight");
            AddFloat(ModConfig.PositionOffsetX, "Layout", "PositionOffsetX", texts);
            AddFloat(ModConfig.PositionOffsetY, "Layout", "PositionOffsetY", texts);
            AddFloat(ModConfig.TextOffsetX, "Layout", "TextOffsetX", texts);
            AddFloat(ModConfig.TextOffsetY, "Layout", "TextOffsetY", texts);
            AddInt(ModConfig.TextFontSize, "Layout", "TextFontSize", texts);
            AddBool(ModConfig.ShowPanelBackground, "Layout", "ShowPanelBackground", texts);
            AddFloat(ModConfig.PanelBackgroundAlpha, "Layout", "PanelBackgroundAlpha", texts);
            AddFloat(ModConfig.TerminalFadeAlpha, "Layout", "TerminalFadeAlpha", texts);
            AddBool(ModConfig.ReduceAliasing, "Layout", "ReduceAliasing", texts);

            AddBool(ModConfig.VanillaReuseNativePosition, "VanillaHud", "VanillaReuseNativePosition", texts);
            AddBool(ModConfig.VanillaArcTextEnabled, "VanillaHud", "VanillaArcTextEnabled", texts);
            AddFloat(ModConfig.VanillaRingScale, "VanillaHud", "VanillaRingScale", texts);
            AddFloat(ModConfig.VanillaRingOffsetX, "VanillaHud", "VanillaRingOffsetX", texts);
            AddFloat(ModConfig.VanillaRingOffsetY, "VanillaHud", "VanillaRingOffsetY", texts);
            AddBool(ModConfig.VanillaWarningTextOffsetEnabled, "VanillaHud", "VanillaWarningTextOffsetEnabled", texts);
            AddFloat(ModConfig.VanillaWarningTextOffsetX, "VanillaHud", "VanillaWarningTextOffsetX", texts);
            AddFloat(ModConfig.VanillaWarningTextOffsetY, "VanillaHud", "VanillaWarningTextOffsetY", texts);

            AddTextDropDown(ModConfig.LabelLanguageMode, "Language", "LabelLanguageMode", texts,
                "Auto", "Chinese", "English");

            logger.LogInfo("Registered live localized LethalConfig entries.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Could not register LethalConfig entries: {ex.Message}");
        }
    }

    private static TOptions CreateOptions<TOptions>(string section, string key, ConfigTextCatalog texts)
        where TOptions : BaseOptions, new()
    {
        return new TOptions
        {
            Name = texts.Name(key),
            Section = texts.Section(section),
            Description = texts.Description(key),
            RequiresRestart = false
        };
    }

    private static void AddBool(ConfigEntry<bool> entry, string section, string key, ConfigTextCatalog texts)
    {
        LethalConfigManager.SkipAutoGenFor(entry);
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(entry, CreateOptions<BoolCheckBoxOptions>(section, key, texts)));
    }

    private static void AddFloat(ConfigEntry<float> entry, string section, string key, ConfigTextCatalog texts)
    {
        LethalConfigManager.SkipAutoGenFor(entry);
        LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(entry, CreateOptions<FloatInputFieldOptions>(section, key, texts)));
    }

    private static void AddInt(ConfigEntry<int> entry, string section, string key, ConfigTextCatalog texts)
    {
        LethalConfigManager.SkipAutoGenFor(entry);
        LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(entry, CreateOptions<IntInputFieldOptions>(section, key, texts)));
    }

    private static void AddTextDropDown(ConfigEntry<string> entry, string section, string key, ConfigTextCatalog texts, params string[] values)
    {
        LethalConfigManager.SkipAutoGenFor(entry);
        LethalConfigManager.AddConfigItem(new TextDropDownConfigItem(entry, new TextDropDownOptions(values)
        {
            Name = texts.Name(key),
            Section = texts.Section(section),
            Description = texts.Description(key),
            RequiresRestart = false
        }));
    }
}
