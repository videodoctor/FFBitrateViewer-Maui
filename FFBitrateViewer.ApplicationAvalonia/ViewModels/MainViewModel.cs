using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Extensibility.OxyPlot;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]

    private bool _isPlotterOn = false;

    [ObservableProperty]
    private FileItemViewModel? _selectedFile;

    public ObservableCollection<FileItemViewModel> Files { get; } = new();
    public FFProbePlotModel PlotModel { get; } = new(string.Empty);

    private readonly AppProcessService _appProcessService = new();
    private readonly FileDialogService _fileDialogService = new();
    private readonly FFProbeAppClient _ffprobeAppClient = new();


    [RelayCommand]
    private async Task OnLoaded(CancellationToken token)
    {
        var version = await _ffprobeAppClient.GetVersionAsync();
        Version = $"{System.IO.Path.GetFileName(_ffprobeAppClient.FFProbeFilePath)} v{version}";
    }

    [RelayCommand]
    private async Task AddFiles(CancellationToken token)
    {
        var fileInfoEntries = await _fileDialogService.OpenAsync(IsSingleSelection: false);
        for (int fileInfoIndex = 0; fileInfoIndex < fileInfoEntries.Count; fileInfoIndex++)
        {
            var fileInfo = fileInfoEntries[fileInfoIndex];
            const bool isFileSelected = true;
            var filePath = fileInfo.Path.LocalPath;
            var mediaInfo = await _ffprobeAppClient.GetMediaInfoAsync(filePath);
            var fileItemViewModel = new FileItemViewModel(fileInfo, mediaInfo) { IsSelected = isFileSelected };

            // Add file to Data Grid
            Files.Add(fileItemViewModel);

            // Add Series to data grid
            int idx = Files.Count - 1;
            PlotModel.SerieSet(null, PlotModel.SerieCreate(filePath, isFileSelected, idx));
            PlotModel.Redraw();
        }
        SelectedFile = Files.LastOrDefault();
    }

    [RelayCommand(IncludeCancelCommand = true, FlowExceptionsToTaskScheduler = true)]
    private async Task ToggleOnOffPlotterPlotter(CancellationToken token)
    {
        var selectedFiles = Files.Where(f => f.IsSelected).ToImmutableArray();
        for (int fileIndex = 0; fileIndex < selectedFiles.Length; fileIndex++)
        {
            token.ThrowIfCancellationRequested();

            FileItemViewModel? file = selectedFiles[fileIndex];
            await foreach (var probePacket in _ffprobeAppClient.GetProbePackets(file.Path.LocalPath, token: token))
            {
                token.ThrowIfCancellationRequested();
            }
        }
    }

    [RelayCommand]
    private void Exit()
    {
        _appProcessService.Exit();
    }

}
