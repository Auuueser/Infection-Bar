using BepInEx.Bootstrap;
using BepInEx.Logging;

namespace IndependentCadaverInfectionBar;

internal static class ChineseConfigDetector
{
    internal static bool IsInstalled(ManualLogSource logger)
    {
        bool installed = ChineseProjectIdentity.IsInstalled(Chainloader.PluginInfos.Keys);
        if (installed)
        {
            logger.LogInfo("LC Chinese Project detected by stable plugin GUID. Using Chinese configuration text.");
        }
        return installed;
    }
}
