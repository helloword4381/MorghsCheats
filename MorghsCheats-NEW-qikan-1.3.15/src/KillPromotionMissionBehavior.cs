using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace MorghsCheats
{
    /// <summary>
    /// 兵种杀敌追踪与晋升系统。
    /// 友方兵种累计击杀 1000 人后，自动晋升为家族伙伴 NPC。
    /// </summary>
    public class KillPromotionMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private static KillPromotionMissionBehavior? _current;
        public static KillPromotionMissionBehavior? Current => _current;

        private static readonly Dictionary<string, int> _troopKillCount = new Dictionary<string, int>();
        private static readonly HashSet<string> _promotedTroops = new HashSet<string>();
        private bool _battleEndedProcessed = false;

        private int KillThreshold => Settings.MCMConfigProvider.GetKillPromotionThreshold();

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _current = this;
            LogService.Log("[击杀追踪] OnBehaviorInitialize");
        }

        public override void OnRemoveBehavior()
        {
            _current = null;
            LogService.Log("[击杀追踪] OnRemoveBehavior");
            base.OnRemoveBehavior();
        }

        public override void AfterStart()
        {
            _battleEndedProcessed = false;
            LogService.Log("[击杀追踪] 战斗开始！累计击杀统计继承上次战斗");
            base.AfterStart();
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (_battleEndedProcessed) return;
            if (!Settings.MCMConfigProvider.IsKillPromotionEnabled()) return;

            try
            {
                if (affectorAgent?.Character == null) return;
                if (affectedAgent?.Character == null) return;
                if (affectedAgent.Character.IsHero) return;

                var mainAgent = Mission.Current?.MainAgent;
                if (mainAgent?.Team == null) return;
                if (affectorAgent.Team != mainAgent.Team) return;
                if (affectedAgent.Team == mainAgent.Team) return;

                string killerTroopId = affectorAgent.Character.StringId;
                if (string.IsNullOrEmpty(killerTroopId)) return;

                if (!_troopKillCount.ContainsKey(killerTroopId))
                    _troopKillCount[killerTroopId] = 0;

                _troopKillCount[killerTroopId]++;
                int kills = _troopKillCount[killerTroopId];

                if (kills >= KillThreshold && !_promotedTroops.Contains(killerTroopId))
                {
                    _promotedTroops.Add(killerTroopId);
                    TryPromoteTroopToHero(affectorAgent.Character as CharacterObject, kills);
                }
            }
            catch (Exception ex)
            {
                LogService.Error("KillPromotion.OnAgentRemoved", ex);
            }
        }

        protected override void OnEndMission()
        {
            if (_battleEndedProcessed) return;
            try
            {
                _battleEndedProcessed = true;
                ProcessSurvivingTroopKills();

                string msg = "[击杀统计] " + GetKillSummary();
                LogService.Log(msg);
                Info(msg);
            }
            catch (Exception ex)
            {
                LogService.Error("KillPromotion.OnEndMission", ex);
            }
            base.OnEndMission();
        }

        private void ProcessSurvivingTroopKills()
        {
            foreach (var kv in _troopKillCount)
            {
                if (_promotedTroops.Contains(kv.Key)) continue;
                if (kv.Value < KillThreshold) continue;

                var troop = MBObjectManager.Instance.GetObject<CharacterObject>(kv.Key);
                if (troop != null)
                {
                    _promotedTroops.Add(kv.Key);
                    TryPromoteTroopToHero(troop, kv.Value);
                }
            }
        }

        private void TryPromoteTroopToHero(CharacterObject? troopTemplate, int killCount)
        {
            try
            {
                if (troopTemplate == null) return;
                if (Campaign.Current == null) return;

                var playerClan = Hero.MainHero?.Clan;
                if (playerClan == null) return;

                var homeSettlement = GetPlayerHomeSettlement();
                if (homeSettlement == null)
                {
                    Info("[晋升] 没有可用定居点，无法晋升");
                    return;
                }

                LogService.Log($"[晋升] 开始晋升：{troopTemplate.Name}");

                var hero = HeroCreator.CreateSpecialHero(troopTemplate, homeSettlement, playerClan, age: 25);
                hero.Level = 30;

                SetHeroMaxAttributes(hero);
                SetHeroMaxSkills(hero);
                SetHeroMaxPerks(hero);

                AddCompanionAction.Apply(playerClan, hero);

                string msg = $"[晋升] {troopTemplate.Name} (杀{KillThreshold}人→家族伙伴)「{hero.Name}」";
                LogService.Log(msg);
                Info(msg);
            }
            catch (Exception ex)
            {
                LogService.Error("KillPromotion.TryPromote", ex);
                Err($"[晋升] 失败：{ex.Message}");
            }
        }

        private void SetHeroMaxAttributes(Hero hero)
        {
            try
            {
                var dev = hero.HeroDeveloper;
                string[] attrIds = { "vigor", "control", "endurance", "cunning", "social", "intelligence" };
                foreach (var attrId in attrIds)
                {
                    var attr = MBObjectManager.Instance.GetObject<CharacterAttribute>(attrId);
                    if (attr == null) continue;
                    try
                    {
                        var devType = dev.GetType();
                        var dictField = devType.GetField("_attributeValues", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (dictField != null)
                        {
                            var dict = dictField.GetValue(dev) as System.Collections.IDictionary;
                            if (dict != null)
                            {
                                if (dict.Contains(attr)) dict[attr] = 10;
                                else dict.Add(attr, 10);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void SetHeroMaxSkills(Hero hero)
        {
            try
            {
                var dev = hero.HeroDeveloper;
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
                    try { for (int i = 0; i < 10; i++) dev.AddSkillXp(skill, 100_000_000f); }
                    catch { }
                }
            }
            catch { }
        }

        private void SetHeroMaxPerks(Hero hero)
        {
            try
            {
                var setPerkMethod = typeof(Hero).GetMethod("SetPerkValueInternal",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (setPerkMethod == null) return;

                int count = 0;
                foreach (var perk in PerkObject.All)
                {
                    try { setPerkMethod.Invoke(hero, new object[] { perk, true }); count++; }
                    catch { }
                }
                LogService.Log($"[晋升] Perks 解锁 {count} 个");
            }
            catch { }
        }

        private Settlement? GetPlayerHomeSettlement()
        {
            var clan = Hero.MainHero?.Clan;
            if (clan == null) return null;

            if (clan.Settlements.Count > 0)
            {
                foreach (var s in clan.Settlements)
                    if (s.IsTown || s.IsCastle) return s;
            }

            foreach (var s in Settlement.All)
                if (s.OwnerClan == clan && (s.IsTown || s.IsCastle)) return s;

            return Settlement.All.GetRandomElementWithPredicate(s => s.OwnerClan == clan);
        }

        private string GetKillSummary()
        {
            if (_troopKillCount.Count == 0) return "无";

            var lines = new List<string>();
            foreach (var kv in _troopKillCount)
            {
                var troop = MBObjectManager.Instance.GetObject<CharacterObject>(kv.Key);
                string name = troop?.Name?.ToString() ?? kv.Key;
                bool promoted = _promotedTroops.Contains(kv.Key);
                lines.Add($"{name}:{kv.Value}{(promoted ? "★" : "")}");
            }
            return string.Join(" | ", lines);
        }

        public static string GetGlobalKillReport()
        {
            lock (_troopKillCount)
            {
                if (_troopKillCount.Count == 0) return "无累计击杀";
                var lines = new List<string>();
                foreach (var kv in _troopKillCount)
                {
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>(kv.Key);
                    string name = troop?.Name?.ToString() ?? kv.Key;
                    bool promoted = _promotedTroops.Contains(kv.Key);
                    lines.Add($"{name}:{kv.Value}{(promoted ? "★" : "")}");
                }
                return string.Join(" | ", lines);
            }
        }

        private static void Info(string text)
        {
            try { InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(0xFF00FF00u))); }
            catch { }
        }

        private static void Err(string text)
        {
            try { InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(0xFFFF4444u))); }
            catch { }
        }
    }
}
