using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using STS2OrchisNecrobinderSkinFix.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class MegaAnimationStateRestSiteDefaultAnimationPatch : IPatchMethod
{
    private static readonly HashSet<string> VanillaRestSiteAnimations =
    [
        "overgrowth_loop",
        "hive_loop",
        "glory_loop"
    ];

    private static readonly LogLimiter SuppressedVanillaAnimationLog =
        new("Skipped vanilla Necrobinder rest-site default animation because Orchis replaced the skeleton");

    public static string PatchId => "orchis_rest_site_default_set_animation_guard";
    public static bool IsCritical => false;

    public static string Description =>
        "Skip vanilla Necrobinder rest-site default animations after Orchis skeleton replacement";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(
                typeof(MegaAnimationState),
                nameof(MegaAnimationState.SetAnimation),
                [typeof(string), typeof(bool), typeof(int)],
                true)
        ];
    }

    private static bool Prefix(string animationName, int trackId)
    {
        if (!RestSiteReadyContext.IsNecrobinderReadyActive) return true;
        if (trackId != 0 || !VanillaRestSiteAnimations.Contains(animationName)) return true;

        RestSiteReadyContext.SuppressNextVanillaDefaultTrackRead();
        SuppressedVanillaAnimationLog.Info(animationName);
        return false;
    }
}

internal sealed class MegaAnimationStateRestSiteDefaultTrackReadPatch : IPatchMethod
{
    public static string PatchId => "orchis_rest_site_default_get_current_guard";
    public static bool IsCritical => false;

    public static string Description =>
        "Skip the GetCurrent call that follows skipped vanilla Necrobinder rest-site animation setup";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(
                typeof(MegaAnimationState),
                nameof(MegaAnimationState.GetCurrent),
                [typeof(int)],
                true)
        ];
    }

    private static bool Prefix(int trackIndex, ref MegaTrackEntry? __result)
    {
        if (!RestSiteReadyContext.TryConsumeSuppressedVanillaDefaultTrackRead(trackIndex)) return true;

        __result = null;
        return false;
    }
}