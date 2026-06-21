using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using AgentControllerType = TaleWorlds.Core.AgentControllerType;

namespace MorghsCheats
{
    /// <summary>
    /// 战场作弊 MissionBehavior
    /// F6 → 原生委托 + AI托管 + 4x快进（一键三连）
    /// 适用场景：野战、攻城（进城墙后）、藏身处、竞技场
    /// 小键盘1~6 → 召唤各国精英兵种（HP倍率由 MCM 菜单控制）
    /// </summary>
    public class CheatBattleBehavior : MissionBehavior
    {
        private bool _aiControlActive = false;
        private bool _fastForwardActive = false;
        private bool _heroHpActive = false;
        private bool _heroHpApplied = false;
        private float _heroHpMul = 1f;
        private bool _troopHpActive = false;
        private bool _troopHpApplied = false;
        private float _troopHpMul = 1f;
        private int _tickCounter = 0;
        private readonly bool[] _numpadDown = new bool[10];
        private readonly List<Agent> _summonedAgents = new List<Agent>();
        private bool _f6Down = false;
        private int _pendingAIControl = 0;
        private bool _hasFormations = false;

        private static readonly string[] _troopSlots = {
            "",                          // 0 (unused)
            "aserai_vanguard_faris",     // 1 阿塞莱先锋法里斯
            "vlandian_banner_knight",    // 2 瓦兰迪亚方旗骑士
            "druzhinnik_champion",       // 3 斯特吉亚精锐亲卫骑兵
            "khuzait_khans_guard",      // 4 库塞特可汗卫士
            "imperial_elite_cataphract", // 5 帝国精英具装骑士
            "battanian_fian_champion",   // 6 巴旦尼亚费奥纳冠军
        };

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        public static CheatBattleBehavior? Current { get; private set; }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Current = this;
        }

        public override void OnRemoveBehavior()
        {
            if (Current == this) Current = null;
            base.OnRemoveBehavior();
        }

        public override void AfterStart()
        {
            _tickCounter = 0;

            // 检测是否是有阵型的战斗场景（野战/攻城有阵型，藏身处/竞技场无阵型）
            _hasFormations = Mission?.PlayerTeam?.FormationsIncludingSpecialAndEmpty != null
                && Mission.PlayerTeam.FormationsIncludingSpecialAndEmpty.Any(f => f?.CountOfUnits > 0);

            // 入场时重置状态
            ResetCombatState();

            // 自动读取 MCM 滑块值 → 进战场自动生效 HP 倍率（不依赖 MainAgent 是否就绪）
            float hpMul = Settings.MCMConfigProvider.GetHpMultiplier();
            if (hpMul > 1f)
            {
                _heroHpActive = true;
                _heroHpMul = hpMul;
                _heroHpApplied = false;
                // 如果 MainAgent 已就绪则立即应用，否则等 OnMissionTick 中补
                if (Mission?.MainAgent != null)
                {
                    ApplyHeroHp();
                    _heroHpApplied = true;
                    Info($"[HP] 自动生效 x{hpMul:F0}");
                }
            }

            // 士兵/马匹 HP 倍率
            float tMul = Settings.MCMConfigProvider.GetTroopHpMultiplier();
            if (tMul > 1f)
            {
                _troopHpActive = true;
                _troopHpMul = tMul;
                _troopHpApplied = false;
                if (Mission != null)
                {
                    ApplyFriendlyHp();
                    _troopHpApplied = true;
                    Info($"[兵HP] 自动生效 x{tMul:F0}");
                }
            }

            // 藏身处/偷袭场景：强制自动AI托管 + 快进
            if (Mission?.Mode == MissionMode.Stealth && Mission?.MainAgent != null)
            {
                Info("[入场] 藏身处模式 → 自动AI托管 + 快进");
                SetAiControl(true);
                SetFastForward(true);
            }

            base.AfterStart();
        }

