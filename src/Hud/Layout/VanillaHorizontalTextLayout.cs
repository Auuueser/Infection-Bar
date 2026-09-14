using System;

namespace IndependentCadaverInfectionBar
{
internal static class VanillaHorizontalTextLayout
{
    internal const float RingClearance = 6f;
    internal static void Calculate(float nativeLeft, float nativeY, float staminaRight,
        float infectionRight, bool infectionVisible, float fontHeight,
        out float left, out float weightY, out float infectionY, out float rowHeight)
    {
        float right = infectionVisible ? Math.Max(staminaRight, infectionRight) : staminaRight;
        left = Math.Max(nativeLeft, right + RingClearance);
        rowHeight = Math.Max(20f, Math.Abs(fontHeight) * 1.4f);
        weightY = nativeY;
        infectionY = nativeY - rowHeight;
    }
}
}
