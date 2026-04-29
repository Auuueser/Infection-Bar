using System;

namespace IndependentCadaverInfectionBar
{
internal enum VanillaArcTextTrack
{
    StaminaWeightUpper,
    InfectionWeightInner,
    InfectionLabelOuter
}

internal struct VanillaArcGlyphLayout
{
    internal VanillaArcGlyphLayout(char character, float angle, float radius, float strokeInnerRadius, float strokeOuterRadius, float localX, float localY, float rotationZ, float fontScale)
    {
        Character = character;
        Angle = angle;
        Radius = radius;
        StrokeInnerRadius = strokeInnerRadius;
        StrokeOuterRadius = strokeOuterRadius;
        LocalX = localX;
        LocalY = localY;
        RotationZ = rotationZ;
        FontScale = fontScale;
    }

    internal readonly char Character;

    internal readonly float Angle;

    internal readonly float Radius;

    internal readonly float StrokeInnerRadius;

    internal readonly float StrokeOuterRadius;

    internal readonly float LocalX;

    internal readonly float LocalY;

    internal readonly float RotationZ;

    internal readonly float FontScale;
}

internal static class VanillaArcTextLayout
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

        if (ContainsCjk(textValue))
        {
            return VanillaChineseArcTextLayout.Build(textValue, track, rootWidth, rootHeight);
        }

        TrackSettings settings = GetTrackSettings(track, textValue);
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

        VanillaArcGlyphLayout[] glyphs = new VanillaArcGlyphLayout[textValue.Length];
        float firstAngle = angles[0];
        float lastAngle = angles[angles.Length - 1];
        Point[] baselinePoints = null;
        float[] baselineRotations = null;
        SmoothBaseline smoothBaseline;
        bool useBaseline = TryCreateSmoothBaseline(firstAngle, lastAngle, settings, rootWidth, rootHeight, out smoothBaseline);
        if (useBaseline)
        {
            float[] smoothProgresses = BuildSmoothArcLengthProgresses(smoothBaseline, layoutProgresses);
            baselinePoints = BuildSmoothBaselinePoints(smoothProgresses, smoothBaseline);
            baselineRotations = BuildSmoothBaselineRotations(smoothProgresses, smoothBaseline, settings);
        }
        else
        {
            Point[] rawBaselinePoints;
            FittedBaseline fittedBaseline;
            useBaseline = TryCreateFittedBaseline(angles, settings, rootWidth, rootHeight, out rawBaselinePoints, out fittedBaseline);
            if (useBaseline)
            {
                baselinePoints = BuildFittedBaselinePoints(rawBaselinePoints, fittedBaseline);
                baselineRotations = BuildBaselineRotations(baselinePoints, settings);
            }
        }

        for (int i = 0; i < textValue.Length; i++)
        {
            float angle = angles[i];
            float strokeInner = GetArcRadius(angle, ArcInnerRadii);
            float strokeOuter = GetArcRadius(angle, ArcOuterRadii);
            float radius = GetTrackRadius(angle, settings);
            float localX;
            float localY;
            float rotationZ;
            if (useBaseline)
            {
                Point point = baselinePoints[i];
                localX = point.X;
                localY = point.Y;
                rotationZ = baselineRotations[i];
            }
            else
            {
                float radians = angle * Deg2Rad;
                localX = (float)Math.Cos(radians) * radius * (Math.Abs(rootWidth) / SpriteSize);
                localY = (float)Math.Sin(radians) * radius * (Math.Abs(rootHeight) / SpriteSize);
                rotationZ = GetGlyphRotation(angle, firstAngle, lastAngle, settings);
            }

            glyphs[i] = new VanillaArcGlyphLayout(
                textValue[i],
                angle,
                radius,
                strokeInner,
                strokeOuter,
                localX,
                localY,
                rotationZ,
                settings.FontScale);
        }

