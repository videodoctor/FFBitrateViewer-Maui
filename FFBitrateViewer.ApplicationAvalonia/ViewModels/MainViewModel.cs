﻿using CommunityToolkit.Mvvm.ComponentModel;
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
            var mediaInfo = await _ffprobeAppClient.GetMediaInfoAsync(fileInfo.Path.LocalPath);
            var fileItemViewModel = new FileItemViewModel(fileInfo, mediaInfo);
            Files.Add(fileItemViewModel);
        }
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
