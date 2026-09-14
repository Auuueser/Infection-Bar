using System;

namespace IndependentCadaverInfectionBar;

// Language is a user choice or a stable plugin identity decision.
// Font availability belongs to rendering and must never change this result.
internal static class InfectionLanguagePolicy
{
    internal static bool UseChinese(string mode, bool chineseProjectInstalled)
    {
        string value = (mode ?? "Auto").Trim();
        if (value.Equals("Chinese", StringComparison.OrdinalIgnoreCase) || value == "中文") return true;
        if (value.Equals("English", StringComparison.OrdinalIgnoreCase) || value == "英文") return false;
        return chineseProjectInstalled;
    }
}