        return glyphs;
    }

    private static TrackSettings GetTrackSettings(VanillaArcTextTrack track, string text)
    {
        bool containsCjk = ContainsCjk(text);
        switch (track)
        {
            case VanillaArcTextTrack.StaminaWeightUpper:
            case VanillaArcTextTrack.InfectionWeightInner:
                return containsCjk
                    ? new TrackSettings(centerAngle: 45f, minAngleSpan: 8f, maxAngleSpan: 16f, advanceScale: 22f, innerClearance: 0f, outerClearance: 22.5f, fontScale: 0.40f, insideStroke: false, rotationMin: -46f, rotationMax: -28f, fixedFirstGlyphAngle: 48.8f, smoothRotationStart: -37f, smoothRotationEnd: -42f, baselineHandleScale: 0.32f, baselineFitDegree: 0, rotationMaxStep: 1.6f, rotationMaxPositiveStep: 0.8f)
                    : new TrackSettings(centerAngle: 50f, minAngleSpan: 9f, maxAngleSpan: 28f, advanceScale: 22f, innerClearance: 0f, outerClearance: 22.5f, fontScale: 0.40f, insideStroke: false, rotationMin: -46f, rotationMax: -28f, fixedFirstGlyphAngle: 53.7f, smoothRotationStart: -37f, smoothRotationEnd: -42f, baselineHandleScale: 0.32f, baselineFitDegree: 0, rotationMaxStep: 1.6f, rotationMaxPositiveStep: 0.8f);
            case VanillaArcTextTrack.InfectionLabelOuter:
                return containsCjk
                    ? new TrackSettings(centerAngle: 40f, minAngleSpan: 19f, maxAngleSpan: 27f, advanceScale: 28f, innerClearance: 0f, outerClearance: 44f, fontScale: 0.50f, insideStroke: false, rotationMin: -62f, rotationMax: -38f, baselineFitDegree: 1)
                    : new TrackSettings(centerAngle: 37f, minAngleSpan: 24f, maxAngleSpan: 39f, advanceScale: 22f, innerClearance: 0f, outerClearance: 46f, fontScale: 0.44f, insideStroke: false, rotationMin: -74f, rotationMax: -28f, smoothRotationStart: -37f, smoothRotationEnd: -65f, baselineHandleScale: 0.42f, baselineFitDegree: 0);
            default:
                return containsCjk
                    ? new TrackSettings(centerAngle: 45f, minAngleSpan: 8f, maxAngleSpan: 16f, advanceScale: 22f, innerClearance: 0f, outerClearance: 22.5f, fontScale: 0.40f, insideStroke: false, rotationMin: -46f, rotationMax: -28f, fixedFirstGlyphAngle: 48.8f, smoothRotationStart: -37f, smoothRotationEnd: -42f, baselineHandleScale: 0.32f, baselineFitDegree: 0, rotationMaxStep: 1.6f, rotationMaxPositiveStep: 0.8f)
                    : new TrackSettings(centerAngle: 50f, minAngleSpan: 9f, maxAngleSpan: 28f, advanceScale: 22f, innerClearance: 0f, outerClearance: 22.5f, fontScale: 0.40f, insideStroke: false, rotationMin: -46f, rotationMax: -28f, fixedFirstGlyphAngle: 53.7f, smoothRotationStart: -37f, smoothRotationEnd: -42f, baselineHandleScale: 0.32f, baselineFitDegree: 0, rotationMaxStep: 1.6f, rotationMaxPositiveStep: 0.8f);
        }
    }

    private static bool ContainsCjk(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if ((character >= '\u3400' && character <= '\u9FFF') || (character >= '\uF900' && character <= '\uFAFF'))
            {
                return true;
            }
        }

        return false;
    }

    private static float[] BuildGlyphAdvances(string text)
    {
        float[] advances = new float[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            advances[i] = GetGlyphAdvanceWeight(text, i);
        }

        return advances;
    }

    private static float GetGlyphAdvanceWeight(string text, int index)
    {
        char character = text[index];
        if (char.IsWhiteSpace(character))
        {
            if (index + 2 < text.Length && text[index + 1] == 'l' && text[index + 2] == 'b')
            {
                return 0.72f;
            }

            return 0.38f;
        }

        if (character == 'l' && index + 1 < text.Length && text[index + 1] == 'b')
        {
            return 0.76f;
        }

        if (character == 'b' && index > 0 && text[index - 1] == 'l')
        {
            return 0.75f;
        }

        if (character >= '0' && character <= '9')
        {
            if (index + 1 < text.Length && text[index + 1] == '%')
            {
                return 1.05f;
            }

            return 0.58f;
        }

        if (character == '%' && index > 0 && text[index - 1] >= '0' && text[index - 1] <= '9')
        {
            return 1.65f;
        }

        if (character == '%' || character == '.')
        {
            return 0.52f;
        }

        if (character == 'i' || character == 'l' || character == 'I')
        {
            return 0.44f;
        }

        if (character == 'f' || character == 't' || character == 'r')
        {
            return 0.48f;
        }

        if (character == 'm' || character == 'w' || character == 'M' || character == 'W')
        {
            return 0.78f;
        }

        if (character < 128)
        {
            return 0.60f;
        }

        return 1f;
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

    private static float GetTrackRadius(float angle, TrackSettings settings)
    {
        float strokeInner = GetArcRadius(angle, ArcInnerRadii);
        float strokeOuter = GetArcRadius(angle, ArcOuterRadii);
        return settings.InsideStroke
            ? Math.Max(0f, strokeInner - settings.InnerClearance)
            : strokeOuter + settings.OuterClearance;
    }

    private static float GetArcRadius(float angle, float[] radii)
    {
        if (angle <= ArcAngles[0])
        {
            return radii[0];
        }

        int lastIndex = ArcAngles.Length - 1;
        if (angle >= ArcAngles[lastIndex])
        {
            return radii[lastIndex];
        }

        for (int i = 0; i < lastIndex; i++)
        {
            float startAngle = ArcAngles[i];
            float endAngle = ArcAngles[i + 1];
            if (angle < startAngle || angle > endAngle)
            {
                continue;
            }

            float t = (angle - startAngle) / (endAngle - startAngle);
            return Lerp(radii[i], radii[i + 1], t);
        }

        return radii[lastIndex];
    }

    private static float GetTangentRotation(float angle, float radius, TrackSettings settings)
    {
        const float sampleStep = 1.2f;
        float previousAngle = angle + sampleStep;
        float nextAngle = angle - sampleStep;
        Point previous = GetPoint(previousAngle, GetTrackRadius(previousAngle, settings));
        Point next = GetPoint(nextAngle, GetTrackRadius(nextAngle, settings));
        float tangentAngle = (float)(Math.Atan2(next.Y - previous.Y, next.X - previous.X) * Rad2Deg);
        return Clamp(tangentAngle, settings.RotationMin, settings.RotationMax);
    }

    private static bool TryCreateFittedBaseline(float[] angles, TrackSettings settings, float rootWidth, float rootHeight, out Point[] points, out FittedBaseline baseline)
    {
        points = null;
        baseline = default(FittedBaseline);
        if (settings.BaselineFitDegree <= 0 || angles.Length < 2)
        {
            return false;
        }

        points = new Point[angles.Length];
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int i = 0; i < angles.Length; i++)
        {
            points[i] = GetScaledTrackPoint(angles[i], settings, rootWidth, rootHeight);
            minX = Math.Min(minX, points[i].X);
            maxX = Math.Max(maxX, points[i].X);
        }

        float xCenter = (minX + maxX) * 0.5f;
        float xScale = Math.Max(1f, (maxX - minX) * 0.5f);
        int degree = Math.Min(settings.BaselineFitDegree, points.Length - 1);
        int size = degree + 1;
        double[,] matrix = new double[size, size];
        double[] rhs = new double[size];

        for (int i = 0; i < points.Length; i++)
        {
            double u = (points[i].X - xCenter) / xScale;
            double[] powers = new double[size * 2 - 1];
            powers[0] = 1.0;
            for (int p = 1; p < powers.Length; p++)
            {
                powers[p] = powers[p - 1] * u;
            }

            for (int row = 0; row < size; row++)
            {
                rhs[row] += points[i].Y * powers[row];
                for (int col = 0; col < size; col++)
                {
                    matrix[row, col] += powers[row + col];
                }
            }
        }

        double[] coefficients = SolveLinearSystem(matrix, rhs, size);
        if (coefficients == null)
        {
            return false;
        }

        baseline = new FittedBaseline(coefficients, degree, xCenter, xScale);
        return true;
    }

    private static double[] SolveLinearSystem(double[,] matrix, double[] rhs, int size)
    {
        for (int pivot = 0; pivot < size; pivot++)
        {
            int bestRow = pivot;
            double bestValue = Math.Abs(matrix[pivot, pivot]);
            for (int row = pivot + 1; row < size; row++)
            {
                double value = Math.Abs(matrix[row, pivot]);
                if (value > bestValue)
                {
                    bestValue = value;
                    bestRow = row;
                }
            }

            if (bestValue < 0.0000001)
            {
                return null;
            }

            if (bestRow != pivot)
            {
                for (int col = pivot; col < size; col++)
                {
                    double temp = matrix[pivot, col];
                    matrix[pivot, col] = matrix[bestRow, col];
                    matrix[bestRow, col] = temp;
                }

                double rhsTemp = rhs[pivot];
                rhs[pivot] = rhs[bestRow];
                rhs[bestRow] = rhsTemp;
            }

            double divisor = matrix[pivot, pivot];
            for (int col = pivot; col < size; col++)
            {
                matrix[pivot, col] /= divisor;
            }

            rhs[pivot] /= divisor;
            for (int row = 0; row < size; row++)
            {
                if (row == pivot)
                {
                    continue;
                }

                double factor = matrix[row, pivot];
                for (int col = pivot; col < size; col++)
                {
                    matrix[row, col] -= factor * matrix[pivot, col];
                }

                rhs[row] -= factor * rhs[pivot];
            }
        }

        return rhs;
    }

    private static float EvaluatePolynomial(FittedBaseline baseline, float x)
    {
        double u = (x - baseline.XCenter) / baseline.XScale;
        double result = 0.0;
        for (int i = baseline.Degree; i >= 0; i--)
        {
            result = result * u + baseline.Coefficients[i];
        }

        return (float)result;
    }

    private static Point[] BuildFittedBaselinePoints(Point[] rawPoints, FittedBaseline baseline)
    {
        Point[] points = new Point[rawPoints.Length];
        for (int i = 0; i < rawPoints.Length; i++)
        {
            points[i] = new Point(rawPoints[i].X, EvaluatePolynomial(baseline, rawPoints[i].X));
        }

        return points;
    }

    private static Point[] BuildSmoothBaselinePoints(float[] progresses, SmoothBaseline baseline)
    {
        Point[] points = new Point[progresses.Length];
        for (int i = 0; i < progresses.Length; i++)
        {
            points[i] = GetBezierPoint(baseline, progresses[i]);
        }

        return points;
    }

    private static float[] BuildSmoothBaselineRotations(float[] progresses, SmoothBaseline baseline, TrackSettings settings)
    {
        float[] rotations = new float[progresses.Length];
        for (int i = 0; i < progresses.Length; i++)
        {
            Point derivative = GetBezierDerivative(baseline, progresses[i]);
            rotations[i] = Clamp((float)(Math.Atan2(derivative.Y, derivative.X) * Rad2Deg), settings.RotationMin, settings.RotationMax);
        }

        NormalizeBaselineRotations(rotations, settings);
        return rotations;
    }

    private static float[] BuildSmoothArcLengthProgresses(SmoothBaseline baseline, float[] layoutProgresses)
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

    private static float[] BuildBaselineRotations(Point[] points, TrackSettings settings)
    {
        float[] rotations = new float[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Point previous = i == 0 ? points[i] : points[i - 1];
            Point next = i == points.Length - 1 ? points[i] : points[i + 1];
            if (i > 0 && i < points.Length - 1)
            {
                previous = points[i - 1];
                next = points[i + 1];
            }

            rotations[i] = Clamp((float)(Math.Atan2(next.Y - previous.Y, next.X - previous.X) * Rad2Deg), settings.RotationMin, settings.RotationMax);
        }

        NormalizeBaselineRotations(rotations, settings);
        return rotations;
    }

    private static void NormalizeBaselineRotations(float[] rotations, TrackSettings settings)
    {
        if (rotations.Length < 2 || (settings.RotationMaxStep <= 0f && settings.RotationMaxPositiveStep < 0f))
        {
            return;
        }

        for (int i = 1; i < rotations.Length; i++)
        {
            float previous = rotations[i - 1];
            if (settings.RotationMaxPositiveStep >= 0f && rotations[i] > previous + settings.RotationMaxPositiveStep)
            {
                rotations[i] = previous + settings.RotationMaxPositiveStep;
            }

            if (settings.RotationMaxStep > 0f && rotations[i] < previous - settings.RotationMaxStep)
            {
                rotations[i] = previous - settings.RotationMaxStep;
            }

            rotations[i] = Clamp(rotations[i], settings.RotationMin, settings.RotationMax);
        }
    }

    private static bool TryCreateSmoothBaseline(float firstAngle, float lastAngle, TrackSettings settings, float rootWidth, float rootHeight, out SmoothBaseline baseline)
    {
        baseline = default(SmoothBaseline);
        if (float.IsNaN(settings.SmoothRotationStart) || float.IsNaN(settings.SmoothRotationEnd))
        {
            return false;
        }

        Point start = GetScaledTrackPoint(firstAngle, settings, rootWidth, rootHeight);
        Point end = GetScaledTrackPoint(lastAngle, settings, rootWidth, rootHeight);
        float chord = Distance(start, end);
        if (chord < 0.001f)
        {
            return false;
        }

        float handleLength = chord * settings.BaselineHandleScale;
        Point startDirection = GetDirection(settings.SmoothRotationStart);
        Point endDirection = GetDirection(settings.SmoothRotationEnd);
        baseline = new SmoothBaseline(
            start,
            Add(start, Multiply(startDirection, handleLength)),
            Subtract(end, Multiply(endDirection, handleLength)),
            end);
        return true;
    }

    private static Point GetScaledTrackPoint(float angle, TrackSettings settings, float rootWidth, float rootHeight)
    {
        float radius = GetTrackRadius(angle, settings);
        float radians = angle * Deg2Rad;
        return new Point(
            (float)Math.Cos(radians) * radius * (Math.Abs(rootWidth) / SpriteSize),
            (float)Math.Sin(radians) * radius * (Math.Abs(rootHeight) / SpriteSize));
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

    private static float GetGlyphRotation(float angle, float firstAngle, float lastAngle, TrackSettings settings)
    {
        if (!float.IsNaN(settings.SmoothRotationStart) && !float.IsNaN(settings.SmoothRotationEnd))
        {
            float progress = GetArcProgress(angle, firstAngle, lastAngle);
            float easedProgress = SmoothStep(progress);
            return Clamp(Lerp(settings.SmoothRotationStart, settings.SmoothRotationEnd, easedProgress), settings.RotationMin, settings.RotationMax);
        }

        float rotation = GetTangentRotation(angle, GetTrackRadius(angle, settings), settings);
        if (!float.IsNaN(settings.FixedFirstGlyphAngle) && settings.RotationProgressStrength > 0f)
        {
            rotation -= Math.Max(0f, settings.FixedFirstGlyphAngle - angle) * settings.RotationProgressStrength;
            rotation = Clamp(rotation, settings.RotationMin, settings.RotationMax);
        }

        return rotation;
    }

    private static float GetArcProgress(float angle, float firstAngle, float lastAngle)
    {
        float denominator = firstAngle - lastAngle;
        if (Math.Abs(denominator) < 0.0001f)
        {
            return 0f;
        }

        return Clamp01((firstAngle - angle) / denominator);
    }

    private static float SmoothStep(float value)
    {
        float t = Clamp01(value);
        return t * t * (3f - 2f * t);
    }

    private static Point GetPoint(float angle, float radius)
    {
        float radians = angle * Deg2Rad;
        return new Point((float)Math.Cos(radians) * radius, (float)Math.Sin(radians) * radius);
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

    private struct TrackSettings
    {
        internal TrackSettings(float centerAngle, float minAngleSpan, float maxAngleSpan, float advanceScale, float innerClearance, float outerClearance, float fontScale, bool insideStroke, float rotationMin, float rotationMax, float fixedFirstGlyphAngle = float.NaN, float rotationProgressStrength = 0f, float smoothRotationStart = float.NaN, float smoothRotationEnd = float.NaN, float baselineHandleScale = 0.42f, int baselineFitDegree = 0, float rotationMaxStep = 0f, float rotationMaxPositiveStep = -1f)
        {
            CenterAngle = centerAngle;
            MinAngleSpan = minAngleSpan;
            MaxAngleSpan = maxAngleSpan;
            AdvanceScale = advanceScale;
            InnerClearance = innerClearance;
            OuterClearance = outerClearance;
            FontScale = fontScale;
            InsideStroke = insideStroke;
            RotationMin = rotationMin;
            RotationMax = rotationMax;
            FixedFirstGlyphAngle = fixedFirstGlyphAngle;
            RotationProgressStrength = rotationProgressStrength;
            SmoothRotationStart = smoothRotationStart;
            SmoothRotationEnd = smoothRotationEnd;
            BaselineHandleScale = baselineHandleScale;
            BaselineFitDegree = baselineFitDegree;
            RotationMaxStep = rotationMaxStep;
            RotationMaxPositiveStep = rotationMaxPositiveStep;
        }

        internal readonly float CenterAngle;

        internal readonly float MinAngleSpan;

        internal readonly float MaxAngleSpan;

        internal readonly float AdvanceScale;

        internal readonly float InnerClearance;

        internal readonly float OuterClearance;

        internal readonly float FontScale;

        internal readonly bool InsideStroke;

        internal readonly float RotationMin;

        internal readonly float RotationMax;

        internal readonly float FixedFirstGlyphAngle;

        internal readonly float RotationProgressStrength;

        internal readonly float SmoothRotationStart;

        internal readonly float SmoothRotationEnd;

        internal readonly float BaselineHandleScale;

        internal readonly int BaselineFitDegree;

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

    private struct FittedBaseline
    {
        internal FittedBaseline(double[] coefficients, int degree, float xCenter, float xScale)
        {
            Coefficients = coefficients;
            Degree = degree;
            XCenter = xCenter;
            XScale = xScale;
        }

        internal readonly double[] Coefficients;

        internal readonly int Degree;

        internal readonly float XCenter;

        internal readonly float XScale;
    }
}
}
