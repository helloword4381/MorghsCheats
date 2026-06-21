using System;
using System.IO;

namespace MorghsCheats
{
    public static class LogService
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "MorghsCheats_debug.log");

        public static void Log(string msg)
        {
            try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n"); }
            catch { }
        }

        public static void Error(string source, Exception ex)
        {
            Log($"!! [{source}] {ex.GetType().Name}: {ex.Message}");
            Log($"!! Stack: {ex.StackTrace}");
        }

        public static void LogHeroOp(string op, string detail)
        {
            Log($"[作弊] {op}: {detail}");
        }
    }
}
