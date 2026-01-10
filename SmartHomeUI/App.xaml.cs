using System.Windows;
using SmartHomeUI.Data;
using System;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHomeUI.Services;

namespace SmartHomeUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private SmartThingsPollingService? _polling;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Ensure SQLite database exists
        using (var db = new SmartHomeDbContext())
        {
            db.Database.EnsureCreated();
        }
        // Ensure schema compatibility with older DBs (users/devices/automations)
        SqliteMigrator.EnsureUserColumns();
        SqliteMigrator.EnsureDeviceColumns();
        Services.DeviceService.InitializeDispatcher(System.Windows.Threading.Dispatcher.CurrentDispatcher);
        HookGlobalExceptionHandlers();
        SmartHomeUI.Services.AutomationService.Start();
        _polling = new SmartThingsPollingService(new SmartThingsPollingOptions(), NullLogger<SmartThingsPollingService>.Instance);
        _polling.StartAsync(CancellationToken.None);
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_polling is not null)
        {
            _polling.StopAsync().GetAwaiter().GetResult();
        }
        base.OnExit(e);
    }

    private void HookGlobalExceptionHandlers()
    {
        this.DispatcherUnhandledException += (s, exArgs) =>
        {
            try { File.AppendAllText("error.log", MaskSensitive($"[UI] {DateTime.Now}: {exArgs.Exception}\n\n")); } catch { }
            MessageBox.Show($"Unexpected error:\n{exArgs.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            exArgs.Handled = true; // keep app alive to diagnose further
        };
        AppDomain.CurrentDomain.UnhandledException += (s, exArgs) =>
        {
            try { File.AppendAllText("error.log", MaskSensitive($"[Domain] {DateTime.Now}: {exArgs.ExceptionObject}\n\n")); } catch { }
        };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, exArgs) =>
        {
            try { File.AppendAllText("error.log", MaskSensitive($"[Task] {DateTime.Now}: {exArgs.Exception}\n\n")); } catch { }
            exArgs.SetObserved();
        };
    }

    private static string MaskSensitive(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        // Mask Bearer tokens and long base64/hex-like strings
        var masked = Regex.Replace(input, @"Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*", "Bearer [REDACTED]", RegexOptions.IgnoreCase);
        masked = Regex.Replace(masked, @"[A-Fa-f0-9]{32,}", "[HEX_REDACTED]");
        masked = Regex.Replace(masked, @"[A-Za-z0-9\+\/]{32,}={0,2}", "[B64_REDACTED]");
        return masked;
    }
}




