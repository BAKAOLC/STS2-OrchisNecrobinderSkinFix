using System.Globalization;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2OrchisNecrobinderSkinFix.Compat;
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

    private const string MerchantCharacterReadyPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+MerchantCharacterReadyPatch";

    private const string CharacterModelCreateVisualsPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+CharacterModelCreateVisualsPatch";

    private const string RestSiteCharacterCreatePatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+RestSiteCharacterCreatePatch";

    private const string RestSiteCharacterReadyPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+RestSiteCharacterReadyPatch";

    private const string GameOverScreenMoveCreaturesPatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+GameOverScreenMoveCreaturesPatch";

    private const string OstyScaleToSizePatchTypeName =
        "OrchisNecrobinderSkinMod.Scripts.Entry+OstyScaleToSizePatch";

    private const string VisualSettingsInteropTypeName = "OrchisNecrobinderSkinMod.Scripts.VisualSettingsInterop";

    private const string CardPortraitReplacementPatchTypeName =
        "NecrobinderCardPortraits.NecrobinderCardPortraitsCode.CardPortraitReplacementPatch";

    private const string NecrobinderRestSiteSkeletonPath =
        "res://OrchisNecrobinderSkinMod/animations/rest_site/necrobinder/rest_site_necrobinder_skel_data.tres";

    private const string OstyRestSiteSkeletonPath =
        "res://OrchisNecrobinderSkinMod/animations/rest_site/necrobinder/lloyd_relax_skel_data.tres";

    private static readonly Vector2 NecrobinderRestSitePositionOffset = new(0f, 215f);

    private static Resource? necrobinderRestSiteSkeletonData;
    private static Resource? ostyRestSiteSkeletonData;
    private static MethodInfo? orchisApplySkeletonMethod;

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
            AccessTools.TypeByName(MerchantCharacterReadyPatchTypeName),
            "Postfix",
            [typeof(NMerchantCharacter)],
            nameof(MerchantCharacterReadyPrefix),
            patchId: "orchis_merchant_character_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(CharacterModelCreateVisualsPatchTypeName),
            "Postfix",
            [typeof(CharacterModel), typeof(NCreatureVisuals).MakeByRefType()],
            nameof(FakeMerchantCharacterVisualsPrefix),
            patchId: "orchis_fake_merchant_character_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(RestSiteCharacterCreatePatchTypeName),
            "Postfix",
            [typeof(Player), typeof(NRestSiteCharacter).MakeByRefType()],
            nameof(RestSiteCharacterCreatePrefix),
            patchId: "orchis_rest_site_create_feature_gate");

        AddOptional(
            builder,
            AccessTools.TypeByName(RestSiteCharacterReadyPatchTypeName),
            "Postfix",
            [typeof(NRestSiteCharacter)],
            nameof(RestSiteCharacterReadyPrefix),
            patchId: "orchis_rest_site_ready_feature_gate");

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

    private static bool MerchantCharacterReadyPrefix()
    {
        return FeatureSettings.Current.NecrobinderMerchantModel;
    }

    private static bool FakeMerchantCharacterVisualsPrefix()
    {
        return FeatureSettings.Current.NecrobinderFakeMerchantModel;
    }

    private static bool RestSiteCharacterCreatePrefix(Player? player)
    {
        return player?.Character is not Necrobinder;
    }

    private static bool RestSiteCharacterReadyPrefix(NRestSiteCharacter __instance)
    {
        try
        {
            return RestSiteCharacterReadyPrefixImpl(__instance);
        }
        catch (Exception ex)
        {
            Main.Logger.Error($"Failed to apply Orchis rest-site feature gates: {ex}");
            return false;
        }
    }

    private static bool RestSiteCharacterReadyPrefixImpl(NRestSiteCharacter __instance)
    {
        var player = __instance.Player;
        if (player?.Character is not Necrobinder) return true;

        var settings = FeatureSettings.Current;
        if (!settings.NecrobinderRestSiteModel && !settings.OstyRestSiteModel) return false;

        var necro = __instance.GetNodeOrNull<Node2D>("Necro");
        var osty = __instance.GetNodeOrNull<Node2D>("Osty");
        var currentActIndex = GetCurrentActIndex(player);
        var restSiteScale = GetOrchisVisualSettingFloat("NecrobinderRestSiteScale", 1f);

        if (settings.NecrobinderRestSiteModel && necro != null)
        {
            var skeletonWasApplied = ApplyRestSiteSkeleton(necro, GetOrLoadRestSiteSkeleton(
                NecrobinderRestSiteSkeletonPath,
                ref necrobinderRestSiteSkeletonData));

            if (skeletonWasApplied)
            {
                necro.Position += NecrobinderRestSitePositionOffset;
                necro.Scale = GetUniformScale(restSiteScale);
                RemoveChildNodes(necro, "SpineBoneNode");
                SpineAnimationCompat.PlayRestSiteAnimation(necro, currentActIndex);
                SpineAnimationCompat.PlayAnimation(necro, "_tracks/light_off", true, false, 1, false);
            }
        }

        if (settings.OstyRestSiteModel && osty != null)
        {
            var skeletonWasApplied = ApplyRestSiteSkeleton(osty, GetOrLoadRestSiteSkeleton(
                OstyRestSiteSkeletonPath,
                ref ostyRestSiteSkeletonData));

            if (skeletonWasApplied)
            {
                osty.Scale *= 1.35f * restSiteScale;
                RemoveChildNodes(osty, "SpineSlotNode");
                SpineAnimationCompat.PlayRestSiteAnimation(osty, currentActIndex);
                SpineAnimationCompat.PlayAnimation(osty, "_tracks/light_off", true, false, 1, false);
            }
        }

        return false;
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

    private static Vector2 GetUniformScale(float value)
    {
        return new Vector2(value, value);
    }

    private static Resource? GetOrLoadRestSiteSkeleton(string resourcePath, ref Resource? cached)
    {
        try
        {
            if (cached != null && GodotObject.IsInstanceValid(cached)) return cached;
        }
        catch
        {
            cached = null;
        }

        try
        {
            cached = ResourceLoader.Load<Resource>(resourcePath);
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Could not load Orchis rest-site skeleton data '{resourcePath}': {ex.Message}");
            cached = null;
        }

        if (cached == null) Main.Logger.Warn($"Could not load Orchis rest-site skeleton data: {resourcePath}");

        return cached;
    }

    private static bool ApplyRestSiteSkeleton(Node2D? spineNode, Resource? skeletonData)
    {
        if (spineNode == null || skeletonData == null) return false;
        if (!string.Equals(spineNode.GetClass(), "SpineSprite", StringComparison.Ordinal)) return false;

        var applySkeletonMethod = GetOrchisApplySkeletonMethod();
        if (applySkeletonMethod == null)
        {
            Main.Logger.Warn("Could not find Orchis ApplySkeleton method for rest-site replacement.");
            return false;
        }

        try
        {
            return applySkeletonMethod.Invoke(null, [spineNode, skeletonData]) is true;
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Orchis ApplySkeleton failed for rest-site node '{spineNode.Name}': {ex}");
            return false;
        }
    }

    private static MethodInfo? GetOrchisApplySkeletonMethod()
    {
        if (orchisApplySkeletonMethod != null) return orchisApplySkeletonMethod;

        var entryType = AccessTools.TypeByName(EntryTypeName);
        if (entryType == null) return null;

        orchisApplySkeletonMethod = AccessTools.DeclaredMethod(
            entryType,
            "ApplySkeleton",
            [typeof(Node2D), typeof(Resource)]);

        return orchisApplySkeletonMethod;
    }

    private static int GetCurrentActIndex(Player player)
    {
        try
        {
            return player.RunState.CurrentActIndex;
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Could not read rest-site act index; falling back to Overgrowth animation: {ex.Message}");
            return 0;
        }
    }

    private static void RemoveChildNodes(Node? parent, string childName)
    {
        if (parent == null) return;

        foreach (var child in parent.GetChildren())
        {
            if (!string.Equals(child.Name.ToString(), childName, StringComparison.Ordinal)) continue;

            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static float GetOrchisVisualSettingFloat(string propertyName, float fallback)
    {
        try
        {
            var interopType = AccessTools.TypeByName(VisualSettingsInteropTypeName);
            var settings = AccessTools.Property(interopType, "Settings")?.GetValue(null);
            var value = settings == null
                ? null
                : AccessTools.Property(settings.GetType(), propertyName)?.GetValue(settings);
            return value == null ? fallback : Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return fallback;
        }
    }
}