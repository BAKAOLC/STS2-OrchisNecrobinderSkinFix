using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using STS2OrchisNecrobinderSkinFix.Settings;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal static class OrchisFeatureGatePatch
{
    private const string TextReplacePatchTypeName = "OrchisNecrobinderSkinMod.Scripts.TextReplacePatch";
    private const string RelicIconPatchTypeName = "OrchisNecrobinderSkinMod.Scripts.RelicIconPatch";
    private const string EntryTypeName = "OrchisNecrobinderSkinMod.Scripts.Entry";

    private const string CharacterSelectBgPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+CharacterSelectBgPatch";

    private const string CharacterSelectScreenPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+CharacterSelectScreenPatch";

    private const string CharacterModelCreateVisualsPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+CharacterModelCreateVisualsPatch";

    private const string GameOverScreenMoveCreaturesPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+GameOverScreenMoveCreaturesPatch";

    private const string OstyScaleToSizePatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+OstyScaleToSizePatch";

    private const string CardPortraitReplacementPatchTypeName =
        "NecrobinderCardPortraits.NecrobinderCardPortraitsCode.CardPortraitReplacementPatch";

    public static void ApplyDynamic(ModPatcher patcher)
    {
        var builder = new DynamicPatchBuilder("orchis_feature_gate");
        var entryType = AccessTools.TypeByName(EntryTypeName);

        AddOptional(
            builder,
            AccessTools.TypeByName(CardPortraitReplacementPatchTypeName),
            "Postfix",
            [typeof(CardModel), typeof(string).MakeByRefType()],
            nameof(CardPortraitReplacementPrefix),
            patchId: "necrobinder_card_portrait_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(TextReplacePatchTypeName),
            "ShouldReplace",
            [typeof(LocString)],
            postfix: nameof(TextShouldReplacePostfix),
            patchId: "orchis_text_replace_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(RelicIconPatchTypeName),
            "TryGetReplacementIcon",
            [typeof(RelicModel), typeof(Texture2D).MakeByRefType()],
            nameof(RelicIconReplacementPrefix),
            patchId: "orchis_relic_icon_feature_gate");

        AddOptional(
            builder,
            entryType,
            "ApplySkeleton",
            [typeof(Node2D), typeof(Resource)],
            nameof(ApplySkeletonPrefix),
            patchId: "orchis_apply_skeleton_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(CharacterSelectBgPatchTypeName),
            "Postfix",
            [typeof(CharacterModel), typeof(string).MakeByRefType()],
            nameof(CharacterSelectScenePrefix),
            patchId: "orchis_character_select_scene_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(CharacterSelectScreenPatchTypeName),
            "Postfix",
            [typeof(NCharacterSelectScreen), typeof(NCharacterSelectButton), typeof(CharacterModel)],
            nameof(CharacterSelectCompanionPrefix),
            patchId: "orchis_character_select_companion_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(CharacterModelCreateVisualsPatchTypeName),
            "Postfix",
            [typeof(CharacterModel), typeof(NCreatureVisuals).MakeByRefType()],
            nameof(FakeMerchantCharacterVisualsPrefix),
            patchId: "orchis_fake_merchant_character_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(GameOverScreenMoveCreaturesPatchTypeName),
            "Postfix",
            [typeof(NGameOverScreen)],
            nameof(GameOverRestSiteVisualsPrefix),
            patchId: "orchis_game_over_rest_site_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(OstyScaleToSizePatchTypeName),
            "Prefix",
            [typeof(NCreature), typeof(float), typeof(double)],
            nameof(OstyScaleToSizePrefixPatch),
            patchId: "orchis_osty_scale_feature_gate");

        AddOptional(
            builder,
            entryType,
            "ApplyNecrobinderCombatScale",
            [typeof(NCreature)],
            nameof(ApplyNecrobinderCombatScalePrefix),
            patchId: "orchis_necrobinder_combat_scale_feature_gate");

        AddOptional(
            builder,
            entryType,
            "ScheduleCombatOstyOrdering",
            [typeof(NCreature)],
            nameof(ScheduleCombatOstyOrderingPrefix),
            patchId: "orchis_osty_ordering_feature_gate");

        if (builder.Patches.Count == 0)
        {
            Main.Logger.Warn("No Orchis feature gate patch targets were found.");
            return;
        }

        patcher.ApplyDynamic(builder);
    }

    private static void AddOptional(
        DynamicPatchBuilder builder,
        Type? targetType,
        string methodName,
        Type[] parameterTypes,
        string? prefix = null,
        string? postfix = null,
        string? patchId = null)
    {
        var target = targetType == null ? null : AccessTools.DeclaredMethod(targetType, methodName, parameterTypes);
        if (target == null)
        {
            Main.Logger.Warn(
                $"Optional Orchis feature gate target '{targetType?.FullName ?? "<missing type>"}.{methodName}' was not found.");
            return;
        }

        builder.Add(
            target,
            prefix == null ? null : DynamicPatchBuilder.FromMethod(typeof(OrchisFeatureGatePatch), prefix),
            postfix == null ? null : DynamicPatchBuilder.FromMethod(typeof(OrchisFeatureGatePatch), postfix),
            isCritical: false,
            description: $"Gate Orchis feature '{patchId ?? methodName}' behind RitsuLib settings",
            patchId: patchId);
    }

    private static bool CardPortraitReplacementPrefix()
    {
        return FeatureSettings.Current.CardPortraits;
    }

    private static void TextShouldReplacePostfix(
        LocString locString,
        ref bool __result)
    {
        if (__result && !FeatureSettings.IsTextLocStringEnabled(locString)) __result = false;
    }

    private static bool RelicIconReplacementPrefix(out Texture2D icon, ref bool __result)
    {
        icon = null!;
        if (FeatureSettings.Current.RelicIcons) return true;

        __result = false;
        return false;
    }

    private static bool ApplySkeletonPrefix(Resource? skeletonData, ref bool __result)
    {
        if (FeatureSettings.IsSkeletonPathEnabled(skeletonData?.ResourcePath)) return true;

        __result = false;
        return false;
    }

    private static bool CharacterSelectScenePrefix()
    {
        return FeatureSettings.Current.CharacterSelectScene;
    }

    private static bool CharacterSelectCompanionPrefix()
    {
        return FeatureSettings.Current.CharacterSelectCompanionModels;
    }

    private static bool FakeMerchantCharacterVisualsPrefix()
    {
        return FeatureSettings.Current.NecrobinderFakeMerchantModel;
    }

    private static bool GameOverRestSiteVisualsPrefix()
    {
        return FeatureSettings.Current.GameOverRestSiteVisuals;
    }

    private static bool OstyScaleToSizePrefixPatch(ref bool __result)
    {
        if (FeatureSettings.Current.OstyCombatModel) return true;

        __result = true;
        return false;
    }

    private static bool ApplyNecrobinderCombatScalePrefix()
    {
        return FeatureSettings.Current.NecrobinderCombatModel;
    }

    private static bool ScheduleCombatOstyOrderingPrefix()
    {
        return FeatureSettings.Current.OstyCombatModel;
    }
}