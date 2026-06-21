using System;

namespace MorghsCheats.Settings
{
    /// <summary>
    /// 安全读取 MCM 配置，MCM 未加载时返回默认值
    /// </summary>
    public static class MCMConfigProvider
    {
        public static bool IsSpeedLockEnabled()
        {
            try { var s = Instance(); return s != null && s.EnableSpeedLock; }
            catch { return false; }
        }

        public static float GetHpMultiplier()
        {
            try { var s = Instance(); return s?.BattleHpMultiplier ?? 1f; }
            catch { return 1f; }
        }

        public static float GetTroopHpMultiplier()
        {
            try { var s = Instance(); return s?.TroopHpMultiplier ?? 1f; }
            catch { return 1f; }
        }

        public static int GetTargetSkillLevel()
        {
            try { var s = Instance(); return (int)(s?.TargetSkillLevel ?? 350f); }
            catch { return 350; }
        }

        public static int GetCompanionLimitBonus()
        {
            try { var s = Instance(); return (int)(s?.CompanionLimitBonus ?? 0f); }
            catch { return 0; }
        }

        public static int GetPartyLimitBonus()
        {
            try { var s = Instance(); return (int)(s?.PartyLimitBonus ?? 0f); }
            catch { return 0; }
        }

        public static bool IsKillPromotionEnabled()
        {
            try { var s = Instance(); return s != null && s.EnableKillPromotion; }
            catch { return false; }
        }

        public static int GetKillPromotionThreshold()
        {
            try { var s = Instance(); return (int)(s?.KillPromotionThreshold ?? 1000f); }
            catch { return 1000; }
        }

        private static MorghsCheatsSettings? Instance()
        {
            try { return MorghsCheatsSettings.Instance; }
            catch { return null; }
        }
    }
}
