using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using FFBitrateViewer.ApplicationAvalonia.ViewModels;
using FFBitrateViewer.ApplicationAvalonia.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FFBitrateViewer.ApplicationAvalonia;

public partial class App : Application
{
    public Models.Config.ApplicationOptions? ApplicationOptions { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
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
            }));

        // Creates a ServiceProvider containing services from the provided IServiceCollection
        var services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
