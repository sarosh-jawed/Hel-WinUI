using Hel.Application.Contracts;
using Hel.Application.Wizard;
using Hel.App.WinUI.Pages;
using Hel.App.WinUI.Services;
using Hel.App.WinUI.ViewModels;
using Hel.Infrastructure.Configuration;
using Hel.Infrastructure.Csv;
using Hel.Infrastructure.Classification;
using Hel.Infrastructure.Workflow;
using Hel.Infrastructure.Export;
using Hel.Infrastructure.Filtering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    public static MainWindow MainWindow
        => Services.GetRequiredService<MainWindow>();

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

                config.AddJsonFile("config.json", optional: false, reloadOnChange: true);

                string localAppData =
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string localOverridePath = Path.Combine(localAppData, "Hel", "config.local.json");

                config.AddJsonFile(localOverridePath, optional: true, reloadOnChange: true);
            })
            .UseSerilog((context, services, loggerConfig) =>
            {
                var helConfig = HelConfigLoader.LoadAndValidate(context.Configuration);

                string logsRoot = PathTokenResolver.ResolveKnownTokens(helConfig.Output.LogsRoot);

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
                var helConfig = HelConfigLoader.LoadAndValidate(context.Configuration);

                services.AddSingleton(helConfig);
                services.AddSingleton<IConfiguration>(context.Configuration);

                services.AddSingleton<IConfigProvider, ConfigProvider>();
                services.AddSingleton<ICsvIngestService, CsvIngestService>();
                services.AddSingleton<ILocationFilterService, LocationFilterService>();
                services.AddSingleton<IClassificationService, ClassificationService>();
                services.AddSingleton<TextBodyBuilder>();
                services.AddSingleton<ITextExportService, TextExportService>();
                services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();

                services.AddSingleton<WizardState>();
                services.AddSingleton<WizardSessionStore>();
                services.AddSingleton<IStepNavigationService, StepNavigationService>();

                services.AddSingleton<ShellViewModel>();
                services.AddTransient<StartPageViewModel>();
                services.AddTransient<LoadCsvPageViewModel>();
                services.AddTransient<SelectLocationsPageViewModel>();
                services.AddTransient<PreviewResultsPageViewModel>();
                services.AddTransient<ExportFinishPageViewModel>();

                services.AddTransient<StartPage>();
                services.AddTransient<LoadCsvPage>();
                services.AddTransient<SelectLocationsPage>();
                services.AddTransient<PreviewResultsPage>();
                services.AddTransient<ExportFinishPage>();

                services.AddSingleton<MainWindow>();

                services.AddLogging();
            })
            .Build();
    }
}
