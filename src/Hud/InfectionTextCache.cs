namespace IndependentCadaverInfectionBar;
internal static class InfectionTextCache
{
    private static readonly string[] English = new string[101];
    private static readonly string[] Chinese = new string[101];
    internal static string Get(string label, int percent)
    {
        if (percent < 0 || percent > 100 || (label != "感染" && label != "Infection"))
            return label + " " + percent.ToString() + "%";
        string[] cache = label == "感染" ? Chinese : English;
        return cache[percent] ?? (cache[percent] = label + " " + percent.ToString() + "%");
    }
}
