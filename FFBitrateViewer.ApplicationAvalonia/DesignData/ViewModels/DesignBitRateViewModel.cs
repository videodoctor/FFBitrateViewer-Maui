using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.ViewModels;
using ScottPlot;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.DesignData.ViewModels;

public partial class DesignBitRateViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private bool _isPlotterOn = false;

    [ObservableProperty]
    private bool _hasToAdjustFrameStartTime = false;

    [ObservableProperty]
    private IPlotControl? _plotController;

    [ObservableProperty]
    private FileItemViewModel? _selectedFile;

    [ObservableProperty]
    private PlotViewType _plotView = PlotViewType.FrameBased;

    public ObservableCollection<FileItemViewModel> Files { get; } = [];

    [RelayCommand]
    private async Task OnLoaded(CancellationToken token) { await Task.Yield(); }

    [RelayCommand]
    private void SetPlotViewType(PlotViewType plotViewType) { }

    [RelayCommand]
    private async Task AddFiles(CancellationToken token) { await Task.Yield(); }

    [RelayCommand]
    private void RemoveSelectedFiles() { }

    [RelayCommand]
    private void RemoveAllFiles() { }

    [RelayCommand]
    private void Exit() { }

    [RelayCommand(IncludeCancelCommand = true, FlowExceptionsToTaskScheduler = true)]
    private async Task ToggleOnOffPlotterPlotter(CancellationToken cancellationToken) { await Task.Yield(); }

    [RelayCommand]
    private void AutoScale() { }

    [RelayCommand]
    private void PlotPointerMoved(Avalonia.Input.PointerEventArgs pointerEventArgs) { }

    [RelayCommand]
    private async Task SavePlotToFile(CancellationToken token) { await Task.Yield(); }

    [RelayCommand]
    private void GoToAboutView() { }

    [RelayCommand]
    private async Task RefreshMediaInfo() { await Task.Yield(); }

}