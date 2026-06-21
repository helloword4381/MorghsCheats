using System;

namespace MorghsCheats
{
    /// <summary>
    /// 运行时配置桥接（MCM 持久化 + 热键切换的运行时状态）
    /// 热键开关写入这里，MCM 设置也通过这里控制功能
    /// </summary>
    public static class ModSettings
    {
        public static bool EnableSpeedLock { get; set; } = false;
    }
}
