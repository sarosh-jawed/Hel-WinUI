using Hel.Application.Contracts;
using Hel.Infrastructure.Configuration;
using Hel.Infrastructure.Csv;
using Hel.Infrastructure.Classification;
using Hel.Infrastructure.Workflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.IO;
using Hel.Infrastructure.Filtering;

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
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();

                config.SetBasePath(AppContext.BaseDirectory);

                // Base shipped config with the app
                config.AddJsonFile("config.json", optional: false, reloadOnChange: true);

                // Optional machine/user-specific override
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string localOverridePath = Path.Combine(localAppData, "Hel", "config.local.json");

                config.AddJsonFile(localOverridePath, optional: true, reloadOnChange: true);
            })
            .UseSerilog((context, services, loggerConfig) =>
            {
                var helConfig = HelConfigLoader.LoadAndValidate(context.Configuration);

                string logsRoot = PathTokenResolver.ResolveLocalAppDataTokens(helConfig.Output.LogsRoot);

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
                // Bind and validate once at startup.
                var helConfig = HelConfigLoader.LoadAndValidate(context.Configuration);

                services.AddSingleton(helConfig);

                services.AddSingleton<IConfiguration>(context.Configuration);
                services.AddSingleton<MainWindow>();

                services.AddSingleton<IConfigProvider, ConfigProvider>();
                services.AddSingleton<ICsvIngestService, CsvIngestService>();
                services.AddSingleton<ILocationFilterService, LocationFilterService>();
                services.AddSingleton<IClassificationService, ClassificationService>();
                services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();

                services.AddLogging();
            })
            .Build();
    }
}
