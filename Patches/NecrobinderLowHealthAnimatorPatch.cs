using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using STS2RitsuLib.Patching.Models;

namespace STS2OrchisNecrobinderSkinFix.Patches;

internal sealed class NecrobinderLowHealthAnimatorPatch : IPatchMethod
{
    public static string PatchId => "orchis_necrobinder_low_health_animator";
    public static string Description => "Fall back to the standard idle when the replacement skeleton has no low-health animation";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CharacterModel), nameof(CharacterModel.GenerateAnimator))];
    }

    public static bool Prefix(CharacterModel __instance, object[] __args, ref CreatureAnimator __result)
    {
        if (__instance is not Necrobinder || __args.Length < 2 || __args[0] is not MegaSprite controller)
            return true;

        try
        {
            if (controller.HasAnimation("low_health_loop"))
                return true;
        }
        catch (Exception ex)
        {
            Main.Logger.Warn($"Could not inspect the Orchis Necrobinder low-health animation: {ex.Message}");
            return true;
        }

        __result = CreateFallbackAnimator(controller);
        return false;
    }

    private static CreatureAnimator CreateFallbackAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle_loop", true);
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Relaxed", new AnimState("relaxed_loop", true));
        animator.AddAnyState("Dead", new AnimState("die"));
        AddAction(animator, "Attack", "attack", idle);
        AddAction(animator, "Hit", "hurt", idle);
        AddAction(animator, "PowerUp", "cast", idle);
        AddAction(animator, "Cast", "cast_mighty", idle);
        AddAction(animator, "summonTrigger", "cast", idle);
        return animator;
    }

    private static void AddAction(CreatureAnimator animator, string trigger, string animation, AnimState idle)
    {
        var action = new AnimState(animation) { NextState = idle };
        animator.AddAnyState(trigger, action);
    }
}
