using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace MorghsCheats
{
    /// <summary>
    /// 全局作弊服务：一键加点 + 安全执行包装
    /// </summary>
    public static class CheatService
    {
        /// <summary>安全执行：捕获所有异常并记日志，防止传播到游戏引擎导致崩溃</summary>
        public static void SafeExecute(string source, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogService.Error(source, ex);
                try
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[!] {source} 异常: {ex.Message}", Color.FromUint(0xFFFF4444u)));
                }
                catch { }
            }
        }

        public static void MapMaxAllHeroes()
        {
            LogService.Log("========== [一键加点] 开始 ==========");
            if (Campaign.Current == null) { LogService.Log("  Campaign.Current == null"); return; }
            var mainHero = Hero.MainHero;
            if (mainHero == null) { LogService.Log("  mainHero == null"); return; }
            var playerClan = mainHero.Clan;
            if (playerClan == null) { LogService.Log("  playerClan == null"); return; }

            var allHeroes = new List<Hero>();
            allHeroes.Add(mainHero);
            foreach (var h in playerClan.Heroes)
            {
                if (h != null && h != mainHero && h.IsAlive && !h.IsDisabled)
                    allHeroes.Add(h);
            }
            LogService.Log($"  目标英雄数: {allHeroes.Count}");

            foreach (var hero in allHeroes)
            {
                try { MaxHero(hero); }
                catch (Exception ex) { LogService.Error($"英雄 {hero.Name}", ex); }
            }
            InformationManager.DisplayMessage(new InformationMessage($"[✓] 一键加点完成，共处理 {allHeroes.Count} 名英雄"));
            LogService.Log("========== [一键加点] 结束 ==========");
        }

        private static void MaxHero(Hero hero)
        {
            string hName = hero.Name?.ToString() ?? "未知";
            LogService.Log($"===== MaxHero: {hName} =====");

            if (hero?.HeroDeveloper == null) { LogService.Log("  HeroDeveloper 为 null，跳过"); return; }
            var dev = hero.HeroDeveloper;

            int maxAttr = 10;
            int maxFocus = 5;
            int maxLevel = 62;
            int targetSkill = Settings.MCMConfigProvider.GetTargetSkillLevel();
            try
            {
                maxAttr = Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute;
                maxFocus = Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill;
            }
            catch { }

            dev.UnspentAttributePoints += 200;
            dev.UnspentFocusPoints += 200;

            // 六维属性
            string[] attrIds = { "vigor", "control", "endurance", "cunning", "social", "intelligence" };
            foreach (var attrId in attrIds)
            {
                try
                {
                    var attr = GetCharAttribute(attrId);
                    if (attr == null) continue;
                    int current = hero.GetAttributeValue(attr);
                    int toAdd = maxAttr - current;
                    if (toAdd > 0) dev.AddAttribute(attr, toAdd, false);
                }
                catch { }
            }

            // 18项技能
            var skillList = new SkillObject[] {
                DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm,
                DefaultSkills.Bow, DefaultSkills.Crossbow, DefaultSkills.Throwing,
                DefaultSkills.Riding, DefaultSkills.Athletics, DefaultSkills.Crafting,
                DefaultSkills.Scouting, DefaultSkills.Tactics, DefaultSkills.Roguery,
                DefaultSkills.Charm, DefaultSkills.Steward, DefaultSkills.Trade,
                DefaultSkills.Medicine, DefaultSkills.Leadership, DefaultSkills.Engineering
            };

            foreach (var skill in skillList)
            {
                if (skill == null) continue;
                try
                {
                    hero.SetSkillValue(skill, targetSkill);
                    dev.InitializeSkillXp(skill);
                }
                catch { }
            }

            // 专精加满
            foreach (var skill in skillList)
            {
                if (skill == null) continue;
                try
                {
                    int currentFocus = dev.GetFocus(skill);
                    int toAdd = maxFocus - currentFocus;
                    if (toAdd > 0) dev.AddFocus(skill, toAdd, false);
                }
                catch { }
            }

            dev.UnspentAttributePoints = 0;
            dev.UnspentFocusPoints = 0;
            LogService.Log($"  {hName} 属性/技能/专精设置完成");

            // 等级
            try { hero.Level = maxLevel; } catch { }

            // 解锁全部 Perk
            try
            {
                int count = 0;
                foreach (var perk in PerkObject.All)
                {
                    try { if (!hero.GetPerkValue(perk)) { dev.AddPerk(perk); count++; } }
                    catch { }
                }
                LogService.Log($"  {hName} 解锁 {count} 个Perk");
            }
            catch { }

            LogService.Log($"===== MaxHero 完成: {hName} =====");
        }

        private static CharacterAttribute GetCharAttribute(string id)
        {
            try
            {
                foreach (var attr in Attributes.All)
                {
                    if (attr.StringId.Equals(id, StringComparison.OrdinalIgnoreCase))
                        return attr;
                }
            }
            catch { }
            return null;
        }
    }
}
