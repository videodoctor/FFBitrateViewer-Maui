using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FFBitrateViewer.ApplicationAvalonia;

public static class ServiceCollectionExtensions
{
    public static void AddFFBitrateViewerServices(this IServiceCollection collection)
    {
        collection.AddSingleton<FileDialogService>();
        collection.AddSingleton<GuiService>();
        //collection.AddSingleton<OSPlatformClient>();
        collection.AddSingleton<FFProbeClient>();
        collection.AddSingleton<IPlotStrategy, FrameBasedPlotStrategy>();
        collection.AddSingleton<IPlotStrategy, SecondBasedPlotStrategy>();
        collection.AddSingleton<IPlotStrategy, GOPBasedPlotStrategy>();
    }

    public static void AddFFBitrateViewerViewModels(this IServiceCollection collection)
    {
        collection.AddTransient<FileItemViewModel>();
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<ViewModelBase>();
    }

}
