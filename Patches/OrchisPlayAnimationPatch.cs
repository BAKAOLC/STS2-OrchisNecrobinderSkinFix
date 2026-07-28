using Godot;
using HarmonyLib;
using STS2OrchisNecrobinderSkinFix.Compat;
using STS2OrchisNecrobinderSkinFix.Diagnostics;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal static class OrchisPlayAnimationPatch
{
    private const string OrchisEntryTypeName = "OrchisNecrobinderSkinMod.Scripts.Entry";

    private static readonly LogLimiter SuppressedFailureLog =
        new("Suppressed Orchis animation compatibility patch failure");

    public static void ApplyDynamic(ModPatcher patcher)
    {
        var builder = new DynamicPatchBuilder("orchis_animation_compat");

        var entryType = AccessTools.TypeByName(OrchisEntryTypeName);
        if (entryType != null) AddPlayLoopingAnimationPatch(builder, entryType);

        if (builder.Patches.Count == 0)
        {
            Main.Logger.Warn("No Orchis animation compatibility patch targets were found.");
            return;
        }

        patcher.ApplyDynamic(builder);
    }

    private static void AddPlayLoopingAnimationPatch(DynamicPatchBuilder builder, Type entryType)
    {
        var target = AccessTools.DeclaredMethod(
            entryType,
            "PlayLoopingAnimation",
            [typeof(Node2D), typeof(string[])]);
        if (target == null)
        {
            Main.Logger.Warn($"Optional patch target '{OrchisEntryTypeName}.PlayLoopingAnimation' was not found.");
            return;
        }

        builder.Add(
            target,
            DynamicPatchBuilder.FromMethod(typeof(OrchisPlayAnimationPatch), nameof(PlayLoopingAnimationPrefix)),
            isCritical: false,
            description: "Redirect Orchis looping animation playback through a 0.107/0.108 compatible Spine API path",
            patchId: "orchis_play_looping_animation_compat");
    }

    private static bool PlayLoopingAnimationPrefix(Node2D? spineNode, string[] animationCandidates)
    {
        try
        {
            SpineAnimationCompat.PlayLoopingAnimation(spineNode, animationCandidates);
        }
        catch (Exception ex)
        {
            SuppressedFailureLog.Info(ex.Message);
        }

        return false;
    }
}