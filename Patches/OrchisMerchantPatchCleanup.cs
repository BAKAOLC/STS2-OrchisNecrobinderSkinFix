using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal static class OrchisMerchantPatchCleanup
{
    private const string OrchisMerchantReadyPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+MerchantCharacterReadyPatch";

    public static void RemoveOriginalPostfix()
    {
        var original = AccessTools.DeclaredMethod(
            typeof(NMerchantCharacter),
            nameof(NMerchantCharacter._Ready),
            Type.EmptyTypes);
        if (original == null)
        {
            Main.Logger.Warn("Optional patch cleanup target NMerchantCharacter._Ready was not found.");
            return;
        }

        var postfixes = Harmony.GetPatchInfo(original)?.Postfixes
            .Where(patch => patch.owner == Const.OrchisHarmonyId &&
                            patch.PatchMethod.DeclaringType?.FullName == OrchisMerchantReadyPatchTypeName)
            .ToArray() ?? [];

        var harmony = new Harmony(Const.HarmonyId);
        foreach (var postfix in postfixes) harmony.Unpatch(original, postfix.PatchMethod);

        Main.Logger.Info($"Removed {postfixes.Length} Orchis NMerchantCharacter._Ready postfix patch(es).");
    }
}