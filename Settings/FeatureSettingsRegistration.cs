using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace STS2OrchisNecrobinderSkinFix.Settings;

internal static class FeatureSettingsRegistration
{
    public const string SettingsKey = "settings";

    private const string SettingsFileName = "settings.json";

    public static void RegisterData()
    {
        using (RitsuLibFramework.BeginModDataRegistration(Const.ModId))
        {
            var store = RitsuLibFramework.GetDataStore(Const.ModId);
            store.Register(
                SettingsKey,
                SettingsFileName,
                SaveScope.Global,
                () => new FeatureSettingsState(),
                true);
        }
    }

    public static void RegisterSettings()
    {
        RitsuLibFramework.RegisterModSettings(Const.ModId, page => page
            .WithTitle(T("Orchis Necrobinder Skin Fix", "奥契丝亡灵皮肤修复"))
            .WithModDisplayName(T("Orchis Necrobinder Skin Fix", "奥契丝亡灵皮肤修复"))
            .WithDescription(T(
                "Choose which dynamic parts of OrchisNecrobinderSkinMod should stay active.",
                "选择 OrchisNecrobinderSkinMod 中哪些可动态控制的部分保持启用。"))
            .AddSection("text", ConfigureTextSection)
            .AddSection("icons", ConfigureIconSection)
            .AddSection("models", ConfigureModelsSection));
    }

    private static void ConfigureTextSection(ModSettingsSectionBuilder section)
    {
        section.WithTitle(T("Text replacements", "文本替换"));
        section.AddToggle("card_text", T("Card text", "卡牌文本"), Bind(s => s.CardText, (s, v) => s.CardText = v));
        section.AddToggle("relic_text", T("Relic text", "遗物文本"), Bind(s => s.RelicText, (s, v) => s.RelicText = v));
        section.AddToggle("power_text", T("Power text", "能力文本"), Bind(s => s.PowerText, (s, v) => s.PowerText = v));
        section.AddToggle("monster_text", T("Monster text", "怪物文本"),
            Bind(s => s.MonsterText, (s, v) => s.MonsterText = v));
        section.AddToggle("event_text", T("Event text", "事件文本"), Bind(s => s.EventText, (s, v) => s.EventText = v));
        section.AddToggle("achievement_text", T("Achievement text", "成就文本"),
            Bind(s => s.AchievementText, (s, v) => s.AchievementText = v));
        section.AddToggle("ancient_text", T("Ancient text", "远古文本"),
            Bind(s => s.AncientText, (s, v) => s.AncientText = v));
        section.AddToggle("epoch_text", T("Epoch text", "纪元文本"), Bind(s => s.EpochText, (s, v) => s.EpochText = v));
        section.AddToggle("hover_tip_text", T("Hover-tip text", "悬停提示文本"),
            Bind(s => s.HoverTipText, (s, v) => s.HoverTipText = v));
    }

    private static void ConfigureIconSection(ModSettingsSectionBuilder section)
    {
        section.WithTitle(T("Icons and card art", "图标和卡图"));
        section.AddToggle("card_portraits", T("Card portraits", "卡图"),
            Bind(s => s.CardPortraits, (s, v) => s.CardPortraits = v));
        section.AddToggle("relic_icons", T("Relic icons", "遗物图标"), Bind(s => s.RelicIcons, (s, v) => s.RelicIcons = v));
    }

    private static void ConfigureModelsSection(ModSettingsSectionBuilder section)
    {
        section.WithTitle(T("Models and scenes", "模型和场景"));
        section.AddToggle("necrobinder_combat_model", T("Combat: Orchis model", "战斗：奥契丝模型"),
            Bind(s => s.NecrobinderCombatModel, (s, v) => s.NecrobinderCombatModel = v));
        section.AddToggle("osty_combat_model", T("Combat: Lloyd body", "战斗：洛伊德本体"),
            Bind(s => s.OstyCombatModel, (s, v) => s.OstyCombatModel = v));
        section.AddToggle("necrobinder_merchant_model", T("Merchant: Orchis model", "商人：奥契丝模型"),
            Bind(s => s.NecrobinderMerchantModel, (s, v) => s.NecrobinderMerchantModel = v));
        section.AddToggle("necrobinder_fake_merchant_model", T("Fake Merchant event: Orchis model", "假商人事件：奥契丝模型"),
            Bind(s => s.NecrobinderFakeMerchantModel, (s, v) => s.NecrobinderFakeMerchantModel = v));
        section.AddToggle("necrobinder_rest_site_model", T("Rest site: Orchis model", "营火：奥契丝模型"),
            Bind(s => s.NecrobinderRestSiteModel, (s, v) => s.NecrobinderRestSiteModel = v));
        section.AddToggle("osty_rest_site_model", T("Rest site: Lloyd model", "营火：洛伊德模型"),
            Bind(s => s.OstyRestSiteModel, (s, v) => s.OstyRestSiteModel = v));
        section.AddToggle("character_select_scene", T("Character select background scene", "角色选择背景场景"),
            Bind(s => s.CharacterSelectScene, (s, v) => s.CharacterSelectScene = v));
        section.AddToggle("character_select_companion_models", T("Character select: companion models", "角色选择：同伴模型"),
            Bind(s => s.CharacterSelectCompanionModels, (s, v) => s.CharacterSelectCompanionModels = v));
        section.AddToggle("game_over_rest_site_visuals", T("Game over: show rest-site visuals", "游戏结束：营火模型显示"),
            Bind(s => s.GameOverRestSiteVisuals, (s, v) => s.GameOverRestSiteVisuals = v));
    }

    private static IModSettingsValueBinding<bool> Bind(
        Func<FeatureSettingsState, bool> getter,
        Action<FeatureSettingsState, bool> setter)
    {
        var defaults = new FeatureSettingsState();
        return ModSettingsBindings.WithDefault(
            ModSettingsBindings.Global(Const.ModId, SettingsKey, getter, setter),
            () => getter(defaults));
    }

    private static ModSettingsText T(string english, string chinese)
    {
        return ModSettingsText.Dynamic(() => IsChinese() ? chinese : english);
    }

    private static bool IsChinese()
    {
        try
        {
            return LocManager.Instance?.Language is "zhs" or "zht";
        }
        catch
        {
            return false;
        }
    }
}