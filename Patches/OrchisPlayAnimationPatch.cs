using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2OrchisNecrobinderSkinFix.Compat;
using STS2OrchisNecrobinderSkinFix.Diagnostics;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal static class OrchisPlayAnimationPatch
{
    private const string OrchisEntryTypeName = "OrchisNecrobinderSkinMod.Scripts.Entry";

    private const string MerchantCharacterReadyPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+MerchantCharacterReadyPatch";

    private const string RestSiteCharacterReadyPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+RestSiteCharacterReadyPatch";

    private static readonly LogLimiter SuppressedFailureLog =
        new("Suppressed Orchis animation compatibility patch failure");

    public static void ApplyDynamic(ModPatcher patcher)
    {
        var builder = new DynamicPatchBuilder("orchis_animation_compat");

        var entryType = AccessTools.TypeByName(OrchisEntryTypeName);
        if (entryType != null) AddPlayLoopingAnimationPatch(builder, entryType);

        AddMerchantCharacterReadyPatch(builder);
        AddRestSiteCharacterReadyPatch(builder);

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

    private static void AddMerchantCharacterReadyPatch(DynamicPatchBuilder builder)
    {
        var targetType = AccessTools.TypeByName(MerchantCharacterReadyPatchTypeName);
        var target = targetType == null
            ? null
            : AccessTools.DeclaredMethod(targetType, "Postfix", [typeof(NMerchantCharacter)]);
        if (target == null)
        {
            Main.Logger.Warn($"Optional patch target '{MerchantCharacterReadyPatchTypeName}.Postfix' was not found.");
            return;
        }

        builder.Add(
            target,
            transpiler: DynamicPatchBuilder.FromMethod(typeof(OrchisPlayAnimationPatch),
                nameof(AnimationCallTranspiler)),
            isCritical: false,
            description: "Replace Orchis merchant animation calls with 0.107/0.108 compatible calls",
            patchId: "orchis_merchant_ready_animation_compat");
    }

    private static void AddRestSiteCharacterReadyPatch(DynamicPatchBuilder builder)
    {
        var targetType = AccessTools.TypeByName(RestSiteCharacterReadyPatchTypeName);
        var target = targetType == null
            ? null
            : AccessTools.DeclaredMethod(targetType, "Postfix", [typeof(NRestSiteCharacter)]);
        if (target == null)
        {
            Main.Logger.Warn($"Optional patch target '{RestSiteCharacterReadyPatchTypeName}.Postfix' was not found.");
            return;
        }

        builder.Add(
            target,
            transpiler: DynamicPatchBuilder.FromMethod(typeof(OrchisPlayAnimationPatch),
                nameof(AnimationCallTranspiler)),
            isCritical: false,
            description: "Replace Orchis rest-site animation calls with 0.107/0.108 compatible calls",
            patchId: "orchis_rest_site_ready_animation_compat");
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

    private static IEnumerable<CodeInstruction> AnimationCallTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var entryType = AccessTools.TypeByName(OrchisEntryTypeName);
        var orchisPlayAnimation = entryType == null
            ? null
            : AccessTools.DeclaredMethod(
                entryType,
                "PlayAnimation",
                [typeof(Node2D), typeof(string), typeof(bool), typeof(bool), typeof(int), typeof(bool)]);
        var orchisPlayRestSiteAnimation = entryType == null
            ? null
            : AccessTools.DeclaredMethod(entryType, "PlayRestSiteAnimation", [typeof(Node2D), typeof(int)]);
        var compatiblePlayAnimation =
            AccessTools.DeclaredMethod(typeof(OrchisPlayAnimationPatch), nameof(PlayAnimationCompat));
        var compatiblePlayRestSiteAnimation =
            AccessTools.DeclaredMethod(typeof(OrchisPlayAnimationPatch), nameof(PlayRestSiteAnimationCompat));

        foreach (var instruction in instructions)
        {
            if (orchisPlayAnimation != null && instruction.Calls(orchisPlayAnimation))
            {
                yield return CopyMetadata(instruction, new CodeInstruction(OpCodes.Call, compatiblePlayAnimation));
                continue;
            }

            if (orchisPlayRestSiteAnimation != null && instruction.Calls(orchisPlayRestSiteAnimation))
            {
                yield return CopyMetadata(instruction,
                    new CodeInstruction(OpCodes.Call, compatiblePlayRestSiteAnimation));
                continue;
            }

            yield return instruction;
        }
    }

    private static CodeInstruction CopyMetadata(CodeInstruction source, CodeInstruction replacement)
    {
        replacement.labels.AddRange(source.labels);
        replacement.blocks.AddRange(source.blocks);
        return replacement;
    }

    private static void PlayAnimationCompat(
        Node2D? spineNode,
        string animationName,
        bool loop,
        bool randomizeTrackTime,
        int trackId,
        bool logIfMissing)
    {
        try
        {
            SpineAnimationCompat.PlayAnimation(spineNode, animationName, loop, randomizeTrackTime, trackId,
                logIfMissing);
        }
        catch (Exception ex)
        {
            SuppressedFailureLog.Info(ex.Message);
        }
    }

    private static void PlayRestSiteAnimationCompat(Node2D? spineNode, int actIndex)
    {
        try
        {
            SpineAnimationCompat.PlayRestSiteAnimation(spineNode, actIndex);
        }
        catch (Exception ex)
        {
            SuppressedFailureLog.Info(ex.Message);
        }
    }
}