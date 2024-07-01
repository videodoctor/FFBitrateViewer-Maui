using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Models.Config;
using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
using Microsoft.Extensions.Options;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

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
    private IPlotControl? _plotController;

    [ObservableProperty]
    private FileItemViewModel? _selectedFile;

    [ObservableProperty]
    private PlotViewType _plotView = PlotViewType.FrameBased;

    public System.Collections.IList? SelectedFiles { get; set; }

    private PlotControllerFacade _plotControllerFacade = PlotControllerFacade.None;

    partial void OnPlotControllerChanging(global::ScottPlot.IPlotControl? value)
        => _plotControllerFacade = new PlotControllerFacade(value);

    public ObservableCollection<FileItemViewModel> Files { get; } = [];

    private IPlotStrategy PlotStrategy => _plotStrategies[PlotView];

    private readonly IDictionary<PlotViewType, IPlotStrategy> _plotStrategies = plotStrategies.ToDictionary(p => p.PlotViewType);

    private string TrackerFormatStringBuild => $@"{{0}}{Environment.NewLine}Time={{2:hh\:mm\:ss\.fff}}{Environment.NewLine}{{3}}={{4:0}} {PlotStrategy.AxisYTickLabelSuffix}";

    private readonly GuiService _guiService = guiService;

    private readonly FileDialogService _fileDialogService = fileDialogService;

    private readonly FFProbeClient _probeAppClient = probeAppClient;

    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;

    [RelayCommand]
    private async Task OnLoaded(CancellationToken token)
    {
        // Sets the plot view based on the CLI input
        SetPlotViewType(_applicationOptions.PlotView);

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
        PlotView = plotViewType;

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
        await Parallel.ForEachAsync(Files.Where(file => file.IsSelected), cancellationToken, async (file, token) =>
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


            // Once all probe packets are received, we compute max and average
            var bitRateAverage = file.GetAverageBitRate(magnitudeOrder: 1000);
            var bitRateMaximum = file.GetBitRateMaximum(magnitudeOrder: 1000);

            _guiService.RunLater(() =>
            {
                file.BitRateAverage = bitRateAverage;
                file.BitRateMaximum = bitRateMaximum;
            });

            // Gather data points for plotting
            // NOTE: This could be done in `probePacketConsumerTask` but 
            //       some plot strategies use BitRates which depends having all frames
            List<double> xs = [];
            List<int> ys = [];
            for (int frameIndex = 0; frameIndex < file.Frames.Count; frameIndex++)
            {
                FFProbePacket? frame = file.Frames[frameIndex];
                var (x, y) = PlotStrategy.GetDataPoint(file.StartTime, frame);
                xs.Add(x ?? 0);
                ys.Add(Convert.ToInt32(y));
            }

            // Add scatter to plot view
            file.Scatter = _plotControllerFacade.InsertScatter(xs, ys, Path.GetFileName(file.Path.LocalPath));

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
    {
        // Get the control that raised the event
        var avaPlot = (ScottPlot.Avalonia.AvaPlot)pointerEventArgs.Source!;

        // Get the position relative to the control
        var position = pointerEventArgs.GetPosition(avaPlot);

        // determine where the mouse is
        Pixel mousePixel = new(position.X, position.Y);
        Coordinates mouseLocation = avaPlot.Plot.GetCoordinates(mousePixel);

        // get the nearest point of each scatter
        Dictionary<int, DataPoint> nearestPoints = new();
        var MyScatters = avaPlot.Plot.PlottableList.OfType<Scatter>().ToList();
        for (int i = 0; i < MyScatters.Count; i++)
        {
            DataPoint nearestPoint = MyScatters[i].Data.GetNearest(mouseLocation, avaPlot.Plot.LastRender);
            nearestPoints.Add(i, nearestPoint);
        }

        // determine which scatter's nearest point is nearest to the mouse
        bool pointSelected = false;
        int scatterIndex = -1;
        double smallestDistance = double.MaxValue;
        for (int i = 0; i < nearestPoints.Count; i++)
        {
            if (nearestPoints[i].IsReal)
            {
                // calculate the distance of the point to the mouse
                double distance = nearestPoints[i].Coordinates.Distance(mouseLocation);
                if (distance < smallestDistance)
                {
                    // store the index
                    scatterIndex = i;
                    pointSelected = true;
                    // update the smallest distance
                    smallestDistance = distance;
                }
            }
        }

        // place the crosshair, marker and text over the selected point
        if (pointSelected)
        {
            ScottPlot.Plottables.Scatter scatter = MyScatters[scatterIndex];
            DataPoint point = nearestPoints[scatterIndex];

            //MyCrosshair.IsVisible = true;
            //MyCrosshair.Position = point.Coordinates;
            //MyCrosshair.LineColor = scatter.MarkerStyle.FillColor;

            //MyHighlightMarker.IsVisible = true;
            //MyHighlightMarker.Location = point.Coordinates;
            //MyHighlightMarker.MarkerStyle.LineColor = scatter.MarkerStyle.FillColor;

            //MyHighlightText.IsVisible = true;
            //MyHighlightText.Location = point.Coordinates;
            //MyHighlightText.LabelText = $"{point.X:0.##}, {point.Y:0.##}";
            //MyHighlightText.LabelFontColor = scatter.MarkerStyle.FillColor;

            avaPlot.Refresh();
            string text = $"Selected Scatter={scatter.LegendText}, Index={point.Index}, X={point.X:0.##}, Y={point.Y:0.##}";
            Debug.WriteLine(text);
        }

        //// hide the crosshair, marker and text when no point is selected
        //if (!pointSelected && MyCrosshair.IsVisible)
        //{
        //    MyCrosshair.IsVisible = false;
        //    MyHighlightMarker.IsVisible = false;
        //    MyHighlightText.IsVisible = false;
        //    avaPlot.Refresh();
        //    string text = $"No point selected";
        //    Debug.WriteLine(text);
        //}
    }

    private async Task AddFilesAsync(IEnumerable<IFileEntry> fileInfoEntries, CancellationToken token = default)
    {
        await Parallel.ForEachAsync(fileInfoEntries, token, async (fileInfo, token) =>
        {
            var mediaInfo = await _probeAppClient.GetMediaInfoAsync(fileInfo.Path.LocalPath, cancellationToken: token).ConfigureAwait(false);
            FileItemViewModel fileItemViewModel = new()
            {
                FileEntry = fileInfo,
                MediaInfo = mediaInfo,
                IsSelected = true
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
            _plotControllerFacade.RemoveScatter(file.Scatter);
            Files.Remove(file);
        }

        _plotControllerFacade.Refresh();

    }
}