using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace MorghsCheats
{
    public class MorghsCheatsSubModule : MBSubModuleBase
    {
        private bool _initialized = false;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            LogService.Log("========== Morgh's Cheats v3.1 ==========");
            LogService.Log("[Mod] 模组加载中...");

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                LogService.Error("AppDomain全局", ex ?? new Exception("未知异常"));
            };

            LogService.Log("[Mod] 模组加载完成");
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (!_initialized)
            {
                _initialized = true;
                try
                {
                    Patches.CheatPatches.Install();
                    LogService.Log("[Mod] Harmony 初始化完成");
                }
                catch (Exception ex) { LogService.Error("Harmony初始化", ex); }
            }
        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);
            LogService.Log("[Mod] 存档已加载");
        }

        public override void OnNewGameCreated(Game game, object initializerObject)
        {
            base.OnNewGameCreated(game, initializerObject);
            LogService.Log("[Mod] 新游戏已创建");
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            LogService.Log("[Mod] 游戏开始（功能由 MCM 菜单控制）");
            Settings.MorghsCheatsSettings.SetReady();
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(new CheatBattleBehavior());
            mission.AddMissionBehavior(new KillPromotionMissionBehavior());
            LogService.Log("[Mod] 战场行为已注册 (Battle/KillPromotion)");
        }
    }

    /// <summary>
    /// 一键换装工具：从全游戏装备库选最强装备给所有伙伴穿上
    /// </summary>
    public static class AutoEquipHelper
    {
        /// <summary>给所有在队伙伴穿上全游戏最强的装备</summary>
        public static int EquipAllCompanions()
        {
            try
            {
                if (Campaign.Current == null || Hero.MainHero == null) return 0;

                var clan = Hero.MainHero.Clan;
                var party = Hero.MainHero.PartyBelongedTo;
                if (clan == null || party == null) return 0;

                var targets = clan.Heroes
                    .Where(h => h != Hero.MainHero && h.IsAlive && !h.IsDisabled && !h.IsNotSpawned && !h.IsChild)
                    .ToList();
                if (targets.Count == 0) return 0;

                // 遍历全游戏装备，按类型记录价值最高的
                var best = new Dictionary<ItemObject.ItemTypeEnum, ItemObject>();
                foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                {
                    if (item == null || !IsHeroEquipable(item)) continue;
                    if (!best.ContainsKey(item.Type) || item.Value > best[item.Type].Value)
                        best[item.Type] = item;
                }

                int count = 0;
                foreach (var m in targets)
                {
                    if (m.PartyBelongedTo != party) continue;
                    var eq = m.BattleEquipment;
                    if (eq == null) continue;

                    // 填满12个槽位
                    eq[EquipmentIndex.Head]         = Pick(best, ItemObject.ItemTypeEnum.HeadArmor);
                    eq[EquipmentIndex.Cape]         = Pick(best, ItemObject.ItemTypeEnum.Cape);
                    eq[EquipmentIndex.Body]         = Pick(best, ItemObject.ItemTypeEnum.BodyArmor);
                    eq[EquipmentIndex.Gloves]       = Pick(best, ItemObject.ItemTypeEnum.HandArmor);
                    eq[EquipmentIndex.Leg]          = Pick(best, ItemObject.ItemTypeEnum.LegArmor);
                    eq[EquipmentIndex.Horse]        = Pick(best, ItemObject.ItemTypeEnum.Horse);
                    eq[EquipmentIndex.HorseHarness] = Pick(best, ItemObject.ItemTypeEnum.HorseHarness);
                    eq[EquipmentIndex.Weapon0]      = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("khuzait_polearm_2_t5")); // 长偃月刀
                    eq[EquipmentIndex.Weapon1]      = Pick(best, ItemObject.ItemTypeEnum.Shield);
                    eq[EquipmentIndex.Weapon2]      = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_bow"));           // 贵族弓
                    eq[EquipmentIndex.Weapon3]      = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("heavy_steppe_arrows")); // 一大袋草原箭
                    eq[EquipmentIndex.ExtraWeaponSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("heavy_steppe_arrows"));
                    count++;
                }
                return count;
            }
            catch { return 0; }
        }

        private static EquipmentElement Pick(Dictionary<ItemObject.ItemTypeEnum, ItemObject> best, ItemObject.ItemTypeEnum type)
        {
            if (best.TryGetValue(type, out var item)) return new EquipmentElement(item);
            return default;
        }

        private static bool IsHeroEquipable(ItemObject item)
        {
            if (item == null) return false;
            if (item.ArmorComponent != null) return true;
            if (item.Type == ItemObject.ItemTypeEnum.Horse || item.Type == ItemObject.ItemTypeEnum.HorseHarness)
                return true;
            if (item.WeaponComponent == null) return false;
            switch (item.Type)
            {
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                case ItemObject.ItemTypeEnum.Polearm:
                case ItemObject.ItemTypeEnum.Bow:
                case ItemObject.ItemTypeEnum.Crossbow:
                case ItemObject.ItemTypeEnum.Shield:
                case ItemObject.ItemTypeEnum.Thrown:
                case ItemObject.ItemTypeEnum.Arrows:
                case ItemObject.ItemTypeEnum.Bolts:
                    return true;
            }
            return false;
        }
    }
}
