using System;
namespace IndependentCadaverInfectionBar;

// Conservative 4-pixel row bounds sampled from the original 326x326 SprintMeter alpha.
// Only geometry is stored here; no game texture is distributed.
internal static class VanillaRingTextClearance
{
    private static readonly float[] Bands = { 95f, 135f, 98f, 139f, 94f, 131f, 106f, 135f, 102f, 127f, 113f, 131f, 107f, 123f, 127f, 127f, 114f, 119f, 125f, 123f, 116f, 115f, 128f, 119f, 114f, 111f, 132f, 115f, 124f, 107f, 135f, 111f, 127f, 103f, 137f, 107f, 130f, 99f, 140f, 103f, 132f, 95f, 141f, 99f, 134f, 91f, 143f, 95f, 135f, 87f, 144f, 91f, 137f, 83f, 145f, 87f, 135f, 79f, 150f, 83f, 133f, 75f, 147f, 79f, 139f, 71f, 148f, 75f, 139f, 67f, 148f, 71f, 139f, 63f, 149f, 67f, 139f, 59f, 148f, 63f, 139f, 55f, 148f, 59f, 139f, 51f, 148f, 55f, 138f, 47f, 148f, 51f, 138f, 43f, 148f, 47f, 137f, 39f, 146f, 43f, 137f, 35f, 145f, 39f, 132f, 15f, 139f, 19f, 127f, 11f, 139f, 15f, 125f, 7f, 137f, 11f, 124f, 3f, 136f, 7f, 122f, -1f, 134f, 3f, 121f, -5f, 132f, -1f, 118f, -9f, 130f, -5f, 112f, -13f, 128f, -9f, 114f, -17f, 127f, -13f, 112f, -21f, 129f, -17f, 108f, -25f, 121f, -21f, 106f, -29f, 118f, -25f, 103f, -33f, 115f, -29f, 100f, -37f, 112f, -33f, 96f, -41f, 109f, -37f, -143f, -45f, 106f, -41f, -144f, -49f, 102f, -45f, -144f, -53f, 99f, -49f, -150f, -57f, 96f, -53f, -144f, -61f, 92f, -57f, -144f, -65f, 89f, -61f, -144f, -69f, 90f, -65f, -144f, -73f, 81f, -69f, -143f, -77f, 77f, -73f, -142f, -81f, 72f, -77f, -141f, -85f, 67f, -81f, -139f, -89f, 62f, -85f, -141f, -93f, 56f, -89f, -141f, -97f, 51f, -93f, -133f, -101f, 45f, -97f, -131f, -105f, 39f, -101f, -128f, -109f, 41f, -105f, -120f, -113f, 25f, -109f, -121f, -117f, 17f, -113f, -116f, -121f, 7f, -117f, -111f, -125f, -7f, -121f, -104f, -129f, -13f, -125f, -93f, -133f, -27f, -129f, -94f, -137f, -38f, -133f, -95f, -141f, -38f, -137f };

    internal static float RightEdge(float width, float height,
        float xx, float xy, float tx, float yx, float yy, float ty, float minY, float maxY)
    {
        float right = float.NegativeInfinity;
        float sx = Math.Abs(width) / 326f, sy = Math.Abs(height) / 326f;
        for (int i = 0; i < Bands.Length; i += 4)
        {
            float x0 = Bands[i] * sx, y0 = Bands[i + 1] * sy;
            float x1 = Bands[i + 2] * sx, y1 = Bands[i + 3] * sy;
            float ax = xx*x0+xy*y0+tx, ay = yx*x0+yy*y0+ty;
            float bx = xx*x1+xy*y0+tx, by = yx*x1+yy*y0+ty;
            float cx = xx*x1+xy*y1+tx, cy = yx*x1+yy*y1+ty;
            float dx = xx*x0+xy*y1+tx, dy = yx*x0+yy*y1+ty;
            IncludeEdge(ax, ay, bx, by, minY, maxY, ref right);
            IncludeEdge(bx, by, cx, cy, minY, maxY, ref right);
            IncludeEdge(cx, cy, dx, dy, minY, maxY, ref right);
            IncludeEdge(dx, dy, ax, ay, minY, maxY, ref right);
        }
        return right;
    }

    private static void IncludeEdge(float x0, float y0, float x1, float y1,
        float minY, float maxY, ref float right)
    {
        if (y0 >= minY && y0 <= maxY) right = Math.Max(right, x0);
        if (y1 >= minY && y1 <= maxY) right = Math.Max(right, x1);
        if (Math.Abs(y1-y0) < 0.00001f) return;
        float t = (minY-y0)/(y1-y0);
        if (t>=0f && t<=1f) right = Math.Max(right, x0+(x1-x0)*t);
        t = (maxY-y0)/(y1-y0);
        if (t>=0f && t<=1f) right = Math.Max(right, x0+(x1-x0)*t);
    }
}
