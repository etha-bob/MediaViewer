using System;
using System.Windows;
using System.Windows.Threading;
using MediaBrowser.Services;

namespace MediaBrowser
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Register global exception handlers before anything else so that
            // even errors during WPF initialisation are captured in log.txt.
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            Logger.LogStartupDiagnostics();
            Logger.Info("Startup: beginning application initialisation");

            try
            {
                base.OnStartup(e);
                Logger.Info("Startup: application initialised successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Startup: fatal error during initialisation", ex);
                MessageBox.Show(
                    $"MediaBrowser failed to start.\n\nError: {ex.Message}\n\nDetails may have been written to log.txt in the application folder.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info($"Application exiting (code {e.ApplicationExitCode})");
            base.OnExit(e);
        }

        // ── Global exception handlers ─────────────────────────────────────────

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error("Unhandled UI exception", e.Exception);
            MessageBox.Show(
                $"An unexpected error occurred.\n\nError: {e.Exception.Message}\n\nDetails may have been written to log.txt in the application folder.",
                "Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception
                     ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown error");
            Logger.Error($"Fatal unhandled exception (IsTerminating={e.IsTerminating})", ex);
        }
    }
}
