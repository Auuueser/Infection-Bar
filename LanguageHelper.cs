using System;
using BepInEx.Bootstrap;
using UnityEngine;

namespace IndependentCadaverInfectionBar;

internal sealed class LanguageHelper
{
    private const string ChineseLocalizationPluginGuid = "cn.codex.v81testchn";

    internal string GetInfectionLabel()
    {
        return DetermineInfectionLabel();
    }

    internal string DetermineInfectionLabel()
    {
        string mode = (ModConfig.LabelLanguageMode.Value ?? "Auto").Trim();
        if (mode.Equals("Chinese", StringComparison.OrdinalIgnoreCase))
        {
            return "感染";
        }

        if (mode.Equals("English", StringComparison.OrdinalIgnoreCase))
        {
            return "Infection";
        }

        return ShouldUseChineseInfectionLabel() ? "感染" : "Infection";
    }

    internal bool ShouldUseChineseInfectionLabel()
    {
        try
        {
            if (Chainloader.PluginInfos != null && Chainloader.PluginInfos.ContainsKey(ChineseLocalizationPluginGuid))
            {
                return true;
            }
        }
        catch
        {
        }

        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string assemblyName = assembly.GetName().Name;
            if (assemblyName.Equals("V81TestChn", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
