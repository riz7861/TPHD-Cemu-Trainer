using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace TphdCemuTrainer;

public partial class App : Application
{
    private static readonly object LogLock = new();

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private static string StartupLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "startup.log");

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException(e.ExceptionObject?.ToString() ?? "Unknown non-Exception failure.");

        LogException("AppDomain.CurrentDomain.UnhandledException", exception);
        ShowExceptionMessage(exception);
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("TaskScheduler.UnobservedTaskException", e.Exception);
        ShowExceptionMessage(e.Exception);
        e.SetObserved();
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("DispatcherUnhandledException", e.Exception);
        ShowExceptionMessage(e.Exception);
        e.Handled = true;
    }

    private static void LogException(string source, Exception exception)
    {
        try
        {
            var logDirectory = Path.GetDirectoryName(StartupLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var entry = $"""
                [{DateTimeOffset.Now:O}] {source}
                {exception}

                """;

            lock (LogLock)
            {
                File.AppendAllText(StartupLogPath, entry);
            }
        }
        catch
        {
            // Last-resort logger: never let logging throw while handling a startup exception.
        }
    }

    private static void ShowExceptionMessage(Exception exception)
    {
        try
        {
            var message = $"""
                TPHD Cemu Trainer hit an unexpected startup error.

                {exception.GetType().Name}: {exception.Message}

                Details were written to:
                {StartupLogPath}
                """;

            MessageBox.Show(
                message,
                "TPHD Cemu Trainer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Avoid recursive failures if WPF is already shutting down.
        }
    }
}
