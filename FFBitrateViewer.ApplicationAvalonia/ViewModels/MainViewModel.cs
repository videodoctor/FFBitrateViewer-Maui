using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Models.Config;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using Microsoft.Extensions.Options;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

// TODO: Following dependencies make the preview designer to crash. Consider use DesignData or ViewModelLocator
public partial class MainViewModel(
    GuiService guiService,
    FileDialogService fileDialogService,
    FFProbeClient probeAppClient,
    IEnumerable<IPlotStrategy> plotStrategies,
    IOptions<Models.Config.ApplicationOptions> applicationOptions
    ) : ViewModelBase
{

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]

    private bool _isPlotterOn = false;

    [ObservableProperty]
    private bool _hasToAdjustFrameStartTime = false;

    [ObservableProperty]
    private FileItemViewModel? _selectedFile;

    [ObservableProperty]

    public IPlotControl? _plotController;

    private PlotControllerFacade _plotControllerFacade = PlotControllerFacade.None;

    partial void OnPlotControllerChanging(global::ScottPlot.IPlotControl? value)
        => _plotControllerFacade = new PlotControllerFacade(value);

    public ObservableCollection<FileItemViewModel> Files { get; } = [];

    private PlotViewType _plotViewType = PlotViewType.FrameBased;

    public IPlotStrategy PlotStrategy => _plotStrategies[_plotViewType];

    private readonly IDictionary<PlotViewType, IPlotStrategy> _plotStrategies = plotStrategies.ToDictionary(p => p.PlotViewType);

    private string TrackerFormatStringBuild => $@"{{0}}{Environment.NewLine}Time={{2:hh\:mm\:ss\.fff}}{Environment.NewLine}{{3}}={{4:0}} {PlotStrategy.AxisYTickLabelSuffix}";

    private readonly GuiService _guiService = guiService;

    private readonly FileDialogService _fileDialogService = fileDialogService;

    private readonly FFProbeClient _probeAppClient = probeAppClient;

    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;

    [RelayCommand]
    private async Task OnLoaded(CancellationToken token)
    {
        // initialize the plot view
        _plotControllerFacade.Initialize(PlotStrategy.AxisYTitleLabel, _guiService.IsDarkTheme);
        _plotControllerFacade.Refresh();

        // gets version of the ffprobe
        var version = await _probeAppClient.GetVersionAsync(token).ConfigureAwait(false);
        Version = $"{Path.GetFileName(_probeAppClient.FFProbeFilePath)} v{version}";

        // load files from CLI
        var localFiles = _applicationOptions.Files.Select(f => new LocalFileEntry(f));
        await AddFilesAsync(localFiles, token).ConfigureAwait(false);

        // renders plot on autorun
        if (_applicationOptions.AutoRun)
        {
            await ToggleOnOffPlotterPlotter(token).ConfigureAwait(false);
        }

    }

    [RelayCommand]
    private void SetPlotViewType(PlotViewType plotViewType)
    {
        _plotViewType = plotViewType;

        _plotControllerFacade.AxisYTitleLabel = PlotStrategy.AxisYTitleLabel;
        _plotControllerFacade.Refresh();
    }

    [RelayCommand]
    private async Task AddFiles(CancellationToken token)
    {

        var fileInfoEntries = await _fileDialogService.OpenAsync(IsSingleSelection: false).ConfigureAwait(false);
        await AddFilesAsync(fileInfoEntries, token).ConfigureAwait(false);
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
        await Parallel.ForEachAsync(Files.Where(file => file.IsSelected), cancellationToken, async (file, token) =>
        {

            List<double> xs = [];
            List<int> ys = [];
            var probePacketChannel = Channel.CreateUnbounded<FFProbePacket>();

            var probePacketProducerTask = Task.Run(async () =>
            {
                await _probeAppClient.GetProbePacketsAsync(probePacketChannel, file.Path.LocalPath).ConfigureAwait(false);
                probePacketChannel.Writer.TryComplete();
            }, token);

            var probePacketConsumerTask = Task.Run(async () =>
            {
                await foreach (var probePacket in probePacketChannel.Reader.ReadAllAsync())
                {
                    file.Frames.Add(probePacket);
                    var (x, y) = PlotStrategy.GetDataPoint(file.StartTime, probePacket);
                    xs.Add(x ?? 0);
                    ys.Add(Convert.ToInt32(y));
                }
            }, token);

            await Task.WhenAll(probePacketProducerTask, probePacketConsumerTask).ConfigureAwait(false);

            // Once all probe packets are received, we compute max and average
            var bitRateAverage = file.GetAverageBitRate(magnitudeOrder: 1000);
            var bitRateMaximum = file.GetBitRateMaximum(magnitudeOrder: 1000);

            _guiService.RunLater(() =>
            {
                file.BitRateAverage = bitRateAverage;
                file.BitRateMaximum = bitRateMaximum;
            });

            // Add scatter to plot view
            _plotControllerFacade.AddScatter(xs, ys, Path.GetFileName(file.Path.LocalPath));
            
        });

        // Request Plot to adjust viewport and redraw
        _plotControllerFacade.AutoScaleViewport();
        _plotControllerFacade.Refresh();
    }

    private async Task AddFilesAsync(IEnumerable<IFileEntry> fileInfoEntries, CancellationToken token = default)
    {
        await Parallel.ForEachAsync(fileInfoEntries, token, async (fileInfo, token) =>
        {
            var mediaInfo = await _probeAppClient.GetMediaInfoAsync(fileInfo.Path.LocalPath, cancellationToken: token).ConfigureAwait(false);
            var fileItemViewModel = new FileItemViewModel(fileInfo, mediaInfo) { IsSelected = true };

            // Add file to Data Grid
            await _guiService.RunNowAsync(() =>
            {
                Files.Add(fileItemViewModel);
            }).ConfigureAwait(false);
        }).ConfigureAwait(false);

        SelectedFile = Files.LastOrDefault();
    }

}