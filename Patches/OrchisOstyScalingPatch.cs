using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2OrchisNecrobinderSkinFix.Settings;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal static class OrchisOstyScalingPatch
{
    private const string OstySkeletonPath =
        "res://OrchisNecrobinderSkinMod/animations/monsters/osty/osty_combat_skel_data.tres";

    private const string InteropTypeName = "OrchisNecrobinderSkinMod.Scripts.VisualSettingsInterop";

    public static void ApplyDynamic(ModPatcher patcher)
    {
        var target = AccessTools.DeclaredMethod(typeof(NCreature), "ScaleAndPositionOsty",
            [typeof(float), typeof(double)]);
        if (target == null)
            return;

        var builder = new DynamicPatchBuilder("orchis_osty_scale_compat");
        builder.Add(target,
            transpiler: DynamicPatchBuilder.FromMethod(typeof(OrchisOstyScalingPatch), nameof(Transpiler)),
            patchId: "orchis_osty_native_scale_factor");
        if (!patcher.ApplyDynamic(builder))
            throw new InvalidOperationException("Could not apply the Orchis Osty scaling compatibility patch.");
    }

    private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var lerp = AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.Lerp),
            [typeof(float), typeof(float), typeof(float)]);
        var indices = codes.Select((code, index) => (code, index)).Where(item => item.code.Calls(lerp))
            .Select(item => item.index).ToArray();
        if (indices.Length != 1)
            throw new InvalidOperationException("Expected one native Osty health scaling factor.");

        codes.InsertRange(indices[0] + 1,
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call,
                AccessTools.DeclaredMethod(typeof(OrchisOstyScalingPatch), nameof(ResolveScaleFactor)))
        ]);
        return codes;
    }

    private static float ResolveScaleFactor(float nativeFactor, NCreature creature, float ostyHealth)
    {
        if (!FeatureSettings.Current.OstyCombatModel || creature.Entity.Monster is not Osty ||
            creature.Entity.PetOwner == null)
            return nativeFactor;

        var body = creature.Visuals.GetNodeOrNull<Node2D>("Visuals");
        if (body == null || body.GetClass() != "SpineSprite" ||
            body.Get("skeleton_data_res").AsGodotObject() is not Resource { ResourcePath: OstySkeletonPath })
            return nativeFactor;

        var interopType = AccessTools.TypeByName(InteropTypeName);
        var settings = interopType == null ? null : AccessTools.Property(interopType, "Settings")?.GetValue(null);
        var scaleMethod = settings == null
            ? null
            : AccessTools.DeclaredMethod(settings.GetType(),
                "GetOstyScaleForHealth", [typeof(float)]);
        if (scaleMethod?.Invoke(settings, [ostyHealth]) is not float scale)
            return nativeFactor;

        creature.Visuals.DefaultScale = scale;
        return 1f;
    }
}

// ReSharper restore ArrangeNamespaceBody