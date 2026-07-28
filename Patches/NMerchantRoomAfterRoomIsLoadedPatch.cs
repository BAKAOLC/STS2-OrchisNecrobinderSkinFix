using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2OrchisNecrobinderSkinFix.Compat;
using STS2OrchisNecrobinderSkinFix.Settings;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class NMerchantRoomAfterRoomIsLoadedPatch : IPatchMethod
{
    private const string NecrobinderSkeletonPath =
        "res://OrchisNecrobinderSkinMod/animations/merchant/necrobinder/necrobinder_shop_skel_data.tres";

    private static Resource? necrobinderSkeletonData;

    public static string PatchId => "orchis_merchant_room_player_visuals";
    public static bool IsCritical => true;

    public static string Description =>
        "Apply Orchis merchant visuals to every Necrobinder player after all merchant characters are created";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(NMerchantRoom), "AfterRoomIsLoaded", Type.EmptyTypes)
        ];
    }

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(
        NMerchantRoom __instance,
        List<Player> ____players)
    {
        if (!FeatureSettings.Current.NecrobinderMerchantModel) return;

        try
        {
            ApplyNecrobinderVisuals(__instance.PlayerVisuals, ____players);
        }
        catch (Exception ex)
        {
            Main.Logger.Error($"Failed to apply Orchis merchant visuals: {ex}");
        }
    }

    private static void ApplyNecrobinderVisuals(
        IReadOnlyList<NMerchantCharacter> playerVisuals,
        IReadOnlyList<Player> players)
    {
        if (playerVisuals.Count != players.Count)
            Main.Logger.Warn(
                $"Merchant player/visual count mismatch: players={players.Count}, visuals={playerVisuals.Count}.");

        var skeletonData = GetOrLoadSkeleton();
        if (skeletonData == null) return;

        var appliedCount = 0;
        for (var i = 0; i < Math.Min(playerVisuals.Count, players.Count); i++)
        {
            if (players[i].Character is not Necrobinder) continue;

            try
            {
                if (ApplyVisual(playerVisuals[i], skeletonData)) appliedCount++;
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Could not apply Orchis merchant visual for player index {i}: {ex}");
            }
        }

        if (appliedCount > 0)
            Main.Logger.Info($"Applied Orchis merchant visuals to {appliedCount} Necrobinder player(s).");
    }

    private static bool ApplyVisual(NMerchantCharacter merchantCharacter, Resource skeletonData)
    {
        if (merchantCharacter.GetChildCount() <= 0 ||
            merchantCharacter.GetChild(0) is not Node2D spineNode ||
            !string.Equals(spineNode.GetClass(), "SpineSprite", StringComparison.Ordinal))
        {
            Main.Logger.Warn(
                $"Merchant character '{merchantCharacter.Name}' does not have a SpineSprite as its first child.");
            return false;
        }

        spineNode.Set("skeleton_data_res", skeletonData);
        var scale = OrchisVisualSettingsCompat.GetFloat("NecrobinderMerchantScale", 1f);
        spineNode.Scale = new Vector2(scale, scale);
        RemoveChildNodes(spineNode, "HeadBoneNode");
        SpineAnimationCompat.PlayAnimation(spineNode, "relaxed_loop", true, true, 0, true);

        GC.KeepAlive(spineNode);
        GC.KeepAlive(skeletonData);
        return true;
    }

    private static Resource? GetOrLoadSkeleton()
    {
        if (necrobinderSkeletonData != null && GodotObject.IsInstanceValid(necrobinderSkeletonData))
            return necrobinderSkeletonData;

        try
        {
            necrobinderSkeletonData = ResourceLoader.Load<Resource>(NecrobinderSkeletonPath);
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Could not load Orchis merchant skeleton '{NecrobinderSkeletonPath}': {ex.Message}");
            necrobinderSkeletonData = null;
        }

        if (necrobinderSkeletonData == null)
            Main.Logger.Warn($"Could not load Orchis merchant skeleton: {NecrobinderSkeletonPath}");

        return necrobinderSkeletonData;
    }

    private static void RemoveChildNodes(Node parent, string childName)
    {
        foreach (var child in parent.GetChildren())
        {
            if (!string.Equals(child.Name.ToString(), childName, StringComparison.Ordinal)) continue;

            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}