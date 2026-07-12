using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2OrchisNecrobinderSkinFix.Patches;
using STS2OrchisNecrobinderSkinFix.Settings;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Core;

namespace STS2OrchisNecrobinderSkinFix;

[ModInitializer(nameof(Initialize))]
public static class Main
{
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(Const.ModId);

    public static bool IsModActive { get; private set; }

    public static void Initialize()
    {
        Logger.Info($"Mod ID: {Const.ModId}");
        Logger.Info($"Version: {Const.Version}");
        Logger.Info("Initializing mod...");

        try
        {
            FeatureSettingsRegistration.RegisterData();
            FeatureSettingsRegistration.RegisterSettings();

            var patcher = RitsuLibFramework.CreatePatcher(Const.ModId, "main");
            RegisterStaticPatches(patcher);

            OrchisRelicReadyPatchCleanup.RemoveUnsafePostfix();
            OrchisRestSitePatchCleanup.RemoveOriginalPostfixes();

            if (!RitsuLibFramework.ApplyRequiredPatcher(patcher, () => IsModActive = false))
            {
                Logger.Error("Mod initialization failed: patch application failed");
                return;
            }

            OrchisPlayAnimationPatch.ApplyDynamic(patcher);
            OrchisFeatureGatePatch.ApplyDynamic(patcher);

            IsModActive = true;
            Logger.Info("Mod initialization complete - Mod is now ACTIVE");
        }
        catch (Exception ex)
        {
            Logger.Error($"Mod initialization failed with exception: {ex}");
            IsModActive = false;
        }
    }

    private static void RegisterStaticPatches(ModPatcher patcher)
    {
        patcher.RegisterPatch<NRelicReadyPatch>();
        patcher.RegisterPatch<NNecrobinderVfxReadyPatch>();
        patcher.RegisterPatch<NNecrobinderVfxUpdateFlameVisibilityPatch>();
        patcher.RegisterPatch<NRestSiteCharacterReadyPatch>();
    }
}
