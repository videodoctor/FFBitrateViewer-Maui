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

public partial class MainViewModel(
    GuiService guiService,
    FileDialogService fileDialogService,
    FFProbeClient probeAppClient,
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

    public ObservableCollection<FileItemViewModel> Files { get; } = [];


    private PlotViewType _plotViewType = PlotViewType.FrameBased;

    private string AxisYTickLabelSuffix => _plotViewType switch
    {
        PlotViewType.FrameBased => "kb",
        PlotViewType.SecondBased => "kb/s",
        PlotViewType.GOPBased => "kb/GOP",
        _ => throw new NotImplementedException($"Text for {nameof(AxisYTickLabelSuffix)} equals to {_plotViewType} is not implemented.")
    };

    private string AxisYTitleLabel => _plotViewType switch
    {
        PlotViewType.FrameBased => "Frame size",
        PlotViewType.SecondBased => "Bit rate",
        PlotViewType.GOPBased => "Bit rate",
        _ => throw new NotImplementedException($"Text for {nameof(AxisYTitleLabel)} equals to {_plotViewType} is not implemented.")
    };

    private string TrackerFormatStringBuild => $@"{{0}}{Environment.NewLine}Time={{2:hh\:mm\:ss\.fff}}{Environment.NewLine}{{3}}={{4:0}} {AxisYTickLabelSuffix}";

    private string AxisXTickLabelFormatter(double duration) =>
          (duration < 60) ? TimeSpan.FromSeconds(duration).ToString(@"m\:ss")
        : (duration < 60 * 60) ? TimeSpan.FromSeconds(duration).ToString(@"mm\:ss")
        : (duration < 60 * 60 * 24) ? TimeSpan.FromSeconds(duration).ToString(@"h\:mm\:ss")
        : TimeSpan.FromSeconds(duration).ToString(@"d\.hh\:mm\:ss")
        ;

    private readonly GuiService _guiService = guiService;

    private readonly FileDialogService _fileDialogService = fileDialogService;

    private readonly FFProbeClient _probeAppClient = probeAppClient;

    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;

    [RelayCommand]
    private async Task OnLoaded(CancellationToken token)
    {

        var version = await _probeAppClient.GetVersionAsync(token).ConfigureAwait(false);
        Version = $"{Path.GetFileName(_probeAppClient.FFProbeFilePath)} v{version}";

        InitializePlotController();

        var localFiles = _applicationOptions.Files.Select(f => new LocalFileEntry(f));
        await AddFilesAsync(localFiles, token).ConfigureAwait(false);

        if (_applicationOptions.AutoRun)
        {
            await ToggleOnOffPlotterPlotter(token).ConfigureAwait(false);
        }

    }

    [RelayCommand]
    private void SetPlotViewType(PlotViewType plotViewType)
    {
        _plotViewType = plotViewType;

        if (PlotController is null)
        { return; }

        PlotController.Plot.Axes.Left.Label.Text = this.AxisYTitleLabel;
        PlotController.Refresh();
    }

    [RelayCommand]
    private async Task AddFiles(CancellationToken token)
    {

        var fileInfoEntries = await _fileDialogService.OpenAsync(IsSingleSelection: false).ConfigureAwait(false);
        await AddFilesAsync(fileInfoEntries, token).ConfigureAwait(false);
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
                    var (x, y) = GetDataPoint(probePacket, file, _plotViewType);
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
            var scatter = PlotController?.Plot.Add.Scatter(xs, ys)!;
            scatter.LegendText = Path.GetFileName(file.Path.LocalPath);
            scatter.ConnectStyle = ConnectStyle.StepHorizontal;
            //scatter.Smooth = true;
        });

        // Request Plot to adjust viewport and redraw
        PlotController?.Plot.Axes.AutoScale();
        PlotController?.Refresh();
    }

    private (double? X, double Y) GetDataPoint(FFProbePacket probePacket, FileItemViewModel file, PlotViewType plotViewType)
        => plotViewType switch
        {
            PlotViewType.FrameBased => ((probePacket.PTSTime ?? 0) - file.StartTime, Convert.ToDouble(probePacket.Size) / 1000.0),
            //PlotViewType.SecondBased => new DataPoint(),
            _ => throw new NotImplementedException($"{nameof(GetDataPoint)} for Plot Type {_plotViewType} is not implemented.")
        };

    private double? GetAxisYForFile(FileItemViewModel file)
        => _plotViewType switch
        {
            PlotViewType.FrameBased => file.Frames.Max(f => f.Size),
            PlotViewType.SecondBased => file.GetBitRateMaximum(magnitudeOrder: 1000),
            _ => throw new NotImplementedException($"{nameof(GetAxisYForFile)} for Plot Type {_plotViewType} is not implemented.")
        } / 1000;

    private void InitializePlotController()
    {
        if (PlotController is null)
        {
            return;
        }

        try
        {
            var platformClient = OSPlatformClient.GetOSPlatformClient();
            if (platformClient.IsDark())
            {
                PlotController.Plot.Add.Palette = new ScottPlot.Palettes.Penumbra();
                // change figure colors
                PlotController.Plot.FigureBackground.Color = Color.FromHex("#181818");
                PlotController.Plot.DataBackground.Color = Color.FromHex("#1f1f1f");

                // change axis and grid colors
                PlotController.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
                PlotController.Plot.Grid.MajorLineColor = Color.FromHex("#404040");

                // change legend colors
                PlotController.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
                PlotController.Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
                PlotController.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");
            }
        }
        catch
        {
            // NOTE: Ignoring error when trying to set dark theme
        }

        // Showing the left title
        PlotController.Plot.Axes.Left.Label.Text = this.AxisYTitleLabel;

        // Change style for the tick labels
        PlotController.Plot.Axes.Bottom.TickLabelStyle.Rotation = 45;
        PlotController.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

        // create a custom tick generator using your custom label formatter
        ScottPlot.TickGenerators.NumericAutomatic myTickGenerator = new()
        {
            LabelFormatter = AxisXTickLabelFormatter
        };
        PlotController.Plot.Axes.Bottom.TickGenerator = myTickGenerator;

        // Shows title label in the left side
        PlotController.Plot.ShowLegend(Alignment.LowerRight, Orientation.Horizontal);

        // refresh the plot
        PlotController.Refresh();
    }

}