using System;

namespace IndependentCadaverInfectionBar
{
internal static class VanillaChineseArcTextLayout
{
    private const float SpriteSize = 326f;

    private static readonly float[] ArcAngles =
    {
        -162.5f, -157.5f, -152.5f, -147.5f, -142.5f, -137.5f, -132.5f, -127.5f, -122.5f, -117.5f,
        -112.5f, -107.5f, -102.5f, -97.5f, -92.5f, -87.5f, -82.5f, -77.5f, -72.5f, -67.5f,
        -62.5f, -57.5f, -52.5f, -47.5f, -42.5f, -37.5f, -32.5f, -27.5f, -22.5f, -17.5f,
        -12.5f, -7.5f, -2.5f, 2.5f, 7.5f, 17.5f, 22.5f, 27.5f, 32.5f, 37.5f,
        42.5f, 47.5f, 52.5f
    };

    private static readonly float[] ArcInnerRadii =
    {
        148.3f, 146.4f, 155.9f, 153.8f, 159.1f, 157.7f, 154.7f, 144.9f, 145.9f, 140.7f,
        134.0f, 125.9f, 122.8f, 118.0f, 115.8f, 108.8f, 106.8f, 104.0f, 97.1f, 100.4f,
        98.6f, 97.8f, 97.4f, 97.7f, 98.4f, 96.5f, 100.6f, 102.2f, 104.1f, 107.9f,
        111.2f, 114.8f, 119.9f, 125.5f, 129.0f, 144.0f, 150.1f, 155.9f, 158.0f, 165.0f,
        165.2f, 166.8f, 164.5f
    };

    private static readonly float[] ArcOuterRadii =
    {
        152.1f, 158.5f, 161.1f, 165.2f, 165.5f, 165.5f, 164.1f, 160.9f, 163.2f, 150.6f,
        145.2f, 140.9f, 133.5f, 128.2f, 123.9f, 117.6f, 115.1f, 112.2f, 110.8f, 112.3f,
        106.7f, 105.3f, 105.1f, 105.4f, 106.2f, 109.9f, 108.1f, 110.3f, 113.0f, 116.4f,
        120.8f, 127.6f, 130.5f, 135.8f, 139.3f, 154.7f, 161.0f, 167.2f, 169.6f, 171.7f,
        173.9f, 171.2f, 168.6f
    };

    internal static VanillaArcGlyphLayout[] Build(string text, VanillaArcTextTrack track, float rootWidth, float rootHeight)
    {
        string textValue = text ?? string.Empty;
        if (textValue.Length == 0)
        {
            return Array.Empty<VanillaArcGlyphLayout>();
        }

        ChineseTrackSettings settings = GetTrackSettings(track);
        float[] advances = BuildGlyphAdvances(textValue);
        float totalAdvance = Sum(advances) * settings.AdvanceScale;
        float trackRadius = GetTrackRadius(settings.CenterAngle, settings);
        float naturalSpan = totalAdvance / Math.Max(1f, trackRadius) * Rad2Deg;
        float totalSpan = Clamp(naturalSpan, settings.MinAngleSpan, settings.MaxAngleSpan);
        float spanScale = naturalSpan > 0.0001f ? totalSpan / naturalSpan : 1f;
        float startArc = -totalSpan * 0.5f;
        if (!float.IsNaN(settings.FixedFirstGlyphAngle))
        {
            float firstGlyphAdvance = advances[0] * settings.AdvanceScale;
            float firstGlyphHalfArc = firstGlyphAdvance * 0.5f / Math.Max(1f, trackRadius) * Rad2Deg * spanScale;
            startArc = settings.CenterAngle - settings.FixedFirstGlyphAngle - firstGlyphHalfArc;
        }

        float consumed = 0f;
        float[] angles = new float[textValue.Length];
        float[] layoutProgresses = new float[textValue.Length];
        for (int i = 0; i < textValue.Length; i++)
        {
            float glyphAdvance = advances[i] * settings.AdvanceScale;
            layoutProgresses[i] = totalAdvance > 0.0001f ? (consumed + glyphAdvance * 0.5f) / totalAdvance : 0f;
            float glyphCenterArc = startArc + (consumed + glyphAdvance * 0.5f) / Math.Max(1f, trackRadius) * Rad2Deg * spanScale;
            angles[i] = settings.CenterAngle - glyphCenterArc;
            consumed += glyphAdvance;
        }

        float firstAngle = angles[0];
        float lastAngle = angles[angles.Length - 1];
        SmoothBaseline baseline = CreateSmoothBaseline(firstAngle, lastAngle, settings, rootWidth, rootHeight);
        float[] progresses = BuildArcLengthProgresses(baseline, layoutProgresses);
        VanillaArcGlyphLayout[] glyphs = new VanillaArcGlyphLayout[textValue.Length];

        for (int i = 0; i < textValue.Length; i++)
        {
            float angle = angles[i];
            Point point = GetBezierPoint(baseline, progresses[i]);
            Point derivative = GetBezierDerivative(baseline, progresses[i]);
            float rotationZ = Clamp((float)(Math.Atan2(derivative.Y, derivative.X) * Rad2Deg), settings.RotationMin, settings.RotationMax);
            glyphs[i] = new VanillaArcGlyphLayout(
                textValue[i],
                angle,
                GetTrackRadius(angle, settings),
                GetArcRadius(angle, ArcInnerRadii),
                GetArcRadius(angle, ArcOuterRadii),
                point.X,
                point.Y,
                rotationZ,
                settings.FontScale);
        }

        NormalizeRotations(glyphs, settings);
        return glyphs;
    }

