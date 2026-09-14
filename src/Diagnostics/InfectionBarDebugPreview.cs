using System;

namespace IndependentCadaverInfectionBar;

internal enum InfectionBarDebugPreviewMode
{
    Off,
    Visible,
    Empty,
    Half,
    Full,
    Growing,
    Decreasing
}

internal readonly struct InfectionBarDebugPreviewFrame
{
    internal InfectionBarDebugPreviewFrame(float infectionNormalized, bool forceVisible)
    {
        InfectionNormalized = infectionNormalized;
        ForceVisible = forceVisible;
    }

    internal float InfectionNormalized { get; }

    internal bool ForceVisible { get; }
}

internal static class InfectionBarDebugPreview
{
    internal const float MinimumCycleSeconds = 0.5f;
    internal const float DefaultCycleSeconds = 5f;

    internal static InfectionBarDebugPreviewMode ParseMode(string value)
    {
        string normalizedValue = (value ?? string.Empty).Trim();
        return Enum.TryParse(normalizedValue, ignoreCase: true, out InfectionBarDebugPreviewMode mode)
            ? mode
            : InfectionBarDebugPreviewMode.Off;
    }

    internal static bool RefreshSettings(
        string modeValue,
        float cycleSecondsValue,
        ref string currentModeValue,
        ref InfectionBarDebugPreviewMode currentMode,
        ref float currentCycleSeconds)
    {
        float nextCycleSeconds = NormalizeCycleSeconds(cycleSecondsValue);
        bool modeValueChanged = !string.Equals(currentModeValue, modeValue, StringComparison.Ordinal);
        if (!modeValueChanged && Math.Abs(currentCycleSeconds - nextCycleSeconds) <= 0.0001f)
        {
            return false;
        }

        InfectionBarDebugPreviewMode nextMode = modeValueChanged ? ParseMode(modeValue) : currentMode;
        bool effectiveSettingsChanged = currentMode != nextMode
            || Math.Abs(currentCycleSeconds - nextCycleSeconds) > 0.0001f;
        currentModeValue = modeValue;
        currentMode = nextMode;
        currentCycleSeconds = nextCycleSeconds;
        return effectiveSettingsChanged;
    }

    internal static InfectionBarDebugPreviewFrame Evaluate(
        InfectionBarDebugPreviewMode mode,
        float realInfectionNormalized,
        float elapsedSeconds,
        float cycleSeconds)
    {
        float realValue = Clamp01(realInfectionNormalized);
        switch (mode)
        {
            case InfectionBarDebugPreviewMode.Visible:
                return new InfectionBarDebugPreviewFrame(realValue, forceVisible: true);
            case InfectionBarDebugPreviewMode.Empty:
                return new InfectionBarDebugPreviewFrame(0f, forceVisible: true);
            case InfectionBarDebugPreviewMode.Half:
                return new InfectionBarDebugPreviewFrame(0.5f, forceVisible: true);
            case InfectionBarDebugPreviewMode.Full:
                return new InfectionBarDebugPreviewFrame(1f, forceVisible: true);
            case InfectionBarDebugPreviewMode.Growing:
                return new InfectionBarDebugPreviewFrame(GetCycleProgress(elapsedSeconds, cycleSeconds), forceVisible: true);
            case InfectionBarDebugPreviewMode.Decreasing:
                return new InfectionBarDebugPreviewFrame(1f - GetCycleProgress(elapsedSeconds, cycleSeconds), forceVisible: true);
            default:
                return new InfectionBarDebugPreviewFrame(realValue, forceVisible: false);
        }
    }

    private static float GetCycleProgress(float elapsedSeconds, float cycleSeconds)
    {
        float safeElapsedSeconds = Math.Max(0f, elapsedSeconds);
        float safeCycleSeconds = NormalizeCycleSeconds(cycleSeconds);
        float cycles = safeElapsedSeconds / safeCycleSeconds;
        return cycles - (float)Math.Floor(cycles);
    }

    private static float NormalizeCycleSeconds(float cycleSeconds)
    {
        if (float.IsNaN(cycleSeconds) || float.IsInfinity(cycleSeconds))
        {
            return DefaultCycleSeconds;
        }

        return Math.Max(MinimumCycleSeconds, cycleSeconds);
    }

    private static float Clamp01(float value)
    {
        if (value <= 0f)
        {
            return 0f;
        }

        return Math.Min(value, 1f);
    }
}
