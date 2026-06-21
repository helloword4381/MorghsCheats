using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace MorghsCheats.Settings
{
    public class MorghsCheatsSettings : AttributeGlobalSettings<MorghsCheatsSettings>
    {
        public override string Id => "MorghsCheats_v3";
        public override string DisplayName => "Morgh's Cheats";
        public override string FolderName => "MorghsCheats";
        public override string FormatType => "json";

        private static bool _ready = false;
        internal static void SetReady() => _ready = true;

        // ═════════════════════════════════════════════════════
        //  一、金币/声望/影响力（RequireRestart=false 所有设置）
        // ═════════════════════════════════════════════════════

        [SettingPropertyFloatingInteger("金币增加量", 100000f, 10000000f, Order = 0, HintText = "每次点击增加的金币数量", RequireRestart = false)]
        [SettingPropertyGroup("金币/声望/影响力", GroupOrder = 0)]
        public float GoldAmount { get; set; } = 1000000f;

        private bool _applyGold;
        [SettingPropertyBool("增加金币", Order = 1, HintText = "点一下增加金币", RequireRestart = false)]
        [SettingPropertyGroup("金币/声望/影响力")]
        public bool ApplyGold
        {
            get => _applyGold;
            set { if (value && _ready) { _applyGold = false; ExecInGame(() => { if (Instance != null) { Hero.MainHero!.Gold += (int)Instance.GoldAmount; GameMsg($"[✓] 金币 +{Instance.GoldAmount:N0}"); } }); } }
        }

        [SettingPropertyFloatingInteger("声望增加量", 1000f, 100000f, Order = 2, HintText = "每次点击增加的声望数量", RequireRestart = false)]
        [SettingPropertyGroup("金币/声望/影响力")]
        public float RenownAmount { get; set; } = 10000f;

        private bool _applyRenown;
        [SettingPropertyBool("增加声望", Order = 3, HintText = "点一下增加声望", RequireRestart = false)]
        [SettingPropertyGroup("金币/声望/影响力")]
        public bool ApplyRenown
        {
            get => _applyRenown;
            set { if (value && _ready) { _applyRenown = false; ExecInGame(() => { if (Instance != null) { Hero.MainHero.Clan?.AddRenown((int)Instance.RenownAmount); GameMsg($"[✓] 声望 +{Instance.RenownAmount:N0}"); } }); } }
        }

        [SettingPropertyFloatingInteger("影响力增加量", 1000f, 100000f, Order = 4, HintText = "每次点击增加的影响力数量", RequireRestart = false)]
        [SettingPropertyGroup("金币/声望/影响力")]
        public float InfluenceAmount { get; set; } = 10000f;

        private bool _applyInfluence;
        [SettingPropertyBool("增加影响力", Order = 5, HintText = "点一下增加影响力", RequireRestart = false)]
        [SettingPropertyGroup("金币/声望/影响力")]
        public bool ApplyInfluence
        {
            get => _applyInfluence;
            set { if (value && _ready) { _applyInfluence = false; ExecInGame(() => { if (Instance != null) { Hero.MainHero.Clan!.Influence += Instance.InfluenceAmount; GameMsg($"[✓] 影响力 +{Instance.InfluenceAmount:N0}"); } }); } }
        }

        // ═════════════════════════════════════════════════════
        //  二、一键加点
        // ═════════════════════════════════════════════════════

        [SettingPropertyFloatingInteger("目标技能等级", 100f, 500f, Order = 0, HintText = "一键加点时所有技能升到的等级", RequireRestart = false)]
        [SettingPropertyGroup("一键加点", GroupOrder = 1)]
        public float TargetSkillLevel { get; set; } = 350f;

        private bool _maxAll;
        [SettingPropertyBool("一键全属性满级", Order = 1, HintText = "按设定等级加点", RequireRestart = false)]
        [SettingPropertyGroup("一键加点")]
        public bool MaxAllHeroes
        {
            get => _maxAll;
            set { if (value && _ready) { _maxAll = false; ExecInGame(() => CheatService.MapMaxAllHeroes()); } }
        }

        // ═════════════════════════════════════════════════════
        //  三、好感度
        // ═════════════════════════════════════════════════════

        private bool _setCompRel;
        [SettingPropertyBool("伙伴好感度→100", Order = 0, HintText = "所有家族伙伴好感度设为100", RequireRestart = false)]
        [SettingPropertyGroup("好感度", GroupOrder = 2)]
        public bool SetCompanionRelation100
        {
            get => _setCompRel;
            set { if (value && _ready) { _setCompRel = false; ExecInGame(SetCompanionRelationAction); } }
        }

        [SettingPropertyFloatingInteger("领主好感增加量", 1f, 50f, Order = 1, HintText = "每次增加的本王国领主好感度数值", RequireRestart = false)]
        [SettingPropertyGroup("好感度")]
        public float LordRelationIncrement { get; set; } = 10f;

        private bool _addLordRel;
        [SettingPropertyBool("增加领主好感", Order = 2, HintText = "按设定值增加领主好感", RequireRestart = false)]
        [SettingPropertyGroup("好感度")]
        public bool AddLordRelation
        {
            get => _addLordRel;
            set { if (value && _ready) { _addLordRel = false; ExecInGame(SetLordRelationAction); } }
        }

        // ═════════════════════════════════════════════════════
        //  四、自动功能
        // ═════════════════════════════════════════════════════

        // 换装已改为 MCM 按钮手动触发，不再监听交易事件

        [SettingPropertyBool("锁3倍速", Order = 2, HintText = "大地图始终以3倍速运行", RequireRestart = false)]
        [SettingPropertyGroup("自动功能")]
        public bool EnableSpeedLock { get; set; } = false;

        [SettingPropertyFloatingInteger("伙伴上限加成", 0f, 500f, Order = 3, HintText = "家族可加入伙伴数量额外增加", RequireRestart = false)]
        [SettingPropertyGroup("自动功能")]
        public float CompanionLimitBonus { get; set; } = 50f;

        [SettingPropertyFloatingInteger("部队上限加成", 0f, 500f, Order = 4, HintText = "家族可创建部队数量额外增加", RequireRestart = false)]
        [SettingPropertyGroup("自动功能")]
        public float PartyLimitBonus { get; set; } = 50f;

        [SettingPropertyBool("杀敌晋升", Order = 5, HintText = "兵种累计击杀达到阈值后自动晋升为家族伙伴NPC", RequireRestart = false)]
        [SettingPropertyGroup("自动功能")]
        public bool EnableKillPromotion { get; set; } = false;

        [SettingPropertyFloatingInteger("晋升杀敌阈值", 500f, 1000f, Order = 6, HintText = "兵种累计击杀多少敌人后晋升为伙伴", RequireRestart = false)]
        [SettingPropertyGroup("自动功能")]
        public float KillPromotionThreshold { get; set; } = 1000f;

        private bool _equipAll;
        [SettingPropertyBool("一键换装(最强骑射套)", Order = 7, HintText = "从全游戏装备库选最强骑射装备给所有伙伴穿上（每点一次执行一次）", RequireRestart = false)]
        [SettingPropertyGroup("自动功能")]
        public bool EquipAllCompanions
        {
            get => _equipAll;
            set { if (value && _ready) { _equipAll = false; ExecInGame(DoEquipAll); } }
        }

        // ═════════════════════════════════════════════════════
        //  五、战场作弊
        // ═════════════════════════════════════════════════════

        [SettingPropertyFloatingInteger("HP倍率", 1f, 10f, Order = 0, HintText = "战场按小键盘1后生效的HP倍率", RequireRestart = false)]
        [SettingPropertyGroup("战场作弊", GroupOrder = 4)]
        public float BattleHpMultiplier { get; set; } = 1f;

        [SettingPropertyFloatingInteger("士兵/马匹HP倍率", 1f, 10f, Order = 1, HintText = "友方士兵和马匹的HP倍率，自动生效", RequireRestart = false)]
        [SettingPropertyGroup("战场作弊", GroupOrder = 4)]
        public float TroopHpMultiplier { get; set; } = 1f;

        // ═════════════════════════════════════════════════════
        //  六、快捷键说明
        // ═════════════════════════════════════════════════════

        private bool _showKeys;
        [SettingPropertyBool("查看快捷键说明", Order = 0, HintText = "显示所有快捷键", RequireRestart = false)]
        [SettingPropertyGroup("快捷键说明", GroupOrder = 5)]
        public bool ShowHotkeys
        {
            get => _showKeys;
            set { if (value && _ready) { _showKeys = false; ShowHotkeyHelp(); } }
        }

        // ═════════════════════════════════════════════════════
        //  七、帮助
        // ═════════════════════════════════════════════════════

        private bool _openLog;
        [SettingPropertyBool("查看崩溃日志", Order = 0, HintText = "打开Mod调试日志文件", RequireRestart = false)]
        [SettingPropertyGroup("帮助", GroupOrder = 6)]
        public bool OpenLog
        {
            get => _openLog;
            set { if (value && _ready) { _openLog = false; OpenLogFile(); } }
        }

        // ═════════════════════════════════════════════════════
        //  内部方法
        // ═════════════════════════════════════════════════════

        private static void ExecInGame(Action action)
        {
            try
            {
                if (Campaign.Current == null || Hero.MainHero == null) { LogService.Log("[MCM] 未在游戏中，忽略"); return; }
                action();
            }
            catch (Exception ex) { LogService.Error("[MCM] 操作异常", ex); }
        }

        private static void GameMsg(string text)
        {
            try { InformationManager.DisplayMessage(new InformationMessage(text)); }
            catch { }
        }

        private static void SetCompanionRelationAction()
        {
            var clan = Hero.MainHero.Clan;
            if (clan == null) return;
            int count = 0;
            foreach (var h in clan.Companions)
                if (h != null && h.IsAlive && !h.IsDisabled && h != Hero.MainHero)
                { try { h.SetPersonalRelation(Hero.MainHero, 100); count++; } catch { } }
            GameMsg($"[✓] 伙伴好感→100（{count}人）");
        }

        private static void SetLordRelationAction()
        {
            var inc = (int)(Instance?.LordRelationIncrement ?? 10);
            var clan = Hero.MainHero.Clan;
            if (clan?.Kingdom == null) { GameMsg("[!] 你没有王国"); return; }
            int count = 0;
            foreach (var c in clan.Kingdom.Clans)
            {
                if (c == clan) continue;
                foreach (var h in c.Heroes)
                {
                    if (h != Hero.MainHero && h.IsAlive && !h.IsDisabled && h.IsLord)
                    {
                        try { int cur = h.GetRelation(Hero.MainHero); h.SetPersonalRelation(Hero.MainHero, cur + inc); count++; }
                        catch { }
                    }
                }
            }
            GameMsg($"[✓] 领主好感+{inc}（{count}人）");
        }

        private static void ShowHotkeyHelp()
        {
            GameMsg(@"
╔══════════════════════════════════════════╗
║     Morgh's Cheats v3.1                  ║
╠══════════════════════════════════════════╣
║  Mod菜单操作（所有功能都在菜单控制）       ║
║  金币/声望/影响力 → 菜单设数量+点击按钮    ║
║  一键加点 → 菜单设等级+点击按钮            ║
║  好感度 → 菜单点击按钮                   ║
║  锁速/建造/换装 → 菜单开关               ║
╠══════════════════════════════════════════╣
║  键盘快捷操作（仅战场可用）                ║
║  F6 → AI托管+快进                        ║
║  小键盘1~6 → 召唤各国精英兵种             ║
╚══════════════════════════════════════════╝");
        }

        private static void OpenLogFile()
        {
            try
            {
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Mount and Blade II Bannerlord", "MorghsCheats_debug.log");
                System.Diagnostics.Process.Start("notepad.exe", path);
            }
            catch { }
        }

        private static void DoEquipAll()
        {
            int count = AutoEquipHelper.EquipAllCompanions();
            if (count > 0) GameMsg($"[换装] 已为 {count} 名伙伴装备最强骑射套装");
            else GameMsg("[换装] 没有可换装的伙伴");
        }
    }
}
