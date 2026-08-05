using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2OrchisNecrobinderSkinFix.Diagnostics;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class NNecrobinderVfxReadyPatch : IPatchMethod
{
    private static readonly string[] OriginalScytheParticlePaths =
    [
        "ScytheVfxSlot1/ScytheParticles",
        "ScytheVfxSlot2/ScytheParticles"
    ];

    private static readonly LogLimiter MissingHeadLog =
        new("Disabled orphaned vanilla Necrobinder VFX because Orchis removed HeadBoneNode before initialization");

    private static readonly LogLimiter SuppressedExceptionLog =
        new("Suppressed NNecrobinderVfx._Ready exception");

    public static string PatchId => "orchis_necrobinder_vfx_ready_head_guard";
    public static bool IsCritical => false;
    public static string Description =>
        "Disable orphaned vanilla Necrobinder particles after Orchis removes HeadBoneNode";

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

        DisableOriginalScytheParticles(parent);
        MissingHeadLog.Info();
        return false;
    }

    private static void DisableOriginalScytheParticles(Node2D? parent)
    {
        if (parent == null) return;

        foreach (var path in OriginalScytheParticlePaths)
        {
            var particles = parent.GetNodeOrNull<GpuParticles2D>(path);
            if (particles == null) continue;

            particles.Emitting = false;
            particles.OneShot = true;
            particles.Visible = false;
        }
    }

    private static Exception? Finalizer(
        NNecrobinderVfx __instance,
        Exception? __exception)
    {
        if (__exception == null) return null;

        if (__exception is InvalidOperationException or NullReferenceException &&
            MissingHead(__instance))
        {
            SuppressedExceptionLog.Info(__exception.Message);
            return null;
        }

        return __exception;
    }

    private static bool MissingHead(NNecrobinderVfx instance)
    {
        try
        {
            return instance.GetParent<Node2D>()?.GetNodeOrNull<Node2D>("HeadBoneNode") == null;
        }
        catch
        {
            return true;
        }
    }
}