        private void ResetCombatState()
        {
            if (_aiControlActive)
            {
                SetAiControl(false);
                Info("[入场] 关闭AI托管");
            }
            if (_fastForwardActive && Mission != null)
            {
                Mission.SetFastForwardingFromUI(false);
                _fastForwardActive = false;
                Info("[入场] 关闭快进");
            }
            _pendingAIControl = 0;
            _heroHpActive = false;
            _heroHpMul = 1f;
            _troopHpActive = false;
            _troopHpMul = 1f;
        }

        public override void OnMissionTick(float dt)
        {
            if (Mission == null) return;
            try
            {
                _tickCounter++;
                var mainAgent = Mission.MainAgent;
                if (mainAgent == null) return;

                HandleHotkeys(mainAgent);

                // 主角 HP倍率：首次就绪时补应用，然后每5帧刷新
                if (_heroHpActive)
                {
                    if (!_heroHpApplied)
                    {
                        _heroHpApplied = true;
                        ApplyHeroHp();
                        Info($"[HP] 自动生效 x{_heroHpMul:F0}");
                    }
                    if (_tickCounter % 5 == 0)
                        ApplyHeroHp();
                }

                // 士兵/马匹 HP倍率：首次就绪时补应用，然后每60帧刷新
                if (_troopHpActive)
                {
                    if (!_troopHpApplied)
                    {
                        _troopHpApplied = true;
                        ApplyFriendlyHp();
                        Info($"[兵HP] 自动生效 x{_troopHpMul:F0}");
                    }
                    if (_tickCounter % 60 == 0)
                        ApplyFriendlyHp();
                }

                // 延迟AI托管：让原生F6委托先走一帧再接管AI
                if (_pendingAIControl > 0)
                {
                    _pendingAIControl--;
                    if (_pendingAIControl == 0 && !_aiControlActive)
                    {
                        SetAiControl(true);
                        Info("[F6] AI托管已接管");
                    }
                }

                // AI战斗辅助：防发呆（仅在有阵型场景生效）
                if (_aiControlActive && _hasFormations && mainAgent.Controller == AgentControllerType.AI)
                    TickCombatAssist(mainAgent);
            }
            catch (Exception ex)
            {
                LogService.Error("CheatBattleBehavior.OnMissionTick", ex);
            }
        }

        protected override void OnEndMission()
        {
            try
            {
                // 关闭快进
                if (_fastForwardActive && Mission != null)
                {
                    Mission.SetFastForwardingFromUI(false);
                    _fastForwardActive = false;
                }
                // 恢复玩家控制
                if (_aiControlActive && Mission?.MainAgent != null)
                {
                    Mission.MainAgent.Controller = AgentControllerType.Player;
                    _aiControlActive = false;
                }
                // 召唤兵结算
                MergeSummonedTroops();
            }
            catch { }
            _heroHpActive = false;
            _heroHpApplied = false;
            _heroHpMul = 1f;
            _troopHpActive = false;
            _troopHpApplied = false;
            _troopHpMul = 1f;
            base.OnEndMission();
        }

        // ═══════════════════════════════════════════════════════
        //  热键处理（按键来自MCM配置，当前硬编码默认值）
        // ═══════════════════════════════════════════════════════

        private void HandleHotkeys(Agent mainAgent)
        {
            // F6：原生委托 + AI托管(延迟2帧) + 快进切换
            if (InputKey.F6.IsPressed() && !_f6Down)
            {
                _f6Down = true;
                string parts = "";

                // 1. 原生委托（有阵型才执行）
                if (_hasFormations)
                {
                    // 触发游戏原生F6委托阵型（系统已经做了，我们只需后续托管）
                }

                // 2. AI托管（延迟2帧让委托先生效）
                if (_aiControlActive || _pendingAIControl > 0)
                {
                    // 关闭模式
                    _pendingAIControl = 0;
                    SetAiControl(false);
                    if (_hasFormations) CancelF6Delegation();
                    parts = "AI托管关";
                }
                else
                {
                    // 开启模式：延迟2帧
                    _pendingAIControl = 2;
                    parts = "AI托管开(延迟)";
                }

                // 3. 快进切换
                SetFastForward(!_fastForwardActive);
                parts += _fastForwardActive ? " + 快进开" : " + 快进关";

                Info($"[F6] {parts}");
                LogService.Log($"[战场] F6: {parts}");
            }
            if (!InputKey.F6.IsPressed()) _f6Down = false;

            // 小键盘1~6：召唤各国精英兵种
            for (int i = 1; i <= 6; i++)
            {
                if (IsKeyPressed(i)) { SummonTroops(i); break; }
            }
        }

