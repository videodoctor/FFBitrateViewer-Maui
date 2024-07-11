using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FFBitrateViewer.ApplicationAvalonia.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System;

namespace FFBitrateViewer.ApplicationAvalonia;

public partial class App : Application
{
    public Models.Config.ApplicationOptions? ApplicationOptions { get; set; }

    internal IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Creates the configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", Design.IsDesignMode)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("FFBITRATEVIEWER_ENVIRONMENT") ?? "Production"}.json", true)
            .Build();

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        collection.AddLogging(c =>
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            c.AddProvider(new SerilogLoggerProvider(logger, dispose: true));
        });
        collection.AddFFBitrateViewerServices();
        collection.AddFFBitrateViewerViewModels();
        collection.AddOptions<Models.Config.ApplicationOptions>()
            .Configure((options =>
            {
                if (ApplicationOptions is null)
                { return; }

                options.StartTimeAdjustment = ApplicationOptions.StartTimeAdjustment;
                options.Exit = ApplicationOptions.Exit;
                options.LogCommands = ApplicationOptions.LogCommands;
                options.AutoRun = ApplicationOptions.AutoRun;
                options.TempDir = ApplicationOptions.TempDir;
                options.Files = ApplicationOptions.Files;
                options.PlotView = ApplicationOptions.PlotView;
                options.FFProbeFilePath = ApplicationOptions.FFProbeFilePath;
            }));

        // Creates a ServiceProvider containing services from the provided IServiceCollection
        var services = collection.BuildServiceProvider();
        ServiceProvider = services;

        Microsoft.Extensions.Logging.ILogger logger = services.GetService<ILogger<App>>()!;
        logger.LogInformation("Command Line: {commandLine}", Environment.CommandLine);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindowView
            {
                DataContext = services.GetRequiredService<ReactiveUI.IScreen>()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindowView
            {
                DataContext = services.GetRequiredService<ReactiveUI.IScreen>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}