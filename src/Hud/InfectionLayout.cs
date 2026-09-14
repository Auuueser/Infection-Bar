using UnityEngine;

namespace IndependentCadaverInfectionBar;

internal sealed class InfectionLayout
{
    private enum AnchorPreset
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        Center,
        TopLeft,
        TopCenter,
        TopRight
    }

    internal Vector2 GetAnchoredPosition()
    {
        return new Vector2(ModConfig.PositionOffsetX.Value, ModConfig.PositionOffsetY.Value);
    }

    internal Vector2 GetAnchorMin()
    {
        return GetAnchorVector();
    }

    internal Vector2 GetAnchorMax()
    {
        return GetAnchorVector();
    }

    internal Vector2 GetPivot()
    {
        return GetAnchorVector();
    }

    internal Vector2 GetRootSize()
    {
        return new Vector2(ModConfig.UiWidth.Value, ModConfig.UiHeight.Value);
    }

    internal Vector2 GetBarSize()
    {
        return new Vector2(
            Mathf.Max(16f, ModConfig.UiWidth.Value - 20f),
            Mathf.Max(4f, ModConfig.UiHeight.Value - 26f));
    }

    private Vector2 GetAnchorVector()
    {
        switch (ParseAnchorPreset())
        {
            case AnchorPreset.BottomLeft:
                return new Vector2(0f, 0f);
            case AnchorPreset.BottomCenter:
                return new Vector2(0.5f, 0f);
            case AnchorPreset.BottomRight:
                return new Vector2(1f, 0f);
            case AnchorPreset.TopLeft:
                return new Vector2(0f, 1f);
            case AnchorPreset.TopCenter:
                return new Vector2(0.5f, 1f);
            case AnchorPreset.TopRight:
                return new Vector2(1f, 1f);
            default:
                return new Vector2(0.5f, 0.5f);
        }
    }

    private AnchorPreset ParseAnchorPreset()
    {
        string preset = (ModConfig.AnchorPreset.Value ?? string.Empty).Trim();
        if (System.Enum.TryParse(preset, ignoreCase: true, out AnchorPreset anchorPreset))
        {
            return anchorPreset;
        }

        return AnchorPreset.Center;
    }
}
