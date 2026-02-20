using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MediaBrowser.Services
{
    /// <summary>
    /// Thread-safe file logger that writes to log.txt in the application directory.
    /// Designed to never throw — logging errors are silently swallowed so they
    /// cannot cause secondary crashes.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "log.txt");

        private static readonly object _fileLock = new();

        public static void Info(string message)  => Write("INFO",  message);
        public static void Warn(string message)  => Write("WARN",  message);
        public static void Error(string message) => Write("ERROR", message);

        public static void Error(string message, Exception ex)
            => Write("ERROR", $"{message}{Environment.NewLine}  Exception: {ex}");

        /// <summary>
        /// Writes startup diagnostics: app version, executable path, OS, .NET runtime.
        /// Call this as the very first thing in OnStartup.
        /// </summary>
        public static void LogStartupDiagnostics()
        {
            var asm = Assembly.GetExecutingAssembly();
            Info("============================================================");
            Info("=== MediaBrowser Starting ===");
            Info($"Version   : {asm.GetName().Version}");
            Info($"Executable: {AppDomain.CurrentDomain.BaseDirectory}");
            Info($"WorkingDir: {Environment.CurrentDirectory}");
            Info($"OS        : {RuntimeInformation.OSDescription}");
            Info($".NET      : {RuntimeInformation.FrameworkDescription}");
            Info($"Arch      : {RuntimeInformation.ProcessArchitecture}");
            Info("============================================================");
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private static void Write(string level, string message)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level,-5}] {message}";
                lock (_fileLock)
                {
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Never let a logging failure crash the application.
            }
        }
    }
}
