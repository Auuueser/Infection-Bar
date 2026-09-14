using System;

namespace IndependentCadaverInfectionBar
{
internal static class VanillaRingFillMapping
{
    private const float VisibleArcStartFill = 0.30f;

    internal static float MapInfectionToVisibleFill(float infectionNormalized)
    {
        float clampedInfection = Clamp01(infectionNormalized);
        if (clampedInfection <= 0f)
        {
            return 0f;
        }

        return VisibleArcStartFill + (1f - VisibleArcStartFill) * clampedInfection;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return Math.Min(value, 1f);
    }
}
}
