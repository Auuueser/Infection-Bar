namespace IndependentCadaverInfectionBar
{
internal static class VanillaWarningTextOffsetCalculator
{
    internal static void CalculateShiftedPosition(bool enabled, float originalX, float originalY, float offsetX, float offsetY, out float shiftedX, out float shiftedY)
    {
        shiftedX = enabled ? originalX + offsetX : originalX;
        shiftedY = enabled ? originalY + offsetY : originalY;
    }
}
}
