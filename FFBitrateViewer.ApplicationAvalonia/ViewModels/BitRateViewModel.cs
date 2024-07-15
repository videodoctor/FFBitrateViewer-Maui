using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Extensions;
using FFBitrateViewer.ApplicationAvalonia.Models.Config;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class BitRateViewModel(
    GuiService guiService,
    FileDialogService fileDialogService,
    FFProbeClient probeAppClient,
    PlotControllerFacade plotControllerFacade,
    ILogger<BitRateViewModel> logger,
    IOptions<Models.Config.ApplicationOptions> applicationOptions,
    IScreen screen,
    IServiceProvider serviceProvider
    ) : RoutableViewModelBase(screen, nameof(BitRateViewModel))
{

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private bool _isPlotterOn = false;

    [ObservableProperty]
    private bool _hasToAdjustFrameStartTime = false;

    [ObservableProperty]
    private ScottPlot.IPlotControl? _plotController;

    [ObservableProperty]
    private FileItemViewModel? _selectedFile;

    [ObservableProperty]
    private PlotViewType _plotView = PlotViewType.FrameBased;

    public System.Collections.IList? SelectedFiles { get; set; }

    private readonly PlotControllerFacade _plotControllerFacade = plotControllerFacade;

    public ObservableCollection<FileItemViewModel> Files { get; } = [];

    private readonly GuiService _guiService = guiService;

    private readonly FileDialogService _fileDialogService = fileDialogService;

    private readonly FFProbeClient _probeAppClient = probeAppClient;

    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;

    private readonly ILogger _logger = logger;

    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private static readonly SaveFileFilterOption SavePlotImagesOption = new(
        "All Image formats",
        ["*.bmp", "*.jpg", "*.png", "*.svg", "*.webp"],
        ["public.image"],
        ["image/*"]
    );

    private static readonly OpenFileFilterOption OpenVideoFilesOption = new(
        "All Video formats",
        ["*.264", "*.avi", "*.avs", "*.h264", "*.hevc", "*.m2ts", "*.mkv", "*.mov", "*.mp4", "*.mpeg", "*.mpg", "*.mts", "*.mxf", "*.ts", "*.webm"],
        ["public.video"],
        ["video/*"]
    );

    private bool _hasBeenLoaded = false;

    [RelayCommand]
    private async Task OnLoaded(CancellationToken token)
    {
        if (_hasBeenLoaded)
        { return; }

        // Set up plot controller for the plot view
        if (PlotController is not null)
        {
            _plotControllerFacade.PlotController = PlotController;
        }

        // Sets the plot view based on the CLI input
        SetPlotViewType(_applicationOptions.PlotView);

        // initialize the plot view
        _plotControllerFacade.Initialize(_plotControllerFacade.PlotStrategy.AxisYLegendTitle, _guiService.IsDarkTheme);
        _plotControllerFacade.Refresh();

        // gets version of the ffprobe
        var version = await _probeAppClient.GetVersionAsync(token).ConfigureAwait(false);
        _guiService.RunLater(() =>
        {
            Version = $"{Path.GetFileName(_probeAppClient.FFProbeFilePath)} v{version}";
        });

        // load files from CLI
        var localFiles = _applicationOptions.Files.Select(f => new LocalFileEntry(f));
        await AddFilesAsync(localFiles, token).ConfigureAwait(false);

        // renders plot on autorun
        if (_applicationOptions.AutoRun)
        {
            await ToggleOnOffPlotterPlotter(token).ConfigureAwait(false);
        }

        _hasBeenLoaded = true;
    }

    [RelayCommand]
    private async Task AddFiles(CancellationToken token)
    {
        IEnumerable<IFileEntry> fileInfoEntries = await _fileDialogService.OpenAsync("Open video file", false, OpenVideoFilesOption).ConfigureAwait(false);

        // Prevent duplicated files by name.
        StringComparer stringComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var existingFiles = Files.Select(f => f.Path.LocalPath).ToHashSet(stringComparer);
        fileInfoEntries = fileInfoEntries.Where(f => !existingFiles.Contains(f.Path.LocalPath));

        await AddFilesAsync(fileInfoEntries, token).ConfigureAwait(false);
    }

    [RelayCommand]
    private void RemoveSelectedFiles()
    {
        if (SelectedFiles is null || SelectedFiles.Count == 0)
        { return; }

        RemoveFiles(SelectedFiles.OfType<FileItemViewModel>());
    }

    [RelayCommand]
    private void RemoveAllFiles()
    {
        RemoveFiles(Files);
    }

    [RelayCommand]
    private void Exit()
    {
        _guiService.Exit();
    }

    [RelayCommand(IncludeCancelCommand = true, FlowExceptionsToTaskScheduler = true)]
    private async Task ToggleOnOffPlotterPlotter(CancellationToken cancellationToken)
    {

        cancellationToken.ThrowIfCancellationRequested();

        // process files in parallel
        await Parallel.ForEachAsync(Files.Where(file => file.IsActive), cancellationToken, async (file, token) =>
        {
            // Skip file is it already has a plot
            if (file.ScattersByType[_plotControllerFacade.PlotView] is not null)
            { return; }

            // Check if Probe Packets were already loaded
            var hasFrames = file.Frames.Count > 0;
            if (hasFrames is false)
            {
                var probePacketChannel = Channel.CreateUnbounded<FFProbePacket>();

                var probePacketProducerTask = Task.Run(async () =>
                {
                    await _probeAppClient.GetProbePacketsAsync(probePacketChannel, file.Path.LocalPath).ConfigureAwait(false);
                    probePacketChannel.Writer.TryComplete();
                }, token);

                var probePacketConsumerTask = Task.Run(async () =>
                {
                    file.Frames.AddRange(await probePacketChannel.Reader.ReadAllAsync().ToListAsync());
                }, token);

                await Task.WhenAll(probePacketProducerTask, probePacketConsumerTask).ConfigureAwait(false);
            }

            // Once all probe packets are received, we compute max and average
            var bitRateAverage = double.IsNaN(file.BitRateAverage) ? file.GetAverageBitRate(magnitudeOrder: 1000) : file.BitRateAverage;
            var bitRateMaximum = double.IsNaN(file.BitRateMaximum) ? file.GetBitRateMaximum(magnitudeOrder: 1000) : file.BitRateMaximum;

            await _guiService.RunNowAsync(() =>
            {
                file.BitRateAverage = bitRateAverage;
                file.BitRateMaximum = bitRateMaximum;
                file.FrameCount = file.Frames.Count;
            }).ConfigureAwait(false);

            // Gather data points for plotting
            // NOTE: This could be done in `probePacketConsumerTask` but 
            //       some plot strategies use BitRates which depends having all frames
            List<double> xs = [];
            List<int> ys = [];
            for (int frameIndex = 0; frameIndex < file.Frames.Count; frameIndex++)
            {
                FFProbePacket? frame = file.Frames[frameIndex];
                var (x, y) = _plotControllerFacade.PlotStrategy.GetDataPoint(file.StartTime, frame);
                xs.Add(x ?? 0);
                ys.Add(Convert.ToInt32(y));
            }

            // Add scatter to plot view
            (ScottPlot.IPlottable? scatter, System.Drawing.Color scatterLineColor) = _plotControllerFacade.InsertScatter(xs, ys, Path.GetFileName(file.Path.LocalPath));
            file.ScattersByType[_plotControllerFacade.PlotView] = scatter;
            file.ScatterLineColor = scatterLineColor;
        });

        // Request Plot to adjust viewport and redraw
        _plotControllerFacade.AutoScaleViewport();
        _plotControllerFacade.Refresh();
    }

    [RelayCommand]
    private void AutoScale()
    {
        _plotControllerFacade.AutoScaleViewport();
        _plotControllerFacade.Refresh();
    }

    [RelayCommand]
    private void PlotPointerMoved(Avalonia.Input.PointerEventArgs pointerEventArgs)
        => _plotControllerFacade.HandleMouseMoved(pointerEventArgs);

    [RelayCommand]
    private async Task SavePlotToFile(CancellationToken token)
    {
        await Task.Yield();
        var file = await _fileDialogService.SaveAsync("Save Plot", SavePlotImagesOption).ConfigureAwait(false);

        if (file is null)
        { return; }

        _logger.LogDebug("Save Plot to file {targetFilename}", file.Path);
        _plotControllerFacade.SavePlotImage(file.Path.LocalPath);

    }

    [RelayCommand]
    private void GoToAboutView()
    {
        AboutViewModel aboutViewModel = _serviceProvider.GetService<AboutViewModel>()!;
        HostScreen.Router.Navigate.Execute(aboutViewModel);
    }

    [RelayCommand]
    private async Task RefreshMediaInfo(CancellationToken token)
    {
        await Task.Yield();
        if (Files is null)
        { return; }

        List<IFileEntry> fileInfoEntries = new(Files.Count);

        // Remove plot references, and computed plot data
        bool hasAnyPlot = false;
        foreach (var file in Files)
        {
            fileInfoEntries.Add(file.FileEntry!);

            foreach (var kvp in file.ScattersByType)
            {
                var scatter = kvp.Value;
                if (scatter is null)
                { continue; }

                hasAnyPlot = hasAnyPlot || scatter.IsVisible;

                scatter.IsVisible = false;

                _plotControllerFacade.RemoveScatter(scatter);
            }

            file.Frames?.Clear();
            file.BitRateAverage = double.NaN;
            file.BitRateMaximum = double.NaN;
            file.FrameCount = 0;
        }

        // Remove all files
        Files.Clear();

        // Readd the files and regenerates media info
        await AddFilesAsync(fileInfoEntries, token).ConfigureAwait(false);

        // If there is any plot, then regenerate it
        if (hasAnyPlot)
        {
            await _guiService.RunNowAsync(async () => await ToggleOnOffPlotterPlotterCommand.ExecuteAsync(default));
        }
    }

    //[RelayCommand]
    //private async Task CopyPlotToClipboard(CancellationToken token)
    //{
    //    byte[]? imageBytes = _plotControllerFacade.GetPlotImageAsStream();

    //    if (imageBytes is null) 
    //    { return; }

    //    token.ThrowIfCancellationRequested();

    //    await _guiService.SetBitmapToClipboard(imageBytes).ConfigureAwait(false);
    //}

    partial void OnPlotViewChanged(global::FFBitrateViewer.ApplicationAvalonia.Models.Media.PlotViewType value)
        => SetPlotViewType(value);

    private void SetPlotViewType(PlotViewType newPlotViewType)
    {
        // Updates value in plot control facade
        _plotControllerFacade.PlotView = newPlotViewType;

        // Update plot settings for each file
        foreach (var file in Files)
        {
            file.ScatterLineColor = PlotControllerFacade.TransparentColor;
            foreach (var plotViewType in Enum.GetValues<PlotViewType>())
            {
                // TODO: Compute plots for `PlotView`
                if (plotViewType == newPlotViewType && file.ScattersByType[plotViewType] is null)
                {
                }

                if (file.ScattersByType[plotViewType] is null)
                {
                    continue;
                }
                // Update plot visibility(Hide plots different from `PlotView`, show the others
                file.ScattersByType[plotViewType]!.IsVisible = file.IsActive && plotViewType == newPlotViewType;
                if (file.ScattersByType[plotViewType] is ScottPlot.Plottables.Scatter scatter && scatter.IsVisible)
                {
                    file.ScatterLineColor = scatter.LineColor.ToDrawingColor();
                }
            }
        }

        _plotControllerFacade.AxisYTitleLabel = _plotControllerFacade.PlotStrategy.AxisYLegendTitle;
        _plotControllerFacade.AutoScaleViewport();
        _plotControllerFacade.Refresh();
    }

    private async Task AddFilesAsync(IEnumerable<IFileEntry> fileInfoEntries, CancellationToken token = default)
    {
        await Parallel.ForEachAsync(fileInfoEntries, token, async (fileInfo, token) =>
        {
            _logger.LogDebug("Adding file {localFilePath}", Path.GetFileName(fileInfo.Path.LocalPath));
            var mediaInfo = await _probeAppClient.GetMediaInfoAsync(fileInfo.Path.LocalPath, cancellationToken: token).ConfigureAwait(false);
            FileItemViewModel fileItemViewModel = new()
            {
                FileEntry = fileInfo,
                MediaInfo = mediaInfo,
                PlotControllerFacade = _plotControllerFacade,
                IsActive = true
            };
            fileItemViewModel.Initialize();

            // Add file to Data Grid
            await _guiService.RunNowAsync(() =>
            {
                Files.Add(fileItemViewModel);
            }).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private void RemoveFiles(IEnumerable<FileItemViewModel> files)
    {
        var filesToRemove = files.ToArray();

        if (filesToRemove.Length == 0)
        { return; }

        foreach (var file in filesToRemove)
        {
            foreach (var plotViewType in Enum.GetValues<PlotViewType>())
            {
                if (file.ScattersByType[plotViewType] is null)
                { continue; }

                _plotControllerFacade.RemoveScatter(file.ScattersByType[plotViewType]);
            }
            Files.Remove(file);
        }

        _plotControllerFacade.Refresh();

    }
}