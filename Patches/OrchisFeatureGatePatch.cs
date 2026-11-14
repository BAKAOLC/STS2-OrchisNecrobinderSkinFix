using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using STS2OrchisNecrobinderSkinFix.Settings;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

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

    private const string CardPortraitReplacementPatchTypeName =
        "NecrobinderCardPortraits.NecrobinderCardPortraitsCode.CardPortraitReplacementPatch";

    private static ModPatcher? _deferredPatcher;
    private static bool _cardPortraitGateApplied;
    private static IReadOnlyDictionary<Type, string>? _cardPortraitReplacements;

    public static void ApplyDynamic(ModPatcher patcher)
    {
        var builder = new DynamicPatchBuilder("orchis_feature_gate");
        var entryType = AccessTools.TypeByName(EntryTypeName);

        _deferredPatcher = patcher;
        ApplyDeferredCardPortraitGate();

        AddOptional(
            builder,
            AccessTools.TypeByName(TextReplacePatchTypeName),
            "ShouldReplace",
            [typeof(LocString)],
            postfix: nameof(TextShouldReplacePostfix),
            patchId: "orchis_text_replace_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(TextReplacePatchTypeName),
            "TryGetCharacterText",
            [typeof(LocString), typeof(string).MakeByRefType()],
            nameof(CharacterTextPrefix),
            patchId: "orchis_character_text_feature_gate");

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
            [typeof(NCharacterSelectScreen), typeof(object[])],
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

    internal static void ApplyDeferredCardPortraitGate()
    {
        if (_cardPortraitGateApplied || _deferredPatcher == null)
            return;

        var targetType = AccessTools.TypeByName(CardPortraitReplacementPatchTypeName);
        if (targetType == null)
            return;

        var builder = new DynamicPatchBuilder("orchis_card_portrait_gate");
        AddOptional(builder, targetType, "Postfix", [typeof(CardModel), typeof(string).MakeByRefType()],
            nameof(CardPortraitReplacementPrefix), patchId: "necrobinder_card_portrait_feature_gate");
        var bigPortraitGetter = AccessTools.DeclaredPropertyGetter(typeof(CardModel), "BigPortrait");
        if (bigPortraitGetter != null)
        {
            _cardPortraitReplacements = AccessTools.DeclaredField(targetType, "Replacements")?.GetValue(null)
                as IReadOnlyDictionary<Type, string>;
            var prefix = DynamicPatchBuilder.FromMethod(typeof(OrchisFeatureGatePatch),
                nameof(BigPortraitPrefix));
            prefix.priority = Priority.Last;
            builder.Add(bigPortraitGetter, prefix,
                patchId: "necrobinder_card_big_portrait_compat");
        }

        if (builder.Patches.Count != 0)
            _cardPortraitGateApplied = _deferredPatcher.ApplyDynamic(builder);
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

    // ReSharper disable InconsistentNaming
    private static bool BigPortraitPrefix(CardModel __instance, bool __runOriginal, ref Texture2D __result)
    {
        if (!__runOriginal || !FeatureSettings.Current.CardPortraits || _cardPortraitReplacements == null ||
            !_cardPortraitReplacements.TryGetValue(__instance.GetType(), out var path) ||
            !string.Equals(__instance.PortraitPath, path, StringComparison.Ordinal) || !ResourceLoader.Exists(path))
            return true;

        var texture = ResourceLoader.Load<Texture2D>(path);
        if (texture == null)
            return true;

        __result = texture;
        return false;
    }
    // ReSharper restore InconsistentNaming

    // ReSharper disable once InconsistentNaming
    private static void TextShouldReplacePostfix(
        LocString locString,
        ref bool __result)
    {
        if (__result && !FeatureSettings.IsTextLocStringEnabled(locString)) __result = false;
    }

    // ReSharper disable once InconsistentNaming
    private static bool CharacterTextPrefix(out string __1, ref bool __result)
    {
        __1 = string.Empty;
        if (FeatureSettings.Current.CharacterText) return true;

        __result = false;
        return false;
    }

    // ReSharper disable once InconsistentNaming
    private static bool RelicIconReplacementPrefix(RelicModel __0, out Texture2D icon, ref bool __result)
    {
        icon = null!;
        // ReSharper disable once MergeIntoPattern
        if (FeatureSettings.Current.RelicIcons &&
            (__0 is not YummyCookie || __0.IsMutable && __0.Owner is { Character: Necrobinder }))
            return true;

        __result = false;
        return false;
    }

    // ReSharper disable once InconsistentNaming
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

    private static bool ApplyNecrobinderCombatScalePrefix()
    {
        return FeatureSettings.Current.NecrobinderCombatModel;
    }

    private static bool ScheduleCombatOstyOrderingPrefix()
    {
        return FeatureSettings.Current.OstyCombatModel;
    }
}

internal sealed class OrchisDeferredFeatureGatePatch : IPatchMethod
{
    public static string PatchId => "orchis_deferred_card_portrait_gate";
    public static string Description => "Install the optional card portrait gate after its mod initializes";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(ModManager), "CallModInitializer", [typeof(Type)])];
    }

    public static void Postfix(Type initializerType)
    {
        if (initializerType.Namespace?.StartsWith("NecrobinderCardPortraits.", StringComparison.Ordinal) == true)
            OrchisFeatureGatePatch.ApplyDeferredCardPortraitGate();
    }
}

// ReSharper restore ArrangeNamespaceBody
