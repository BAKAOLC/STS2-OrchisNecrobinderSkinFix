using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2OrchisNecrobinderSkinFix.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class NRelicReadyPatch : IPatchMethod
{
    private static readonly FieldInfo? ModelField = AccessTools.Field(typeof(NRelic), "_model");

    private static readonly LogLimiter ModelUnsetLog =
        new("Skipped replacement relic outline update because NRelic.Model is not set");

    public static string PatchId => "orchis_safe_relic_ready_outline";
    public static bool IsCritical => false;
    public static string Description => "Safely update Orchis replacement relic outlines after NRelic._Ready";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(NRelic), nameof(NRelic._Ready), Type.EmptyTypes, true)
        ];
    }

    private static void Postfix(NRelic __instance)
    {
        if (ModelField?.GetValue(__instance) is not RelicModel model)
        {
            ModelUnsetLog.Info();
            return;
        }

        if (__instance.Outline != null) __instance.Outline.Texture = model.IconOutline;
    }
}