using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2OrchisNecrobinderSkinFix.Diagnostics;
using STS2RitsuLib;

namespace STS2OrchisNecrobinderSkinFix.Settings;

internal static class FeatureSettings
{
    private static readonly FeatureSettingsState DefaultState = new();
    private static readonly LogLimiter SettingsFallbackLog = new("Using default feature settings");

    public static FeatureSettingsState Current =>
        GetCurrentOrDefault();

    private static FeatureSettingsState GetCurrentOrDefault()
    {
        try
        {
            return RitsuLibFramework.GetDataStore(Const.ModId).Get<FeatureSettingsState>(
                FeatureSettingsRegistration.SettingsKey) ?? DefaultState;
        }
        catch (Exception ex)
        {
            SettingsFallbackLog.Info(ex.Message);
            return DefaultState;
        }
    }

    public static bool IsTextTableEnabled(string? locTable)
    {
        var settings = Current;
        return locTable switch
        {
            "cards" => settings.CardText,
            "relics" => settings.RelicText,
            "powers" => settings.PowerText,
            "monsters" => settings.MonsterText,
            "events" => settings.EventText,
            "achievements" => settings.AchievementText,
            "ancients" => settings.AncientText,
            "epochs" => settings.EpochText,
            "static_hover_tips" => settings.HoverTipText,
            _ => true
        };
    }

    public static bool IsTextLocStringEnabled(LocString locString)
    {
        return IsTextTableEnabled(locString.LocTable);
    }

    public static bool IsSkeletonPathEnabled(string? resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return true;

        var normalized = resourcePath.Replace('\\', '/');
        var settings = Current;

        if (normalized.EndsWith("/animations/characters/necrobinder/necrobinder_combat_skel_data.tres",
                StringComparison.OrdinalIgnoreCase))
            return settings.NecrobinderCombatModel;

        if (normalized.EndsWith("/animations/monsters/osty/osty_combat_skel_data.tres",
                StringComparison.OrdinalIgnoreCase))
            return settings.OstyCombatModel;

        if (normalized.EndsWith("/animations/merchant/necrobinder/necrobinder_shop_skel_data.tres",
                StringComparison.OrdinalIgnoreCase))
            return IsFakeMerchantEventRoom(GetCurrentRoom())
                ? settings.NecrobinderFakeMerchantModel
                : settings.NecrobinderMerchantModel;

        if (normalized.EndsWith("/animations/rest_site/necrobinder/rest_site_necrobinder_skel_data.tres",
                StringComparison.OrdinalIgnoreCase))
            return settings.NecrobinderRestSiteModel;

        if (normalized.EndsWith("/animations/rest_site/necrobinder/lloyd_relax_skel_data.tres",
                StringComparison.OrdinalIgnoreCase))
            return settings.OstyRestSiteModel;

        if (normalized.EndsWith("/animations/char_select/asset/data/lloyd.tres",
                StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith("/animations/char_select/asset/data/oqs.tres",
                StringComparison.OrdinalIgnoreCase))
            return settings.CharacterSelectCompanionModels;

        return true;
    }

    private static AbstractRoom? GetCurrentRoom()
    {
        try
        {
            return RunManager.Instance.DebugOnlyGetState()?.CurrentRoom;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsFakeMerchantEventRoom(AbstractRoom? currentRoom)
    {
        return currentRoom is EventRoom { CanonicalEvent: FakeMerchant };
    }
}