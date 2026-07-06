using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace STS2OrchisNecrobinderSkinFix.Compat;

internal static class SpineAnimationCompat
{
    private static readonly string[] OvergrowthAnimationCandidates = ["overgrowth_loop", "overgrowth"];
    private static readonly string[] HiveAnimationCandidates = ["hive_loop", "hive"];
    private static readonly string[] GloryAnimationCandidates = ["glory_loop", "glory"];

    public static void PlayAnimation(
        Node2D? spineNode,
        string animationName,
        bool loop,
        bool randomizeTrackTime,
        int trackId,
        bool logIfMissing)
    {
        if (spineNode == null || !string.Equals(spineNode.GetClass(), "SpineSprite", StringComparison.Ordinal)) return;

        var sprite = new MegaSprite(spineNode);
        if (!HasAnimation(spineNode, sprite, animationName))
        {
            if (logIfMissing) Main.Logger.Warn($"Animation '{animationName}' was not found on '{spineNode.Name}'.");

            return;
        }

        spineNode.RunWhenSpineReady(sprite, animationState =>
        {
            using var nativeTrackEntry = animationState.BoundObject.Call("set_animation", animationName, loop, trackId);
            if (!randomizeTrackTime) return;

            using var returnedTrackEntry = TryCreateTrackEntry(nativeTrackEntry);
            if (returnedTrackEntry != null)
            {
                RandomizeTrackTime(returnedTrackEntry);
                return;
            }

            using var currentTrackEntry = animationState.GetCurrent(trackId);
            if (currentTrackEntry != null) RandomizeTrackTime(currentTrackEntry);
        });
    }

    public static void PlayLoopingAnimation(Node2D? spineNode, params string[] animationCandidates)
    {
        if (spineNode == null || !string.Equals(spineNode.GetClass(), "SpineSprite", StringComparison.Ordinal)) return;

        var sprite = new MegaSprite(spineNode);
        var animationName = FindAnimationName(spineNode, sprite, animationCandidates);
        if (animationName == null)
        {
            Main.Logger.Warn($"No playable animation was found on '{spineNode.Name}'.");
            return;
        }

        PlayAnimation(spineNode, animationName, true, false, 0, true);
    }

    public static void PlayRestSiteAnimation(Node2D? spineNode, int actIndex)
    {
        if (spineNode == null || !string.Equals(spineNode.GetClass(), "SpineSprite", StringComparison.Ordinal)) return;

        var sprite = new MegaSprite(spineNode);
        var animationName = FindRestSiteAnimationName(spineNode, sprite, actIndex);
        if (animationName == null)
        {
            Main.Logger.Warn($"No rest site animation was found on '{spineNode.Name}' for act index {actIndex}.");
            return;
        }

        PlayAnimation(spineNode, animationName, true, true, 0, true);
    }

    private static MegaTrackEntry? TryCreateTrackEntry(Variant nativeTrackEntry)
    {
        return nativeTrackEntry.VariantType == Variant.Type.Object && nativeTrackEntry.AsGodotObject() != null
            ? new MegaTrackEntry(nativeTrackEntry)
            : null;
    }

    private static void RandomizeTrackTime(MegaTrackEntry trackEntry)
    {
        trackEntry.SetTrackTime(trackEntry.GetAnimationEnd() * Random.Shared.NextSingle());
    }

    private static bool HasAnimation(Node2D spineNode, MegaSprite sprite, string animationName)
    {
        try
        {
            return sprite.HasAnimation(animationName);
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Failed to inspect animation '{animationName}' on '{spineNode.Name}': {ex.Message}");
            return false;
        }
    }

    private static string? FindRestSiteAnimationName(Node2D spineNode, MegaSprite sprite, int actIndex)
    {
        var animationName = FindAnimationName(spineNode, sprite, GetRestSiteAnimationCandidates(actIndex));
        if (animationName != null) return animationName;

        var actNeedle = actIndex switch
        {
            0 => "overgrowth",
            1 => "hive",
            2 => "glory",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(actNeedle))
            foreach (var candidate in GetAnimationNames(spineNode, sprite))
                if (candidate.Contains(actNeedle, StringComparison.OrdinalIgnoreCase))
                    return candidate;

        foreach (var candidate in GetAnimationNames(spineNode, sprite))
            if (!candidate.StartsWith("_tracks/", StringComparison.OrdinalIgnoreCase) &&
                !candidate.Contains("light", StringComparison.OrdinalIgnoreCase))
                return candidate;

        return FindAnimationName(spineNode, sprite);
    }

    private static string? FindAnimationName(Node2D spineNode, MegaSprite sprite, params string[] animationCandidates)
    {
        foreach (var candidate in animationCandidates)
            if (!string.IsNullOrWhiteSpace(candidate) && HasAnimation(spineNode, sprite, candidate))
                return candidate;

        foreach (var animationName in GetAnimationNames(spineNode, sprite))
            if (!string.IsNullOrWhiteSpace(animationName))
                return animationName;

        return null;
    }

    private static IReadOnlyList<string> GetAnimationNames(Node2D spineNode, MegaSprite sprite)
    {
        try
        {
            return sprite.GetSkeleton()?.GetData().GetAnimationNames() ?? [];
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Failed to read animation list from '{spineNode.Name}': {ex.Message}");
            return [];
        }
    }

    private static string[] GetRestSiteAnimationCandidates(int actIndex)
    {
        return actIndex switch
        {
            0 => OvergrowthAnimationCandidates,
            1 => HiveAnimationCandidates,
            2 => GloryAnimationCandidates,
            _ => []
        };
    }
}