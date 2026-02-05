using Hel.Application.Abstractions;
using Hel.Infrastructure.Csv;
using Hel.Infrastructure.Workflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.IO;

namespace Hel.App.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private IHost? _host;

    public static IServiceProvider Services
        => ((App)Current).GetServiceProvider();

    private IServiceProvider GetServiceProvider()
    {
        if (_host is null)
            throw new InvalidOperationException("Host is not initialized.");

        return _host.Services;
    }

    public App()
    {
        InitializeComponent();
        _host = CreateHost();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Create the window via DI (lets us inject services into UI later).
        var window = Services.GetRequiredService<MainWindow>();
        window.Activate();
    }

    private static IHost CreateHost()
    {
        // IMPORTANT: We intentionally avoid "using Hel.Application" in this UI project,
        // because "Application" is a WinUI type name and can conflict with namespaces.

        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Base path = app folder
                config.SetBasePath(AppContext.BaseDirectory);

                // 1) Required base config
                config.AddJsonFile("config.json", optional: false, reloadOnChange: true);

                // 2) Optional local override beside app (dev convenience)
                config.AddJsonFile("config.local.json", optional: true, reloadOnChange: true);

                // 3) Optional local override in LocalAppData (user/machine specific)
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var localConfigPath = Path.Combine(localAppData, "Hel", "config.local.json");
                config.AddJsonFile(localConfigPath, optional: true, reloadOnChange: true);
            })
            .UseSerilog((context, services, loggerConfig) =>
            {
                // Log folder derived from config, with fallback.
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var logsRoot = context.Configuration["App:LogsRoot"]
                    ?.Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase)
                    ?? Path.Combine(localAppData, "Hel", "Logs");

                Directory.CreateDirectory(logsRoot);

                loggerConfig
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .WriteTo.File(
                        path: Path.Combine(logsRoot, "Hel-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        shared: true);
            })
            .ConfigureServices((context, services) =>
            {
                // --- Config ---
                services.AddSingleton<IConfiguration>(context.Configuration);

                // --- App window ---
                services.AddSingleton<MainWindow>();

                // --- Core services (Phase 1 placeholders) ---
                services.AddSingleton<ICsvLoader, CsvLoader>();
                services.AddSingleton<IWorkflowRunner, WorkflowRunner>();

                // Logging via Serilog is already wired; also keep MS ILogger available.
                services.AddLogging();
            })
            .Build();
    }
}
