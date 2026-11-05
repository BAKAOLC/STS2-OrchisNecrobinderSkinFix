using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal static class OrchisPatchRecovery
{
    private const string PatchNamespace = "OrchisNecrobinderSkinMod.Scripts.";
    private static MethodInfo? _characterSelectPostfix;

    public static void ApplyDynamic(ModPatcher patcher)
    {
        if (AccessTools.DeclaredMethod(typeof(NCreature), "ScaleAndPositionOsty",
                [typeof(float), typeof(double)]) == null)
            return;

        var builder = new DynamicPatchBuilder("orchis_patch_recovery");
        AddPostfix(builder, "Entry+CreatureCreateVisualsPatch",
            AccessTools.DeclaredMethod(typeof(Creature), nameof(Creature.CreateVisuals), Type.EmptyTypes));
        AddPostfix(builder, "Entry+CreatureReadyPatch",
            AccessTools.DeclaredMethod(typeof(NCreature), nameof(NCreature._Ready), Type.EmptyTypes));
        AddPostfix(builder, "Entry+CharacterModelCreateVisualsPatch",
            AccessTools.DeclaredMethod(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals),
                Type.EmptyTypes));
        AddPostfix(builder, "Entry+CharacterSelectBgPatch",
            AccessTools.PropertyGetter(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg)));
        AddCharacterSelectPostfix(builder);
        AddPostfix(builder, "Entry+GameOverScreenMoveCreaturesPatch",
            AccessTools.DeclaredMethod(typeof(NGameOverScreen), "MoveCreaturesToDifferentLayerAndDisableUi",
                Type.EmptyTypes));
        AddPostfix(builder, "RelicModelGetIconPatch",
            AccessTools.PropertyGetter(typeof(RelicModel), nameof(RelicModel.Icon)));
        AddPostfix(builder, "RelicModelGetIconOutlinePatch",
            AccessTools.PropertyGetter(typeof(RelicModel), nameof(RelicModel.IconOutline)));
        AddPostfix(builder, "RelicModelUpdateTexturePatch",
            AccessTools.DeclaredMethod(typeof(RelicModel), nameof(RelicModel.UpdateTexture),
                [typeof(TextureRect)]));
        AddPostfix(builder, "LocStringGetFormattedTextPatch",
            AccessTools.DeclaredMethod(typeof(LocString), nameof(LocString.GetFormattedText), Type.EmptyTypes));
        AddPostfix(builder, "LocStringGetRawTextPatch",
            AccessTools.DeclaredMethod(typeof(LocString), nameof(LocString.GetRawText), Type.EmptyTypes));

        if (builder.Patches.Count == 0)
            return;

        if (!patcher.ApplyDynamic(builder))
            throw new InvalidOperationException("Could not restore the missing Orchis visual patches.");

        Main.Logger.Info($"Restored {builder.Patches.Count} missing Orchis visual patch(es).");
    }

    private static void AddPostfix(DynamicPatchBuilder builder, string patchTypeName, MethodBase? target)
    {
        var patchType = AccessTools.TypeByName(PatchNamespace + patchTypeName);
        var postfix = patchType == null ? null : AccessTools.DeclaredMethod(patchType, "Postfix");
        if (target == null || postfix == null)
        {
            Main.Logger.Warn($"Could not resolve Orchis compatibility target '{patchTypeName}'.");
            return;
        }

        if (IsApplied(target, postfix))
            return;

        builder.Add(target, postfix: new HarmonyMethod(postfix),
            patchId: "orchis_restore_" + patchTypeName.Replace('+', '_'));
    }

    private static void AddCharacterSelectPostfix(DynamicPatchBuilder builder)
    {
        var patchType = AccessTools.TypeByName(PatchNamespace + "Entry+CharacterSelectScreenPatch");
        _characterSelectPostfix = patchType == null ? null : AccessTools.DeclaredMethod(patchType, "Postfix");
        var target = AccessTools.DeclaredMethod(typeof(NCharacterSelectScreen), "SelectCharacter",
            [typeof(NCharacterSelectButton)]);
        var bridge = AccessTools.DeclaredMethod(typeof(OrchisPatchRecovery), nameof(CharacterSelectPostfix));
        if (target == null || _characterSelectPostfix == null)
        {
            Main.Logger.Warn("Could not resolve the Orchis character selection compatibility target.");
            return;
        }

        if (IsApplied(target, _characterSelectPostfix) || IsApplied(target, bridge))
            return;

        builder.Add(target, postfix: new HarmonyMethod(bridge), patchId: "orchis_restore_character_select");
    }

    private static bool IsApplied(MethodBase target, MethodInfo patchMethod)
    {
        var info = Harmony.GetPatchInfo(target);
        return info != null && info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers)
            .Any(patch => patch.PatchMethod == patchMethod);
    }

    // ReSharper disable once InconsistentNaming
    private static void CharacterSelectPostfix(NCharacterSelectScreen __instance, NCharacterSelectButton __0)
    {
        _characterSelectPostfix!.Invoke(null, [__instance, __0, __0.Character]);
    }
}

// ReSharper restore ArrangeNamespaceBody