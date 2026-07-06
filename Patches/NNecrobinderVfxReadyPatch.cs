using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2OrchisNecrobinderSkinFix.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class NNecrobinderVfxReadyPatch : IPatchMethod
{
    private static readonly LogLimiter MissingHeadLog =
        new("Skipped NNecrobinderVfx._Ready because Orchis removed HeadBoneNode before the VFX node initialized");

    private static readonly LogLimiter SuppressedExceptionLog =
        new("Suppressed NNecrobinderVfx._Ready exception");

    public static string PatchId => "orchis_necrobinder_vfx_ready_head_guard";
    public static bool IsCritical => false;
    public static string Description => "Skip Necrobinder flame VFX setup after Orchis removes HeadBoneNode";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(NNecrobinderVfx), nameof(NNecrobinderVfx._Ready), Type.EmptyTypes, true)
        ];
    }

    private static bool Prefix(NNecrobinderVfx __instance)
    {
        var parent = __instance.GetParent<Node2D>();
        if (parent?.GetNodeOrNull<Node2D>("HeadBoneNode") != null) return true;

        MissingHeadLog.Info();
        return false;
    }

    private static Exception? Finalizer(Exception? __exception)
    {
        if (__exception == null) return null;

        if (__exception is InvalidOperationException or NullReferenceException)
        {
            SuppressedExceptionLog.Info(__exception.Message);
            return null;
        }

        return __exception;
    }
}