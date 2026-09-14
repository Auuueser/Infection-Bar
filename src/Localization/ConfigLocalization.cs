using System;
using System.Collections.Generic;

namespace IndependentCadaverInfectionBar;

internal static class ChineseProjectIdentity
{
    internal const string CurrentPluginGuid = "Aueser.LCChineseProject";
    internal const string LegacyPluginGuid = "cn.codex.v81testchn";

    internal static bool IsInstalled(IEnumerable<string> pluginGuids)
    {
        if (pluginGuids == null) return false;
        foreach (string guid in pluginGuids)
        {
            if (IsKnownPluginGuid(guid)) return true;
        }
        return false;
    }

    internal static bool IsKnownPluginGuid(string pluginGuid)
    {
        return string.Equals(pluginGuid, CurrentPluginGuid, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pluginGuid, LegacyPluginGuid, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class ConfigTextCatalog
{
    private readonly bool chinese;

    private ConfigTextCatalog(bool chinese)
    {
        this.chinese = chinese;
    }

    internal static ConfigTextCatalog Create(bool chinese)
    {
        return new ConfigTextCatalog(chinese);
    }

    internal string ModDescription => chinese
        ? "显示尸体感染进度，并提供原版 HUD、布局和截图测试选项。HUD 测试显示可在游戏内实时切换。"
        : "Displays Cadaver infection progress with vanilla HUD, layout, and screenshot-preview options. HUD preview modes can be switched live in game.";

    internal string Section(string section)
    {
        if (!chinese)
        {
            return section;
        }

        switch (section)
        {
            case "General": return "常规";
            case "Debug": return "测试与调试";
            case "Layout": return "布局";
            case "VanillaHud": return "原版 HUD";
            case "Language": return "语言";
            default: return section;
        }
    }

    internal string Name(string key)
    {
        if (!chinese)
        {
            return key;
        }

        switch (key)
        {
            case "InfectionBarEnabled": return "启用感染条";
            case "InfectionBarAlwaysVisible": return "始终显示感染条";
            case "DebugLogging": return "调试日志";
            case "HudStyleMode": return "HUD 样式";
            case "DebugVanillaHudLiveLayoutRefresh": return "实时刷新原版 HUD 布局";
            case "HudPreviewMode": return "HUD 测试显示";
            case "HudPreviewCycleSeconds": return "测试动画周期（秒）";
            case "UiWidth": return "界面宽度";
            case "UiHeight": return "界面高度";
            case "AnchorPreset": return "锚点位置";
            case "PositionOffsetX": return "水平位置偏移";
            case "PositionOffsetY": return "垂直位置偏移";
            case "TextOffsetX": return "文字水平偏移";
            case "TextOffsetY": return "文字垂直偏移";
            case "TextFontSize": return "文字大小";
            case "ShowPanelBackground": return "显示面板背景";
            case "PanelBackgroundAlpha": return "面板背景透明度";
            case "TerminalFadeAlpha": return "终端使用时透明度";
            case "ReduceAliasing": return "减少锯齿和模糊";
            case "SortingOrder": return "界面绘制顺序";
            case "VanillaReuseNativePosition": return "复用原版数值位置";
            case "VanillaArcTextEnabled": return "弧形文字排列";
            case "VanillaRingScale": return "感染环缩放";
            case "VanillaRingOffsetX": return "感染环水平偏移";
            case "VanillaRingOffsetY": return "感染环垂直偏移";
            case "VanillaWarningTextOffsetEnabled": return "移动原版警告文字";
            case "VanillaWarningTextOffsetX": return "警告文字水平偏移";
            case "VanillaWarningTextOffsetY": return "警告文字垂直偏移";
            case "LabelLanguageMode": return "感染标签语言";
            default: return key;
        }
    }

    internal string Description(string key)
    {
        if (!chinese)
        {
            return EnglishDescription(key);
        }

        switch (key)
        {
            case "InfectionBarEnabled": return "是否显示尸体感染条。";
            case "InfectionBarAlwaysVisible": return "感染为 0% 时是否仍保持感染条可见。";
            case "DebugLogging": return "是否记录感染条生命周期和原生 HUD 挂接诊断日志。";
            case "HudStyleMode": return "HUD 样式。可选：Auto、CurrentStyle、VanillaStaminaRingStyle。";
            case "DebugVanillaHudLiveLayoutRefresh": return "仅用于布局验证。启用后每帧强制同步原版感染环和弧形文字，会增加 CPU 开销。";
            case "HudPreviewMode": return "仅用于截图测试。Off 使用真实感染值；Visible 强制显示真实值；Empty、Half、Full 分别显示 0%、50%、100%；Growing、Decreasing 循环增长或减少。";
            case "HudPreviewCycleSeconds": return "Growing 或 Decreasing 完成一次单向动画所需秒数；低于 0.5 的值会被限制为 0.5。";
            case "UiWidth": return "感染条完整界面根节点的宽度。";
            case "UiHeight": return "感染条完整界面根节点的高度。";
            case "AnchorPreset": return "原生 HUD 容器内的锚点。可选：BottomLeft、BottomCenter、BottomRight、Center、TopLeft、TopCenter、TopRight。";
            case "PositionOffsetX": return "相对于所选锚点的水平偏移。";
            case "PositionOffsetY": return "相对于所选锚点的垂直偏移。";
            case "TextOffsetX": return "感染标签文字在界面根节点内的水平偏移。";
            case "TextOffsetY": return "感染标签文字在界面根节点内的垂直偏移。";
            case "TextFontSize": return "感染标签文字大小。";
            case "ShowPanelBackground": return "是否显示感染条后的浅色面板背景。";
            case "PanelBackgroundAlpha": return "启用面板背景时的透明度。";
            case "TerminalFadeAlpha": return "玩家正在使用游戏内终端时的感染条透明度。";
            case "ReduceAliasing": return "使用更保守的像素对齐和最小线条厚度，减少轻微旋转时的锯齿与模糊。";
            case "SortingOrder": return "独立覆盖画布的绘制顺序。";
            case "VanillaReuseNativePosition": return "保留原版磅数组件与位置，感染值横排在其下方并按需避让环形笔画。开启时优先于弧形文字排列，关闭后恢复原来的布局选择。可局内实时切换。";
            case "VanillaArcTextEnabled": return "开启后磅数和感染值沿弧线排列；关闭后使用原版字体和字号横向分两行显示，并避开体力条和感染条。可在局内实时切换。";
            case "VanillaRingScale": return "由原版耐力环派生的感染环缩放倍数。";
            case "VanillaRingOffsetX": return "自动放置到外环后附加的水平微调偏移。";
            case "VanillaRingOffsetY": return "自动放置到外环后附加的垂直微调偏移。";
            case "VanillaWarningTextOffsetEnabled": return "原版 HUD 模式下是否移动原版警告文字，以避免与感染环重叠。";
            case "VanillaWarningTextOffsetX": return "原版 HUD 模式下应用于警告文字根节点的水平偏移。";
            case "VanillaWarningTextOffsetY": return "原版 HUD 模式下应用于警告文字根节点的垂直偏移。";
            case "LabelLanguageMode": return "感染标签语言：Auto（自动）、Chinese（中文）、English（英文）。自动模式仅按 LC Chinese Project 的稳定插件 GUID 检测，有则中文，无则英文，不检查版本号。手动选择优先；字体仅用于显示兜底，不能改变语言选择。";
            default: return EnglishDescription(key);
        }
    }

    private static string EnglishDescription(string key)
    {
        switch (key)
        {
            case "InfectionBarEnabled": return "Whether to show the standalone Cadaver infection bar.";
            case "InfectionBarAlwaysVisible": return "Whether the infection bar should stay visible even when infection is at 0%.";
            case "DebugLogging": return "Whether to write diagnostic logs for infection bar lifecycle and native HUD attachment.";
            case "HudStyleMode": return "HUD style mode. Supported values: Auto, CurrentStyle, VanillaStaminaRingStyle.";
            case "DebugVanillaHudLiveLayoutRefresh": return "For layout validation only. When enabled, the vanilla HUD ring and curved text are force-resynced every frame, which costs more CPU.";
            case "HudPreviewMode": return "Test-only HUD preview. Off uses the real value; Visible forces the real value visible; Empty, Half, and Full show 0%, 50%, and 100%; Growing and Decreasing animate in one direction.";
            case "HudPreviewCycleSeconds": return "Seconds for one Growing or Decreasing preview cycle. Values below 0.5 are clamped.";
            case "UiWidth": return "Overall width of the full infection bar UI root.";
            case "UiHeight": return "Overall height of the full infection bar UI root.";
            case "AnchorPreset": return "Anchor preset used inside the native HUD container. Supported values: BottomLeft, BottomCenter, BottomRight, Center, TopLeft, TopCenter, TopRight.";
            case "PositionOffsetX": return "Horizontal offset relative to the selected anchor preset.";
            case "PositionOffsetY": return "Vertical offset relative to the selected anchor preset.";
            case "TextOffsetX": return "Horizontal offset of the infection label text inside the bar root.";
            case "TextOffsetY": return "Vertical offset of the infection label text inside the bar root.";
            case "TextFontSize": return "Font size of the infection label text.";
            case "ShowPanelBackground": return "Whether to show the faint panel background behind the infection bar.";
            case "PanelBackgroundAlpha": return "Alpha of the panel background when it is enabled.";
            case "TerminalFadeAlpha": return "Alpha used while the in-game terminal is actively being used.";
            case "ReduceAliasing": return "Uses more conservative UI alignment and minimum element thickness to reduce jagged edges and blur when the infection bar is slightly tilted.";
            case "SortingOrder": return "Sorting order for the standalone overlay canvas.";
            case "VanillaReuseNativePosition": return "Keep the native weight component and position; show infection horizontally below it, avoiding ring strokes where necessary. Overrides arc text while enabled. Applies live.";
            case "VanillaArcTextEnabled": return "Curve weight and infection text around the rings. Disable for two horizontal rows using the native font and size, clear of both rings. Applies live.";
            case "VanillaRingScale": return "Scale multiplier applied to the infection ring derived from the original sprint meter UI.";
            case "VanillaRingOffsetX": return "Additional horizontal fine-tune offset after the automatic outer-ring placement.";
            case "VanillaRingOffsetY": return "Additional vertical fine-tune offset after the automatic outer-ring placement.";
            case "VanillaWarningTextOffsetEnabled": return "Whether vanilla HUD mode should move the original warning text right so it does not overlap the infection ring.";
            case "VanillaWarningTextOffsetX": return "Horizontal offset applied to the original warning text root while vanilla HUD mode is active.";
            case "VanillaWarningTextOffsetY": return "Vertical offset applied to the original warning text root while vanilla HUD mode is active.";
            case "LabelLanguageMode": return "Infection label language: Auto, Chinese, English. Auto uses only the stable LC Chinese Project plugin GUID (no version requirement); installed means Chinese, otherwise English. Explicit language selection takes priority. Font fallback must never change the selected language.";
            default: return key;
        }
    }
}
