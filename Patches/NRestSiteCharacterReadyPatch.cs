using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2OrchisNecrobinderSkinFix.Compat;
using STS2OrchisNecrobinderSkinFix.Settings;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class NRestSiteCharacterReadyPatch : IPatchMethod
{
    private const string NecrobinderSkeletonPath =
        "res://OrchisNecrobinderSkinMod/animations/rest_site/necrobinder/rest_site_necrobinder_skel_data.tres";

    private const string OstySkeletonPath =
        "res://OrchisNecrobinderSkinMod/animations/rest_site/necrobinder/lloyd_relax_skel_data.tres";

    private static readonly Vector2 NecrobinderPositionOffset = new(0f, 215f);

    private static Resource? necrobinderSkeletonData;
    private static Resource? ostySkeletonData;

    public static string PatchId => "orchis_rest_site_ready_replacement";
    public static bool IsCritical => true;

    public static string Description =>
        "Apply Orchis rest-site visuals after vanilla initialization and use version-compatible Spine animation calls";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready), Type.EmptyTypes)
        ];
    }

    private static void Postfix(NRestSiteCharacter __instance)
    {
        try
        {
            if (__instance.Player?.Character is not Necrobinder) return;

            var settings = FeatureSettings.Current;
            var necro = __instance.GetNodeOrNull<Node2D>("Necro");
            var osty = __instance.GetNodeOrNull<Node2D>("Osty");
            var restSiteScale = OrchisVisualSettingsCompat.GetFloat("NecrobinderRestSiteScale", 1f);

            var necroWasReplaced = settings.NecrobinderRestSiteModel &&
                                   ApplySkeleton(necro, GetOrLoadSkeleton(
                                       NecrobinderSkeletonPath,
                                       ref necrobinderSkeletonData));
            if (necroWasReplaced)
            {
                necro!.Position += NecrobinderPositionOffset;
                necro.Scale = new Vector2(restSiteScale, restSiteScale);
                RemoveChildNodes(necro, "SpineBoneNode");
            }

            var ostyWasReplaced = settings.OstyRestSiteModel &&
                                  ApplySkeleton(osty, GetOrLoadSkeleton(OstySkeletonPath, ref ostySkeletonData));
            if (ostyWasReplaced)
            {
                osty!.Scale *= 1.35f * restSiteScale;
                RemoveChildNodes(osty, "SpineSlotNode");
            }

            if (!necroWasReplaced && !ostyWasReplaced) return;

            TaskHelper.RunSafely(ConfigureAnimationsAfterSkeletonSwap(
                __instance,
                necroWasReplaced ? necro : null,
                ostyWasReplaced ? osty : null,
                __instance.Player.RunState.CurrentActIndex));
        }
        catch (Exception ex)
        {
            Main.Logger.Error($"Failed to apply Orchis rest-site visuals: {ex}");
        }
    }

    private static async Task ConfigureAnimationsAfterSkeletonSwap(
        NRestSiteCharacter host,
        Node2D? necro,
        Node2D? osty,
        int actIndex)
    {
        await host.AwaitProcessFrame();
        await host.AwaitProcessFrame();

        if (!GodotObject.IsInstanceValid(host) || !host.IsInsideTree()) return;

        ConfigureNode(necro, actIndex);
        ConfigureNode(osty, actIndex);
    }

    private static void ConfigureNode(Node2D? node, int actIndex)
    {
        if (node == null || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) return;

        SpineAnimationCompat.PlayRestSiteAnimation(node, actIndex);
        SpineAnimationCompat.PlayAnimation(node, "_tracks/light_off", true, false, 1, false);
    }

    private static bool ApplySkeleton(Node2D? spineNode, Resource? skeletonData)
    {
        if (spineNode == null || skeletonData == null) return false;
        if (!string.Equals(spineNode.GetClass(), "SpineSprite", StringComparison.Ordinal)) return false;

        try
        {
            spineNode.Set("skeleton_data_res", skeletonData);
            GC.KeepAlive(spineNode);
            GC.KeepAlive(skeletonData);
            return true;
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Could not replace rest-site skeleton on '{spineNode.Name}': {ex}");
            return false;
        }
    }

    private static Resource? GetOrLoadSkeleton(string path, ref Resource? cached)
    {
        if (cached != null && GodotObject.IsInstanceValid(cached)) return cached;

        try
        {
            cached = ResourceLoader.Load<Resource>(path);
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Could not load Orchis rest-site skeleton '{path}': {ex.Message}");
            cached = null;
        }

        return cached;
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