    private static ChineseTrackSettings GetTrackSettings(VanillaArcTextTrack track)
    {
        switch (track)
        {
            case VanillaArcTextTrack.InfectionLabelOuter:
                return new ChineseTrackSettings(centerAngle: 40f, minAngleSpan: 21f, maxAngleSpan: 29f, advanceScale: 23f, outerClearance: 48f, fontScale: 0.48f, fixedFirstGlyphAngle: float.NaN, rotationMin: -64f, rotationMax: -36f, smoothRotationStart: -40f, smoothRotationEnd: -56f, baselineHandleScale: 0.40f, rotationMaxStep: 5.0f, rotationMaxPositiveStep: 0.4f);
            case VanillaArcTextTrack.StaminaWeightUpper:
            case VanillaArcTextTrack.InfectionWeightInner:
            default:
                return new ChineseTrackSettings(centerAngle: 45f, minAngleSpan: 10f, maxAngleSpan: 18f, advanceScale: 22f, outerClearance: 22.5f, fontScale: 0.40f, fixedFirstGlyphAngle: 48.8f, rotationMin: -48f, rotationMax: -30f, smoothRotationStart: -36f, smoothRotationEnd: -42f, baselineHandleScale: 0.36f, rotationMaxStep: 2.0f, rotationMaxPositiveStep: 0.8f);
        }
    }

    private static float[] BuildGlyphAdvances(string text)
    {
        float[] advances = new float[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            advances[i] = GetGlyphAdvance(text[i]);
        }

        return advances;
    }

    private static float GetGlyphAdvance(char character)
    {
        if (char.IsWhiteSpace(character))
        {
            return 0.58f;
        }

        if (character >= '0' && character <= '9')
        {
            return 0.76f;
        }

        if (character == '%' || character == '.')
        {
            return 0.78f;
        }

        if (IsCjk(character))
        {
            return 1.16f;
        }

        return 0.80f;
    }

    private static bool IsCjk(char character)
    {
        return (character >= '\u3400' && character <= '\u9FFF') || (character >= '\uF900' && character <= '\uFAFF');
    }

    private static SmoothBaseline CreateSmoothBaseline(float firstAngle, float lastAngle, ChineseTrackSettings settings, float rootWidth, float rootHeight)
    {
        Point start = GetScaledTrackPoint(firstAngle, settings, rootWidth, rootHeight);
        Point end = GetScaledTrackPoint(lastAngle, settings, rootWidth, rootHeight);
        float chord = Math.Max(0.001f, Distance(start, end));
        float handleLength = chord * settings.BaselineHandleScale;
        Point startDirection = GetDirection(settings.SmoothRotationStart);
        Point endDirection = GetDirection(settings.SmoothRotationEnd);
        return new SmoothBaseline(
            start,
            Add(start, Multiply(startDirection, handleLength)),
            Subtract(end, Multiply(endDirection, handleLength)),
            end);
    }