        private bool IsKeyPressed(int digit)
        {
            var key = DigitToKey(digit);
            bool pressed = key.IsPressed();
            if (pressed && !_numpadDown[digit]) { _numpadDown[digit] = true; return true; }
            if (!pressed) _numpadDown[digit] = false;
            return false;
        }

        private static InputKey DigitToKey(int digit) => digit switch
        {
            0 => InputKey.Numpad0, 1 => InputKey.Numpad1, 2 => InputKey.Numpad2,
            3 => InputKey.Numpad3, 4 => InputKey.Numpad4, 5 => InputKey.Numpad5,
            6 => InputKey.Numpad6, 7 => InputKey.Numpad7, 8 => InputKey.Numpad8,
            9 => InputKey.Numpad9, _ => InputKey.Numpad0,
        };

        // ═══════════════════════════════════════════════════════
        //  AI托管（不含自由相机）
        // ═══════════════════════════════════════════════════════

        private void SetAiControl(bool enable)
        {
            var agent = Mission?.MainAgent;
            if (agent == null) return;

            if (enable)
            {
                try
                {
                    // 清空交互焦点
                    try
                    {
                        var ctrlType = Type.GetType("TaleWorlds.MountAndBlade.MissionMainAgentController, TaleWorlds.MountAndBlade");
                        if (ctrlType != null)
                        {
                            var getBehavior = typeof(Mission).GetMethod("GetMissionBehavior", new[] { typeof(Type) });
                            var controller = getBehavior?.Invoke(Mission, new object[] { ctrlType });
                            if (controller != null)
                            {
                                var icProp = ctrlType.GetProperty("InteractionComponent");
                                icProp?.GetValue(controller)?.GetType().GetMethod("ClearFocus")?.Invoke(
                                    icProp.GetValue(controller), null);
                            }
                        }
                    }
                    catch { }

                    if (agent.IsUsingGameObject) agent.HandleStopUsingAction();
                    if (agent.AIMoveToGameObjectIsEnabled())
                    {
                        agent.AIMoveToGameObjectDisable();
                        agent.DisableScriptedMovement();
                    }

                    // 移交控制权给AI
                    agent.Controller = AgentControllerType.AI;
                    agent.AIStateFlags = Agent.AIStateFlag.None;

                    // 初始化AI组件
                    agent.CommonAIComponent?.Initialize();
                    if (agent.HumanAIComponent != null)
                    {
                        agent.HumanAIComponent.Initialize();
                        bool mounted = agent.HasMount;
                        bool ranged = HasRangedWeapon(agent);
                        var bvs = !mounted && !ranged ? HumanAIComponent.BehaviorValueSet.Default
                                : !mounted && ranged ? HumanAIComponent.BehaviorValueSet.Default
                                : mounted && !ranged ? HumanAIComponent.BehaviorValueSet.Charge
                                : HumanAIComponent.BehaviorValueSet.Default;
                        agent.HumanAIComponent.SetBehaviorValueSet(bvs);
                    }

                    agent.SetWatchState(Agent.WatchState.Alarmed);
                    agent.SetIsAIPaused(false);

                    // 有阵型时自动冲锋
                    if (_hasFormations) AutoDelegateFormations();

                    _aiControlActive = true;
                    Info("[AI托管] 已开启");
                    LogService.Log("[AI托管] 开启成功");
                }
                catch (Exception ex)
                {
                    LogService.Error("AI托管开启", ex);
                    Info("[AI托管] 开启失败: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    agent.Controller = AgentControllerType.Player;
                    agent.SetWatchState(Agent.WatchState.Patrolling);
                }
                catch { }
                _aiControlActive = false;
                Info("[AI托管] 已关闭（恢复玩家控制）");
            }
        }

        private void AutoDelegateFormations()
        {
            try
            {
                var mission = Mission.Current;
                if (mission?.PlayerTeam == null) return;

                var orderCtrl = mission.PlayerTeam.PlayerOrderController;
                orderCtrl.SelectAllFormations(false);
                var chargeOrder = MovementOrder.MovementOrderCharge;

                // 方式A：通过OrderController
                try
                {
                    var setOrderMethod = orderCtrl.GetType().GetMethod("SetMovementOrder", new[] { typeof(MovementOrder) });
                    setOrderMethod?.Invoke(orderCtrl, new object[] { chargeOrder });
                }
                catch { }

                // 方式B：遍历阵型
                int count = 0;
                foreach (var formation in mission.PlayerTeam.FormationsIncludingSpecialAndEmpty)
                {
                    if (formation == null || formation.CountOfUnits <= 0) continue;
                    if (formation.FormationIndex == FormationClass.NumberOfRegularFormations) continue;
                    try { formation.SetMovementOrder(chargeOrder); count++; }
                    catch { }
                }
                LogService.Log($"自动冲锋完成 (处理 {count} 个阵型)");
            }
            catch (Exception ex) { LogService.Error("AutoDelegateFormations", ex); }
        }

        private void TickCombatAssist(Agent agent)
        {
            if (_tickCounter % 20 != 0) return;
            try
            {
                if (agent.GetCurrentVelocity().LengthSquared > 0.5f) return;
                Agent nearest = null;
                float minDist = float.MaxValue;
                foreach (var other in Mission.Agents)
                {
                    if (other == agent || other.Team == agent.Team || !other.IsActive()) continue;
                    float d = agent.Position.DistanceSquared(other.Position);
                    if (d < minDist) { minDist = d; nearest = other; }
                }
                if (nearest != null && minDist < 2500f)
                {
                    agent.SetWatchState(Agent.WatchState.Alarmed);
                    agent.SetIsAIPaused(false);
                }
            }
            catch { }
        }

        private static bool HasRangedWeapon(Agent agent)
        {
            var eq = agent.SpawnEquipment;
            if (eq == null) return false;
            for (int slot = 0; slot < 6; slot++)
            {
                var w = eq[slot];
                if (w.Item?.WeaponComponent == null) continue;
                foreach (var wd in w.Item.WeaponComponent.Weapons)
                {
                    var wc = wd.WeaponClass;
                    if (wc == WeaponClass.Bow || wc == WeaponClass.Crossbow ||
                        wc == WeaponClass.ThrowingAxe || wc == WeaponClass.ThrowingKnife ||
                        wc == WeaponClass.Javelin || wc == WeaponClass.Stone)
                        return true;
                }
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════
        //  快进
        // ═══════════════════════════════════════════════════════

        private void SetFastForward(bool enable)
        {
            if (Mission == null) return;
            try
            {
                Mission.SetFastForwardingFromUI(enable);
                _fastForwardActive = enable;
                Info(enable ? "[⏩] 4x快进：开" : "[⏩] 4x快进：关");
            }
            catch { Info("[⏩] 快进失败"); }
        }

        // ═══════════════════════════════════════════════════════
        //  HP倍率
        // ═══════════════════════════════════════════════════════

        private void CycleHeroHp()
        {
            // 切换 HP 倍率开关，数值从 MCM 配置读取
            _heroHpActive = !_heroHpActive;
            if (_heroHpActive)
            {
                _heroHpMul = Settings.MCMConfigProvider.GetHpMultiplier();
                _heroHpApplied = true;
                ApplyHeroHp();
                ApplyFriendlyHp();
                Info($"[HP] 主角HP x{_heroHpMul:F0}（开启）");
            }
            else
            {
                // 恢复原始 HP
                _heroHpMul = 1f;
                _heroHpApplied = false;
                ApplyHeroHp();
                ApplyFriendlyHp();
                Info($"[HP] HP倍率已关闭");
            }
        }

        private void ApplyHeroHp()
        {
            var pl = Mission?.MainAgent;
            if (pl == null) return;
            try
            {
                float baseMax = pl.HealthLimit;
                float trueBase = baseMax / (_heroHpMul > 1f ? _heroHpMul : 1f);
                float newMax = trueBase * _heroHpMul;
                SetAgentHealthLimit(pl, newMax);
                if (pl.Health < newMax * 0.95f) pl.Health = newMax;

                if (pl.MountAgent != null)
                {
                    var mount = pl.MountAgent;
                    float mBase = mount.HealthLimit;
                    float mTrue = mBase / (_heroHpMul > 1f ? _heroHpMul : 1f);
                    float mNew = mTrue * _heroHpMul;
                    SetAgentHealthLimit(mount, mNew);
                    if (mount.Health < mNew * 0.95f) mount.Health = mNew;
                }
            }
            catch { }
        }

        private static void SetAgentHealthLimit(Agent a, float v)
        {
            try
            {
                var f = a.GetType().GetField("_healthLimit", BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) { f.SetValue(a, v); return; }
                var p = a.GetType().GetProperty("HealthLimit");
                if (p != null && p.CanWrite) p.SetValue(a, v);
            }
            catch { }
        }

        private void ApplyFriendlyHp()
        {
            if (Mission == null || _troopHpMul <= 1f) return;
            try
            {
                int count = 0;
                foreach (var agent in Mission.Agents)
                {
                    if (agent == null || !agent.IsActive()) continue;
                    if (agent.Team != Mission.PlayerTeam) continue;
                    if (agent.IsHero) continue; // 主角由单独滑块控制
                    if (agent.Character == null) continue;

                    float baseMax = agent.HealthLimit;
                    float trueBase = baseMax / _troopHpMul;
                    float newMax = trueBase * _troopHpMul;
                    SetAgentHealthLimit(agent, newMax);
                    if (agent.Health < newMax * 0.95f) agent.Health = newMax;

                    if (agent.MountAgent != null)
                    {
                        var mount = agent.MountAgent;
                        float mBase = mount.HealthLimit;
                        float mTrue = mBase / _troopHpMul;
                        float mNew = mTrue * _troopHpMul;
                        SetAgentHealthLimit(mount, mNew);
                        if (mount.Health < mNew * 0.95f) mount.Health = mNew;
                    }
                    count++;
                }
                if (count > 0) LogService.Log($"[兵HP] 友方HP已应用: {count}个单位");
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════
        //  召唤精英兵种
        // ═══════════════════════════════════════════════════════

        private void SummonTroops(int slot)
        {
            if (Mission == null || slot < 1 || slot >= _troopSlots.Length) return;
            var pl = Mission.MainAgent;
            if (pl == null) return;

            string troopId = _troopSlots[slot];
            CharacterObject? troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopId)
                                     ?? FindTroopFuzzy(troopId);
            if (troop == null) { Info($"[召唤] 兵种未找到: {troopId}"); return; }

            int count = 0;
            var rng = new Random();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    float angle = (float)(rng.NextDouble() * Math.PI * 2);
                    float radius = 2f + (float)(rng.NextDouble() * 6f);
                    var pos = pl.Position + new Vec3(
                        (float)Math.Cos(angle) * radius,
                        (float)Math.Sin(angle) * radius, 0f);
                    var dir = Vec2.Forward;
                    var origin = new SimpleAgentOrigin(troop, -1, null, default(UniqueTroopDescriptor));
                    var abd = new AgentBuildData(origin)
                        .Team(pl.Team)
                        .InitialPosition(ref pos)
                        .InitialDirection(ref dir)
                        .NoHorses(false)
                        .Equipment(troop.Equipment)
                        .IsReinforcement(true)
                        .CanSpawnOutsideOfMissionBoundary(true);
                    var agent = Mission.SpawnAgent(abd, false);
                    if (agent != null) { agent.SetWatchState(Agent.WatchState.Alarmed); _summonedAgents.Add(agent); count++; }
                }
                catch { }
            }
            Info(count > 0 ? $"[召唤] {count}名 {troop.Name}" : $"[召唤] 失败");
        }

        private CharacterObject? FindTroopFuzzy(string idPart)
        {
            try
            {
                var allChars = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
                foreach (var c in allChars ?? Enumerable.Empty<CharacterObject>())
                    if (c?.StringId?.IndexOf(idPart, StringComparison.OrdinalIgnoreCase) >= 0
                        && c.Occupation == Occupation.Soldier && c.Tier >= 4)
                        return c;
            }
            catch { }
            return null;
        }

        // ═══════════════════════════════════════════════════════
        //  召唤兵结算：存活兵种→加入现有商队/创建新商队
        // ═══════════════════════════════════════════════════════

        private void MergeSummonedTroops()
        {
            try
            {
                if (_summonedAgents.Count == 0) return;
                if (Campaign.Current == null || Hero.MainHero?.Clan == null) { _summonedAgents.Clear(); return; }

                var troopCounts = new Dictionary<CharacterObject, int>();
                foreach (var agent in _summonedAgents)
                {
                    try
                    {
                        if (agent == null || agent.Health <= 0) continue;
                        var ch = agent.Character as CharacterObject;
                        if (ch == null) continue;
                        if (!troopCounts.ContainsKey(ch)) troopCounts[ch] = 0;
                        troopCounts[ch]++;
                    }
                    catch { }
                }
                _summonedAgents.Clear();
                if (troopCounts.Count == 0) return;

                Settlement home = FindRichestPlayerTown();
                if (home == null) { Info("[援兵] 没有城镇，援兵解散"); return; }

                const string tag = "护国商队";
                var existing = MobileParty.All.FirstOrDefault(p =>
                    p?.Name?.ToString()?.StartsWith(tag) == true && p.MemberRoster.TotalManCount < 50);

                if (existing != null)
                {
                    int added = 0;
                    foreach (var kv in troopCounts)
                    {
                        int toAdd = Math.Min(kv.Value, 50 - existing.MemberRoster.TotalManCount - added);
                        if (toAdd > 0) { existing.MemberRoster.AddToCounts(kv.Key, toAdd); added += toAdd; }
                    }
                    Info($"[援兵] 加入商队{existing.Name}，+{added}人");
                }
                else
                {
                    // 加入城镇驻军
                    int added = 0;
                    var garrison = home.Town!.GarrisonParty;
                    if (garrison != null)
                    {
                        foreach (var kv in troopCounts)
                        {
                            garrison.MemberRoster.AddToCounts(kv.Key, kv.Value);
                            added += kv.Value;
                        }
                        Info($"[援兵] 加入{home.Name}驻军，+{added}人");
                    }
                }
            }
            catch (Exception ex) { Info($"[援兵] 结算异常: {ex.Message}"); }
        }

        private Settlement? FindRichestPlayerTown()
        {
            var clan = Hero.MainHero?.Clan;
            if (clan == null) return null;
            Settlement best = null;
            float bestP = -1;
            foreach (var s in Settlement.All)
            {
                if (s.OwnerClan != clan || s.IsVillage) continue;
                float p = s.IsTown ? (s.Town?.Prosperity ?? 0) : 0;
                if (p > bestP) { bestP = p; best = s; }
            }
            return best;
        }

        private void CancelF6Delegation()
        {
            try
            {
                var mission = Mission.Current;
                if (mission?.MainAgent == null) return;
                var team = mission.MainAgent.Team;
                if (team == null) return;
                foreach (var f in team.FormationsIncludingSpecialAndEmpty)
                {
                    if (f == null || f.CountOfUnits <= 0) continue;
                    try { f.SetMovementOrder(MovementOrder.MovementOrderFollow(mission.MainAgent)); }
                    catch { }
                }
            }
            catch { }
        }

        private static void Info(string text)
        {
            try { InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(0xFF00FF00u))); }
            catch { }
        }
    }
}
