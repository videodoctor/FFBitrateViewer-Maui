using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;
using FFBitrateViewer.ApplicationAvalonia.Views;
using Hmb.ProcessRunner;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace FFBitrateViewer.ApplicationAvalonia;

public static class ServiceCollectionExtensions
{
    public static void AddFFBitrateViewerServices(this IServiceCollection collection)
    {
        collection.AddSingleton<ProcessService>();
        collection.AddSingleton<FileDialogService>();
        collection.AddSingleton<GuiService>();
        collection.AddSingleton<FFProbeClient>();
        collection.AddSingleton<PlotControllerFacade>();
        collection.AddSingleton<IPlotStrategy, FrameBasedPlotStrategy>();
        collection.AddSingleton<IPlotStrategy, SecondBasedPlotStrategy>();
        collection.AddSingleton<IPlotStrategy, GOPBasedPlotStrategy>();
    }

    public static void AddFFBitrateViewerViewModels(this IServiceCollection collection)
    {
        // Views
        collection.AddSingleton<BitRateView>();
        collection.AddSingleton<AboutView>();

        // ViewModels
        collection.AddSingleton<FileItemViewModel>();
        collection.AddSingleton<BitRateViewModel>();
        collection.AddSingleton<AboutViewModel>();

        collection.AddSingleton<IScreen, MainWindowViewModel>();
    }

}