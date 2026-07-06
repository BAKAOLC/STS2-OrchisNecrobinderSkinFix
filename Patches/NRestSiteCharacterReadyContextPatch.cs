using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class NRestSiteCharacterReadyContextPatch : IPatchMethod
{
    public static string PatchId => "orchis_rest_site_ready_context";
    public static bool IsCritical => false;
    public static string Description => "Track Necrobinder rest-site ready while vanilla animation callbacks run";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready), Type.EmptyTypes, true)
        ];
    }

    private static void Prefix(NRestSiteCharacter __instance)
    {
        RestSiteReadyContext.Enter(__instance);
    }

    private static Exception? Finalizer(Exception? __exception)
    {
        RestSiteReadyContext.Exit();
        return __exception;
    }
}

internal static class RestSiteReadyContext
{
    [ThreadStatic] private static int depth;

    [ThreadStatic] private static int suppressedDefaultTrackReads;

    public static bool IsNecrobinderReadyActive => depth > 0;

    public static void Enter(NRestSiteCharacter restSiteCharacter)
    {
        if (restSiteCharacter.Player?.Character is Necrobinder) depth++;
    }

    public static void Exit()
    {
        if (depth > 0) depth--;
    }

    public static void SuppressNextVanillaDefaultTrackRead()
    {
        suppressedDefaultTrackReads++;
    }

    public static bool TryConsumeSuppressedVanillaDefaultTrackRead(int trackIndex)
    {
        if (trackIndex != 0 || suppressedDefaultTrackReads <= 0) return false;

        suppressedDefaultTrackReads--;
        return true;
    }
}