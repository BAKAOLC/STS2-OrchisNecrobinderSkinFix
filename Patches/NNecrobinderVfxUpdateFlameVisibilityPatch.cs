using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2OrchisNecrobinderSkinFix.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class NNecrobinderVfxUpdateFlameVisibilityPatch : IPatchMethod
{
    private static readonly FieldInfo? HeadRefField = AccessTools.Field(typeof(NNecrobinderVfx), "_headRef");

    private static readonly LogLimiter MissingHeadLog =
        new("Skipped NNecrobinderVfx.UpdateFlameVisibility because Orchis removed or freed HeadBoneNode");

    private static readonly LogLimiter SuppressedExceptionLog =
        new("Suppressed NNecrobinderVfx.UpdateFlameVisibility NullReferenceException");

    public static string PatchId => "orchis_necrobinder_vfx_update_flame_guard";
    public static bool IsCritical => false;
    public static string Description => "Skip Necrobinder flame visibility updates after Orchis removes HeadBoneNode";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(
                typeof(NNecrobinderVfx),
                "UpdateFlameVisibility",
                [typeof(GodotObject), typeof(GodotObject), typeof(GodotObject)],
                true)
        ];
    }

    private static bool Prefix(NNecrobinderVfx __instance)
    {
        if (HasValidHeadRef(__instance)) return true;

        MissingHeadLog.Info();
        return false;
    }

    private static Exception? Finalizer(Exception? __exception)
    {
        if (__exception == null) return null;

        if (__exception is NullReferenceException)
        {
            SuppressedExceptionLog.Info();
            return null;
        }

        return __exception;
    }

    private static bool HasValidHeadRef(NNecrobinderVfx instance)
    {
        try
        {
            if (HeadRefField?.GetValue(instance) is not GodotObject headRef) return false;

            return GodotObject.IsInstanceValid(headRef);
        }
        catch
        {
            return false;
        }
    }
}