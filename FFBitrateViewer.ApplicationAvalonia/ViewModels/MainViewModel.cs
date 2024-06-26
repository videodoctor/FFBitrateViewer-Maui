using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFBitrateViewer.ApplicationAvalonia.Models;
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;
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

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]

    private bool _isPlotterOn = false;

    [ObservableProperty]
    private bool _hasToAdjustFrameStartTime = false;

    [ObservableProperty]
    private FileItemViewModel? _selectedFile;

    public ObservableCollection<FileItemViewModel> Files { get; } = new();

    //public PlotModel? PlotModelData { get; set; }
    //public PlotController? PlotControllerData { get; set; }

    public IPlotControl? PlotController { get; set; }

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

    private string AxisXTickLabel(double duration) =>
          (duration < 60) ? @"m\:ss"
        : (duration < 60 * 60) ? @"mm\:ss"
        : (duration < 60 * 60 * 24) ?  @"h\:mm\:ss"
        : @"d\.hh\:mm\:ss"
        ;

    private readonly UIApplicationService _uiApplicationService = new();

    private readonly FileDialogService _fileDialogService = new();

    private readonly FFProbeClient _probeAppClient = new();


    [RelayCommand]
    private async Task OnLoaded(CancellationToken token)
    {

        var version = await _probeAppClient.GetVersionAsync(token).ConfigureAwait(false);
        Version = $"{System.IO.Path.GetFileName(_probeAppClient.FFProbeFilePath)} v{version}";
        
        SetPlotLabels();

        if (PlotController is not null)
        {
            //PlotController.Plot.RenderManager.RenderStarting += (s, e) =>
            //{
            //    Tick[] ticks = PlotController.Plot.Axes.Bottom.TickGenerator.Ticks;
            //    for (int i = 0; i < ticks.Length; i++)
            //    {
            //        string tickLabel =  TimeSpan.FromSeconds(ticks[i].Position).ToString(AxisXTickLabel(ticks[i].Position));
            //        ticks[i] = new Tick(ticks[i].Position, tickLabel);
            //    }
            //};

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

                    PlotController.Refresh();
                }
            }
            catch { 
                // NOTE: Ignoring error when trying to set dark theme
            }

            // Change style for the tick labels
            PlotController.Plot.Axes.Bottom.TickLabelStyle.Rotation = 45;
            PlotController.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

        }


        // setting up Plot View
        //PlotControllerData.UnbindMouseDown(OxyMouseButton.Left);
        //PlotControllerData.BindMouseEnter(PlotCommands.HoverSnapTrack);

        // TODO: Move to XAML when possible
        #region Move to XAML when possible 
        //var legend = PlotModelData.Legends.FirstOrDefault();
        //if (legend != null)
        //{
        //    legend.ShowInvisibleSeries = false;
        //    legend.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
        //}
        //for (int axisIndex = 0; axisIndex < PlotModelData.Axes.Count; axisIndex++)
        //{
        //    OxyPlot.Axes.Axis? axis = PlotModelData.Axes[axisIndex];
        //    axis.MajorGridlineColor = OxyColor.FromArgb(40, 0, 0, 139);
        //    axis.MinorGridlineColor = OxyColor.FromArgb(20, 0, 0, 139);

        //    axis.AbsoluteMaximum = axis.Maximum = 1;
        //    if (axisIndex == 0)
        //    {
        //        axis.AbsoluteMaximum = axis.Maximum = 10;
        //        axis.StringFormat = AxisXStringFormatBuild(axis.Maximum);
        //    }
        //    if (axisIndex == 1)
        //    {
        //        axis.Title = AxisYTitleLabel;
        //        axis.Unit = AxisYTickLabelSuffix;
        //    }

        //}
        //for (int serieIndex = 0; serieIndex < PlotModelData.Series.Count; serieIndex++)
        //{
        //    OxyPlot.Series.Series? serie = PlotModelData.Series[serieIndex];
        //    serie.TrackerFormatString = TrackerFormatStringBuild;
        //}
        #endregion

    }

    [RelayCommand]
    private void SetPlotViewType(PlotViewType plotViewType)
    {
        _plotViewType = plotViewType;
        SetPlotLabels();
    }

    private void SetPlotLabels()
    {
        if (PlotController is null)
        { return; }

        PlotController.Plot.Axes.Left.Label.Text = this.AxisYTitleLabel;
        PlotController.Refresh();
    }

    [RelayCommand]
    private async Task AddFiles(CancellationToken token)
    {
        // TODO: Check if we really need to use Task.Run
        await Task.Run(async () =>
        {
            var fileInfoEntries = await _fileDialogService.OpenAsync(IsSingleSelection: false).ConfigureAwait(false);
            for (int fileInfoIndex = 0; fileInfoIndex < fileInfoEntries.Count; fileInfoIndex++)
            {
                var fileInfo = fileInfoEntries[fileInfoIndex];
                var mediaInfo = await _probeAppClient.GetMediaInfoAsync(fileInfo.Path.LocalPath).ConfigureAwait(false);
                var fileItemViewModel = new FileItemViewModel(fileInfo, mediaInfo) { IsSelected = true };

                // Add file to Data Grid
                _uiApplicationService.FireAndForget(() =>
                {
                    Files.Add(fileItemViewModel);
                });
            }
            SelectedFile = Files.LastOrDefault();
            //PlotModelData!.InvalidatePlot(updateData: true);
        }, token).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true, FlowExceptionsToTaskScheduler = true)]
    private async Task ToggleOnOffPlotterPlotter(CancellationToken cancellationToken)
    {

        cancellationToken.ThrowIfCancellationRequested();

        // for each selected file
        await Parallel.ForEachAsync(
            Files.Where(file => file.IsSelected),
            cancellationToken,
            async (file, token) =>
            {
                // gets frames
                List<double> xs = [];
                List<int> ys = [];
                var probePacketChannel = Channel.CreateUnbounded<FFProbePacket>();

                var producer = Task.Run(async () =>
                {
                    await _probeAppClient.GetProbePacketsAsync(probePacketChannel, file.Path.LocalPath).ConfigureAwait(false);
                    probePacketChannel.Writer.TryComplete();
                }, token);

                var consumer = Task.Run(async () =>
                {
                    await foreach (var probePacket in probePacketChannel.Reader.ReadAllAsync())
                    {
                        file.Frames.Add(probePacket);
                        var (x, y) = GetDataPoint(probePacket, file, _plotViewType);
                        xs.Add(x ?? 0);
                        ys.Add(Convert.ToInt32(y));
                    }
                }, token);

                await Task.WhenAll(producer, consumer).ConfigureAwait(false);

                // Once all probe packets are received, we compute max and average
                var bitRateAverage = file.GetRefreshedBitRateAverage();
                var bitRateMaximum = file.RefreshBitRateMaximum();
                _uiApplicationService.FireAndForget(() =>
                {
                    file.BitRateAverage = bitRateAverage;
                    file.BitRateMaximum = bitRateMaximum;
                });

                //    (var maxX, var maxY) =
                //        fileAxis.Aggregate(
                //            seed: (MaxX: -1.0, MaxY: -1.0),
                //            func: (accumulated, current) => (
                //                Math.Max(accumulated.MaxX, current.AxisX ?? -1.0),
                //                Math.Max(accumulated.MaxY, current.AxisY ?? -1.0)
                //            )
                //        );

                //    // Adjust axis x based on max duration
                //    if (maxX > 0)
                //    {
                //        //var axisX = PlotModelData!.Axes[0];
                //        //axisX.AbsoluteMaximum = axisX.Maximum = maxX;
                //        //axisX.StringFormat = AxisXStringFormatBuild(maxX);
                //    }

                //    // Adjust axis y based on Frames or Time
                //    if (maxY > 0)
                //    {
                //        //var axisY = PlotModelData!.Axes[1];
                //        //axisY.AbsoluteMaximum = axisY.Maximum = maxY;
                //    }

                var scatter = PlotController?.Plot.Add.Scatter(xs, ys)!;
                scatter.LegendText = Path.GetFileName(file.Path.LocalPath);
                scatter.ConnectStyle = ConnectStyle.StepHorizontal;
                //scatter.Smooth = true;
            }
        );

      
        PlotController?.Plot.ShowLegend(Alignment.LowerRight, Orientation.Horizontal);
        PlotController?.Plot.Axes.AutoScale();
        PlotController?.Refresh();

        ////PlotModelData!.Series.Clear();

        //await Task.Run(async () =>
        //{

        //    token.ThrowIfCancellationRequested();

        //    var tasks = Files
        //        .Where(file => file.IsSelected)
        //        .Select(file => PopulateFramesAndSeries(file, token));
        //    var fileAxis = await Task.WhenAll(tasks).ConfigureAwait(false);

        //    (var maxX, var maxY) =
        //        fileAxis.Aggregate(
        //            seed: (MaxX: -1.0, MaxY: -1.0),
        //            func: (accumulated, current) => (
        //                Math.Max(accumulated.MaxX, current.AxisX ?? -1.0),
        //                Math.Max(accumulated.MaxY, current.AxisY ?? -1.0)
        //            )
        //        );

        //    // Adjust axis x based on max duration
        //    if (maxX > 0)
        //    {
        //        //var axisX = PlotModelData!.Axes[0];
        //        //axisX.AbsoluteMaximum = axisX.Maximum = maxX;
        //        //axisX.StringFormat = AxisXStringFormatBuild(maxX);
        //    }

        //    // Adjust axis y based on Frames or Time
        //    if (maxY > 0)
        //    {
        //        //var axisY = PlotModelData!.Axes[1];
        //        //axisY.AbsoluteMaximum = axisY.Maximum = maxY;
        //    }


        //}, token).ConfigureAwait(false);

        ////PlotModelData!.InvalidatePlot(updateData: true);
    }

    //private async Task PopulateFramesAndSeries(FileItemViewModel file, CancellationToken token)
    //{
    //    token.ThrowIfCancellationRequested();

    //    var fileName = Path.GetFileName(file.Path.LocalPath);

    //    var probePacketChannel = Channel.CreateUnbounded<FFProbePacket>();

    //    var producer = Task.Run(async () =>
    //    {
    //        await _probeAppClient.GetProbePackets(probePacketChannel, file.Path.LocalPath);
    //    });

    //    var consumer = Task.Run(async () =>
    //    {
    //        await foreach (var probePacket in probePacketChannel.Reader.ReadAllAsync())
    //        {

    //            file.Frames.Add(probePacket);
    //            //series.Points.Add(GetDataPoint(probePacket, file, _plotViewType));

    //            // Computes axis x based on max duration
    //            var axisX = GetAxisXForFile(file);

    //            // Computes axis y based on Frames or Time
    //            var axisY = GetAxisYForFile(file);

    //            // Refresh BitRateAverage and BitRateMaximum
    //            var bitRateAverage = file.GetRefreshedBitRateAverage();
    //            var bitRateMaximum = file.RefreshBitRateMaximum();

    //            _uiApplicationService.FireAndForget(() =>
    //            {
    //                file.BitRateAverage = bitRateAverage;
    //                file.BitRateMaximum = bitRateMaximum;
    //                //PlotModelData!.Series.Add(series);
    //            });

    //            //return (axisX, axisY);
    //        }
    //    });

    //    await Task.WhenAll(producer, consumer);
    //}

    //private StairStepSeries GetNewSerie(string fileName)
    //    => new()
    //    {
    //        IsVisible = true,
    //        StrokeThickness = 1.5,
    //        Title = fileName,
    //        TrackerFormatString = TrackerFormatStringBuild,
    //        Decimator = Decimator.Decimate,
    //        LineJoin = LineJoin.Miter,
    //        VerticalStrokeThickness = 0.5,
    //        VerticalLineStyle = LineStyle.Dash,
    //        LineStyle = LineStyle.Solid,
    //        MarkerType = MarkerType.None,
    //        //Color = style.Color; 
    //    };

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
            PlotViewType.SecondBased => file.GetBitRateMaximum(),
            _ => throw new NotImplementedException($"{nameof(GetAxisYForFile)} for Plot Type {_plotViewType} is not implemented.")
        } / 1000;

    private static double? GetAxisXForFile(FileItemViewModel file)
        => file.GetDuration();

    [RelayCommand]
    private void Exit()
    {
        _uiApplicationService.Exit();
    }

}