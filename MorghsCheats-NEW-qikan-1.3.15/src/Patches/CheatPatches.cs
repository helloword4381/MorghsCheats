using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Module = TaleWorlds.MountAndBlade.Module;

namespace MorghsCheats.Patches
{
    public static class CheatPatches
    {
        private static bool _patchesInstalled = false;
        private static Harmony? _harmony;

        public static void Install()
        {
            if (_patchesInstalled) return;
            _patchesInstalled = true;

            _harmony = new Harmony("morghs.morghscheats.patches");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            PatchSpeedLock();
            PatchClanLimits();

            LogService.Log("[Harmony] 所有 Patch 安装完成");
        }

        // ═══════════════════════════════════════════════════
        //  时间控制 — 锁3倍速
        // ═══════════════════════════════════════════════════

        private static void PatchSpeedLock()
        {
            try
            {
                var mbMethod = typeof(Module).GetMethod("OnApplicationTick",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (mbMethod == null) return;

                _harmony!.Patch(mbMethod, postfix: new HarmonyMethod(typeof(CheatPatches),
                    nameof(Postfix_OnTick)));
                LogService.Log("[Harmony] Module.OnApplicationTick 已 Patch");
            }
            catch (Exception ex) { LogService.Error("PatchSpeedLock", ex); }
        }

        private static int _speedTick = 0;

        public static void Postfix_OnTick(Module __instance)
        {
            try
            {
                if (Campaign.Current == null || Hero.MainHero == null) return;
                if (Mission.Current != null) return;
                if (Settlement.CurrentSettlement != null) return; // 城镇UI中不执行任何操作

                _speedTick++;
                if (_speedTick % 60 != 0) return;

                if (ModSettings.EnableSpeedLock || Settings.MCMConfigProvider.IsSpeedLockEnabled())
                {
                    var ctm = Campaign.Current.TimeControlMode;
                    if (ctm == CampaignTimeControlMode.StoppablePlay)
                        Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
                }
            }
            catch (Exception ex) { LogService.Error("Postfix_OnTick", ex); }
        }

        // ═══════════════════════════════════════════════════
        //  家族上限补丁
        // ═══════════════════════════════════════════════════

        private static void PatchClanLimits()
        {
            try
            {
                var compMethod = typeof(DefaultClanTierModel).GetMethod("GetCompanionLimit",
                    BindingFlags.Public | BindingFlags.Instance);
                if (compMethod != null)
                {
                    _harmony!.Patch(compMethod, postfix: new HarmonyMethod(typeof(CheatPatches),
                        nameof(Postfix_CompanionLimit)));
                    LogService.Log("[Harmony] 伙伴上限补丁已安装");
                }

                var partyMethod = typeof(DefaultClanTierModel).GetMethod("GetPartyLimitForTier",
                    BindingFlags.Public | BindingFlags.Instance);
                if (partyMethod != null)
                {
                    _harmony!.Patch(partyMethod, postfix: new HarmonyMethod(typeof(CheatPatches),
                        nameof(Postfix_PartyLimit)));
                    LogService.Log("[Harmony] 部队上限补丁已安装");
                }

                LogService.Log("[Harmony] 工坊上限已禁用");
            }
            catch (Exception ex) { LogService.Error("PatchClanLimits", ex); }
        }

        public static void Postfix_CompanionLimit(ref int __result)
        {
            __result += Settings.MCMConfigProvider.GetCompanionLimitBonus();
        }

        public static void Postfix_PartyLimit(ref int __result)
        {
            __result += Settings.MCMConfigProvider.GetPartyLimitBonus();
        }
    }
}
