using System.Globalization;
using HarmonyLib;

namespace STS2OrchisNecrobinderSkinFix.Compat;

internal static class OrchisVisualSettingsCompat
{
    private const string InteropTypeName = "OrchisNecrobinderSkinMod.Scripts.VisualSettingsInterop";

    public static float GetFloat(string propertyName, float fallback)
    {
        try
        {
            var interopType = AccessTools.TypeByName(InteropTypeName);
            var settings = AccessTools.Property(interopType, "Settings")?.GetValue(null);
            var value = settings == null
                ? null
                : AccessTools.Property(settings.GetType(), propertyName)?.GetValue(settings);
            return value == null ? fallback : Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return fallback;
        }
    }
}
