namespace IndependentCadaverInfectionBar;
internal static class NativeWeightVisibility
{
    internal static bool ShouldReplace(bool enabled, bool infectionVisible, bool vanillaStyle, bool reuseNativePosition = false)
        => enabled && infectionVisible && vanillaStyle && !reuseNativePosition;
}
