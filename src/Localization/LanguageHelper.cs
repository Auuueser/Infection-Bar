using BepInEx.Bootstrap;

namespace IndependentCadaverInfectionBar;

internal sealed class LanguageHelper
{
    private bool hasCachedAutomaticLanguage;
    private bool cachedAutomaticChinese;

    internal string GetInfectionLabel() => DetermineInfectionLabel();

    internal string DetermineInfectionLabel()
    {
        return InfectionLanguagePolicy.UseChinese(ModConfig.LabelLanguageMode.Value,
            ShouldUseChineseInfectionLabel()) ? "感染" : "Infection";
    }

    internal bool ShouldUseChineseInfectionLabel()
    {
        if (!hasCachedAutomaticLanguage)
        {
            cachedAutomaticChinese = ChineseProjectIdentity.IsInstalled(Chainloader.PluginInfos.Keys);
            hasCachedAutomaticLanguage = true;
        }
        return cachedAutomaticChinese;
    }
}