    private static float[] BuildArcLengthProgresses(SmoothBaseline baseline, float[] layoutProgresses)
    {
        float[] sampleLengths = BuildBezierLengthSamples(baseline);
        float totalLength = sampleLengths[sampleLengths.Length - 1];
        float[] progresses = new float[layoutProgresses.Length];
        for (int i = 0; i < layoutProgresses.Length; i++)
        {
            progresses[i] = GetProgressAtLength(sampleLengths, totalLength * Clamp01(layoutProgresses[i]));
        }

        return progresses;
    }

    private static float[] BuildBezierLengthSamples(SmoothBaseline baseline)
    {
        float[] lengths = new float[BezierLengthSamples + 1];
        Point previous = GetBezierPoint(baseline, 0f);
        lengths[0] = 0f;
        for (int i = 1; i <= BezierLengthSamples; i++)
        {
            float progress = (float)i / BezierLengthSamples;
            Point current = GetBezierPoint(baseline, progress);
            lengths[i] = lengths[i - 1] + Distance(previous, current);
            previous = current;
        }

        return lengths;
    }

    private static float GetProgressAtLength(float[] sampleLengths, float targetLength)
    {
        if (targetLength <= 0f)
        {
            return 0f;
        }

        float totalLength = sampleLengths[sampleLengths.Length - 1];
        if (targetLength >= totalLength)
        {
            return 1f;
        }

        for (int i = 1; i < sampleLengths.Length; i++)
        {
            if (sampleLengths[i] < targetLength)
            {
                continue;
            }

            float segmentLength = sampleLengths[i] - sampleLengths[i - 1];
            float segmentProgress = segmentLength > 0.0001f ? (targetLength - sampleLengths[i - 1]) / segmentLength : 0f;
            return ((i - 1) + segmentProgress) / BezierLengthSamples;
        }

        return 1f;
    }

    private static void NormalizeRotations(VanillaArcGlyphLayout[] glyphs, ChineseTrackSettings settings)
    {
        if (glyphs.Length < 2)
        {
            return;
        }

        float previous = glyphs[0].RotationZ;
        for (int i = 1; i < glyphs.Length; i++)
        {
            float rotation = glyphs[i].RotationZ;
            if (settings.RotationMaxPositiveStep >= 0f && rotation > previous + settings.RotationMaxPositiveStep)
            {
                rotation = previous + settings.RotationMaxPositiveStep;
            }

            if (settings.RotationMaxStep > 0f && rotation < previous - settings.RotationMaxStep)
            {
                rotation = previous - settings.RotationMaxStep;
            }

            rotation = Clamp(rotation, settings.RotationMin, settings.RotationMax);
            glyphs[i] = new VanillaArcGlyphLayout(
                glyphs[i].Character,
                glyphs[i].Angle,
                glyphs[i].Radius,
                glyphs[i].StrokeInnerRadius,
                glyphs[i].StrokeOuterRadius,
                glyphs[i].LocalX,
                glyphs[i].LocalY,
                rotation,
                glyphs[i].FontScale);
            previous = rotation;
        }
    }

    private static float GetTrackRadius(float angle, ChineseTrackSettings settings)
    {
        return GetArcRadius(angle, ArcOuterRadii) + settings.OuterClearance;
    }

    private static Point GetScaledTrackPoint(float angle, ChineseTrackSettings settings, float rootWidth, float rootHeight)
    {
        float radius = GetTrackRadius(angle, settings);
        float radians = angle * Deg2Rad;
        return new Point(
            (float)Math.Cos(radians) * radius * (Math.Abs(rootWidth) / SpriteSize),
            (float)Math.Sin(radians) * radius * (Math.Abs(rootHeight) / SpriteSize));
    }

    private static float GetArcRadius(float angle, float[] radii)
    {
        if (angle <= ArcAngles[0])
        {
            return radii[0];
        }

        int last = ArcAngles.Length - 1;
        if (angle >= ArcAngles[last])
        {
            return radii[last];
        }

        for (int i = 1; i < ArcAngles.Length; i++)
        {
            if (angle > ArcAngles[i])
            {
                continue;
            }

            float startAngle = ArcAngles[i - 1];
            float endAngle = ArcAngles[i];
            float t = (angle - startAngle) / (endAngle - startAngle);
            return Lerp(radii[i - 1], radii[i], t);
        }

        return radii[last];
    }

