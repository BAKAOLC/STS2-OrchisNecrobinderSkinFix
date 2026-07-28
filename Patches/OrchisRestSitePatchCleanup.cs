using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal static class OrchisRestSitePatchCleanup
{
    private const string CreatePatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+RestSiteCharacterCreatePatch";

    private const string ReadyPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+RestSiteCharacterReadyPatch";

    public static void RemoveOriginalPostfixes()
    {
        RemovePostfix(
            AccessTools.DeclaredMethod(
                typeof(NRestSiteCharacter),
                nameof(NRestSiteCharacter.Create),
                [typeof(Player), typeof(int)]),
            CreatePatchTypeName,
            "NRestSiteCharacter.Create");

        RemovePostfix(
            AccessTools.DeclaredMethod(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready), Type.EmptyTypes),
            ReadyPatchTypeName,
            "NRestSiteCharacter._Ready");
    }

    private static void RemovePostfix(MethodBase? original, string patchTypeName, string targetName)
    {
        if (original == null)
        {
            Main.Logger.Warn($"Optional patch cleanup target {targetName} was not found.");
            return;
        }

        var postfixes = Harmony.GetPatchInfo(original)?.Postfixes
            .Where(patch => patch.owner == Const.OrchisHarmonyId &&
                            patch.PatchMethod.DeclaringType?.FullName == patchTypeName)
            .ToArray() ?? [];

        var harmony = new Harmony(Const.HarmonyId);
        foreach (var postfix in postfixes) harmony.Unpatch(original, postfix.PatchMethod);

        Main.Logger.Info($"Removed {postfixes.Length} Orchis {targetName} postfix patch(es).");
    }
}