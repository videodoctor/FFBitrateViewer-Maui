using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels.DesignData;

public partial class DesignMainViewModel : ViewModelBase
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

}