    private static Point GetBezierPoint(SmoothBaseline baseline, float progress)
    {
        float t = Clamp01(progress);
        float inverse = 1f - t;
        float inverse2 = inverse * inverse;
        float t2 = t * t;
        return new Point(
            inverse2 * inverse * baseline.P0.X + 3f * inverse2 * t * baseline.P1.X + 3f * inverse * t2 * baseline.P2.X + t2 * t * baseline.P3.X,
            inverse2 * inverse * baseline.P0.Y + 3f * inverse2 * t * baseline.P1.Y + 3f * inverse * t2 * baseline.P2.Y + t2 * t * baseline.P3.Y);
    }

    private static Point GetBezierDerivative(SmoothBaseline baseline, float progress)
    {
        float t = Clamp01(progress);
        float inverse = 1f - t;
        return new Point(
            3f * inverse * inverse * (baseline.P1.X - baseline.P0.X) + 6f * inverse * t * (baseline.P2.X - baseline.P1.X) + 3f * t * t * (baseline.P3.X - baseline.P2.X),
            3f * inverse * inverse * (baseline.P1.Y - baseline.P0.Y) + 6f * inverse * t * (baseline.P2.Y - baseline.P1.Y) + 3f * t * t * (baseline.P3.Y - baseline.P2.Y));
    }

    private static Point GetDirection(float angle)
    {
        float radians = angle * Deg2Rad;
        return new Point((float)Math.Cos(radians), (float)Math.Sin(radians));
    }

    private static float Sum(float[] values)
    {
        float result = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            result += values[i];
        }

        return result;
    }

    private static float Distance(Point a, Point b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    private static Point Add(Point a, Point b)
    {
        return new Point(a.X + b.X, a.Y + b.Y);
    }

    private static Point Subtract(Point a, Point b)
    {
        return new Point(a.X - b.X, a.Y - b.Y);
    }

    private static Point Multiply(Point point, float scale)
    {
        return new Point(point.X * scale, point.Y * scale);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Clamp01(t);
    }

    private static float Clamp01(float value)
    {
        return Clamp(value, 0f, 1f);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    private const float Deg2Rad = (float)(Math.PI / 180.0);
    private const float Rad2Deg = (float)(180.0 / Math.PI);
    private const int BezierLengthSamples = 48;

    private struct ChineseTrackSettings
    {
        internal ChineseTrackSettings(float centerAngle, float minAngleSpan, float maxAngleSpan, float advanceScale, float outerClearance, float fontScale, float fixedFirstGlyphAngle, float rotationMin, float rotationMax, float smoothRotationStart, float smoothRotationEnd, float baselineHandleScale, float rotationMaxStep, float rotationMaxPositiveStep)
        {
            CenterAngle = centerAngle;
            MinAngleSpan = minAngleSpan;
            MaxAngleSpan = maxAngleSpan;
            AdvanceScale = advanceScale;
            OuterClearance = outerClearance;
            FontScale = fontScale;
            FixedFirstGlyphAngle = fixedFirstGlyphAngle;
            RotationMin = rotationMin;
            RotationMax = rotationMax;
            SmoothRotationStart = smoothRotationStart;
            SmoothRotationEnd = smoothRotationEnd;
            BaselineHandleScale = baselineHandleScale;
            RotationMaxStep = rotationMaxStep;
            RotationMaxPositiveStep = rotationMaxPositiveStep;
        }

        internal readonly float CenterAngle;
        internal readonly float MinAngleSpan;
        internal readonly float MaxAngleSpan;
        internal readonly float AdvanceScale;
        internal readonly float OuterClearance;
        internal readonly float FontScale;
        internal readonly float FixedFirstGlyphAngle;
        internal readonly float RotationMin;
        internal readonly float RotationMax;
        internal readonly float SmoothRotationStart;
        internal readonly float SmoothRotationEnd;
        internal readonly float BaselineHandleScale;
        internal readonly float RotationMaxStep;
        internal readonly float RotationMaxPositiveStep;
    }

    private struct Point
    {
        internal Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        internal readonly float X;
        internal readonly float Y;
    }

    private struct SmoothBaseline
    {
        internal SmoothBaseline(Point p0, Point p1, Point p2, Point p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        internal readonly Point P0;
        internal readonly Point P1;
        internal readonly Point P2;
        internal readonly Point P3;
    }
}
}
