using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal static class OrchisRelicReadyPatchCleanup
{
    private const string OrchisRelicReadyPatchTypeName = "OrchisNecrobinderSkinMod.Scripts.NRelicReadyPatch";

    public static void RemoveUnsafePostfix()
    {
        var original = AccessTools.DeclaredMethod(typeof(NRelic), nameof(NRelic._Ready), Type.EmptyTypes);
        if (original == null)
        {
            Main.Logger.Warn("Optional patch cleanup target NRelic._Ready was not found.");
            return;
        }

        var patchInfo = Harmony.GetPatchInfo(original);
        var orchisPostfixes = patchInfo?.Postfixes
            .Where(patch => patch.owner == Const.OrchisHarmonyId &&
                            patch.PatchMethod.DeclaringType?.FullName == OrchisRelicReadyPatchTypeName)
            .ToArray() ?? [];

        var harmony = new Harmony(Const.HarmonyId);
        foreach (var patch in orchisPostfixes) harmony.Unpatch(original, patch.PatchMethod);

        Main.Logger.Info($"Removed {orchisPostfixes.Length} Orchis NRelic._Ready postfix patch(es).");
    }
}